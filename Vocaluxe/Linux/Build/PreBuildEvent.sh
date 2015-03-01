#!/bin/sh
PROJECT=$1
cd $PROJECT
sed -i -e s/GITVERSION/$(git describe --long)/ $PROJECT/Properties/AssemblyInfo.cs
