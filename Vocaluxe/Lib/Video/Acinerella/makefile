acinerella: acinerella.c acinerella.h
	gcc -c acinerella.c -I /usr/local/include

ifeq ($(shell uname),Linux)
	gcc -shared -o libacinerella.so acinerella.o -lavformat -lavcodec -lavutil -lm -lswscale 
	strip libacinerella.so
else
	gcc -shared -o acinerella.dll -fPIC acinerella.o -lavformat -lavcodec -lavutil -lm -lswscale -lws2_32
	strip acinerella.dll
endif
