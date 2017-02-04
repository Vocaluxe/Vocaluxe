// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.

#define _USE_MATH_DEFINES
#include "ptAKF.h"
#include <cmath>

#ifdef USE_FFT
#include "FFT/FFT.h"
#endif

static constexpr double BaseToneFrequency = 65.4064; // lowest (half-)tone to analyze (C2 = 65.4064 Hz)
static constexpr double HalftoneBase = 1.05946309436; // 2^(1/12) -> HalftoneBase^12 = 2 (one octave)

template<typename T>
T abs_template(T t)
{
	return t>0 ? t : -t;
}

int PtAKF::_InitCount = 0;
float* restrict PtAKF::_SamplesPerPeriodPerTone = NULL;
float* restrict PtAKF::_SamplesPerPeriodPerToneFine = NULL;
float* restrict PtAKF::_Window = NULL;

double GetFrequencyFromTone(double toneIndex){
	return BaseToneFrequency * pow(HalftoneBase, toneIndex);
}

PtAKF::PtAKF(unsigned step){
	_InitCount++;
	if(_InitCount == 1){
		_SamplesPerPeriodPerTone = new float[_MaxHalfTone + 1 + _HalfTonesAdd];
		_SamplesPerPeriodPerToneFine = new float[(_MaxHalfTone + 1 + _HalfTonesAdd) * 2];
		//Init Array to avoid costly calculations
		for (int toneIndex = 0; toneIndex <= _MaxHalfTone + _HalfTonesAdd; toneIndex++)
		{
			_SamplesPerPeriodPerTone[toneIndex] = static_cast<float>(44100.0 / GetFrequencyFromTone(toneIndex)); // samples in one period
			_SamplesPerPeriodPerToneFine[2 * toneIndex] = static_cast<float>(44100.0 / GetFrequencyFromTone(toneIndex - 1./3.)); // samples for a bit below exact frequency
			_SamplesPerPeriodPerToneFine[2 * toneIndex + 1] = static_cast<float>(44100.0 / GetFrequencyFromTone(toneIndex + 1./3.)); // samples for a bit above exact frequency
		}
		_Window = new float[_SampleCt];
		double cosMult = 2. * M_PI / (2 * _SampleCt - 1.); //To simplify and speed up; 2 * _SampleCt because we extend the data by a factor of 2
		for (int i = 0; i < _SampleCt; i++) {
			_Window[i] = static_cast<float>(0.54 - 0.46 * cos(i * cosMult));
		}
	}
	_Step = step;
	_VolTreshold = 0.01f;
	_LastMaxVol = 0.f;
	for(int i = 0; i < _SmoothCt; i++)
		_LastTones[i] = -1;
	_LastToneIndex = 0;
}

PtAKF::~PtAKF(){
	_InitCount--;
	if(!_InitCount){
		delete[] _SamplesPerPeriodPerTone;
		_SamplesPerPeriodPerTone = NULL;
		delete[] _SamplesPerPeriodPerToneFine;
		_SamplesPerPeriodPerToneFine = NULL;
	}
}

void PtAKF::SetVolumeThreshold(float threshold){
	_VolTreshold = threshold;
}

int PtAKF::GetNote(float* restrict maxVolume, float* restrict weights){
	float AnaylsisBuf[_SampleCt];
	int note = _LastTones[_LastToneIndex];
	if(_AnalysisBuf.read(AnaylsisBuf, AnaylsisBuf + _SampleCt)){
		do{
			_AnalysisBuf.pop(_Step);
			note = _GetNote(AnaylsisBuf, maxVolume, weights);
			if(++_LastToneIndex >= _SmoothCt)
				_LastToneIndex = 0;
			_LastTones[_LastToneIndex] = note;
		}while(_AnalysisBuf.read(AnaylsisBuf, AnaylsisBuf + _SampleCt));
		note = _GetSmoothTone();
		_LastMaxVol = *maxVolume;
	}else{
		*maxVolume = _LastMaxVol * 0.85f;
	}
	return note;
}

