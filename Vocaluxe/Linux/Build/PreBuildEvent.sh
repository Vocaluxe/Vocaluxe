#!/bin/sh
PROJECT=$1
cd $PROJECT
sed -i -e s/GITVERSION/$(git describe --long)/ $PROJECT/Properties/AssemblyInfo.cs
shortv=$(git describe --abbrev=0 | sed -e 's/alpha/0/' -e 's/beta/1/' -e 's/rc/2/')
sed -i -e s/SHORTVERSION/${shortv}/ $PROJECT/Properties/AssemblyInfo.cs
