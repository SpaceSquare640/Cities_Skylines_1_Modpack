# AI Improve Development build

This prerelease contains experimental development tools. It can damage saves.
Back up your saves and use a disposable test city. Do not install this build
alongside the AI Improve Release or Development Workshop edition or another local
copy. Cloud compilation and file checks do not establish in-game acceptance.

## Install on Windows

1. Exit Cities: Skylines completely. Disable any existing AI Improve copy in
   Content Manager and ensure only this edition will be enabled on the next start.
2. Subscribe to and enable the Harmony Workshop dependency:
   https://steamcommunity.com/sharedfiles/filedetails/?id=2040656402
3. Download `AIImprove.dll` and `CitiesHarmony.API.dll` from the same prerelease.
   Check them against `SHA256SUMS` using `Get-FileHash -Algorithm SHA256`.
4. Create `%LOCALAPPDATA%\Colossal Order\Cities_Skylines\Addons\Mods\AIImproveDevelopment`
   and place both DLLs inside. Keep the accompanying license and notices with them.
5. Start the game, enable AI_Improve in Content Manager, and load a disposable city.

## Development tools

This build includes DevTool automatically (`DevTools=true`); no separate settings
switch is required. In a loaded city, Ctrl+Shift+R resets diagnostic trackers;
Ctrl+Shift+T toggles the thunderstorm override. These are development controls,
not ordinary gameplay recommendations. The separate path-altering LateMergeProbe
is not enabled in this build. The in-game settings remain available through the
mod's Content Manager options.

## Uninstall or return to Workshop

Exit the game completely, then remove the dedicated `AIImproveDevelopment` folder
you created. Restart and enable only the intended Workshop edition. Do not delete
your saves. Retain backups; reverting the DLL does not undo changes already saved.

## Included files and provenance

`LICENSE` contains the unchanged AI Improve PolyForm Noncommercial 1.0.0 terms.
`CitiesHarmony-LICENSE.txt` and `THIRD-PARTY-NOTICES.md` cover the redistributed API.
`build-info.json` records the exact source SHA, shared reference SHA, run and flags.
`Steam_Workshop_Link.txt` lists stable editions from project records; current Steam
visibility was not verified for this release. No game assemblies, System DLLs,
0Harmony.dll or public ZIP archive are included. GitHub's automatically generated
source archives are source snapshots, not the binary installation package.

Signature: OpenAI-Codex_Responsible: AI_Improve
