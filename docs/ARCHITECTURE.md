# AI Gaming Architecture

**Scope:** This document describes a possible development path for the Cities: Skylines 1 mod. It does not claim that any future bridge, model connection, or control mechanism has been implemented.

**Evidence labels:** `[Verified]` means confirmed from this branch's files, the installed game assembly metadata, or the requested static build. `[Inferred]` means a design proposal or an assumption that still needs validation in the game and its Unity Mono runtime.

## Current 0.3.0 Preview

- `[Verified]` The mod targets .NET Framework 3.5 and references `ICities.dll`, `ColossalManaged.dll`, `Assembly-CSharp.dll`, and `UnityEngine.dll` from the configured game installation. These references are not copied beside the mod DLL.
- `[Verified]` The `IUserMod` entry point exposes the name, an honest early-preview description, and a settings page with English and Traditional Chinese strings. Missing or unsupported locale codes fall back to English.
- `[Verified]` The optional schemaVersion 1 snapshot queues reads of the HUD population, funds in cents, game date, and three demand values through `SimulationManager.AddAction`. The callback catches exceptions and writes one aggregate summary only through the game's log API.
- `[Verified]` The 0.3.0 source has no model connection, network client or listener, API-key, save-file write, Harmony, or game-state mutation code. The project has no target that copies the build into a Mods folder.
- `[Inferred]` Static compilation and assembly inspection cannot prove that the mod loads, that the callback runs on the expected thread in every game state, or that the chosen values match the in-game UI. Verify those behaviors in a disposable city before a public release.

## Bridge Options

### In-game loopback bridge

- `[Approved]` The selected design uses a separate local companion and loopback HTTP. Bind only to `127.0.0.1` on a fresh ephemeral port; never expose the listener to LAN or wildcard interfaces. The companion generates a new in-memory session token at each start, shares it with the mod through a reviewed bootstrap mechanism, and requires it on every request. Reject requests containing `Origin`; apply request/response size limits, rate limits, and deadlines. Exact limits and the bootstrap remain to be designed.
- `[Inferred]` A persistent connection can reduce polling and file coordination, but it adds port ownership, authentication, message framing, timeout, lifecycle, and game-thread scheduling concerns. The mod must never block the simulation thread waiting for a response.
- `[Inferred]` Do not host cloud credentials or perform model inference inside the simulation callback. Send bounded snapshots to the companion and accept only versioned, size-limited messages.

### External process with files or IPC

- `[Inferred]` A companion can exchange versioned JSON snapshots and proposals using an explicitly selected directory, or use an IPC mechanism supported by the target runtime. The mod should write atomically and read only a validated command file or message.
- `[Inferred]` File exchange is easier to inspect and recover after a crash, but polling adds latency and concurrent reads can observe partial data unless writes use temporary files followed by an atomic rename. IPC can lower latency but requires the same framing, timeout, and lifecycle protections as a loopback bridge.
- `[Inferred]` The selected location must be user-visible and outside save data. The 0.1.0 restriction against project-external file access still applies until a later version explicitly designs and documents consent for an export location.

### Recommendation

- `[Approved]` Use a separate companion process with a narrow loopback HTTP contract for the first connected prototype. Keep network clients and model SDKs out of the game process. A small compatibility probe is still required before implementation.

## Model Connections

