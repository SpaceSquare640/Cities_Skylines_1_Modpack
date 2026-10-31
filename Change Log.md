# Change Log

Copyright © Kingsley.

## 2026-10-28 — Development route refusal diagnostics

- Added eligibility and build-index details to route creation refusal logs while retaining the existing safety checks.
- Author: OpenAI-Codex.

## 2026-10-28 — Development description

- Updated the Development content-manager entry to identify DevTool and warn that it can damage saves.
- Author: OpenAI-Codex.

## 2026-10-27 — Development route proposal creation

- Added a Development-only flow to select and create one eligible planning proposal. It remains disarmed by default and requires a fresh confirmation for each creation.
- Stop proposals now preserve the captured segment build index, world direction, selected bus lane, position, and platform setting. Creation revalidates these values, the bus prefab, city build index, stop anchorability, and line limits on the simulation thread.
- Added a 3–6 distinct-stop-lane eligibility rule and asynchronous checks for stop connections and `PathFailed` flags on segments adjacent to stops.
- The planner still does not model lane-level turns or TM:PE restrictions; this remains a Development prototype.
- Author: OpenAI-Codex.

## 2026-10-31 — Independent cloud workflow readiness

- Disabled automatic Development Workshop dispatch.
- Added a manual, intentionally failing dependency-readiness gate and documented the unresolved game-reference source, independent branch ownership, and direct DLL release direction.
- No compilation, public release, or Workshop upload is enabled by this change.
- Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
