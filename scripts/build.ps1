param()
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskOut=Join-Path $taskRoot 'dist\TasiaCraft'
New-Item -ItemType Directory -Force $taskOut | Out-Null
New-Item -ItemType Directory -Force (Join-Path $taskRoot 'artifacts') | Out-Null
$releaseVersion=(Get-Content -LiteralPath (Join-Path $taskRoot 'VERSION') -Raw).Trim()
if($releaseVersion -notmatch '^(0|[1-9][0-9]*)\.(0|[1-9]|1[0-2])\.(0|[1-9]|1[0-2])$'){throw 'VERSION must use major.minor.patch with minor and patch from 0 through 12'}
$versionSource=Join-Path $taskRoot 'artifacts\VersionInfo.cs'
@"
using System.Reflection;
[assembly: AssemblyVersion("$releaseVersion.0")]
[assembly: AssemblyFileVersion("$releaseVersion.0")]
[assembly: AssemblyInformationalVersion("$releaseVersion")]
"@ | Set-Content -LiteralPath $versionSource -Encoding UTF8
$fx='C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$refs=@('System.dll','System.Core.dll','System.Web.Extensions.dll','System.Drawing.dll','System.Windows.Forms.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll','System.Xaml.dll') | ForEach-Object { '/r:'+(Join-Path $fx $_) }
$taskSources=@(Get-ChildItem (Join-Path $taskRoot 'src') -Filter '*.cs' | ForEach-Object FullName)
if($env:TATSIACRAFT_RELEASE_TREE){
 $taskCurrentTree=(& git -C $taskRoot write-tree).Trim()
 if($LASTEXITCODE -ne 0 -or $taskCurrentTree -ne $env:TATSIACRAFT_RELEASE_TREE){throw 'Staged release source changed during verification'}
 & git -C $taskRoot diff --quiet
 if($LASTEXITCODE -ne 0){throw 'Tracked source changed during release verification'}
 $taskSourceNames=@(& git -C $taskRoot ls-tree -r --name-only $env:TATSIACRAFT_RELEASE_TREE -- src)
 if($LASTEXITCODE -ne 0){throw 'Could not enumerate staged release sources'}
 $taskSources=@($taskSourceNames | Where-Object { $_.EndsWith('.cs') } | ForEach-Object {Join-Path $taskRoot $_})
 if(!$taskSources.Count){throw 'The staged release has no C# sources'}
}
$argsBuild=@('/nologo','/target:winexe','/platform:x64','/optimize+','/debug:pdbonly',('/win32icon:'+(Join-Path $taskRoot 'assets\TasiaCraft.ico')),('/out:'+(Join-Path $taskOut 'TasiaCraft.exe'))) + $refs + $taskSources
$argsBuild += $versionSource
& "$fx\csc.exe" $argsBuild 2>&1 | Tee-Object (Join-Path $taskRoot 'artifacts\build.log')
if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
