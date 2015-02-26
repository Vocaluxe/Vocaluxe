#pragma once
#include <stddef.h>
float* short2FloatArray(short* in, size_t len);
double* short2DoubleArray(short* in, size_t len, bool scale = true);
void freeFloatArray(float* floats);
void freeDoubleArray(double* floats);
double FreqToNote(double freq);

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