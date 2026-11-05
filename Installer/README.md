# Vocaluxe Windows installer

The installer is built with [Inno Setup](https://jrsoftware.org/isinfo.php) (6.3 or newer) from a
build of the `InstallerWin` configuration. In this configuration Vocaluxe stores config, highscores,
profiles, songs, logs and screenshots in `Documents\Vocaluxe`, because the program folder
(`C:\Program Files\Vocaluxe`) is not writable for normal users.

## Requirements

- Visual Studio 2026 or Build Tools with the .NET 10 SDK and the C++ workload
- Node.js LTS (for the WebApp)
- Inno Setup 6.3+ (`winget install JRSoftware.InnoSetup`)

## Build

```powershell
.\Installer\build-installer.ps1                         # x64 and x86
.\Installer\build-installer.ps1 -Platform x64           # only x64
.\Installer\build-installer.ps1 -Platform x64 -SkipBuild  # only pack the current Output folder
```

The setups are written to `Installer\Setup Files`:

- versioned build: `Vocaluxe_<version>_Windows_Installer_<arch>.exe`, e.g. `Vocaluxe_0.5.0_Windows_Installer_x64.exe`
- unversioned/nightly build (version 0.0.0): `Vocaluxe_Nightly_Windows_Installer_<arch>.exe`

AppVeyor builds the setups automatically for every platform and attaches them to the GitHub
release together with the ZIP files. There the setups are named like the ZIP files, after the
tag (or the branch as fallback), e.g. `Vocaluxe_Nightly_Windows_Installer_x64.exe` for the
nightly release, regardless of the version stored in the setup. Pull requests skip the installer, unless the message of the
latest commit contains `[build installer]`.

To pack a build by hand, build `InstallerWin|x64` (or `x86`) in Visual Studio and run
`ISCC.exe /DArch=x64 Installer\Vocaluxe.iss`.

## What the setup does

- Installs everything from `Output` except user data (config, databases, songs, logs, ...),
  the Linux files and the libraries of the other platform
- Checks for the **.NET Runtime 10** and the **ASP.NET Core Runtime 10** (needed by the web server)
  of the matching platform and downloads and installs them if missing
- Creates start menu and (optional) desktop shortcuts and a `Songs` folder next to the program
- Removes the native libraries and leftovers of Vocaluxe 0.4 (Ambient theme, ChallengeMedley) on update

No DirectX or Visual C++ runtime is needed: all native libraries are linked statically or only
use the Universal CRT that is part of Windows 10 and newer.

## Files

| File | Purpose |
|---|---|
| `Vocaluxe.iss` | Inno Setup script |
| `build-installer.ps1` | Builds the solution and the setups |
| `License.rtf` | License shown in the setup |
| `Graphics\WizardImage.bmp`, `Graphics\WizardSmallImage.bmp` | Setup images (made from `dialog.jpg` and `banner.jpg`) |
