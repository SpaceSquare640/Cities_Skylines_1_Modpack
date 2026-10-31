# AI Improve — Development Build

## Module Overview

AI Improve adjusts traffic, citizen and service vehicle decision-making in
Cities: Skylines 1. This is an experimental Development edition with diagnostic
controls; compilation and release checks do not establish in-game acceptance.

## Build Information and Changes

<!-- BUILD_INFORMATION -->

This update unsubscribes shared-patch diagnostics when the mod is disabled and
restores the lane bottleneck diagnostic switch to false when resetting defaults.
Active project links now use the verified KingsleyHK0323 repository and Pages site.
These are code-level fixes; repeated enable/disable and settings UI behavior
still require subscribed-game validation. Full provenance is in `build-info.json`.

## Development Build Risk Warning

This prerelease can damage saves. Back up saves and use a disposable test city.
Do not enable it alongside the AI Improve Release or Development Workshop edition
or another local copy. Reverting DLLs does not undo changes already saved.

## Downloaded Files and Their Purpose

| File | Purpose |
| --- | --- |
| AIImprove.dll | Development mod with diagnostic controls |
| CitiesHarmony.API.dll | Required API; Harmony provider is installed separately |
| README.md | Installation, development tools, removal and risk instructions |
| LICENSE | Unchanged AI Improve PolyForm Noncommercial 1.0.0 terms |
| CitiesHarmony-LICENSE.txt | Complete upstream MIT license |
| THIRD-PARTY-NOTICES.md | API origin and independent third-party terms |
| Steam_Workshop_Link.txt | Stable editions recorded in project source |
| build-info.json | Exact source/reference SHAs, run and build flags |
| SHA256SUMS | SHA-256 checksums for the other eight files |

## Installation Instructions

1. Exit Cities: Skylines completely. Disable existing AI Improve copies in
   Content Manager and ensure only one edition will be enabled on the next start.
2. Subscribe to and enable the Harmony Workshop dependency:
   https://steamcommunity.com/sharedfiles/filedetails/?id=2040656402
3. Download `AIImprove.dll` and `CitiesHarmony.API.dll` from this same prerelease.
   Check them against `SHA256SUMS` using `Get-FileHash -Algorithm SHA256`.
4. Create `%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\AIImproveDevelopment`
   and place both DLLs inside. Keep the accompanying license and notices with them.
5. Start the game, enable AI_Improve in Content Manager, and load a disposable city.

## Dev Tool Enablement and Usage

DevTool is included automatically (`DevTools=true`); no separate settings switch
is required. In a loaded city, Ctrl+Shift+R resets diagnostic trackers and
Ctrl+Shift+T toggles the thunderstorm override. These are development controls,
not ordinary gameplay recommendations. The separate path-altering LateMergeProbe
is not enabled. The mod's settings remain accessible through Content Manager.

## Uninstallation and Return to Stable Release

Exit the game completely, then remove the dedicated `AIImproveDevelopment` folder
you created. Restart and enable only the intended Workshop edition. Keep your
saves and backups; reverting DLLs does not undo changes already saved.

## License, Dependencies, and Integrity Verification

AI Improve uses PolyForm Noncommercial 1.0.0, Copyright Kingsley; see `LICENSE`.
CitiesHarmony.API uses its independent MIT terms; see the included official
license and third-party notices. The Harmony Workshop provider remains required.
Compare each downloaded file's SHA-256 with `SHA256SUMS` before installation.
`Steam_Workshop_Link.txt` records stable edition links from project source;
current Steam visibility was not verified for this release. No game assemblies,
System DLLs, 0Harmony.dll or public ZIP asset are included. GitHub's automatic
source archives are source snapshots, not the binary installation package.

Signature: OpenAI-Codex_Responsible: AI_Improve
