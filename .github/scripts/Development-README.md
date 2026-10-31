# APTP Development build

Experimental Development prototype: route creation has not passed subscribed-game acceptance. Back up saves and use a disposable copy. Development tools can damage saves. Never install alongside the stable Release/Workshop APTP copy.

## Installation and Dev Tool

Close the game and disable/unsubscribe the stable APTP copy. Create `%LOCALAPPDATA%/Colossal Order/Cities_Skylines/Addons/Mods/APTPDevelopment` on Windows and place only `AutomatedPublicTransportPlanning.dll` there. Do not install game reference assemblies or System.Core.dll.

Start the game, enable the APTP Development entry in Content Manager > Mods, and open its Options page. Dev Tool controls are compiled in with `DevTools=true`; no additional DLL is needed. On a disposable save, request a planning preview under diagnostics, inspect and select a proposal, arm route creation using its checkbox, and explicitly confirm each creation. Proposals do not guarantee connected or runnable routes.

## Uninstall

Close the game and remove this Development copy from the Mods folder. Removing the DLL does not reverse saved city changes; restore your save backup where necessary. Re-enable the stable Workshop copy only after removing this copy.

## License and provenance

Copyright Kingsley. The original PolyForm Noncommercial License 1.0.0 is attached unchanged as LICENSE. See build-info.txt and SHA256SUMS for provenance and integrity. No game references or PDBs are distributed. Workshop links are project-source records, not a live Steam status check.

Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
