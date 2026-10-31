# Automated Public Transport Planning

Visit the [Automated Public Transport Planning website page](https://spacesquare640.github.io/Cities_Skylines_1_Modpack/automated-public-transport-planning/) for project information, documentation, and updates.

Author: Kingsley

## Licensing

- This app is source-available; its source code is licensed under the PolyForm Noncommercial License 1.0.0. See LICENSE.
- Website source code: PolyForm Noncommercial License 1.0.0.
- Website content, including text and images: Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International (CC BY-NC-SA 4.0).
- Copyright Kingsley.

<!-- Prepared by OpenAI-Codex. -->

## Planning engine checks

The .NET Framework 3.5 check runner uses no third-party test framework. It compiles the production pure-data planning sources against the game assemblies configured by `Local.props` or `CITIES_SKYLINES_DIR`. If MSBuild is not on `PATH`, locate it with the Visual Studio Installer `vswhere.exe`. From this directory, run:

```text
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$msbuild = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
& $msbuild Tests\PlanningEngineChecks.csproj /t:Rebuild /p:Configuration=Release /p:ModsDir=
.\bin\PlanningEngineChecks\Release\PlanningEngineChecks.exe
```

The checks cover citizen-unit-to-people conversion, planning snapshot copy isolation, the stale-snapshot warning, the 3–6 distinct-lane creation rule, concrete proposal stop data, people totals and route-segment coverage, normalized endpoint pairing, missing directed return paths, and the current endpoint/pair search caps. Build output stays under `bin\`; the test project has no game Mods deployment target.

The Development build also offers selected proposal creation. It records the actual bus lane and world direction selected for each stop, then revalidates the snapshot build index, bus prefab, segment build indices, lane, stop position, anchorability, and line limits on the simulation thread. Creation is hidden from Release builds, starts disarmed, requires confirmation on each click, and accepts only proposals with 3–6 distinct stop lanes. A later simulation-frame check inspects stop-lane connections, the `LineNotConnected` flag, and `PathFailed` flags on segments adjacent to stops; it does not verify each route's full path.

If creation is refused for stop eligibility or build-index changes, the Development log records the individual conditions and the capture and current indices. The global build index is diagnostic during capture but is a strict creation guard. <!-- OpenAI-Codex -->

The Development content-manager entry identifies DevTool and warns that the development version can damage saves. <!-- OpenAI-Codex -->

The route graph still treats junction-connected segments as connected and does not model lane-level turns or TM:PE restrictions. A generated proposal and its asynchronous checks are a Development prototype, not proof that every line can run in a live game.

<!-- Updated by OpenAI-Codex on 2026-10-26. -->

## Cloud build and release readiness

The `Automated_Public_Transport_Planning` branch owns this module's CI. The existing source-integrity workflow checks project metadata only; it does not compile the DLL or validate gameplay. The manual `transport-cloud-readiness.yml` workflow intentionally fails while an approved, authenticated source of matching game reference assemblies is unavailable. It does not accept a variable as proof that dependencies exist.

Required references are `mscorlib.dll`, `System.dll`, `System.Core.dll`, `System.Xml.dll`, `ICities.dll`, `Assembly-CSharp.dll`, `ColossalManaged.dll`, and `UnityEngine.dll`. No reference DLLs are committed or redistributed by this change. Cloud compilation, planning checks, and direct per-module GitHub Release DLL attachments remain blocked pending a dependency provisioning decision. Releases must provide the mod DLL and necessary permitted files directly, without ZIP archives.

Automatic Development Workshop dispatch is disabled regardless of `WORKSHOP_DEV_AUTO_UPLOAD`. Workshop upload is a separately authorized local operation; this branch must not dispatch the Website upload workflow. No automatic public release is enabled.

Signature: OpenAI-Codex_Responsible: Automated_Public_Transport_Planning
