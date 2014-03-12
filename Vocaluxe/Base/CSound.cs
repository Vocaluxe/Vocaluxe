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

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Vocaluxe.Lib.Sound;
using VocaluxeLib;

namespace Vocaluxe.Base
{
    enum ESounds
    {
        T440
    }

    static class CSound
    {
        #region Playback
        private static IPlayback _Playback;

        public static bool Init()
        {
            switch (CConfig.PlayBackLib)
            {
                case EPlaybackLib.PortAudio:
                    _Playback = new CPortAudioPlay();
                    break;

                case EPlaybackLib.OpenAL:
                    _Playback = new COpenALPlay();
                    break;

                case EPlaybackLib.Gstreamer:
                    _Playback = new CGstreamerAudio();
                    break;

                case EPlaybackLib.GstreamerSharp:
                    _Playback = new CGstreamerSharpAudio();
                    break;

                default:
                    _Playback = new CPortAudioPlay();
                    break;
            }
            return _Playback.Init();
        }

        public static void SetGlobalVolume(float volume)
        {
            _Playback.SetGlobalVolume(volume);
        }

        public static int GetStreamCount()
        {
            return _Playback.GetStreamCount();
        }

        public static void CloseAllStreams()
        {
            _Playback.CloseAll();
        }

        #region Stream Handling
        public static int Load(string media)
        {
            return _Playback.Load(media);
        }

        public static int Load(string media, bool prescan)
        {
            return _Playback.Load(media, prescan);
        }

        public static void Close(int stream)
        {
            _Playback.Close(stream);
        }

        public static void Play(int stream)
        {
            _Playback.Play(stream);
        }

        public static void Play(int stream, bool loop)
        {
            _Playback.Play(stream, loop);
        }

        public static void Pause(int stream)
        {
            _Playback.Pause(stream);
        }

        public static void Stop(int stream)
        {
            _Playback.Stop(stream);
        }

        public static void Fade(int stream, float targetVolume, float seconds)
        {
            _Playback.Fade(stream, targetVolume, seconds);
        }

        public static void FadeAndPause(int stream, float targetVolume, float seconds)
        {
            _Playback.FadeAndPause(stream, targetVolume, seconds);
        }

        public static void FadeAndClose(int stream, float targetVolume, float seconds)
        {
            _Playback.FadeAndClose(stream, targetVolume, seconds);
        }

        public static void FadeAndStop(int stream, float targetVolume, float seconds)
        {
            _Playback.FadeAndStop(stream, targetVolume, seconds);
        }

        public static void SetStreamVolume(int stream, float volume)
        {
            _Playback.SetStreamVolume(stream, volume);
        }

        public static void SetStreamVolumeMax(int stream, float volume)
        {
            _Playback.SetStreamVolumeMax(stream, volume);
        }

        public static float GetLength(int stream)
        {
            return _Playback.GetLength(stream);
        }

        public static float GetPosition(int stream)
        {
            return _Playback.GetPosition(stream);
        }

        public static bool IsPlaying(int stream)
        {
            return _Playback.IsPlaying(stream);
        }

        public static bool IsPaused(int stream)
        {
            return _Playback.IsPaused(stream);
        }

        public static bool IsFinished(int stream)
        {
            return _Playback.IsFinished(stream);
        }

        public static void Update()
        {
            _Playback.Update();
        }

        public static void SetPosition(int stream, float position)
        {
            _Playback.SetPosition(stream, position);
        }
        #endregion Stream Handling

        #endregion Playback

        #region Sounds
        public static int PlaySound(ESounds sound)
        {
            string file = Path.Combine(Environment.CurrentDirectory, CSettings.FolderSounds);
            switch (sound)
            {
                case ESounds.T440:
                    file = Path.Combine(file, CSettings.SoundT440);
                    break;
            }

            if (file == "")
                return -1;

            int stream = Load(file);
            float length = GetLength(stream);
            Play(stream);
            FadeAndClose(stream, 100f, length);
            return stream;
        }
        #endregion Sounds

        #region Record
        private static IRecord _Record;

        public static void RecordInit()
        {
            switch (CConfig.RecordLib)
            {
#if WIN
                case ERecordLib.DirectSound:
                    _Record = new CDirectSoundRecord();
                    break;
#endif

                    // case ERecordLib.PortAudio:
                default:
                    _Record = new CPortAudioRecord();
                    break;
            }
        }

        public static void RecordCloseAll()
        {
            _Record.CloseAll();
        }

        public static bool RecordStart()
        {
            return _Record.Start();
        }

        public static bool RecordStop()
        {
            return _Record.Stop();
        }

        public static void AnalyzeBuffer(int player)
        {
            _Record.AnalyzeBuffer(player);
        }

        public static int RecordGetToneAbs(int player)
        {
            return _Record.GetToneAbs(player);
        }

        public static int RecordGetTone(int player)
        {
            return _Record.GetTone(player);
        }

        public static void RecordSetTone(int player, int tone)
        {
            _Record.SetTone(player, tone);
        }

        public static bool RecordToneValid(int player)
        {
            return _Record.ToneValid(player);
        }

        public static float RecordGetMaxVolume(int player)
        {
            return _Record.GetMaxVolume(player);
        }

