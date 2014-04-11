#include "PitchWrapper.h"
#include "Helper.h"


Analyzer* Analyzer_Create(unsigned step){
    return new Analyzer(44100, "", step);
}

void Analyzer_Free(Analyzer* analyzer){
    if(analyzer)
		delete analyzer;
}

void Analyzer_InputFloat(Analyzer* analyzer, float* data, int sampleCt){
	if(sampleCt == 0 || !analyzer)
		return;
	analyzer->input(data, data + sampleCt);
}

void Analyzer_InputShort(Analyzer* analyzer, short* data, int sampleCt){
	if(sampleCt == 0 || !analyzer)
		return;
	float* dataFloat = short2FloatArray(data, sampleCt);
	analyzer->input(dataFloat, dataFloat + sampleCt);
	freeFloatArray(dataFloat);  
}

void Analyzer_InputByte(Analyzer* analyzer, char* data, int sampleCt){
	if(sampleCt <= 0 || !analyzer)
		return;
	float* dataFloat = short2FloatArray(static_cast<short*>(static_cast<void*>(data)), sampleCt);
	analyzer->input(dataFloat, dataFloat + sampleCt);
	freeFloatArray(dataFloat);
}

void Analyzer_Process(Analyzer* analyzer){
	if(!analyzer)
		return;
    analyzer->process();
}

double Analyzer_GetPeak(Analyzer* analyzer){
	if(!analyzer)
		return -999;
	return analyzer->getPeak();   
}

double Analyzer_FindNote(Analyzer* analyzer, double minFreq, double maxFreq){
	if(!analyzer)
		return -1;
	const Tone* tone = analyzer->findTone(minFreq, maxFreq);
	return (tone == NULL) ? -1 : FreqToNote(tone->freq);    
}

bool Analyzer_OutputFloat(Analyzer* analyzer, float* data, int sampleCt, float rate){
	if(!analyzer)
		return false;
    return analyzer->output(data, data + sampleCt, rate);
}

PtAKF* PtAKF_Create(unsigned step){
	return new PtAKF(step);
}

void PtAKF_Free(PtAKF* analyzer){
	if(analyzer)
		delete analyzer;
}

int PtAKF_GetNumHalfTones(){
	return PtAKF::GetNumHalfTones();
}

void PtAKF_InputByte(PtAKF* analyzer, char* data, int sampleCt){
	if(sampleCt <= 0 || !analyzer)
		return;
	//short* dataShort = static_cast<short*>(static_cast<void*>(data));
	float* dataFloat = short2FloatArray(static_cast<short*>(static_cast<void*>(data)), sampleCt);
	analyzer->input(dataFloat, dataFloat + sampleCt);
	freeFloatArray(dataFloat);

	//analyzer->input(dataShort, dataShort + sampleCt);
}

int PtAKF_GetNote(PtAKF* analyzer, double* maxVolume, float* weights){
	if(!analyzer)
		return -1;
	return analyzer->GetNote(maxVolume, weights);
}

PtDyWa* PtDyWa_Create(unsigned step){
	return new PtDyWa(step);
}

void PtDyWa_Free(PtDyWa* analyzer){
	if(analyzer)
		delete analyzer;
}

void PtDyWa_InputByte(PtDyWa* analyzer, char* data, int sampleCt){
	if(sampleCt <= 0 || !analyzer)
		return;
	float* dataFloat = short2FloatArray(static_cast<short*>(static_cast<void*>(data)), sampleCt);
	analyzer->input(dataFloat, dataFloat + sampleCt);
	freeFloatArray(dataFloat);
}

void PtDyWa_Process(PtDyWa* analyzer){
	if(!analyzer)
		return;
	analyzer->Process();
}

double PtDyWa_GetNote(PtDyWa* analyzer){
	if(!analyzer)
		return -1;
	return analyzer->GetNote();
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