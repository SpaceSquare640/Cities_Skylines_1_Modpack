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

The checks cover citizen-unit-to-people conversion, planning snapshot copy isolation, the stale-snapshot warning, people totals and route-segment coverage, normalized endpoint pairing, missing directed return paths, and the current endpoint/pair search caps. Build output stays under `bin\`; the test project has no game Mods deployment target.

<!-- Updated by OpenAI-Codex on 2026-10-26. -->
