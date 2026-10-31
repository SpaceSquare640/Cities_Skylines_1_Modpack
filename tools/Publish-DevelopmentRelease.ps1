# Signature: OpenAI-Codex_Responsible: AI_Improve
[CmdletBinding()]
param([Parameter(Mandatory)][string]$AssetDirectory)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$allowed = @('AIImprove.dll','CitiesHarmony.API.dll','README.md','LICENSE','CitiesHarmony-LICENSE.txt','THIRD-PARTY-NOTICES.md','Steam_Workshop_Link.txt','build-info.json','SHA256SUMS')
$files = @(Get-ChildItem -LiteralPath $AssetDirectory -Force)
if ($files.Count -ne $allowed.Count) { throw 'Unexpected asset count.' }
foreach ($file in $files) {
    if ($file.Name -cnotin $allowed -or $file.PSIsContainer -or $file.Length -le 0 -or ($file.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Unexpected or empty asset.' }
}
$info = Get-Content -LiteralPath (Join-Path $AssetDirectory 'build-info.json') -Raw | ConvertFrom-Json
if ($info.sourceSHA -cne $env:GITHUB_SHA -or $info.runId -cne $env:GITHUB_RUN_ID -or $info.runAttempt -cne $env:GITHUB_RUN_ATTEMPT -or $info.DevTools -ne $true -or $info.EnableLateMergeProbe -ne $false -or $info.sharedReferencesSHA -cne '7b8a5e7c10f2db84388e8dd8af2084f239957b13') { throw 'Build provenance mismatch.' }
$lines = @(Get-Content -LiteralPath (Join-Path $AssetDirectory 'SHA256SUMS'))
if ($lines.Count -ne 8) { throw 'Checksum count mismatch.' }
$seen = @()
foreach ($line in $lines) {
    if ($line -cnotmatch '^([0-9a-f]{64})  ([A-Za-z0-9_.-]+)$') { throw 'Invalid checksum entry.' }
    $hash = $Matches[1]; $name = $Matches[2]
    if ($name -cnotin $allowed -or $name -eq 'SHA256SUMS' -or $name -cin $seen) { throw 'Unexpected checksum name.' }
    $seen += $name
    if ((Get-FileHash -LiteralPath (Join-Path $AssetDirectory $name) -Algorithm SHA256).Hash.ToLowerInvariant() -cne $hash) { throw "Checksum mismatch: $name" }
}
$tag = "ai-improve-dev-$env:GITHUB_RUN_ID-$env:GITHUB_RUN_ATTEMPT"
$headers = @{ Authorization = "Bearer $env:GH_TOKEN"; Accept = 'application/vnd.github+json'; 'X-GitHub-Api-Version' = '2022-11-28' }
foreach ($endpoint in @("git/ref/tags/$tag", "releases/tags/$tag")) {
    try {
        $null = Invoke-RestMethod -Uri "https://api.github.com/repos/$env:GITHUB_REPOSITORY/$endpoint" -Headers $headers
        throw "Refusing to reuse existing tag or release: $tag"
    } catch {
        if ($null -eq $_.Exception.Response -or [int]$_.Exception.Response.StatusCode -ne 404) { throw }
    }
}
gh release create $tag --repo $env:GITHUB_REPOSITORY --target $env:GITHUB_SHA --draft --prerelease --latest=false --title "AI Improve Development $tag" --notes-file (Join-Path $AssetDirectory 'README.md')
if ($LASTEXITCODE -ne 0) { throw 'Draft creation failed.' }
foreach ($file in $files) {
    gh release upload $tag $file.FullName --repo $env:GITHUB_REPOSITORY
    if ($LASTEXITCODE -ne 0) { throw "Asset upload failed: $($file.Name)" }
}
$releaseApi = gh release view $tag --repo $env:GITHUB_REPOSITORY --json apiUrl --jq .apiUrl
if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve draft release ID.' }
$releaseText = gh api $releaseApi
if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect draft.' }
$release = $releaseText | ConvertFrom-Json
if (-not $release.draft -or -not $release.prerelease -or $release.target_commitish -cne $env:GITHUB_SHA -or $release.assets.Count -ne $allowed.Count) { throw 'Draft metadata mismatch.' }
$verifyDirectory = Join-Path $env:RUNNER_TEMP "verify-$tag"
New-Item -ItemType Directory -Path $verifyDirectory | Out-Null
gh release download $tag --repo $env:GITHUB_REPOSITORY --dir $verifyDirectory
if ($LASTEXITCODE -ne 0) { throw 'Cannot download draft for verification.' }
foreach ($file in $files) {
    $remote = @($release.assets | Where-Object { $_.name -ceq $file.Name })
    $download = Get-Item -LiteralPath (Join-Path $verifyDirectory $file.Name)
    $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    if ($remote.Count -ne 1 -or $remote[0].size -ne $file.Length -or $download.Length -ne $file.Length -or (Get-FileHash -LiteralPath $download.FullName -Algorithm SHA256).Hash -cne $hash) { throw "Uploaded asset mismatch: $($file.Name)" }
    Write-Output ("Verified asset: {0} bytes={1} sha256={2}" -f $file.Name, $file.Length, $hash.ToLowerInvariant())
}
gh release edit $tag --repo $env:GITHUB_REPOSITORY --draft=false --prerelease --latest=false
if ($LASTEXITCODE -ne 0) { throw 'Publishing verified draft failed.' }
$finalText = gh api $releaseApi
if ($LASTEXITCODE -ne 0) { throw 'Cannot verify public release.' }
$final = $finalText | ConvertFrom-Json
if ($final.draft -or -not $final.prerelease -or $final.target_commitish -cne $env:GITHUB_SHA) { throw 'Public release state mismatch.' }
Write-Output "Published prerelease: $($final.html_url)"
