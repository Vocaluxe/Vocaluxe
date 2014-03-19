using System;
using System.IO;

namespace Vocaluxe.Lib.Sound.Record
{
    class CBuffer : IDisposable
    {
        //Half tones: C C♯ D Eb E F F♯ G G♯ A Bb B
        private const double _BaseToneFreq = 65.4064; // lowest (half-)tone to analyze (C2 = 65.4064 Hz)
        public const int NumHalfTones = 17; //Number of halftone to analyze C2-E3 (TODO: use more/full octaves?)

        private const int _AnalysisBufLen = 4096;
        private const int _AnalysisByteBufLen = _AnalysisBufLen * 2;

        private readonly Int16[] _AnalysisBuffer = new Int16[_AnalysisBufLen];
        private readonly byte[] _AnalysisByteBuffer = new byte[_AnalysisByteBufLen]; //tmp buffer (stream->tmpBuffer->(Int16)AnalysisBuffer)

        private static readonly int[] _SamplesPerPeriodPerTone = new int[NumHalfTones]; //Precalculated table holding sample# per period for each tone
        private readonly float[] _TmpWeight = new float[NumHalfTones]; //tmp buffer for weights (gets copied to ToneWeight when checked)

        private double _MaxVolume;
        private bool _NewSamples;

        private MemoryStream _Stream = new MemoryStream(); // full buffer

        public CBuffer()
        {
            ToneWeigth = new float[NumHalfTones];
            for (int i = 0; i < ToneWeigth.Length; i++)
                ToneWeigth[i] = 0.99f;
        }

        static CBuffer()
        {
            const double halftoneBase = 1.05946309436; // 2^(1/12) -> HalftoneBase^12 = 2 (one octave)
            //Init Array to avoid costly calculations
            for (int toneIndex = 0; toneIndex < NumHalfTones; toneIndex++)
            {
                double freq = _BaseToneFreq * Math.Pow(halftoneBase, toneIndex);
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

                if (MaxVolume >= 0.02)
                    _AnalyzeByAutocorrelation();
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

        private void _AnalyzeByAutocorrelation()
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
                double curWeight = _AnalyzeAutocorrelationTone(toneIndex);

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

        private double _AnalyzeAutocorrelationTone(int tone)
        {
            int samplesPerPeriod = _SamplesPerPeriodPerTone[tone]; // samples in one period

            int accumDist = 0; // accumulated distances

            // compare correlating samples
            int sampleIndex = 0; // index of sample to analyze
            // Start value= index of sample one period ahead
            for (int correlatingSampleIndex = sampleIndex + samplesPerPeriod; correlatingSampleIndex < _AnalysisBufLen; correlatingSampleIndex++, sampleIndex++)
            {
                // calc distance (correlation: 1-dist/IntMax) to corresponding sample in next period
                // distance (0=equal .. IntMax=totally different) between correlated samples
                int dist = Math.Abs(_AnalysisBuffer[sampleIndex] - _AnalysisBuffer[correlatingSampleIndex]);
                accumDist += dist;
            }

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
    }
}