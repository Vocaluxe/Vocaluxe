#include "Helper.h"
#include <stdlib.h>
#include <cmath>

float* short2FloatArray(short* in, size_t len){
	const float maxShort = 32767.0f;

	float* result = static_cast<float*>(malloc(len * sizeof(float)));
	float* curOut = result;
	for(size_t i=0; i<len; i++){
		*curOut = *in / maxShort;
		curOut++;
		in++;
	}
	return result;
}

double* short2DoubleArray(short* in, size_t len, bool scale){
	const double maxShort = 32767.0;

	double* result = static_cast<double*>(malloc(len * sizeof(double)));
	double* curOut = result;
	for(size_t i=0; i<len; i++){
		*curOut = *in;
		if(scale)
			*curOut /= maxShort;
		curOut++;
		in++;
	}
	return result;
}

void freeFloatArray(float* floats){
	free(floats);
}

void freeDoubleArray(double* floats){
	free(floats);
}

double FreqToNote(double freq){
	return 12.0 * std::log(freq / 127.09) / std::log(2.0) + 27.5 - 16; // 16 = C2
}