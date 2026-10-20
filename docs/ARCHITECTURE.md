# AI Gaming Architecture

**Scope:** This document describes a possible development path for the Cities: Skylines 1 mod. It does not claim that any future bridge, model connection, or control mechanism has been implemented.

**Evidence labels:** `[Verified]` means confirmed from this branch's files, the installed game assembly metadata, or the requested static build. `[Inferred]` means a design proposal or an assumption that still needs validation in the game and its Unity Mono runtime.

## Current 0.1.0 Preview

- `[Verified]` The mod targets .NET Framework 3.5 and references `ICities.dll`, `ColossalManaged.dll`, `Assembly-CSharp.dll`, and `UnityEngine.dll` from the configured game installation. These references are not copied beside the mod DLL.
- `[Verified]` The `IUserMod` entry point exposes the name, an honest early-preview description, and a settings page with English and Traditional Chinese strings. Missing or unsupported locale codes fall back to English.
- `[Verified]` The optional city-summary action queues reads of population, player funds, and the in-game date through `SimulationManager.AddAction`. The callback catches exceptions and writes the summary only through the game's log API.
- `[Verified]` The 0.1.0 source has no HTTP, socket, listener, API-key, save-file, Harmony, or game-state mutation code. The project has no target that copies the build into a Mods folder.
- `[Inferred]` Static compilation and assembly inspection cannot prove that the mod loads, that the callback runs on the expected thread in every game state, or that the chosen values match the in-game UI. Verify those behaviors in a disposable city before a public release.

## Bridge Options

### In-game loopback bridge

- `[Inferred]` A future mod could exchange messages with a separate local companion over a loopback-only transport. Keeping the companion on the same machine can avoid exposing a listener to the LAN, while the companion handles model access and credentials.
- `[Inferred]` A persistent connection can reduce polling and file coordination, but it adds port ownership, authentication, message framing, timeout, lifecycle, and game-thread scheduling concerns. The mod must never block the simulation thread waiting for a response.
- `[Inferred]` Do not host cloud credentials or perform model inference inside the simulation callback. Send bounded snapshots to the companion and accept only versioned, size-limited messages.

### External process with files or IPC

- `[Inferred]` A companion can exchange versioned JSON snapshots and proposals using an explicitly selected directory, or use an IPC mechanism supported by the target runtime. The mod should write atomically and read only a validated command file or message.
- `[Inferred]` File exchange is easier to inspect and recover after a crash, but polling adds latency and concurrent reads can observe partial data unless writes use temporary files followed by an atomic rename. IPC can lower latency but requires the same framing, timeout, and lifecycle protections as a loopback bridge.
- `[Inferred]` The selected location must be user-visible and outside save data. The 0.1.0 restriction against project-external file access still applies until a later version explicitly designs and documents consent for an export location.

### Recommendation

- `[Inferred]` Prefer a separate companion process with a narrow, loopback-only IPC contract for the first connected prototype. Keep network clients and model SDKs out of the game process; choose a transport only after a small compatibility probe against the installed Unity Mono runtime.

## Model Connections

- `[Inferred]` A local-model companion can call a model service already running on the player's machine. Keep that endpoint configurable in the companion, reject non-loopback endpoints by default, bound request size and duration, and make the game mod unaware of model-specific protocols.
- `[Inferred]` A cloud-model companion can send only the approved snapshot and player-approved question to an HTTPS provider. Store credentials in the operating system's protected credential store where available; never place keys in saves, mod settings, prompts, crash reports, or logs. The game DLL should never receive the key.
- `[Inferred]` Treat city names, mod-provided text, workshop descriptions, and model output as untrusted input. They must not be interpreted as executable commands or as permission to bypass the action whitelist.

## Observation and Action Contracts

- `[Verified]` Version 0.1.0 reads only the current population count, player funds, and game date when the player clicks the summary button. It does not export files or transmit data.
- `[Inferred]` A future `CitySnapshot` should be a versioned, read-only DTO with an explicit allowlist of aggregate values, a capture timestamp, game version, and monotonically increasing sequence number. Exclude citizen names, personal identifiers, raw save contents, and unrestricted object graphs.
- `[Inferred]` AI suggestions should first use a proposal-only schema: a short explanation, the proposed typed action, affected object identifiers, expected effect, and expiry. Showing a suggestion must never invoke an action.
- `[Inferred]` Any later control mode should require a visible per-session player opt-in, a separately confirmed start, and a clear stop control. Validate every action against a small typed whitelist, current game state, bounds, rate limits, and a maximum number of queued actions. Reject unknown or stale commands and log only a redacted action summary.
- `[Inferred]` Begin with reversible, low-impact controls such as pausing or selecting a bounded simulation speed. Add economy, zoning, placement, or save operations only after their game APIs, failure behavior, and recovery path have been tested; never expose arbitrary code execution or unrestricted game-object mutation.

## Security and Reliability

- `[Verified]` The 0.1.0 module contains no credential field or persistence, has no network client or listener, and has no code that writes to a save or changes simulation state.
- `[Inferred]` Keep the default mode read-only. Require explicit consent before any data leaves the game process and before enabling any action. Show the destination, data categories, and action scope in the UI.
- `[Inferred]` Keep API keys exclusively in the companion's protected credential store. Redact authorization headers, prompts containing secrets, and provider responses from logs. Apply request deadlines, response-size limits, and cancellation so a stalled model cannot stall the simulation.
- `[Inferred]` Treat transport failure, malformed data, companion restarts, and model refusal as no-op outcomes. Fail closed: no response or invalid response must never create or repeat an action.

