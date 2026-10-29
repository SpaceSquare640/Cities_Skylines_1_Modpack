# AI Gaming Architecture

**Scope:** This document records the current 0.4.0 preview and possible future development paths for the Cities: Skylines 1 mod. Roadmap proposals are not implementation claims.

**Evidence labels:** `[Verified]` means confirmed from this branch's files, installed game assembly metadata, or dated source checks. `[Approved]` records a user-approved direction. `[Inferred]` means a proposal or assumption that still needs validation in the game and its Unity Mono runtime. `[To decide]` marks a choice awaiting user approval.

## Current 0.4.0 Local Ollama Preview

- `[Verified]` The mod targets .NET Framework 3.5 and references `ICities.dll`, `ColossalManaged.dll`, `Assembly-CSharp.dll`, and `UnityEngine.dll` from the configured game installation. These references are not copied beside the mod DLL.
- `[Verified]` The `IUserMod` entry point exposes the name, an honest early-preview description, and a settings page localized in English, Traditional Chinese, Simplified Chinese, Japanese, and Korean. English is the initial default regardless of the game locale; the player's selection is saved and can be changed while the settings page is open.
- `[Verified]` The optional city-summary log remains schemaVersion 1 and queues reads of the HUD population, funds in cents, game date, and three demand values through `SimulationManager.AddAction`. It writes one aggregate summary through the game's log API and does not include DLC identifiers.
- `[Verified]` The 0.4.0 source adds an opt-in local Ollama request path through a companion. It does not include cloud providers, API keys, save-file writes, Harmony, game-state mutation, or a target that copies files into the game Mods folder.
- `[Verified]` The companion request uses schemaVersion 4 and carries schemaVersion 3 city snapshot fields. Its six base city fields and four optional groups (utilities, built zoning, traffic/transit, service status) are checked by default and can be changed individually, with a Select All control. A sorted list of game API-reported installed-and-available `SteamHelper.DLC` enum identifiers is captured automatically and has no separate checkbox. City values are captured asynchronously on the simulation thread; DLC detection runs on the UI dispatcher after that capture. The multi-line preview shows the exact destination, model, response-body limit, selected values, DLC identifiers, and question before a distinct confirmation. Companion auto-start permission defaults off and resets on game restart.
- `[Verified]` The Ollama `/api/chat` response-body limit is user-configurable in MiB and defaults to zero (unlimited). Positive settings from 1 to 2047 MiB reject and discard the complete raw HTTP response body, including the JSON envelope, if it exceeds the setting; the limit does not control generated tokens or total process memory, because parsing/copying may require additional memory. The `/api/tags` response retains its 1 MiB cap. Unlimited response reading can still consume available memory and remains subject to the existing request deadline.
- `[Verified]` DLC detection requires `PlatformService.IsDLCStateReady` and includes a catalog identifier only when `SteamHelper.IsDLCOwned` and `SteamHelper.IsDLCAvailable` both return true. Static IL inspection confirms `IsDLCOwned` calls `PlatformService.IsDlcInstalled`; this is not treated as account ownership. Detection failure prevents preview preparation and sends no request. These game API calls have not been verified in the subscribed game.
- `[Verified]` The companion requires the `installedAvailableDlcs` request key, encoded as UTF-8 Base64 over a sorted, LF-separated list of enum identifiers. It rejects malformed, duplicate, unsorted, or unknown-format identifiers. Its prompt tells the model that the identifiers describe available content and do not prove that a DLC is active in the current save.
- `[Verified]` The companion binds an ephemeral port on `127.0.0.1`, authenticates requests and responses with session HMAC/sequence checks, and connects only to Ollama through `localhost:11434` and an IPv4 loopback socket. It requires a `/api/tags` entry with positive size and rejects common cloud-tag suffixes; it cannot inspect Ollama's configuration or guarantee Ollama will not route elsewhere. Loopback traffic is authenticated but not encrypted.
- `[Verified]` The companion project builds as a .NET 10 framework-dependent executable for local source validation. Workshop packaging must publish a self-contained Windows x64 single-file companion and pin its SHA-256 in the matching mod build. The local self-contained publish has not been verified because its runtime pack is unavailable here.
- `[Inferred]` Static compilation and assembly inspection cannot prove that the mod loads, that the callback runs on the expected thread in every game state, or that the chosen values match the in-game UI. Verify those behaviors in a disposable city before a public release.

