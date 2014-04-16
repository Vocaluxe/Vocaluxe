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

static constexpr double BaseToneFrequency = 65.4064; // lowest (half-)tone to analyze (C2 = 65.4064 Hz)
static constexpr double HalftoneBase = 1.05946309436; // 2^(1/12) -> HalftoneBase^12 = 2 (one octave)
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
		double cosMult = 2. * M_PI / (_SampleCt - 1.); //To simplify and speed up
		for (int i = 0; i < _SampleCt; i++) {
			_Window[i] = static_cast<float>(0.54 - 0.46 * cos(i * cosMult));
		}
	}
	_Step = step;
	_VolTreshold = 0.01f;
	_LastMaxVol = 0.f;
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
	size_t size = _AnalysisBuf.size();
	if(size > _SampleCt)
		_AnalysisBuf.pop(size - _SampleCt);
	if(!_AnalysisBuf.read(AnaylsisBuf, AnaylsisBuf + _SampleCt)){
		*maxVolume = _LastMaxVol * 0.85f;
		return -1;
	}
	_AnalysisBuf.pop(_Step);
	return _GetNote(AnaylsisBuf, maxVolume, weights);
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

int PtAKF::_GetNote(float* restrict samples, float* restrict maxVolume, float* restrict weights){
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

	float samplesWindowed[_SampleCt];

	for(int i = 0; i < _SampleCt; i++){
		samplesWindowed[i] = samples[i] * _Window[i];
	}

	// Now analyze the samples and get peaks at the most appropriate tones

	//Attention: We have a peak at lag 0 that might stretch that far, that we detect a wrong "peak" at _MaxHalfTone
	//Because of that we filter out all tones that are past the last zero crossing from below but keep tones with decreasing weights (going towards zero crossing from above)
	int lastValidTone = 0;
	float lastWeight = 1.f;
	float maxWeight = 0.f;
	for (int toneIndex = _MinHalfTone; toneIndex <= _MaxHalfTone; toneIndex++)
	{
		float curWeight = _AnalyzeByTone(samples, samplesWindowed, toneIndex);

		if(curWeight > maxWeight){
			maxWeight = curWeight;
		}

		weights[toneIndex - _MinHalfTone] = curWeight;

		//Filter:
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
		//Set all invalid weights to 0
		for(int toneIndex = lastValidTone + 1;toneIndex < _MaxHalfTone; toneIndex++){
			weights[toneIndex - _MinHalfTone] = 0.f;
		}
	}

	SPeak peaks[_MaxPeaks];
	InitPeaks(peaks);
	int numPeaks = 0;
	bool up = true;
	for(int i = _MinHalfTone + 1; i <= _MaxHalfTone; i++){
		if(weights[i-1] > weights[i]){
			if(up){
				AddPeak(peaks, weights[i - 1 - _MinHalfTone], i - 1);
				numPeaks++;
				up = false;
			}
		}else if(!up){
			up = true;
		}
	}
	if(up){
		AddPeak(peaks, weights[_MaxHalfTone - _MinHalfTone], _MaxHalfTone);
		numPeaks++;
	}
	if(numPeaks > _MaxPeaks)
		numPeaks = _MaxPeaks;

	// Further analyze the found peaks and find the real maximum
	// This is necessary because we may have our real peak a bit off the exact tone frequency
	// and a 'wrong' peak that is exactly at another tone which might become higher than the one at the 'right' tone

	int maxTone = peaks[_MaxPeaks-1].toneIndex;
	int maxToneFine = -1;
	maxWeight = peaks[_MaxPeaks-1].weight;
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
		weights[toneIndex - _MinHalfTone] = curWeight;
		if(curWeight > maxWeight){
			maxTone = toneIndex;
			maxToneFine = 1 - otherToneFineIndex;
			maxWeight = curWeight;
		}

		// Now check also neighbouring tone
		if(otherToneIndex > 0){
			float curWeightOther = _AnalyzeBySampleCt(samples,  samplesWindowed, _SamplesPerPeriodPerToneFine[otherToneIndex * 2 + otherToneFineIndex]);
			if(curWeightOther > weights[otherToneIndex - _MinHalfTone]){
				weights[otherToneIndex - _MinHalfTone] = curWeightOther;
				if(curWeightOther > maxWeight){
					maxTone = otherToneIndex;
					maxToneFine = otherToneFineIndex;
					maxWeight = curWeightOther;
				}
			}
		}
	}

	float energy = _AKFBySampleCt(samples, 0);
	float maxAKF = (maxToneFine < 0) ? _AKFByTone(samples, maxTone) : _AKFBySampleCt(samples, _SamplesPerPeriodPerToneFine[maxTone * 2 + maxToneFine]);

	//if(maxWeight - minWeight > 0.025){
	if(maxAKF >= 0.35f * energy){
		return maxTone;
	}else return -1;
}

float PtAKF::_AnalyzeByTone(float* restrict samples, float* restrict samplesWindowed, int toneIndex){
	return _AnalyzeBySampleCt(samples, samplesWindowed, _SamplesPerPeriodPerTone[toneIndex]);
}

float PtAKF::_AnalyzeBySampleCt(float* restrict samples, float* restrict samplesWindowed, float samplesPerPeriodD){
	// Use method by Kobayashi and Shimamura (2001): Combine AKF and AMDF to a new f(z)=AKF(z)/(AMDF(z)+k) with k=1

	float akf = _AKFBySampleCt(samplesWindowed, samplesPerPeriodD);
	float amdf = _AMDFBySampleCt(samples, samplesPerPeriodD);

	//return accumDistAKF / (accumDistAMDF * 32767.f + 32767.f * 32767.f); // Need to scale AKF by MAX^2 and AMDF by MAX, so do some maths to divide only once
	float result = akf / (amdf + 1.f);
	return result; // Need to scale AKF by MAX^2 and AMDF by MAX, so do some maths to divide only once
	//{toneIndex}: {accumDistAKF} ; {accumDistAMDF}; {result}
}

float PtAKF::_AKFByTone(float* restrict samples, int toneIndex){
	return _AKFBySampleCt(samples, _SamplesPerPeriodPerTone[toneIndex]);
}

float PtAKF::_AKFBySampleCt(float* restrict samples, float samplesPerPeriodD){
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
		accumDist += xn * xnt;
	}

	return accumDist / _SampleCt;
}

float PtAKF::_AMDFByTone(float* restrict samples, int toneIndex){
	return _AKFBySampleCt(samples, _SamplesPerPeriodPerTone[toneIndex]);
}

float PtAKF::_AMDFBySampleCt(float* restrict samples, float samplesPerPeriodD){
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
		accumDist += abs(xn - xnt);
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