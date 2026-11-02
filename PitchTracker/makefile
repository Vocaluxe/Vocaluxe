objects = \
	compatibility.o \
	dywapitchtrack/dywapitchtrack.o \
	dywapitchtrack/ptDyWa.o \
	FFT/FFT.o \
	FFT/RealFFTf.o \
	Helper.o \
	performous/pitch.o \
	ptAKF.o \
	PitchWrapper.o

CPPFLAGS = -std=gnu++11 -fPIC

PitchTracker: $(objects)
ifeq ($(shell uname),Darwin)
	$(CXX) -shared -o libPitchTracker.dylib -fPIC $(objects)
	cp libPitchTracker.dylib ../Output/
else
	gcc -shared -o libPitchTracker.dll.so -fPIC $(objects)
	strip libPitchTracker.dll.so
	cp libPitchTracker.dll.so ../Output/
endif

clean:
	rm -f ../Output/libPitchTracker.dll.so libPitchTracker.dll.so ../Output/libPitchTracker.dylib libPitchTracker.dylib $(objects)