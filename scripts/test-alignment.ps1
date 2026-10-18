param([string]$ToolsPath)
$ErrorActionPreference='Stop'
$repoRoot=Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'build.ps1')
$testRoot=Join-Path $repoRoot 'artifacts\AlignmentTest'
New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
$editor=Join-Path $repoRoot 'dist\TasiaCraft\TasiaCraft.exe'
Copy-Item -LiteralPath $editor -Destination (Join-Path $testRoot 'TasiaCraft.exe') -Force
if($ToolsPath){
 $target=Join-Path $testRoot 'tools'
 New-Item -ItemType Directory -Path $target -Force | Out-Null
 Get-ChildItem -LiteralPath $ToolsPath -Force | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination $target -Recurse -Force}
}
if(!(Test-Path -LiteralPath (Join-Path $testRoot 'tools\ffmpeg.exe'))){throw 'Supply -ToolsPath with compatible FFmpeg tools.'}
$framework='C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$references=@('System.dll','System.Core.dll','System.Web.Extensions.dll','System.Drawing.dll','System.Windows.Forms.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll','System.Xaml.dll') | ForEach-Object {'/r:'+(Join-Path $framework $_)}
$exe=Join-Path $testRoot 'AlignmentTest.exe'
& (Join-Path $framework 'csc.exe') /nologo /target:winexe /platform:x64 ('/r:'+$editor) ('/out:'+$exe) @references (Join-Path $repoRoot 'tests\AlignmentTest.cs')
if($LASTEXITCODE -ne 0){throw 'Alignment test compilation failed.'}
$process=Start-Process -FilePath $exe -ArgumentList ('"'+$testRoot+'"') -WindowStyle Hidden -PassThru
if(!$process.WaitForExit(60000)){Stop-Process -Id $process.Id;throw 'Isolated alignment test timed out.'}
$log=Join-Path $testRoot 'results.txt'
Get-Content -LiteralPath $log
if($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern '^PASS total [0-9]+$' -Quiet)){throw 'Alignment acceptance test failed.'}
