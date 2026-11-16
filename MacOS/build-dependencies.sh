#!/bin/sh
# Builds the native libraries that are shipped with the macOS app and packs them into one ZIP per library in
# MacOS/Dependencies (uploaded to VocaluxeDependencies, zips/dependencies, like the ZIPs of the Windows build).
# The ZIPs are unpacked into libs/unmanaged of the app:
#
#   ffmpeg_macos-arm64.zip     ffmpeg64/            FFmpeg 8 (LGPL, decoders only), built from source
#   gstreamer_macos-arm64.zip  gstreamer/           GStreamer (official macOS framework), only the libraries and plugins
#                                                   Vocaluxe uses, and libgstreamersharpglue-1.0.0.dylib
#   portaudio_macos-arm64.zip  libportaudio.dylib   PortAudio, built from source
#   hidapi_macos-arm64.zip     libhidapi.dylib      hidapi, built from source
#
# Everything is built for Apple Silicon (arm64) and macOS 14 (the oldest version supported by .NET 10).
# Build requirements: Xcode command line tools, cmake, pkg-config and the GStreamer headers (brew install cmake pkg-config gstreamer)
set -e

FFMPEG_VERSION=8.1.3
PORTAUDIO_VERSION=19.7.0
HIDAPI_VERSION=0.15.0
GSTREAMER_VERSION=1.28.8
export MACOSX_DEPLOYMENT_TARGET=14.0

# GStreamer plugins needed for the playback of the songs (playbin, karaoke effect, mp3/aac/ogg/opus/flac/wav)
GST_PLUGINS="coreelements playback typefindfunctions audioconvert audioresample volume autodetect osxaudio audiofx
    audioparsers id3demux apetag isomp4 ogg vorbis opus flac wavparse mpg123 applemedia"

ROOT=$(cd "$(dirname "$0")/.." && pwd)
WORK="$ROOT/MacOS/Dependencies/work"
OUT="$ROOT/MacOS/Dependencies/out"
ZIPS="$ROOT/MacOS/Dependencies"
JOBS=$(sysctl -n hw.ncpu)

rm -rf "$OUT"
rm -f "$ZIPS"/ffmpeg_macos-arm64.zip "$ZIPS"/gstreamer_macos-arm64.zip "$ZIPS"/portaudio_macos-arm64.zip "$ZIPS"/hidapi_macos-arm64.zip
mkdir -p "$WORK" "$OUT/libs/unmanaged/ffmpeg64" "$OUT/libs/unmanaged/gstreamer"
UNMANAGED="$OUT/libs/unmanaged"

download() {
    [ -f "$WORK/$2" ] || curl -fsSL -o "$WORK/$2" "$1"
}

# --- FFmpeg ---
download "https://ffmpeg.org/releases/ffmpeg-$FFMPEG_VERSION.tar.xz" "ffmpeg-$FFMPEG_VERSION.tar.xz"
# reuse an earlier FFmpeg build of the same version (the build takes a few minutes)
if [ ! -f "$WORK/ffmpeg-prefix/lib/libavutil.60.dylib" ] || [ ! -f "$WORK/ffmpeg-prefix/.version-$FFMPEG_VERSION" ]; then
rm -rf "$WORK/ffmpeg-$FFMPEG_VERSION" "$WORK/ffmpeg-prefix"
tar -xf "$WORK/ffmpeg-$FFMPEG_VERSION.tar.xz" -C "$WORK"
(
    cd "$WORK/ffmpeg-$FFMPEG_VERSION"
    # only system libraries (no Homebrew libraries): --disable-autodetect
    ./configure --prefix="$WORK/ffmpeg-prefix" --install-name-dir='@rpath' --arch=arm64 --cc=clang \
        --extra-cflags="-mmacosx-version-min=$MACOSX_DEPLOYMENT_TARGET" \
        --extra-ldflags="-mmacosx-version-min=$MACOSX_DEPLOYMENT_TARGET -Wl,-rpath,@loader_path" \
        --enable-shared --disable-static --disable-programs --disable-doc --disable-debug --disable-autodetect \
        --enable-zlib --enable-bzlib --enable-videotoolbox --enable-audiotoolbox --enable-avfoundation \
        --disable-encoders --disable-muxers
    make -j"$JOBS"
    make install
)
touch "$WORK/ffmpeg-prefix/.version-$FFMPEG_VERSION"
fi
for lib in avcodec avdevice avfilter avformat avutil swresample swscale; do
    # only lib<name>.<major>.dylib, the name the libraries are loaded with
    find "$WORK/ffmpeg-prefix/lib" -name "lib$lib.*.dylib" | grep -E "/lib$lib\.[0-9]+\.dylib\$" | xargs -I{} cp -L {} "$UNMANAGED/ffmpeg64/"
done
cp "$WORK/ffmpeg-$FFMPEG_VERSION/COPYING.LGPLv2.1" "$UNMANAGED/ffmpeg64/LICENSE.txt"

