# AI Gaming Data Boundary and Consent Design

**Status:** Implementation record for the approved 0.4.0 Ollama Local companion milestone. The source implementation and framework-dependent Release builds have been completed; in-game behavior and the Windows x64 self-contained publish have not been verified. This status does not authorize cloud providers, credentials, extra data fields, or game control.

**Evidence labels:** `[Verified]` describes facts confirmed from the current branch, static assembly inspection, or completed static build. `[Approved]` records a design decision explicitly approved by the user. `[Inferred]` marks a proposal that still needs implementation review and runtime validation. `[To decide]` marks an unresolved choice for the user and Lead Architect.

## 1. Confirmed baseline and design gate

- `[Verified]` AI Gaming 0.3.0 added a read-only `schemaVersion=1` city snapshot log action. AI Gaming 0.4.0 adds a separate player-confirmed Ollama Local request path through the companion; it does not send request payloads or model responses to the game log.
- `[Verified]` The current allowlist contains only `gameDate`, `population`, `fundsCents`, `residentialDemand`, `commercialDemand`, and `workplaceDemand`, in addition to the schema version.
- `[Verified]` The 0.4.0 source connects only to an opt-in companion over authenticated IPv4 loopback HTTP, and the companion connects only to Ollama Local at `localhost:11434` through an IPv4 loopback socket. It has no cloud provider adapter, API-key field or persistence, save-file write, Harmony patch, or simulation-state mutation. Neither 0.3.0 nor 0.4.0 has been tested in Cities: Skylines.
- `[Inferred]` Any future model request must use an explicit, versioned allowlist derived from schema version 1. A new field requires a documented source, type, unit, and in-game UI comparison, user approval, and a schema-version increment before it can be sent.
- `[Inferred]` A future data-export milestone requires its own implementation plan and approval. This proposal alone does not approve transmission or model access.

## 2. Outbound data boundary

### 2.1 Schema version 1 allowlist

- `[Verified]` `schemaVersion`: integer value `1`; identifies the snapshot definition.
- `[Verified]` `gameDate`: string formatted as `yyyy-MM-dd`; sourced from the current game date.
- `[Verified]` `population`: unsigned integer; sourced from the HUD population value at district 0, `m_populationData.m_finalCount`.
- `[Verified]` `fundsCents`: 64-bit integer in cents; sourced from `EconomyManager.playerMoney`. Static IL inspection confirmed the getter multiplies `LastCashAmount` by 100. In-game HUD equality is not yet verified.
- `[Verified]` `residentialDemand`, `commercialDemand`, and `workplaceDemand`: integer values sourced from the corresponding `ZoneManager` demand fields used by the game information panel and tooltips. In-game value and label matching is not yet verified.
- `[Verified]` The 0.4.0 settings page exposes one checkbox per schemaVersion 1 field, all checked by default, plus a Select All control. The player may uncheck any subset; only selected values are serialized. The player-written question and selected model name are also shown in a preview and sent after a separate confirmation.

### 2.2 Prohibited data

- `[Verified]` The mod does not read or attach a city name, save file or save contents, citizen personal or identifying data, citizen or object lists, raw map data, screenshots, player account data, or arbitrary files to the request. The free-text player question is not automatically scanned or filtered; the player must review it and must not include prohibited data or secrets.
- `[Inferred]` Never send player account data, Steam account names or identifiers, platform identifiers, device identifiers, or other account-linked identifiers.
- `[Inferred]` Never send API keys, access tokens, authentication headers, passwords, private prompts containing credentials, or other secrets.
- `[Inferred]` Never send game logs, crash reports, mod configuration files, arbitrary files, or data collected from another mod unless a separately approved schema explicitly defines the field and its provider has authorized that use.
- `[Inferred]` Never treat model-generated code, commands, URLs, or text as permission to read files, run code, or change the game.
- `[Approved]` The 0.4.0 request is limited to player-selected schemaVersion 1 values, the entered model name, and the player-written question shown in the consent preview. Any additional game field requires source/type/unit verification, in-game UI verification, explicit approval, and a schema-version increment.

