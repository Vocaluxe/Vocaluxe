#!/bin/sh
# Resets the version in Properties/AssemblyInfo.cs after the build (macOS, see winPostBuild.ps1).
PROJECT=$1
cd "$PROJECT" || exit 1

if [ -n "$VOCALUXE_SKIP_POSTBUILD" ]; then
    echo "Skipping post build script (as requested)"
    exit 0
fi

perl -CSD -pi -e '
    s/(AssemblyInformationalVersion\(")[^"]*(")/${1}0.0.0-na-notversioned$2/;
    s/((?:AssemblyVersion|AssemblyFileVersion)\(")[^"]*(")/${1}0.0.0$2/;
    s/(AssemblyTitle\(")[^"]*(")/${1}Vocaluxe '"'"'Not Versioned'"'"' 0.0.0 (NA) (0.0.0-na-notversioned)$2/;
' Properties/AssemblyInfo.cs
