# AI Improve

Visit the [AI Improve website page](https://spacesquare640.github.io/Cities_Skylines_1_Modpack/ai-improve/) for project information, documentation, and updates.

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
