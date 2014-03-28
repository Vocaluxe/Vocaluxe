#region license
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
#endregion

#define TEST_PITCH

using System;
#if TEST_PITCH
using System.Windows.Forms;
using System.Diagnostics;
using VocaluxeLib;

#endif

namespace Vocaluxe.Lib.Sound.Record
{
    class CBuffer : IDisposable
    {
        //Half tones: C C♯ D D# E F F♯ G G♯ A A# B
        private const double _BaseToneFreq = 65.4064; // lowest (half-)tone to analyze (C2 = 65.4064 Hz)
#if TEST_PITCH
        private const double _HalftoneBase = 1.05946309436; // 2^(1/12) -> HalftoneBase^12 = 2 (one octave)
#endif
        private const int _HalfToneMin = 0; // C2
        private const int _HalfToneMax = 38; //45; // inclusive, 38=D5; 45=A5
        public const int NumHalfTones = _HalfToneMax - _HalfToneMin + 1; //Number of halftones to analyze
        private CAnalyzer _Analyzer = new CAnalyzer(_BaseToneFreq, _HalfToneMin, _HalfToneMax);

        private double _MaxVolume;

        public CBuffer()
        {
            MinVolume = 0.02f;
            ToneWeigths = new float[NumHalfTones];
            Reset();
#if TEST_PITCH
            _TestPitchDetection();
#endif
        }

        ~CBuffer()
        {
            _Dispose();
        }

        public int ToneAbs { get; private set; }

        public int Tone { get; set; }

        public float MaxVolume
        {
            get { return (float)_MaxVolume; }
        }

        public bool ToneValid { get; private set; }

        public float[] ToneWeigths { get; private set; }

        /// <summary>
        ///     Minimum volume for a tone to be valid
        /// </summary>
        public float MinVolume { get; set; }

        public void Reset()
        {
            ToneValid = false;
            ToneAbs = 0;
            Tone = 0;
            for (int i = 0; i < ToneWeigths.Length; i++)
                ToneWeigths[i] = 0f;
        }

        public void ProcessNewBuffer(byte[] buffer)
        {
            // apply software boost
            //BoostBuffer(Buffer, BufferSize);

            // voice passthrough (send data to playback-device)
            //if (assigned(fVoiceStream)) then
            //fVoiceStream.WriteData(Buffer, BufferSize);
            _Analyzer.Input(buffer);
        }

        public void AnalyzeBuffer()
        {
            _Analyzer.Process();
            _MaxVolume = _Analyzer.GetPeak() / 43 + 1;
            int tone = (int)Math.Round(_Analyzer.FindNote());
            if (tone >= 0)
            {
                ToneAbs = tone;
                Tone = ToneAbs % 12;
                ToneValid = true;
            }
            else
                ToneValid = false;
        }

        public void Dispose()
        {
            _Dispose();
            GC.SuppressFinalize(this);
        }

        private void _Dispose()
        {
            if (_Analyzer != null)
            {
                _Analyzer.Dispose();
                _Analyzer = null;
            }
        }

#if TEST_PITCH
        private static bool _PitchTestRun;

        private static string _ToneToNote(int tone, bool withOctave = true)
        {
            tone += 24;
            string[] notes = {"C", "C♯", "D", "D#", "E", "F", "F♯", "G", "G♯", "A", "A#", "B"};
            string result = notes[tone % 12];
            if (withOctave)
                result += (tone / 12);
            return result;
        }

