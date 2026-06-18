acinerella: acinerella.c acinerella.h
	gcc -c -fPIC acinerella.c -I /usr/local/include

ifeq ($(shell uname),Linux)
	gcc -shared -o libacinerella.so acinerella.o -lavformat -lavcodec -lavutil -lm -lswscale -lswresample
	strip libacinerella.so
	cp libacinerella.so ../../../../Output/
else
	gcc -shared -o acinerella.dll -fPIC acinerella.o -lavformat -lavcodec -lavutil -lm -lswscale -lws2_32
	strip acinerella.dll
endif

clean:
	rm -f libacinerella.so ../../../../Output/libacinerella.so