## 3. Player consent and revocation

- `[Verified]` Every local request has a separate prepare-and-review step followed by a distinct confirm action. The preview identifies Ollama Local at `http://localhost:11434`, the model name, exact selected snapshot values, and the exact question. The mod captures selected values asynchronously on the simulation thread, then returns to the UI thread; it does not wait for capture in the UI.
- `[Verified]` Companion auto-start permission defaults off and resets on game restart. Even when enabled, the companion starts only after the player confirms that request; this permission is not blanket consent to send future data.
- `[Verified]` The companion transport is fixed to loopback. The Ollama connector accepts only `localhost:11434` and opens an IPv4 socket to `127.0.0.1`; it does not use a proxy or redirect. The selected model must also appear in the local model list with a positive local size; cloud model tags are rejected.
- `[Inferred]` Do not start a background request or retry after failure without a new, explicit confirmation. Do not send a later snapshot under consent given for an earlier snapshot.
- `[Inferred]` If the player revokes consent before transmission, discard the pending request. If transmission has started, attempt cancellation, discard any later response, and do not initiate game actions. Explain that data already delivered to a provider cannot necessarily be recalled.
- `[Inferred]` A returned answer is an untrusted suggestion only. Displaying, copying, or dismissing it must not change the simulation.
- `[Verified]` The accepted Modpack interop contract is `contractVersion 1.0`; its Write capability rules are recorded in `共通參考\Modpack 互通契約 v1.md` in the project notes.
- `[Inferred]` Any future game operation requires a separate player confirmation for that operation and must stay within the accepted interop contract's Write rules, a typed action allowlist, current-state validation, and action limits. A model response or earlier data-sharing consent cannot grant that permission.
- `[To decide]` Decide how the UI will present cancellation after a request has left the game, including whether it offers a provider-side deletion request. Recommendation: provide a local cancel control and clearly state that cancellation cannot guarantee deletion of data already received remotely.

## 4. Retention and deletion

- `[Verified]` The request snapshot and response are held in process memory for the active request/UI display. No conversation history, prompt, snapshot, or response is persisted by the mod or companion. The companion exits when its paired process closes the input pipe.
- `[Inferred]` Do not write prompts, responses, snapshots, or credentials into game saves, the mod directory, project files, source control, or logs.
- `[Verified]` The only persistent data created by this implementation is the verified companion executable staged under `%LOCALAPPDATA%\SpaceSquare640\AI Gaming\Companion\<edition>\<version>-<sha256>\`. Request payloads are not written there. Old version directories are not automatically deleted; no cache cleanup is implemented.
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

- `[Verified]` The separate 0.3.0 city-summary button writes an aggregate schema version 1 line to the local game log. The 0.4.0 AI request path does not write selected values, prompt text, model output, credentials, session tokens, or provider error bodies to the game log.
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
- `[Verified]` The local 0.4.0 bridge encoding and HMAC/session protocol are documented in `docs/COMPANION_PROTOCOL.md`. A shared request/response abstraction across all four model sources remains a future design item.

### 7.3 Four source-specific data flows

- `[Verified]` **Ollama Local:** after per-request consent, send only the selected snapshot fields and player question to the companion over the local bridge. The companion queries `http://localhost:11434/api/tags`, accepts only a selected model listed with a positive local size, rejects cloud-tagged names, then calls `/api/chat`. Its socket is forced to `127.0.0.1`; no provider fallback is implemented. Ollama documents that cloud features can be disabled with `OLLAMA_NO_CLOUD=1` or `disable_ollama_cloud=true`; the mod does not modify or inspect the player's Ollama configuration.
- `[Inferred]` **Ollama Cloud:** after a fresh per-request confirmation that names Ollama Cloud as the destination, the companion sends only the approved snapshot and prompt to Ollama Cloud over HTTPS. Confirm its current authentication and data-retention terms before implementation.
- `[Inferred]` **OpenAI:** after a fresh per-request confirmation that names OpenAI as the destination, the companion sends only the approved snapshot and prompt to the selected OpenAI model over HTTPS, using the player's key only inside the companion.
- `[Inferred]` **Anthropic:** after a fresh per-request confirmation that names Anthropic as the destination, the companion sends only the approved snapshot and prompt to the selected Anthropic model over HTTPS, using the player's key only inside the companion.
- `[Inferred]` No request may switch source, endpoint, or model provider automatically. A change of destination requires a new consent preview and confirmation.
- `[Approved]` Implement Ollama Local first through the companion. Add Ollama Cloud, OpenAI, and Anthropic only in a later cloud phase, after checking the current authentication requirements and terms for each provider.

