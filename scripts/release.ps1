param([Parameter(Mandatory=$true)][string]$Message,[switch]$Resume)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'version.ps1')
$releaseRoot=Split-Path $PSScriptRoot -Parent
Push-Location $releaseRoot
try {
 $env:GIT_TERMINAL_PROMPT='0'
 $branch=(& git branch --show-current).Trim()
 if($LASTEXITCODE -ne 0 -or $branch -ne 'main'){throw 'Release from the main branch'}
 & git diff --quiet
 if($LASTEXITCODE -ne 0){throw 'Stage intended tracked changes before releasing'}
 $remote=(& git remote get-url origin).Trim()
 if($remote -notmatch '^https://github\.com/([^/]+)/([^/]+?)(?:\.git)?$'){throw 'Expected an HTTPS github.com origin'}
 $repository=$Matches[1]+'/'+$Matches[2]
 & git -c credential.interactive=never fetch origin --tags
 if($LASTEXITCODE -ne 0){throw 'Could not fetch GitHub tags'}
 $token=$env:GH_TOKEN
 if(-not $token){$token=$env:GITHUB_TOKEN}
 if(-not $token){
  $credentialLines="protocol=https`nhost=github.com`n`n" | git -c credential.interactive=never credential fill 2>$null
  if($LASTEXITCODE -ne 0){throw 'Authenticate Git with GitHub, or supply GH_TOKEN'}
  foreach($line in $credentialLines){if($line.StartsWith('password=')){$token=$line.Substring(9)}}
 }
 if(-not $token){throw 'GitHub release authentication unavailable'}
 $headers=@{Authorization=('Bearer '+$token);Accept='application/vnd.github+json';'User-Agent'='TatsiaCraft-Release'}
 $api='https://api.github.com/repos/'+$repository
 if($Resume){
  & git diff --cached --quiet
  if($LASTEXITCODE -ne 0){throw 'Resume requires a clean index'}
  $version=(Get-Content VERSION -Raw).Trim()
  $tag='v'+$version
  $tagHead=(& git rev-list -n 1 $tag).Trim()
  $head=(& git rev-parse HEAD).Trim()
  if($tagHead -ne $head){throw 'Resume requires HEAD to match the version tag'}
 }else{
  $versions=@(& git tag --list 'v[0-9]*' --sort=-version:refname | Where-Object {$_ -match '^v[0-9]+\.(?:[0-9]|1[0-2])\.(?:[0-9]|1[0-2])$'})
  $previous=if($versions.Count){$versions[0].Substring(1)}else{'0.0.0'}
  $version=Get-NextRevision $previous
  $tag='v'+$version
  Set-Content -LiteralPath VERSION -Value $version -Encoding ASCII
 }
 # Compile and test before pushing or publishing any revision.
 & (Join-Path $PSScriptRoot 'test-canvas.ps1')
 if(-not $Resume){
  & git add -- VERSION
  if($LASTEXITCODE -ne 0){throw 'Could not stage VERSION'}
  & git commit -m ("Release $version`: $Message")
  if($LASTEXITCODE -ne 0){throw 'Release commit failed'}
  & git tag -a $tag -m ("TatsiaCraft $version")
  if($LASTEXITCODE -ne 0){throw 'Version tag failed'}
 }
 & git -c credential.interactive=never push --atomic origin HEAD:refs/heads/main ('refs/tags/'+$tag)
 if($LASTEXITCODE -ne 0){throw 'GitHub push failed; preserve this tag and use -Resume after resolving the failure'}
 $head=(& git rev-parse HEAD).Trim()
 $sourcePath=Join-Path $releaseRoot ("artifacts\TatsiaCraft-$version-source.zip")
 & git archive --format=zip ('--output='+$sourcePath) HEAD
 if($LASTEXITCODE -ne 0){throw 'Source archive failed; rerun with -Resume'}
 $appPath=Join-Path $releaseRoot ("artifacts\TatsiaCraft-$version.exe")
 Copy-Item -LiteralPath 'dist\TasiaCraft\TasiaCraft.exe' -Destination $appPath
 $release=$null
 try{$release=Invoke-RestMethod -Uri ($api+'/releases/tags/'+$tag) -Headers $headers}catch{if([int]$_.Exception.Response.StatusCode -ne 404){throw}}
 if(-not $release){
  $body=@{tag_name=$tag;target_commitish=$head;name=('TatsiaCraft '+$version);body=($Message+"`n`nWindows x64 development preview. The executable requires .NET Framework 4.8. Media features require separately supplied compatible FFmpeg/FFprobe tools beside the executable in a tools folder. Complete feature parity and comparative performance are not yet established.");draft=$true;prerelease=$true} | ConvertTo-Json
  $release=Invoke-RestMethod -Method Post -Uri ($api+'/releases') -Headers $headers -ContentType 'application/json' -Body $body
 }
 $releaseApi=$api+'/releases/'+$release.id
 foreach($path in @($appPath,$sourcePath)){
  $file=Get-Item -LiteralPath $path
  $existing=@($release.assets | Where-Object name -eq $file.Name)
  if($existing.Count){
   $digest='sha256:'+(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
   if($existing[0].size -ne $file.Length -or ($existing[0].digest -and $existing[0].digest -ne $digest)){throw ('Existing release asset differs: '+$file.Name)}
  }else{
   if(-not $release.draft){throw 'Published release is missing an asset; inspect it before changing the published release'}
   $uploadBase=$release.upload_url -replace '\{.*$',''
   $uploadUri=$uploadBase+'?name='+[Uri]::EscapeDataString($file.Name)
   $asset=Invoke-RestMethod -Method Post -Uri $uploadUri -Headers $headers -ContentType 'application/octet-stream' -InFile $file.FullName
   if($asset.state -ne 'uploaded' -or $asset.size -ne $file.Length){throw ('Asset upload verification failed: '+$file.Name)}
  }
 }
 if($release.draft){$release=Invoke-RestMethod -Method Patch -Uri $releaseApi -Headers $headers -ContentType 'application/json' -Body '{"draft":false}'}
 $verified=Invoke-RestMethod -Uri $releaseApi -Headers $headers
 $remoteHead=(& git ls-remote origin refs/heads/main).Split()[0]
 if($LASTEXITCODE -ne 0 -or $remoteHead -ne $head -or $verified.draft -or $verified.tag_name -ne $tag -or @($verified.assets | Where-Object state -eq 'uploaded').Count -lt 2){throw 'Remote revision verification failed'}
 Write-Output ('Published '+$verified.html_url)
} finally {Pop-Location}
