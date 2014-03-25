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

//#define TEST_PITCH
#define USE_NATIVE_DETECTION
//If defined, it uses a library with native code that speeds up detection by a factor of 10

using System;
using System.IO;
using System.Diagnostics;
#if USE_NATIVE_DETECTION
using Native.PitchTracking;
#endif
#if TEST_PITCH
using System.Windows.Forms;
using VocaluxeLib;

#endif

namespace Vocaluxe.Lib.Sound.Record
{
    class CBuffer : IDisposable
    {
        private static int _InitCount;
        //Half tones: C C♯ D D# E F F♯ G G♯ A A# B
        private const double _BaseToneFreq = 65.4064; // lowest (half-)tone to analyze (C2 = 65.4064 Hz)
        private const double _HalftoneBase = 1.05946309436; // 2^(1/12) -> HalftoneBase^12 = 2 (one octave)
        private const int _HalfToneMin = 0; // C2
        private const int _HalfToneMax = 38; //45; // inclusive, 38=D5; 45=A5
        public const int NumHalfTones = _HalfToneMax - _HalfToneMin + 1; //Number of halftones to analyze

        private const int _AnalysisBufLen = 4096;
        private const int _AnalysisByteBufLen = _AnalysisBufLen * 2;

        private readonly short[] _AnalysisBuffer = new short[_AnalysisBufLen];
        private readonly byte[] _AnalysisByteBuffer = new byte[_AnalysisByteBufLen]; //tmp buffer (stream->tmpBuffer->(Int16)AnalysisBuffer)

        private readonly float[] _TmpWeights = new float[NumHalfTones]; //tmp buffer for weights (gets copied to ToneWeight when checked)
#if !USE_NATIVE_DETECTION
    //Precalculated table holding sample# per period for each tone (actually we may not use the lower few entries, but keep it to avoid index modification
        private static readonly double[] _SamplesPerPeriodPerTone = new double[_HalfToneMax + 1];

        public enum EAnalyzeFunction
        {
            AutoCorrelation, //Faster, more resistent to noise, good value for HalfToneMax=38 (higher->wrong results in test case, WHY?) with 38 no wrong values in test case
            Amdf //Average magnitude difference, slower, detects everything in range -> HalfToneMax=45
        }
        private delegate double DAnalyzeTone(int tone);

        // ReSharper disable MemberCanBePrivate.Global
        // ReSharper disable ConvertToConstant.Global
        // ReSharper disable FieldCanBeMadeReadOnly.Global
        public static EAnalyzeFunction AnalyzeFunction = EAnalyzeFunction.AutoCorrelation;
        // ReSharper restore FieldCanBeMadeReadOnly.Global
        // ReSharper restore ConvertToConstant.Global
        // ReSharper restore MemberCanBePrivate.Global
        // ReSharper disable FieldCanBeMadeReadOnly.Local
        private DAnalyzeTone _AnalyzeToneFunc;
        private double _MinWeightDiff;
        // ReSharper restore FieldCanBeMadeReadOnly.Local
#endif
        private double _MaxVolume;
        private bool _NewSamples;

        private MemoryStream _Stream = new MemoryStream(); // full buffer

        public CBuffer()
        {
            _Init();
            MinVolume = 0.02f;
            ToneWeigth = new float[NumHalfTones];
            Reset();
#if !USE_NATIVE_DETECTION
            if (AnalyzeFunction == EAnalyzeFunction.Amdf)
            {
                _AnalyzeToneFunc = _GetAmdf;
                _MinWeightDiff = 0.01;
            }
            else if (AnalyzeFunction == EAnalyzeFunction.AutoCorrelation)
            {
                _AnalyzeToneFunc = _GetAutoCorrelation;
                _MinWeightDiff = 0.0001;
            }
#endif
#if TEST_PITCH
            _TestPitchDetection();
#endif
        }

        ~CBuffer()
        {
            _Dispose();
        }

        private static void _Init()
        {
            _InitCount++;
            if (_InitCount > 1)
                return; //Do init only once
#if USE_NATIVE_DETECTION
            CPitchTracker.Init(_BaseToneFreq, _HalfToneMin, _HalfToneMax);
#else
    //Init Array to avoid costly calculations
            for (int toneIndex = 0; toneIndex <= _HalfToneMax; toneIndex++)
            {
                double freq = _BaseToneFreq * Math.Pow(_HalftoneBase, toneIndex);
                _SamplesPerPeriodPerTone[toneIndex] = 44100.0 / freq; // samples in one period
            }
#endif
        }

