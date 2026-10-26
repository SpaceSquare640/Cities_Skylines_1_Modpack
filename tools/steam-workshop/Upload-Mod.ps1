[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('AI_Improve', 'Automated_Public_Transport_Planning', 'AI_Gaming')]
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
    'AI_Improve/Release'                              = '3782858610'
    'AI_Improve/Development'                          = '3823575900'
    'Automated_Public_Transport_Planning/Release'     = '3823739383'
    'Automated_Public_Transport_Planning/Development' = '3823739653'
    'AI_Gaming/Release'                               = '3823740106'
    'AI_Gaming/Development'                           = '3823740277'
}
$itemKey = "$Module/$Edition"
if (-not $registeredItemIds.ContainsKey($itemKey)) {
    throw "No Workshop item ID is registered for $itemKey yet. Create the item, then register its ID in Upload-Mod.ps1."
}
if ($PublishedFileId -cne $registeredItemIds[$itemKey]) {
    throw "PublishedFileId $PublishedFileId is not the registered $itemKey item ($($registeredItemIds[$itemKey]))."
}

# The edition decides the build, not a workflow input: Development items always carry the
# DevTools build (testing panels and probes), Release items never do. Nothing a dispatcher types
# can put DevTools into a Release item.
$devTools = if ($Edition -eq 'Development') { 'true' } else { 'false' }

