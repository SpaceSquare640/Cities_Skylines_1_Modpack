# AI Gaming Data Boundary and Consent Design

**Status:** Proposal for a future milestone. This document does not authorize or implement an AI connection, data export, credential handling, or game control.

**Evidence labels:** `[Verified]` describes facts confirmed from the current branch, static assembly inspection, or completed static build. `[Approved]` records a design decision explicitly approved by the user. `[Inferred]` marks a proposal that still needs implementation review and runtime validation. `[To decide]` marks an unresolved choice for the user and Lead Architect.

## 1. Confirmed baseline and design gate

- `[Verified]` AI Gaming 0.3.0 captures a read-only `schemaVersion=1` city snapshot only after the player requests it. The data is written to the Cities: Skylines game log; it is not exported or transmitted.
- `[Verified]` The current allowlist contains only `gameDate`, `population`, `fundsCents`, `residentialDemand`, `commercialDemand`, and `workplaceDemand`, in addition to the schema version.
- `[Verified]` The current source has no model connection, network client or listener, API-key field or persistence, save-file write, or simulation-state mutation. The 0.3.0 game behavior has not been tested in Cities: Skylines.
- `[Inferred]` Any future model request must use an explicit, versioned allowlist derived from schema version 1. A new field requires a documented source, type, unit, and in-game UI comparison, user approval, and a schema-version increment before it can be sent.
- `[Inferred]` A future data-export milestone requires its own implementation plan and approval. This proposal alone does not approve transmission or model access.

## 2. Outbound data boundary

### 2.1 Schema version 1 allowlist

- `[Verified]` `schemaVersion`: integer value `1`; identifies the snapshot definition.
- `[Verified]` `gameDate`: string formatted as `yyyy-MM-dd`; sourced from the current game date.
- `[Verified]` `population`: unsigned integer; sourced from the HUD population value at district 0, `m_populationData.m_finalCount`.
- `[Verified]` `fundsCents`: 64-bit integer in cents; sourced from `EconomyManager.playerMoney`. Static IL inspection confirmed the getter multiplies `LastCashAmount` by 100. In-game HUD equality is not yet verified.
- `[Verified]` `residentialDemand`, `commercialDemand`, and `workplaceDemand`: integer values sourced from the corresponding `ZoneManager` demand fields used by the game information panel and tooltips. In-game value and label matching is not yet verified.
- `[Inferred]` For a first connected prototype, send only the fields needed for the specific player-approved request. Do not add an unapproved field merely because it exists in the game API.

### 2.2 Prohibited data

- `[Inferred]` Never send a city name, save file or save contents, citizen personal or identifying data, citizen or object lists, raw map data, or screenshots.
- `[Inferred]` Never send player account data, Steam account names or identifiers, platform identifiers, device identifiers, or other account-linked identifiers.
- `[Inferred]` Never send API keys, access tokens, authentication headers, passwords, private prompts containing credentials, or other secrets.
- `[Inferred]` Never send game logs, crash reports, mod configuration files, arbitrary files, or data collected from another mod unless a separately approved schema explicitly defines the field and its provider has authorized that use.
- `[Inferred]` Never treat model-generated code, commands, URLs, or text as permission to read files, run code, or change the game.
- `[To decide]` Confirm whether any future schema may include fields beyond schema version 1. Recommendation: keep the first connected prototype limited to the six confirmed snapshot fields plus a player-written question that is shown in the consent preview.

## 3. Player consent and revocation

