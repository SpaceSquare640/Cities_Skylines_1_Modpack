# AI Gaming

Visit the [AI Gaming website page](https://spacesquare640.github.io/Cities_Skylines_1_Modpack/ai-gaming/) for project information, documentation, and updates.

## In-game language

The settings UI defaults to English regardless of the game's language. Players can select English, Traditional Chinese, Simplified Chinese, Japanese, or Korean in the AI Gaming settings. The preference is saved and changes apply while the settings page is open.

## DLC context

Before each local AI request, AI Gaming reads the game's DLC catalog and includes the enum identifiers that the game API reports as installed and available. The complete list appears in the request preview and is sent to local Ollama only after the separate per-request confirmation. If the platform DLC state is not ready, AI Gaming does not prepare or send the request. The list describes availability on this installation; it does not prove that a DLC is active in the current save.

## Additional city data

The request preview can also include utility status (electricity, water, sewage, and garbage), built building counts and areas by zoning use, game-reported traffic-flow and public-transport statistics, and service indicators. These four groups are selectable, start checked, and can be excluded before preparing a request. Physical units and statistic time windows are not verified. Zoning reports built buildings, not vacant zoned cells; traffic flow is not a congestion percentage; police and healthcare values are citywide workload or capacity, not geographic coverage. Fire coverage is an average game byte over created network segments, and is unavailable when there are no segments to sample. Only the reviewed preview is sent after the separate confirmation.

## Ollama response body limit

The complete raw HTTP response body from Ollama `/api/chat`, including its JSON envelope, has no application-level size limit by default (`0`). Players may enter a whole-number limit from 1 to 2047 MiB in the settings. If a configured limit is exceeded, AI Gaming discards the entire response before parsing it and does not display a partial answer. This byte-size limit does not set the model's generated-token limit or cap total process memory; parsing may require additional memory. With the default unlimited setting, normal memory and request-time constraints still apply. The `/api/tags` model-list response remains limited to 1 MiB.

## Diagnostic logs

Release records warnings and errors only; Development also records lifecycle, discovery, request status, and request elapsed time. Diagnostic logs do not include prompts, model responses, city values, or credentials. The manual city snapshot action remains separate and writes to the game log.

On Windows, daily UTC diagnostic files are stored under the local application data directory in `SpaceSquare640/AI Gaming/Logs/release` or `Logs/development`. A background check runs at activation if due and then every 48 hours; it removes only marked, correctly named, non-current diagnostic files last modified more than 48 hours ago. Depending on the check schedule, files may remain for nearly 96 hours. Directory and file handles are verified before deletion; files that cannot be safely verified are retained. On other operating systems, or when safe file access fails, diagnostics fall back to the game log and automatic deletion is unavailable. The manual city snapshot remains in the game log.

An invalid cleanup timestamp is treated as due. If a damaged state file still has the expected ownership marker, it can be updated; an unmarked file is left untouched. In that case the current process schedules its next check after 48 hours, but restarting the mod can trigger an earlier check. The 48-hour minimum age for deleting a diagnostic file still applies.

Author: Kingsley


Source-available software (noncommercial).
Code license: PolyForm Noncommercial License 1.0.0. See LICENSE.
Website code: PolyForm Noncommercial License 1.0.0.
Website content (text and images): CC BY-NC-SA 4.0.
Copyright: Kingsley.

<!-- Updated by OpenAI-Codex_relicense_ai_gaming on 2026-10-27. -->
<!-- Updated by OpenAI-Codex on 2026-10-29. -->
<!-- Updated by OpenAI-Codex_Responsible: AI Gaming on 2026-10-29. -->
<!-- Updated by OpenAI-Codex on 2026-10-29: optional city data groups. -->
<!-- Updated by OpenAI-Codex_Responsible: AI Gaming on 2026-10-29. -->

## Cloud validation and release status

The AI Gaming cloud baseline checks required files and project XML metadata on GitHub-hosted Ubuntu. It does not compile the mod or Companion, run gameplay tests, or publish a release. Game reference provisioning remains unresolved for ICities.dll, ColossalManaged.dll, Assembly-CSharp.dll, and UnityEngine.dll. The mod targets .NET Framework 3.5; its paired Companion targets .NET 10.

The legacy Development Workshop dispatch job is disabled unconditionally. Future releases are intended to provide the mod DLL and matching Companion files directly as this module's GitHub Release assets, without ZIP packaging. Automatic public releases remain unconfigured pending dependency provisioning and build validation.

<!-- Signature: OpenAI-Codex_Responsible: AI_Gaming -->

### Diagnostic compilation

The separately scoped cloud compile diagnostic runs on GitHub-hosted Windows when its workflow file changes on AI_Gaming, or by manual dispatch. It publishes a self-contained win-x64 single-file development Companion with .NET 10, verifies matching project version and protocol metadata, computes its SHA256, and passes that pairing to a real DevTools mod build. An empty game root deliberately exposes the unresolved game references through the build tool's original errors. A failed build remains failed. No artifacts, archives, releases, or Workshop uploads are produced.

<!-- Signature: OpenAI-Codex_Responsible: AI_Gaming -->

The compile diagnostic now restores the approved encrypted game references from fixed dependency commit `7b8a5e7c10f2db84388e8dd8af2084f239957b13`. Only the restore step receives the decryption secret; plaintext stays under the cloud runner temporary directory. Successful compilation must also produce a nonempty mod DLL and paired Companion. This workflow still publishes no assets and does not establish gameplay acceptance.
<!-- Signature: OpenAI-Codex_Responsible: AI_Gaming -->

## Development prereleases

The former diagnostic workflow now builds and publishes Development prereleases on every AI_Gaming push and manual dispatch, using approved encrypted references. A separate publisher uploads only individually allowlisted DLL, paired self-contained Companion, installation instructions, licenses, checksums and build information after draft verification. See [Development installation](docs/Development-Installation.md) and [automation changes](docs/Development-Release-Changes.md). The earlier diagnostic status descriptions above are historical; public prerelease automation supersedes them. The baseline remains a separate metadata check. No Workshop upload is dispatched.
<!-- Signature: OpenAI-Codex_Responsible: AI_Gaming -->
