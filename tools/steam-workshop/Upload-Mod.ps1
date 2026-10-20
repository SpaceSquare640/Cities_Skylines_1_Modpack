[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('AI_Improve', 'Automated_Public_Transport_Planning')]
    [string]$Module,

    [Parameter(Mandatory = $true)]
    [ValidateSet('Release', 'Development')]
    [string]$Edition,

    [Parameter(Mandatory = $true)]
    [string]$SourcePath,

    [Parameter(Mandatory = $true)]
    [string]$PublishedFileId,

    [Parameter(Mandatory = $true)]
    [string]$PreviewFileName,

    [string]$ChangeNote = '',

    [Parameter(Mandatory = $true)]
    [string]$ConfirmUpload
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($ConfirmUpload -cne 'UPLOAD EXISTING ITEM') {
    throw 'Upload blocked. Choose "UPLOAD EXISTING ITEM" in the manual workflow input to continue.'
}

if ($PublishedFileId -notmatch '^\d{1,20}$' -or $PublishedFileId -eq '0') {
    throw 'A non-zero existing Steam Workshop PublishedFileId is required. New item creation is disabled.'
}

# Each module and edition is pinned to its own Workshop item, so a mistyped or pasted ID can never
# push a build into the wrong item (the Release item is public with real subscribers). Register an
# ID here once its item exists; any combination without one fails closed.
$registeredItemIds = @{
    'AI_Improve/Release'     = '3782858610'
    'AI_Improve/Development' = '3823575900'
}
$itemKey = "$Module/$Edition"
if (-not $registeredItemIds.ContainsKey($itemKey)) {
    throw "No Workshop item ID is registered for $itemKey yet. Create the item, then register its ID in Upload-Mod.ps1."
}
if ($PublishedFileId -cne $registeredItemIds[$itemKey]) {
    throw "PublishedFileId $PublishedFileId is not the registered $itemKey item ($($registeredItemIds[$itemKey]))."
}

foreach ($name in @('CITIES_SKYLINES_DIR', 'MODPACK_ROOT', 'STEAMCMD_EXE', 'STEAMCMD_USERNAME', 'RUNNER_TEMP')) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) {
        throw "Required self-hosted runner environment variable is missing: $name"
    }
}

$gameRoot = [IO.Path]::GetFullPath($env:CITIES_SKYLINES_DIR)
$managedAssembly = Join-Path $gameRoot 'Cities_Data\Managed\ICities.dll'
if (-not (Test-Path -LiteralPath $managedAssembly -PathType Leaf)) {
    throw "Cities: Skylines managed assemblies were not found at the configured game path: $managedAssembly"
}

$modpackRoot = [IO.Path]::GetFullPath($env:MODPACK_ROOT)
$iconRoot = [IO.Path]::GetFullPath((Join-Path $modpackRoot 'Resource\Icon\Uses_icon'))
if ($PreviewFileName -match '[\\/]' -or $PreviewFileName -notmatch '\.(png|jpe?g|gif)$') {
    throw 'PreviewFileName must be a PNG, JPG, JPEG, or GIF filename without a directory path.'
}

$previewPath = [IO.Path]::GetFullPath((Join-Path $iconRoot $PreviewFileName))
$iconRootPrefix = $iconRoot.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $previewPath.StartsWith($iconRootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Preview image must be inside the configured Modpack Icon folder.'
}
if (-not (Test-Path -LiteralPath $previewPath -PathType Leaf)) {
    throw "Preview image not found in the Modpack Icon folder: $PreviewFileName"
}

$steamCmd = [IO.Path]::GetFullPath($env:STEAMCMD_EXE)
if (-not (Test-Path -LiteralPath $steamCmd -PathType Leaf)) {
    throw "SteamCMD executable not found at the configured path: $steamCmd"
}
$steamCmdRoot = Split-Path -Parent $steamCmd
$steamLoginConfig = Join-Path $steamCmdRoot 'config\config.vdf'
if (-not (Test-Path -LiteralPath $steamLoginConfig -PathType Leaf)) {
    throw 'SteamCMD has no saved login configuration. Complete one interactive SteamCMD login with Steam Guard on this runner first.'
}
if ($env:STEAMCMD_USERNAME -match '\s') {
    throw 'STEAMCMD_USERNAME must be a Steam account name without whitespace.'
}

$sourceRoot = [IO.Path]::GetFullPath($SourcePath)
$runnerTemp = [IO.Path]::GetFullPath($env:RUNNER_TEMP)
$packageId = [Guid]::NewGuid().ToString('N')
$packageRoot = Join-Path $runnerTemp "steam-workshop-$Module-$Edition-$packageId"
$contentRoot = Join-Path $packageRoot 'content'
New-Item -ItemType Directory -Path $contentRoot -Force | Out-Null

