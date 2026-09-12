# Application branding

`logo-c.png` is the user-approved C logo generated in this task. Preserve the original.

`app.ico` packages this image in 16, 20, 24, 32, 40, 48, 64, 128 and 256 px resolutions. It is embedded in the executable and WPF resources, used for the window/taskbar and notification area, and attached to MSI shortcuts and the installed-apps entry.

`installer-logo.bmp` is a 300 px RGB conversion for the native Windows Installer bitmap control. The same logo appears on the welcome and progress screens. Format conversion preserves the approved composition.

No additional image runtime is needed to build: generated ICO/BMP assets are checked in.
