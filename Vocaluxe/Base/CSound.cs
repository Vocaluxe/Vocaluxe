using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using Vocaluxe.Lib.Sound;
using Vocaluxe.Menu;

namespace Vocaluxe.Base
{
    enum ESounds
    {
        T440
    }

    static class CSound
    {
        #region Playback
        private static IPlayback _Playback = null;

        public static bool PlaybackInit()
        {
            switch (CConfig.PlayBackLib)
            {
                case EPlaybackLib.PortAudio:
                    _Playback = new CPortAudioPlay();
                    break;

                case EPlaybackLib.OpenAL:
                    _Playback = new COpenALPlay();
                    break;

                default:
                    _Playback = new CPortAudioPlay();
                    break;
            }
            return true;
        }

        public static void SetGlobalVolume(float Volume)
        {
            _Playback.SetGlobalVolume(Volume);
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
        public static int Load(string Media)
        {
            return _Playback.Load(Media);
        }

        public static int Load(string Media, bool Prescan)
        {
            return _Playback.Load(Media, Prescan);
        }

        public static void Close(int Stream)
        {
            _Playback.Close(Stream);
        }

        public static void Play(int Stream)
        {
            _Playback.Play(Stream);
        }

        public static void Play(int Stream, bool Loop)
        {
            _Playback.Play(Stream, Loop);
        }

        public static void Pause(int Stream)
        {
            _Playback.Pause(Stream);
        }

        public static void Stop(int Stream)
        {
            _Playback.Stop(Stream);
        }

        public static void Fade(int Stream, float TargetVolume, float Seconds)
        {
            _Playback.Fade(Stream, TargetVolume, Seconds);
        }

        public static void FadeAndPause(int Stream, float TargetVolume, float Seconds)
        {
            _Playback.FadeAndPause(Stream, TargetVolume, Seconds);
        }

        public static void FadeAndStop(int Stream, float TargetVolume, float Seconds)
        {
            _Playback.FadeAndStop(Stream, TargetVolume, Seconds);
        }

        public static void SetStreamVolume(int Stream, float Volume)
        {
            _Playback.SetStreamVolume(Stream, Volume);
        }

        public static void SetStreamVolumeMax(int Stream, float Volume)
        {
            _Playback.SetStreamVolumeMax(Stream, Volume);
        }

        public static float GetLength(int Stream)
        {
            return _Playback.GetLength(Stream);
        }

        public static float GetPosition(int Stream)
        {
            return _Playback.GetPosition(Stream);
        }

        public static bool IsPlaying(int Stream)
        {
            return _Playback.IsPlaying(Stream);
        }

        public static bool IsPaused(int Stream)
        {
            return _Playback.IsPaused(Stream);
        }

        public static bool IsFinished(int Stream)
        {
            return _Playback.IsFinished(Stream);
        }

        public static void Update()
        {
            _Playback.Update();
        }

        public static void SetPosition(int Stream, float Position)
        {
            _Playback.SetPosition(Stream, Position);
        }

        #endregion Stream Handling
        #endregion Playback

        #region Sounds
        public static int PlaySound(ESounds Sound)
        {
            string file = Path.Combine(Environment.CurrentDirectory, CSettings.sFolderSounds);
            switch (Sound)
            {
                case ESounds.T440:
                    file = Path.Combine(file, CSettings.sSoundT440);
                    break;
                default:
                    break;
            }

            if (file == String.Empty)
                return -1;

            int stream = CSound.Load(file);
            float length = CSound.GetLength(stream);
            CSound.Play(stream);
            CSound.FadeAndStop(stream, 100f, length);
            return stream;
        }
        #endregion Sounds

        #region Record
        private static IRecord _Record = null;

        public static bool RecordInit()
        {
            switch (CConfig.RecordLib)
            {
                case ERecordLib.PortAudio:
                    _Record = new CPortAudioRecord();
                    break;

#if WIN
                case ERecordLib.DirectSound:
                    _Record = new CDirectSoundRecord();
                    break;
#endif

                default:
                    _Record = new CPortAudioRecord();
                    break;
            }
            
            return true;
        }

        public static void RecordCloseAll()
        {
            _Record.CloseAll();
        }

        public static bool RecordStart()
        {
            SRecordDevice[] devices = RecordGetDevices();
            
            return _Record.Start(devices);
        }

        public static bool RecordStop()
        {
            return _Record.Stop();
        }

        public static void AnalyzeBuffer(int Player)
        {
            _Record.AnalyzeBuffer(Player);
        }

        public static int RecordGetToneAbs(int Player)
        {
            return _Record.GetToneAbs(Player);
        }

        public static int RecordGetTone(int Player)
        {
            return _Record.GetTone(Player);
        }

        public static void RecordSetTone(int Player, int Tone)
        {
            _Record.SetTone(Player, Tone);
        }

        public static bool RecordToneValid(int Player)
        {
            return _Record.ToneValid(Player);
        }

        public static float RecordGetMaxVolume(int Player)
        {
            return _Record.GetMaxVolume(Player);
        }

        public static int NumHalfTones(int Player)
        {
            return _Record.NumHalfTones(Player);
        }

        public static float[] ToneWeigth(int Player)
        {
            return _Record.ToneWeigth(Player);
        }

        public static SRecordDevice[] RecordGetDevices()
        {
            SRecordDevice[] devices = _Record.RecordDevices();
			
			if (devices != null)
			{
	            for (int dev = 0; dev < devices.Length; dev++)
	            {
	                for (int inp = 0; inp < devices[dev].Inputs.Count; inp++)
	                {
	                    SInput input = devices[dev].Inputs[inp];
	
	                    input.PlayerChannel1 = GetPlayerFromMicConfig(devices[dev].Name, devices[dev].Driver, input.Name, 1);
	                    input.PlayerChannel2 = GetPlayerFromMicConfig(devices[dev].Name, devices[dev].Driver, input.Name, 2);
	
	                    devices[dev].Inputs[inp] = input;
	                }
	            }
				return devices;
			}

            return null;
        }

        private static int GetPlayerFromMicConfig(string device, string devicedriver, string input, int channel)
        {
            for (int p = 0; p < CSettings.MaxNumPlayer; p++)
            {
                if (CConfig.MicConfig[p].Channel != 0 &&
                    CConfig.MicConfig[p].DeviceName == device &&
                    CConfig.MicConfig[p].DeviceDriver == devicedriver &&
                    CConfig.MicConfig[p].InputName == input &&
                    CConfig.MicConfig[p].Channel == channel)
                {
                    return p + 1;
                }
            }
            return 0;
        }
        #endregion Record
    }

