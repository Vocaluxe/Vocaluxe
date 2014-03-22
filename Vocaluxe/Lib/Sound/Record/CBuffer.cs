//#define TEST_PITCH

using System;
using System.IO;

#if TEST_PITCH
using System.Diagnostics;
//using System.Windows.Forms;
using VocaluxeLib;

#endif

namespace Vocaluxe.Lib.Sound.Record
{
    class CBuffer : IDisposable
    {
        public enum EAnalyzeFunction
        {
            AutoCorrelation, //Faster but strict (All right in set range, more resistent to noise, but does not detect tones outside the range)
            Amdf //Average magnitude difference, slower and more heuristic (false detections in set range but also right detections outside)
        }

        private delegate double DAnalyzeTone(int tone);

        //Half tones: C C♯ D Eb E F F♯ G G♯ A Bb B
        private const double _BaseToneFreq = 65.4064; // lowest (half-)tone to analyze (C2 = 65.4064 Hz)
        private const double _HalftoneBase = 1.05946309436; // 2^(1/12) -> HalftoneBase^12 = 2 (one octave)
        public const int NumHalfTones = 24; //Number of halftone to analyze C2-B4

        private const int _AnalysisBufLen = 4096;
        private const int _AnalysisByteBufLen = _AnalysisBufLen * 2;

        private readonly Int16[] _AnalysisBuffer = new Int16[_AnalysisBufLen];
        private readonly byte[] _AnalysisByteBuffer = new byte[_AnalysisByteBufLen]; //tmp buffer (stream->tmpBuffer->(Int16)AnalysisBuffer)

        private static readonly int[] _SamplesPerPeriodPerTone = new int[NumHalfTones]; //Precalculated table holding sample# per period for each tone
        private readonly float[] _TmpWeight = new float[NumHalfTones]; //tmp buffer for weights (gets copied to ToneWeight when checked)
        // ReSharper disable MemberCanBePrivate.Global
        // ReSharper disable ConvertToConstant.Global
        // ReSharper disable FieldCanBeMadeReadOnly.Global
        public static EAnalyzeFunction AnalyzeFunction = EAnalyzeFunction.AutoCorrelation;
        // ReSharper restore FieldCanBeMadeReadOnly.Global
        // ReSharper restore ConvertToConstant.Global
        // ReSharper restore MemberCanBePrivate.Global
        // ReSharper disable FieldCanBeMadeReadOnly.Local
        private DAnalyzeTone _AnalyzeToneFunc;
        // ReSharper restore FieldCanBeMadeReadOnly.Local

        private double _MaxVolume;
        private bool _NewSamples;

        private MemoryStream _Stream = new MemoryStream(); // full buffer

        public CBuffer()
        {
            MinVolume = 0.02f;
            ToneWeigth = new float[NumHalfTones];
            Reset();
            if (AnalyzeFunction == EAnalyzeFunction.Amdf)
                _AnalyzeToneFunc = _GetAmdf;
            else if (AnalyzeFunction == EAnalyzeFunction.AutoCorrelation)
                _AnalyzeToneFunc = _GetAutoCorrelation;
#if TEST_PITCH
            _TestPitchDetection();
#endif
        }

        static CBuffer()
        {
            //Init Array to avoid costly calculations
            for (int toneIndex = 0; toneIndex < NumHalfTones; toneIndex++)
            {
                double freq = _BaseToneFreq * Math.Pow(_HalftoneBase, toneIndex);
                _SamplesPerPeriodPerTone[toneIndex] = (int)Math.Round(44100.0 / freq); // samples in one period
            }
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
        /// Minimum volume for a tone to be valid
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

            for (int i = 0; i < _AnalysisBufLen; i++)
                _AnalysisBuffer[i] = BitConverter.ToInt16(_AnalysisByteBuffer, i * 2);

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
            short maxVolume = 0;
            for (int i = 0; i < _AnalysisBuffer.Length / 4; i++)
            {
                short volume = _AnalysisBuffer[i];
                if (volume < 0)
                {
                    if (volume == Int16.MinValue)
                    {
                        maxVolume = Int16.MaxValue;
                        break;
                    }
                    volume = (short)-volume;
                }
                if (volume > maxVolume)
                    maxVolume = volume;
            }
            _MaxVolume = (double)maxVolume / Int16.MaxValue;
        }

        private void _AnalyzeTones()
        {
            // prepare to analyze
            double maxWeight = -1.0;
            double minWeight = 1.0;
            int maxTone = -1;

            // analyze halftones
            // Note: at the lowest tone (~65Hz) and a buffer-size of 4096
            // at 44.1 (or 48kHz) only 6 (or 5) samples are compared, this might be
            // too few samples -> use a bigger buffer-size
            for (int toneIndex = 0; toneIndex < NumHalfTones; toneIndex++)
            {
                double curWeight = _AnalyzeToneFunc(toneIndex);

                if (curWeight > maxWeight)
                {
                    maxWeight = curWeight;
                    maxTone = toneIndex;
                }

                if (curWeight < minWeight)
                    minWeight = curWeight;

                _TmpWeight[toneIndex] = (float)curWeight;
            }

            if (maxWeight - minWeight > 0.01)
            {
                Array.Copy(_TmpWeight, ToneWeigth, NumHalfTones);

                ToneAbs = maxTone;
                Tone = maxTone % 12;
                ToneValid = true;
            }
            else
                ToneValid = false;
        }

