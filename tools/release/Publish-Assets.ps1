# Signature: OpenAI-Codex_Responsible: AI_Gaming
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
function Invoke-Gh { param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Arguments) & gh @Arguments; if ($LASTEXITCODE -ne 0) { throw 'GitHub operation failed; leave draft unchanged.' } }
$assets = Join-Path $env:RUNNER_TEMP 'release-assets'
$expected = @('AIGaming.dll','AIGaming.Companion.exe','README.md','LICENSE','Steam_Workshop_Link.txt','DOTNET-LICENSE.txt','DOTNET-THIRD-PARTY-NOTICES.txt','build-info.json','SHA256SUMS.txt')
$files = @(Get-ChildItem -LiteralPath $assets -File)
if (@(Get-ChildItem -LiteralPath $assets -Directory).Count -or @(Compare-Object ($expected | Sort-Object) ($files.Name | Sort-Object)).Count) { throw 'Asset allowlist mismatch' }
foreach ($line in Get-Content (Join-Path $assets 'SHA256SUMS.txt')) {
    if ($line -notmatch '^([a-f0-9]{64})  ([A-Za-z0-9._-]+)$') { throw 'Invalid checksum line' }
    $digest=$Matches[1]; $name=$Matches[2]
    if ($name -notin $expected -or $name -eq 'SHA256SUMS.txt') { throw 'Invalid checksum filename' }
    if ((Get-FileHash (Join-Path $assets $name) -Algorithm SHA256).Hash.ToLowerInvariant() -cne $digest) { throw 'Checksum mismatch' }
}
$info = Get-Content (Join-Path $assets 'build-info.json') -Raw | ConvertFrom-Json
if ($info.sourceSHA -cne $env:GITHUB_SHA -or $info.runId -ne $env:GITHUB_RUN_ID -or -not $info.DevTools) { throw 'Source provenance mismatch' }
$tag = "ai-gaming-dev-$($env:GITHUB_RUN_ID)-$($env:GITHUB_RUN_ATTEMPT)"
$repo = $env:GITHUB_REPOSITORY
Invoke-Gh @('--repo',$repo,'release','create',$tag,'--target',$env:GITHUB_SHA,'--draft','--prerelease','--latest=false','--title',"AI Gaming Development $tag",'--notes-file',(Join-Path $assets 'README.md'))
foreach ($file in $files) { Invoke-Gh @('--repo',$repo,'release','upload',$tag,$file.FullName) }
$drafts = (Invoke-Gh @('api',"repos/$repo/releases?per_page=100")) | ConvertFrom-Json
$matching = @($drafts | Where-Object tag_name -CEQ $tag)
if ($matching.Count -ne 1) { throw 'Unique draft not found' }
$release = $matching[0]
if (-not $release.draft -or -not $release.prerelease -or $release.target_commitish -cne $env:GITHUB_SHA -or $release.assets.Count -ne $expected.Count) { throw 'Draft metadata mismatch' }
$verify = Join-Path $env:RUNNER_TEMP 'verified-assets'
Invoke-Gh @('--repo',$repo,'release','download',$tag,'--dir',$verify)
foreach ($file in $files) {
    $remote = @($release.assets | Where-Object name -CEQ $file.Name)
    if ($remote.Count -ne 1 -or $remote[0].size -ne $file.Length) { throw 'Uploaded size/name mismatch' }
    $hash = (Get-FileHash $file.FullName -Algorithm SHA256).Hash
    if ((Get-FileHash (Join-Path $verify $file.Name) -Algorithm SHA256).Hash -cne $hash) { throw 'Uploaded hash mismatch' }
    Write-Output "$($file.Name) $($file.Length) SHA256=$($hash.ToLowerInvariant())"
}
Invoke-Gh @('--repo',$repo,'release','edit',$tag,'--draft=false','--prerelease','--latest=false')
$final = (Invoke-Gh @('api',"repos/$repo/releases/$($release.id)")) | ConvertFrom-Json
if ($final.draft -or -not $final.prerelease) { throw 'Release publication not confirmed' }
"Published prerelease: $($final.html_url)" | Tee-Object -FilePath $env:GITHUB_STEP_SUMMARY -Append
