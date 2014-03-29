#pragma once

// A pitch detection that is based on the AKF and AMDF (initially from original Vocaluxe/USDx)
namespace PitchTrackerAKF{
	void Init(double baseToneFrequency, int minHalfTone, int maxHalfTone);
	void DeInit();
	template<typename T> int GetTone(T *samples, int sampleCt, double* maxVolume, float *weights, bool scale = false);

	typedef void (__stdcall * LogCallback)(const char* text);
	static LogCallback Log;
	void SetLogCallback(LogCallback Callback);
}