## Bridge Options

### In-game loopback bridge

- `[Approved]` The selected design uses a separate Windows companion and loopback HTTP bound only to `127.0.0.1` on an OS-assigned ephemeral port. Reject requests containing `Origin`. No request-size or request-frequency caps will be enforced; retain request deadlines and per-request consent.
- `[Approved]` Generate a 32-byte CSPRNG session secret for each companion start and rotate it on every launch. Transfer it only through a private anonymous pipe after opt-in; never expose it in files, logs, URLs, environment variables, or command-line arguments. Each request uses an HMAC proof with a monotonically increasing sequence number; compare proofs in constant time and reject replay or out-of-order requests. Accept only the paired game process session established through that handshake, and check parent-process and exact edition/build/protocol/hash identity before connecting.
- `[Approved]` This protects the session against guessed proofs and replay, but loopback HTTP does not encrypt payloads. The accepted threat model does not defend against local privileged software able to read or inject into the game or companion process, inspect their memory, or control the game process.
- `[Verified]` The current companion uses one short-lived authenticated HTTP request per player confirmation, then exits when the paired mod process closes its input pipe. The mod does not wait on the simulation thread for the model response.
- `[Inferred]` Do not host cloud credentials or perform model inference inside the simulation callback. Send only consented, schema-validated snapshots to the companion; no application request-size or request-frequency caps are defined in this design.

### External process with files or IPC

- `[Inferred]` A companion can exchange versioned JSON snapshots and proposals using an explicitly selected directory, or use an IPC mechanism supported by the target runtime. The mod should write atomically and read only a validated command file or message.
- `[Inferred]` File exchange is easier to inspect and recover after a crash, but polling adds latency and concurrent reads can observe partial data unless writes use temporary files followed by an atomic rename. IPC can lower latency but requires the same framing, timeout, and lifecycle protections as a loopback bridge.
- `[Inferred]` The selected location must be user-visible and outside save data. The 0.1.0 restriction against project-external file access still applies until a later version explicitly designs and documents consent for an export location.

### Recommendation

- `[Approved]` Use a separate companion with a narrow loopback HTTP contract. The implementation is present; game-runtime compatibility and Workshop packaging still need validation.

## Model Connections

- `[Verified]` The installed game `System.dll` exposes `HttpWebRequest` and asynchronous response APIs, but its `SecurityProtocolType` and `SslProtocols` metadata contains no TLS 1.2 member. The installed `UnityEngine.dll` exposes `UnityWebRequest`, but its `InternalBegin` implementation is a native `InternalCall`; managed DLL inspection cannot verify its TLS behavior. See the verified/inferred split in `docs/AI_DATA_BOUNDARY.md`.
- `[Approved]` The intended source set is Ollama Local, Ollama Cloud, OpenAI, and Anthropic, implemented in that order: Ollama Local first, then the three cloud sources after provider authentication and terms are checked. Use one provider-neutral request/response model with adapters; the player selects the model name, which is not hard-coded in the mod.
- `[Verified]` The mod communicates only with its companion at `127.0.0.1`. The companion restricts the provider connector to `localhost:11434`, validates that the model is installed locally and has nonzero local size, rejects cloud model tags, and explicitly connects to `127.0.0.1`; no cloud fallback is implemented. This path has not been tested in Cities: Skylines.
- `[Inferred]` Route the three cloud sources through the companion's HTTPS stack because TLS 1.2 support in the game's managed/native runtime is not established. The companion sends each request only after per-request consent and displays the selected cloud destination.
- `[Approved]` Keep all provider keys in companion memory only for the initial version; clear them when the companion exits. Do not persist keys in the mod, saves, logs, repository, or companion storage.
- `[Approved]` Release and Development Workshop items each carry their matching companion build. The binary is unsigned; both item descriptions and the website must disclose this and publish that build's SHA-256 and source commit. The mod can start the installed companion only after player consent, and a settings preference can disable auto-start.
- `[Approved]` Before public release, upload a companion-bearing build to the hidden Development Workshop item and observe whether Steam/Workshop blocks the upload or removes the item. This is a viability check, not a guarantee of policy approval. If Workshop distribution is blocked, removed, or otherwise not viable, distribute the companion through GitHub Releases instead. Only the installation source changes; companion discovery, edition/version/hash checks, opt-in launch, authentication, and the loopback protocol stay the same.
- `[Approved]` Use an immutable, edition/version/hash-specific per-user `%LOCALAPPDATA%` cache to avoid executing from a Workshop directory or replacing a locked executable during Steam updates. Verify the expected hash and refuse the session on any edition, build, protocol, or hash mismatch. The implementation plan must define safe stale-cache cleanup before cleanup code is written.
- `[Inferred]` If the companion is missing or not running, keep read-only features available and report that model access is unavailable. Do not silently download it or make a direct model connection. The player installs it from the selected distribution source, and the executable runs only after opt-in.
- `[Inferred]` No companion request-size or request-frequency limits are planned. Player-facing documentation should tell users to set provider-side usage/spending caps before cloud requests; local Ollama use is not covered by cloud account quotas.
- `[Inferred]` Treat city names, mod-provided text, workshop descriptions, and model output as untrusted input. They must not be interpreted as executable commands or as permission to bypass the action whitelist.

