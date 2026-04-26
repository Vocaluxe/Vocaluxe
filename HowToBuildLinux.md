# Build Vocaluxe - HowTo (Linux)

Running Vocaluxe on Linux is not officially supported, but it can be built and run using Mono and the provided nix dev shell.

## Requirements

- [Nix](https://nixos.org/download/) with flakes enabled
- A display server: X11 or Wayland (XWayland also works)

## Build

```sh
nix develop
make
```

The `make` target runs the full build: restores NuGet packages, builds the PitchTracker and Acinerella native libraries, and builds the managed assemblies.

For a debug build step by step:

```sh
nix develop
msbuild /t:Restore /p:Configuration=DebugLinux /p:Platform=x64 /p:TargetFrameworkVersion=v4.8 Vocaluxe.sln
make -C PitchTracker
make -C Vocaluxe/Lib/Video/Acinerella
msbuild /p:Configuration=DebugLinux /p:Platform=x64 /p:TargetFrameworkVersion=v4.8 Vocaluxe.sln
```

## Run

```sh
nix develop
cd Output && mono Vocaluxe.exe
```

The nix dev shell sets `LD_LIBRARY_PATH`, `MONO_PATH`, and `GST_PLUGIN_PATH` automatically so no manual configuration is needed.

## Known limitations

- **No application icon** — Mono does not support PNG-compressed `.ico` files. The window has no icon; this is cosmetic only.
- **No GStreamer audio backend** — The GStreamer C# binding requires a native glue library (`libgstreamersharpglue.so`) that is not packaged for Linux. The app automatically falls back to the PortAudio backend, which works fully.
- **Missing skin assets** — The default skin references video files (`BG_Video.mp4` etc.) and a texture (`TextBG`) that are not included in the repository. These produce warnings but do not affect functionality.
- **ALSA error spam on startup** — PortAudio enumerates audio devices on startup, which produces harmless ALSA error messages in the terminal.

## Testing Windows compatibility with Wine

You can smoke-test a Windows release under Wine to verify Windows compatibility without a real Windows machine.

### One-time setup

```sh
winetricks vcrun2010
```

This installs the VC++ 2010 runtime that PitchTracker.dll depends on.

### Fix the release before running

Download and extract a Windows release zip. Before running it, two things need to be fixed in the extracted folder:

1. **Remove the unconditional PortAudio dllmap** — `PortAudioSharp.dll.config` (in the root and in `libs/managed/`) contains a dllmap that redirects `PortAudio.dll` to `libportaudio.so.2` on all platforms. Under Wine, Mono still sees itself as Linux and applies the redirect, then Wine rejects the `.so` as not a PE binary. Remove the `<dllmap>` line from both copies.

2. **Copy native DLLs to the root** — Mono resolves P/Invoke relative to the executable directory:

```sh
cp libs/unmanaged/portaudio.dll .
cp libs/unmanaged/portaudio.dll libs/managed/
cp libs/unmanaged/x64/PitchTracker.dll .
```

### Run

```sh
DISPLAY=:0 wine Vocaluxe.exe
```
