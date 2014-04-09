#pragma once
#include "performous/pitch.hh"

// A pitch detection that is based on the AKF and AMDF (initially from original Vocaluxe/USDx)
class PtAKF{
public:
	PtAKF();
	~PtAKF();

	void input(short* begin, short* end){
		_AnalysisBuf.insert(begin, end);
	}

	int GetNote(double* maxVolume, float* weights);
	static int GetNumHalfTones(){ return _MaxHalfTone - _MinHalfTone + 1;}
private:
	static int _InitCount;
	static float* _SamplesPerPeriodPerTone;
	static constexpr int _MinHalfTone = 0;  //C2
	static constexpr int _MaxHalfTone = 47; //B5

	constexpr static size_t _SampleCt = 4096;
	RingBuffer<_SampleCt * 2> _AnalysisBuf;

	static int _GetNote(float* samples, double* maxVolume, float* weights);
	static float _AnalyzeToneFunc(float* samples, int toneIndex);
};