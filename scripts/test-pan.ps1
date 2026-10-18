param()
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'build.ps1')
$testRoot = Join-Path $repoRoot 'artifacts\PanTest'
New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
$editor = Join-Path $repoRoot 'dist\TasiaCraft\TasiaCraft.exe'
Copy-Item -LiteralPath $editor -Destination (Join-Path $testRoot 'TasiaCraft.exe') -Force
$framework = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$references = @('System.dll','System.Core.dll','System.Web.Extensions.dll','System.Drawing.dll','System.Windows.Forms.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll','System.Xaml.dll') | ForEach-Object { '/r:' + (Join-Path $framework $_) }
$testExe = Join-Path $testRoot 'PanTest.exe'
& (Join-Path $framework 'csc.exe') /nologo /target:winexe /platform:x64 ('/r:' + $editor) ('/out:' + $testExe) @references (Join-Path $repoRoot 'tests\PanTest.cs')
if ($LASTEXITCODE -ne 0) { throw 'Pan test compilation failed.' }
$process = Start-Process -FilePath $testExe -ArgumentList ('"' + $testRoot + '"') -WindowStyle Hidden -PassThru
if (!$process.WaitForExit(30000)) { Stop-Process -Id $process.Id; throw 'Isolated pan test timed out.' }
$log = Join-Path $testRoot 'results.txt'
Get-Content -LiteralPath $log
if ($process.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Pattern '^PASS total [0-9]+$' -Quiet)) { throw 'Pan acceptance test failed.' }
