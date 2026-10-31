# AI Gaming In-Game UI Redesign — Proposed Design

## Intent

Make AI Gaming usable during city play and in the game's mod settings. The redesign covers AI Gaming's interface, not the base game's interface. It preserves the existing advice-only behavior and does not implement game control.

## Core design principles

**Effortless:** Make the next useful action immediately visible, group controls around the player's current task, preserve inputs across views, and avoid repeated setup or unnecessary navigation. Use clear labels and immediate status feedback. Keep the per-request preview and confirmation because they are required to let the player review the data being sent.

**Calm:** Let the question, city-data choices, preview and reply carry the visual emphasis. Use the game's native visual language, restrained neutral surfaces, one consistent accent for primary actions and focus, readable type and spacing, and clear hierarchy. Keep secondary settings and technical details available without crowding the primary workflow. Avoid decorative animation, excessive borders, attention-grabbing colors and fake buttons.

## Research

- TM:PE separates its options into focused tabs: https://doc.tmpe.me/settings.html
- TM:PE provides a compact, movable in-game entry point and toolbar: https://doc.tmpe.me/toolbar.html
- Cities: Skylines exposes native panels, labels and UI components through ColossalFramework.UI: https://skylines-modding-docs.readthedocs.io/en/latest/modding/Development/How-to-Use-ColossalFramework.UI.html

The design borrows these interaction patterns without copying visual assets or adding a dependency on another mod.

## Proposed interface

### In-game AI panel

Provide a compact entry button that opens a movable, closable AI Gaming panel during a loaded game. Keep it within the visible screen and avoid obscuring essential game controls. Give it three clearly labelled sections:

1. **Ask AI:** local model, question, permission to launch the companion and a clearly placed button to prepare a request. Retain entered values while the session remains active so routine use does not require repeating setup.
2. **City data:** the existing ten selectable data groups, a select-all action and an explanation that the detected DLC list is included in the preview.
3. **Review and reply:** the complete request preview, an explicit per-request confirmation, cancel action, request status and the complete model response.

```text
  [AI Gaming]                                  [close]
  [Ask AI]  [City data]  [Review and reply]
  ┌─────────────────────────────────────────────────┐
  │ Active section content                          │
  │                                                 │
  │ [Prepare]             [Confirm / Cancel]        │
  └─────────────────────────────────────────────────┘
```

Only actual actions appear as buttons. Informational text and status use labels. Long preview and response content must remain fully accessible through a bounded scrollable viewport; viewport size is not a limit on AI response length or stored content.

The main request path should feel continuous: preparation reveals or brings the player directly to the review section, and a completed response is visible there without a separate navigation step. Preserve manual access to every section and make the active request state clear without intrusive notifications.

### Mod settings

Keep the settings available from the game's standard Options / Mods Settings entry, including the main menu. Group language and persistent settings, connection and privacy disclosure, optional mod interoperability, and the existing manual city snapshot action. Use English as default and retain English, Traditional Chinese, Simplified Chinese, Japanese and Korean, including immediate language updates.

Retain access to the existing local AI settings and input controls from the main-menu settings entry. City capture still requires a loaded simulation and must clearly report when it is unavailable. Settings and the in-game panel must reflect the same request state.

## Behavior and safeguards

- Preserve every existing data-selection default, installed-and-available DLC detection, preview content, per-request confirmation, cancellation and error state.
- Companion launch permission remains off by default and resets on game restart. No request is sent just by opening a panel or switching a section.
- The existing user-selected response body limit remains supported and defaults to unlimited; do not truncate responses or previews for display.
- Switching sections or language must preserve entered values, prepared requests and in-flight results. Closing or reopening the panel must not silently submit, duplicate, or cancel a request.
- Changing the model, question, selected data, response limit or companion-launch permission invalidates a prepared preview; section and language changes alone do not. Both views must show the same action availability and status.
- Unloading a city or disabling the mod must cancel active work or invalidate callbacks before views are destroyed. Opening or closing views must not duplicate event subscriptions or apply callbacks to destroyed controls.
- Keep the current local Ollama and data-routing disclosure accessible before request confirmation. Keep the manual snapshot separate and explicitly initiated.
- UI changes must not depend on the user's game language, a third-party UI framework, a cloud provider, or running a local workflow.
- Long replies must not be rendered by expanding one unbounded control or limiting stored text to the viewport. Choose a game-compatible scrollable rendering approach that preserves the entire response, and verify large responses in the subscribed game build.

## Implementation boundaries

An initial, not-yet-integrated text viewport buffer preserves the full string and reads visible windows without splitting UTF-16 surrogate pairs. The UI must provide continuous forward and backward navigation and send the complete original request, never just the visible window.

- Separate request state and lifecycle from its view so settings and the in-game panel do not create competing controllers or lose an active request. Bind translated controls to stable localization keys rather than reverse-matching their displayed text.
- Use native UI components and game-provided visual resources. Text must wrap without overlap; scrolling must remain usable at narrow sizes and for all five supported languages. Keep keyboard focus and visible labels for controls.
- Preserve existing architecture where possible and update affected README and Change Log entries. Source code comments and technical documentation remain English.
- Review actual game API availability against the installed project references. Treat layout sizing and in-game behavior as unverified until the user tests the Steam Workshop subscribed Development build.

## Open risks

- A floating in-game panel is a proposed design choice, not a behavior verified in the game. Its anchoring, drag behavior, input focus, rendering and coexistence with other mod panels need subscribed-game testing.
- The previously observed `companion_missing` has a separate unresolved runtime cause. A UI redesign must display the error accurately and cannot by itself establish that AI requests work.
- Changing the settings view and request controller requires replacing existing UI code; the repository's deletion confirmation rules apply before removing the identified code.

Design: OpenAI-Codex  
Responsibility: OpenAI-Codex_Responsible: AI Gaming

## Integration follow-up — 2026-10-31

The shared controller, settings and in-game navigation, bounded full-text display, and click-time confirmation lifecycle guards are now integrated for cloud regression verification. The earlier proposed/foundation descriptions record design history. Independent static review found no demonstrated confirmation bypass in the latest fix; dispatched callback loss and actual game UI behavior remain unverified. Signature: OpenAI-Codex_Responsible: AI_Gaming.
