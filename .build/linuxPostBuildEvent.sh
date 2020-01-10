#!/bin/sh
PROJECT=$1
cd $PROJECT
sed -i -r -e 's/AssemblyInformationalVersion\(".*?"\)/AssemblyInformationalVersion("0.0.0-na-notversioned")/' $PROJECT/Properties/AssemblyInfo.cs
sed -i -r -e 's/AssemblyVersion\(".*?"\)/AssemblyVersion("0.0.0")/' $PROJECT/Properties/AssemblyInfo.cs
sed -i -r -e 's/AssemblyFileVersion\(".*?"\)/AssemblyFileVersion("0.0.0")/' $PROJECT/Properties/AssemblyInfo.cs
sed -i -r -e 's/AssemblyTitle\(".*?"\)/AssemblyTitle("Vocaluxe '"'"'Not Versioned'"'"' 0.0.0 (NA) (0.0.0-na-notversioned)")/' $PROJECT/Properties/AssemblyInfo.cs
