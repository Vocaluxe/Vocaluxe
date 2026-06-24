# Build Vocaluxe on Windows (.NET 10)

Since the cross-platform port (issue #768) Vocaluxe builds with the **.NET 10 SDK** from one
cross-platform code base — the same sources build on Windows, Linux and macOS (OpenGL + PortAudio
everywhere; the old Direct3D / DirectSound / WinForms paths were dropped). The legacy Visual Studio
`ReleaseWin` / .NET Framework 4.x flow no longer applies — the projects are SDK-style and target `net10.0`.

## 1. Prerequisites

* **.NET 10 SDK** — <https://dotnet.microsoft.com/download>
* **Visual Studio 2022** (17.x) with the **"Desktop development with C++"** workload — needed to build the
  native `PitchTracker.dll` (a C++ project, `PitchTracker\PitchTracker.vcxproj`, still part of
  `Vocaluxe.sln`). JetBrains Rider works for the managed code but won't build the C++ project.
* The **multimedia native libraries** — `acinerella.dll` and the FFmpeg DLLs (`avcodec-*`, `avformat-*`,
  `avutil-*`, `swresample-*`, `swscale-*`) — are **not** built here. Take them from an official Windows
  build / nightly (<https://vocaluxe.org/#download>); see step 4. `portaudio.dll` ships with the
  PortAudioSharp2 NuGet package automatically.

## 2. Clone

```cmd
git clone https://github.com/Vocaluxe/Vocaluxe.git
cd Vocaluxe
```

## 3. Build

**Visual Studio (recommended — also builds `PitchTracker.dll`):**

* Open `Vocaluxe.sln`, pick the `Release` configuration, and build the solution (`Ctrl`+`Shift`+`B`).

**Command line (managed only — does not build the C++ `PitchTracker`):**

```cmd
:: compile while developing
dotnet build Vocaluxe\Vocaluxe.csproj -c Release

:: or a self-contained, runnable build under .\publish
dotnet publish Vocaluxe\Vocaluxe.csproj -c Release -r win-x64 --self-contained true -o publish
```

`dotnet publish` brings the .NET runtime and the SkiaSharp native, but not the C/C++ helpers (step 4).

## 4. Assemble a runnable layout

Next to the built/published `Vocaluxe.exe`:

* Copy the game data: everything under `Output\` (Themes, Graphics, Fonts, Languages, Sounds, Website).
* Place the native helpers where the app adds them to its DLL search path at startup:
  * `PitchTracker.dll` (built in step 3, or from an official build) → `libs\unmanaged\x64\`
  * `acinerella.dll` + the FFmpeg `av*.dll` → `libs\unmanaged\`
  * `portaudio.dll` is already in the output via PortAudioSharp2.

(This is exactly what the CI "Windows (x64)" job in `.github/workflows/ci.yml` does — it builds acinerella
from source and copies the FFmpeg/PitchTracker DLLs out of the official Windows nightly.)

## 5. Run

```cmd
Vocaluxe.exe
```

User config, profiles, songs and logs are written **next to `Vocaluxe.exe`** (the portable build stores its
data in the program folder; only the installer build uses `Documents\Vocaluxe\`).

---

> **Status:** the Windows build of the .NET 10 port currently **compiles and passes the unit tests in CI
> on `windows-latest`, but has not yet been run on real Windows hardware** — feedback from a hands-on test
> is very welcome. (Linux is validated end-to-end; macOS is experimental.)