int PtAKF::_GetSmoothTone(){
	int tones[_SmoothCt];
	int ct = 0;
	for(int i=0; i < _SmoothCt; i++){
		int j;
		int tone = _LastTones[i];
		if(tone < 0)
			continue;
		for(j = ct; j > 0; j--){
			if(tones[j-1] > tone)
				tones[j] = tones[j-1];
			else break;
		}
		tones[j] = tone;
		ct++;
	}
	if(ct == 0)
		return -1; //Nothing detected
	if(ct % 2 == 0)
		return (tones[ct/2] + tones[ct/2-1]) / 2; //For even cts get the mean of the 2 middle tones
	return tones[ct/2]; //For odd cts get the median (middle tone)
}

void PtAKF::InitPeaks(SPeak peaks[_MaxPeaks]){
	for(int i = 0; i < _MaxPeaks; i++){
		peaks[i].toneIndex = -1;
		peaks[i].weight = -9.f;
	}
}

void PtAKF::AddPeak(SPeak peaks[], float curWeight, int toneIndex){
	if(curWeight > peaks[0].weight){
		int i;
		for(i = 1; i < _MaxPeaks; i++){
			if(curWeight < peaks[i].weight){
				peaks[i-1].toneIndex = toneIndex;
				peaks[i-1].weight = curWeight;
				break;
			}else{
				peaks[i-1] = peaks[i];
			}
		}
		if(i == _MaxPeaks){
			peaks[i-1].toneIndex = toneIndex;
			peaks[i-1].weight = curWeight;
		}
	}
}

