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

## First public Development prerelease — 2026-10-31

- Publish individual allowlisted DLLs and documentation through a separate,
  least-privilege publisher after verified cloud compilation and draft asset checks.
- Include the official CitiesHarmony MIT license, stable Workshop links, installation
  and removal instructions, save-risk warnings, checksums and exact build provenance.
- Use unique module-prefixed tags and retain existing public versions unchanged.
- Resolve unpublished drafts through their release ID before inspecting assets;
  the published-release-by-tag endpoint does not find unpublished drafts.

Signature: OpenAI-Codex_Responsible: AI_Improve

## Lifecycle and settings corrections — 2026-10-31

- Unsubscribe shared-patch diagnostics on disable so ordinary disable/re-enable
  cycles do not accumulate the previous anonymous level-loaded handler.
- Reset the optional lane bottleneck diagnostic to its default disabled state.
- Apply the approved full-name Development release title, module-prefixed tag,
  eight-section installation notes, and exact source/run information in new releases.

Signature: OpenAI-Codex_Responsible: AI_Improve

- Follow the verified repository transfer to KingsleyHK0323 in the cloud job guard;
  the repository ID is unchanged and all branch/publication boundaries remain.
  Signature: OpenAI-Codex_Responsible: AI_Improve

## Canonical account links — 2026-10-31

- Update active repository, issue, website and change-log links in README,
  Content Manager settings and local Workshop descriptions to KingsleyHK0323.
- Verify the new Pages root, AI Improve page and change-log page respond with HTTP
  200. Existing public releases, tags and historical records remain unchanged.
- Local Workshop text changes do not constitute a Workshop upload.

Signature: OpenAI-Codex_Responsible: AI_Improve

## Aircraft refusal result — 2026-10-31

- Explicitly set the pathfinding result to false before skipping the original
  aircraft pathfinder for thunderstorm and airport-saturation refusals.
- Preserve the existing saturation cleanup and all allowed-flight branches.
- This corrects AI-06 from the dated audit; shared-prefix behavior and live flight
  handling still require subscribed-game validation. LateMergeProbe stays disabled.

Signature: OpenAI-Codex_Responsible: AI_Improve
