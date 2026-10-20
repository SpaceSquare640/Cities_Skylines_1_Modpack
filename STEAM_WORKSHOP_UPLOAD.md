# Steam Workshop Upload Workflows

These workflows prepare and submit updates to existing Cities: Skylines Workshop items. They do not create new items, run on pushes, or publish AI Gaming. Each module has separate Release Version and Development Version items.

## Current readiness

| Workshop item | Workflow | Status |
| --- | --- | --- |
| AI Improve: Release Version | AI Improve: Release Version upload | Builds the current AI_Improve branch HEAD with DevTools=false, then submits the DLL and selected preview image. Defaults to `Cities_Skylines_1_AI_Improve_Icon_Kingsley.png`. |
| AI Improve: Development Version | AI Improve: Development Version upload | Uses the same branch HEAD and safe build profile, but targets a separate Workshop item. Defaults to `Cities_Skylines_1_AI_Improve_Icon_Kingsley.png`. |
| Automated Public Transport Planning: Release Version | Automated Public Transport Planning: Release Version upload | Builds the current Automated_Public_Transport_Planning branch HEAD and submits the DLL and selected preview image. Defaults to `Cities_Skylines_1_Automated_Public_Transport_Planning_Icon_Kingsley.png`. |
| Automated Public Transport Planning: Development Version | Automated Public Transport Planning: Development Version upload | Uses the same branch HEAD and build configuration, but targets a separate Workshop item. Defaults to `Cities_Skylines_1_Automated_Public_Transport_Planning_Icon_Kingsley.png`. |
| AI Gaming: Release Version | AI Gaming: Release Version upload (blocked) | Intentionally stops because there is no buildable project or package definition. |
| AI Gaming: Development Version | AI Gaming: Development Version upload (blocked) | Intentionally stops because there is no buildable project or package definition. |

The Release and Development items currently differ by Workshop item ID and title only. Both build from the module branch HEAD. AI Improve uses DevTools=false for both because its project explicitly warns against uploading the DevTools build. Transport has no separate Release/Development build profile. Add a distinct build profile only after its package contents and behavior are defined.

The upload workflows live on the Modpack_Website default branch because GitHub requires a workflow_dispatch workflow to exist on the default branch. The source checkout remains pinned to each module's named branch and uses that branch's latest commit at dispatch time. Local edits must be pushed to the respective branch before a future GitHub workflow can use them.

## Required local runner

AI Improve and Transport need Cities: Skylines managed DLLs to compile. The upload also needs SteamCMD and a saved SteamCMD login. GitHub-hosted runners do not have those local game files or the persistent Steam login, so these four workflows target a Windows self-hosted runner with the labels self-hosted, Windows, X64, and cs1-workshop.

Before enabling the runner:

1. Install a Windows GitHub Actions runner for this repository and add the cs1-workshop label. Keep it dedicated to these manual upload jobs; do not add push or pull request triggers.
2. Make pwsh, dotnet, and msbuild.exe available on the runner. Install the Cities: Skylines game and set the machine environment variable CITIES_SKYLINES_DIR to its game folder.
3. Set MODPACK_ROOT to the local Cities_Skyline_1_Modpack folder. The preview filename entered at dispatch must exist under `Resource/Icon/Uses_icon/`. Workshop preview images stay in this local resource folder and are not copied into the repository. Each runnable workflow defaults to its mod's `_Kingsley.png` icon; the filename input remains editable if you replace or rename an icon. The AI Gaming icon is `Cities_Skylines_1_AI_Gaming_Icon_Kingsley.png`; AI Gaming uploads remain blocked until a buildable package exists.
4. Install SteamCMD in a persistent folder and set STEAMCMD_EXE to its executable and STEAMCMD_USERNAME to the account name. Using that same Windows account and SteamCMD folder, complete one interactive SteamCMD login and Steam Guard check before using the workflow. Preserve SteamCMD's local config/config.vdf between runs; never commit or upload it.
5. Restrict who can run Actions in the repository. GitHub requires write access to run workflow_dispatch; the upload jobs use read-only GitHub content permissions.

GitHub warns that self-hosted runners attached to public repositories need extra care because untrusted code can be dangerous. These workflows avoid running on pull requests or pushes, but a self-hosted runner still executes repository code. Use a dedicated Windows account and keep repository write access limited to trusted maintainers.

## Run an existing-item update

1. Open the repository's Actions tab and choose one of the six version workflows.
2. Enter the existing Workshop PublishedFileId, the preview image filename, and an optional change note. Leave the confirmation set to DO NOT UPLOAD to stop safely.
3. Before selecting UPLOAD EXISTING ITEM, check that the ID belongs to the displayed module and edition. The workflow sets the title to the exact version name shown above.
4. Development item visibility is not changed by the workflow. Set and verify the intended visibility in Steam before uploading a development build.
5. After SteamCMD completes, inspect its Workshop build log and the item page. A successful process exit alone is not sufficient confirmation that the item is visible and correct.

The Workshop App ID is 255710 (Cities: Skylines). Each runnable upload requires a non-zero existing PublishedFileId; missing IDs and AI Gaming uploads fail closed. The VDF updates the item ID, title, content folder, preview image, and optional change note. It omits visibility, description, and tags so they remain unchanged.

Steam's Workshop guide describes the SteamCMD Workshop route as intended for testing and notes that it requires Steam credentials. These workflows keep the password and Steam Guard code out of GitHub and use a persistent local SteamCMD login. Review each update manually before relying on it as a public release.

## References

- Steam Workshop Implementation Guide: https://partner.steamgames.com/doc/features/workshop/implementation?l=english
- SteamPipe CI/CD login guidance: https://partner.steamgames.com/doc/sdk/uploading?l=english
- Cities: Skylines on Steam: https://store.steampowered.com/app/255710/Cities_Skylines/
- GitHub: Manually running a workflow: https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow
- GitHub: Self-hosted runner security guidance: https://docs.github.com/en/actions/how-tos/manage-runners/self-hosted-runners/manage-access

Author: OpenAI-Codex.