    class CBuffer
    {
        private const double _BaseToneFreq = 65.4064;
        private const int _NumHalfTones = 47;

        private float[] _ToneWeigth;
        private Int16[] _AnalysisBuffer = new Int16[4096];
        private Object _AnalysisBufferLock = new Object();

        private bool _ToneValid = false;
        private int _Tone = 0;
        private int _ToneAbs = 0;
        private double _MaxVolume = 0.0;
        private bool _NewSamples;

        private MemoryStream _Stream;                       // full buffer

        public CBuffer()
        {
            _ToneWeigth = new float[_NumHalfTones];
            for (int i = 0; i < _ToneWeigth.Length; i++)
            {
                _ToneWeigth[i] = 0.99f;
            }
            _Stream = new MemoryStream();
            _NewSamples = false;
        }

        public int NumHalfTones
        {
            get { return _NumHalfTones; }
        }

        public int ToneAbs
        {
            get 
            {
                lock (_AnalysisBufferLock)
                {
                    return _ToneAbs; 
                } 
            }
        }

        public int Tone
        {
            get
            {
                lock (_AnalysisBufferLock)
                {
                    return _Tone;
                }
            }
            set
            {
                lock (_AnalysisBufferLock)
                {
                    _Tone = value;
                }
            }
        }

        public float MaxVolume
        {
            get
            {
                lock (_AnalysisBufferLock)
                {
                    return (float)_MaxVolume;
                }
            }
        }

        public bool ToneValid
        {
            get
            {
                lock (_AnalysisBufferLock)
                {
                    return _ToneValid;
                }
            }
        }

        public long Length
        {
            get { return _Stream.Length; }
        }

        public byte[] Buffer
        {
            get { return _Stream.ToArray(); }
        }

        public float[] ToneWeigth
        {
            get
            {
                lock (_AnalysisBufferLock)
                {
                    return _ToneWeigth;
                }
            }
        }

        public void Reset()
        {
            _Stream.SetLength(0L);
            _AnalysisBuffer = new Int16[_AnalysisBuffer.Length];
            _ToneValid = false;
            _ToneAbs = 0;
            _Tone = 0;
            _NewSamples = false;
        }

        public void Add(byte[] bytes)
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

            lock (_AnalysisBufferLock)
            {
                Add(buffer);
                _NewSamples = true;
            }
        }

        public void AnalyzeBuffer()
        {
            if (!_NewSamples)
                return;

            lock (_AnalysisBufferLock)
            {
                int len = _AnalysisBuffer.Length * 2;
                if (_Stream.Length >= len)
                {
                    byte[] buf = new byte[len];
                    _Stream.Position -= len;
                    _Stream.Read(buf, 0, len);

                    byte[] b = new byte[2];
                    for (int i = 0; i < _AnalysisBuffer.Length; i++)
                    {
                        b[0] = buf[i * 2];
                        b[1] = buf[i * 2 + 1];

                        _AnalysisBuffer[i] = BitConverter.ToInt16(b, 0);
                    }
                }
                _NewSamples = false;
            }

            try
            {
                // find maximum volume
                _MaxVolume = 0;
                for (int i = 0; i < _AnalysisBuffer.Length / 4; i++)
                {
                    float Volume = Math.Abs((float)_AnalysisBuffer[i]) / (float)Int16.MaxValue;
                    if (Volume > MaxVolume)
                        _MaxVolume = Volume;
                }

                if (_MaxVolume >= 0.02f)
                    AnalyzeByAutocorrelation(true);
                else
                    AnalyzeByAutocorrelation(false);

            }
            catch (Exception)
            {

            }
        }

