#pragma once

#include "performous/pitch.hh"

#define DllExport extern "C" __declspec(dllexport)

class AnalyzerExt: public Analyzer{
public:
	AnalyzerExt(double baseToneFrequency, int minHalfTone, int maxHalfTone, unsigned step = 200);
	~AnalyzerExt();
	template <typename InIt> void input(InIt begin, InIt end){
		m_fastAnalysisBuf.insert(begin, end);
		Analyzer::input(begin, end);
	}
	int GetNoteFast(double* maxVolume, float* weights);
private:
	constexpr static size_t m_SampleCt = 4096;
	RingBuffer<m_SampleCt * 2> m_fastAnalysisBuf; // Buffer used for fast analysis instead of FFT
};

DllExport void PtFast_Init(double baseToneFrequency, int minHalfTone, int maxHalfTone);
DllExport void PtFast_DeInit();
DllExport int PtFast_GetTone(short* samples, int sampleCt, float* weights);

DllExport AnalyzerExt* Analyzer_Create(double baseToneFrequency, int minHalfTone, int maxHalfTone, unsigned step);
DllExport void Analyzer_Free(AnalyzerExt* analyzer);
DllExport void Analyzer_InputFloat(AnalyzerExt* analyzer, float* data, int sampleCt);
DllExport void Analyzer_InputShort(AnalyzerExt* analyzer, short* data, int sampleCt);
DllExport void Analyzer_InputByte(AnalyzerExt* analyzer, char* data, int sampleCt);
DllExport void Analyzer_Process(AnalyzerExt* analyzer);
DllExport double Analyzer_GetPeak(AnalyzerExt* analyzer);
DllExport int Analyzer_GetNoteFast(AnalyzerExt* analyzer, double* maxVolume, float* weights);
DllExport double Analyzer_FindNote(AnalyzerExt* analyzer, double minFreq, double maxFreq);
DllExport bool Analyzer_OutputFloat(AnalyzerExt* analyzer, float* data, int sampleCt, float rate);

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
	AnalyzerExt* m_analyzer;
	String^ m_id;
};

public ref class CPitchTracker abstract sealed{
public:
	static void Init(double baseToneFrequency, int minHalfTone, int maxHalfTone);
	static void DeInit();
	static int GetTone(array<short>^ samples, array<float>^ weights);
};
*/