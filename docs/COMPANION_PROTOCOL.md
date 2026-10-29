# AI Gaming Local Companion Protocol

**Status:** Implemented source for the 0.4.0 Ollama Local preview, including companion request schemaVersion 4, schemaVersion 3 city snapshot fields, optional city data groups, game-detected DLC identifiers, and a user-configurable Ollama response-body limit that defaults to unlimited. The companion transport protocol and `/v1` route remain version 1. Release builds and companion checks pass; the game runtime and self-contained Windows x64 publish have not been verified.

**Scope:** Windows x64 companion, player-confirmed Ollama Local advice, schemaVersion 4 request data carrying schemaVersion 3 city snapshot fields, selected aggregate city fields, optional utilities/zoning/traffic-transit/service groups, automatically detected installed-and-available DLC identifiers, and the configured maximum response-body byte count. The separate city-summary log remains schemaVersion 1. This document does not describe or authorize cloud providers, API keys, game control, or calls to other Modpack capabilities.

## 1. User-visible request flow

1. The settings page asks for an Ollama model name, a response-body limit in MiB (`0` means unlimited), and a player-written question. The six base city fields and four optional groups (utilities, built zoning, traffic/transit, and service status) start checked and can be selected with the Select All button. The game-detected installed-and-available DLC identifier list is always included; it has no separate checkbox.
2. The player prepares a preview. AI Gaming queues the selected aggregate city reads through `SimulationManager.AddAction`, then dispatches the result to the UI thread. On that UI dispatcher, it requires a ready platform DLC state and collects identifiers that both game API checks report as installed and available. Failure prevents preview preparation and sends no request. The UI does not wait for the simulation callback.
3. A multi-line read-only text field shows the destination, model, response-body limit, exact selected values, complete DLC identifier list, and question. It labels unverified utility units/statistic windows and clarifies that zoning covers built buildings, traffic flow is not a congestion percentage, police/health values are not geographic coverage, and fire coverage is unavailable when no segments are sampled. The DLC names are the game's enum identifiers, not guaranteed store display names or account-ownership claims. The user must review the complete preview and press the separate confirmation button. Changing any selection, model, response limit, question, or companion-start preference invalidates the pending preview.
4. Companion auto-start permission defaults off and resets on game restart. The companion starts only after that request's confirmation and only while this permission is enabled. No later request inherits the confirmation.
5. The model response is displayed as advice only. It is not interpreted as a game action and does not change simulation state. The mod does not automatically retry or switch providers.

The request contains schemaVersion 4 and only the six base fields, the selected optional groups, the required `installedAvailableDlcs` list, selected model name, configured `maxResponseBytes`, and free-text player question. `maxResponseBytes=0` means no application-level `/api/chat` response-body cap; a positive value is a byte count. DLC identifiers are included automatically after the separate per-request confirmation. Each selected group is encoded as a complete set; partial groups are rejected. The player question is not scanned; do not enter a city name, account information, secrets, personal data, or other prohibited content. The mod does not attach saves, logs, object lists, screenshots, or files.

## 2. Companion packaging and local cache

