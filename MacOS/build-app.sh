#!/bin/sh
# Builds Vocaluxe for macOS (Apple Silicon) and creates the self-contained app MacOS/Build/Vocaluxe.app
# (.NET runtime, FFmpeg, GStreamer, PortAudio and hidapi are included, users don't have to install anything).
#
# Build requirements: Xcode command line tools, .NET 10 SDK and Node.js.
# The native libraries come from the ZIPs in VocaluxeDependencies (zips/dependencies/*_macos-arm64.zip, like FFmpeg and
# GStreamer of the Windows build); they are downloaded to MacOS/Dependencies if they aren't there yet (ZIPs created by
# MacOS/build-dependencies.sh land there too). VOCALUXE_MAC_DEPENDENCIES can point to another folder or URL with the ZIPs.
#
# Usage: MacOS/build-app.sh [--debug] [--no-videos] [--dmg]
#   --dmg  also creates the disk image MacOS/Build/Vocaluxe.dmg (app + link to the Applications folder)
set -e

ROOT=$(cd "$(dirname "$0")/.." && pwd)
CONFIGURATION=ReleaseMac
VIDEOS=1
DMG=0
for arg in "$@"; do
    case $arg in
        --debug) CONFIGURATION=DebugMac ;;
        --no-videos) VIDEOS=0 ;;
        --dmg) DMG=1 ;;
        *) echo "Unknown option: $arg"; exit 1 ;;
    esac
done

if [ "$(uname -m)" != "arm64" ]; then
    echo "Only Apple Silicon (arm64) is supported."
    exit 1
fi

# oldest macOS version supported by .NET 10
export MACOSX_DEPLOYMENT_TARGET=14.0

command -v dotnet > /dev/null || { echo "The .NET 10 SDK is missing"; exit 1; }
command -v node > /dev/null || { echo "Node.js is missing"; exit 1; }

# native libraries, one ZIP per library (see MacOS/build-dependencies.sh)
DEPENDENCIES_URL='https://github.com/Vocaluxe/VocaluxeDependencies/raw/master/zips/dependencies'
DEPENDENCY_ZIPS="ffmpeg_macos-arm64.zip gstreamer_macos-arm64.zip portaudio_macos-arm64.zip hidapi_macos-arm64.zip"
DEPENDENCIES=${VOCALUXE_MAC_DEPENDENCIES:-$DEPENDENCIES_URL}
mkdir -p "$ROOT/MacOS/Dependencies"
for zip in $DEPENDENCY_ZIPS; do
    target="$ROOT/MacOS/Dependencies/$zip"
    case $DEPENDENCIES in
        http://*|https://*)
            if [ -n "$VOCALUXE_MAC_DEPENDENCIES" ] || [ ! -f "$target" ]; then
                echo "Downloading $zip..."
                curl -fsSL -o "$target" "$DEPENDENCIES/$zip"
            fi
            ;;
        *)
            cp "$DEPENDENCIES/$zip" "$target"
            ;;
    esac
done

# .NET runtime that is copied into the app: the official runtime of Microsoft in the version of the installed SDK
# (the .NET of Homebrew depends on Homebrew libraries and is built only for the installed macOS version)
DOTNET_HOME=${DOTNET_ROOT:-$(dirname "$(readlink -f "$(command -v dotnet)")")}
RUNTIME_VERSION=$(ls "$DOTNET_HOME/shared/Microsoft.NETCore.App" | grep '^10\.' | sort -t. -k3 -n | tail -1)
if [ -z "$RUNTIME_VERSION" ]; then
    echo "No .NET 10 runtime found in $DOTNET_HOME"
    exit 1
fi
RUNTIME_ARCHIVE="$ROOT/MacOS/Dependencies/aspnetcore-runtime-$RUNTIME_VERSION-osx-arm64.tar.gz"
if [ ! -f "$RUNTIME_ARCHIVE" ]; then
    mkdir -p "$ROOT/MacOS/Dependencies"
    echo "Downloading the .NET runtime $RUNTIME_VERSION..."
    curl -fsSL -o "$RUNTIME_ARCHIVE" \
        "https://builds.dotnet.microsoft.com/dotnet/aspnetcore/Runtime/$RUNTIME_VERSION/aspnetcore-runtime-$RUNTIME_VERSION-osx-arm64.tar.gz"
fi

cd "$ROOT"

# --- native helper library ---
make -C PitchTracker clean > /dev/null
make -C PitchTracker

# --- theme videos (not in the repository) ---
if [ $VIDEOS = 1 ] && [ ! -f "Output/Themes/Vocaluxe 2024/BG_Video.mp4" ]; then
    echo "Downloading theme videos..."
    tmp=$(mktemp -d)
    curl -fsSL -o "$tmp/videos.zip" 'https://github.com/Vocaluxe/VocaluxeDependencies/raw/master/zips/themes/vocaluxe.themes.vocaluxe-2024.video-part1.zip'
    unzip -o -q "$tmp/videos.zip" -d Output
    rm -rf "$tmp"
fi

# --- build ---
dotnet build Vocaluxe.sln -c $CONFIGURATION -p:Platform=arm64 -v:minimal -nologo