# Types that exist only in a DevTools build. Checked after the build so a broken gate in a project
# file cannot slip a DevTools DLL into a Release item, or ship a Development item without its tools.
$devToolsMarkers = @{
    'AI_Improve'                          = 'DevTriggerExtension'
    'Automated_Public_Transport_Planning' = 'SaveRoundTripSpike'
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
# Workshop previews come from the JPG set: the PNG originals in Uses_icon\PNG are 1.8-2.1 MB, over
# Steam's 1 MB preview limit.
$iconRoot = [IO.Path]::GetFullPath((Join-Path $modpackRoot 'Resource\Icon\Uses_icon\JPG'))
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
if ((Get-Item -LiteralPath $previewPath).Length -gt 1MB) {
    throw "Preview image is larger than Steam's 1 MB limit: $PreviewFileName"
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
        & $dotnet.Source build $project --configuration Release --no-incremental "-p:DevTools=$devTools" "-p:CSInstallPath=$gameRoot" "-p:CSDataPath=$(Join-Path $packageRoot 'mods-copy')"
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
        # Old-style project: needs Visual Studio's MSBuild, which is rarely on PATH. Fall back to
        # vswhere, the locator Visual Studio installs for exactly this purpose.
        # StrictMode is on, so reading .Source from a failed Get-Command would itself throw.
        $msbuildCommand = Get-Command msbuild.exe -ErrorAction SilentlyContinue
        $msbuildPath = if ($msbuildCommand) { $msbuildCommand.Source } else { $null }
        if (-not $msbuildPath) {
            $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
            if (Test-Path -LiteralPath $vswhere -PathType Leaf) {
                $msbuildPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
            }
        }
        if (-not $msbuildPath) {
            throw 'MSBuild was not found on PATH or through vswhere. Install Visual Studio or its Build Tools with MSBuild on the runner.'
        }
        # ModsDir is cleared so the project's DeployToMods step cannot copy into the runner
        # account's own Mods folder.
        & $msbuildPath $project '/t:Rebuild' '/p:Configuration=Release' "/p:DevTools=$devTools" "/p:CitiesSkylinesDir=$gameRoot" '/p:ModsDir='
        if ($LASTEXITCODE -ne 0) {
            throw "Transport build failed with exit code $LASTEXITCODE."
        }
    }

    'AI_Gaming' {
        $project = Join-Path $sourceRoot 'AIGaming.csproj'
        $outputDll = Join-Path $sourceRoot 'bin\Release\AIGaming.dll'
        $companionProject = Join-Path $sourceRoot 'Companion\AIGaming.Companion.csproj'
        if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
            throw 'AIGaming.csproj is missing from the checked-out AI_Gaming branch.'
        }
        if (-not (Test-Path -LiteralPath $companionProject -PathType Leaf)) {
            throw 'Companion\AIGaming.Companion.csproj is missing from the checked-out AI_Gaming branch.'
        }
        $dotnet = Get-Command dotnet -ErrorAction Stop

        function Get-ProjectProperty {
            param(
                [Parameter(Mandatory = $true)]
                [xml]$ProjectXml,

                [Parameter(Mandatory = $true)]
                [string]$PropertyName
            )

            $node = $ProjectXml.SelectSingleNode("/Project/PropertyGroup/$PropertyName")
            if ($null -eq $node -or [string]::IsNullOrWhiteSpace($node.InnerText)) {
                throw "Required property '$PropertyName' was not found in a project file."
            }

            return $node.InnerText.Trim()
        }

        function Test-ByteSequence {
            param(
                [Parameter(Mandatory = $true)]
                [byte[]]$Bytes,

                [Parameter(Mandatory = $true)]
                [byte[]]$Pattern
            )

            if ($Pattern.Length -eq 0 -or $Pattern.Length -gt $Bytes.Length) {
                return $false
            }

            for ($start = 0; $start -le $Bytes.Length - $Pattern.Length; $start++) {
                $matches = $true
                for ($offset = 0; $offset -lt $Pattern.Length; $offset++) {
                    if ($Bytes[$start + $offset] -ne $Pattern[$offset]) {
                        $matches = $false
                        break
                    }
                }

                if ($matches) {
                    return $true
                }
            }

            return $false
        }

        function Assert-CleanSourceCheckout {
            param(
                [Parameter(Mandatory = $true)]
                [string]$Path
            )

            $status = & git -C $Path status --porcelain=v1 --untracked-files=all
            if ($LASTEXITCODE -ne 0) {
                throw 'Could not verify that the AI Gaming source checkout is clean.'
            }
            if (-not [string]::IsNullOrWhiteSpace(($status -join "`n"))) {
                throw 'The AI Gaming source checkout has modified or untracked files. Upload stopped so the source commit exactly identifies the build.'
            }
        }

        [xml]$moduleProjectXml = Get-Content -LiteralPath $project -Raw
        [xml]$companionProjectXml = Get-Content -LiteralPath $companionProject -Raw
        $moduleVersion = Get-ProjectProperty -ProjectXml $moduleProjectXml -PropertyName 'Version'
        $companionVersion = Get-ProjectProperty -ProjectXml $companionProjectXml -PropertyName 'Version'
        $moduleProtocolVersion = Get-ProjectProperty -ProjectXml $moduleProjectXml -PropertyName 'CompanionProtocolVersion'
        $companionProtocolVersion = Get-ProjectProperty -ProjectXml $companionProjectXml -PropertyName 'CompanionProtocolVersion'
        if ($moduleVersion -cne $companionVersion -or $moduleProtocolVersion -cne $companionProtocolVersion) {
            throw "AI Gaming module and companion versions/protocols do not match. No Workshop upload was prepared."
        }

        Assert-CleanSourceCheckout -Path $sourceRoot

        $companionEdition = $Edition.ToLowerInvariant()
        $companionBuildRoot = Join-Path $packageRoot 'companion-build'
        New-Item -ItemType Directory -Path $companionBuildRoot -Force | Out-Null
        $companionPublishArguments = @(
            'publish',
            $companionProject,
            '--configuration', 'Release',
            '--runtime', 'win-x64',
            '--self-contained', 'true',
            '--output', $companionBuildRoot,
            '-p:PublishSingleFile=true',
            '-p:EnableCompressionInSingleFile=true',
            '-p:IncludeNativeLibrariesForSelfExtract=true',
            "-p:CompanionEdition=$companionEdition",
            "-p:CompanionProtocolVersion=$moduleProtocolVersion",
            "-p:Version=$moduleVersion"
        )
        Write-Output "Publishing the matching $Module $Edition self-contained win-x64 companion."
        & $dotnet.Source @companionPublishArguments
        if ($LASTEXITCODE -ne 0) {
            throw "AI Gaming companion publish failed with exit code $LASTEXITCODE. Workshop upload stopped."
        }

        $companionSourceExe = Join-Path $companionBuildRoot 'AIGaming.Companion.exe'
        if (-not (Test-Path -LiteralPath $companionSourceExe -PathType Leaf)) {
            throw "Expected self-contained companion was not produced: $companionSourceExe"
        }
        $companionHash = (Get-FileHash -LiteralPath $companionSourceExe -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($companionHash -notmatch '^[0-9a-f]{64}$') {
            throw 'The AI Gaming companion produced an invalid SHA-256 value. Workshop upload stopped.'
        }

        & $dotnet.Source build $project --configuration Release --no-incremental `
            "-p:DevTools=$devTools" `
            "-p:CSInstallPath=$gameRoot" `
            "-p:CompanionEdition=$companionEdition" `
            "-p:CompanionVersion=$moduleVersion" `
            "-p:CompanionProtocolVersion=$moduleProtocolVersion" `
            "-p:CompanionSha256=$companionHash"
        if ($LASTEXITCODE -ne 0) {
            throw "AI Gaming build failed with exit code $LASTEXITCODE."
        }

        Assert-CleanSourceCheckout -Path $sourceRoot
        if (-not (Test-Path -LiteralPath $outputDll -PathType Leaf)) {
            throw "Expected AI Gaming DLL was not produced: $outputDll"
        }
        $moduleBuildInfo = [IO.File]::ReadAllBytes($outputDll)
        $hashAscii = [Text.Encoding]::ASCII.GetBytes($companionHash)
        $hashUnicode = [Text.Encoding]::Unicode.GetBytes($companionHash)
        if (-not (Test-ByteSequence -Bytes $moduleBuildInfo -Pattern $hashAscii) -and
            -not (Test-ByteSequence -Bytes $moduleBuildInfo -Pattern $hashUnicode)) {
            throw 'The AI Gaming DLL does not contain the expected companion SHA-256. Workshop upload stopped.'
        }

        Write-Output "Pinned companion SHA-256 into the matching $Module $Edition module build: $companionHash"
    }
}

