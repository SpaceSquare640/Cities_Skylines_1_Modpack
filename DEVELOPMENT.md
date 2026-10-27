# Cities: Skylines 1 Modpack website

Author: OpenAI-Codex

The static website for the public [Modpack repository](https://github.com/SpaceSquare640/Cities_Skylines_1_Modpack), intended for its `Modpack_Website` branch.

The site identifies Kingsley as the collection creator and shows a small, inline SVG Hong Kong flag beside the footer credit. The repository URL retains its existing `SpaceSquare640` account name.

## Pages

- `index.html`: collection overview, keyboard-accessible project selector, and website documentation.
- `ai-improve/index.html`: AI Improve introduction and source branch.
- `automated-public-transport-planning/index.html`: transport planning introduction and source branch.
- `ai-gaming/index.html`: explicitly states that development has not started.
- `change-log/index.html`: updates generated from the website branch `Change Log.md`.

Specific capabilities and release status for AI Improve and Transport are not verified by this website. The three project artworks and the original SVG city drawings are illustrations, not game screenshots or proof of implemented features. No downloads or performance claims are invented.

## Preview

Open `index.html` directly, or serve this directory with a trusted local static server. Relative page and asset links also support a GitHub Pages repository subpath. The site needs no package installation, build framework, external font service, or API key.

The included loopback-only preview server supports the repository subpath:

```sh
node tools/serve.cjs
```

Then open `http://127.0.0.1:4173/Cities_Skylines_1_Modpack/`. Stop the server with Ctrl+C.

The generated HTML includes readable English content without JavaScript. With JavaScript enabled, the interface defaults to English and dark mode; visitors can independently switch to Traditional Chinese and light mode. Validated choices are saved to localStorage and shared across pages on the same origin. If storage is blocked, controls continue working for the current page. Language and theme controls are hidden when JavaScript is unavailable, and all project summaries remain visible.

For development, regenerate the five pages using Node.js:

```sh
node tools/build.cjs
```

Edit page content in `tools/build.cjs`; project artwork lives in `assets/branding/projects/`. Shared appearance and behavior live in `assets/site.css`, `assets/preferences.js`, and `assets/site.js`. Generated files and developer artifacts carry author signatures.

## Accessibility and design

Design follows the Taste skill's borrowed-category direction (seed 1524132471), interpreted as a city-planning exhibition and adapted to the approved dark default. Project-specific artwork gives each page its own image while the home and Change Log pages use original SVG city illustrations. No verified gameplay screenshots were provided, so the site does not claim to meet Taste's photographic asset floor.

The project selector follows the [W3C WAI tabs pattern](https://www.w3.org/WAI/ARIA/apg/patterns/tabs/): arrow keys change tabs; Home and End select the first and last tabs. Native buttons/selects, visible focus, a skip link, semantic landmarks, and reduced-motion support are included. This is an independent implementation of the pattern, not a copied third-party component library. The GitHub link adapts the user's local Uiverse button reference by kamehame-ha; source and MIT terms are included in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

The Modpack is source-available and noncommercial. Mod and website code are licensed under the [PolyForm Noncommercial License 1.0.0](https://polyformproject.org/licenses/noncommercial/1.0.0/) (see [LICENSE](LICENSE)); website content, including text and images, is licensed under [CC BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/) (see [LICENSE-CONTENT](LICENSE-CONTENT)). Copyright © Kingsley. Third-party components keep their own licenses as listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md), such as the MIT-licensed Uiverse component. Publishing, Git operations, third-party asset licensing, and GitHub Pages configuration require separate handling.

Author: OpenAI-Codex
