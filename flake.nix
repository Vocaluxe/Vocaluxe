{
  description = "Vocaluxe development environment";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { self, nixpkgs, flake-utils }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = nixpkgs.legacyPackages.${system};
      in {
        devShells.default = pkgs.mkShell {
          name = "vocaluxe-dev";

          packages = with pkgs; [
            # Build tools
            mono
            msbuild
            nuget
            gcc
            gnumake

            # Native libs (build + runtime)
            portaudio
            ffmpeg_6
            sqlite
            pkg-config

            # GStreamer
            gst_all_1.gstreamer
            gst_all_1.gst-plugins-base
            gst_all_1.gst-plugins-good
            gst_all_1.gst-plugins-bad
            gst_all_1.gst-plugins-ugly
            gst_all_1.gst-libav

            # OpenGL
            mesa
            libGL

            # Other runtime deps
            openal
          ];

          shellHook = ''
            export PKG_CONFIG_PATH="${pkgs.portaudio}/lib/pkgconfig:${pkgs.ffmpeg_6}/lib/pkgconfig:$PKG_CONFIG_PATH"
            # Use the system libGL/libGLX so the host GL vendor (NVIDIA/Mesa) is used.
            # Nix's libglvnd is a dispatch stub that doesn't know about host vendor libs on non-NixOS.
            export LD_LIBRARY_PATH="${pkgs.portaudio}/lib:${pkgs.sqlite.out}/lib:${pkgs.gst_all_1.gstreamer}/lib:${pkgs.gst_all_1.gst-plugins-base}/lib:${pkgs.openal}/lib:/usr/lib:$LD_LIBRARY_PATH"
            export GST_PLUGIN_PATH="${pkgs.gst_all_1.gst-plugins-base}/lib/gstreamer-1.0:${pkgs.gst_all_1.gst-plugins-good}/lib/gstreamer-1.0:${pkgs.gst_all_1.gst-plugins-bad}/lib/gstreamer-1.0:${pkgs.gst_all_1.gst-plugins-ugly}/lib/gstreamer-1.0:${pkgs.gst_all_1.gst-libav}/lib/gstreamer-1.0"
            # Mono assembly search path: managed NuGet packages deployed alongside the binary.
            # The app.config probing element handles this at runtime; MONO_PATH is a fallback.
            VOCALUXE_ROOT="$(git -C "$PWD" rev-parse --show-toplevel 2>/dev/null || echo "$PWD")"
            export MONO_PATH="$VOCALUXE_ROOT/Output/libs/managed:$VOCALUXE_ROOT/Output/libs/unmanaged:$VOCALUXE_ROOT/Output/libs"
            export LD_LIBRARY_PATH="$VOCALUXE_ROOT/Output:$LD_LIBRARY_PATH"
            echo "Vocaluxe dev shell ready."
            echo ""
            echo "Build (first time or after adding packages):"
            echo "  msbuild /t:Restore /p:Configuration=DebugLinux /p:Platform=x64 /p:TargetFrameworkVersion=v4.8 Vocaluxe.sln"
            echo "  make -C PitchTracker"
            echo "  make -C Vocaluxe/Lib/Video/Acinerella"
            echo "  msbuild /p:Configuration=DebugLinux /p:Platform=x64 /p:TargetFrameworkVersion=v4.8 Vocaluxe.sln"
            echo ""
            echo "Run (requires a display / X11 session):"
            echo "  cd Output && mono Vocaluxe.exe"
          '';
        };
      });
}
