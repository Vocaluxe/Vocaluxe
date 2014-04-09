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

#include "ptAKF.h"
#include <math.h>

static constexpr double BaseToneFrequency = 65.4064; // lowest (half-)tone to analyze (C2 = 65.4064 Hz)
static constexpr double HalftoneBase = 1.05946309436; // 2^(1/12) -> HalftoneBase^12 = 2 (one octave)
int PtAKF::_InitCount = 0;
float* PtAKF::_SamplesPerPeriodPerTone = NULL;

PtAKF::PtAKF(){
	_InitCount++;
	if(_InitCount == 1){
		_SamplesPerPeriodPerTone = new float[_MaxHalfTone + 1];
		//Init Array to avoid costly calculations
		for (int toneIndex = 0; toneIndex <= _MaxHalfTone; toneIndex++)
		{
			double freq = BaseToneFrequency * pow(HalftoneBase, toneIndex);
			_SamplesPerPeriodPerTone[toneIndex] = static_cast<float>(44100.0 / freq); // samples in one period
		}
	}
}

PtAKF::~PtAKF(){
	_InitCount--;
	if(!_InitCount){
		delete[] _SamplesPerPeriodPerTone;
		_SamplesPerPeriodPerTone = NULL;
	}
}

int PtAKF::GetNote(double* maxVolume, float* weights){
	float AnaylsisBuf[_SampleCt];
	size_t size = _AnalysisBuf.size();
	if(size > _SampleCt)
		_AnalysisBuf.pop(size - _SampleCt);
	if(!_AnalysisBuf.read(AnaylsisBuf, AnaylsisBuf + _SampleCt))
		return -1;
	return _GetNote(AnaylsisBuf, maxVolume, weights);
}

int PtAKF::_GetNote(float* samples, double* maxVolume, float* weights){
	float maxVolumeL = 0;
	for(int i = 0; i < _SampleCt; i++){
		float vol = abs(samples[i]);
		if(vol > maxVolumeL)
			maxVolumeL = vol;
	}
	maxVolumeL /= 32767.;
	*maxVolume = maxVolumeL;

	float maxWeight = -1;
	float minWeight = 1;
	int maxTone = -1;
	for (int toneIndex = _MinHalfTone; toneIndex <= _MaxHalfTone; toneIndex++)
	{
		float curWeight = _AnalyzeToneFunc(samples, toneIndex);

		if (curWeight > maxWeight)
		{
			maxWeight = curWeight;
			maxTone = toneIndex;
		}

		if (curWeight < minWeight)
			minWeight = curWeight;

		weights[toneIndex - _MinHalfTone] = curWeight;
	}

	if(maxWeight - minWeight > 0.01){
		return maxTone;
	}else return -1;
}

float PtAKF::_AnalyzeToneFunc(float* samples, int toneIndex){
	// Use method by Kobayashi and Shimamura (1995): Combine AKF and AMDF to a new f(z)=AKF(z)/(AMDF(z)+k) with k=1

	float samplesPerPeriodD = _SamplesPerPeriodPerTone[toneIndex]; // samples in one period
	int samplesPerPeriod = static_cast<int>(samplesPerPeriodD);
	float fHigh = samplesPerPeriodD - samplesPerPeriod;
	float fLow = 1.0f - fHigh;

	float accumDistAKF = 0; // accumulated distances
	float accumDistAMDF = 0; // accumulated distances

	// compare correlating samples
	int sampleIndex = 0; // index of sample to analyze
	// Start value= index of sample one period ahead
	for (int correlatingSampleIndex = sampleIndex + samplesPerPeriod; correlatingSampleIndex + 1 < _SampleCt; correlatingSampleIndex++, sampleIndex++)
	{
		// calc distance to corresponding sample in next period
		float xn = samples[sampleIndex];
		float xnt = samples[correlatingSampleIndex] * fLow + samples[correlatingSampleIndex + 1] * fHigh;
		accumDistAKF += xn * xnt;
		accumDistAMDF += abs(xn - xnt);
	}

	accumDistAKF /= _SampleCt;
	accumDistAMDF /= sampleIndex;

	return accumDistAKF / (accumDistAMDF * 32767.f + 32767.f); // Need to scale AKF by MAX^2 and AMDF by MAX, so do some maths to divide only once
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