        private double _GetAutoCorrelation(int tone)
        {
            int samplesPerPeriod = _SamplesPerPeriodPerTone[tone]; // samples in one period

            double accumDist = 0; // accumulated distances

            // compare correlating samples
            int sampleIndex = 0; // index of sample to analyze
            // Start value= index of sample one period ahead
            for (int correlatingSampleIndex = sampleIndex + samplesPerPeriod; correlatingSampleIndex < _AnalysisBufLen; correlatingSampleIndex++, sampleIndex++)
            {
                // calc distance to corresponding sample in next period (lower means more distant)
                double dist = _AnalysisBuffer[sampleIndex] * _AnalysisBuffer[correlatingSampleIndex];
                accumDist += dist;
            }

            //Using _AnalysisBufLen here makes it return correct values among all analyzed frequencies
            const double scaleValue = (double)Int16.MaxValue * (double)Int16.MaxValue * _AnalysisBufLen;
            return accumDist / scaleValue;
        }

        private double _GetAmdf(int tone)
        {
            int samplesPerPeriod = _SamplesPerPeriodPerTone[tone]; // samples in one period

            int accumDist = 0; // accumulated distances

            // compare correlating samples
            int sampleIndex = 0; // index of sample to analyze
            // Start value= index of sample one period ahead
            for (int correlatingSampleIndex = sampleIndex + samplesPerPeriod; correlatingSampleIndex < _AnalysisBufLen; correlatingSampleIndex++, sampleIndex++)
            {
                // calc distance (correlation: 1-dist/IntMax*2) to corresponding sample in next period (0=equal .. IntMax*2=totally different)
                int dist = Math.Abs(_AnalysisBuffer[sampleIndex] - _AnalysisBuffer[correlatingSampleIndex]);
                accumDist += dist;
            }
            //Use analyzed sample count here. BufLen yields more errors
            return 1.0 - (double)accumDist / Int16.MaxValue / sampleIndex;
        }

        public void Dispose()
        {
            if (_Stream != null)
            {
                _Stream.Dispose();
                _Stream = null;
            }
            GC.SuppressFinalize(this);
        }

#if TEST_PITCH
        private static bool _PitchTestRun;

        private static string _ToneToNote(int tone, bool withOctave = true)
        {
            string[] notes = {"C", "C♯", "D", "Eb", "E", "F", "F♯", "G", "G♯", "A", "Bb", "B"};
            string result = notes[tone % 12];
            if (withOctave)
                result += (tone / 12) + 2;
            return result;
        }

        private void _TestPitchDetection()
        {
            if (_PitchTestRun)
                return;
            _PitchTestRun = true;
            const int toneFrom = 0;
            const int tontTo = 48;
            Console.WriteLine("Testing notes " + _ToneToNote(toneFrom) + " - " + _ToneToNote(tontTo));
            byte[] data;
            int ok = 0;
            for (int distort = 0; distort < 5; distort++)
            {
                for (int tone = toneFrom; tone <= tontTo; tone++)
                {
                    _GetSineWave(_BaseToneFreq * Math.Pow(_HalftoneBase, tone), 44100, out data);
                    for (int i = 0; i < distort; i++)
                        _Distort(data, tone + 2 + i * 2);

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
            for (int i = 0; i < 10000; i++)
            {
                ProcessNewBuffer(data);
                AnalyzeBuffer();
                // _FindMaxVolume();
                // _AnalyzeTones();
            }
            sw.Stop();
            string msg = "Analyser: Errors: " + ((tontTo - toneFrom + 1) * 5 - ok) + "; Speed: " +
                         (int)(10000.0 / (sw.ElapsedMilliseconds / 1000.0)) + " buffers/s";
            Console.WriteLine(msg);
            //MessageBox.Show(msg);
            Reset();
        }

        private static void _Distort(byte[] data, int tone)
        {
            short[] sdata = new short[data.Length / 2];
            Buffer.BlockCopy(data, 0, sdata, 0, data.Length);
            short[] sdata2 = new short[data.Length / 2];
            if (tone < 4)
            {
                byte[] data2;
                _GetSineWave(_BaseToneFreq * Math.Pow(_HalftoneBase, tone), 44100, out data2);
                Buffer.BlockCopy(data2, 0, sdata2, 0, data.Length);
            }
            else
            {
                Random r = new Random(0xBEEF);
                for (int i = 0; i < sdata.Length; i++)
                    sdata2[i] = (short)(r.Next() - Int16.MinValue);
            }
            for (int i = 0; i < sdata.Length; i++)
                sdata[i] = (short)(sdata[i] * 4.0 / 5.0 + sdata2[i] / 5.0);
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