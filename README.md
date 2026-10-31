# AI Improve

Visit the [AI Improve website page](https://kingsleyhk0323.github.io/Cities_Skylines_1_Modpack/ai-improve/) for project information, documentation, and updates.

Author: Kingsley


Source-available status: AI Improve source code is available under the PolyForm Noncommercial License 1.0.0. See LICENSE.
Website code: PolyForm Noncommercial License 1.0.0.
Website content (text and images): Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International (CC BY-NC-SA 4.0).
Copyright © Kingsley.

<!-- Updated by OpenAI-Codex_relicense_ai_improve on 2026-10-27. -->

## Cloud delivery status

This branch runs source checks on pushes and pull requests. The manually dispatched
`cloud-build-readiness.yml` deliberately fails with the missing dependency list;
it does not compile or publish artifacts. An authorized, versioned acquisition
source for ICities.dll, ColossalManaged.dll, Assembly-CSharp.dll and UnityEngine.dll
is not configured for GitHub-hosted runners. Compilation also needs .NET Framework
3.5 reference assemblies and CitiesHarmony.API 2.2.0. The current CopyToMods build
target must be disabled for cloud builds before compilation is enabled.

The intended delivery is branch-specific GitHub Releases containing the mod DLL
and required redistributable files directly, without ZIP archives. Automated
public releases remain disabled until dependencies and output validation are ready.
The legacy push-to-Workshop dispatcher is disabled. Workshop Release uploads are
separate, explicitly authorized local operations; source checks do not upload.

Signature: OpenAI-Codex_Responsible: AI_Improve

### Diagnostic cloud compilation

`cloud-compile-test.yml` performs an actual Development DLL build on a
GitHub-hosted Windows runner, using .NET SDK 10. It runs manually or when this
workflow changes on AI_Improve. Game references intentionally point to an empty
RUNNER_TEMP directory; the original compiler errors and nonzero exit are retained.
CopyToMods targets a separate RUNNER_TEMP directory. LateMergeProbe is not enabled.
This diagnostic does not upload artifacts, create Releases, or upload to Workshop.
A successful toolchain setup or restore does not establish successful DLL compilation.

Signature: OpenAI-Codex_Responsible: AI_Improve

### Verified encrypted references

The compile test now restores the eight approved game reference files from the
same repository at commit `7b8a5e7c10f2db84388e8dd8af2084f239957b13`.
Only the isolated restore step receives `CS1_GAME_REFERENCES_KEY`; the helper
verifies authenticated encryption and checksums before extracting into RUNNER_TEMP.
No decrypted references enter artifacts or caches. The build uses that temporary
game root and asserts nonempty AIImprove.dll, CitiesHarmony.API.dll, and the
redirected temporary deployment DLL. This supersedes the empty-root diagnostic
above; it still publishes no artifacts or Releases.

Signature: OpenAI-Codex_Responsible: AI_Improve

## Current Development prerelease delivery

The cloud workflow now builds and publishes a uniquely tagged Development
prerelease on each push to AI_Improve or manual dispatch on that branch. Earlier
readiness and diagnostic sections above describe historical stages. The current
workflow uses the verified encrypted references and retains the existing build
boundaries. A read-only Windows build job stages only the two approved DLLs,
installation instructions, licenses, notices, Workshop links, provenance and
checksums. A separate publisher has no game-reference secret; it verifies every
uploaded draft asset by name, count, size and downloaded SHA-256 before publishing
as a prerelease with Latest disabled. Existing tags and assets are never replaced.

See [Development installation](docs/Development-Installation.md) for installation,
uninstallation, DevTool shortcuts, save risks and required Harmony subscription.
Release assets are individual files, with no binary ZIP. This delivery test does
not establish Workshop subscribed-game acceptance.

Signature: OpenAI-Codex_Responsible: AI_Improve

## Lifecycle and reset behavior

Disabling AI Improve now unsubscribes shared-patch diagnostics from level-loaded
notifications. Reset to defaults also disables lane bottleneck diagnostics.
These corrections do not enable the experimental LateMergeProbe. Game lifecycle
and settings behavior still need validation in the subscribed Workshop build.
New prereleases use the full title `AI Improve — Development Build`, tags of the
form `AI_Improve-dev-<Run ID>-<Attempt>`, and the approved eight-section release notes.
Existing tags, drafts and public versions are preserved.

Signature: OpenAI-Codex_Responsible: AI_Improve

<!-- Canonical links updated by OpenAI-Codex_Responsible: AI_Improve on 2026-10-31. -->

## Aircraft refusal result

Thunderstorm and airport-saturation refusals now explicitly set the pathfinding
result to false before skipping the original method. This avoids inheriting a
true result from an earlier Harmony prefix. The saturation cleanup and allowed
flight paths are unchanged. This does not establish compatibility with every
other prefix; shared-patch behavior remains subject to subscribed-game testing.

Signature: OpenAI-Codex_Responsible: AI_Improve

## Retrying a Development publication

The compile job passes its immutable artifact ID and build attempt to the publisher. Re-running only a failed publish job reuses that successful build; re-running all jobs produces a new artifact for the new build attempt. Source SHA, run ID, build attempt, reference provenance and file hashes remain checked before publication. Build provenance records the compilation attempt, while the release tag keeps the publication run attempt. Artifacts expire after one day; if the original artifact is unavailable, re-run all jobs instead of bypassing verification. No retry deletes or publishes a previous failed draft automatically.

<!-- Signature: OpenAI-Codex_release_format_audit -->