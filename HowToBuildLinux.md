# Build Vocaluxe on Linux (.NET 10)

As of the cross-platform port (see issue #768) Vocaluxe builds and runs natively
on Linux on **.NET 10** — no Mono required. It is a single cross-platform code
base: the same sources build on Windows, Linux and macOS (OpenGL + PortAudio
everywhere; the old Direct3D/DirectSound/WinForms paths were dropped).

## 1. Prerequisites

* **.NET 10 SDK** — https://dotnet.microsoft.com/download (or your distro's `dotnet-sdk-10.0`)
* **gcc/g++ and make** — to build the native helpers (`PitchTracker`, `Acinerella`)
* **FFmpeg dev headers (build-time)** — to compile the Acinerella audio/video wrapper:

  ```bash
  sudo apt install -y libavcodec-dev libavformat-dev libswscale-dev libavutil-dev libswresample-dev
  ```

* **Runtime system libraries:**

  ```bash
  sudo apt install -y libportaudio2 libfontconfig1
  # the ffmpeg runtime libs (libavcodec/…) come in as deps of the -dev packages above
  # optional: libhidapi-hidraw0   (HID / Wiimote)
  ```

  (Equivalent packages on other distributions.)

## 2. Build & run (one step)

```bash
./.build/build-linux.sh        # builds PitchTracker + publishes the app into ./dist/Vocaluxe
./dist/Vocaluxe.sh             # run it
```

Environment overrides for `build-linux.sh`:
`DOTNET` (dotnet path), `RID` (default `linux-x64`),
`SELFCONTAINED` (`true` bundles the .NET runtime, default `true`).

## 3. Build an AppImage (optional)

```bash
./.build/make-appimage.sh      # -> ./dist/Vocaluxe-x86_64.AppImage
```

Needs `appimagetool` (auto-downloaded; requires network + FUSE) and, optionally,
ImageMagick for the icon. The resulting AppImage is **fully self-contained**: it
bundles the .NET runtime, the SkiaSharp/PitchTracker/Acinerella native libraries
**and** the multimedia system libraries (PortAudio, FFmpeg, fontconfig) plus their
dependencies — so it runs on a host without those packages installed.

## 4. Developing / manual build

```bash
make -C PitchTracker                                   # native pitch detector -> libPitchTracker.dll.so
dotnet build Vocaluxe/Vocaluxe.csproj -c Release       # managed build
```

The game data lives in `Output/`; the app resolves it relative to the
executable, which is why `build-linux.sh` copies `Output/` next to the published
binaries.

## Notes / current limitations

* The browser remote-control **webserver is disabled** on this build (the old WCF
  implementation is gone; an ASP.NET Core replacement is planned).
* Gamepad (OpenTK 1.x) input is stubbed; Wiimote needs `libhidapi`.
* Windows builds are kept working by design but are not verified in this port.
