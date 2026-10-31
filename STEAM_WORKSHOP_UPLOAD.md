# Steam Workshop Upload Workflows

These workflows build and submit updates to the six existing Cities: Skylines Workshop items: a Release Version and a Development Version for each of AI Improve, Automated Public Transport Planning and AI Gaming. They never create new items. Each item was created once with the Mod Uploader to obtain its ID; every later update, including the preview image and description, goes through these workflows.

## Release and Development

| | Development Version | Release Version |
| --- | --- | --- |
| Build | Always `DevTools=true` (testing panels and probes included) | Always `DevTools=false` |
| When | Automatically after each push to the module branch, or manually | Manually only, after approval in the `workshop-release` environment |
| Source | The pushed commit, or the `source_ref` input | The `source_ref` input; use the commit already tested on the Development item |
| Change note | Commit hash and subject (automatic runs) | Entered when dispatching |

`tools/steam-workshop/Upload-Mod.ps1` decides `DevTools` from the edition; no input can change it. After building, it checks the DLL for a type that exists only in DevTools builds (`DevTriggerExtension` for AI Improve, `SaveRoundTripSpike` for Transport) and stops if a Release DLL contains it or a Development DLL lacks it. AI Gaming has no DevTools content yet.

Development builds contain tools that can change a city: AI Improve's hotkey-armed dev panel and LateMergeProbe (which deliberately alters vehicle paths), and Transport's save round-trip spike (behind an arming checkbox). Keep Development items hidden or limited to trusted testers, and say so in their descriptions.

## Registered Workshop item IDs

Each module and edition is pinned to one item in `Upload-Mod.ps1`. An upload whose ID does not match, or whose item has no registered ID, stops before anything is built or sent. Every workflow prefills its registered ID.

| Workshop item | PublishedFileId | Visibility |
| --- | --- | --- |
| AI Improve: Release Version | 3782858610 | Public |
| AI Improve: Development Version | 3823575900 | Hidden |
| Automated Public Transport Planning: Release Version | 3823739383 | Hidden |
| Automated Public Transport Planning: Development Version | 3823739653 | Hidden |
| AI Gaming: Release Version | 3823740106 | Hidden |
| AI Gaming: Development Version | 3823740277 | Hidden |

To add an item, register its ID in `$registeredItemIds`, add the workflow default, and update this table.

## Repository switches

| Repository setting | Effect | Status (2026-10-21) |
| --- | --- | --- |
| Variable `WORKSHOP_DEV_AUTO_UPLOAD` = `true` | Pushes to a module branch dispatch its Development upload (`development-workshop-dispatch.yml` on each module branch) | On |
| Environment `workshop-release`: required reviewer SpaceSquare640 (Kingsley), deployments only from `Modpack_Website` | Release jobs wait for approval | On |
| Variable `WORKSHOP_RELEASE_APPROVAL_READY` = `true` | Release workflows run at all; without it they stop with an error | On |

To pause automation, set either variable to `false`. Uploads only run while the self-hosted runner is online; start it with `Start Actions Runner.lnk` in the local Modpack folder and keep its window open. The first automated upload (AI Improve: Development Version, run 39518052371) succeeded on 2026-10-21.

## Required local runner

The mods compile against Cities: Skylines managed DLLs, and the upload needs SteamCMD with a saved login. GitHub-hosted runners have neither, so the upload jobs target a Windows self-hosted runner with the labels self-hosted, Windows, X64 and cs1-workshop. The Development dispatch jobs on the module branches run on GitHub-hosted runners and only call the GitHub API.

1. Install a Windows GitHub Actions runner for this repository with the cs1-workshop label. Do not add pull request triggers to any job that uses it.
2. Provide pwsh, the .NET SDK (AI Improve, AI Gaming) and Visual Studio or Build Tools with MSBuild (Transport; found through vswhere when not on PATH). Set CITIES_SKYLINES_DIR to the game folder.
3. Set MODPACK_ROOT to the local Cities_Skyline_1_Modpack folder. Preview images are read from `Resource/Icon/Uses_icon/JPG/` and must be at most 1 MB (Steam's preview limit). Each module has a Release and a Development image (for example `Cities_Skylines_1_AI_Improve_Release_Version.jpg`), converted from the PNG originals in `Uses_icon/PNG/`, which are larger and are not used. Each workflow prefills the image for its edition. Preview images stay local and are not committed.
4. Install SteamCMD in a persistent folder; set STEAMCMD_EXE to its executable and STEAMCMD_USERNAME to the account name. Under the same Windows account that runs the runner, complete one interactive SteamCMD login with Steam Guard. Keep SteamCMD's `config/config.vdf`; never commit or upload it. If the saved login expires, uploads fail until it is renewed interactively.
5. Builds never copy into the runner account's own Mods folder: AI Improve's copy goes to the job's package folder and Transport's deploy step is disabled with an empty ModsDir.

GitHub warns that self-hosted runners attached to public repositories need extra care because untrusted code can be dangerous. These jobs run only on manual dispatch or on dispatches from pushes by people with write access, never on pull requests. Use a dedicated Windows account if possible and keep write access limited to trusted maintainers.

## Run an update manually

1. Open the repository's Actions tab and choose one of the six version workflows.
2. Keep the prefilled ID and preview filename; set `source_ref` to the commit to build and add a change note if wanted. Leave the confirmation on DO NOT UPLOAD to stop safely.
3. Choose UPLOAD EXISTING ITEM to submit. Release runs then wait for approval in the `workshop-release` environment.
4. The workflow sets the title to the exact version name above. Visibility and tags are never changed.
5. After SteamCMD completes, check its Workshop build log and the item page. A successful process exit alone does not prove the item is correct.

The Workshop App ID is 255710 (Cities: Skylines). The VDF updates the item ID, title, content folder, preview image, optional change note and optional description.

## Workshop descriptions

Each mod branch may contain `Workshop/Release.txt` and `Workshop/Development.txt` in Steam BBCode. When the file for the uploaded edition exists, its text replaces the Workshop description; when it is absent, the current description stays unchanged. The upload stops if the file is empty, longer than 8,000 characters, or contains double quotes or backslashes (use typographic quotes instead). Line breaks are written literally into the VDF; confirm the first description update on a Development item before relying on it for a Release item.

Steam's Workshop guide describes the SteamCMD Workshop route as intended for testing and notes that it requires Steam credentials. These workflows keep the password and Steam Guard code out of GitHub and use a persistent local SteamCMD login.

## References

- Steam Workshop Implementation Guide: https://partner.steamgames.com/doc/features/workshop/implementation?l=english
- SteamPipe CI/CD login guidance: https://partner.steamgames.com/doc/sdk/uploading?l=english
- Cities: Skylines on Steam: https://store.steampowered.com/app/255710/Cities_Skylines/
- GitHub: Manually running a workflow: https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow
- GitHub: Self-hosted runner security guidance: https://docs.github.com/en/actions/how-tos/manage-runners/self-hosted-runners/manage-access

Author: OpenAI-Codex.
Modified by: Anthropic-Claude_Code.

## Account rename — 2026-10-31

The current repository is https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack. The dated status table above retains the reviewer name recorded on 2026-10-21; the current environment reviewer configuration has not been verified.

Author: OpenAI-Codex_Responsible: Modpack_Website
