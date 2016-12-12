#!/bin/sh
PROJECT=$1
cd $PROJECT
sed -i -r -e 's/AssemblyInformationalVersion\(".*?"\)/AssemblyInformationalVersion("GITVERSION")/' $PROJECT/Properties/AssemblyInfo.cs
sed -i -r -e 's/AssemblyVersion\(".*?"\)/AssemblyVersion("SHORTVERSION")/' $PROJECT/Properties/AssemblyInfo.cs
sed -i -r -e 's/AssemblyFileVersion\(".*?"\)/AssemblyFileVersion("SHORTVERSION")/' $PROJECT/Properties/AssemblyInfo.cs
sed -i -e 's/var matchingServerVersion = "";/var matchingServerVersion = "'$(git describe --long)'";/' $PROJECT/../Output/Website/js/main.js