- `[Inferred]` Require explicit consent for every individual request, including requests to a local model service. Consent must be off by default and must not be remembered as blanket permission for later requests.
- `[Inferred]` Before sending, show the selected source, player-selected model name, purpose, exact snapshot fields, and exact player-written question or prompt. Require a distinct confirmation for every request. For Ollama local, explicitly say that data remains on this computer and verify that the configured endpoint is loopback-only. For each cloud source, name the destination provider before confirmation.
- `[Inferred]` Do not start a background request or retry after failure without a new, explicit confirmation. Do not send a later snapshot under consent given for an earlier snapshot.
- `[Inferred]` If the player revokes consent before transmission, discard the pending request. If transmission has started, attempt cancellation, discard any later response, and do not initiate game actions. Explain that data already delivered to a provider cannot necessarily be recalled.
- `[Inferred]` A returned answer is an untrusted suggestion only. Displaying, copying, or dismissing it must not change the simulation.
- `[Verified]` The accepted Modpack interop contract is `contractVersion 1.0`; its Write capability rules are recorded in `共通參考\Modpack 互通契約 v1.md` in the project notes.
- `[Inferred]` Any future game operation requires a separate player confirmation for that operation and must stay within the accepted interop contract's Write rules, a typed action allowlist, current-state validation, and action limits. A model response or earlier data-sharing consent cannot grant that permission.
- `[To decide]` Decide how the UI will present cancellation after a request has left the game, including whether it offers a provider-side deletion request. Recommendation: provide a local cancel control and clearly state that cancellation cannot guarantee deletion of data already received remotely.

## 4. Retention and deletion

- `[Inferred]` Keep request snapshots and model responses in memory only for the active request and visible suggestion. Clear them when dismissed, when the request ends, or when the companion closes. Do not retain conversation history by default.
- `[Inferred]` Do not write prompts, responses, snapshots, or credentials into game saves, the mod directory, project files, source control, or logs.
- `[Inferred]` If a future transport requires temporary files, use a user-visible, explicitly selected location, a bounded format and size, and a documented expiry. Remove temporary data after completion, cancellation, or expiry; do not use the save directory.
- `[Inferred]` Local deletion can clear in-memory data and any temporary companion files owned by the application. It cannot erase a copy another process or user has already made.
- `[Inferred]` Cloud retention and deletion depend on the selected provider and account settings. Do not claim remote deletion unless the provider confirms it; surface the provider's retention and training terms before enabling that provider.
- `[To decide]` Set the retention duration for any future local cache or temporary file and decide whether users may opt into saved conversations. Recommendation: no persistent cache or saved conversation in the first connected prototype.

## 5. Credential boundary and storage options

- `[Verified]` The current AI Gaming source has no API-key field or credential persistence.
- `[Inferred]` Provider credentials must never enter the game mod, mod settings, game saves, game log, repository, source code, commits, prompts, or diagnostic bundles. The game mod must not receive a provider key from the companion.
- `[Inferred]` Windows Credential Manager can retain a per-user credential outside the game and companion files. It reduces repeated entry, but persists the secret, is Windows-specific, and requires a separately reviewed credential-management implementation in the companion.
- `[Inferred]` Companion-memory-only storage avoids persistence and clears the key when the companion exits, but requires re-entry after restart and cannot protect a key from inspection while the companion is running.
- `[Approved]` For the initial companion, keep player-supplied cloud keys in companion memory only and clear them when it exits. Do not persist keys in the mod, game saves, logs, repository, or companion storage.
- `[Approved]` OpenAI and Anthropic use player-supplied keys kept only by the companion. Check Ollama Cloud authentication requirements before starting that cloud integration; if it requires a credential, apply the same memory-only boundary. Ollama Local needs no remote provider key in the default local setup; any local endpoint authentication secret also stays outside the mod.
- `[To decide]` Confirm the companion operating systems and whether a later release may offer remembered keys through the operating system's credential store. Recommendation: memory-only first, with no key persistence until the user separately approves it.

## 6. Log and diagnostic redaction

- `[Verified]` The current 0.3.0 button writes the aggregate schema version 1 snapshot to the local game log. That log output is not sent to an AI service by the current source.
- `[Inferred]` Future local diagnostics may record only allowlisted operational metadata such as a random request correlation value that is not sent to the provider, local/cloud mode, coarse duration, and success or failure category. They must not include snapshot values unless a separate, visible local diagnostic action explicitly requests them.
- `[Inferred]` Redact or omit prompt text, model responses, account identifiers, credentials, authorization headers, full request URLs containing query secrets, raw payloads, and provider error bodies. Sanitize exception messages before logging.
- `[Inferred]` Never upload the game log, crash report, or diagnostic bundle as part of a model request. Do not log model text as an executable or trusted instruction.
- `[To decide]` Decide whether future operational diagnostics should be retained locally and for how long. Recommendation: keep them disabled by default and provide a one-click clear action if enabled.

