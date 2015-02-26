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
	gcc -shared -o libPitchTracker.dll.so -fPIC $(objects)
	strip libPitchTracker.dll.so
	cp libPitchTracker.dll.so ../Output/

clean:
	rm -f ../Output/libPitchTracker.dll.so libPitchTracker.dll.so $(objects)