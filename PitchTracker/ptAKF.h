#pragma once
#include "performous/pitch.hh"

// A pitch detection that is based on the AKF and AMDF (initially from original Vocaluxe/USDx)
class PtAKF{
public:
	PtAKF(unsigned step);
	~PtAKF();

	/** Add input data to buffer. This is thread-safe (against other functions). **/
	template <typename InIt> void input(InIt begin, InIt end) {
		_AnalysisBuf.insert(begin, end);
	}

	int GetNote(double* maxVolume, float* weights);
	static int GetNumHalfTones(){ return _MaxHalfTone - _MinHalfTone + 1;}
private:
	static int _InitCount;
	static float* _SamplesPerPeriodPerTone;
	static constexpr int _MinHalfTone = 0;  //C2
	static constexpr int _MaxHalfTone = 55;//47; //B5
	static constexpr int _HalfTonesAdd = 4; //Additonal half tones to analyze to remove the peak at lag 0

	constexpr static size_t _SampleCt = 2048;
	RingBuffer<_SampleCt * 2> _AnalysisBuf;
	unsigned _Step;

	static int _GetNote(float* samples, double* maxVolume, float* weights);
	static float _AnalyzeToneFunc(float* samples, int toneIndex);
	static float _FastAKF(float* samples, float samplesPerPeriodD);
};