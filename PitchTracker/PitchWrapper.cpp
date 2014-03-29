#include "PitchWrapper.h"
#include "pitchTracker.h"
#include "Helper.h"

AnalyzerExt::AnalyzerExt(double baseToneFrequency, int minHalfTone, int maxHalfTone, unsigned step): Analyzer(44100., "", step){
	PitchTracker::Init(baseToneFrequency, minHalfTone, maxHalfTone);
}

AnalyzerExt::~AnalyzerExt(){
	PitchTracker::DeInit();
}

int AnalyzerExt::GetNoteFast(double* maxVolume, float* weights){
	float AnaylsisBuf[m_SampleCt];
	size_t size = m_fastAnalysisBuf.size();
	if(size > m_SampleCt)
		m_fastAnalysisBuf.pop(size - m_SampleCt);
	if(!m_fastAnalysisBuf.read(AnaylsisBuf, AnaylsisBuf + m_SampleCt))
		return -1;
	return PitchTracker::GetTone(AnaylsisBuf, m_SampleCt, maxVolume, weights);
}

void PtFast_Init(double baseToneFrequency, int minHalfTone, int maxHalfTone){
	PitchTracker::Init(baseToneFrequency, minHalfTone, maxHalfTone);
}

void PtFast_DeInit(){
	PitchTracker::DeInit();
}

int PtFast_GetTone(short* samples, int sampleCt, double* maxVolume, float* weights){
	if(sampleCt <= 0)
		return -1;
	double* dataDouble = short2DoubleArray(samples, sampleCt, false);
	int result = PitchTracker::GetTone(dataDouble, sampleCt, maxVolume, weights, true);
	freeDoubleArray(dataDouble);
	return result;
}

AnalyzerExt* Analyzer_Create(double baseToneFrequency, int minHalfTone, int maxHalfTone, unsigned step){
    return new AnalyzerExt(baseToneFrequency, minHalfTone, maxHalfTone, step);
}

void Analyzer_Free(AnalyzerExt* analyzer){
    if(analyzer)
		delete analyzer;
}

void Analyzer_InputFloat(AnalyzerExt* analyzer, float* data, int sampleCt){
	if(sampleCt == 0 || !analyzer)
		return;
	analyzer->input(data, data + sampleCt);
}

void Analyzer_InputShort(AnalyzerExt* analyzer, short* data, int sampleCt){
	if(sampleCt == 0 || !analyzer)
		return;
	float* dataFloat = short2FloatArray(data, sampleCt);
	analyzer->input(dataFloat, dataFloat + sampleCt);
	freeFloatArray(dataFloat);  
}

void Analyzer_InputByte(AnalyzerExt* analyzer, char* data, int sampleCt){
	if(sampleCt == 0 || !analyzer)
		return;
	float* dataFloat = short2FloatArray(static_cast<short*>(static_cast<void*>(data)), sampleCt);
	analyzer->input(dataFloat, dataFloat + sampleCt);
	freeFloatArray(dataFloat);
}

void Analyzer_Process(AnalyzerExt* analyzer){
	if(!analyzer)
		return;
    analyzer->process();
}

double Analyzer_GetPeak(AnalyzerExt* analyzer){
	if(!analyzer)
		return -999;
	return analyzer->getPeak();   
}

double Analyzer_FindNote(AnalyzerExt* analyzer, double minFreq, double maxFreq){
	if(!analyzer)
		return -1;
	const Tone* tone = analyzer->findTone(minFreq, maxFreq);
	return (tone == NULL) ? -1 : ToneToNote(tone);    
}

int Analyzer_GetNoteFast(AnalyzerExt* analyzer, double* maxVolume, float* weights){
	if(!analyzer)
		return -1;
	return analyzer->GetNoteFast(maxVolume, weights);
}

bool Analyzer_OutputFloat(AnalyzerExt* analyzer, float* data, int sampleCt, float rate){
	if(!analyzer)
		return false;
    return analyzer->output(data, data + sampleCt, rate);
}


/*
namespace Native{
	namespace PitchTracking{

		//CAnalyzer

		void CAnalyzer::Input(array<float>^ data){
			if(data->Length == 0)
				return;
			pin_ptr<float> dataPtr = &data[0];
			float* dataPtr2 = dataPtr;
			m_analyzer->input(dataPtr2, dataPtr2 + data->Length);
		}

		void CAnalyzer::Input(array<short>^ data){
			if(data->Length == 0)
				return;
			pin_ptr<short> dataPtr = &data[0];
			float* dataFloat = short2FloatArray(dataPtr, data->Length);
			m_analyzer->input(dataFloat, dataFloat + data->Length);
			freeFloatArray(dataFloat);
		}

		void CAnalyzer::Input(array<Byte>^ data){
			if(data->Length == 0)
				return;
			pin_ptr<Byte> dataPtr = &data[0];
			float* dataFloat = short2FloatArray(static_cast<short*>(static_cast<void*>(dataPtr)), data->Length / 2);
			m_analyzer->input(dataFloat, dataFloat + data->Length);
			freeFloatArray(dataFloat);
		}

		void CAnalyzer::Process(){
			m_analyzer->process();
		}

		double CAnalyzer::GetPeak(){
			return m_analyzer->getPeak();
		}

		array<CTone^>^ CAnalyzer::GetTones(){
			Analyzer::tones_t umTones = m_analyzer->getTones();
			array<CTone^>^ tones = gcnew array<CTone^>(static_cast<int>(umTones.size()));
			int i = 0;
			for(auto it=umTones.begin(); it!=umTones.end(); it++, i++){
				tones[i]=ConvertToneToManaged(*it);
			}
			return tones;
		}

		CTone^ CAnalyzer::FindTone(){
			return FindTone(65.0);
		}

		CTone^ CAnalyzer::FindTone(double minFreq){
			return FindTone(minFreq, 1000.0);
		}

		CTone^ CAnalyzer::FindTone(double minFreq, double maxFreq){
			const Tone* tone = m_analyzer->findTone(minFreq, maxFreq);
			return (tone == NULL) ? nullptr : ConvertToneToManaged(tone);
		}

		void CAnalyzer::Output(array<float>^ data, float rate){
			pin_ptr<float> dataPtr = &data[0];
			m_analyzer->output(dataPtr, dataPtr + data->Length, rate);
		}

		String^ CAnalyzer::GetId(){
			return m_id;
		}


		//CPitchTracker

		void CPitchTracker::Init(double baseToneFrequency, int minHalfTone, int maxHalfTone){
			PitchTracker::Init(baseToneFrequency, minHalfTone, maxHalfTone);
		}

		void CPitchTracker::DeInit(){
			PitchTracker::DeInit();
		}

		int CPitchTracker::GetTone(array<short>^ samples, array<float>^ weights){
			if(samples->Length == 0)
				return -1;
			pin_ptr<short> dataPtr = &samples[0];
			pin_ptr<float> weightsPtr = &weights[0];
			double* dataDouble = short2DoubleArray(dataPtr, samples->Length, false);
			int result = PitchTracker::GetTone(dataDouble, samples->Length, weightsPtr, true);
			freeDoubleArray(dataDouble);
			return result;
		}
	}
}
*/