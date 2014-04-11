#pragma once
#include "../performous/pitch.hh"
#include "dywapitchtrack.h"

class PtDyWa{
public:
	PtDyWa(unsigned step);

	/** Add input data to buffer. This is thread-safe (against other functions). **/
	template <typename InIt> void input(InIt begin, InIt end) {
		_AnalysisBuf.insert(begin, end);
	}

	void Process();
	double GetNote();
private:
	dywapitchtracker _State;
	constexpr static size_t _SampleCt = 2048;
	RingBuffer<_SampleCt * 2> _AnalysisBuf;
	unsigned _Step;
	double _LastNote;
};