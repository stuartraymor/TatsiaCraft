$ErrorActionPreference='Stop'
$repoRoot=Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'build.ps1')
$frameworkRoot='C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$references=@('System.dll','System.Core.dll','System.Web.Extensions.dll','System.Drawing.dll','System.Windows.Forms.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll','System.Xaml.dll') | ForEach-Object { '/r:'+(Join-Path $frameworkRoot $_) }
$editorPath=Join-Path $repoRoot 'dist\TasiaCraft\TasiaCraft.exe'
$testPath=Join-Path $repoRoot 'dist\TasiaCraft\CanvasWorkspaceTest.exe'
$logPath=Join-Path $repoRoot 'artifacts\canvas-workspace-tests.txt'
& (Join-Path $frameworkRoot 'csc.exe') /nologo /target:winexe /platform:x64 ('/r:'+$editorPath) ('/out:'+$testPath) @references (Join-Path $repoRoot 'tests\CanvasWorkspaceTest.cs')
if($LASTEXITCODE -ne 0){throw 'Test compilation failed'}
$testProcess=Start-Process -FilePath $testPath -ArgumentList ('"'+$logPath+'"') -WindowStyle Hidden -PassThru
if(-not $testProcess.WaitForExit(30000)){Stop-Process -Id $testProcess.Id;throw 'Canvas test timed out'}
Get-Content -LiteralPath $logPath
if($testProcess.ExitCode -ne 0){throw ('Canvas test failed: '+$testProcess.ExitCode)}
if(-not (Select-String -LiteralPath $logPath -SimpleMatch 'PASS total 28' -Quiet)){throw 'Canvas suite did not finish'}
