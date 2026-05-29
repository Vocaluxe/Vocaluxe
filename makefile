ARCH?=$(shell uname -m | sed -e s/i.86/x86/ -e s/x86_64/x64/)
windowsLibs = Output/gstreamer-sharp.dll Output/glib-sharp.dll
all:
	$(MAKE) -C PitchTracker
	$(MAKE) -C Vocaluxe/Lib/Video/Acinerella
	rm -f $(windowsLibs)
	msbuild /t:Restore /property:Platform=$(ARCH) /property:Configuration=ReleaseLinux /property:TargetFrameworkVersion=v4.8
	msbuild /property:Platform=$(ARCH) /property:Configuration=ReleaseLinux /property:TargetFrameworkVersion=v4.8
	cp Vocaluxe/Linux/SQLitePCLRaw.provider.e_sqlite3.dll.config Output/libs/managed/

clean:
	xbuild /target:Clean
	$(MAKE) -C PitchTracker clean
	$(MAKE) -C Vocaluxe/Lib/Video/Acinerella clean
	git checkout $(windowsLibs)

INSTALLPATH?=/usr/share/vocaluxe
WRONG_ARCH=$(shell echo $(ARCH) | sed -e s/x86/x64/ -e s/x64/x86/)
install:
	mkdir -p $(DESTDIR)$(INSTALLPATH)
	cp -ru Output/. $(DESTDIR)$(INSTALLPATH)
	rm -rf $(DESTDIR)$(INSTALLPATH)/$(WRONG_ARCH)

ICONPATH?=/usr/share/applications
install_icon:
	mkdir -p $(DESTDIR)$(ICONPATH)
	cp Vocaluxe/Linux/Package/Vocaluxe.desktop $(DESTDIR)$(ICONPATH)/
	sed -i -e 's=INSTALLPATH=$(INSTALLPATH)=' $(DESTDIR)$(ICONPATH)/Vocaluxe.desktop

