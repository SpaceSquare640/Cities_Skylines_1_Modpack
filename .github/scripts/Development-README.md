# Automated Public Transport Planning — Development Build

## Module Overview

Automated Public Transport Planning provides city-data capture, demand analysis and ordinary in-city bus planning previews. Route creation is a Development prototype; connected or runnable routes and save/reload are not established by a successful build.

## Build Information and Changes

This build uses Configuration=Release with DevTools=true. The accompanying build-info.txt identifies the exact source SHA, fixed shared-reference SHA, workflow run and attempt. This development iteration adds cloud execution of the existing pure-data planning checks and the approved full-name release format. The checks use a cloud CLR v4 host, not the game's Mono runtime. It does not implement editing existing bus routes.

## Development Build Risk Warning

Back up saves and use a disposable copy. Development tools can damage saves. Never install alongside the stable Release/Workshop copy. Successful compilation and pure-data checks are not subscribed-game acceptance.

## Downloaded Files and Their Purpose

| File | Purpose |
| --- | --- |
| AutomatedPublicTransportPlanning.dll | Development mod |
| README.md | Installation, risks and usage |
| LICENSE | Original PolyForm Noncommercial License 1.0.0 |
| build-info.txt | Exact source and build provenance |
| Steam_Workshop_Link.txt | Stable item links recorded in source |
| SHA256SUMS | SHA-256 hashes of the other five assets |

## Installation Instructions

1. Close the game and disable/unsubscribe the stable Automated Public Transport Planning copy.
2. Create `%LOCALAPPDATA%/Colossal Order/Cities_Skylines/Addons/Mods/APTPDevelopment` on Windows.
3. Place only AutomatedPublicTransportPlanning.dll there. Do not add game assemblies, System.Core.dll or PDBs.
4. Start the game and enable the Development entry in Content Manager > Mods. Use a disposable save copy.

## Dev Tool Enablement and Usage

Open the module's Options page. Dev Tool controls are compiled in; no additional DLL is required. Under Diagnostics, select "Generate a read-only planning preview". Inspect and select a proposal, tick "Arm route creation; every click still asks for confirmation", then explicitly confirm each creation. Arming resets when the settings page opens. Do not assume proposals guarantee connected or runnable routes.

## Uninstallation and Return to Stable Release

Close the game and remove this Development copy from the Mods folder. Removing the DLL does not reverse saved city changes; restore your backup where necessary. Re-enable the stable Workshop copy only after this Development copy is removed.

## License, Dependencies, and Integrity Verification

Copyright Kingsley. LICENSE contains the unchanged PolyForm Noncommercial License 1.0.0. No game references, PDBs or public ZIP assets are distributed. The mod requires a compatible Cities: Skylines installation. Workshop links are source records, not a live Steam visibility check. Use SHA256SUMS to verify downloaded files, for example `Get-FileHash AutomatedPublicTransportPlanning.dll -Algorithm SHA256` in PowerShell, and compare the exact digest.

Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
