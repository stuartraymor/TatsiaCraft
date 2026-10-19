param([Parameter(Mandatory=$true)][string]$Source,[Parameter(Mandatory=$true)][string]$Destination)
# Mirrors the FFmpeg tools folder with hard links (no extra disk space on the same volume),
# falling back to copies when a link cannot be made (for example across volumes).
$ErrorActionPreference='Stop'
$Source=(Get-Item -LiteralPath $Source).FullName
New-Item -ItemType Directory -Path $Destination -Force | Out-Null
foreach($item in Get-ChildItem -LiteralPath $Source -Recurse -Force){
 $relative=$item.FullName.Substring($Source.Length).TrimStart('\')
 $target=Join-Path $Destination $relative
 if($item.PSIsContainer){New-Item -ItemType Directory -Path $target -Force | Out-Null;continue}
 if(Test-Path -LiteralPath $target){
  $existing=Get-Item -LiteralPath $target
  if($existing.Length -eq $item.Length -and $existing.LastWriteTimeUtc -eq $item.LastWriteTimeUtc){continue}
  Remove-Item -LiteralPath $target -Force
 }
 try{New-Item -ItemType HardLink -Path $target -Target $item.FullName | Out-Null}
 catch{Copy-Item -LiteralPath $item.FullName -Destination $target -Force}
}