## .NET 3.5 and Unity Mono Constraints

- `[Verified]` The mod is built against the .NET 3.5 target and the game-provided managed assemblies; this preview does not reference networking libraries.
- `[Inferred]` Modern .NET APIs and TLS behavior can differ between .NET Framework 3.5, Unity Mono versions, operating systems, and installed security updates. Do not assume a particular TLS protocol or modern HTTP API is available in the game process.
- `[Inferred]` Before selecting in-process networking or newer BCL APIs, compile and run a focused compatibility probe in the exact game runtime. Prefer keeping HTTPS and provider SDKs in a separately updated companion process that can use a current runtime and TLS stack.
- `[Inferred]` Keep simulation-thread work bounded and synchronous only for local reads. Perform model requests, file polling, retries, and parsing outside the simulation thread; pass validated, small results back through the game's simulation queue.

## Roadmap

1. `[Verified]` **0.1.0 — Read-only preview:** mod identity, localized settings page, version/status/site display, and a user-triggered city summary in the game log. No AI connection or control.
2. `[Inferred]` **Observation output:** add a versioned aggregate snapshot and a user-selected export/IPC path after consent, path validation, and runtime threading checks.
3. `[Inferred]` **AI suggestions:** connect the external companion to a local or cloud model; display proposals in game without executing them and let the player dismiss or copy them.
4. `[Inferred]` **Restricted control:** add a separate opt-in mode with a typed action allowlist, current-state validation, explicit start/stop controls, per-action limits, and a tested recovery path.

## Modpack interoperability (proposal)

**Status:** Proposal only. `[Inferred]` The Lead Architect will decide the final cross-mod architecture. Once that decision is recorded in its authoritative design document, replace this section with a reference to that document.

### Optional capability discovery

- `[Inferred]` AI Gaming should treat every other Modpack module as an optional dependency. It may inspect loaded mods for a published capability provider through a small optional contract or a narrowly scoped reflection adapter. AI Gaming must not add a hard assembly reference that prevents loading when the provider is absent.
- `[Inferred]` A provider should publish stable capability identifiers and contract versions through a public facade. AI Gaming can discover identifiers and compatible versions first, then call only the documented facade. Reflection should be confined to a compatibility adapter; it must not reach into private types, fields, or implementation details.
- `[Inferred]` If AI Improve is absent, incompatible, or fails during discovery, AI Gaming should omit its integration and continue with its own features. If present and compatible, AI Gaming may query documented feature states and, when the player explicitly enables the integration, request supported feature-toggle changes through AI Improve's own API so AI Improve retains ownership of its behavior and validation.
- `[Inferred]` If Automated Public Transport Planning is absent or incompatible, AI Gaming should continue without transport-planning capabilities. If present, AI Gaming may request a documented planning result or proposal and present it to the player. Planning and any later execution must remain separate capabilities; AI Gaming must not assume that receiving a plan authorizes applying it.
- `[Inferred]` Discovery and calls should be bounded, exception-isolated, and fail closed. A missing provider, unknown capability, unsupported version, timeout, or provider exception should disable only that integration for the current session and must not block the simulation thread or the rest of AI Gaming.

### Conflict-avoidance rules

- `[Inferred]` Keep each module authoritative for the behavior it owns. AI Gaming should call a provider's supported capability instead of implementing a second feature switch, transport planner, or other behavior already owned by that module.
- `[Inferred]` Integrate through public contracts, events, or documented facades. Do not patch another module's methods, and do not Harmony-patch a method that another Modpack module already patches. Avoid depending on load order or another module's private implementation.
- `[Inferred]` When no supported capability is available, hide or disable the related AI Gaming option and explain that the optional module or compatible capability is unavailable. Do not silently substitute a competing implementation.

### Capability contract sketch

- `[Inferred]` A provider descriptor should expose a stable provider ID, provider version, contract/API version, and a collection of capability descriptors. Each capability descriptor should contain a stable ID, its own version, a short description, and an operation classification.
- `[Inferred]` Classify operations explicitly as `ReadOnly` or `Write`. Read-only examples include querying AI Improve feature status or requesting a transport-planning proposal. Write examples include asking AI Improve to change a feature setting or asking a transport module to apply a plan. A proposal-producing call must not be classified as an applied write unless it changes game or module state.
- `[Inferred]` The contract should make write operations visibly distinct, require explicit player consent for each enabled write-capable integration, and let the provider validate and reject every request. AI Gaming should not invoke write capabilities in the default read-only mode.
- `[Inferred]` Calls should use typed, bounded request and response objects, include the contract version, and return an explicit result such as success, unsupported, rejected, or failed. Providers should reject unknown versions and invalid or stale requests; consumers should treat errors as no-ops.
- `[Inferred]` The final contract shape, discovery mechanism, version policy, and ownership rules require a Lead Architect decision and compatibility validation against the game's .NET 3.5 / Unity Mono runtime before implementation.

## Validation Boundary

- `[Verified]` A successful Release build and static inspection establish that the source compiles against the available game references and that the built assembly contains no direct networking type references.
- `[Inferred]` Workshop packaging, game loading, UI rendering, simulation-thread behavior, log output, localization in the game, and compatibility with other mods remain unverified until tested in Cities: Skylines 1.
