# AI Gaming Data Boundary and Consent Design

**Status:** Proposal for a future milestone. This document does not authorize or implement an AI connection, data export, credential handling, or game control.

**Evidence labels:** `[Verified]` describes facts confirmed from the current branch, static assembly inspection, or completed static build. `[Inferred]` marks a proposed design that still needs implementation review and runtime validation. `[To decide]` marks a choice for the user and Lead Architect.

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
- `[Inferred]` Before sending, show the model destination and type (local or named cloud provider), purpose, exact snapshot fields, and the exact player-written question or prompt. Require a distinct confirmation for that request.
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

## 5. Credential boundary

- `[Verified]` The current AI Gaming source has no API-key field or credential persistence.
- `[Inferred]` A cloud credential must never enter the game mod, game save, mod settings, game log, project repository, source file, commit, prompt, or diagnostic bundle.
- `[Inferred]` Keep any future cloud credential in the companion process, using an operating-system credential store where available. The game mod must not receive the credential; the companion must not return it in a response or error.
- `[Inferred]` A local model may need no credential when its service is unauthenticated. If authentication is required, keep that credential within the companion's protected store and apply the same exclusions.
- `[To decide]` Select the supported companion operating systems and their credential-store mechanism before implementing cloud access. Recommendation: do not add a cloud provider until protected storage and credential redaction are verified on each supported system.

## 6. Log and diagnostic redaction

- `[Verified]` The current 0.3.0 button writes the aggregate schema version 1 snapshot to the local game log. That log output is not sent to an AI service by the current source.
- `[Inferred]` Future local diagnostics may record only allowlisted operational metadata such as a random request correlation value that is not sent to the provider, local/cloud mode, coarse duration, and success or failure category. They must not include snapshot values unless a separate, visible local diagnostic action explicitly requests them.
- `[Inferred]` Redact or omit prompt text, model responses, account identifiers, credentials, authorization headers, full request URLs containing query secrets, raw payloads, and provider error bodies. Sanitize exception messages before logging.
- `[Inferred]` Never upload the game log, crash report, or diagnostic bundle as part of a model request. Do not log model text as an executable or trusted instruction.
- `[To decide]` Decide whether future operational diagnostics should be retained locally and for how long. Recommendation: keep them disabled by default and provide a one-click clear action if enabled.

## 7. Model data flows and risks

### 7.1 Local model

- `[Inferred]` Proposed flow: the game captures the approved schema version 1 fields on the simulation queue; the UI presents the exact request; after per-request consent, a separate companion sends the bounded snapshot and player question to a model service on the same machine; the companion returns a suggestion for display. The game simulation thread never waits for the model.
- `[Inferred]` Keep the model endpoint on loopback by default, do not bind to the LAN, authenticate the local IPC peer where practical, validate message shape and size, and enforce deadlines. Keep model SDKs and provider credentials outside the game process.
- `[Inferred]` Local inference can reduce disclosure to a remote provider, but another local process may impersonate or inspect the endpoint, the model service may retain prompts, and malware or other users with access to the machine may read data. Local processing is not automatically private.
- `[To decide]` Choose the first local model runtime and the companion-to-game transport. Recommendation: begin with one supported loopback companion and a narrow, bounded message contract after a runtime compatibility probe.

### 7.2 Cloud model

- `[Inferred]` Proposed flow: the game captures the approved schema version 1 fields; after per-request consent, the companion sends only the approved snapshot and displayed player question to the named cloud provider over HTTPS; the companion validates the response and returns it as a suggestion for display.
- `[Inferred]` The cloud credential remains in the companion's operating-system credential store. The game process does not make the provider request and never sees the credential.
- `[Inferred]` A cloud provider receives the approved payload and may also receive account, network, timing, or service metadata. Retention, model-training use, jurisdiction, breach exposure, and account linkage depend on that provider and its settings.
- `[Inferred]` Use a current HTTPS/TLS stack in the companion rather than assuming the game's .NET 3.5 / Unity Mono networking stack supports current provider requirements.
- `[To decide]` Choose whether cloud models are allowed, which providers may be offered, and what retention, training, region, and account-linkage terms are acceptable. Recommendation: keep cloud mode disabled until the provider and its data terms are named in the consent screen and reviewed by the user.

## 8. Failure and safe degradation

- `[Inferred]` If the companion or model is unavailable, a request times out, consent is absent or revoked, the response is malformed or oversized, or the model refuses, show a concise status and return to the current read-only game state.
- `[Inferred]` A failure must not block the UI or simulation thread, modify a save, change simulation state, or create an action. Discard stale responses and clear pending local payloads.
- `[Inferred]` Do not silently retry, switch from local to cloud, or redirect to another provider. Any retry or destination change requires a fresh consent preview and confirmation.
- `[Inferred]` Log only a redacted failure category and non-sensitive diagnostic metadata. Never log credentials, payloads, prompts, or raw provider error bodies.

## 9. Decisions before implementation

- `[To decide]` Select the first supported model route: local only, cloud only, or a staged local-first rollout. Recommendation: local-only prototype first; consider cloud only after its provider and data terms are approved.
- `[To decide]` Confirm the exact payload scope and whether any fields may be added after schema version 1. Recommendation: no additional city or player data in the first connected prototype.
- `[To decide]` Select the player-facing UI location for the request preview, per-request confirmation, revocation, and suggestion display.
- `[To decide]` Select the local companion runtime, IPC transport, supported operating systems, endpoint authentication, size limits, and request deadlines.
- `[To decide]` If cloud access is later approved, select providers and approve their retention, training, data-region, account-linkage, and credential-storage terms.
- `[To decide]` Set local retention and deletion behavior for any future temporary files or saved conversation feature. Recommendation: no persistence by default, with a clear action for deleting any explicitly retained local data.
- `[To decide]` Define how a future action request is displayed and confirmed per operation, while keeping the accepted interop contract's Write rules as the maximum authority.

## 10. Approval boundary

- `[Verified]` This document is a design proposal only. AI Gaming 0.3.0 remains a local, read-only preview with no model connection or outbound data transfer.
- `[Inferred]` Before implementation, resolve the decisions above, prepare a separate scoped plan, and obtain approval. Any future change to the accepted interop contract follows its governance rule and is outside this document's scope.
