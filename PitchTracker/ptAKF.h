#pragma once
#include "performous/pitch.hh"

#if __STDC__ != 1
#    define restrict __restrict
#else
#    ifndef __STDC_VERSION__
#        define restrict __restrict
#    else
#        if __STDC_VERSION__ < 199901L
#            define restrict __restrict
#        endif
#    endif
#endif

struct SPeak{
	int toneIndex;
	float weight;
};

// A pitch detection that is based on the AKF and AMDF (initially from original Vocaluxe/USDx)
class PtAKF{
public:
	PtAKF(unsigned step);
	~PtAKF();

	/** Add input data to buffer. This is thread-safe (against other functions). **/
	template <typename InIt> void input(InIt begin, InIt end) {
		_AnalysisBuf.insert(begin, end);
	}

	int GetNote(double* restrict maxVolume, float* restrict weights);
	static int GetNumHalfTones(){ return _MaxHalfTone - _MinHalfTone + 1;}

private:
	static int _InitCount;
	static float* restrict _SamplesPerPeriodPerTone;
	// Use a 3 times finer resolution for exact peak detection
	// So this array will store for each note the number of samples 1/3 below and up
	// (cannot use halves as it would be ambiguous)
	static float* restrict _SamplesPerPeriodPerToneFine;
	static float* restrict _Window;
	static constexpr int _MinHalfTone = 0;  //C2
	static constexpr int _MaxHalfTone = 55;//47; //B5
	static constexpr int _HalfTonesAdd = 4; //Additonal half tones to analyze to remove the peak at lag 0
	static constexpr int _MaxPeaks = 3;

	constexpr static size_t _SampleCt = 2048;
	RingBuffer<_SampleCt * 2> _AnalysisBuf;
	unsigned _Step;

	static int _GetNote(float* restrict samples, double* restrict maxVolume, float* restrict weights);
	static float _AnalyzeBySampleCt(float* restrict samples, float samplesPerPeriodD);
	static inline float _AnalyzeByTone(float* restrict samples, int toneIndex);
	static float _AKFBySampleCt(float* restrict samples, float samplesPerPeriodD);
	static inline float _AKFByTone(float* restrict samples, int toneIndex);
	static inline void InitPeaks(SPeak peaks[_MaxPeaks]);
	static inline void AddPeak(SPeak peaks[_MaxPeaks], float curWeight, int toneIndex);
};