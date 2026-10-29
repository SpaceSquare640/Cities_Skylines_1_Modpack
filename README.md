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
