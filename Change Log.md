# Change Log

## Unreleased

- Disable the path-altering Development probe unless explicitly enabled at build time.
- Document that an opted-in probe remains patched until the game exits.
- Discard diagnostic samples for rejected reroute requests and label the distance statistic as a reference, not a confirmed failure cause.
- Show the requested Development Version and save-risk warning in the in-game mod description when development tools are included. The Release description remains unchanged.

Signed: OpenAI-Codex.

## Cloud delivery preparation — 2026-10-31

- Disable the legacy push-triggered Workshop dispatcher.
- Add an explicit manual cloud-build dependency blocker and document direct DLL
  release intent, missing references, and the existing local deployment target.
- No gameplay code or automatic public release is changed.

Signature: OpenAI-Codex_Responsible: AI_Improve

## Cloud compilation diagnosis — 2026-10-31

- Add a real, narrowly triggered Windows Development DLL build against an empty
  game root, preserving dependency failures and redirecting local deployment output
  to runner temporary storage. No publication or artifact upload is enabled.

Signature: OpenAI-Codex_Responsible: AI_Improve

## Verified cloud references — 2026-10-31

- Connect the isolated cloud compile test to pinned encrypted reference restoration
  and assert real DLL outputs without enabling publication or the LateMergeProbe.

Signature: OpenAI-Codex_Responsible: AI_Improve
