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

The route graph still treats junction-connected segments as connected and does not model lane-level turns or TM:PE restrictions. A generated proposal and its asynchronous checks are a Development prototype, not proof that every line can run in a live game.

<!-- Updated by OpenAI-Codex on 2026-10-26. -->
