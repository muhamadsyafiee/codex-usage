# Application branding

`logo-c.png` is the user-approved C logo generated in this task. Preserve the original.

`app.ico` packages this image in 16, 20, 24, 32, 40, 48, 64, 128 and 256 px resolutions. It is embedded in the executable and WPF resources, used for the window/taskbar and notification area, and attached to MSI shortcuts and the installed-apps entry.

`installer-logo.bmp` is a 300 px RGB conversion for the native Windows Installer bitmap control. The same logo appears on the welcome and progress screens. Format conversion preserves the approved composition.

No additional image runtime is needed to build: generated ICO/BMP assets are checked in.

## Friendly mascot (v1.3.2)

The active artwork is `logo-c-mascot.png`: an original black C character with friendly white eyes and waving hands on an opaque white canvas. The white backing prevents black details disappearing on dark taskbars; the black body prevents the old white icon disappearing in light Windows Settings. It replaces the original white C in the executable, tray, installer, shortcuts and installed-apps icon.

`app-mascot.ico` contains 16–256 px versions. `installer-mascot.bmp` is the matching 300 px installer image. Earlier assets are retained as source history and are no longer referenced by the build.

Created with the built-in image generation tool. Final prompt direction: preserve the friendly C silhouette, use a solid black body with white eyes/hands, remove fine details, and replace the background with opaque white for reliable small Windows icons. ICO/BMP exports only resize/convert the approved artwork.