int PtAKF::_GetNote(float samples[_SampleCt], float* restrict maxVolume, float weights[_MaxHalfTone+1]){
	// Calculate maximum volume

	float maxVolumeL = 0;
	for(int i = _SampleCt / 2; i < _SampleCt; i++){
		float vol = abs(samples[i]);
		if(vol > maxVolumeL)
			maxVolumeL = vol;
	}
	//maxVolumeL /= 32767.f;
	*maxVolume = maxVolumeL;

	if(maxVolumeL < _VolTreshold)
		return -1;

	float samplesWindowed[_SampleCt * 2];

	for(int i = 0; i < _SampleCt; i++){
		samplesWindowed[i] = samples[i] * _Window[i];
	}

#ifdef USE_FFT
	for(int i = _SampleCt; i < _SampleCt * 2; i++){
		samplesWindowed[i] = 0.f;
	}
	float samplesFFT[_SampleCt+1]; // +1 for middle value!
	PowerSpectrum(_SampleCt*2, samplesWindowed, samplesFFT);
	RealInverseRealFFT(_SampleCt*2, samplesFFT, _AKFValues);
#endif
	// Now analyze the samples and get peaks at the most appropriate tones

	//Attention: We have a peak at lag 0 that might stretch that far, that we detect a wrong "peak" at _MaxHalfTone
	//Because of that we filter out all tones that are past the last zero crossing from below but keep tones with decreasing weights (going towards zero crossing from above)
	int lastValidTone = 0;
	float lastWeight = 1.f;
	float maxWeight = 0.f;

	//TODO: When using FFT this can be speed up a lot by only checking around the AKF peaks
	for (int toneIndex = 0; toneIndex <= _MaxHalfTone; toneIndex++){
		float curWeight = _AnalyzeByTone(samples, samplesWindowed, toneIndex);

		weights[toneIndex] = curWeight;

		if(curWeight > maxWeight){
			maxWeight = curWeight;
		}
		if(lastWeight > curWeight || (lastWeight > 0.f && curWeight <= 0.f))
			lastValidTone = toneIndex - 1;
		lastWeight = curWeight;
	}

	// Now clear off the lag 0 peak if required

	if(maxWeight >= weights[_MaxHalfTone]){
		//We might have caught the lag 0 peak so go a bit further to check for other zero crossings (or we won't be able to detect _MaxHalfTone)
		float lastWeight =  _AKFByTone(samples, _MaxHalfTone);
		for(int toneIndex = _MaxHalfTone+1; toneIndex <= _MaxHalfTone + _HalfTonesAdd; toneIndex++){
			float curWeight = _AKFByTone(samples, toneIndex);
			if(lastWeight > curWeight || (lastWeight > 0.f && curWeight <= 0.f)){
				lastValidTone = toneIndex - 1;
				break;
			}
			lastWeight = curWeight;
		}
		if(lastValidTone < 0)
			return -1;
		//Set all invalid weights to 0
		for(int toneIndex = lastValidTone + 1;toneIndex <= _MaxHalfTone; toneIndex++){
			weights[toneIndex] = 0.f;
		}
	}

	SPeak peaks[_MaxPeaks];
	InitPeaks(peaks);
	int numPeaks = 0;
	bool up = true;
	for(int i = 1; i <= _MaxHalfTone; i++){
		if(weights[i - 1] > weights[i]){
			if(up){
				AddPeak(peaks, weights[i - 1], i - 1);
				numPeaks++;
				up = false;
			}
		}else if(!up){
			up = true;
		}
	}
	if(up && weights[_MaxHalfTone] > 0.001f){
		AddPeak(peaks, weights[_MaxHalfTone], _MaxHalfTone);
		numPeaks++;
	}
	if(numPeaks > _MaxPeaks)
		numPeaks = _MaxPeaks;

	// Further analyze the found peaks and find the real maximum
	// This is necessary because we may have our real peak a bit off the exact tone frequency
	// and a 'wrong' peak that is exactly at another tone which might become higher than the one at the 'right' tone

	maxWeight = peaks[_MaxPeaks-1].weight;
	if(maxWeight < 0.00001f) // Just a small number to check for zero values
		return -1;
	int maxTone = peaks[_MaxPeaks-1].toneIndex;
	int maxToneFine = -1;
	for(int i = _MaxPeaks - numPeaks; i < _MaxPeaks; i++){
		float curWeight = peaks[i].weight;
		if(curWeight * 3.f < maxWeight)
			continue;
		int toneIndex = peaks[i].toneIndex;
		float curWeightDown = _AnalyzeBySampleCt(samples, samplesWindowed, _SamplesPerPeriodPerToneFine[toneIndex * 2]);
		int otherToneIndex;
		int otherToneFineIndex;
		if(curWeightDown > curWeight){
			otherToneIndex = toneIndex - 1;
			otherToneFineIndex = 1;
			curWeight = curWeightDown;
		}else{
			float curWeightUp = _AnalyzeBySampleCt(samples,  samplesWindowed, _SamplesPerPeriodPerToneFine[toneIndex * 2 + 1]);
			if(curWeightUp > curWeight){
				otherToneIndex = (toneIndex < _MaxHalfTone) ? toneIndex + 1: -1; // If the other tone is invalid just set it to -1 which will be skipped below
				otherToneFineIndex = 0;
				curWeight = curWeightUp;
			}else
				continue;
		}
		weights[toneIndex] = curWeight;
		if(curWeight > maxWeight){
			maxTone = toneIndex;
			maxToneFine = 1 - otherToneFineIndex;
			maxWeight = curWeight;
		}

		// Now check also neighbouring tone
		if(otherToneIndex >= 0){
			float curWeightOther = _AnalyzeBySampleCt(samples,  samplesWindowed, _SamplesPerPeriodPerToneFine[otherToneIndex * 2 + otherToneFineIndex]);
			if(curWeightOther > weights[otherToneIndex]){
				weights[otherToneIndex] = curWeightOther;
				if(curWeightOther > maxWeight){
					maxTone = otherToneIndex;
					maxToneFine = otherToneFineIndex;
					maxWeight = curWeightOther;
				}
			}
		}
	}

	float energy = _AKFBySampleCt(samplesWindowed, 0);
	float maxAKF = (maxToneFine < 0) ? _AKFByTone(samplesWindowed, maxTone) : _AKFBySampleCt(samplesWindowed, _SamplesPerPeriodPerToneFine[maxTone * 2 + maxToneFine]);

	//if(maxWeight - minWeight > 0.025){
	if(maxAKF >= 0.33f * energy){
		return maxTone;
	}else return -1;
}

float PtAKF::_AnalyzeByTone(float samples[_SampleCt], float samplesWindowed[_SampleCt], int toneIndex){
	return _AnalyzeBySampleCt(samples, samplesWindowed, _SamplesPerPeriodPerTone[toneIndex]);
}

