#include "ptDyWa.h"
#include "../Helper.h"


PtDyWa::PtDyWa(unsigned step){
	_Step = step;
	dywapitch_inittracking(&_State);
	_VolTreshold = 0.01f;
	_LastMaxVol = 0.f;
}

void PtDyWa::SetVolumeThreshold(float threshold){
	_VolTreshold = threshold;
}

double PtDyWa::FindNote(float* maxVolume){
	double AnaylsisBuf[_SampleCt];
	size_t size = _AnalysisBuf.size();
	if(size > _SampleCt)
		_AnalysisBuf.pop(size - _SampleCt);
	if(!_AnalysisBuf.read(AnaylsisBuf, AnaylsisBuf + _SampleCt)){
		*maxVolume = _LastMaxVol * 0.85f;
		return -1;
	}
	_AnalysisBuf.pop(_Step);
	double pitch = dywapitch_computepitch(&_State, AnaylsisBuf, 0, _SampleCt, maxVolume, _VolTreshold);
	if(pitch == 0.0)
		return -1;
	else
		return FreqToNote(pitch);
}