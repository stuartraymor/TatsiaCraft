param([Parameter(Mandatory=$true)][string]$ToolsPath)
$ErrorActionPreference='Stop'
$repoRoot=Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'build.ps1')
$testRoot=Join-Path $repoRoot 'artifacts\ResizeSnapTest'
New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
$editor=Join-Path $repoRoot 'dist\TasiaCraft\TasiaCraft.exe'
Copy-Item -LiteralPath $editor -Destination (Join-Path $testRoot 'TasiaCraft.exe') -Force
$ToolsPath=(Get-Item -LiteralPath $ToolsPath).FullName
foreach($name in @('ffmpeg.exe','ffprobe.exe')){if(!(Test-Path -LiteralPath (Join-Path $ToolsPath $name))){throw "ToolsPath missing $name"}}
$targetTools=Join-Path $testRoot 'tools'
New-Item -ItemType Directory -Path $targetTools -Force | Out-Null
Get-ChildItem -LiteralPath $ToolsPath -Force | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination $targetTools -Recurse -Force}
$framework='C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$references=@('System.dll','System.Core.dll','System.Web.Extensions.dll','System.Drawing.dll','System.Windows.Forms.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll','System.Xaml.dll')|ForEach-Object{'/r:'+(Join-Path $framework $_)}
$testExe=Join-Path $testRoot 'ResizeSnapTest.exe'
& (Join-Path $framework 'csc.exe') /nologo /target:winexe /platform:x64 ('/r:'+$editor) ('/out:'+$testExe) @references (Join-Path $repoRoot 'tests\ResizeSnapTest.cs')
if($LASTEXITCODE -ne 0){throw 'Resize snap test compilation failed'}
$process=Start-Process -FilePath $testExe -ArgumentList ('"'+$testRoot+'"') -WindowStyle Hidden -PassThru
if(!$process.WaitForExit(60000)){Stop-Process -Id $process.Id;throw 'Isolated resize snap test timed out'}
$log=Join-Path $testRoot 'results.txt'
Get-Content -LiteralPath $log
if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern '^PASS total [0-9]+$' -Quiet)){throw 'Resize snap acceptance test failed'}


