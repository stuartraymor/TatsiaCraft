param([Parameter(Mandatory=$true)][string]$Message,[switch]$Resume,[string]$ToolsPath,[switch]$PrepareOnly)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'version.ps1')
$releaseRoot=Split-Path $PSScriptRoot -Parent
Push-Location $releaseRoot
$priorReleaseTree=$env:TATSIACRAFT_RELEASE_TREE
try {
 $env:GIT_TERMINAL_PROMPT='0'
 $branch=(& git branch --show-current).Trim()
 if($LASTEXITCODE -ne 0 -or $branch -ne 'main'){throw 'Release from the main branch'}
 & git diff --quiet
 if($LASTEXITCODE -ne 0){throw 'Stage intended tracked changes before releasing'}
 $remote=(& git remote get-url origin).Trim()
 if($remote -notmatch '^https://github\.com/([^/]+)/([^/]+?)(?:\.git)?$'){throw 'Expected an HTTPS github.com origin'}
 $repository=$Matches[1]+'/'+$Matches[2]
 & git -c credential.interactive=never fetch origin --tags
 if($LASTEXITCODE -ne 0){throw 'Could not fetch GitHub tags'}
 $token=$env:GH_TOKEN
 if(-not $token){$token=$env:GITHUB_TOKEN}
 if(-not $token){
  $credentialLines="protocol=https`nhost=github.com`n`n" | git -c credential.interactive=never credential fill 2>$null
  if($LASTEXITCODE -ne 0){throw 'Authenticate Git with GitHub, or supply GH_TOKEN'}
  foreach($line in $credentialLines){if($line.StartsWith('password=')){$token=$line.Substring(9)}}
 }
 if(-not $token){throw 'GitHub release authentication unavailable'}
 $headers=@{Authorization=('Bearer '+$token);Accept='application/vnd.github+json';'User-Agent'='TatsiaCraft-Release'}
 $api='https://api.github.com/repos/'+$repository
 if($Resume){
  & git diff --cached --quiet
  if($LASTEXITCODE -ne 0){throw 'Resume requires a clean index'}
  $version=(Get-Content VERSION -Raw).Trim()
  $tag='v'+$version
  $tagHead=(& git rev-list -n 1 $tag).Trim()
  $head=(& git rev-parse HEAD).Trim()
  if($tagHead -ne $head){throw 'Resume requires HEAD to match the version tag'}
 }else{
  $versions=@(& git tag --list 'v[0-9]*' --sort=-version:refname | Where-Object {$_ -match '^v[0-9]+\.(?:[0-9]|1[0-2])\.(?:[0-9]|1[0-2])$'})
  $previous=if($versions.Count){$versions[0].Substring(1)}else{'0.0.0'}
  $version=Get-NextRevision $previous
  $tag='v'+$version
  Set-Content -LiteralPath VERSION -Value $version -Encoding ASCII
 }
 & git add -- VERSION
 if($LASTEXITCODE -ne 0){throw 'Could not stage VERSION'}
 $releaseTree=(& git write-tree).Trim()
 if($LASTEXITCODE -ne 0){throw 'Could not identify staged release source'}
 # Compile, test and install before pushing or publishing any revision.
 if(-not $ToolsPath){$ToolsPath=Join-Path $releaseRoot 'dist\TasiaCraft\tools'}
 $ToolsPath=(Get-Item -LiteralPath $ToolsPath).FullName
 $preservedApp=Join-Path $releaseRoot ("artifacts\TatsiaCraft-$version.exe")
 $treeRecord=Join-Path $releaseRoot ("artifacts\TatsiaCraft-$version-tree.txt")
 $preservedSetup=Join-Path $releaseRoot ("artifacts\TatsiaCraft-Setup-$version.exe")
 $canReuse=(Test-Path -LiteralPath $preservedApp) -and (Test-Path -LiteralPath $preservedSetup) -and (Test-Path -LiteralPath $treeRecord) -and ((Get-Content -LiteralPath $treeRecord -Raw).Trim() -eq $releaseTree)
 if($canReuse){
  if((Get-Item -LiteralPath $preservedApp).VersionInfo.ProductVersion -ne $version){throw 'Preserved release artifact version mismatch'}
  Copy-Item -LiteralPath $preservedApp -Destination 'dist\TasiaCraft\TasiaCraft.exe'
  if((Get-Item -LiteralPath $preservedSetup).VersionInfo.ProductVersion -ne $version){throw 'Preserved setup version mismatch'}
 }else{
  $env:TATSIACRAFT_RELEASE_TREE=$releaseTree
  & (Join-Path $PSScriptRoot 'test-exportcommit.ps1') -ToolsPath $ToolsPath
  & (Join-Path $PSScriptRoot 'test-mediabin.ps1') -ToolsPath $ToolsPath
  & (Join-Path $PSScriptRoot 'test-cursor.ps1') -ToolsPath $ToolsPath
  & (Join-Path $PSScriptRoot 'test-narration.ps1') -ToolsPath $ToolsPath
  & (Join-Path $PSScriptRoot 'test-behaviors.ps1') -ToolsPath $ToolsPath
  & (Join-Path $PSScriptRoot 'test-audiopoints.ps1') -ToolsPath $ToolsPath
  & (Join-Path $PSScriptRoot 'test-audiostitch.ps1') -ToolsPath $ToolsPath
  & (Join-Path $PSScriptRoot 'test-ripple.ps1') -ToolsPath $ToolsPath
  & (Join-Path $PSScriptRoot 'test-markers.ps1') -ToolsPath $ToolsPath
  & (Join-Path $PSScriptRoot 'test-ui.ps1') -ToolsPath $ToolsPath
  & (Join-Path $PSScriptRoot 'test-grouping.ps1') -ToolsPath $ToolsPath
  & (Join-Path $PSScriptRoot 'test-projectsave.ps1') -ToolsPath $ToolsPath
  & (Join-Path $PSScriptRoot 'test-resizesnap.ps1') -ToolsPath $ToolsPath
  & (Join-Path $PSScriptRoot 'test-cropmove.ps1') -ToolsPath $ToolsPath
  & (Join-Path $PSScriptRoot 'test-alignment.ps1') -ToolsPath $ToolsPath
  & (Join-Path $PSScriptRoot 'test-offcanvas.ps1') -ToolsPath $ToolsPath
  & (Join-Path $PSScriptRoot 'test-resize.ps1') -ToolsPath $ToolsPath
  & (Join-Path $PSScriptRoot 'test-pan.ps1')
  & (Join-Path $PSScriptRoot 'test-crop.ps1') -ToolsPath $ToolsPath
  & (Join-Path $PSScriptRoot 'test-canvas.ps1')
  # Consumer installer: build, then verify install/update/repair/rollback/uninstall in isolation.
  & (Join-Path $PSScriptRoot 'build-setup.ps1') -ToolsPath $ToolsPath
  & (Join-Path $releaseRoot 'tests\SetupTest.ps1') -SetupPath (Join-Path $releaseRoot ("dist\TatsiaCraft-Setup-$version.exe"))
  if($LASTEXITCODE -and $LASTEXITCODE -ne 0){throw 'Setup tests failed'}
  Copy-Item -LiteralPath 'dist\TasiaCraft\TasiaCraft.exe' -Destination $preservedApp
  Copy-Item -LiteralPath (Join-Path $releaseRoot ("dist\TatsiaCraft-Setup-$version.exe")) -Destination $preservedSetup
  Set-Content -LiteralPath $treeRecord -Value $releaseTree -Encoding ASCII
 }
 $setupRun=Start-Process -FilePath $preservedSetup -ArgumentList '/quiet' -PassThru -Wait
 if($setupRun.ExitCode -ne 0){throw ('Setup install failed with exit code '+$setupRun.ExitCode+'; see %TEMP%\TatsiaCraft-Setup.log')}
 $installed=Join-Path $env:LOCALAPPDATA 'Programs\TatsiaCraft'
 if((Get-FileHash -LiteralPath (Join-Path $installed 'TasiaCraft.exe')).Hash -ne (Get-FileHash -LiteralPath $preservedApp).Hash){throw 'Installed and preserved release executables differ'}
 $manifest=Get-Content -LiteralPath (Join-Path $installed 'install-manifest.json') -Raw | ConvertFrom-Json
 if($manifest.version -ne $version){throw 'Installed manifest does not match this release'}
 foreach($entry in $manifest.files){$payload=Get-Item -LiteralPath (Join-Path $installed $entry.path);if($payload.Length -ne $entry.bytes){throw ('Installed payload verification failed: '+$entry.path)}}
 $smoke=Start-Process -FilePath (Join-Path $installed 'TasiaCraft.exe') -WindowStyle Hidden -PassThru
 try{
  $deadline=[DateTime]::UtcNow.AddSeconds(15)
  do{$smoke.Refresh();if($smoke.HasExited){throw 'Installed editor exited during startup'};if($smoke.MainWindowTitle.StartsWith('TatsiaCraft '+$version+' ')){break};Start-Sleep -Milliseconds 100}while([DateTime]::UtcNow -lt $deadline)
  if(-not $smoke.MainWindowTitle.StartsWith('TatsiaCraft '+$version+' ')){throw 'Installed editor did not expose its expected versioned title'}
  if(-not $smoke.CloseMainWindow() -or -not $smoke.WaitForExit(10000)){throw 'Installed editor did not shut down cleanly'}
  if($smoke.ExitCode -ne 0){throw 'Installed editor startup smoke failed'}
  Write-Output ('PASS installed startup and clean shutdown: '+$installed)
 }finally{if(-not $smoke.HasExited){Stop-Process -Id $smoke.Id}}
 if($PrepareOnly){Write-Output ("Prepared and installed "+$version+"; source and executable are preserved for workflow verification before publication.");return}
 if(-not $Resume){
  & git add -- VERSION
  if($LASTEXITCODE -ne 0){throw 'Could not stage VERSION'}
  & git commit -m ("Release $version`: $Message")
  if($LASTEXITCODE -ne 0){throw 'Release commit failed'}
  & git tag -a $tag -m ("TatsiaCraft $version")
  if($LASTEXITCODE -ne 0){throw 'Version tag failed'}
 }
 & git -c credential.interactive=never push --atomic origin HEAD:refs/heads/main ('refs/tags/'+$tag)
 if($LASTEXITCODE -ne 0){throw 'GitHub push failed; preserve this tag and use -Resume after resolving the failure'}
 $head=(& git rev-parse HEAD).Trim()
 $sourcePath=Join-Path $releaseRoot ("artifacts\TatsiaCraft-$version-source.zip")
 & git archive --format=zip ('--output='+$sourcePath) HEAD
 if($LASTEXITCODE -ne 0){throw 'Source archive failed; rerun with -Resume'}
 $appPath=Join-Path $releaseRoot ("artifacts\TatsiaCraft-$version.exe")
 if((Get-FileHash -LiteralPath $appPath).Hash -ne (Get-FileHash -LiteralPath (Join-Path $installed 'TasiaCraft.exe')).Hash){throw 'Release binary differs from tested installed binary'}
 $release=$null
 try{$release=Invoke-RestMethod -Uri ($api+'/releases/tags/'+$tag) -Headers $headers}catch{if([int]$_.Exception.Response.StatusCode -ne 404){throw}}
 if(-not $release){
  $body=@{tag_name=$tag;target_commitish=$head;name=('TatsiaCraft '+$version);body=($Message+"`n`n**Install:** download TatsiaCraft-Setup-$version.exe and run it. Setup installs for the current user (no administrator rights), includes the FFmpeg media tools, updates an existing install in place, and adds TatsiaCraft to Apps & Features for uninstalling. Existing users can also use Help > Check for Updates.`n`nTatsiaCraft-$version.exe is the bare editor for advanced use (it needs FFmpeg in a tools folder beside it). Windows 10/11 x64 with .NET Framework 4.8. Development preview: complete feature parity and comparative performance are not yet established. Setup is not code-signed, so Windows SmartScreen may ask you to confirm.");draft=$true;prerelease=$true} | ConvertTo-Json
  $release=Invoke-RestMethod -Method Post -Uri ($api+'/releases') -Headers $headers -ContentType 'application/json' -Body $body
 }
 $releaseApi=$api+'/releases/'+$release.id
 foreach($path in @($preservedSetup,$appPath,$sourcePath)){
  $file=Get-Item -LiteralPath $path
  $existing=@($release.assets | Where-Object name -eq $file.Name)
  if($existing.Count){
   $digest='sha256:'+(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
   if($existing[0].size -ne $file.Length -or ($existing[0].digest -and $existing[0].digest -ne $digest)){throw ('Existing release asset differs: '+$file.Name)}
  }else{
   if(-not $release.draft){throw 'Published release is missing an asset; inspect it before changing the published release'}
   $uploadBase=$release.upload_url -replace '\{.*$',''
   $uploadUri=$uploadBase+'?name='+[Uri]::EscapeDataString($file.Name)
   $asset=Invoke-RestMethod -Method Post -Uri $uploadUri -Headers $headers -ContentType 'application/octet-stream' -InFile $file.FullName
   if($asset.state -ne 'uploaded' -or $asset.size -ne $file.Length){throw ('Asset upload verification failed: '+$file.Name)}
  }
 }
 if($release.draft){$release=Invoke-RestMethod -Method Patch -Uri $releaseApi -Headers $headers -ContentType 'application/json' -Body '{"draft":false}'}
 $verified=Invoke-RestMethod -Uri $releaseApi -Headers $headers
 $remoteHead=(& git ls-remote origin refs/heads/main).Split()[0]
 if($LASTEXITCODE -ne 0 -or $remoteHead -ne $head -or $verified.draft -or $verified.tag_name -ne $tag -or @($verified.assets | Where-Object state -eq 'uploaded').Count -lt 3){throw 'Remote revision verification failed'}
 Write-Output ('Published '+$verified.html_url)
} finally {$env:TATSIACRAFT_RELEASE_TREE=$priorReleaseTree;Pop-Location}