## Observation and Action Contracts

- `[Verified]` Version 0.4.0 retains the 0.3.0 read-only schemaVersion 1 snapshot log button and sends a schemaVersion 4 companion request carrying city snapshot schemaVersion 3 fields. The request includes gameDate (`yyyy-MM-dd`), HUD population (`uint`, `DistrictManager` district 0 `m_populationData.m_finalCount`), fundsCents (`long`, `EconomyManager.playerMoney`), three demand values (`int`), installed-and-available DLC enum identifiers, selected optional groups, and the configured response-body byte count. The four optional groups add utilities, built building counts/areas by use, game traffic/transit statistics, and service indicators. City values use `SimulationManager.AddAction`; DLC detection and completion use the UI dispatcher. The request path does not log snapshot values or DLC identifiers.
- `[Verified]` Static IL inspection confirms the HUD population path reads district 0 `m_populationData.m_finalCount`. The `EconomyManager.playerMoney` getter multiplies `LastCashAmount` by 100, so the snapshot labels this raw value as cents. This confirms the compiled API path; in-game HUD equality has not been tested.
- `[Verified]` The 0.3.0 snapshot log contains no city name, citizen identifiers, or object list and remains unchanged. The new request fields are aggregate values only: utility units and statistic windows remain unverified; zoning covers built buildings, not vacant zoned cells; traffic flow is not a congestion percentage; police and health values are workload/capacity rather than geographic coverage; fire coverage is an average byte over created network segments and is unavailable with zero samples.
- `[Verified]` The DLC list describes items the game API reports as installed and available on this installation. It does not identify account ownership or prove that the current save activates or uses those items.
- `[Inferred]` A future `CitySnapshot` should be a versioned, read-only DTO with an explicit allowlist of aggregate values, a capture timestamp, game version, and monotonically increasing sequence number. Exclude citizen names, personal identifiers, raw save contents, and unrestricted object graphs.
- `[Inferred]` AI suggestions should first use a proposal-only schema: a short explanation, the proposed typed action, affected object identifiers, expected effect, and expiry. Showing a suggestion must never invoke an action.
- `[Inferred]` Any later control mode should require a visible per-session player opt-in, a separately confirmed start, and a clear stop control. Validate every action against a small typed whitelist, current game state, bounds, rate limits, and a maximum number of queued actions. Reject unknown or stale commands and log only a redacted action summary.
- `[Inferred]` Begin with reversible, low-impact controls such as pausing or selecting a bounded simulation speed. Add economy, zoning, placement, or save operations only after their game APIs, failure behavior, and recovery path have been tested; never expose arbitrary code execution or unrestricted game-object mutation.