switch ($Module) {
    'AI_Improve' {
        $project = Join-Path $sourceRoot 'AIImprove.csproj'
        $outputDll = Join-Path $sourceRoot 'bin\Release\AIImprove.dll'
        if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
            throw 'AIImprove.csproj is missing from the checked-out AI_Improve branch.'
        }
        $dotnet = Get-Command dotnet -ErrorAction Stop
        # The project's CopyToMods target copies every build into the local Mods folder. On the
        # runner that folder belongs to the maintainer's own game, so send the copy to the job's
        # package folder instead of replacing whatever build they are playing with.
        & $dotnet.Source build $project --configuration Release '-p:DevTools=false' "-p:CSInstallPath=$gameRoot" "-p:CSDataPath=$(Join-Path $packageRoot 'mods-copy')"
        if ($LASTEXITCODE -ne 0) {
            throw "AI Improve build failed with exit code $LASTEXITCODE."
        }
    }

    'Automated_Public_Transport_Planning' {
        $project = Join-Path $sourceRoot 'AutomatedPublicTransportPlanning.csproj'
        $outputDll = Join-Path $sourceRoot 'bin\Release\AutomatedPublicTransportPlanning.dll'
        if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
            throw 'AutomatedPublicTransportPlanning.csproj is missing from the checked-out Transport branch.'
        }
        $msbuild = Get-Command msbuild.exe -ErrorAction Stop
        & $msbuild.Source $project '/t:Build' '/p:Configuration=Release' "/p:CitiesSkylinesDir=$gameRoot"
        if ($LASTEXITCODE -ne 0) {
            throw "Transport build failed with exit code $LASTEXITCODE."
        }
    }
}

if (-not (Test-Path -LiteralPath $outputDll -PathType Leaf)) {
    throw "Expected release DLL was not produced: $outputDll"
}

$packageDll = Join-Path $contentRoot (Split-Path -Leaf $outputDll)
Copy-Item -LiteralPath $outputDll -Destination $packageDll
$hash = (Get-FileHash -LiteralPath $packageDll -Algorithm SHA256).Hash
Write-Output "Prepared $Module $Edition Workshop content: $(Split-Path -Leaf $packageDll), SHA-256 $hash"

$workshopTitle = switch ("$Module/$Edition") {
    'AI_Improve/Release' { 'AI Improve: Release Version' }
    'AI_Improve/Development' { 'AI Improve: Development Version' }
    'Automated_Public_Transport_Planning/Release' { 'Automated Public Transport Planning: Release Version' }
    'Automated_Public_Transport_Planning/Development' { 'Automated Public Transport Planning: Development Version' }
    default { throw 'Unsupported module and edition combination.' }
}

function ConvertTo-VdfValue {
    param([Parameter(Mandatory = $true)][string]$Value)
    if ($Value.Contains([char]13) -or $Value.Contains([char]10)) {
        throw 'Workshop VDF values must not contain line breaks.'
    }
    return $Value.Replace('\', '\\').Replace('"', '\"')
}

$vdfPath = Join-Path $packageRoot 'workshop-item.vdf'
$vdfLines = @(
    '"workshopitem"'
    '{'
    '    "appid" "255710"'
    '    "publishedfileid" "' + (ConvertTo-VdfValue $PublishedFileId) + '"'
    '    "title" "' + (ConvertTo-VdfValue $workshopTitle) + '"'
    '    "contentfolder" "' + (ConvertTo-VdfValue $contentRoot) + '"'
    '    "previewfile" "' + (ConvertTo-VdfValue $previewPath) + '"'
)
if (-not [string]::IsNullOrWhiteSpace($ChangeNote)) {
    $vdfLines += '    "changenote" "' + (ConvertTo-VdfValue $ChangeNote.Trim()) + '"'
}
$vdfLines += '}'
[IO.File]::WriteAllText($vdfPath, ($vdfLines -join [Environment]::NewLine), [Text.UTF8Encoding]::new($false))

Write-Output "Target: $workshopTitle (existing item $PublishedFileId)"
Write-Output 'Visibility, description, and tags are omitted from the VDF and remain unchanged.'
Write-Output 'SteamCMD credentials and Steam Guard codes are not supplied by this workflow; the runner uses its saved local SteamCMD login.'

$startInfo = [Diagnostics.ProcessStartInfo]::new()
$startInfo.FileName = $steamCmd
$startInfo.WorkingDirectory = $steamCmdRoot
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true
$startInfo.ArgumentList.Add('+login')
$startInfo.ArgumentList.Add($env:STEAMCMD_USERNAME)
$startInfo.ArgumentList.Add('+workshop_build_item')
$startInfo.ArgumentList.Add($vdfPath)
$startInfo.ArgumentList.Add('+quit')

$process = [Diagnostics.Process]::Start($startInfo)
$process.WaitForExit()
if ($process.ExitCode -ne 0) {
    throw "SteamCMD upload command failed with exit code $($process.ExitCode). Check the SteamCMD Workshop logs and item page before retrying."
}

Write-Output 'SteamCMD exited with code 0. Verify the item page and Workshop build logs before treating the update as published.'

# Author: OpenAI-Codex.
# Modified by: Anthropic-Claude_Code.