if (-not (Test-Path -LiteralPath $outputDll -PathType Leaf)) {
    throw "Expected DLL was not produced: $outputDll"
}

if ($devToolsMarkers.ContainsKey($Module)) {
    $marker = $devToolsMarkers[$Module]
    $hasDevTools = [Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($outputDll)).Contains($marker)
    if ($Edition -eq 'Release' -and $hasDevTools) {
        throw "Release build of $Module contains the DevTools type $marker. Upload stopped."
    }
    if ($Edition -eq 'Development' -and -not $hasDevTools) {
        throw "Development build of $Module is missing the DevTools type $marker. Upload stopped."
    }
}
Write-Output "Built $Module $Edition with DevTools=$devTools."

$packageDll = Join-Path $contentRoot (Split-Path -Leaf $outputDll)
Copy-Item -LiteralPath $outputDll -Destination $packageDll
$hash = (Get-FileHash -LiteralPath $packageDll -Algorithm SHA256).Hash
Write-Output "Prepared $Module $Edition Workshop content: $(Split-Path -Leaf $packageDll), SHA-256 $hash"

if ($Module -eq 'AI_Gaming') {
    $packageCompanionDirectory = Join-Path $contentRoot 'Companion'
    New-Item -ItemType Directory -Path $packageCompanionDirectory -Force | Out-Null
    $packageCompanionExe = Join-Path $packageCompanionDirectory 'AIGaming.Companion.exe'
    Copy-Item -LiteralPath $companionSourceExe -Destination $packageCompanionExe
    $packagedCompanionHash = (Get-FileHash -LiteralPath $packageCompanionExe -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($packagedCompanionHash -cne $companionHash) {
        throw 'The packaged AI Gaming companion SHA-256 differs from the value pinned in its module build.'
    }
    Write-Output "Prepared $Module $Edition companion: Companion\AIGaming.Companion.exe, SHA-256 $packagedCompanionHash"
}

$workshopTitle = switch ("$Module/$Edition") {
    'AI_Improve/Release' { 'AI Improve: Release Version' }
    'AI_Improve/Development' { 'AI Improve: Development Version' }
    'Automated_Public_Transport_Planning/Release' { 'Automated Public Transport Planning: Release Version' }
    'Automated_Public_Transport_Planning/Development' { 'Automated Public Transport Planning: Development Version' }
    'AI_Gaming/Release' { 'AI Gaming: Release Version' }
    'AI_Gaming/Development' { 'AI Gaming: Development Version' }
    default { throw 'Unsupported module and edition combination.' }
}

function ConvertTo-VdfValue {
    param([Parameter(Mandatory = $true)][string]$Value)
    if ($Value.Contains([char]13) -or $Value.Contains([char]10)) {
        throw 'Workshop VDF values must not contain line breaks.'
    }
    return $Value.Replace('\', '\\').Replace('"', '\"')
}

# The Workshop description lives on the mod branch (Workshop/<Edition>.txt, Steam BBCode) so each
# update ships the text that matches its code. A missing file leaves the current description as it
# is. Line breaks are written literally inside the quoted VDF value; double quotes and backslashes
# are refused rather than escaped, because whether SteamCMD unescapes them is not verified yet.
$descriptionPath = Join-Path $sourceRoot "Workshop\$Edition.txt"
$description = $null
if (Test-Path -LiteralPath $descriptionPath -PathType Leaf) {
    $description = [IO.File]::ReadAllText($descriptionPath, [Text.Encoding]::UTF8).Replace("`r`n", "`n").Trim()
    if ($description.Length -eq 0) {
        throw "Workshop description file is empty: Workshop\$Edition.txt. Delete it to keep the current description, or add text."
    }
    if ($description.Contains('"') -or $description.Contains('\')) {
        throw "Workshop\$Edition.txt must not contain double quotes or backslashes; use typographic quotes instead."
    }
    if ($description.Length -gt 8000) {
        throw "Workshop\$Edition.txt is $($description.Length) characters; Steam allows at most 8000."
    }
}

if ($Module -eq 'AI_Gaming') {
    if ($null -eq $description) {
        throw "AI Gaming requires Workshop\$Edition.txt so the companion notice can be included."
    }

    $legacyStatement = switch ($Edition) {
        'Development' { 'The current build is an early preview: it does not control the game, makes no network connections and asks for no API keys. Future developer tools will appear in this version first.' }
        'Release' { 'It does not control the game yet, makes no network connections and asks for no API keys.' }
        default { throw "Unsupported AI Gaming edition: $Edition" }
    }
    if (-not $description.Contains($legacyStatement)) {
        throw "AI Gaming Workshop\$Edition.txt needs review before upload; the expected early-preview statement was not found."
    }

    $updatedStatement = "Version $moduleVersion can send selected read-only city fields and your question to Ollama Local at http://localhost:11434 through the optional companion after a separate preview and confirmation. Replies are advice only; AI Gaming does not control the game, connect to cloud providers, or ask for API keys. Ollama's own cloud settings may affect data routing."
    $description = $description.Replace($legacyStatement, $updatedStatement)

    $sourceCommit = & git -C $sourceRoot rev-parse --verify HEAD
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not identify the AI Gaming source commit for its Workshop companion notice.'
    }
    $sourceCommit = $sourceCommit.Trim()
    if ($sourceCommit -notmatch '^[0-9a-fA-F]{40}$') {
        throw 'The AI Gaming source commit for the Workshop companion notice is not a full commit SHA.'
    }

    $description += "`n`n[h2]Unsigned companion[/h2]`nThe optional Windows x64 companion executable included in this item is unsigned. It starts only after request confirmation and when its setting is enabled; permission defaults off and resets after the game restarts.`nCompanion SHA-256: $companionHash`nAI Gaming source commit: $sourceCommit"

    if ($description.Contains('"') -or $description.Contains('\')) {
        throw "AI Gaming Workshop\$Edition.txt companion notice contains a character that cannot be safely written to the Workshop VDF."
    }
    if ($description.Length -gt 8000) {
        throw "AI Gaming Workshop\$Edition.txt with companion details is $($description.Length) characters; Steam allows at most 8000."
    }
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
if ($null -ne $description) {
    $vdfLines += '    "description" "' + $description.Replace("`n", [Environment]::NewLine) + '"'
}
$vdfLines += '}'
[IO.File]::WriteAllText($vdfPath, ($vdfLines -join [Environment]::NewLine), [Text.UTF8Encoding]::new($false))

Write-Output "Target: $workshopTitle (existing item $PublishedFileId)"
if ($null -ne $description) {
    Write-Output "Description: updated from Workshop\$Edition.txt ($($description.Length) characters)."
} else {
    Write-Output "Description: no Workshop\$Edition.txt on the branch; the current description stays unchanged."
}
Write-Output 'Visibility and tags are omitted from the VDF and remain unchanged.'
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
# Updated by: OpenAI-Codex.
