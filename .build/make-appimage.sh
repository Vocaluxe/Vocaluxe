#!/usr/bin/env bash
#
# Packages Vocaluxe as a Linux AppImage (part of the cross-platform port, #768).
#
# Strategy: a self-contained .NET build (bundled runtime) + SkiaSharp/PitchTracker
# native libs are placed in an AppDir; appimagetool wraps it. System multimedia
# libraries (libportaudio2, libfontconfig1, libSDL2) are expected on the host -
# they are near-ubiquitous; bundle them with linuxdeploy if you need full isolation.
#
# Requires: appimagetool (auto-downloaded if missing; needs network + FUSE),
#           optionally ImageMagick `convert` for the icon.
#
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
APPDIR="$ROOT/dist/Vocaluxe.AppDir"
OUT="$ROOT/dist/Vocaluxe-x86_64.AppImage"
TOOL="${APPIMAGETOOL:-}"

echo ">> Building self-contained app"
SELFCONTAINED=true "$ROOT/.build/build-linux.sh"

echo ">> Assembling AppDir"
rm -rf "$APPDIR"
mkdir -p "$APPDIR/usr/bin" "$APPDIR/usr/share/applications" "$APPDIR/usr/share/icons/hicolor/256x256/apps"
cp -r "$ROOT/dist/Vocaluxe/." "$APPDIR/usr/bin/"

# Icon: convert the .ico, fall back to a logo PNG from the game data
if command -v convert >/dev/null 2>&1; then
    convert "$ROOT/Output/Vocaluxe.ico[0]" "$APPDIR/vocaluxe.png" 2>/dev/null || true
fi
if [ ! -f "$APPDIR/vocaluxe.png" ]; then
    cp "$ROOT/Output/Graphics/Logo.png" "$APPDIR/vocaluxe.png" 2>/dev/null || \
        cp "$ROOT/Output/Graphics/CreditsLogo.png" "$APPDIR/vocaluxe.png" 2>/dev/null || true
fi
[ -f "$APPDIR/vocaluxe.png" ] && cp "$APPDIR/vocaluxe.png" "$APPDIR/usr/share/icons/hicolor/256x256/apps/vocaluxe.png"

cat > "$APPDIR/vocaluxe.desktop" <<'DESK'
[Desktop Entry]
Type=Application
Name=Vocaluxe
Comment=Free and open source singing game
Exec=Vocaluxe
Icon=vocaluxe
Terminal=false
Categories=Game;
DESK
cp "$APPDIR/vocaluxe.desktop" "$APPDIR/usr/share/applications/"

cat > "$APPDIR/AppRun" <<'RUN'
#!/usr/bin/env bash
HERE="$(dirname "$(readlink -f "$0")")"
exec "$HERE/usr/bin/Vocaluxe" "$@"
RUN
chmod +x "$APPDIR/AppRun"

echo ">> Locating appimagetool"
if [ -z "$TOOL" ]; then
    TOOL="$ROOT/dist/appimagetool-x86_64.AppImage"
    if [ ! -f "$TOOL" ]; then
        echo "   downloading appimagetool (needs network)"
        curl -fsSL -o "$TOOL" \
            https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage
        chmod +x "$TOOL"
    fi
fi

echo ">> Building AppImage"
ARCH=x86_64 "$TOOL" "$APPDIR" "$OUT"
echo ">> Done: $OUT"
