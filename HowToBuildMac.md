# Build Vocaluxe - HowTo (macOS)

Vocaluxe runs on Macs with Apple Silicon (M1 or newer) and macOS 14 (Sonoma) or later. Intel Macs are not supported.
The app is self-contained: the .NET runtime, FFmpeg, GStreamer, PortAudio and hidapi are included.

## Requirements for building

* Xcode command line tools: `xcode-select --install`
* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and [Node.js (LTS)](https://nodejs.org)
  (e.g. `brew install dotnet node`). The app contains the official .NET runtime of Microsoft in the version of the
  installed SDK; `build-app.sh` downloads it once into `MacOS/Dependencies`.

## Build the app

```bash
MacOS/build-app.sh
open MacOS/Build/Vocaluxe.app
```

The script builds `PitchTracker`, downloads the theme videos, builds the solution in the configuration `ReleaseMac`
(platform `arm64`) and creates `MacOS/Build/Vocaluxe.app` with the .NET runtime and the native libraries
(downloaded once from VocaluxeDependencies into `MacOS/Dependencies`).
Options: `--debug` builds `DebugMac`, `--no-videos` skips the download of the theme videos, `--dmg` also creates
the disk image `MacOS/Build/Vocaluxe.dmg` (the app and a link to the Applications folder, as published by the CI).

## Native libraries (dependency ZIPs)

The native libraries come from VocaluxeDependencies (`zips/dependencies`), one ZIP per library like FFmpeg and
GStreamer of the Windows build. `MacOS/build-dependencies.sh` creates them in `MacOS/Dependencies`:

* `ffmpeg_macos-arm64.zip`: FFmpeg 8 (LGPL, decoders only), built from source for macOS 14
* `gstreamer_macos-arm64.zip`: GStreamer from the official macOS framework (only the plugins Vocaluxe uses) and the
  glue library for gstreamer-sharp (`GstreamerSharpGlue`)
* `portaudio_macos-arm64.zip`, `hidapi_macos-arm64.zip`: PortAudio and hidapi, built from source for macOS 14

It needs `brew install cmake pkg-config gstreamer` (only to build; GStreamer of Homebrew provides the headers).
To update the libraries: change the versions in the script, create the ZIPs (`build-app.sh` uses them as long as they
are in `MacOS/Dependencies`), test the app and upload the changed ZIPs to VocaluxeDependencies.

The CI workflow builds macOS for pull requests only with `[build mac]` in the latest commit message; pushes to develop
and tags always build it.

## Notes

* On the first start macOS asks for access to the microphone (and to the camera if a webcam is used). If access was
  denied, allow it in System Settings > Privacy & Security > Microphone/Camera.
* The app is not signed by Apple. A downloaded app needs `xattr -dr com.apple.quarantine Vocaluxe.app` before the
  first start.
* The configuration, profiles, highscores, logs and your songs are stored in `~/Library/Application Support/Vocaluxe`
  (put your songs into its `Songs` folder). Songs in the program folder are also loaded.
