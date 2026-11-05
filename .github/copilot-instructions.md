# Copilot instructions for Vocaluxe

Vocaluxe is a free, open source karaoke/singing game written in C# (.NET 10). These notes help with
code reviews and suggestions in this repository.

## Projects

- `Vocaluxe` – the game (WinExe), `VocaluxeLib` – shared library for the game and the party modes
- `PartyModes/*` – party modes, loaded as plugins
- `VocaluxeWeb` – ASP.NET Core web server for the companion web app, reachable in the local network
- `WebApp` – Vue 3 / TypeScript web app (Vite), built by `VocaluxeWeb` with Node.js
- `PitchTracker` – native C++ library for pitch detection
- `Tests` – NUnit tests
- `Installer` – Inno Setup script for the Windows installer
- CI: `.github/workflows/ci.yml` (GitHub Actions)

## Build configurations

- Configurations: `DebugWin`, `ReleaseWin`, `InstallerWin`, `DebugLinux`, `ReleaseLinux`;
  platforms `x64` and `x86` (see `Directory.Build.props`)
- Preprocessor symbols: `WIN` / `LINUX`, `ARCH_X64` / `ARCH_X86`, `INSTALLER`
- Windows-only code (Direct3D, NAudio, AForge webcam, ...) must stay behind `#if WIN` or be excluded
  for Linux builds, so `ReleaseLinux` keeps compiling
- In the `InstallerWin` configuration user data (config, profiles, highscores, songs, logs) is stored in
  `Documents\Vocaluxe`, because the program folder is not writable

## Code conventions

- Follow the style of the surrounding code
- Classes start with `C` (`CBase`), interfaces with `I`, enums with `E`, structs with `S`
- Private fields use a leading underscore and PascalCase (`_Timer`)
- C# source files start with the GPL license header (`#region license`)

## Review focus

- Bugs, crashes and missing null/bounds checks, especially when reading song files (`.txt`), covers,
  videos and other user-provided files
- `VocaluxeWeb`: input validation, path traversal, authentication/authorization of API endpoints
- Thread safety: drawing, sound, video and the web server run on different threads
- Resource handling: dispose native resources (FFmpeg, audio, textures) and avoid leaks in loops
- Platform differences (Windows/Linux, x86/x64) and file paths (use `Path.Combine`, no hard-coded `\`)

## Translations

- Texts are translated with Transifex. Only change the English source files
  (`Output/Languages/en.xml`, `Output/PartyModes/*/Languages/en.xml`, `WebApp/src/locales/en.json`);
  other languages are updated by Transifex pull requests.
