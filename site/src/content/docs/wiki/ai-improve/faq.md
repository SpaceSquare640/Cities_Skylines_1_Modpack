---
title: AI Improve FAQ
description: "Common questions about AI Improve, and how to report a problem."
---

## Can I use multiple AI Improve editions together?

No. Enable only one copy, whether it comes from Steam Workshop or a manually installed GitHub Development build. Back up saves and follow the [Development installation guide](/Cities_Skylines_1_Modpack/wiki/development-downloads/) before switching versions.

## Why are some features off by default?

They change behaviour that some players rely on (such as emergency vehicles leaving their route) or are still experimental. Read the feature's description in [All features](/Cities_Skylines_1_Modpack/wiki/ai-improve/features/) before turning it on.

## Aircraft rerouting does nothing. Is it broken?

It is a known limit. Measured taxiway congestion never reaches the level the feature reacts to, so it does not trigger yet. It needs a different signal, not a different setting.

## I changed an advanced value and a feature stopped working.

Use **Reset to defaults** in the options header. Advanced values are calibrated against real play, which is why they are hidden by default.

## Where is the output log?

Cities: Skylines writes it next to the game's data folder, as `output_log.txt` inside `Cities_Data` in the game's installation folder. Turn on **Verbose logging** only while reproducing a problem, then turn it off again.

## How do I report a problem?

Use the [issue forms](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/issues/new/choose) and include:

1. The mod version (Release or Development) and your game version.
2. Your full mod list.
3. The steps that lead to the problem.
4. The relevant part of the output log.

Remove your Windows username and other private details from logs before posting.

<!-- Author: OpenAI-Codex_Responsible: Modpack_Website. -->
