# Build reference tooling change log

## 2026-10-31

- Add an authenticated AES-256-GCM reference container and a narrowly scoped restore helper.
- Validate the exact reference file list, sizes and SHA-256 digests before extraction.
- Restrict extraction to the runner temporary directory and refuse existing files and reparse points.
- Keep reference assemblies and keys out of public build artifacts and logs.
- Verify authenticated extraction on the GitHub-hosted Windows runner without publishing artifacts.

Signature: OpenAI-Codex
