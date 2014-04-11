#include "ptDyWa.h"
#include "../Helper.h"


PtDyWa::PtDyWa(unsigned step){
	_Step = step;
	dywapitch_inittracking(&_State);
}

void PtDyWa::Process(){
	double AnaylsisBuf[_SampleCt];
	size_t size = _AnalysisBuf.size();
	if(size > _SampleCt)
		_AnalysisBuf.pop(size - _SampleCt);
	if(!_AnalysisBuf.read(AnaylsisBuf, AnaylsisBuf + _SampleCt))
		return;
	_AnalysisBuf.pop(_Step);
	double pitch = dywapitch_computepitch(&_State, AnaylsisBuf,0,_SampleCt);
	if(pitch == 0.0)
		_LastNote = -1;
	else
		_LastNote = FreqToNote(pitch);
}

double PtDyWa::GetNote(){
	return _LastNote;
}