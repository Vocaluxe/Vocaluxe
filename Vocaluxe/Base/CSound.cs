﻿#region license
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
            FadeAndStop(stream, 100f, length);
            return stream;
        }
        #endregion Sounds

        #region Record
        private static IRecord _Record;

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
        private const int _NumHalfTones = 47;

        private readonly float[] _ToneWeigth;
        private Int16[] _AnalysisBuffer = new Int16[4096];
        private readonly Object _AnalysisBufferLock = new Object();

        private bool _ToneValid;
        private int _Tone;
        private int _ToneAbs;
        private double _MaxVolume;
        private bool _NewSamples;

        private MemoryStream _Stream; // full buffer

        public CBuffer()
        {
            _ToneWeigth = new float[_NumHalfTones];
            for (int i = 0; i < _ToneWeigth.Length; i++)
                _ToneWeigth[i] = 0.99f;
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
                    var buf = new byte[len];
                    _Stream.Position -= len;
                    _Stream.Read(buf, 0, len);

                    var b = new byte[2];
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
                    float volume = Math.Abs((float)_AnalysisBuffer[i]) / Int16.MaxValue;
                    if (volume > MaxVolume)
                        _MaxVolume = volume;
                }

                _AnalyzeByAutocorrelation(_MaxVolume >= 0.02f);
            }
            catch (Exception) {}
        }

        private void _AnalyzeByAutocorrelation(bool valid)
        {
            if (!valid)
            {
                _ToneValid = false;
                return;
            }

            const double halftoneBase = 1.05946309436; // 2^(1/12) -> HalftoneBase^12 = 2 (one octave)

            // prepare to analyze
            double maxWeight = -1.0;
            double minWeight = 1.0;
            int maxTone = -1;
            var weigth = new float[_NumHalfTones];

            // analyze halftones
            // Note: at the lowest tone (~65Hz) and a buffer-size of 4096
            // at 44.1 (or 48kHz) only 6 (or 5) samples are compared, this might be
            // too few samples -> use a bigger buffer-size
            for (int toneIndex = 0; toneIndex < _NumHalfTones; toneIndex++)
            {
                double curFreq = _BaseToneFreq * Math.Pow(halftoneBase, toneIndex);
                double curWeight = _AnalyzeAutocorrelationFreq(curFreq);

                if (curWeight > maxWeight)
                {
                    maxWeight = curWeight;
                    maxTone = toneIndex;
                }

                if (curWeight < minWeight)
                    minWeight = curWeight;

                weigth[toneIndex] = (float)curWeight;
            }

            if (maxWeight - minWeight > 0.01)
            {
                for (int i = 0; i < weigth.Length; i++)
                    _ToneWeigth[i] = weigth[i];

                _ToneAbs = maxTone;
                _Tone = maxTone % 12;
                _ToneValid = true;
            }
            else
                _ToneValid = false;
        }

        private double _AnalyzeAutocorrelationFreq(double freq)
        {
            int sampleIndex = 0; // index of sample to analyze
            var samplesPerPeriod = (int)Math.Round(44100.0 / freq); // samples in one period
            int correlatingSampleIndex = sampleIndex + samplesPerPeriod; // index of sample one period ahead

            double accumDist = 0.0; // accumulated distances

            // compare correlating samples
            while (correlatingSampleIndex < _AnalysisBuffer.Length)
            {
                // calc distance (correlation: 1-dist) to corresponding sample in next period
                // distance (0=equal .. 1=totally different) between correlated samples
                double dist = Math.Abs((double)_AnalysisBuffer[sampleIndex] - _AnalysisBuffer[correlatingSampleIndex]) / Int16.MaxValue;
                accumDist += dist;
                sampleIndex++;
                correlatingSampleIndex++;
            }

            return 1 - accumDist / _AnalysisBuffer.Length;
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