#### Ollama local/cloud distinction checked 2026-10-25

- `[Verified]` Ollama's cloud documentation distinguishes app/CLI cloud models from local API usage and states that cloud model inference runs on Ollama's servers. Its FAQ says cloud features can be disabled with `OLLAMA_NO_CLOUD=1` or `disable_ollama_cloud=true`, followed by an Ollama restart.
- `[Verified]` The local `GET /api/tags` documentation returns installed model entries including a `size` field; the local `/api/chat` endpoint accepts a model name and message list. AI Gaming now requires a listed entry with a positive size and rejects cloud-tag suffixes before sending a prompt.
- `[Inferred]` The local model list and suffix check are a practical guard against choosing documented cloud aliases, but they do not inspect the user's Ollama server configuration or prove behavior for every future Ollama version. The player-facing notice recommends disabling Ollama cloud features for additional assurance.
- Official sources, checked 2026-10-25: https://docs.ollama.com/cloud ; https://docs.ollama.com/faq ; https://docs.ollama.com/api/tags ; https://docs.ollama.com/api/chat

### 7.4 Companion HTTP authentication and token handoff (approved and implemented; runtime validation pending)

- `[Approved]` Use HTTP bound only to IPv4 loopback address `127.0.0.1`, on an OS-assigned ephemeral port that remains bound for the listener's lifetime. Reject every request containing an `Origin` header; do not enable CORS. Never bind to wildcard, LAN, or IPv6 interfaces.
- `[Approved]` Generate a fresh 32-byte (256-bit) session secret with an operating-system cryptographically secure random generator at every companion start. Keep it only in the companion and paired game process memory, rotate it on every start, and expire it when either process exits.
- `[Approved]` Never send the reusable secret itself in HTTP. Each request carries a fixed-length HMAC-SHA-256 proof over a canonical message containing protocol version, edition, sequence number, HTTP method, route, and request-body hash. Compare proofs in constant time and reject repeated or out-of-order sequence numbers. Authenticate companion responses with the same session-bound approach. Never pass the secret through files, logs, URLs, environment variables, or command-line arguments.
- `[Verified]` After request confirmation and only when the setting is enabled, AI Gaming starts the matching companion as a child process. Redirected standard input/output anonymous pipes carry a bootstrap and a readiness message containing edition, version, protocol, hash, ephemeral port, process identities, and the 32-byte secret. These streams are not attached to a visible console and the message is not logged. The companion checks its actual parent PID, the `Cities.exe` image path/name, its own executable SHA-256, and exact edition/version/protocol agreement. It refuses the session on a mismatch.
- `[Approved]` Threat boundary: HMAC and sequence checks prevent guessing, request forgery without the secret, and replay of accepted requests; they do not encrypt loopback HTTP. The design does not defend against local privileged software that can read or inject into the game or companion process, inspect their memory, or control the game process. Such software may observe payloads, steal the in-memory secret, forge responses, or bypass the checks. The player must see this limitation before enabling a connection.
- `[Verified]` A Windows CLR probe verified a redirected pipe round trip with a small non-secret payload. The game's Unity Mono process startup and handshake behavior remains unverified. The source has no file, URL, log, environment-variable, or command-line token fallback.
- `[Inferred]` PID/path checks are not publisher-signature authentication. A same-user process able to run the companion itself can establish its own session; it cannot obtain a game snapshot unless it can already inspect or control the game process. This residual identity boundary needs review before public distribution.

