#!/bin/sh
# Writes the version into Properties/AssemblyInfo.cs before the build (macOS, see winPreBuild.ps1).
# Uses only tools that come with macOS (BSD sed/grep are not compatible with the GNU options of the Linux script).
PROJECT=$1
ARCH=$2
cd "$PROJECT" || exit 1

if [ -n "$GIT_VERSION_TAG" ]; then
    version=$GIT_VERSION_TAG
else
    version=$(git describe --long --tags --exclude Nightly 2>/dev/null)
fi
[ -z "$version" ] && version="0.0.0-na-notversioned"

shortVersion=${version%%-*}
shortVersionClean=$(echo "$shortVersion" | sed -e 's/alpha/0/' -e 's/beta/1/' -e 's/rc/2/' -e 's/[[:alpha:]]//g' -e 's/\.\.*/./g' -e 's/\.$//')

case $(echo "$shortVersion" | cut -d. -f3-) in
    *alpha*) releaseType='Alpha' ;;
    *beta*) releaseType='Beta' ;;
    *rc*) releaseType='RC' ;;
    *) releaseType='Release' ;;
esac

# e.g. "0.4." -> "Blue Sunrise"
versionKey=$(echo "$shortVersion" | sed -E 's/^([0-9]+\.[0-9]+\.).*/\1/')
versionName=$(sed -n -E "s/^[[:space:]]*\"$(echo "$versionKey" | sed 's/\./\\./g')\"[[:space:]]*:[[:space:]]*\"([^\"]+)\".*/\1/p" ../.build/versionNames.json)

fullVersionName="Vocaluxe"
[ -n "$versionName" ] && fullVersionName="$fullVersionName '$versionName'"
fullVersionName="$fullVersionName $shortVersion ($ARCH)"
[ "$releaseType" != "Release" ] && fullVersionName="$fullVersionName $releaseType"
fullVersionName="$fullVersionName ($version)"

export VX_VERSION="$version" VX_SHORT="$shortVersionClean" VX_TITLE="$fullVersionName" VX_YEAR="$(date +%Y)"
perl -CSD -pi -e '
    s/(AssemblyInformationalVersion\(")[^"]*(")/$1$ENV{VX_VERSION}$2/;
    s/((?:AssemblyVersion|AssemblyFileVersion)\(")[^"]*(")/$1$ENV{VX_SHORT}$2/;
    s/(AssemblyTitle\(")[^"]*(")/$1$ENV{VX_TITLE}$2/;
    s/(AssemblyCopyright\(".*?)[0-9]+(")/$1$ENV{VX_YEAR}$2/;
' Properties/AssemblyInfo.cs
