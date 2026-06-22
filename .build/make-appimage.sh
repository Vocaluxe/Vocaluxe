#!/usr/bin/env bash
#
# Packages Vocaluxe as a self-contained Linux AppImage (part of the cross-platform port, #768).
#
# Strategy: a self-contained .NET build (bundled runtime) + the native helper libs
# (libSkiaSharp/libPitchTracker/libacinerella) are placed in an AppDir. On top of that we bundle
# the *runtime* shared libraries the game dlopen()s - PortAudio, FFmpeg (libav*) and fontconfig -
# together with their transitive dependencies, EXCEPT the core system/graphics libraries that must
# come from the host (glibc, libstdc++, libGL, X11, ALSA, ...). appimagetool then wraps it.
#
# Result: a single .AppImage that runs without installing libportaudio2/ffmpeg/libfontconfig1.
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
mkdir -p "$APPDIR/usr/bin" "$APPDIR/usr/lib" "$APPDIR/usr/share/applications" "$APPDIR/usr/share/icons/hicolor/256x256/apps"
cp -r "$ROOT/dist/Vocaluxe/." "$APPDIR/usr/bin/"

# ---------------------------------------------------------------------------
# Bundle the dlopen()'d runtime libraries and their transitive dependencies.
# ---------------------------------------------------------------------------
LIBDIR="$APPDIR/usr/lib"

# Core libraries that must be provided by the host (ABI must match the running system / GPU / X
# server). Everything else a seed library needs is copied in.
_is_excluded() {
    case "$1" in
        ld-linux*|libc.so*|libm.so*|libdl.so*|libpthread.so*|librt.so*|libutil.so*|libresolv.so*|\
        libgcc_s.so*|libstdc++.so*|\
        libGL.so*|libGLX*|libEGL*|libGLdispatch*|libOpenGL*|libglapi*|libgbm*|libdrm*|\
        libX11*|libXext*|libXau*|libXdmcp*|libxcb*|libxshmfence*|libwayland*|libffi.so*|\
        libasound.so*|libpulse*|libpulsecommon*|libjack*|libpipewire*|\
        libdbus*|libsystemd*|libudev*|libcap.so*) return 0;;
        *) return 1;;
    esac
}

_collect_deps() {
    local lib="$1"
    ldd "$lib" 2>/dev/null | awk '/=>/ {print $3} !/=>/ && /^\// {print $1}' | while read -r dep; do
        [ -n "$dep" ] && [ -f "$dep" ] || continue
        local base; base="$(basename "$dep")"
        _is_excluded "$base" && continue
        [ -e "$LIBDIR/$base" ] && continue
        cp -L "$dep" "$LIBDIR/$base"
        _collect_deps "$dep"
    done
}

# Seeds: the native libs we ship (their NEEDED entries pull in the FFmpeg/fontconfig trees) plus
# the system PortAudio the app loads by name.
SEEDS=("$APPDIR/usr/bin/libacinerella.so" "$APPDIR/usr/bin/libSkiaSharp.so" "$APPDIR/usr/bin/libPitchTracker.dll.so")
PA="$(ldconfig -p 2>/dev/null | awk -F'=> ' '/libportaudio\.so\.2/ {print $2; exit}')"
if [ -n "$PA" ] && [ -f "$PA" ]; then
    cp -L "$PA" "$LIBDIR/libportaudio.so.2"
    ln -sf libportaudio.so.2 "$LIBDIR/libportaudio.so"
    SEEDS+=("$PA")
else
    echo "   WARNING: libportaudio.so.2 not found on host - audio may not work in the AppImage"
fi

for s in "${SEEDS[@]}"; do
    [ -f "$s" ] && _collect_deps "$s"
done
echo "   bundled $(ls -1 "$LIBDIR" | wc -l) runtime libraries into usr/lib"

# ---------------------------------------------------------------------------
# Icon / desktop entry / AppRun
# ---------------------------------------------------------------------------
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
# Bundled runtime libs first, then the app dir (native helpers live next to the executable).
export LD_LIBRARY_PATH="$HERE/usr/lib:$HERE/usr/bin${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
exec "$HERE/usr/bin/Vocaluxe" "$@"
RUN
chmod +x "$APPDIR/AppRun"

echo ">> Locating appimagetool"
if [ -z "$TOOL" ]; then
    # Cache the build tool OUTSIDE dist/ so the output directory only ever contains the deliverable
    # (Vocaluxe-x86_64.AppImage) - having appimagetool's own .AppImage next to it is confusing.
    CACHE="${XDG_CACHE_HOME:-$HOME/.cache}/vocaluxe-build"
    mkdir -p "$CACHE"
    TOOL="$CACHE/appimagetool-x86_64.AppImage"
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