### 7.5 Workshop distribution and update lifecycle

- `[Verified, checked 2026-10-22]` The Skyve v4.0 Stable Cities: Skylines Workshop page describes an external `Skyve.exe` included under the item's `App` directory in the Workshop content folder. The page says the app is copied rather than installed and does not execute without user input. The same page currently displays a Steam notice that the item was removed for violating Community & Content Guidelines, but it does not state the reason. The removal reason is unknown; Skyve is not a precedent for AI Gaming's distribution or launch design.
- `[Verified, checked 2026-10-22]` Steamworks' general Workshop implementation guide describes an item as a folder of files. It says the Steam client automatically downloads and updates subscribed Workshop content; on an app launch, existing items that need updates are installed before the app launches, while newly subscribed items may download in the background. The guide asks the consuming app to load the file formats it expects.
- `[Verified, checked 2026-10-22]` The Steam Subscriber Agreement allows app-specific Workshop terms and says Valve may restrict or remove Workshop contributions. I found no general executable-extension prohibition in the official Steamworks Workshop pages and Subscriber Agreement reviewed for this update. That absence is not affirmative permission; CS1/Paradox app-specific rules and approval to launch a Workshop-bundled executable remain to be confirmed before public release.
- `[Approved]` The Release Workshop item must contain its matching Release companion, and the Development Workshop item must contain its matching Development companion. Steam subscription supplies and updates the files; Steam does not launch the companion. AI Gaming may launch it only after player consent.
- `[Approved]` Before any public release, first upload the companion-bearing build to the hidden Development Workshop item and observe whether the upload is blocked or the item is removed. This is an empirical viability check, not policy approval. If Workshop distribution is blocked, removed, or otherwise not viable, publish the companion through GitHub Releases instead. Only the installation source changes: companion discovery, edition/version/hash validation, opt-in launch, authentication, and loopback connection behavior remain the same.
- `[Verified]` On a consented launch the mod checks the Workshop-side companion hash, stages it under the edition/version/hash cache path, rechecks the copied hash, and starts only the staged copy. It never overwrites a versioned cache file and does not delete old cache versions. Steam update behavior and locked-file recovery have not been tested in game.
- `[Verified]` The mod build carries companion edition/version/protocol/hash values. A hash value of `UNCONFIGURED` deliberately fails closed. Packaging must publish the matching self-contained companion, calculate its SHA-256, and pass that exact value into the mod build; edition, version, protocol, or hash mismatch refuses launch.
- `[Verified]` The settings page exposes a companion-start checkbox that defaults off and resets when the game restarts. If it is off, the mod never starts the executable. Each request still requires a separate preview and confirmation.
- `[Approved]` The versioned `%LOCALAPPDATA%` cache is approved. Automatic stale-cache cleanup remains disabled; any cleanup implementation requires a separate plan and approval.

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
- `[Verified]` AI Gaming fixes the Ollama endpoint to local IPv4 loopback, requires the selected model to appear in `/api/tags` with a positive local size, and rejects names ending in `:cloud` or `-cloud`. It does not inspect or change Ollama's cloud configuration. The player may disable Ollama cloud features for an additional server-side guard; the UI explains this limitation.
- `[Inferred]` Each cloud provider receives the approved prompt and snapshot and may also receive account, network, timing, or service metadata. Retention, training use, data region, and account linkage depend on the provider and the player's settings.
- `[To decide]` Before enabling any cloud source, confirm its endpoint, authentication method, retention, training, region, and account-linkage terms and display the named destination before every request.

## 8. Failure and safe degradation