# --- PortAudio ---
download "https://github.com/PortAudio/portaudio/archive/refs/tags/v$PORTAUDIO_VERSION.tar.gz" "portaudio-$PORTAUDIO_VERSION.tar.gz"
rm -rf "$WORK/portaudio-$PORTAUDIO_VERSION"
tar -xzf "$WORK/portaudio-$PORTAUDIO_VERSION.tar.gz" -C "$WORK"
cmake -S "$WORK/portaudio-$PORTAUDIO_VERSION" -B "$WORK/portaudio-build" -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_POLICY_VERSION_MINIMUM=3.5 \
    -DCMAKE_OSX_ARCHITECTURES=arm64 -DCMAKE_OSX_DEPLOYMENT_TARGET=$MACOSX_DEPLOYMENT_TARGET \
    -DBUILD_SHARED_LIBS=ON -DPA_BUILD_SHARED=ON -DPA_BUILD_STATIC=OFF -DCMAKE_INSTALL_NAME_DIR='@rpath'
cmake --build "$WORK/portaudio-build" -j"$JOBS"
cp "$(find "$WORK/portaudio-build" -name 'libportaudio*.dylib' -type f | head -1)" "$UNMANAGED/libportaudio.dylib"
install_name_tool -id @rpath/libportaudio.dylib "$UNMANAGED/libportaudio.dylib"

# --- hidapi ---
download "https://github.com/libusb/hidapi/archive/refs/tags/hidapi-$HIDAPI_VERSION.tar.gz" "hidapi-$HIDAPI_VERSION.tar.gz"
rm -rf "$WORK/hidapi-hidapi-$HIDAPI_VERSION"
tar -xzf "$WORK/hidapi-$HIDAPI_VERSION.tar.gz" -C "$WORK"
cmake -S "$WORK/hidapi-hidapi-$HIDAPI_VERSION" -B "$WORK/hidapi-build" -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_POLICY_VERSION_MINIMUM=3.5 \
    -DCMAKE_OSX_ARCHITECTURES=arm64 -DCMAKE_OSX_DEPLOYMENT_TARGET=$MACOSX_DEPLOYMENT_TARGET \
    -DBUILD_SHARED_LIBS=ON -DHIDAPI_WITH_TESTS=OFF -DCMAKE_INSTALL_NAME_DIR='@rpath'
cmake --build "$WORK/hidapi-build" -j"$JOBS"
cp "$(find "$WORK/hidapi-build" -name 'libhidapi*.dylib' -type f | head -1)" "$UNMANAGED/libhidapi.dylib"
install_name_tool -id @rpath/libhidapi.dylib "$UNMANAGED/libhidapi.dylib"

# --- GStreamer (official framework) ---
download "https://gstreamer.freedesktop.org/data/pkg/osx/$GSTREAMER_VERSION/gstreamer-1.0-$GSTREAMER_VERSION-universal.pkg" "gstreamer-$GSTREAMER_VERSION.pkg"
rm -rf "$WORK/gst-pkg" "$WORK/gst-fw"
pkgutil --expand-full "$WORK/gstreamer-$GSTREAMER_VERSION.pkg" "$WORK/gst-pkg"
mkdir -p "$WORK/gst-fw"
for payload in "$WORK"/gst-pkg/*.pkg/Payload; do
    rsync -a "$payload/" "$WORK/gst-fw/"
done
python3 "$ROOT/MacOS/copy-gstreamer.py" "$WORK/gst-fw" "$UNMANAGED/gstreamer" $GST_PLUGINS

# --- gstreamer-sharp glue (only struct sizes/offsets, the symbols are resolved at runtime) ---
clang -shared -fPIC -O2 -arch arm64 -mmacosx-version-min=$MACOSX_DEPLOYMENT_TARGET -undefined dynamic_lookup \
    -install_name @rpath/libgstreamersharpglue-1.0.0.dylib \
    -o "$UNMANAGED/libgstreamersharpglue-1.0.0.dylib" "$ROOT/GstreamerSharpGlue/glue.c" \
    $(pkg-config --cflags gstreamer-1.0 gstreamer-base-1.0 gstreamer-controller-1.0)

# --- check, sign and pack ---
find "$OUT" -type f | while read -r f; do
    if file "$f" | grep -q 'Mach-O'; then
        minos=$(otool -l "$f" | awk '/minos/{print $2; exit}')
        if otool -L "$f" | tail -n +2 | grep -q -E '/opt/homebrew|/usr/local'; then
            echo "ERROR: $f links to a Homebrew library"; exit 1
        fi
        echo "$(basename "$f"): macOS $minos"
        codesign --force --sign - "$f" 2> /dev/null
    fi
done
(
    cd "$UNMANAGED"
    zip -qry "$ZIPS/ffmpeg_macos-arm64.zip" ffmpeg64
    zip -qry "$ZIPS/gstreamer_macos-arm64.zip" gstreamer libgstreamersharpglue-1.0.0.dylib
    zip -qy "$ZIPS/portaudio_macos-arm64.zip" libportaudio.dylib
    zip -qy "$ZIPS/hidapi_macos-arm64.zip" libhidapi.dylib
)
echo
for zip in ffmpeg gstreamer portaudio hidapi; do
    echo "Created $ZIPS/${zip}_macos-arm64.zip ($(du -h "$ZIPS/${zip}_macos-arm64.zip" | cut -f1))"
done