        public static int NumHalfTones(int player)
        {
            return _Record.NumHalfTones(player);
        }

        public static float[] ToneWeigth(int player)
        {
            return _Record.ToneWeigth(player);
        }

        public static ReadOnlyCollection<CRecordDevice> RecordGetDevices()
        {
            ReadOnlyCollection<CRecordDevice> devices = _Record.RecordDevices();

            if (devices != null)
            {
                foreach (CRecordDevice device in devices)
                {
                    device.PlayerChannel1 = _GetPlayerFromMicConfig(device.Name, device.Driver, 1);
                    device.PlayerChannel2 = _GetPlayerFromMicConfig(device.Name, device.Driver, 2);
                }
                return devices;
            }

            return null;
        }

        private static int _GetPlayerFromMicConfig(string device, string devicedriver, int channel)
        {
            for (int p = 0; p < CSettings.MaxNumPlayer; p++)
            {
                if (CConfig.MicConfig[p].Channel != 0 &&
                    CConfig.MicConfig[p].DeviceName == device &&
                    CConfig.MicConfig[p].DeviceDriver == devicedriver &&
                    CConfig.MicConfig[p].Channel == channel)
                    return p + 1;
            }
            return 0;
        }
        #endregion Record
    }

    class CBuffer : IDisposable
    {
        private const double _BaseToneFreq = 65.4064;
        public const int NumHalfTones = 17;

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

            return 1.0 - (double)accumDist / Int16.MaxValue / _AnalysisBufLen;
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

    class CSyncTimer
    {
        private readonly CPt1 _ExternTime;
        private readonly Stopwatch _Timer;
        private float _SetValue;

        public float Time
        {
            get
            {
                double nanosecPerTick = (1000.0 * 1000.0 * 1000.0) / Stopwatch.Frequency;
                long ticks = _Timer.ElapsedTicks;
                float dt = _Timer.ElapsedMilliseconds / 1000f;

                if (Stopwatch.IsHighResolution && ticks != 0)
                    dt = (float)(ticks * nanosecPerTick / 1000000000.0);

                return _SetValue + dt;
            }

            set
            {
                _ExternTime.Time = value;
                _SetValue = value;
                _Timer.Reset();
                _Timer.Start();
            }
        }

        public CSyncTimer(float currentTime, float k, float t)
        {
            _ExternTime = new CPt1(currentTime, k, t);
            _Timer = new Stopwatch();
            _SetValue = currentTime;
        }

        public float Update(float newTime)
        {
            float et = _ExternTime.Update(newTime);

            float dt = Time;

            float diff = et - dt;
            if (Math.Abs(diff) > 0.05f)
            {
                _Timer.Reset();
                _Timer.Start();
                _SetValue = dt + diff;
                dt = _SetValue;
                //Console.WriteLine("DRIFTED!!! " + diff.ToString());
            }
            else
            {
                if (diff > 0.01f)
                    _SetValue += 0.000025f;

                if (diff < -0.01f)
                    _SetValue -= 0.000025f;
            }
            //Console.WriteLine(diff.ToString());
            return dt;
        }

        public void Pause()
        {
            _ExternTime.Pause();
            _Timer.Stop();
        }

        public void Resume()
        {
            _ExternTime.Resume();
            _Timer.Start();
        }
    }

    class CPt1
    {
        private float _CurrentTime;
        private float _OldTime;

        private readonly Stopwatch _Timer;
        private readonly float _K;
        private readonly float _T;

        public float Time
        {
            get
            {
                double nanosecPerTick = (1000.0 * 1000.0 * 1000.0) / Stopwatch.Frequency;
                long ticks = _Timer.ElapsedTicks;
                float dt = _Timer.ElapsedMilliseconds / 1000f;

                if (Stopwatch.IsHighResolution && ticks != 0)
                    dt = (float)(ticks * nanosecPerTick / 1000000000.0);

                return _CurrentTime + dt;
            }

            set
            {
                if (value < 0f)
                    return;

                _CurrentTime = value;
                _OldTime = value;
                _Timer.Reset();
                _Timer.Start();
            }
        }

        public CPt1(float currentTime, float k, float t)
        {
            _CurrentTime = currentTime;
            _OldTime = currentTime;

            _Timer = new Stopwatch();
            _K = k;
            _T = t;
        }

        public float Update(float newTime)
        {
            _Timer.Stop();

            double nanosecPerTick = (1000.0 * 1000.0 * 1000.0) / Stopwatch.Frequency;
            long ticks = _Timer.ElapsedTicks;
            float dt = _Timer.ElapsedMilliseconds / 1000f;

            if (Stopwatch.IsHighResolution && ticks != 0)
                dt = (float)(ticks * nanosecPerTick / 1000000000.0);

            float ts = 0f;
            if (dt > 0)
                ts = 1 / (_T / dt + 1);

            _CurrentTime = ts * (_K * newTime - _OldTime) + _OldTime;
            _OldTime = _CurrentTime;

            _Timer.Reset();
            _Timer.Start();

            return _CurrentTime;
        }

        public void Pause()
        {
            _Timer.Stop();
        }

        public void Resume()
        {
            _Timer.Start();
        }
    }
}