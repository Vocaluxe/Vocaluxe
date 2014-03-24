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

void doLog(const char* msg,...){
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

double *SamplesPerPeriodPerTone = NULL;
int MinHalfTone = 0, MaxHalfTone = 38;
const double HalftoneBase = 1.05946309436; // 2^(1/12) -> HalftoneBase^12 = 2 (one octave)

void Init(double baseToneFrequency, int minHalfTone, int maxHalfTone){
	MinHalfTone = minHalfTone;
	MaxHalfTone = maxHalfTone;
	DeInit();
	if(maxHalfTone <= 0)
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

static double _AnalyzeToneFunc(double *samples, int sampleCt, int toneIndex){
	double samplesPerPeriodD = SamplesPerPeriodPerTone[toneIndex]; // samples in one period
	int samplesPerPeriod = (int)samplesPerPeriodD;
	double fHigh = samplesPerPeriodD - samplesPerPeriod;
	double fLow = 1.0 - fHigh;

	double accumDist = 0; // accumulated distances

	// compare correlating samples
	int sampleIndex = 0; // index of sample to analyze
	// Start value= index of sample one period ahead
	for (int correlatingSampleIndex = sampleIndex + samplesPerPeriod; correlatingSampleIndex + 1 < sampleCt; correlatingSampleIndex++, sampleIndex++)
	{
		// calc distance to corresponding sample in next period (lower means more distant)
		double dist = samples[sampleIndex] * (samples[correlatingSampleIndex] * fLow + samples[correlatingSampleIndex + 1] * fHigh);
		accumDist += dist;
	}

	//Using _AnalysisBufLen here makes it return correct values among all analyzed frequencies
	double scaleValue = 32767.0 * 32767.0 * sampleCt; // Divide also by Int16.MaxValue^2
	return accumDist / scaleValue;
}

int GetTone(double *samples, int sampleCt, float *weights){
	double maxVolume = 0;
	for(int i = 0; i < sampleCt; i++){
		double vol = abs(samples[i]);
		if(vol > maxVolume)
			maxVolume = vol;
	}
	double maxWeight = -1;
	double minWeight = 1;
	int maxTone = -1;
	for (int toneIndex = MinHalfTone; toneIndex <= MaxHalfTone; toneIndex++)
	{
		double curWeight = _AnalyzeToneFunc(samples, sampleCt, toneIndex) / maxVolume * 32767.0;

		if (curWeight > maxWeight)
		{
			maxWeight = curWeight;
			maxTone = toneIndex;
		}

		if (curWeight < minWeight)
			minWeight = curWeight;

		weights[toneIndex - MinHalfTone] = (float) curWeight;
	}

	if(maxWeight - minWeight > 0.01){
		return maxTone;
	}else return -1;
}

