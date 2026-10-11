# Quiver Launcher 3.5.1-rc.1

This is a prerelease for testing before 3.5.1.

## Safer installs and updates

- If several antivirus engines flag the file Quiver is about to download, Quiver asks first and says your antivirus may block the download or remove the app's files. Automatic updates leave these for you to install yourself.
- The app page says what **Verified** means: the release comes from the app's usual developer, its files haven't been swapped, and it waited 48 hours with its files scanned by VirusTotal.
- When a developer removes or replaces a release file, Quiver says so and offers the newest other verified release.
- Apps that rebuild the same release, like nightly builds, install again when the file matches the one GitHub publishes.

## Windows

- More recent gamepads are recognised, because Quiver now ships a newer SDL2 (2.32.10).

## Linux

- Programs with dots in their names install, like P.T.'s `P.T.PC.Port.Setup-linux` and files ending in `.x86_64`.
- Flatpak bundles that come inside a zip are offered, and install like any other Flatpak.
- The AppImage uses Quiver's data in `~/.local/share/QuiverLauncher` when it's already there. **Settings → General → Quiver data folder** has a button that moves it there. Installed apps stay where they are.

## macOS

- **Create Desktop Shortcut** works, and picking a `.app` from several programs no longer fails.
- The controller stays with a `.app` game until you quit it, instead of jumping back to Quiver.
- Quiver's data can move to Application Support, with the same button as on Linux.

## Android

- Quiver no longer crashes on start when the library uses compact grid cards.
- App Catalog artwork and app page icons show again.
- Installing from the list view works.
- Closing Quiver no longer crashes.

## Other improvements and fixes

- Sub-menus follow your theme instead of opening dark grey.
- The **Release asset filter** accepts wildcards: `*` for any text and `?` for one character, like `*portable*x64*`. Plain text still matches anywhere in the file name.

Thanks to sdelavega for the macOS shortcut and controller fixes.
