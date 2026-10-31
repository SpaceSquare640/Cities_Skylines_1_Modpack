# Realistic Railway Traffic Control

This source-available Cities: Skylines 1 module is in initial planning. No game implementation, build project, DLL, or playable release exists yet.

## Project links

- [Canonical repository](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack)
- [Module branch](https://github.com/KingsleyHK0323/Cities_Skylines_1_Modpack/tree/Realistic_Railway_Traffic_Control)
- [Modpack website](https://kingsleyhk0323.github.io/Cities_Skylines_1_Modpack/)

## Intended scope

Railway signals, block occupancy, junction control, and train scheduling are proposed areas of work. Compatibility and behavior have not been designed or validated.

## Automation and release boundary

The `module-baseline` workflow runs documentation and repository baseline checks on GitHub-hosted Ubuntu for pushes and pull requests targeting `Realistic_Railway_Traffic_Control`, or by manual dispatch. It does not compile or test game code.

Future compilation needs an implemented build project and a legally permitted source for the required Cities: Skylines 1, ICities, Unity, and other declared reference assemblies. Their versions and acquisition method remain unresolved. Do not commit proprietary game assemblies.

Once those prerequisites are resolved, the intended distribution is an independent GitHub cloud build with a module-specific GitHub Release providing the DLL and necessary files directly, without ZIP packaging. No automatic release or Workshop upload is configured. Local execution of runners or workflows requires separate explicit authorization. Gameplay acceptance requires the Steam Workshop subscribed version.

## License

Copyright Kingsley. Code and original documentation in this branch use the PolyForm Noncommercial License 1.0.0; see [LICENSE](LICENSE). This is source-available software with noncommercial restrictions. The separate website's original content uses CC BY-NC-SA 4.0; website content is not included here. Third-party material retains its own terms.

Signature: OpenAI-Codex_Responsible: Realistic_Railway_Traffic_Control
