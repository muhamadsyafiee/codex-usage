# Codex Usage Widget design direction

## Identity

- **Product:** Windows utility that keeps Codex usage visible at a glance.
- **Personality:** Quiet, friendly, and human. The C mascot with eyes and hands gives the utility a small human gesture.
- **Palette:** Black and white are the core palette because the user requested a monochrome theme and both themes need strong contrast. Grey appears only for borders and tracks.
- **Typography:** Segoe UI fits Windows-native reading and keeps compact utility text legible.

## Composition

- **Structure:** One focused usage panel with quota rows, status, actions, and attribution. Each element exists to support checking usage or controlling the widget.
- **Reset context:** Each quota row shows its next reset in local time so users can decide when to resume work without opening another page.
- **Dock:** A narrow CODEX handle expands on hover or keyboard activation. This keeps the resting widget unobtrusive while preserving a direct path to usage.
- **Controls:** Text labels describe actions. The gear icon means settings because it is the standard Windows settings affordance.
- **Settings:** A dedicated window groups dock, appearance, Windows startup and updates, and account actions so choices are visible without nested menus. A scrollable body and fixed Done footer keep closing available on small displays.
- **Settings hierarchy:** Segoe UI section titles and 22px section gaps separate tasks; compact labels and 6px control gaps connect related choices. Flat dividers separate groups without decorative cards.
- **Settings selection:** Inverted monochrome radio buttons show selected dock, layout and theme. The existing C mascot identifies the window; the inverted Done button is the primary finishing action. Opacity displays a live percentage and changes apply immediately.
- **Feedback:** A native dialog keeps submission inside the app. Device info disclosure is visible beside its opt-out checkbox.

## Motion and dials

- **ENERGY 1 / RHYTHM 1 / MOTION 1:** Calm utility UI, predictable single-panel rhythm, and motion limited to hover expansion, focus, and update feedback.
- **Opacity:** Adjustable resting opacity serves desktop unobtrusiveness. Expanded content becomes opaque so usage values remain readable.
- **Mascot:** The black C with white backing is the identity motif and keeps the app recognisable in taskbar, tray, installer, and widget states.
