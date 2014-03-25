#include "PitchWrapper.h"
#pragma managed(push, off)
#include "pitchTracker.h"
#pragma managed(pop)

#include "Helper.h"

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
			return (tone == NULL) ? nullptr : ConvertToneToManaged(*tone);
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