        private void AnalyzeByAutocorrelation(bool valid)
        {
            const double HalftoneBase = 1.05946309436; // 2^(1/12) -> HalftoneBase^12 = 2 (one octave)

            // prepare to analyze
            double MaxWeight = -1.0;
            double MinWeight = 1.0;
            int MaxTone = -1;
            float[] Weigth = new float[_NumHalfTones];

            // analyze halftones
            // Note: at the lowest tone (~65Hz) and a buffer-size of 4096
            // at 44.1 (or 48kHz) only 6 (or 5) samples are compared, this might be
            // too few samples -> use a bigger buffer-size

            for (int ToneIndex = 0; ToneIndex < _NumHalfTones; ToneIndex++)
            {
                double CurFreq = _BaseToneFreq * Math.Pow(HalftoneBase, ToneIndex);
                double CurWeight = AnalyzeAutocorrelationFreq(CurFreq);

                if (CurWeight > MaxWeight)
                {
                    MaxWeight = CurWeight;
                    MaxTone = ToneIndex;
                }

                if (CurWeight < MinWeight)
                    MinWeight = CurWeight;

                Weigth[ToneIndex] = (float)CurWeight;
            }

            if (valid && MaxWeight - MinWeight > 0.01)
            {
                for (int i = 0; i < Weigth.Length; i++)
                {
                    _ToneWeigth[i] = Weigth[i];
                }

                _ToneAbs = MaxTone;
                _Tone = MaxTone % 12;
                _ToneValid = true;
            }
            else
                _ToneValid = false;
        }

        private double AnalyzeAutocorrelationFreq(double Freq)
        {
            int SampleIndex = 0;                                            // index of sample to analyze
            int SamplesPerPeriod = (int)Math.Round(44100.0 / Freq);         // samples in one period
            int CorrelatingSampleIndex = SampleIndex + SamplesPerPeriod;    // index of sample one period ahead

            double AccumDist = 0.0;                                         // accumulated distances

            // compare correlating samples
            while (CorrelatingSampleIndex < _AnalysisBuffer.Length)
            {
                // calc distance (correlation: 1-dist) to corresponding sample in next period
                // distance (0=equal .. 1=totally different) between correlated samples
                double Dist = Math.Abs((double)_AnalysisBuffer[SampleIndex] - _AnalysisBuffer[CorrelatingSampleIndex]) / (double)Int16.MaxValue;
                AccumDist += Dist;
                SampleIndex++;
                CorrelatingSampleIndex++;
            }

            return 1 - AccumDist / _AnalysisBuffer.Length;
        }
    }

    class CSyncTimer
    {
        private PT1 _ExternTime;
        private Stopwatch _Timer;
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

        public CSyncTimer(float CurrentTime, float K, float T)
        {
            _ExternTime = new PT1(CurrentTime, K, T);
            _Timer = new Stopwatch();
            _SetValue = CurrentTime;
        }

        public float Update(float NewTime)
        {
            float et = _ExternTime.Update(NewTime);

            float dt = Time;

            float diff = _ExternTime.Time - dt;
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

    class PT1
    {
        private float _CurrentTime;
        private float _OldTime;

        private Stopwatch _STimer;
        private float _K;
        private float _T;

        public float Time
        {
            get
            {
                double nanosecPerTick = (1000.0 * 1000.0 * 1000.0) / Stopwatch.Frequency;
                long ticks = _STimer.ElapsedTicks;
                float dt = _STimer.ElapsedMilliseconds / 1000f;

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
                _STimer.Reset();
                _STimer.Start();
            }
        }

        public PT1(float CurrentTime, float K, float T)
        {
            _CurrentTime = CurrentTime;
            _OldTime = CurrentTime;

            _STimer = new Stopwatch();
            _K = K;
            _T = T;
        }

        public float Update(float NewTime)
        {
            _STimer.Stop();

            double nanosecPerTick = (1000.0 * 1000.0 * 1000.0) / Stopwatch.Frequency;
            long ticks = _STimer.ElapsedTicks;
            float dt = _STimer.ElapsedMilliseconds / 1000f;

            if (Stopwatch.IsHighResolution && ticks != 0)
                dt = (float)(ticks * nanosecPerTick / 1000000000.0);
                        
            float Ts = 0f;
            if (dt > 0)
                Ts = 1 / (_T / dt + 1);

            _CurrentTime = Ts * (_K * NewTime - _OldTime) + _OldTime;
            _OldTime = _CurrentTime;

            _STimer.Reset();
            _STimer.Start();

            return _CurrentTime;
        }

        public void Pause()
        {
            _STimer.Stop();
        }

        public void Resume()
        {
            _STimer.Start();
        }
    }
}
