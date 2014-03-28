#pragma once

namespace PitchTracker{
	void Init(double baseToneFrequency, int minHalfTone, int maxHalfTone);
	void DeInit();
	template<typename T> int GetTone(T *samples, int sampleCt, float *weights, bool scale = false);

	typedef void (__stdcall * LogCallback)(const char* text);
	static LogCallback Log;
	void SetLogCallback(LogCallback Callback);
}
