# Codex Usage Widget

Windows 11 x64 desktop widget, written in WPF. Black and white themes; Compact, Card and Detailed layouts; draggable header; always-on-top toggle; tray icon; automatic refresh every 60 seconds. This is a floating desktop window, not an extension inside the Windows Widgets board.

## Install

Download the [Windows 11 x64 MSI installer](https://github.com/muhamadsyafiee/codex-usage/releases/latest). Release assets include the installer and its SHA-256 checksum. Exit the existing app before upgrading.

Run `dist/CodexUsageWidget-1.3.2-x64.msi`, then open **Codex Usage Widget** from Start. Select **Sign in** and finish the official ChatGPT browser login. Remaining limits appear automatically. Use **···** to change layout/theme or sign out. **Hide** hides to the tray; double-click the tray icon to restore. **Exit** exits.

Installer includes .NET and the official Codex CLI 0.154.0. No Node.js or separate Codex install is required. Internet and a ChatGPT account with available Codex limits are required. Installer is unsigned.

## Dock mode (v1.1)

Open **··· → Dock position** and choose **Left**, **Right**, **Top**, or **Bottom**. A small CODEX tab stays at the chosen edge. Hover to reveal usage; moving away collapses the panel after 450 ms. Right-click the tab to change its settings. Choose **Floating** to restore the floating position. Dock mode stays on top and uses the monitor work area, keeping clear of the taskbar. To move to another monitor, switch to Floating, drag there, then dock again. Layout, theme, login and floating position survive upgrades. Exit the old app before running the new MSI.

## Dock movement, opacity and updates (v1.2)

Drag the CODEX heading along the selected screen edge to reposition the dock. Position is stored as a relative offset, so expansion and collapse keep the same anchor. Choose another edge through **Dock position**. Use **Opacity** in the menu to select 20–100%; the default is 70%.

The app checks this repository's latest stable GitHub Release at startup and every six hours. A tray notification, UPDATE tab and update button announce a newer version. **Check for updates** checks manually. Selecting Update downloads the x64 MSI and verifies its published SHA-256 checksum before opening the normal Windows Installer interface and exiting the widget. A successful interactive installation opens the widget automatically with its panel expanded. A cancelled or failed installation does not auto-launch; reopen the existing app from Start. Fully silent deployments do not launch UI.

Version 1.1 users need to download and install 1.2 manually once. Future releases must include both `CodexUsageWidget-VERSION-x64.msi` and `SHA256-VERSION.txt` before publication. Use a draft release while uploading so clients never receive an incomplete release. Checks require internet access; background failures are quiet and retried on the next scheduled check. Manual checks show an error if GitHub cannot be reached.

## Data and authentication

Uses the official Codex App Server JSON-RPC methods `account/login/start`, `account/read`, `account/rateLimits/read`, and `account/logout` over local standard input/output. No password collection, analytics, model calls or usage-credit redemption. Login is stored by Codex in a separate `%LOCALAPPDATA%/CodexUsage/session` directory. It does not use or sign out the existing Codex desktop session. Sign out before uninstalling to clear the managed login; uninstall retains per-user settings/session files.

Remaining percentage is `clamp(100 - usedPercent, 0, 100)`. Multiple limit buckets are supported. Null windows are omitted; unknown percentages are shown as unavailable. Failed refresh clears displayed readings instead of showing stale values as current. Reset timestamps use the local timezone.

Official protocol: https://learn.chatgpt.com/docs/app-server

## Build

Requires Windows, .NET 10 SDK and Node.js/npm. Run `./build.ps1`. Build installs WiX 6.0.2 into `.tools`, downloads Codex through npm, publishes a self-contained x64 application and creates the MSI in `dist`.

Authentication completion and account-backed usage must be checked with an interactive user login. No account credentials are included in this repository or installer.

## Stable background refresh (v1.3)

English is the default UI language. Codex 5-hour usage appears first, then Codex weekly usage, then GPT reserve when returned by the server; other buckets follow. No missing quotas are fabricated. Existing saved layout and dock choices remain compatible.

Minute refreshes update existing text and progress bars without rebuilding controls or resetting the dock size. A collapsed dock keeps the same tab through background account/data changes. The opacity menu displays the saved percentage beside its slider; the expanded dock remains opaque for readability. Hidden and docked windows do not appear on the taskbar. Only an open Floating widget has a taskbar entry; the tray icon stays available to reopen it.

## Branding (v1.3.1)

The approved C logo is bundled into the installer welcome/progress screens, application executable, taskbar, notification tray, Start/desktop shortcuts and installed-apps entry. The expanded widget includes the centered attribution: "Made with ♥ by Syafiee Anis @ 2026". Source artwork and Windows icon variants are in `assets/`.

Version 1.3.2 uses a friendly black C mascot with eyes and waving hands. An opaque white backing keeps it visible on dark Windows surfaces; the bold black body remains visible on light ones. The original white C assets are retained as history.
