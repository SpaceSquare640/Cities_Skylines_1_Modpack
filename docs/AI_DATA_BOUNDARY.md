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
- `[Approved]` Support Windows first. Keep key persistence disabled; any later credential-store option requires separate approval.

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
- `[Inferred]` Direct module-to-Ollama-Local HTTP at the default `http://localhost:11434` is technically plausible at the API level, but Cities: Skylines runtime behavior has not been tested. If implemented, all network work must run off the simulation thread with cancellation and timeouts. The selected design routes it through the companion.

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

### 7.4 Companion HTTP authentication and token handoff (proposal; approval required)

- `[Approved]` Use HTTP bound only to IPv4 loopback address `127.0.0.1`, on an OS-assigned ephemeral port that remains bound for the listener's lifetime. Reject every request containing an `Origin` header; do not enable CORS. Never bind to wildcard, LAN, or IPv6 interfaces.
- `[Inferred]` Generate a fresh 32-byte (256-bit) session secret with the operating system's cryptographically secure random generator at every companion start. Its unpadded base64url form is 43 characters. Keep the secret only in the memory of the companion and paired game process. It expires when either process exits; a later start must generate a different secret.
- `[Inferred]` Do not send the reusable secret itself in HTTP. For each request, the mod should send a fixed-length HMAC-SHA-256 proof derived from the session secret and a canonical message containing the protocol version, edition, sequence number, HTTP method, route, and request-body hash. The companion should compare the proof in constant time and reject a repeated or out-of-order sequence number. Authenticate companion responses with the same session-bound approach so another local process cannot forge a response. This prevents guessing and replay of an already accepted request proof; it does not protect against malware that can inspect either process's memory or inject into the game.
- `[Inferred]` HMAC authenticates requests but does not encrypt loopback HTTP. A privileged local traffic observer may read the request body and could race a valid proof before the companion accepts it. If that adversary is in scope, pinned local TLS or an authenticated OS IPC transport is needed; either would change the selected bridge and requires a separate decision. This residual risk must be shown before the player enables a connection.
- `[Inferred]` Proposed handoff: only after explicit player consent and the enabled auto-start preference, AI Gaming starts the matching companion as a child process with `UseShellExecute=false` and private redirected standard streams backed by anonymous pipes. The companion binds its loopback listener, creates the session secret, and returns a single startup handshake over the private pipe containing the edition, companion build, protocol version, port, and secret. The mod reads it from that pipe and keeps it in memory. Never pass the secret through files, logs, URLs, environment variables, or command-line arguments; do not mirror the handshake to console output.
- `[Inferred]` The companion should accept requests only for a session created through this mod-launched handshake, and should verify the parent process is the expected Cities: Skylines Windows process plus the exact edition/build/protocol values. Other ordinary processes that did not receive the private pipe handle cannot derive the session proof. This is not hardware-backed attestation: a process with same-user debugging/injection access, administrator rights, or control of the game process can bypass the boundary.
- `[To decide]` Approve or revise the 32-byte secret, HMAC/sequence anti-replay protocol, private anonymous-pipe handoff, Windows parent-process checks, and residual local HTTP visibility risk before any implementation. The .NET/Mono process-redirection behavior must first pass a focused runtime probe; if it does not, present a replacement handoff design for approval rather than falling back to a file, URL, log, or command-line secret.

### 7.5 Workshop distribution and update lifecycle

