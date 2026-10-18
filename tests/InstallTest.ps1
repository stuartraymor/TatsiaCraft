param([string]$ToolsPath)
$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path $PSScriptRoot -Parent
$testRoot = Join-Path $sourceRoot ('artifacts\it-' + [guid]::NewGuid().ToString('N').Substring(0,8))
$scriptPath = Join-Path $sourceRoot 'scripts\install.ps1'
$version = (Get-Content -LiteralPath (Join-Path $sourceRoot 'VERSION') -Raw).Trim()
$checks = 0
function Assert($condition, $message) { if (!$condition) { throw $message }; $script:checks++ }
try {
    $installed = & $scriptPath -InstallRoot $testRoot -ToolsPath $ToolsPath -NoShortcuts
    $manifest = Get-Content -LiteralPath (Join-Path $installed 'install-manifest.json') -Raw | ConvertFrom-Json
    Assert ($manifest.version -eq $version) 'Manifest version mismatch.'
    Assert ($manifest.toolsIncluded -eq [bool]$ToolsPath) 'Tools inclusion mismatch.'
    foreach ($file in $manifest.files) {
        $actual = Get-Item -LiteralPath (Join-Path $installed $file.path)
        Assert ((Get-FileHash -LiteralPath $actual.FullName -Algorithm SHA256).Hash -eq $file.sha256) "Hash mismatch: $($file.path)"
        Assert ($actual.Length -eq $file.bytes) "Size mismatch: $($file.path)"
    }
    Assert (Test-Path -LiteralPath (Join-Path $installed 'LICENSE')) 'License missing.'
    $refused = $false
    try { & $scriptPath -InstallRoot $testRoot -NoShortcuts | Out-Null } catch { $refused = $_.Exception.Message -like '*never overwritten*' }
    Assert $refused 'Existing install was not protected.'
    $refused = $false
    try { & $scriptPath -ExecutablePath ([Diagnostics.Process]::GetCurrentProcess().MainModule.FileName) -InstallRoot (Join-Path $testRoot 'mismatch') -NoShortcuts | Out-Null } catch { $refused = $_.Exception.Message -like '*version must match*' }
    Assert $refused 'Mismatched executable version was not rejected.'
    Write-Output "PASS: $checks installation checks. Temporary installation: $installed"
} finally {
    # Confined to the unique directory created for this test, never a user installation.
    $expectedParent = [IO.Path]::GetFullPath((Join-Path $sourceRoot 'artifacts'))
    if ((Split-Path ([IO.Path]::GetFullPath($testRoot)) -Parent) -ne $expectedParent) { throw 'Unsafe test cleanup path.' }
    if (Test-Path -LiteralPath $testRoot) { Remove-Item -LiteralPath $testRoot -Recurse -Force }
}
