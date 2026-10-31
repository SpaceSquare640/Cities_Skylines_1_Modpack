# Development release automation

The AI_Gaming branch now builds and publishes its own uniquely tagged Development prerelease on branch pushes or manual dispatch. The read-only build job restores approved encrypted references, publishes the paired self-contained Companion with native libraries bundled, compiles the mod, verifies outputs, and prepares only allowlisted assets. A separate publisher verifies the internal artifact, creates a draft, uploads individual files, checks downloaded hashes and sizes, and publishes it as a prerelease without marking it Latest. Existing releases and assets are never overwritten.

This change does not include the uncommitted UI redesign. Runtime/gameplay acceptance remains pending.

Signature: OpenAI-Codex_Responsible: AI_Gaming

Subsequent releases use `AI Gaming — Development Build` with `AI_Gaming-dev-<Run ID>-<Attempt>` tags and the approved eight-section release-note order. Shared UI changes are validated by cloud checks before publication; prior releases and tags are retained.
Signature: OpenAI-Codex_Responsible: AI_Gaming
