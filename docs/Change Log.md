# Change Log

## Unreleased

- Limited Ollama `/api/tags` and `/api/chat` response bodies to 1 MiB before JSON parsing; oversized responses use the sanitized `invalid_response` category.

## 0.4.0 — Early preview

- Added player-confirmed Ollama Local suggestions through an optional companion, with selectable schemaVersion 1 city fields and advice-only responses.
- Hardened companion startup failure handling and timeout reporting.
- Added localized consent disclosure labeling and content-based initial text height.
- Added a direct-dotnet local packaging helper for matching Release or Development companion and module outputs; it does not run a local runner or workflow.
- Clarified that DevTools=true builds are development-only and may be uploaded only to the matching Development Workshop item, never a Release item.

Documentation update signed by OpenAI-Codex.
