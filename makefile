windowsLibs = Output/gstreamer-sharp.dll Output/glib-sharp.dll
all:
	$(MAKE) -C PitchTracker
	$(MAKE) -C Vocaluxe/Lib/Video/Acinerella
	rm -f $(windowsLibs)
	xbuild /property:Platform=x64 /property:Configuration=ReleaseLinux

clean:
	xbuild /target:Clean
	$(MAKE) -C PitchTracker clean
	$(MAKE) -C Vocaluxe/Lib/Video/Acinerella clean
	git checkout $(windowsLibs)
