param([Parameter(Mandatory=$true)][string]$SetupPath)
# End-to-end checks for TatsiaCraft-Setup in quiet mode. Uses a unique directory under artifacts and a test-only
# registry root (HKCU\Software\TatsiaCraftSetupTest-*), so the real installation and registration are never touched.
$ErrorActionPreference='Stop'
$sourceRoot=Split-Path $PSScriptRoot -Parent
$version=(Get-Content -LiteralPath (Join-Path $sourceRoot 'VERSION') -Raw).Trim()
$token=[guid]::NewGuid().ToString('N').Substring(0,8)
$dir=Join-Path $sourceRoot ('artifacts\setup-'+$token)
$reg='Software\TatsiaCraftSetupTest-'+$token
$checks=0
function Assert($condition,$message){if(!$condition){throw $message};$script:checks++}
function Run($exe,[string[]]$arguments){$p=Start-Process -FilePath $exe -ArgumentList $arguments -PassThru -WindowStyle Hidden;if(!$p.WaitForExit(300000)){Stop-Process -Id $p.Id;throw "Timed out: $exe"};return $p.ExitCode}
function Key($sub){return [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($reg+'\'+$sub)}
try{
 $common=@('/quiet',('"/dir='+$dir+'"'),('"/regroot='+$reg+'"'))
 Assert ((Run $SetupPath ($common))-eq 0) 'Fresh quiet install failed.'
 $manifest=Get-Content -LiteralPath (Join-Path $dir 'install-manifest.json') -Raw | ConvertFrom-Json
 Assert ($manifest.version -eq $version -and $manifest.application -eq 'TatsiaCraft') 'Installed manifest version mismatch.'
 foreach($f in $manifest.files){$item=Get-Item -LiteralPath (Join-Path $dir $f.path);Assert ($item.Length -eq $f.bytes) ("Size mismatch: "+$f.path)}
 foreach($f in @($manifest.files | Where-Object {$_.bytes -lt 4000000} | Select-Object -First 40)){Assert ((Get-FileHash -LiteralPath (Join-Path $dir $f.path) -Algorithm SHA256).Hash -eq $f.sha256) ("Hash mismatch: "+$f.path)}
 foreach($required in @('TasiaCraft.exe','Uninstall TatsiaCraft.exe','tools\ffmpeg.exe','tools\ffprobe.exe','tools\LICENSE-FFmpeg.txt','LICENSE')){Assert (Test-Path -LiteralPath (Join-Path $dir $required)) "Missing $required"}
 Assert ((Get-Item -LiteralPath (Join-Path $dir 'TasiaCraft.exe')).VersionInfo.ProductVersion -eq $version) 'Installed editor version mismatch.'
 $u=Key 'Microsoft\Windows\CurrentVersion\Uninstall\TatsiaCraft';Assert ($u -ne $null -and $u.GetValue('DisplayVersion') -eq $version -and $u.GetValue('DisplayName') -eq 'TatsiaCraft') 'Apps & Features entry missing or wrong.'
 Assert ($u.GetValue('UninstallString') -like '*Uninstall TatsiaCraft.exe*/uninstall' -and $u.GetValue('QuietUninstallString') -like '*/uninstall /quiet') 'Uninstall commands missing.'
 Assert ([int]$u.GetValue('EstimatedSize') -gt 50000) 'Estimated size missing.'
 Assert ((Key 'Microsoft\Windows\CurrentVersion\App Paths\TatsiaCraft.exe').GetValue('') -eq (Join-Path $dir 'TasiaCraft.exe')) 'App Paths alias missing.'
 Assert ((Key 'Classes\.tasiaproj').GetValue('') -eq 'TatsiaCraft.Project') 'Project file association missing.'
 Assert ((Key 'Classes\TatsiaCraft.Project\shell\open\command').GetValue('') -like '*TasiaCraft.exe" "%1"') 'Open command missing.'
 # Opening a project from the command line.
 $project=Join-Path $dir 'Opened From Explorer.tasiaproj'
 [IO.File]::WriteAllText($project,'{"Media":[],"Clips":[{"Text":"From Explorer","Duration":2}],"TrackCount":2}')
 $app=Start-Process -FilePath (Join-Path $dir 'TasiaCraft.exe') -ArgumentList ('"'+$project+'"') -PassThru
 $deadline=[DateTime]::UtcNow.AddSeconds(20);do{Start-Sleep -Milliseconds 200;$app.Refresh()}while(-not $app.HasExited -and $app.MainWindowTitle -notlike '*Opened From Explorer*' -and [DateTime]::UtcNow -lt $deadline)
 Assert ($app.MainWindowTitle -eq ("TatsiaCraft $version - Opened From Explorer.tasiaproj")) ('Project argument did not open: '+$app.MainWindowTitle)
 # Setup must not replace files while the editor is running.
 $before=(Get-Item -LiteralPath (Join-Path $dir 'TasiaCraft.exe')).LastWriteTimeUtc
 Assert ((Run $SetupPath $common) -eq 3) 'Setup did not refuse while TatsiaCraft was running.'
 Assert ((Get-Item -LiteralPath (Join-Path $dir 'TasiaCraft.exe')).LastWriteTimeUtc -eq $before) 'Running editor was modified.'
 $app.CloseMainWindow()|Out-Null;if(!$app.WaitForExit(10000)){Stop-Process -Id $app.Id};Remove-Item -LiteralPath $project
 # Repair writes only what changed.
 $ffmpeg=(Get-Item -LiteralPath (Join-Path $dir 'tools\ffmpeg.exe')).LastWriteTimeUtc
 Set-Content -LiteralPath (Join-Path $dir 'README.md') -Value 'tampered'
 $userFile=Join-Path $dir 'my notes.txt';Set-Content -LiteralPath $userFile -Value 'keep me'
 Assert ((Run $SetupPath $common) -eq 0) 'Repair failed.'
 Assert ((Get-Content -LiteralPath (Join-Path $dir 'README.md') -Raw) -ne "tampered`r`n") 'Repair did not restore a changed file.'
 Assert ((Get-Item -LiteralPath (Join-Path $dir 'tools\ffmpeg.exe')).LastWriteTimeUtc -eq $ffmpeg) 'Repair rewrote an unchanged file.'
 Assert (Test-Path -LiteralPath $userFile) 'Repair removed an unrelated user file.'
 # Files dropped from a newer payload are removed on update.
 $stale=Join-Path $dir 'obsolete.txt';Set-Content -LiteralPath $stale -Value 'old'
 $m=Get-Content -LiteralPath (Join-Path $dir 'install-manifest.json') -Raw | ConvertFrom-Json;$m.files+= [pscustomobject]@{path='obsolete.txt';sha256='00';bytes=4};[IO.File]::WriteAllText((Join-Path $dir 'install-manifest.json'),($m|ConvertTo-Json -Depth 5))
 Assert ((Run $SetupPath $common) -eq 0) 'Update failed.'
 Assert (!(Test-Path -LiteralPath $stale)) 'Update did not remove a file no longer in the payload.'
 # A locked file makes the update fail and roll back without damage.
 Set-Content -LiteralPath (Join-Path $dir 'README.md') -Value 'tampered again'
 $lock=[IO.File]::Open((Join-Path $dir 'README.md'),'Open','Read','None')
 try{Assert ((Run $SetupPath $common) -ne 0) 'Update succeeded despite a locked file.'}finally{$lock.Close()}
 Assert ((Get-Content -LiteralPath (Join-Path $dir 'README.md') -Raw) -eq "tampered again`r`n") 'Failed update did not leave the locked file intact.'
 Assert (Test-Path -LiteralPath (Join-Path $dir 'TasiaCraft.exe')) 'Failed update damaged the installation.'
 Assert (@(Get-ChildItem -LiteralPath $dir -Directory -Force | Where-Object {$_.Name -like '.setup-*' -or $_.Name -like '.backup-*'}).Count -eq 0) 'Failed update left staging folders.'
 # Older side-by-side versions are removed only on request.
 $legacy=Join-Path $dir '0.1.0';New-Item -ItemType Directory -Path $legacy|Out-Null;Copy-Item -LiteralPath (Join-Path $dir 'TasiaCraft.exe') -Destination $legacy
 Assert ((Run $SetupPath $common) -eq 0) 'Reinstall failed.'
 Assert (Test-Path -LiteralPath $legacy) 'Legacy version removed without being asked.'
 Assert ((Run $SetupPath ($common+'/removelegacy')) -eq 0) 'Legacy removal install failed.'
 Assert (!(Test-Path -LiteralPath $legacy)) 'Legacy version was not removed when requested.'
 Assert ((Run $SetupPath @('/quiet','/bogus')) -eq 2) 'Unknown option was not rejected.'
 # Uninstall removes the application and registration but keeps user files.
 Assert ((Run (Join-Path $dir 'Uninstall TatsiaCraft.exe') @('/uninstall','/quiet',('"/dir='+$dir+'"'),('"/regroot='+$reg+'"'))) -eq 0) 'Quiet uninstall failed.'
 Start-Sleep -Seconds 4
 Assert (!(Test-Path -LiteralPath (Join-Path $dir 'TasiaCraft.exe')) -and !(Test-Path -LiteralPath (Join-Path $dir 'tools'))) 'Uninstall left application files.'
 Assert (!(Test-Path -LiteralPath (Join-Path $dir 'Uninstall TatsiaCraft.exe'))) 'Uninstaller did not remove itself.'
 Assert (Test-Path -LiteralPath $userFile) 'Uninstall removed an unrelated user file.'
 Assert ((Key 'Microsoft\Windows\CurrentVersion\Uninstall\TatsiaCraft') -eq $null -and (Key 'Classes\.tasiaproj') -eq $null -and (Key 'Classes\TatsiaCraft.Project') -eq $null) 'Uninstall left registration behind.'
 Write-Output "PASS: $checks setup checks"
}finally{
 try{[Microsoft.Win32.Registry]::CurrentUser.DeleteSubKeyTree($reg,$false)}catch{}
 $expected=[IO.Path]::GetFullPath((Join-Path $sourceRoot 'artifacts'))
 if((Split-Path ([IO.Path]::GetFullPath($dir)) -Parent) -ne $expected){throw 'Unsafe test cleanup path.'}
 Get-Process TasiaCraft -ErrorAction SilentlyContinue | Where-Object {try{$_.Path -like ($dir+'*')}catch{$false}} | Stop-Process -Force
 if(Test-Path -LiteralPath $dir){Remove-Item -LiteralPath $dir -Recurse -Force}
}