## 7. TLS findings, provider abstraction, and data flows

### 7.1 TLS 1.2 findings from installed managed assemblies

#### Verified

- `[Verified]` Read-only metadata and IL inspection was performed on the installed game files in `C:\Program Files (x86)\Steam\steamapps\common\Cities_Skylines\Cities_Data\Managed`. The inspected DLLs were not executed, modified, or copied.
- `[Verified]` The installed `System.dll` (assembly version 2.0.0.0) contains `HttpWebRequest`, including `BeginGetResponse`, `GetResponse`, and `Abort`, plus `ServicePointManager.SecurityProtocol`.
- `[Verified]` In that `System.dll`, `SecurityProtocolType` exposes only `Ssl3=48` and `Tls=192`; it has no `Tls12` member. `SslProtocols` exposes `None=0`, `Ssl2=12`, `Ssl3=48`, `Tls=192`, and `Default=240`; it has no `Tls12` member. The inspected `SslStream.GetMonoSslProtocol` IL maps the legacy values and does not provide an explicit TLS 1.2 value mapping.
- `[Verified]` `UnityWebRequest` is in the installed `UnityEngine.dll`. Its `InternalBegin` method is marked `InternalCall`, so the network implementation is in the native Unity engine and is not available for inspection in these managed DLLs. The inspected managed type exposes no TLS-version selector.

#### Inferred and not established by DLL inspection

- `[Inferred]` The installed managed API does not provide a verified way for this mod to explicitly select TLS 1.2 through `HttpWebRequest`. A numeric enum cast is not evidence that the Mono SSL implementation will accept or negotiate TLS 1.2.
- `[Inferred]` The DLL evidence cannot prove whether the native Unity networking backend or the host operating system may negotiate TLS 1.2 by default. No TLS handshake or live network request was run. Treat cloud HTTPS from the game process as unsupported until a separate runtime test proves otherwise.
- `[Verified]` `HttpWebRequest` and asynchronous response methods exist in `System.dll`; using HTTP does not require TLS.
- `[Inferred]` Direct module-to-Ollama-Local HTTP at the default `http://localhost:11434` is technically plausible at the API level, but Cities: Skylines runtime behavior has not been tested. If implemented, all network work must run off the simulation thread with cancellation, response limits, and timeouts.

### 7.2 One provider-neutral request and response

- `[Inferred]` Route provider-specific work through one provider-neutral request shape containing a protocol version, temporary request ID, source ID, player-selected model name, player-written prompt, and the explicitly consented schemaVersion 1 snapshot fields. Do not include credentials in this request object.
- `[Inferred]` Normalize all four providers to one response shape containing the request ID, status, source ID, selected/actual model name when available, suggestion text, and a sanitized error category. The response must not contain executable instructions or game actions.
- `[Inferred]` Keep provider adapters responsible for translating the common request and response to each provider's native API. Never hard-code model names; let the player choose a model from provider availability or enter a model name, then report a missing or unavailable model without silently substituting another.
- `[Inferred]` The serialization and exact protocol version for the future companion boundary still need a separate implementation design; this proposal does not add a runtime API.

### 7.3 Four source-specific data flows