float PtAKF::_AnalyzeBySampleCt(float samples[_SampleCt], float samplesWindowed[_SampleCt], float samplesPerPeriodD){
	// Use method by Kobayashi and Shimamura (2001): Combine AKF and AMDF to a new f(z)=AKF(z)/(AMDF(z)+k) with k=1

	float akf = _AKFBySampleCt(samplesWindowed, samplesPerPeriodD);
	float amdf = _AMDFBySampleCt(samples, samplesPerPeriodD);

	//return accumDistAKF / (accumDistAMDF * 32767.f + 32767.f * 32767.f); // Need to scale AKF by MAX^2 and AMDF by MAX, so do some maths to divide only once
	float result = akf / (amdf + 1.f);
	return result; // Need to scale AKF by MAX^2 and AMDF by MAX, so do some maths to divide only once
	//{toneIndex}: {accumDistAKF} ; {accumDistAMDF}; {result}
}

float PtAKF::_AKFByTone(float samples[_SampleCt], int toneIndex){
	return _AKFBySampleCt(samples, _SamplesPerPeriodPerTone[toneIndex]);
}

float PtAKF::_AKFBySampleCt(float samples[_SampleCt], float samplesPerPeriodD){
	int samplesPerPeriod = static_cast<int>(samplesPerPeriodD);
	float fHigh = samplesPerPeriodD - samplesPerPeriod;
	float fLow = 1.0f - fHigh;
#ifdef USE_FFT
	float akf2 = _AKFValues[samplesPerPeriod] * fLow + _AKFValues[samplesPerPeriod+1] * fHigh;
	return akf2 / (_SampleCt * _SampleCt);
#else

	float accumDist = 0; // accumulated distances

	// compare correlating samples
	int sampleIndex = 0; // index of sample to analyze
	// Start value= index of sample one period ahead
	for (int correlatingSampleIndex = sampleIndex + samplesPerPeriod; correlatingSampleIndex + 1 < _SampleCt; correlatingSampleIndex++, sampleIndex++)
	{
		// calc distance to corresponding sample in next period
		float xn = samples[sampleIndex];
		float xnt = samples[correlatingSampleIndex] * fLow + samples[correlatingSampleIndex + 1] * fHigh;
		accumDist += xn * xnt;
	}

	return accumDist / _SampleCt;
#endif
}

float PtAKF::_AMDFByTone(float samples[_SampleCt], int toneIndex){
	return _AMDFBySampleCt(samples, _SamplesPerPeriodPerTone[toneIndex]);
}