        private void _TestPitchDetection()
        {
            if (_PitchTestRun)
                return;
            _PitchTestRun = true;
            int toneFrom = Math.Max(0, _HalfToneMin);
            const int toneTo = 47; //B5
            Console.WriteLine("Testing notes " + _ToneToNote(toneFrom) + " - " + _ToneToNote(toneTo));
            byte[] data;
            int ok = 0;
            double angle = 0;
            for (int distort = 0; distort < 10; distort++)
            {
                for (int tone = toneFrom; tone <= toneTo; tone++)
                {
                    _GetSineWave(_BaseToneFreq * Math.Pow(_HalftoneBase, tone), 44100, ref angle, out data);
                    _Distort(data, tone, distort);

                    ProcessNewBuffer(data);
                    AnalyzeBuffer();
                    if (!ToneValid)
                        CBase.Log.LogDebug("Note " + _ToneToNote(tone) + " not detected (Distortion: " + distort + ")");
                    else if (Tone != tone % 12)
                        CBase.Log.LogDebug("Note " + _ToneToNote(tone) + " wrongly detected as " + _ToneToNote(Tone, false) + " (Distortion: " + distort + ")");
                    else
                        ok++;
                }
            }
            _GetSineWave(_BaseToneFreq * Math.Pow(_HalftoneBase, 5), 44100, ref angle, out data);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            const int repeats = 1000;
            for (int i = 0; i < repeats; i++)
            {
                ProcessNewBuffer(data);
                AnalyzeBuffer();
            }
            sw.Stop();
            string msg = "Analyser: Errors: " + ((toneTo - toneFrom + 1) * 10 - ok) + "; Speed: " +
                         (int)(repeats / (sw.ElapsedMilliseconds / 1000.0)) + " buffers/s";
            Console.WriteLine(msg);
            MessageBox.Show(msg);
            Reset();
        }

        private static short[] _GetDistort(int len, int tone, int type)
        {
            short[] sdata = new short[len];
            if (type < 9)
            {
                int newTone = 1;
                switch (type)
                {
                    case 0:
                        newTone = 2;
                        break;
                    case 1:
                        newTone = 3;
                        break;
                    case 2:
                        newTone = 6;
                        break;
                    case 3:
                        newTone = 8;
                        break;
                    case 4:
                        newTone = 10;
                        break;
                    case 5:
                        newTone = 15;
                        break;
                    case 6:
                        newTone = 25;
                        break;
                    case 7:
                        newTone = 26;
                        break;
                    case 8:
                        newTone = 29;
                        break;
                }
                byte[] data2;
                double angle = 0;
                _GetSineWave(_BaseToneFreq * Math.Pow(_HalftoneBase, tone + newTone), 44100, ref angle, out data2);
                Buffer.BlockCopy(data2, 0, sdata, 0, len * 2);
            }
            else
            {
                Random r = new Random(0xBEEF);
                for (int i = 0; i < sdata.Length; i++)
                    sdata[i] = (short)(r.Next() - Int16.MinValue);
            }
            return sdata;
        }

        private static void _Distort(byte[] data, int tone, int distortCt)
        {
            if (distortCt < 1)
                return;
            short[] sdata = new short[data.Length / 2];
            Buffer.BlockCopy(data, 0, sdata, 0, data.Length);

            short[][] distortions = new short[distortCt][];
            for (int i = 0; i < distortCt; i++)
                distortions[i] = _GetDistort(data.Length / 2, tone, i);

            for (int i = 0; i < sdata.Length; i++)
            {
                double distortion = 0;
                for (int j = 0; j < distortCt; j++)
                    distortion += distortions[j][i];
                distortion /= distortCt;
                sdata[i] = (short)(sdata[i] * 4.0 / 5.0 + distortion / 5.0);
            }
            Buffer.BlockCopy(sdata, 0, data, 0, data.Length);
        }

        private static void _GetSineWave(double freq, int sampleRate, ref double angle, out byte[] data)
        {
            const short max = short.MaxValue;
            const int len = 4096; //sampleRate * durationMs / 1000;
            short[] data16Bit = new short[len];
            for (int i = 0; i < len; i++)
                data16Bit[i] = (short)(Math.Sin(2 * Math.PI * i / sampleRate * freq + angle) * max);
            angle = 2 * Math.PI * len / sampleRate * freq + angle;
            angle = angle % (2 * Math.PI);
            data = new byte[data16Bit.Length * 2];
            Buffer.BlockCopy(data16Bit, 0, data, 0, data.Length);
        }
#endif
    }
}