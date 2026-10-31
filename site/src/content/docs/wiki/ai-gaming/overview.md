---
title: AI Gaming
description: "What the 0.4.0 Development source does, its confirmation flow, and its packaging and runtime limits."
---

> **Development build update, 31 October 2026:** A Windows x64 Companion and matching DLL are available as individual GitHub release assets. In-game behavior remains unverified. See [Development downloads and installation](/Cities_Skylines_1_Modpack/wiki/development-downloads/).

The **0.4.0 Development source** adds an optional Windows companion for player-confirmed advice from Ollama Local. It does not control the simulation. Other play modes remain planned.

## What the 0.4.0 source implements

- Choose which aggregate city fields to include: game date, population, funds, residential demand, commercial demand and workplace demand.
- All six fields start selected; remove any fields you do not want to share. The separate city-summary command can still write the game date, population and funds to the local game log when requested.
- Enter an Ollama model name and a question, then review the full request before sending it.
- Confirm each request separately. The model response is shown as advice only and is never converted into a game action.

## Package and runtime status

The GitHub Development prerelease provides the DLL and self-contained Windows x64 Companion as separate assets. Use files from the same release: the DLL checks the exact Companion hash. Published assets and cloud builds do not establish successful in-game use or Ollama requests; both remain unverified. Back up saves and use a separate test city.

## Local data and Ollama

AI Gaming sends only the selected aggregate fields, model name and question to Ollama on this PC. Ollama may route inference to a cloud service depending on its own configuration; AI Gaming cannot inspect or guarantee that configuration. The current source has no cloud-provider adapter or API-key entry/storage. Do not include city names, account details, secrets or personal information in the free-text question.

## Planned modes

Watching an AI build a city and letting it perform limited game actions are planned ideas, not implemented features. Version 0.4.0 also contains no calls to other Modpack mods; using their capabilities is a longer-term design direction.

See the [roadmap](/Cities_Skylines_1_Modpack/wiki/project/roadmap/) for progress.

<!-- Updated by OpenAI-Codex on 2026-10-26. -->

<!-- Author: OpenAI-Codex_Responsible: Modpack_Website. -->