- `[Inferred]` **Ollama Local:** after per-request consent, send only the allowed snapshot and player prompt to the companion over the local bridge; the companion calls `http://localhost:11434`. Show that data stays on this computer, and enforce a loopback-only Ollama endpoint before showing that label. No cloud fallback is allowed. A direct module-to-Ollama HTTP call is technically plausible from the available `HttpWebRequest` API, but the recommended design routes it through the companion to share the provider adapter and keep networking outside the game process.
- `[Inferred]` **Ollama Cloud:** after a fresh per-request confirmation that names Ollama Cloud as the destination, the companion sends only the approved snapshot and prompt to Ollama Cloud over HTTPS. Confirm its current authentication and data-retention terms before implementation.
- `[Inferred]` **OpenAI:** after a fresh per-request confirmation that names OpenAI as the destination, the companion sends only the approved snapshot and prompt to the selected OpenAI model over HTTPS, using the player's key only inside the companion.
- `[Inferred]` **Anthropic:** after a fresh per-request confirmation that names Anthropic as the destination, the companion sends only the approved snapshot and prompt to the selected Anthropic model over HTTPS, using the player's key only inside the companion.
- `[Inferred]` No request may switch source, endpoint, or model provider automatically. A change of destination requires a new consent preview and confirmation.
- `[Approved]` Implement Ollama Local first through the companion. Add Ollama Cloud, OpenAI, and Anthropic only in a later cloud phase, after checking the current authentication requirements and terms for each provider.

### 7.4 Companion communication: loopback HTTP

- `[Verified]` The installed `System.dll` contains the `HttpWebRequest` API, and the installed `System.Core.dll` metadata contains `NamedPipeClientStream` and `NamedPipeServerStream`. These are API-presence findings only; neither transport was exercised in the game.
- `[Approved]` Use HTTP between the mod and companion, with the companion bound only to IPv4 loopback address `127.0.0.1`. Do not bind to wildcard, LAN, or IPv6 interfaces. Select an unused ephemeral port at startup and share the selected endpoint with the mod through a separately reviewed bootstrap mechanism.
- `[Approved]` Generate a fresh, unpredictable session token for every companion process start using a cryptographically secure random source. Keep it in memory, share it only with the paired mod process, and require it on every request in a dedicated HTTP header, never in a URL or query string. Do not write the token to logs or persistent configuration. Compare tokens without content-dependent timing where the runtime allows.
- `[Approved]` Reject every request that contains an `Origin` header, regardless of its value. Do not enable CORS or expose a browser-based control surface on this listener.
- `[Approved]` Apply explicit request-body and response-size bounds, per-session rate limits, and request deadlines. Reject oversized or over-limit requests before provider work begins. Exact numeric limits must be chosen before implementation.
- `[Inferred]` Loopback HTTP is simpler to inspect and support than named pipes, but a local listener can still be targeted by another process on the same computer. The token reduces access by unauthenticated local clients; it does not protect against malware or a process that can read the mod or companion memory.
- `[Inferred]` Named pipes avoid a TCP listener and can use operating-system access controls, especially on Windows, but are less portable and their behavior with the game's Mono runtime remains unverified. They are not the selected initial transport.
- `[To decide]` Specify how the companion securely gives the random port and session token to the mod at startup. Do not use an unauthenticated discovery endpoint that reveals the token, and do not put the token in a URL, log, or persistent file. Recommendation: define and review a narrow local bootstrap handshake before implementing either process.

### 7.5 Companion publication and trust

- `[Approved]` The companion is a separate desktop application, not a Steam Workshop mod. Do not distribute it through Workshop.
- `[Inferred]` Prefer an official GitHub Releases page for versioned companion installers and release notes, with a published SHA-256 checksum for each artifact. Keep the release source and checksum reachable from the project website without making the game mod download or launch the companion automatically.
- `[Inferred]` Offer a manual install and update flow: the player downloads the release from the official project page, verifies the publisher signature when available or the published checksum, installs or replaces the companion, and starts it explicitly. Do not require administrator privileges unless a separately justified installer feature needs them. Keep provider credentials out of installer configuration and update metadata.
- `[To decide]` Choose the final companion release channel and installer format. Recommendation: GitHub Releases, with Workshop excluded.
- `[To decide]` Decide whether Windows builds will receive Authenticode code signing. Recommendation: sign releases when a maintainable certificate and signing process are available; otherwise publish checksums and build provenance, and clearly show that the binary is unsigned.
- `[Inferred]` A new executable that opens a local listener may trigger antivirus or reputation warnings, especially before it has an established signing and distribution history. Publish source and checksums, document the limited loopback-only behavior, submit suspected false positives to the relevant vendor, and do not tell players to disable antivirus protections.
- `[Approved]` If the companion is missing or not running, the mod must report that model access is unavailable and make no model request. Existing read-only snapshot and discovery features remain available; the mod must not download or install the companion automatically and must not fall back to a direct network connection.
- `[To decide]` Confirm supported operating systems, installation/update ownership, and whether the player starts the companion manually or through an explicitly approved launcher integration. Recommendation: support Windows first and require the player to start and update it manually.