float PtAKF::_AMDFBySampleCt(float samples[_SampleCt], float samplesPerPeriodD){
	int samplesPerPeriod = static_cast<int>(samplesPerPeriodD);
	float fHigh = samplesPerPeriodD - samplesPerPeriod;
	float fLow = 1.0f - fHigh;

	float accumDist = 0; // accumulated distances

	// compare correlating samples
	int sampleIndex = 0; // index of sample to analyze
	// Start value= index of sample one period ahead
	for (int correlatingSampleIndex = sampleIndex + samplesPerPeriod; correlatingSampleIndex + 1 < _SampleCt; correlatingSampleIndex++, sampleIndex++)
	{
		// calc distance to corresponding sample in next period
		float xn = samples[sampleIndex];
		float xnt = samples[correlatingSampleIndex] * fLow + samples[correlatingSampleIndex + 1] * fHigh;
		accumDist += abs_template(xn - xnt);
	}

	return accumDist / sampleIndex;
}
/*



namespace PitchTrackerAKF{
	static double *SamplesPerPeriodPerTone = NULL;
	static double BaseToneFrequency = -1;
	static int MinHalfTone = -1, MaxHalfTone = -1;
	static const double HalftoneBase = 1.05946309436; // 2^(1/12) -> HalftoneBase^12 = 2 (one octave)

	void Init(double baseToneFrequency, int minHalfTone, int maxHalfTone){
		if(SamplesPerPeriodPerTone && abs(baseToneFrequency - BaseToneFrequency) < 1. && minHalfTone == MinHalfTone && maxHalfTone == MaxHalfTone)
			return;
		BaseToneFrequency = baseToneFrequency;
		MinHalfTone = minHalfTone;
		MaxHalfTone = maxHalfTone;
		DeInit();
		if(minHalfTone < 0 || maxHalfTone < minHalfTone)
			return;
		SamplesPerPeriodPerTone = (double*) malloc((MaxHalfTone + 1) * sizeof(double));
		//Init Array to avoid costly calculations
		for (int toneIndex = 0; toneIndex <= MaxHalfTone; toneIndex++)
		{
			double freq = baseToneFrequency * pow(HalftoneBase, toneIndex);
			SamplesPerPeriodPerTone[toneIndex] = 44100.0 / freq; // samples in one period
		}
	}

	void DeInit(){
		if(SamplesPerPeriodPerTone){
			free(SamplesPerPeriodPerTone);
			SamplesPerPeriodPerTone=NULL;
		}
	}

	template<typename T>
	static T _AnalyzeToneFunc(T *samples, int sampleCt, int toneIndex, bool scale){
		// Use method by Kobayashi and Shimamura (1995): Combine AKF and AMDF to a new f(z)=AKF(z)/(AMDF(z)+k) with k=1
		double samplesPerPeriodD = SamplesPerPeriodPerTone[toneIndex]; // samples in one period
		int samplesPerPeriod = static_cast<int>(samplesPerPeriodD);
		T fHigh = static_cast<T>(samplesPerPeriodD - samplesPerPeriod);
		T fLow = static_cast<T>(1.0 - fHigh);

		T accumDistAKF = 0; // accumulated distances
		T accumDistAMDF = 0; // accumulated distances

		// compare correlating samples
		int sampleIndex = 0; // index of sample to analyze
		// Start value= index of sample one period ahead
		for (int correlatingSampleIndex = sampleIndex + samplesPerPeriod; correlatingSampleIndex + 1 < sampleCt; correlatingSampleIndex++, sampleIndex++)
		{
			// calc distance to corresponding sample in next period
			T xn = samples[sampleIndex];
			T xnt = samples[correlatingSampleIndex] * fLow + samples[correlatingSampleIndex + 1] * fHigh;
			accumDistAKF += xn * xnt;
			accumDistAMDF += abs(xn - xnt);
		}

		accumDistAKF /= sampleCt;
		accumDistAMDF /= sampleIndex;

		if(scale)
			return accumDistAKF / (accumDistAMDF * 32767.f + 32767.f); // Need to scale AKF by MAX^2 and AMDF by MAX, so do some maths to divide only once
		else
			return accumDistAKF / (accumDistAMDF + 1.f);
	}

	template<typename T>
	int GetTone(T *samples, int sampleCt, double* maxVolume, float *weights, bool scale){
		if(!SamplesPerPeriodPerTone)
			return -1;
		T maxVolumeL = 0;
		for(int i = 0; i < sampleCt; i++){
			T vol = abs(samples[i]);
			if(vol > maxVolumeL)
				maxVolumeL = vol;
		}
		if(scale)
			maxVolumeL /= 32767.;
		*maxVolume = maxVolumeL;

		T maxWeight = -1;
		T minWeight = 1;
		int maxTone = -1;
		for (int toneIndex = MinHalfTone; toneIndex <= MaxHalfTone; toneIndex++)
		{
			T curWeight = _AnalyzeToneFunc<T>(samples, sampleCt, toneIndex, scale);

			if (curWeight > maxWeight)
			{
				maxWeight = curWeight;
				maxTone = toneIndex;
			}

			if (curWeight < minWeight)
				minWeight = curWeight;

			weights[toneIndex - MinHalfTone] = static_cast<float>(curWeight);
		}

		if(maxWeight - minWeight > 0.01){
			return maxTone;
		}else return -1;
	}

	template int GetTone(float *samples, int sampleCt, double* maxVolume, float *weights, bool scale);
	template int GetTone(double *samples, int sampleCt, double* maxVolume, float *weights, bool scale);

}
*/