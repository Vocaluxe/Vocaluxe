# Builds the gstreamer-sharp glue library (macOS: libgstreamersharpglue-1.0.0.dylib)
PKGS = gstreamer-1.0 gstreamer-base-1.0 gstreamer-controller-1.0

ifeq ($(shell uname),Darwin)
LIB = libgstreamersharpglue-1.0.0.dylib
else
LIB = libgstreamersharpglue-1.0.0.so
endif

$(LIB): glue.c
	$(CC) -shared -fPIC -O2 -o $(LIB) glue.c $(shell pkg-config --cflags --libs $(PKGS))
	cp $(LIB) ../Output/

clean:
	rm -f $(LIB) ../Output/$(LIB)
