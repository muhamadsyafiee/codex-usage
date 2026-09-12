# Codex Usage Widget design direction

## Identity

- **Product:** Windows utility that keeps Codex usage visible at a glance.
- **Personality:** Quiet, friendly, and human. The C mascot with eyes and hands gives the utility a small human gesture.
- **Palette:** Black and white are the core palette because the user requested a monochrome theme and both themes need strong contrast. Grey appears only for borders and tracks.
- **Typography:** Segoe UI fits Windows-native reading and keeps compact utility text legible.

## Composition

- **Structure:** One focused usage panel with quota rows, status, actions, and attribution. Each element exists to support checking usage or controlling the widget.
- **Dock:** A narrow CODEX handle expands on hover or keyboard activation. This keeps the resting widget unobtrusive while preserving a direct path to usage.
- **Controls:** Text labels describe actions. The gear icon means settings because it is the standard Windows settings affordance.
- **Feedback:** A native dialog keeps submission inside the app. Device info disclosure is visible beside its opt-out checkbox.

## Motion and dials

- **ENERGY 1 / RHYTHM 1 / MOTION 1:** Calm utility UI, predictable single-panel rhythm, and motion limited to hover expansion, focus, and update feedback.
- **Opacity:** Adjustable resting opacity serves desktop unobtrusiveness. Expanded content becomes opaque so usage values remain readable.
- **Mascot:** The black C with white backing is the identity motif and keeps the app recognisable in taskbar, tray, installer, and widget states.