- The mod expects `Companion\AIGaming.Companion.exe` beside `AIGaming.dll` in the mod's Workshop content directory.
- Before launch, the mod requires a valid expected SHA-256. The default `CompanionSha256=UNCONFIGURED` fails closed. It verifies the source executable, stages it under `%LOCALAPPDATA%\SpaceSquare640\AI Gaming\Companion\<edition>\<version>-<sha256>\`, verifies the staged copy, and launches only that copy.
- The cache is immutable per edition/version/hash. The mod does not overwrite or delete old versions. This lets Steam update Workshop files without replacing a running executable; update and locked-file behavior still needs game testing.
- Release and Development packages must each contain their matching companion and use the same edition, version, protocol version, and SHA-256 values in both the companion and mod build.
- Use `tools\Build-LocalPackage.ps1 -Edition release` or `tools\Build-LocalPackage.ps1 -Edition development` to create one matching local package under the ignored `bin\local-packages\<edition>` directory. This helper invokes `dotnet publish` and `dotnet build` directly; it does not start a local runner, execute a workflow, restore packages, install dependencies, upload files, or copy files into the game Mods folder. It refuses to use an output directory that already exists and does not delete or overwrite files. If interrupted or unsuccessful, inspect the ignored output directory before deciding how to proceed.
- The local package helper requires the runtime assets and win-x64 runtime pack to already be available to `dotnet`. It uses `--no-restore`; if assets or runtime packs are missing, the publish fails and no dependency is downloaded. The script was not executed in this environment because the self-contained runtime pack is unavailable.
- Build the companion as a self-contained Windows x64 single-file executable for Workshop packaging. Example publish command:

  ```powershell
  dotnet publish Companion\AIGaming.Companion.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:CompanionEdition=release
  ```

- Calculate SHA-256 from the final `AIGaming.Companion.exe` and supply it to the matching mod build through `-p:CompanionSha256=<64-hex-sha256>`. Use `-p:CompanionEdition=development -p:DevTools=true` for the Development build. The mod project does not automatically copy the executable into Workshop packaging or into the game Mods folder.
- The local companion project was built framework-dependent for source validation. The self-contained `win-x64` publish was not run because the runtime pack is unavailable in this environment. Do not treat the framework-dependent DLL as a Workshop-ready companion package.

## 3. Startup handshake and process lifecycle

1. Only after request confirmation, `ProcessStartInfo` starts the verified cache executable with no command-line arguments, no shell, no visible window, and redirected standard input, output, and error streams.
2. The mod writes a bootstrap record to the child's anonymous stdin pipe. It contains protocol version, edition, mod and companion versions, the expected companion SHA-256, parent PID, and the parent image path. It contains no session secret.
3. The companion checks the actual parent PID and image path/name (`Cities.exe`), checks the mod/companion version and edition fields, hashes its own executable, and requires the exact expected hash. Any mismatch aborts before opening the listener.
4. The companion generates 32 random bytes using the operating-system CSPRNG, binds its listener, and sends one Base64-framed readiness line to redirected stdout. That message contains the 127.0.0.1 port, edition, version, protocol, companion and parent PIDs, executable hash, and session secret. The parent receives it from the anonymous pipe; it is not placed in a file, URL, environment variable, command-line argument, or game log.
5. After one response or cancellation, the mod closes the stdin pipe. The companion treats EOF as session shutdown, closes the listener, clears its secret buffer, and exits. Each future request requires a new confirmation, process, secret, and port.

The PID/path/name checks are not publisher-signature authentication. A same-user process able to run the companion can establish its own session; it does not receive a game snapshot unless it can already inspect or control the game process. This limitation and the broader accepted threat boundary are described in `AI_DATA_BOUNDARY.md`.

## 4. Loopback HTTP session

- The companion uses `TcpListener` bound to `IPAddress.Loopback` (`127.0.0.1`) with OS-assigned port 0. It does not listen on wildcard, LAN, or IPv6 addresses. The listener accepts the `POST /v1/ollama/local/chat` route only, requires the matching `Host` and rejects every request containing an `Origin` header. It sends no CORS headers.
- AI Gaming's .NET 3.5 client targets `http://127.0.0.1:<port>/v1/ollama/local/chat`, disables proxy and redirects, and sends no other host. The companion's Ollama connector accepts only `localhost:11434`, disables proxy use, and opens an IPv4 socket directly to `127.0.0.1:11434`.
- The 32-byte secret is never sent in an HTTP request. The mod and companion each compute the request body SHA-256 and HMAC-SHA-256 over this newline-delimited canonical request:

  ```text
  protocolVersion
  edition
  sequence
  POST
  /v1/ollama/local/chat
  lowercaseBodySha256
  ```

- The request carries protocol, edition, sequence, body hash, and proof headers. The companion requires the next sequence exactly (`1`, then previous + 1), validates the proof with a fixed-time byte comparison, reads the declared body, checks its hash, then validates its fields and types before contacting Ollama.
- A successful HTTP response includes the protocol, edition, same sequence, response-body hash, and an HMAC over protocol, edition, sequence, status code, method, route, and response hash. The mod validates all values and discards unauthenticated, mismatched, malformed, or replayed responses.
- Loopback HTTP is not encrypted. HMAC protects authenticity and replay within this session; it does not protect the payload from software able to read/inject into either process or inspect their memory. The accepted threat model does not defend against privileged local software with those abilities.
- No application request-size or request-frequency caps are implemented, per the approved user decision. The connection has a ten-minute deadline. The companion processes one client at a time and a mod request ends the session. Request size and repeated local use can still consume memory/CPU; these are residual risks. The complete raw Ollama `/api/chat` HTTP response body, including its JSON envelope, is unlimited by default (`maxResponseBytes=0`). A positive user setting caps the body at 1–2047 MiB; an oversized body is discarded before JSON parsing and maps to the sanitized `response_limit_exceeded` category, with no partial answer. This byte-size setting is not a generated-token cap or a cap on total process memory; parsing and copying the body may require additional memory. The unlimited default still depends on available memory and the existing request deadline. The `/api/tags` model-list response remains limited to 1 MiB before JSON parsing, with oversized bodies mapped to the sanitized `invalid_response` category without logging response content.

## 5. Request encoding and Ollama adapter

