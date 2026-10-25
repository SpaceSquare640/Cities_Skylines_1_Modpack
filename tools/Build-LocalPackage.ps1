[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("release", "development")]
    [string]$Edition,

    [string]$CSInstallPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$moduleProjectPath = Join-Path $repoRoot "AIGaming.csproj"
$companionProjectPath = Join-Path $repoRoot "Companion\AIGaming.Companion.csproj"
$packageRoot = Join-Path $repoRoot ("bin\local-packages\" + $Edition)
$companionOutput = Join-Path $packageRoot "Companion"

if (Test-Path -LiteralPath $packageRoot) {
    throw "Package output already exists at '$packageRoot'. No files were overwritten or removed. Review that directory before retrying."
}

function Get-ProjectProperty {
    param(
        [System.Xml.XmlDocument]$Project,
        [string]$Name,
        [string]$Path
    )

    $nodes = $Project.SelectNodes("/Project/PropertyGroup/$Name")
    if ($nodes.Count -eq 0 -or [string]::IsNullOrWhiteSpace($nodes[0].InnerText)) {
        throw "Required property '$Name' was not found in '$Path'."
    }

    return $nodes[0].InnerText.Trim()
}

[System.Xml.XmlDocument]$moduleProject = New-Object System.Xml.XmlDocument
$moduleProject.Load($moduleProjectPath)
[System.Xml.XmlDocument]$companionProject = New-Object System.Xml.XmlDocument
$companionProject.Load($companionProjectPath)

$moduleVersion = Get-ProjectProperty -Project $moduleProject -Name "Version" -Path $moduleProjectPath
$companionVersion = Get-ProjectProperty -Project $companionProject -Name "Version" -Path $companionProjectPath
$moduleProtocol = Get-ProjectProperty -Project $moduleProject -Name "CompanionProtocolVersion" -Path $moduleProjectPath
$companionProtocol = Get-ProjectProperty -Project $companionProject -Name "CompanionProtocolVersion" -Path $companionProjectPath

if ($moduleVersion -ne $companionVersion -or $moduleProtocol -ne $companionProtocol) {
    throw "Module and companion versions/protocols do not match. No build was started."
}

$dotnetCommand = Get-Command "dotnet" -ErrorAction Stop
$dotnetPath = $dotnetCommand.Source
if ([string]::IsNullOrWhiteSpace($dotnetPath)) {
    throw "The dotnet CLI executable could not be resolved."
}

$companionPublishArguments = @(
    "publish",
    $companionProjectPath,
    "--configuration", "Release",
    "--no-restore",
    "--runtime", "win-x64",
    "--self-contained", "true",
    "--output", $companionOutput,
    "-p:PublishSingleFile=true",
    "-p:EnableCompressionInSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:CompanionEdition=$Edition",
    "-p:CompanionProtocolVersion=$moduleProtocol",
    "-p:Version=$moduleVersion"
)

Write-Output "Publishing the $Edition companion directly with dotnet; no local runner or workflow is invoked."
& $dotnetPath @companionPublishArguments
if ($LASTEXITCODE -ne 0) {
    throw "Self-contained companion publish failed with exit code $LASTEXITCODE. No cleanup is performed; inspect the ignored output directory before retrying."
}

$companionExecutable = Join-Path $companionOutput "AIGaming.Companion.exe"
if (-not (Test-Path -LiteralPath $companionExecutable -PathType Leaf)) {
    throw "The self-contained companion executable was not produced at '$companionExecutable'."
}

$companionSha256 = (Get-FileHash -LiteralPath $companionExecutable -Algorithm SHA256).Hash.ToLowerInvariant()
$devTools = if ($Edition -eq "development") { "true" } else { "false" }
$moduleBuildArguments = @(
    "build",
    $moduleProjectPath,
    "--configuration", "Release",
    "--no-restore",
    "--output", $packageRoot,
    "-p:DevTools=$devTools",
    "-p:CompanionEdition=$Edition",
    "-p:CompanionVersion=$moduleVersion",
    "-p:CompanionProtocolVersion=$moduleProtocol",
    "-p:CompanionSha256=$companionSha256"
)

if (-not [string]::IsNullOrWhiteSpace($CSInstallPath)) {
    $moduleBuildArguments += "-p:CSInstallPath=$CSInstallPath"
}

Write-Output "Building the matching $Edition module directly with dotnet; output is under the ignored bin directory."
& $dotnetPath @moduleBuildArguments
if ($LASTEXITCODE -ne 0) {
    throw "Module build failed with exit code $LASTEXITCODE. No cleanup is performed; inspect the ignored output directory before retrying."
}

$moduleAssembly = Join-Path $packageRoot "AIGaming.dll"
if (-not (Test-Path -LiteralPath $moduleAssembly -PathType Leaf)) {
    throw "The AI Gaming module assembly was not produced at '$moduleAssembly'."
}

$packageInfoPath = Join-Path $packageRoot "PACKAGE-INFO.txt"
@(
    "Edition=$Edition"
    "Version=$moduleVersion"
    "ProtocolVersion=$moduleProtocol"
    "CompanionSha256=$companionSha256"
) | Set-Content -LiteralPath $packageInfoPath -Encoding Ascii

Write-Output "Package directory: $packageRoot"
Write-Output "Companion SHA-256: $companionSha256"

# Local package helper signed by OpenAI-Codex.
