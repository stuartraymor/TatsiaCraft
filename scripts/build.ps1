param()
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskOut=Join-Path $taskRoot 'dist\TasiaCraft'
New-Item -ItemType Directory -Force $taskOut | Out-Null
New-Item -ItemType Directory -Force (Join-Path $taskRoot 'artifacts') | Out-Null
$fx='C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$refs=@('System.dll','System.Core.dll','System.Web.Extensions.dll','System.Drawing.dll','System.Windows.Forms.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll','System.Xaml.dll') | ForEach-Object { '/r:'+(Join-Path $fx $_) }
$argsBuild=@('/nologo','/target:winexe','/platform:x64','/optimize+','/debug:pdbonly',('/win32icon:'+(Join-Path $taskRoot 'assets\TasiaCraft.ico')),('/out:'+(Join-Path $taskOut 'TasiaCraft.exe'))) + $refs + @(Get-ChildItem (Join-Path $taskRoot 'src') -Filter '*.cs' | ForEach-Object FullName)
& "$fx\csc.exe" $argsBuild 2>&1 | Tee-Object (Join-Path $taskRoot 'artifacts\build.log')
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