## Security and Reliability

- `[Verified]` The 0.4.0 source contains no credential field or persistence, save-file write, or simulation-state mutation. Its network client is limited to authenticated loopback communication with the companion; cloud access exists only as a future design direction.
- `[Inferred]` Keep the default mode read-only. Require explicit consent before any data leaves the game process and before enabling any action. Show the destination, data categories, and action scope in the UI.
- `[Inferred]` Keep provider keys exclusively in companion memory; never put them in the mod, saves, repository, prompts, or logs. Redact authorization headers and provider responses. Apply request deadlines and cancellation so a stalled model cannot stall the simulation.
- `[Inferred]` Treat transport failure, malformed data, companion restarts, and model refusal as no-op outcomes. Fail closed: no response or invalid response must never create or repeat an action.
- `[Verified]` The detailed future data allowlist, consent, retention, deletion, credential, and log-redaction proposal is documented in `docs/AI_DATA_BOUNDARY.md`. It does not enable a connection or authorize implementation.

## .NET 3.5 and Unity Mono Constraints

- `[Verified]` The mod targets .NET Framework 3.5 and uses game-provided managed assemblies. Its current `HttpWebRequest` use is limited to the companion's loopback HTTP endpoint; the companion uses a separate .NET 10 process.
- `[Verified]` Static inspection of the installed `System.dll` confirms that `HttpWebRequest` and asynchronous request APIs exist, but the exposed TLS protocol enums and Mono SSL mapping have no explicit TLS 1.2 value. The mod uses HTTP only for loopback. Static inspection of the installed `UnityEngine.dll` cannot reveal the TLS behavior behind `UnityWebRequest.InternalBegin` because it is a native internal call.
- `[Inferred]` A TLS 1.2 handshake may depend on the native Unity backend, Mono version, operating system, and security configuration. No handshake has been attempted; do not assume direct cloud HTTPS works in the game.
- `[Inferred]` Before selecting a direct local HTTP route, validate it in the exact game runtime and keep all requests off the simulation thread. Route cloud HTTPS and provider SDKs through a separately updated companion using a current TLS stack.
- `[Inferred]` Keep simulation-thread work bounded and synchronous only for local reads. Perform model requests, file polling, retries, and parsing outside the simulation thread; pass validated, small results back through the game's simulation queue.

## Roadmap

1. `[Verified]` **0.1.0 — Read-only preview:** mod identity, localized settings page, version/status/site display, and a user-triggered city summary in the game log. No AI connection or control.
2. `[Verified]` **0.3.0 — Read-only city snapshot:** a versioned aggregate allowlist is captured through the simulation queue and written to the game log.
3. `[Verified]` **0.4.0 — Ollama Local advice:** individually selectable snapshot fields and a player question are previewed and sent once to an installed local Ollama model through the companion after confirmation; no cloud, credentials, or game control.
4. `[Inferred]` **Cloud model sources:** add Ollama Cloud, OpenAI, and Anthropic only after checking current authentication/terms, obtaining a separate implementation approval, and preserving per-request consent and key confinement.
5. `[Inferred]` **AI suggestions expansion:** improve the local suggestion UI and provider abstraction without executing model output.
6. `[Inferred]` **Restricted control:** add a separate opt-in mode with a typed action allowlist, current-state validation, explicit start/stop controls, per-action limits, and a tested recovery path.

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

- `[Verified]` A successful Release build establishes that the mod compiles against the available game references and that the companion source compiles against the installed .NET 10 reference packs. The mod assembly intentionally references `System.Net` types for loopback HTTP; static source review must confirm the endpoint remains restricted to `127.0.0.1`.
- `[Inferred]` Self-contained win-x64 publishing, Workshop packaging, game loading, UI rendering, simulation-thread behavior, actual Ollama model filtering, localization in the game, and compatibility with other mods remain unverified until packaged and tested in Cities: Skylines 1.

---

Implementation update signed by OpenAI-Codex.

Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.

Implementation update signed by OpenAI-Codex.

Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.
