# Cities: Skylines 1 Modpack website

The site is built with Astro and Starlight. It contains a bilingual project website and source-based Wiki. Astro writes the static site to `dist/` with the GitHub Pages base path `/Cities_Skylines_1_Modpack/`.

## Local offline setup

This project uses the dependency versions in `package-lock.json`: Astro 7.4.1, Starlight 0.42.6, Sharp 0.35.5 and Playwright 1.64.1. Do not update these versions without reviewing the offline package preparation and site compatibility.

In PowerShell, from this folder:

```powershell
$env:npm_config_cache = 'D:\Code\Game Code\Cities_Skyline_1_Modpack\.tools\npm-cache'
$env:ASTRO_TELEMETRY_DISABLED = '1'
npm ci --offline
npm --offline run extract:ai-improve
npm --offline run prepare:assets
npm --offline run build
```

The Playwright browser is supplied outside the repository. Set its path before browser verification:

```powershell
$env:PLAYWRIGHT_BROWSERS_PATH = 'D:\Code\Game Code\Cities_Skyline_1_Modpack\.tools\playwright-browsers'
```

The Playwright browser check and screenshot report are pending until the Astro content sync error is resolved. Their intended output location is the project `.tools\tmp\website-screenshots` folder; no browser verification has run yet.

## Source-derived content

`src/data/ai-improve.json` is generated from the AI Improve source branch's localization and settings UI. The extractor records the source commit and does not alter source files. Project art is labelled as artwork, not gameplay capture. Missing game evidence uses an explicit screenshot placeholder.

The transport and AI Gaming pages distinguish current preview behaviour from planned work. Their runtime status is not implied by a static site build.

## Deployment

This folder contains no deployment workflow. The Pages deployment change should be reviewed and approved separately before switching the public site.

<!-- Documentation authored by OpenAI-Codex. -->