- `[Verified, checked 2026-10-22]` The Skyve v4.0 Stable Cities: Skylines Workshop page describes an external `Skyve.exe` included under the item's `App` directory in the Workshop content folder. The page says the app is copied rather than installed and does not execute without user input. The same page currently displays a Steam notice that the item was removed for violating Community & Content Guidelines, but it does not state the reason. This is evidence of the package layout and maintainer-described launch flow, not proof that Valve or the game publisher approved this pattern.
- `[Verified, checked 2026-10-22]` Steamworks' general Workshop implementation guide describes an item as a folder of files. It says the Steam client automatically downloads and updates subscribed Workshop content; on an app launch, existing items that need updates are installed before the app launches, while newly subscribed items may download in the background. The guide asks the consuming app to load the file formats it expects.
- `[Verified, checked 2026-10-22]` The Steam Subscriber Agreement allows app-specific Workshop terms and says Valve may restrict or remove Workshop contributions. I found no general executable-extension prohibition in the official Steamworks Workshop pages and Subscriber Agreement reviewed for this update. That absence is not affirmative permission; CS1/Paradox app-specific rules and approval to launch a Workshop-bundled executable remain to be confirmed before public release.
- `[Approved]` The Release Workshop item must contain its matching Release companion, and the Development Workshop item must contain its matching Development companion. Steam subscription supplies and updates the files; Steam does not launch the companion. AI Gaming may launch it only after player consent.
- `[Inferred]` Do not execute the companion directly from the Steam Workshop directory. On a consented launch, copy the matching package into an immutable, versioned per-user cache under `%LOCALAPPDATA%\SpaceSquare640\AI Gaming\Companion\<edition>\<version>-<sha256>\`, verify the SHA-256, and launch that cache copy. Use a separate cache namespace for Release and Development. Never overwrite a running version; retain its files until the process exits. This prevents Steam's Workshop update from trying to replace the executable currently in use.
- `[Inferred]` Each mod build should pin its expected companion edition, companion build/version, protocol version, and SHA-256. The companion startup handshake must report those values, and the mod must refuse all HTTP requests if any value differs. If Steam has updated the Workshop package while the current game process still has an older mod loaded, use only a matching already-cached companion; otherwise report a version mismatch and ask the player to restart/update. Do not kill an unrelated or mismatched process automatically.
- `[Approved]` Provide an explicit settings-page preference to disable automatic companion launch. Require an affirmative first-use consent before enabling launch; recommendation: the preference starts disabled. If disabled, AI Gaming never starts the executable.
- `[To decide]` Approve the versioned `%LOCALAPPDATA%` cache and mismatch/restart flow before implementation, including how stale cache versions are identified and safely cleaned up after no process is using them.

#### Official-source references (checked 2026-10-22)

- Steamworks, Steam Workshop Implementation Guide: https://partner.steamgames.com/doc/features/workshop/implementation
- Steamworks, Steam Workshop overview: https://partner.steamgames.com/doc/features/workshop
- Valve, Steam Subscriber Agreement: https://store.steampowered.com/subscriber_agreement/
- Skyve v4.0 Stable Workshop item (maintainer-authored description): https://steamcommunity.com/workshop/filedetails/?id=2881031511

### 7.6 Unsigned releases and player-facing notices

- `[Approved]` Do not Authenticode-sign the companion. State clearly in both Release and Development Workshop descriptions and on the AI Gaming website that the companion executable is unsigned. Publish a separate SHA-256 and source commit for each edition's exact companion build so players can compare the downloaded file and inspect its source revision.
- `[Inferred]` A hash and source commit aid independent comparison but are not a publisher signature; if the same distribution account is compromised, both the executable and posted hash could be replaced. Keep the commit and hash visible in the item and website copy, and do not describe an unsigned build as publisher-authenticated.
- `[Inferred]` Unsigned executables may trigger antivirus or Windows reputation warnings, especially for a new project. Explain why the companion exists and that its listener is loopback-only; do not tell players to disable antivirus. A false-positive report should go to the security vendor with the exact hash and source commit.
- `[Inferred]` Recommended player notice for each Workshop item and the website: "This companion is unsigned. SHA-256: <edition-specific hash>. Source commit: <edition-specific commit>. Set a usage or spending limit with your cloud provider before using cloud models. AI Gaming does not limit request size or frequency. Local Ollama usage is not covered by a cloud provider quota."

### 7.7 Request usage and local/cloud risks

- `[Approved]` The mod and companion will not enforce request-size or request-frequency limits. Before cloud use, tell players to configure usage/spending caps in the selected provider's account dashboard where available. These provider caps do not constrain local Ollama usage or protect the companion from unusually large local requests.
- `[Inferred]` With no local size or frequency caps, large or repeated requests can consume memory, CPU, and provider usage. Provider quotas may limit cloud charges if correctly configured, but do not prevent local resource exhaustion and may not apply to every provider/account setup. Keep per-request consent, timeouts, schema validation, and no automatic retry; these do not replace usage caps.
- `[Inferred]` Local Ollama avoids sending the approved payload to a cloud model only when the endpoint is verified as loopback. A misconfigured remote endpoint, another local process, or local malware can expose the request; local processing does not guarantee privacy.
- `[Inferred]` Each cloud provider receives the approved prompt and snapshot and may also receive account, network, timing, or service metadata. Retention, training use, data region, and account linkage depend on the provider and the player's settings.
- `[To decide]` Before enabling any cloud source, confirm its endpoint, authentication method, retention, training, region, and account-linkage terms and display the named destination before every request.

## 8. Failure and safe degradation

- `[Approved]` **Companion missing or not running:** report that model access is unavailable and make no model request. Keep read-only snapshot and interop-discovery features available; Steam subscription is the distribution path. The mod may start the matching installed companion only after consent and while auto-start is enabled; it must not download the companion through another channel or fall back to a direct provider connection.
- `[Inferred]` **Ollama Local not started:** show that the local service is unavailable; do not switch to Ollama Cloud or another provider. If local authentication is configured and rejected, request correction without logging the credential. Local quota exhaustion is not applicable, but resource exhaustion should return a bounded local error.
- `[Inferred]` **Ollama Cloud unavailable or key rejected:** show the provider and sanitized authentication/unavailable category. Confirm whether a credential is required before implementation; never log it.
- `[Inferred]` **OpenAI or Anthropic key invalid:** keep the key inside the companion, report a sanitized authentication failure, and let the player re-enter it there. Never echo the key into the mod or logs.
- `[Inferred]` **Cloud quota or usage limit reached:** report the provider limit and stop. Do not silently change providers or consume another provider's key.
- `[Inferred]` **Request timeout:** abort when possible, clear pending local data, ignore late responses, and return to read-only play. A retry requires a new player confirmation.
- `[Inferred]` **Model missing or unavailable:** identify the selected provider/model and ask the player to choose an available name or install/pull it themselves. Do not hard-code a replacement, run a shell command, or switch provider automatically.
- `[Inferred]` All failures, including malformed responses, must leave the game simulation unchanged and must not block the UI or simulation thread. Log only a sanitized error category; never log credentials, request payloads, prompts, or provider error bodies.

## 9. Decisions before implementation

- `[To decide]` Confirm the exact payload scope and whether any fields may be added after schema version 1. Recommendation: no additional city or player data in the first connected prototype; any new snapshot field must be game-verified and require a schema-version increment.
- `[To decide]` Select the player-facing UI location for the request preview, per-request confirmation, revocation, and suggestion display.
- `[Approved]` Support Windows first. No built-in request-size or request-frequency limits; player-facing copy must tell users to configure provider-side cloud usage caps where available.
- `[To decide]` Approve the session-token/HMAC and private-pipe bootstrap proposal, plus the Workshop versioned-cache and mismatch/restart flow, before implementation.
- `[To decide]` Before the cloud phase, verify Ollama Cloud authentication and each provider's current retention, training, data-region, account-linkage, and credential requirements. Do not enable a cloud adapter until those findings are documented and reviewed.
- `[To decide]` Obtain confirmation of CS1/Paradox app-specific rules for including and launching an executable in the Workshop item before public release.
- `[To decide]` Define final player-facing usage-cap text for both Workshop items and the website; it must state that the companion is unsigned and include the edition-specific SHA-256 and source commit.
- `[To decide]` Set retention and deletion behavior for temporary request data. Recommendation: no persistent prompt, snapshot, or response history in the initial rollout.
- `[To decide]` Define how a future action request is displayed and confirmed per operation, while keeping the accepted interop contract's Write rules as the maximum authority.

## 10. Approval boundary

- `[Verified]` This document is a design proposal only. AI Gaming 0.3.0 remains a local, read-only preview with no model connection or outbound data transfer.
- `[Inferred]` Before implementation, resolve the decisions above, prepare a separate scoped plan, and obtain approval. Any future change to the accepted interop contract follows its governance rule and is outside this document's scope.
