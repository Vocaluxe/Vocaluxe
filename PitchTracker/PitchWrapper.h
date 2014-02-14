#pragma once

#include "performous/pitch.hh"
#include "ptAKF.h"
#include "dywapitchtrack/ptDyWa.h"

#ifdef __linux__
	#define DllExport extern "C"
#else
	#define DllExport extern "C" __declspec(dllexport)
#endif


DllExport Analyzer* Analyzer_Create(unsigned step);
DllExport void Analyzer_Free(Analyzer* analyzer);
DllExport void Analyzer_InputFloat(Analyzer* analyzer, float* data, int sampleCt);
DllExport void Analyzer_InputShort(Analyzer* analyzer, short* data, int sampleCt);
DllExport void Analyzer_InputByte(Analyzer* analyzer, char* data, int sampleCt);
DllExport void Analyzer_Process(Analyzer* analyzer);
DllExport float Analyzer_GetPeak(Analyzer* analyzer);
DllExport double Analyzer_FindNote(Analyzer* analyzer, double minFreq, double maxFreq);
DllExport bool Analyzer_OutputFloat(Analyzer* analyzer, float* data, int sampleCt, float rate);

DllExport PtAKF* PtAKF_Create(unsigned step);
DllExport void PtAKF_Free(PtAKF* analyzer);
DllExport int PtAKF_GetNumHalfTones();
DllExport void PtAKF_SetVolumeThreshold(PtAKF* analyzer, float threshold);
DllExport float PtAKF_GetVolumeThreshold(PtAKF* analyzer);
DllExport void PtAKF_InputByte(PtAKF* analyzer, char* data, int sampleCt);
DllExport int PtAKF_GetNote(PtAKF* analyzer, float* maxVolume, float* weights);

DllExport PtDyWa* PtDyWa_Create(unsigned step);
DllExport void PtDyWa_Free(PtDyWa* analyzer);
DllExport void PtDyWa_SetVolumeTreshold(PtDyWa* analyzer,float threshold);
DllExport float PtDyWa_GetVolumeThreshold(PtDyWa* analyzer);
DllExport void PtDyWa_InputByte(PtDyWa* analyzer, char* data, int sampleCt);
DllExport double PtDyWa_FindNote(PtDyWa* analyzer, float* maxVolume);

/*
public ref class CTone {
public:
	static const int MAXHARM = 48; ///< The maximum number of harmonics tracked
	static const int MINAGE = 2; ///< The minimum age required for a tone to be output
	double Freq; ///< Frequency (Hz)
	double DB; ///< Level (dB)
	double Stabledb; ///< Stable level, useful for graphics rendering
	int Age; ///< How many times the tone has been detected in row
	double NoteExact;
	int Note;
	/// Less-than compare by levels (instead of frequencies like operator< does)
	static bool dbCompare(CTone l, CTone r) { return l.DB < r.DB; }
};

public ref class CAnalyzer {
public:
	CAnalyzer(double rate, String^ id) {
		m_analyzer = new Analyzer(rate,"");
		m_id = id;
	}
	CAnalyzer(double rate, String^ id, unsigned step) {
		m_analyzer = new Analyzer(rate,"",step);
		m_id = id;
	}
	~CAnalyzer() { this->!CAnalyzer(); }
	!CAnalyzer() { delete m_analyzer; }

	void Input(array<float>^ data);
	void Input(array<short>^ data);
	void Input(array<Byte>^ data);
	void Process();
	double GetPeak();
	array<CTone^>^ GetTones();
	CTone^ FindTone();
	CTone^ FindTone(double minFreq);
	CTone^ FindTone(double minFreq, double maxFreq);
	void Output(array<float>^ data, float rate);
	String^ GetId();

private:
	Analyzer* m_analyzer;
	String^ m_id;
};

public ref class CPitchTracker abstract sealed{
public:
	static void Init(double baseToneFrequency, int minHalfTone, int maxHalfTone);
	static void DeInit();
	static int GetTone(array<short>^ samples, array<float>^ weights);
};
*/