        public int ToneAbs { get; private set; }

        public int Tone { get; set; }

        public float MaxVolume
        {
            get { return (float)_MaxVolume; }
        }

        public bool ToneValid { get; private set; }

        public float[] ToneWeigth { get; private set; }

        /// <summary>
        ///     Minimum volume for a tone to be valid
        /// </summary>
        public float MinVolume { get; set; }

        public void Reset()
        {
            lock (_Stream)
            {
                _Stream.SetLength(0L);
                _NewSamples = false;
            }
            ToneValid = false;
            ToneAbs = 0;
            Tone = 0;
            for (int i = 0; i < ToneWeigth.Length; i++)
                ToneWeigth[i] = 0f;
        }

        private void _Add(byte[] bytes)
        {
            _Stream.Write(bytes, 0, bytes.Length);
        }

        public void ProcessNewBuffer(byte[] buffer)
        {
            // apply software boost
            //BoostBuffer(Buffer, BufferSize);

            // voice passthrough (send data to playback-device)
            //if (assigned(fVoiceStream)) then
            //fVoiceStream.WriteData(Buffer, BufferSize);
            lock (_Stream)
            {
                _Add(buffer);
                _NewSamples = true;
            }
        }

        public void AnalyzeBuffer()
        {
            if (!_NewSamples)
                return;

            lock (_Stream)
            {
                if (_Stream.Length >= _AnalysisByteBufLen)
                {
                    _Stream.Position -= _AnalysisByteBufLen;
                    _Stream.Read(_AnalysisByteBuffer, 0, _AnalysisByteBufLen);
                }
                _NewSamples = false;
            }

            Buffer.BlockCopy(_AnalysisByteBuffer, 0, _AnalysisBuffer, 0, _AnalysisByteBufLen);

            try
            {
                // find maximum volume
                _FindMaxVolume();

                if (MaxVolume >= MinVolume)
                    _AnalyzeTones();
                else
                    ToneValid = false;
            }
            catch (Exception) {}
        }

        private void _FindMaxVolume()
        {
            int maxVolume = 0;
            for (int i = 0; i < _AnalysisBufLen / 4; i++)
            {
                int volume = _AnalysisBuffer[i];
                if (volume < 0)
                    volume = -volume;
                if (volume > maxVolume)
                    maxVolume = volume;
            }
            _MaxVolume = (double)maxVolume / Int16.MaxValue;
        }

        private void _AnalyzeTones()
        {
#if USE_NATIVE_DETECTION
            int maxTone = CPitchTracker.GetTone(_AnalysisBuffer, _TmpWeights);
            if (maxTone >= 0)
#else
    // prepare to analyze
            double maxWeight = -1.0;
            double minWeight = 1.0;
            int maxTone = -1;


            // analyze halftones
            // Note: at the lowest tone (~65Hz) and a buffer-size of 4096
            // at 44.1 (or 48kHz) only 6 (or 5) samples are compared, this might be
            // too few samples -> use a bigger buffer-size
            for (int toneIndex = _HalfToneMin; toneIndex <= _HalfToneMax; toneIndex++)
            {
                double curWeight = _AnalyzeToneFunc(toneIndex);

                if (curWeight > maxWeight)
                {
                    maxWeight = curWeight;
                    maxTone = toneIndex;
                }

                if (curWeight < minWeight)
                    minWeight = curWeight;

                _TmpWeights[toneIndex - _HalfToneMin] = (float)curWeight;
            }

            if (maxWeight - minWeight > _MinWeightDiff)
#endif
            {
                Array.Copy(_TmpWeights, ToneWeigth, NumHalfTones);

                ToneAbs = maxTone;
                //Console.WriteLine(maxTone);
                Tone = maxTone % 12;
                ToneValid = true;
            }
            else
                ToneValid = false;
        }

#if !USE_NATIVE_DETECTION
        private double _GetAutoCorrelation(int tone)
        {
            double samplesPerPeriodD = _SamplesPerPeriodPerTone[tone]; // samples in one period
            int samplesPerPeriod = (int)samplesPerPeriodD;
            double fHigh = samplesPerPeriodD - samplesPerPeriod;
            double fLow = 1.0 - fHigh;

            double accumDist = 0; // accumulated distances

            // compare correlating samples
            int sampleIndex = 0; // index of sample to analyze
            // Start value= index of sample one period ahead
            for (int correlatingSampleIndex = sampleIndex + samplesPerPeriod; correlatingSampleIndex + 1 < _AnalysisBufLen; correlatingSampleIndex++, sampleIndex++)
            {
                // calc distance to corresponding sample in next period (lower means more distant)
                double dist = _AnalysisBuffer[sampleIndex] *
                              (_AnalysisBuffer[correlatingSampleIndex] * fLow + _AnalysisBuffer[correlatingSampleIndex + 1] * fHigh);
                accumDist += dist;
            }

            //Using _AnalysisBufLen here makes it return correct values among all analyzed frequencies
            const double scaleValue = (double)Int16.MaxValue * (double)Int16.MaxValue * _AnalysisBufLen;
            return accumDist / scaleValue;
        }

