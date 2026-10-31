# Signature: OpenAI-Codex_Responsible: AI_Gaming
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$assets = Join-Path $env:RUNNER_TEMP 'release-assets'
New-Item -ItemType Directory -Path $assets | Out-Null
$publish = Join-Path $env:RUNNER_TEMP 'companion-development'
$unexpected = @(Get-ChildItem -LiteralPath $publish -File | Where-Object { $_.Name -notin @('AIGaming.Companion.exe','AIGaming.Companion.pdb') })
if ($unexpected.Count) { throw 'Unbundled Companion runtime files remain; refuse release.' }
Copy-Item -LiteralPath (Join-Path $publish 'AIGaming.Companion.exe') -Destination $assets
Copy-Item -LiteralPath (Join-Path $env:RUNNER_TEMP 'mod-development/AIGaming.dll') -Destination $assets
Copy-Item -LiteralPath LICENSE -Destination $assets
Copy-Item -LiteralPath docs/Development-Installation.md -Destination (Join-Path $assets 'README.md')
Copy-Item -LiteralPath docs/Steam_Workshop_Link.txt -Destination $assets
$generated = Get-Content obj/Release/CompanionBuildInfo.g.cs -Raw
foreach ($value in @($env:PAIR_VERSION,$env:PAIR_PROTOCOL,$env:PAIR_SHA256,'development')) {
    if (-not $generated.Contains('"' + $value + '"')) { throw 'Module pairing metadata mismatch' }
}
$exeHash = (Get-FileHash (Join-Path $assets 'AIGaming.Companion.exe') -Algorithm SHA256).Hash.ToLowerInvariant()
if ($exeHash -cne $env:PAIR_SHA256) { throw 'Companion hash mismatch' }
$restore = Get-Content (Join-Path $env:RUNNER_TEMP 'companion-obj/project.assets.json') -Raw | ConvertFrom-Json
$downloads = @($restore.project.frameworks.PSObject.Properties | ForEach-Object { $_.Value.downloadDependencies })
$pack = @($downloads | Where-Object { $_.name -ieq 'Microsoft.NETCore.App.Runtime.win-x64' })
if ($pack.Count -ne 1) { throw 'Cannot identify exact runtime version' }
$runtimeVersion = ($pack[0].version.Trim('[',']').Split(',')[0]).Trim()
if ($runtimeVersion -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid runtime version' }
$official = "https://raw.githubusercontent.com/dotnet/runtime/v$runtimeVersion"
Invoke-WebRequest "$official/LICENSE.TXT" -OutFile (Join-Path $assets 'DOTNET-LICENSE.txt')
Invoke-WebRequest "$official/THIRD-PARTY-NOTICES.TXT" -OutFile (Join-Path $assets 'DOTNET-THIRD-PARTY-NOTICES.txt')
@{
    sourceSHA=$env:GITHUB_SHA; sharedrefsSHA='7b8a5e7c10f2db84388e8dd8af2084f239957b13'
    runId=$env:GITHUB_RUN_ID; runAttempt=$env:GITHUB_RUN_ATTEMPT; DevTools=$true
    edition='development'; version=$env:PAIR_VERSION; protocol=$env:PAIR_PROTOCOL
    companionSHA256=$exeHash; runtimeVersion=$runtimeVersion
    dotnetLicenseSource="$official/LICENSE.TXT"; dotnetNoticesSource="$official/THIRD-PARTY-NOTICES.TXT"
    signature='OpenAI-Codex_Responsible: AI_Gaming'
} | ConvertTo-Json | Set-Content (Join-Path $assets 'build-info.json') -Encoding utf8
$expected = @('AIGaming.dll','AIGaming.Companion.exe','README.md','LICENSE','Steam_Workshop_Link.txt','DOTNET-LICENSE.txt','DOTNET-THIRD-PARTY-NOTICES.txt','build-info.json')
$files = @(Get-ChildItem -LiteralPath $assets -File)
if (@(Compare-Object ($expected | Sort-Object) ($files.Name | Sort-Object)).Count) { throw 'Asset allowlist mismatch' }
$sums = foreach ($file in ($files | Sort-Object Name)) {
    if ($file.Length -le 0) { throw 'Empty release file' }
    '{0}  {1}' -f (Get-FileHash $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant(),$file.Name
}
$sums | Set-Content (Join-Path $assets 'SHA256SUMS.txt') -Encoding utf8
Write-Output "Prepared $($files.Count + 1) allowlisted release assets; runtime $runtimeVersion."
