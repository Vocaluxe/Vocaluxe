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

#define USE_FFT

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

	int GetNote(float* restrict maxVolume, float* restrict weights);
	void SetVolumeThreshold(float threshold);
	float GetVolumeThreshold(){return _VolTreshold;}
	static int GetNumHalfTones(){ return _MaxHalfTone + 1;}

private:
	static int _InitCount;
	static float* restrict _SamplesPerPeriodPerTone;
	// Use a 3 times finer resolution for exact peak detection
	// So this array will store for each note the number of samples 1/3 below and up
	// (cannot use halves as it would be ambiguous)
	static float* restrict _SamplesPerPeriodPerToneFine;
	static float* restrict _Window;
	//static constexpr int _MinHalfTone = 0;  //C2
	static constexpr int _MaxHalfTone = 56;//47; //B5
	static constexpr int _HalfTonesAdd = 4; //Additonal half tones to analyze to remove the peak at lag 0
	static constexpr int _MaxPeaks = 10;
	static constexpr int _SmoothCt = 3; //Number of samples used for smoothing the result

	constexpr static size_t _SampleCt = 2048;
	RingBuffer<_SampleCt * 2> _AnalysisBuf;
	unsigned _Step;
	float _VolTreshold;
	float _LastMaxVol;
	int _LastTones[_SmoothCt];
	int _LastToneIndex;
#ifdef USE_FFT
	float _AKFValues[_SampleCt*2];
#endif

	int _GetNote(float samples[_SampleCt], float* restrict maxVolume, float weights[_MaxHalfTone+1]);
	int _GetSmoothTone();

	float _AnalyzeBySampleCt(float samples[_SampleCt], float samplesWindowed[_SampleCt], float samplesPerPeriodD);
	inline float _AnalyzeByTone(float samples[_SampleCt], float samplesWindowed[_SampleCt], int toneIndex);
	float _AKFBySampleCt(float samples[_SampleCt], float samplesPerPeriodD);
	inline float _AKFByTone(float samples[_SampleCt], int toneIndex);
	static float _AMDFBySampleCt(float samples[_SampleCt], float samplesPerPeriodD);
	static inline float _AMDFByTone(float samples[_SampleCt], int toneIndex);
	static inline void InitPeaks(SPeak peaks[_MaxPeaks]);
	static inline void AddPeak(SPeak peaks[_MaxPeaks], float curWeight, int toneIndex);
};