# --- app bundle ---
APP="$ROOT/MacOS/Build/Vocaluxe.app"
PROGRAM="$APP/Contents/Resources/Vocaluxe"
rm -rf "$APP"
mkdir -p "$APP/Contents/MacOS" "$PROGRAM"

# program files; user data (config, profiles, songs, ...) is stored in ~/Library/Application Support/Vocaluxe
rsync -a \
    --exclude 'Config.xml' --exclude '*.sqlite' --exclude 'Logs/' --exclude 'Screenshots/' --exclude 'Photos/' \
    --exclude 'Playlists/' --exclude 'WebCache/' --exclude 'WebServer.*' --exclude 'Linux/' --exclude '.DS_Store' \
    --exclude '/libs/unmanaged/' --exclude 'libgstreamersharpglue*' \
    Output/ "$PROGRAM/"

# native libraries (FFmpeg, GStreamer, PortAudio, hidapi)
mkdir -p "$PROGRAM/libs/unmanaged"
for zip in $DEPENDENCY_ZIPS; do
    unzip -q "$ROOT/MacOS/Dependencies/$zip" -d "$PROGRAM/libs/unmanaged"
done

# .NET runtime (the ASP.NET Core runtime archive contains the .NET runtime too)
mkdir -p "$APP/Contents/Resources/dotnet"
tar -xzf "$RUNTIME_ARCHIVE" -C "$APP/Contents/Resources/dotnet" host shared LICENSE.txt ThirdPartyNotices.txt

cat > "$APP/Contents/MacOS/Vocaluxe" <<'EOF'
#!/bin/sh
RESOURCES=$(cd "$(dirname "$0")/../Resources" && pwd)
export DOTNET_ROOT="$RESOURCES/dotnet"
exec "$RESOURCES/Vocaluxe/Vocaluxe" "$@"
EOF
chmod +x "$APP/Contents/MacOS/Vocaluxe"

# same version as in the program (macPreBuildEvent.sh): GIT_VERSION_TAG of the CI or git describe
version=${GIT_VERSION_TAG:-$(git describe --long --tags --exclude Nightly 2>/dev/null || echo "0.0.0-na-notversioned")}
shortVersion=$(echo "${version%%-*}" | sed -E 's/^([0-9]+(\.[0-9]+){0,2}).*/\1/')
sed -e "s/@VERSION@/$version/" -e "s/@SHORT_VERSION@/$shortVersion/" -e "s/@MIN_MACOS@/$MACOSX_DEPLOYMENT_TARGET/" \
    MacOS/Info.plist > "$APP/Contents/Info.plist"

# icon from Vocaluxe.ico
iconset=$(mktemp -d)/Vocaluxe.iconset
mkdir -p "$iconset"
sips -s format png Vocaluxe/Vocaluxe.ico --out "$iconset/base.png" > /dev/null
for size in 16 32 128 256; do
    sips -z $size $size "$iconset/base.png" --out "$iconset/icon_${size}x${size}.png" > /dev/null
    double=$((size * 2))
    sips -z $double $double "$iconset/base.png" --out "$iconset/icon_${size}x${size}@2x.png" > /dev/null
done
rm "$iconset/base.png"
iconutil -c icns "$iconset" -o "$APP/Contents/Resources/Vocaluxe.icns"
rm -rf "$(dirname "$iconset")"

# --- check and sign ---
# all Mach-O files: arm64 code must be signed (ad-hoc), and nothing may depend on Homebrew
find "$APP/Contents/Resources" -type f | while read -r f; do
    if file "$f" | grep -q 'Mach-O'; then
        if otool -L "$f" | tail -n +2 | grep -q -E '/opt/homebrew|/usr/local'; then
            echo "ERROR: $f depends on a Homebrew library"
            exit 1
        fi
        codesign --force --sign - "$f" 2> /dev/null
    fi
done
codesign --force --sign - "$APP"

# the oldest macOS version the app runs on is given by the newest minimum of its libraries
MIN=$(find "$APP/Contents/Resources" -type f \( -name '*.dylib' -o -perm +111 \) -exec sh -c \
    'otool -l "$1" 2>/dev/null | awk "/minos/{print \$2; exit}"' _ {} \; | sort -t. -k1,1n -k2,2n | tail -1)
echo
echo "Created $APP"
echo "Runs on macOS $MIN or later (target: $MACOSX_DEPLOYMENT_TARGET)"

# --- disk image: drag Vocaluxe into the Applications folder ---
if [ $DMG = 1 ]; then
    IMAGE="$ROOT/MacOS/Build/Vocaluxe.dmg"
    staging=$(mktemp -d)
    ditto "$APP" "$staging/Vocaluxe.app"
    ln -s /Applications "$staging/Applications"
    rm -f "$IMAGE"
    # ULMO (LZMA): smallest image, supported since macOS 10.15
    hdiutil create -quiet -volname Vocaluxe -srcfolder "$staging" -fs HFS+ -format ULMO "$IMAGE"
    rm -rf "$staging"
    echo "Created $IMAGE ($(du -h "$IMAGE" | cut -f1))"
fi

echo "Start it with: open \"$APP\""