### 7.6 Local and cloud risks

- `[Inferred]` Local Ollama avoids sending the approved payload to a cloud model only when the endpoint is verified as loopback. A misconfigured remote endpoint, another local process, or local malware can expose the request; local processing does not guarantee privacy.
- `[Inferred]` Each cloud provider receives the approved prompt and snapshot and may also receive account, network, timing, or service metadata. Retention, training use, data region, and account linkage depend on the provider and the player's settings.
- `[To decide]` Before enabling any cloud source, confirm its endpoint, authentication method, retention, training, region, and account-linkage terms and display the named destination before every request.

## 8. Failure and safe degradation

- `[Approved]` **Companion missing or not running:** report that model access is unavailable and make no model request. Keep read-only snapshot and interop-discovery features available; do not auto-download, install, or start the companion, and do not fall back to a direct provider connection.
- `[Inferred]` **Ollama Local not started:** show that the local service is unavailable; do not switch to Ollama Cloud or another provider. If local authentication is configured and rejected, request correction without logging the credential. Local quota exhaustion is not applicable, but resource exhaustion should return a bounded local error.
- `[Inferred]` **Ollama Cloud unavailable or key rejected:** show the provider and sanitized authentication/unavailable category. Confirm whether a credential is required before implementation; never log it.
- `[Inferred]` **OpenAI or Anthropic key invalid:** keep the key inside the companion, report a sanitized authentication failure, and let the player re-enter it there. Never echo the key into the mod or logs.
- `[Inferred]` **Cloud quota or usage limit reached:** report the provider limit and stop. Do not silently change providers or consume another provider's key.
- `[Inferred]` **Request timeout:** abort when possible, clear pending local data, ignore late responses, and return to read-only play. A retry requires a new player confirmation.
- `[Inferred]` **Model missing or unavailable:** identify the selected provider/model and ask the player to choose an available name or install/pull it themselves. Do not hard-code a replacement, run a shell command, or switch provider automatically.
- `[Inferred]` All failures, including invalid or oversized responses, must leave the game simulation unchanged and must not block the UI or simulation thread. Log only a sanitized error category; never log credentials, request payloads, prompts, or provider error bodies.

## 9. Decisions before implementation

- `[To decide]` Confirm the exact payload scope and whether any fields may be added after schema version 1. Recommendation: no additional city or player data in the first connected prototype; any new snapshot field must be game-verified and require a schema-version increment.
- `[To decide]` Select the player-facing UI location for the request preview, per-request confirmation, revocation, and suggestion display.
- `[To decide]` Select the companion runtime and supported operating systems; design the secure port/token bootstrap; and set numeric request/response size, rate, and deadline limits.
- `[To decide]` Before the cloud phase, verify Ollama Cloud authentication and each provider's current retention, training, data-region, account-linkage, and credential requirements. Do not enable a cloud adapter until those findings are documented and reviewed.
- `[To decide]` Confirm the companion release channel, installer/update flow, and code-signing decision described in section 7.5.
- `[To decide]` Set retention and deletion behavior for temporary request data. Recommendation: no persistent prompt, snapshot, or response history in the initial rollout.
- `[To decide]` Define how a future action request is displayed and confirmed per operation, while keeping the accepted interop contract's Write rules as the maximum authority.

## 10. Approval boundary

- `[Verified]` This document is a design proposal only. AI Gaming 0.3.0 remains a local, read-only preview with no model connection or outbound data transfer.
- `[Inferred]` Before implementation, resolve the decisions above, prepare a separate scoped plan, and obtain approval. Any future change to the accepted interop contract follows its governance rule and is outside this document's scope.
