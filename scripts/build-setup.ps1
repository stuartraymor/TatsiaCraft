param([Parameter(Mandatory=$true)][string]$ToolsPath,[string]$ExecutablePath)
# Builds dist\TatsiaCraft-Setup-<version>.exe: a per-user installer with the application, FFmpeg tools,
# licenses, an uninstaller and a SHA-256 manifest embedded as one compressed payload.
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression,System.IO.Compression.FileSystem
$root=Split-Path $PSScriptRoot -Parent
$version=(Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim()
if(!$ExecutablePath){$ExecutablePath=Join-Path $root 'dist\TasiaCraft\TasiaCraft.exe'}
$exe=Get-Item -LiteralPath $ExecutablePath
if($exe.VersionInfo.ProductVersion -ne $version){throw "Executable version must match VERSION ($version). Build this revision first."}
$ToolsPath=(Get-Item -LiteralPath $ToolsPath).FullName
foreach($tool in @('ffmpeg.exe','ffprobe.exe','LICENSE-FFmpeg.txt')){if(!(Test-Path -LiteralPath (Join-Path $ToolsPath $tool) -PathType Leaf)){throw "ToolsPath is missing $tool."}}
$fx='C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$refs=@('System.dll','System.Core.dll','System.Web.Extensions.dll','System.Drawing.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll','Microsoft.CSharp.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll','System.Xaml.dll') | ForEach-Object {'/r:'+(Join-Path $fx $_)}
$work=Join-Path $root 'artifacts\setup-build'
if(Test-Path -LiteralPath $work){Remove-Item -LiteralPath $work -Recurse -Force}
$stage=Join-Path $work 'payload'
New-Item -ItemType Directory -Path $stage -Force | Out-Null
$versionInfo=Join-Path $work 'SetupVersion.cs'
"[assembly: System.Reflection.AssemblyVersion(""$version.0"")][assembly: System.Reflection.AssemblyFileVersion(""$version.0"")][assembly: System.Reflection.AssemblyInformationalVersion(""$version"")][assembly: System.Reflection.AssemblyProduct(""TatsiaCraft"")]" | Set-Content -LiteralPath $versionInfo -Encoding UTF8
$icon='/win32icon:'+(Join-Path $root 'assets\TasiaCraft.ico')
$source=Join-Path $root 'installer\Setup.cs'
# 1. Uninstaller: the same program without a payload.
& "$fx\csc.exe" /nologo /target:winexe /platform:anycpu /optimize+ $icon ('/out:'+(Join-Path $stage 'Uninstall TatsiaCraft.exe')) @refs $source $versionInfo
if($LASTEXITCODE -ne 0){throw 'Uninstaller compilation failed'}
# 2. Payload files and manifest.
Copy-Item -LiteralPath $exe.FullName -Destination (Join-Path $stage 'TasiaCraft.exe')
foreach($doc in @('LICENSE','README.md','VERSION')){Copy-Item -LiteralPath (Join-Path $root $doc) -Destination $stage}
& (Join-Path $PSScriptRoot 'link-tools.ps1') -Source $ToolsPath -Destination (Join-Path $stage 'tools')
$files=@(Get-ChildItem -LiteralPath $stage -File -Recurse | Sort-Object FullName | ForEach-Object {[ordered]@{path=$_.FullName.Substring($stage.Length+1).Replace('\','/');sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash;bytes=$_.Length}})
$manifest=[ordered]@{application='TatsiaCraft';version=$version;files=$files}
[IO.File]::WriteAllText((Join-Path $stage 'install-manifest.json'),($manifest | ConvertTo-Json -Depth 5),(New-Object Text.UTF8Encoding($false)))
$zip=Join-Path $work 'payload.zip'
# Entries use forward slashes (CreateFromDirectory on .NET Framework writes backslashes).
$archive=[IO.Compression.ZipFile]::Open($zip,[IO.Compression.ZipArchiveMode]::Create)
try{foreach($file in Get-ChildItem -LiteralPath $stage -File -Recurse){$name=$file.FullName.Substring($stage.Length+1).Replace('\','/');[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$file.FullName,$name,[IO.Compression.CompressionLevel]::Optimal)|Out-Null}}finally{$archive.Dispose()}
# 3. Setup with the embedded payload.
$out=Join-Path $root ("dist\TatsiaCraft-Setup-$version.exe")
& "$fx\csc.exe" /nologo /target:winexe /platform:anycpu /optimize+ $icon ('/out:'+$out) ('/resource:'+$zip+',TatsiaCraft.Payload.zip') @refs $source $versionInfo
if($LASTEXITCODE -ne 0){throw 'Setup compilation failed'}
Remove-Item -LiteralPath $work -Recurse -Force
$size=(Get-Item -LiteralPath $out).Length
Write-Output ("Built {0} ({1:N1} MB, {2} payload files)" -f $out,($size/1MB),$files.Count)