        private double _GetAmdf(int tone)
        {
            double samplesPerPeriodD = _SamplesPerPeriodPerTone[tone]; // samples in one period
            int samplesPerPeriod = (int)samplesPerPeriodD;
            double fHigh = samplesPerPeriodD - samplesPerPeriod;
            double fLow = 1.0 - fHigh;

            double accumDist = 0; // accumulated distances

            // compare correlating samples
            int sampleIndex = 0; // index of sample to analyze
            // Start value= index of sample one period ahead
            for (int correlatingSampleIndex = sampleIndex + samplesPerPeriod; correlatingSampleIndex + 1 < _AnalysisBufLen; correlatingSampleIndex++, sampleIndex++)
            {
                // calc distance (correlation: 1-dist/IntMax*2) to corresponding sample in next period (0=equal .. IntMax*2=totally different)
                //int dist = (int)Math.Abs(_AnalysisBuffer[sampleIndex] - _AnalysisBuffer[correlatingSampleIndex]);
                double dist = Math.Abs(_AnalysisBuffer[sampleIndex] -
                                       (_AnalysisBuffer[correlatingSampleIndex] * fLow + _AnalysisBuffer[correlatingSampleIndex + 1] * fHigh));
                accumDist += dist;
            }
            //Use analyzed sample count here. BufLen yields more errors
            return 1.0 - accumDist / Int16.MaxValue / sampleIndex;
        }
#endif

        public void Dispose()
        {
            _Dispose();
            GC.SuppressFinalize(this);
        }

        private void _Dispose()
        {
            if (_Stream != null)
            {
                _Stream.Dispose();
                _Stream = null;
            }
#if USE_NATIVE_DETECTION
            Debug.Assert(_InitCount > 0);
            _InitCount--;
            if (_InitCount == 0)
                CPitchTracker.DeInit();
#endif
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
            for (int distort = 0; distort < 10; distort++)
            {
                for (int tone = toneFrom; tone <= toneTo; tone++)
                {
                    _GetSineWave(_BaseToneFreq * Math.Pow(_HalftoneBase, tone), 44100, out data);
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
            _GetSineWave(_BaseToneFreq * Math.Pow(_HalftoneBase, 5), 44100, out data);
            ProcessNewBuffer(data);
            AnalyzeBuffer();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            const int repeats = 2000;
            for (int i = 0; i < repeats; i++)
            {
                //ProcessNewBuffer(data);
                //AnalyzeBuffer();
                _FindMaxVolume();
                _AnalyzeTones();
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
                _GetSineWave(_BaseToneFreq * Math.Pow(_HalftoneBase, tone + newTone), 44100, out data2);
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

        private static void _GetSineWave(double freq, int sampleRate, out byte[] data)
        {
            const short max = short.MaxValue;
            const int len = _AnalysisBufLen; //sampleRate * durationMs / 1000;
            short[] data16Bit = new short[len];
            for (int i = 0; i < len; i++)
                data16Bit[i] = (short)(Math.Sin(2 * Math.PI * i / sampleRate * freq) * max);
            data = new byte[data16Bit.Length * 2];
            Buffer.BlockCopy(data16Bit, 0, data, 0, data.Length);
        }
#endif
    }
}