---
title: Automated Public Transport Planning
description: "What the 0.2.0 early preview does today, how to use its diagnostics, and what is coming."
---

Automated Public Transport Planning will plan bus lines from your city's real demand. It will show every proposed line as a preview and build nothing until you accept it. It currently covers regular city buses only; other public transport types will be added one at a time later. Version **0.2.0** is an early preview that lays the groundwork.

## What 0.2.0 does

- **Surveys your city when it loads.** It reads roads, stops and the transit network in the background. It is read-only: nothing in your city or save is created, changed or removed.
- **Diagnostics in the mod options.** Each button writes its result to the game log:

| Button | What it reports |
| --- | --- |
| Write a test line to the log | Confirms the mod is loaded and responding. |
| List bus prefabs in detail | The bus models available in your game. |
| Measure planning costs | How long planning work would take on your city. Causes a brief pause. |
| Capture a world snapshot | The roads and stops the planner would work with. |
| Build the route graph | The network of possible connections between stops. |

- **Language selector.** 11 languages, switchable in the mod options without restarting.

## What it does not do yet

The Release Version does not create, change or suggest bus lines, and no transport type other than city buses is planned yet. Automatic planning is in development; see the [roadmap](/Cities_Skylines_1_Modpack/wiki/project/roadmap/).

## Development Version

The Development Version is a public test build. Back up your save before using it.

- **Planning preview (being tested).** The *Generate a read-only planning preview* button estimates city-bus lines from your city's demand and lists them with their scores. It does not create or change any line. The estimates are not yet checked against the game's own pathfinding.
- **Save round-trip test.** It can **create a test bus line in the loaded city**. The create button must be switched on first, and every click asks you to confirm and reminds you to back up. You can delete the test line with the game's own transport tools, but changes to the simulation while it ran cannot be undone. Removing lines from the mod is disabled. Only use it on a copy of your save.

## Helping test

Run the diagnostics on cities of different sizes and include the log output when you [report results or problems](https://github.com/SpaceSquare640/Cities_Skylines_1_Modpack/issues/new/choose). This helps tune the planner before it arrives.
