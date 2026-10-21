---
title: AI Improve compatibility
description: "How AI Improve behaves next to other popular Cities: Skylines 1 mods."
---

AI Improve tries not to fight other mods. When another mod already handles a job well, AI Improve detects it and steps back.

| Mod | What AI Improve does |
| --- | --- |
| **Harmony** | Required. AI Improve uses it to change game behaviour. |
| **UnifiedUI** | Optional. Adds the [in-game panel](/Cities_Skylines_1_Modpack/wiki/ai-improve/in-game/). |
| **Improved Public Transport**, **Express Bus Services** | *Shorten empty stop dwell* and *Unbunch vehicles on a line* stand down while either is installed, because those mods already decide when vehicles depart. |
| **Transfer Manager CE** | When its fire dispatch is active, AI Improve lets it choose which fire a vehicle attends and only keeps the per-building responder cap. |
| **Traffic Manager: President Edition (TM:PE)** | Detected. TM:PE replaces the game's pathfinding. AI Improve's emergency-vehicle cost adjustment for TM:PE's pathfinder currently fails to load, so it is inactive while TM:PE is enabled; a fix is being tested. The optional mid-route emergency reroute is not affected. |
| **Real Time** | Detected. The two mods touch the same fire-dispatch step, but Real Time only does its own bookkeeping there, so they do not interfere. |
| **SingleTrainTrackAI**, **Reversible Tram AI** | Detected. AI Improve does not try to do their jobs. |

AI Improve does not change how many passengers any vehicle carries.

## Release and Development together

Do not enable both versions of AI Improve. They are the same mod and will conflict.

## Reporting a conflict

If you see a problem with another mod, open an [issue](https://github.com/SpaceSquare640/Cities_Skylines_1_Modpack/issues/new/choose) with your game version, full mod list, the AI Improve feature involved, steps to reproduce and the relevant part of the output log (see the [FAQ](/Cities_Skylines_1_Modpack/wiki/ai-improve/faq/)).
