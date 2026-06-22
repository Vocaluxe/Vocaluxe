#!/usr/bin/env bash
#
# Builds Vocaluxe for Linux into ./dist/Vocaluxe and writes a launcher ./dist/Vocaluxe.sh
#
# This is part of the cross-platform port (#768). It:
#   1. compiles the native PitchTracker helper (libPitchTracker.dll.so)
#   2. publishes the managed app with dotnet
#   3. assembles the game data (Output/) next to the executable
#   4. drops in the native helper library
#
# Environment overrides:
#   DOTNET         dotnet command/path        (default: dotnet)
#   RID            runtime identifier          (default: linux-x64)
#   SELFCONTAINED  bundle the .NET runtime     (default: true)
#
# System runtime dependencies (install via your package manager):
#   libportaudio2  libfontconfig1  libSDL2-2.0-0
#   (optional) gstreamer1.0  libhidapi-hidraw0 (HID/Wiimote)
#
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
DIST_ROOT="$ROOT/dist"
DIST="$DIST_ROOT/Vocaluxe"
DOTNET="${DOTNET:-dotnet}"
RID="${RID:-linux-x64}"
SELFCONTAINED="${SELFCONTAINED:-true}"

echo ">> [1/4] Building native helpers (PitchTracker, Acinerella)"
make -C "$ROOT/PitchTracker"
# Acinerella (FFmpeg wrapper for audio/video decode) needs the ffmpeg dev headers
# (libavcodec-dev libavformat-dev libswscale-dev libavutil-dev libswresample-dev).
make -C "$ROOT/Vocaluxe/Lib/Video/Acinerella"

echo ">> [2/4] Publishing managed app ($RID, self-contained=$SELFCONTAINED)"
rm -rf "$DIST"
"$DOTNET" publish "$ROOT/Vocaluxe/Vocaluxe.csproj" \
    -c Release -r "$RID" --self-contained "$SELFCONTAINED" \
    -p:DebugType=none -o "$DIST"

echo ">> [3/4] Copying game data (Themes, Graphics, Fonts, Languages, ...)"
cp -ru "$ROOT/Output/." "$DIST/"
# Windows-only leftovers
rm -f "$DIST"/*.ico 2>/dev/null || true

echo ">> [4/5] Native helper libraries"
cp "$ROOT/PitchTracker/libPitchTracker.dll.so" "$DIST/"
cp "$ROOT/Vocaluxe/Lib/Video/Acinerella/libacinerella.so" "$DIST/"

echo ">> [5/5] Party-mode sources (compiled at runtime via Roslyn -> PartyModes/<Mode>/Code)"
# CParty._CompileFiles compiles these .cs at runtime; the build must place them next to the mode's
# data as <Mode>/Code (mirrors the original Windows build step).
_copy_pm_sources() {
    local src="$1" dst="$2"
    mkdir -p "$DIST/PartyModes/$dst/Code"
    cp "$ROOT/PartyModes/$src"/*.cs "$DIST/PartyModes/$dst/Code/"
}
_copy_pm_sources PartyModeChallenge Challenge
_copy_pm_sources PartyModeTicTacToe TicTacToe

# Launcher that runs from the dist directory regardless of CWD
cat > "$DIST_ROOT/Vocaluxe.sh" <<'LAUNCH'
#!/usr/bin/env bash
DIR="$(cd "$(dirname "$0")/Vocaluxe" && pwd)"
if [ -x "$DIR/Vocaluxe" ]; then
    exec "$DIR/Vocaluxe" "$@"      # self-contained build
else
    exec dotnet "$DIR/Vocaluxe.dll" "$@"  # framework-dependent build
fi
LAUNCH
chmod +x "$DIST_ROOT/Vocaluxe.sh"

echo ">> Done."
echo "   App:      $DIST"
echo "   Launcher: $DIST_ROOT/Vocaluxe.sh"
