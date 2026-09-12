# Codex Usage Widget

Windows 11 x64 desktop widget, written in WPF. Black and white themes; Compact, Card and Detailed layouts; draggable header; always-on-top toggle; tray icon; automatic refresh every 60 seconds. This is a floating desktop window, not an extension inside the Windows Widgets board.

## Install

Download the [Windows 11 x64 MSI installer](https://github.com/muhamadsyafiee/codex-usage/releases/latest). Release assets include the installer and its SHA-256 checksum. Exit the existing app before upgrading.

Run `dist/CodexUsageWidget-1.1.0-x64.msi`, then open **Codex Usage Widget** from Start. Select **Log masuk** and finish the official ChatGPT browser login. Remaining limits appear automatically. Use **···** to change layout/theme or sign out. **Sorok** hides to the tray; double-click the tray icon to restore. **Keluar** exits.

Installer includes .NET and the official Codex CLI 0.154.0. No Node.js or separate Codex install is required. Internet and a ChatGPT account with available Codex limits are required. Installer is unsigned.

## Dock mode (v1.1)

Open **··· → Dock skrin** and choose **Kiri**, **Kanan**, **Atas**, or **Bawah**. A small CODEX tab stays at the chosen edge. Hover to reveal usage; moving away collapses the panel after 450 ms. Right-click the tab to change its settings. Choose **Bebas** to restore the floating position. Dock mode stays on top and uses the monitor work area, keeping clear of the taskbar. To move to another monitor, switch to Bebas, drag there, then dock again. Layout, theme, login and floating position survive upgrades. Exit the old app before running the new MSI.

## Data and authentication

Uses the official Codex App Server JSON-RPC methods `account/login/start`, `account/read`, `account/rateLimits/read`, and `account/logout` over local standard input/output. No password collection, analytics, model calls or usage-credit redemption. Login is stored by Codex in a separate `%LOCALAPPDATA%/CodexUsage/session` directory. It does not use or sign out the existing Codex desktop session. Sign out before uninstalling to clear the managed login; uninstall retains per-user settings/session files.

Remaining percentage is `clamp(100 - usedPercent, 0, 100)`. Multiple limit buckets are supported. Null windows are omitted; unknown percentages are shown as unavailable. Failed refresh clears displayed readings instead of showing stale values as current. Reset timestamps use the local timezone.

Official protocol: https://learn.chatgpt.com/docs/app-server

## Build

Requires Windows, .NET 10 SDK and Node.js/npm. Run `./build.ps1`. Build installs WiX 6.0.2 into `.tools`, downloads Codex through npm, publishes a self-contained x64 application and creates the MSI in `dist`.

Authentication completion and account-backed usage must be checked with an interactive user login. No account credentials are included in this repository or installer.
