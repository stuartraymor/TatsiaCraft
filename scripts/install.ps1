param(
    [string]$ExecutablePath,
    [string]$InstallRoot = (Join-Path $env:LOCALAPPDATA 'Programs\TatsiaCraft'),
    [string]$ToolsPath,
    [switch]$NoShortcuts
)
$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path $PSScriptRoot -Parent
$version = (Get-Content -LiteralPath (Join-Path $sourceRoot 'VERSION') -Raw).Trim()
if ($version -notmatch '^(0|[1-9][0-9]*)\.(0|[1-9]|1[0-2])\.(0|[1-9]|1[0-2])$') { throw 'Invalid VERSION.' }
if (!$ExecutablePath) { $ExecutablePath = Join-Path $sourceRoot 'dist\TasiaCraft\TasiaCraft.exe' }
$executable = Get-Item -LiteralPath $ExecutablePath
if ($executable.VersionInfo.ProductVersion -ne $version) { throw "Executable version must match VERSION ($version). Build this revision first." }
$InstallRoot = [IO.Path]::GetFullPath($InstallRoot)
$destination = Join-Path $InstallRoot $version
# Immutable version directories also protect an editor that is currently running.
if (Test-Path -LiteralPath $destination) { throw "Installation already exists: $destination. Existing versions are never overwritten." }
if ($ToolsPath) {
    $ToolsPath = (Get-Item -LiteralPath $ToolsPath).FullName
    foreach ($tool in @('ffmpeg.exe','ffprobe.exe','ffplay.exe','LICENSE-FFmpeg.txt')) {
        if (!(Test-Path -LiteralPath (Join-Path $ToolsPath $tool) -PathType Leaf)) { throw "ToolsPath is missing $tool." }
    }
    if ((Get-ChildItem -LiteralPath $ToolsPath -Recurse -Force | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count) { throw 'ToolsPath must not contain symbolic links or junctions.' }
}
New-Item -ItemType Directory -Path $InstallRoot -Force | Out-Null
$staging = Join-Path $InstallRoot ('.i-' + [guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $staging | Out-Null
try {
    Copy-Item -LiteralPath $executable.FullName -Destination (Join-Path $staging 'TasiaCraft.exe')
    foreach ($document in @('LICENSE','README.md','VERSION')) { Copy-Item -LiteralPath (Join-Path $sourceRoot $document) -Destination $staging }
    if ($ToolsPath) { & (Join-Path $PSScriptRoot 'link-tools.ps1') -Source $ToolsPath -Destination (Join-Path $staging 'tools') }
    $files = @(Get-ChildItem -LiteralPath $staging -File -Recurse | Sort-Object FullName | ForEach-Object {
        [ordered]@{ path = $_.FullName.Substring($staging.Length + 1).Replace('\','/'); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash; bytes = $_.Length }
    })
    [ordered]@{ application = 'TatsiaCraft'; version = $version; installedAtUtc = [DateTime]::UtcNow.ToString('o'); toolsIncluded = [bool]$ToolsPath; files = $files } |
        ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $staging 'install-manifest.json') -Encoding UTF8
    # Directory.Move fails if another installer creates this version meanwhile.
    for ($attempt=0; $attempt -lt 4; $attempt++) {
        try { [IO.Directory]::Move($staging, $destination); break }
        catch {
            $cause=$_.Exception.InnerException
            if ($attempt -eq 3 -or (Test-Path -LiteralPath $destination) -or !($cause -is [IO.IOException] -or $cause -is [UnauthorizedAccessException])) { throw }
            # Windows can temporarily hold new payload files open. Retry only the atomic move.
            Start-Sleep -Milliseconds (200*($attempt+1))
        }
    }
} catch {
    # Only this invocation's newly allocated staging directory is disposable.
    if ((Split-Path ([IO.Path]::GetFullPath($staging)) -Parent) -ne $InstallRoot -or (Split-Path $staging -Leaf) -notlike '.i-*') { throw 'Unsafe installer staging cleanup path.' }
    if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
    throw
}
if (!$NoShortcuts) {
    $shell = New-Object -ComObject WScript.Shell
    foreach ($folder in @([Environment]::GetFolderPath('Desktop'), [Environment]::GetFolderPath('Programs'))) {
        $shortcut = $shell.CreateShortcut((Join-Path $folder "TatsiaCraft $version.lnk"))
        $shortcut.TargetPath = Join-Path $destination 'TasiaCraft.exe'
        $shortcut.WorkingDirectory = $destination
        $shortcut.Description = "TatsiaCraft $version"
        $shortcut.Save()
    }
}
Write-Output $destination
