#pragma once

#pragma managed(push, off)
#include "pitch.hh"
#pragma managed(pop)

using namespace System;
using namespace System::Collections::Generic;

namespace Native{
	namespace PitchTracking{
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
	}
}