- `[Verified]` The installed game `System.dll` exposes `HttpWebRequest` and asynchronous response APIs, but its `SecurityProtocolType` and `SslProtocols` metadata contains no TLS 1.2 member. The installed `UnityEngine.dll` exposes `UnityWebRequest`, but its `InternalBegin` implementation is a native `InternalCall`; managed DLL inspection cannot verify its TLS behavior. See the verified/inferred split in `docs/AI_DATA_BOUNDARY.md`.
- `[Approved]` The intended source set is Ollama Local, Ollama Cloud, OpenAI, and Anthropic, implemented in that order: Ollama Local first, then the three cloud sources after provider authentication and terms are checked. Use one provider-neutral request/response model with adapters; the player selects the model name, which is not hard-coded in the mod.
- `[Inferred]` Direct HTTP from the mod to Ollama Local at `http://localhost:11434` is technically plausible because the installed `HttpWebRequest` APIs exist and this path does not use TLS. It has not been tested in Cities: Skylines. Route local and cloud model requests through a separate companion in the recommended architecture so provider handling remains outside the game process.
- `[Inferred]` Route the three cloud sources through the companion's HTTPS stack because TLS 1.2 support in the game's managed/native runtime is not established. The companion sends each request only after per-request consent and displays the selected cloud destination.
- `[Approved]` Keep all provider keys in companion memory only for the initial version; clear them when the companion exits. Do not persist keys in the mod, saves, logs, repository, or companion storage.
- `[Inferred]` The companion is not a Workshop item. The proposed distribution is a versioned official release with published checksums; installation, update, and code-signing decisions remain open. If it is not installed or running, keep the mod's read-only features available and report that model access is unavailable without trying another network route.
- `[Inferred]` Treat city names, mod-provided text, workshop descriptions, and model output as untrusted input. They must not be interpreted as executable commands or as permission to bypass the action whitelist.

## Observation and Action Contracts

- `[Verified]` Version 0.3.0 captures a schemaVersion 1 read-only snapshot after the player clicks the settings button. Its allowlist is gameDate (`yyyy-MM-dd`), HUD population (`uint`, `DistrictManager` district 0 `m_populationData.m_finalCount`), fundsCents (`long`, `EconomyManager.playerMoney`), and residentialDemand, commercialDemand, and workplaceDemand (`int`). The button queues capture with `SimulationManager.AddAction`; the callback writes one aggregate line to the game log and catches exceptions. It does not export files or transmit data.
- `[Verified]` Static IL inspection confirms the HUD population path reads district 0 `m_populationData.m_finalCount`. The `EconomyManager.playerMoney` getter multiplies `LastCashAmount` by 100, so the snapshot labels this raw value as cents. This confirms the compiled API path; in-game HUD equality has not been tested.
- `[Verified]` The 0.3.0 snapshot contains no city name, citizen identifiers, or object list. Traffic flow and service coverage are outside this schema.
- `[Inferred]` A future `CitySnapshot` should be a versioned, read-only DTO with an explicit allowlist of aggregate values, a capture timestamp, game version, and monotonically increasing sequence number. Exclude citizen names, personal identifiers, raw save contents, and unrestricted object graphs.
- `[Inferred]` AI suggestions should first use a proposal-only schema: a short explanation, the proposed typed action, affected object identifiers, expected effect, and expiry. Showing a suggestion must never invoke an action.
- `[Inferred]` Any later control mode should require a visible per-session player opt-in, a separately confirmed start, and a clear stop control. Validate every action against a small typed whitelist, current game state, bounds, rate limits, and a maximum number of queued actions. Reject unknown or stale commands and log only a redacted action summary.
- `[Inferred]` Begin with reversible, low-impact controls such as pausing or selecting a bounded simulation speed. Add economy, zoning, placement, or save operations only after their game APIs, failure behavior, and recovery path have been tested; never expose arbitrary code execution or unrestricted game-object mutation.

## Security and Reliability

- `[Verified]` The 0.3.0 source contains no credential field or persistence, network client or listener, save-file write, or simulation-state mutation. It reads only the documented aggregate fields when the player requests a snapshot.
- `[Inferred]` Keep the default mode read-only. Require explicit consent before any data leaves the game process and before enabling any action. Show the destination, data categories, and action scope in the UI.
- `[Inferred]` Keep provider keys exclusively in the companion; never put them in the mod, saves, repository, prompts, or logs. Redact authorization headers and provider responses. Apply request deadlines, response-size limits, and cancellation so a stalled model cannot stall the simulation.
- `[Inferred]` Treat transport failure, malformed data, companion restarts, and model refusal as no-op outcomes. Fail closed: no response or invalid response must never create or repeat an action.
- `[Verified]` The detailed future data allowlist, consent, retention, deletion, credential, and log-redaction proposal is documented in `docs/AI_DATA_BOUNDARY.md`. It does not enable a connection or authorize implementation.

