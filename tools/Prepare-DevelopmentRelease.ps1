# Signature: OpenAI-Codex_Responsible: AI_Improve
[CmdletBinding()]
param([Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Release staging must be new.' }
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$files = @{
    'AIImprove.dll' = 'bin/Release/AIImprove.dll'
    'CitiesHarmony.API.dll' = 'bin/Release/CitiesHarmony.API.dll'
    'README.md' = 'docs/Development-Installation.md'
    'LICENSE' = 'LICENSE'
    'CitiesHarmony-LICENSE.txt' = 'third-party/CitiesHarmony-LICENSE.txt'
    'THIRD-PARTY-NOTICES.md' = 'third-party/THIRD-PARTY-NOTICES.md'
    'Steam_Workshop_Link.txt' = 'docs/Steam_Workshop_Link.txt'
}
$package = Join-Path $env:USERPROFILE '.nuget/packages/citiesharmony.api/2.2.0'
$apiFiles = @(Get-ChildItem -LiteralPath $package -Filter CitiesHarmony.API.dll -Recurse -File)
$apiHash = (Get-FileHash -LiteralPath $files['CitiesHarmony.API.dll'] -Algorithm SHA256).Hash
if (-not ($apiFiles | Where-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash -eq $apiHash })) {
    throw 'The API DLL does not match the restored CitiesHarmony.API 2.2.0 package.'
}
$licenseHash = (Get-FileHash -LiteralPath $files['CitiesHarmony-LICENSE.txt'] -Algorithm SHA256).Hash
if ($licenseHash -ne '93D48EB29E43A507418F2D7931D44593E867F0B1AF59B08676BC71C29F30BAC0') { throw 'Official CitiesHarmony license checksum mismatch.' }
foreach ($name in $files.Keys) {
    $source = Get-Item -LiteralPath $files[$name]
    if ($source.Length -le 0 -or $source.PSIsContainer -or ($source.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "Invalid release file: $name" }
    Copy-Item -LiteralPath $source.FullName -Destination (Join-Path $OutputDirectory $name)
}
@{
    sourceSHA = $env:GITHUB_SHA
    sharedReferencesSHA = '7b8a5e7c10f2db84388e8dd8af2084f239957b13'
    runId = $env:GITHUB_RUN_ID
    runAttempt = $env:GITHUB_RUN_ATTEMPT
    DevTools = $true
    EnableLateMergeProbe = $false
    signature = 'OpenAI-Codex_Responsible: AI_Improve'
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDirectory 'build-info.json') -Encoding utf8
$hashes = Get-ChildItem -LiteralPath $OutputDirectory -File | Sort-Object Name | ForEach-Object {
    '{0}  {1}' -f (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $_.Name
}
$hashes | Set-Content -LiteralPath (Join-Path $OutputDirectory 'SHA256SUMS') -Encoding ascii
if (@(Get-ChildItem -LiteralPath $OutputDirectory -Force).Count -ne 9) { throw 'Unexpected staging contents.' }
