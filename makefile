ARCH?=$(shell uname -m | sed -e s/i.86/x86/ -e s/x86_64/x64/)
windowsLibs = Output/gstreamer-sharp.dll Output/glib-sharp.dll
all:
	$(MAKE) -C PitchTracker
	$(MAKE) -C Vocaluxe/Lib/Video/Acinerella
	rm -f $(windowsLibs)
	xbuild /property:Platform=$(ARCH) /property:Configuration=ReleaseLinux

clean:
	xbuild /target:Clean
	$(MAKE) -C PitchTracker clean
	$(MAKE) -C Vocaluxe/Lib/Video/Acinerella clean
	git checkout $(windowsLibs)
