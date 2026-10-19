param([Parameter(Mandatory=$true)][string]$ToolsPath)
$ErrorActionPreference='Stop'
$repoRoot=Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'build.ps1')
$testRoot=Join-Path $repoRoot 'artifacts\MediaBinTest'
New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
$editor=Join-Path $repoRoot 'dist\TasiaCraft\TasiaCraft.exe'
Copy-Item -LiteralPath $editor -Destination (Join-Path $testRoot 'TasiaCraft.exe') -Force
$ToolsPath=(Get-Item -LiteralPath $ToolsPath).FullName
$targetTools=Join-Path $testRoot 'tools'
New-Item -ItemType Directory -Path $targetTools -Force | Out-Null
& (Join-Path $PSScriptRoot 'link-tools.ps1') -Source $ToolsPath -Destination $targetTools
$framework='C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$references=@('System.dll','System.Core.dll','System.Web.Extensions.dll','System.Drawing.dll','System.Windows.Forms.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll','System.Xaml.dll')|ForEach-Object{'/r:'+(Join-Path $framework $_)}
$testExe=Join-Path $testRoot 'MediaBinTest.exe'
& (Join-Path $framework 'csc.exe') /nologo /target:winexe /platform:x64 ('/r:'+$editor) ('/out:'+$testExe) @references (Join-Path $repoRoot 'tests\MediaBinTest.cs') (Join-Path $repoRoot 'tests\MediaPreviewTest.cs') (Join-Path $repoRoot 'tests\MediaNavigationTest.cs') (Join-Path $repoRoot 'tests\MediaBatchTest.cs') (Join-Path $repoRoot 'tests\MediaDragTest.cs') (Join-Path $repoRoot 'tests\PreviewFailureTest.cs') (Join-Path $repoRoot 'tests\TimelineSnapTest.cs') (Join-Path $repoRoot 'tests\TimelineMoveSnapTest.cs')
if($LASTEXITCODE -ne 0){throw 'Media-bin test compilation failed'}
$process=Start-Process -FilePath $testExe -ArgumentList ('"'+$testRoot+'"') -WindowStyle Hidden -PassThru
if(!$process.WaitForExit(60000)){Stop-Process -Id $process.Id;throw 'Media-bin test timed out'}
$log=Join-Path $testRoot 'results.txt'
Get-Content -LiteralPath $log
if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern '^PASS total [0-9]+$' -Quiet)){throw 'Media-bin acceptance test failed'}
