#pragma once
#include "PitchWrapper.h"
#include <stdlib.h>
#include <cmath>

float* short2FloatArray(short* in, size_t len){
	const float maxShort = 32768.0; //maximum abs value of a short

	float* result = static_cast<float*>(malloc(len * sizeof(float)));
	float* curOut = result;
	for(size_t i=0; i<len; i++){
		*curOut = *in / maxShort;
		curOut++;
		in++;
	}
	return result;
}

double* short2DoubleArray(short* in, size_t len, bool scale = true){
	const double maxShort = 32768.0; //maximum abs value of a short

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

/*
namespace Native{
	namespace PitchTracking{
		CTone^ ConvertToneToManaged(Tone* tone){
			CTone^ result = gcnew CTone();
			result->Freq = tone->freq;
			result->DB = tone->db;
			result->Stabledb = tone->stabledb;
			result->Age = static_cast<int>(tone->age);
			result->NoteExact = 12.0 * std::log(tone->freq / 127.09) / std::log(2.0) + 27.5 - 16; // 16 = C2
			result->Note = static_cast<int>(std::round(result->NoteExact));
			return result;
		}
	}
}
*/