The bridge body is UTF-8 newline-delimited key/value text, not JSON. Model, question, and `installedAvailableDlcs` are Base64-encoded UTF-8 strings. The DLC value decodes to a sorted, unique, LF-separated list of canonical `SteamHelper.DLC` enum identifiers; an empty decoded value means the ready game API reported no installed-and-available catalog items. The companion accepts only ASCII letter/digit/underscore identifiers in strictly increasing ordinal order. Numeric values are invariant-culture decimal; the date is `yyyy-MM-dd`. Only schemaVersion 4, model, question, nonnegative integer `maxResponseBytes`, selected base fields, complete selected groups, and the required DLC list are accepted. `maxResponseBytes` is a raw byte count; zero means unlimited. The city data described in the model prompt remains city snapshot schemaVersion 3. Unknown keys, duplicate keys, partial groups, invalid types, malformed UTF-8, invalid dates, or invalid DLC identifiers are rejected.

Optional group keys are:

- Utilities: `electricityCapacity`, `electricityConsumption`, `waterCapacity`, `waterConsumption`, `sewageCapacity`, `sewageAccumulation`, `garbageAmount`, `garbageCapacity`, `garbageAccumulation`, `incinerationCapacity`. These are raw district values; physical units are unverified.
- Built zoning: `residentialBuildingCount` / `residentialBuildingArea`, `commercialBuildingCount` / `commercialBuildingArea`, `industrialBuildingCount` / `industrialBuildingArea`, and `officeBuildingCount` / `officeBuildingArea`. These describe constructed buildings, not vacant zoned cells; area units are game-defined and unverified.
- Traffic/transit: `trafficFlow` and `averagePassengers`. These are latest game-reported integer statistics with unspecified scale/time window; `trafficFlow` is not a congestion percentage.
- Service status: `policeCriminalAmount`, `policeCriminalCapacity`, `policeExtraCriminals`, `fireCoverageMeanByte`, `fireCoverageSegmentCount`, `healthSickCount`, `healthCapacity`, and `healthCareProductivity`. Police/health are citywide workload/capacity, not geographic coverage. Fire coverage averages the raw byte across created network segments; a count of zero means unavailable.

The `/v1/ollama/local/chat` path and HMAC transport version remain unchanged because the framing and session authentication did not change.

Before inference, the companion requests `GET /api/tags` from Ollama on `localhost` and requires the selected name to match a listed model entry with a positive `size`. Names ending in `:cloud` or `-cloud` are rejected. The player supplies the model name; names are not hard-coded. The generated prompt includes the exact schemaVersion 3 city values and DLC identifiers, preserves the caveats above, and tells the model that DLC availability does not prove the current save uses that content. The companion then sends `POST /api/chat` with the selected model, one user message, and `stream=false`. It accepts only `message.content`; provider error bodies are not forwarded to the game log.

Ollama documents that cloud models can be accessed through Ollama's app/CLI and that cloud model inference is performed on Ollama's servers. The tag/size check and cloud-tag rejection are practical filters, not a guarantee about routing inside the user's Ollama installation. AI Gaming does not edit or inspect Ollama's configuration; for stricter local-only use, the player can disable Ollama cloud features (`OLLAMA_NO_CLOUD=1` or `disable_ollama_cloud=true`) and restart Ollama.

Errors map to sanitized categories, including `ollama_unavailable`, `model_not_found`, `local_model_required`, `local_authentication_failed`, `timeout`, `invalid_response`, `response_limit_exceeded`, `cancelled`, and `ollama_error`. The mod displays a localized category, logs only a coarse success/failure status, and never retries or falls back.

## 6. Static verification boundary and sources

- Release build of `AIGaming.csproj`: 0 warnings, 0 errors.
- Release build of `Companion/AIGaming.Companion.csproj`: 0 warnings, 0 errors, framework-dependent output only.
- Companion checks cover bounded model-list parsing, unlimited and user-capped `/api/chat` response reading, and schemaVersion 4 requests with schemaVersion 3 city data.
- The self-contained publish, Steam Workshop packaging, process launch in Cities: Skylines/Unity Mono, UI rendering/scrolling, actual Ollama calls, and update while the companion is running have not been verified.
- A Windows CLR probe verified a redirected anonymous-pipe round trip with a non-secret payload. It does not establish Unity Mono behavior.

Official Ollama documentation checked 2026-10-25:

- API: List models: https://docs.ollama.com/api/tags
- API: Generate a chat message: https://docs.ollama.com/api/chat
- Cloud models and cloud API behavior: https://docs.ollama.com/cloud
- Disable cloud features and local/cloud data handling: https://docs.ollama.com/faq

Implementation documentation signed by OpenAI-Codex.

Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.

Documentation update signed by OpenAI-Codex_Responsible: AI Gaming.

Implementation update signed by OpenAI-Codex.

Implementation update signed by OpenAI-Codex_Responsible: AI Gaming.
