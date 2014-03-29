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

#include "pitchTracker.h"
#include <math.h>
#include <stdlib.h>
#include <stdarg.h>
#include <stdio.h>

namespace PitchTracker{

	static void doLog(const char* msg,...){
		va_list argptr = NULL;
		va_start(argptr, msg);

		char buffer[200];  
		vsprintf_s(buffer,msg, argptr);
		if(Log) Log(buffer);
		va_end(argptr);    
	}

	void SetLogCallback(LogCallback Callback){
		Log=Callback;
	}

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