- `[Verified]` **Companion missing, hash unconfigured/mismatched, cache unavailable, launch failure, or identity/version mismatch:** report a localized category and send no request. Keep snapshot logging and interop discovery available. No direct model fallback or retry is attempted.
- `[Verified]` **Ollama Local not started:** return a sanitized `ollama_unavailable` result. **Cloud model name or model not installed locally:** return `local_model_required` or `model_not_found`. **Authentication rejected:** return `local_authentication_failed` without logging authentication details. **Timeout, malformed response, or other provider error:** return a category only and do not retry.
- `[Inferred]` **Ollama Cloud unavailable or key rejected:** show the provider and sanitized authentication/unavailable category. Confirm whether a credential is required before implementation; never log it.
- `[Inferred]` **OpenAI or Anthropic key invalid:** keep the key inside the companion, report a sanitized authentication failure, and let the player re-enter it there. Never echo the key into the mod or logs.
- `[Inferred]` **Cloud quota or usage limit reached:** report the provider limit and stop. Do not silently change providers or consume another provider's key.
- `[Inferred]` **Request timeout:** abort when possible, clear pending local data, ignore late responses, and return to read-only play. A retry requires a new player confirmation.
- `[Inferred]` **Model missing or unavailable:** identify the selected provider/model and ask the player to choose an available name or install/pull it themselves. Do not hard-code a replacement, run a shell command, or switch provider automatically.
- `[Inferred]` All failures, including malformed responses, must leave the game simulation unchanged and must not block the UI or simulation thread. Log only a sanitized error category; never log credentials, request payloads, prompts, or provider error bodies.

## 9. Remaining decisions and release gates

- `[Approved]` The 0.4.0 request uses only player-selected schemaVersion 1 values, the entered model name, and the player-written question. The question is free text and is not scanned; the player must not enter secrets or prohibited data.
- `[Verified]` Request preview, per-request confirmation, cancellation, and suggestion display are on the settings page. Cancellation aborts the current request where possible; data already accepted by Ollama cannot be recalled.
- `[Approved]` Support Windows first. No built-in request-size or request-frequency limits; player-facing copy must tell users to configure provider-side cloud usage caps where available.
- `[Approved]` The 32-byte CSPRNG secret, per-start rotation, anonymous-pipe handoff, per-request HMAC and sequence anti-replay checks, constant-time comparison, process/version identity checks, and versioned `%LOCALAPPDATA%` cache direction are approved and present in source. Only static builds and a Windows CLR pipe probe have been performed; game-runtime behavior remains pending.
- `[To decide]` Before the cloud phase, verify Ollama Cloud authentication and each provider's current retention, training, data-region, account-linkage, and credential requirements. Do not enable a cloud adapter until those findings are documented and reviewed.
- `[To decide]` Before public release, confirm CS1/Paradox app-specific rules for including and launching an executable. The hidden Development upload check is required but does not establish policy approval.
- `[Approved]` Before public release, test the companion package on the hidden Development Workshop item. If Workshop is blocked, removed, or otherwise not viable, use GitHub Releases as the companion distribution source without changing the mod's detection and connection protocol.
- `[To decide]` Define final player-facing usage-cap text for both Workshop items and the website; it must state that the companion is unsigned and include the edition-specific SHA-256 and source commit.
- `[Verified]` No prompt, snapshot, or response history is persisted. The separate companion executable cache is versioned and immutable; old entries are not automatically deleted.
- `[To decide]` Define how a future action request is displayed and confirmed per operation, while keeping the accepted interop contract's Write rules as the maximum authority.

## 10. Approval boundary

- `[Verified]` AI Gaming 0.4.0 implements the approved Ollama Local-only request path. It has not been tested in Cities: Skylines, and the self-contained win-x64 publish has not been produced or verified in this environment.
- `[Approved]` The approved implementation does not include cloud adapters, API-key entry/storage, credential persistence, gameplay actions, or interop capability calls. Those require later plans and approval; check Ollama Cloud authentication and each provider's terms before a cloud phase.


---

Implementation update signed by OpenAI-Codex.