## .NET 3.5 and Unity Mono Constraints

- `[Verified]` The mod targets .NET Framework 3.5 and uses game-provided managed assemblies. The current preview has no network implementation.
- `[Verified]` Static inspection of the installed `System.dll` confirms that `HttpWebRequest` and asynchronous request APIs exist, but the exposed TLS protocol enums and Mono SSL mapping have no explicit TLS 1.2 value. Static inspection of the installed `UnityEngine.dll` cannot reveal the TLS behavior behind `UnityWebRequest.InternalBegin` because it is a native internal call.
- `[Inferred]` A TLS 1.2 handshake may depend on the native Unity backend, Mono version, operating system, and security configuration. No handshake has been attempted; do not assume direct cloud HTTPS works in the game.
- `[Inferred]` Before selecting a direct local HTTP route, validate it in the exact game runtime and keep all requests off the simulation thread. Route cloud HTTPS and provider SDKs through a separately updated companion using a current TLS stack.
- `[Inferred]` Keep simulation-thread work bounded and synchronous only for local reads. Perform model requests, file polling, retries, and parsing outside the simulation thread; pass validated, small results back through the game's simulation queue.

## Roadmap

1. `[Verified]` **0.1.0 — Read-only preview:** mod identity, localized settings page, version/status/site display, and a user-triggered city summary in the game log. No AI connection or control.
2. `[Verified]` **0.3.0 — Read-only city snapshot:** a versioned aggregate allowlist is captured through the simulation queue and written to the game log. No file export, network connection, model, or game control.
3. `[Inferred]` **Observation export:** consider a user-selected export/IPC path only after consent, path validation, and runtime threading checks.
4. `[Inferred]` **AI suggestions:** connect the external companion to a local or cloud model; display proposals in game without executing them and let the player dismiss or copy them.
5. `[Inferred]` **Restricted control:** add a separate opt-in mode with a typed action allowlist, current-state validation, explicit start/stop controls, per-action limits, and a tested recovery path.

## Modpack interoperability (accepted contract v1.0)

Status: The accepted contract is recorded in the project notes at E:\[Other]\Obsidian Markdown\Cities_Skyline_1_Modpack_Document\共通參考\Modpack 互通契約 v1.md. This implementation is limited to discovery, version compatibility reporting, and graceful unavailable states.

- [Verified] AI Gaming enumerates the game plugin manager, inspects enabled providers for the public instance manifest and capability entry-point signatures, and reads manifests only for discovery. It has no hard reference to AI Improve or Transport assemblies.
- [Verified] Known module and component names are used only to display whether AI Improve or Transport is installed or disabled when no manifest is available. Names do not identify capabilities and do not determine compatibility.
- [Verified] Provider IDs, contract versions, capability IDs, capability versions, and the ReadOnly classification in the manifest determine compatibility. The settings page reports missing, disabled, missing-interface, invalid, ambiguous, interface-incomplete, incompatible, and compatible states in English and Traditional Chinese.
- [Verified] The presence of the InvokeCapability signature is reported separately as interface readiness; module names do not affect manifest compatibility, and the entry point is never invoked in this version.
- [Verified] AI Gaming validates the public InvokeCapability signature but does not call it or any provider capability. The discovery feature does not change simulation state or access provider internals.
- [Verified] Local builds have no target that copies the mod into the game Mods directory.
- [Inferred] Static compilation and source inspection cannot establish that the Workshop provider manifests will be available, that the UI renders correctly in Cities: Skylines, or that discovery remains reliable with every mod load order. Verify these behaviors in game after an approved Development upload and subscription test.

## Validation Boundary

- `[Verified]` A successful Release build and static inspection establish that the source compiles against the available game references and that the built assembly contains no direct networking type references.
- `[Inferred]` Workshop packaging, game loading, UI rendering, simulation-thread behavior, log output, localization in the game, and compatibility with other mods remain unverified until tested in Cities: Skylines 1.
