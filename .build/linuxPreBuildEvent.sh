#!/bin/sh
PROJECT=$1
cd $PROJECT
arch=$2
version=$(git describe --long)
shortVersion=${version%%-*}
shortVersionClean=$(echo $shortVersion | sed -e 's/alpha/0/' -e 's/beta/1/' -e 's/rc/2/' -e 's/[[:alpha:]]//g')

case ${shortVersion##*.} in
alpha*)
    releaseType='Alpha'
    ;;
beta*)
    releaseType='Beta'
    ;;
rc*)
    releaseType='RC'
    ;;
*)
    releaseType='Release'
    ;;
esac

versionName=$(grep -oP "\"${shortVersionClean%.*}.\" *: *\"\K([^\"]+)(?=\")" ../.build/versionNames.json)
fullVersionName="Vocaluxe $([ "$versionName" == "" ] && echo "" || echo "'$versionName' ")$shortVersion ($arch) $([ "$releaseType" == "Release" ] && echo "" || echo "$releaseType ")($version)"

sed -i -r -e "s/AssemblyInformationalVersion\(\".*?\"\)/AssemblyInformationalVersion(\"$(git describe --long)\")/" $PROJECT/Properties/AssemblyInfo.cs
sed -i -r -e "s/AssemblyVersion\(\".*?\"\)/AssemblyVersion(\"$shortVersionClean\")/" $PROJECT/Properties/AssemblyInfo.cs
sed -i -r -e "s/AssemblyFileVersion\(\".*?\"\)/AssemblyFileVersion(\"$shortVersionClean\")/" $PROJECT/Properties/AssemblyInfo.cs
sed -i -r -e "s/AssemblyTitle\(\".*?\"\)/AssemblyTitle(\"$fullVersionName\")/" $PROJECT/Properties/AssemblyInfo.cs