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
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Vocaluxe.Base;
using Vocaluxe.Lib.Sound.Playback.Decoder;
using VocaluxeLib;

namespace Vocaluxe.Lib.Sound.Playback.PortAudio
{
    class CPortAudioStream : IDisposable
    {
        private const int _Bufsize = 1000000;
        private const int _Beginrefill = 800000;

        public readonly int ID;
        private readonly Closeproc _Closeproc;
        private CPortAudioHandle _PaHandle;

        private readonly CSyncTimer _SyncTimer;
        private int _ByteCount = 4;
        private float _Volume = 1f;
        private float _VolumeMax = 1f;

        private EStreamAction _AfterFadeAction;
        private CFading _Fading;

        private static PortAudioSharp.PortAudio.PaHostApiInfo _ApiInfo;
        private static PortAudioSharp.PortAudio.PaDeviceInfo _OutputDeviceInfo;
        private IntPtr _Stream = IntPtr.Zero;

        private PortAudioSharp.PortAudio.PaStreamCallbackDelegate _PaStreamCallback;
        private IAudioDecoder _Decoder;
        private float _BytesPerSecond;
        private float _Latency;
        private bool _NoMoreData;

        private bool _FileOpened;

        private bool _Skip;

        private bool _Loop;
        private float _Duration;
        private float _CurrentTime;
        private volatile float _TimeCode;

        private volatile bool _Paused;

        private CRingBuffer _Data;
        private volatile float _SetStart;
        private float _Start;
        private volatile bool _SetLoop;
        private volatile bool _SetSkip;
        private volatile bool _Terminated;

        private Thread _DecoderThread;

        private AutoResetEvent _EventDecode = new AutoResetEvent(false);

        private readonly Object _LockData = new Object();
        private readonly Object _LockSyncSignals = new Object();

        public CPortAudioStream(int id, Closeproc closeproc)
        {
            ID = id;
            _Closeproc = closeproc;
            _SyncTimer = new CSyncTimer(0f, 1f, 0.02f);
        }

        ~CPortAudioStream()
        {
            _Dispose(false);
            if (_DecoderThread != null)
                _DecoderThread.Join();
        }

        public void Close()
        {
            _Terminated = true;
            _EventDecode.Set();
        }

        public float Length
        {
            get
            {
                if (_FileOpened)
                    return _Duration;

                return 0f;
            }
        }

        public bool Finished
        {
            get
            {
                lock (_LockData)
                {
                    return _NoMoreData && _Data.BytesNotRead == 0 && _SyncTimer.Time >= _Duration;
                }
            }
        }

        public float Volume
        {
            get { return _Volume * 100f; }
            set
            {
                value.Clamp(0f, 100f);
                _Volume = value / 100f;
            }
        }

        public float VolumeMax
        {
            get { return _VolumeMax * 100f; }
            set
            {
                value.Clamp(0f, 100f);
                _VolumeMax = value / 100f;
            }
        }

        public float Position
        {
            get
            {
                lock (_LockData)
                {
                    if (Finished)
                        _SyncTimer.Pause();
                    //Decoder may return wrong timestamps. If you change this do exessive testing for monoton timestamps espacially for ogg files
                    return _SyncTimer.Time;
                }
            }
        }

        public bool Paused
        {
            get { return _Paused; }
            set
            {
                _Paused = value;
                lock (_LockData)
                {
                    if (_Paused)
                        _SyncTimer.Pause();
                    else
                    {
                        _SyncTimer.Resume();
                        _EventDecode.Set();
                    }
                }
            }
        }

        public void Fade(float targetVolume, float fadeTime)
        {
            targetVolume = targetVolume.Clamp(0f, 100f);
            _Fading = new CFading(_Volume, targetVolume / 100f, fadeTime);
            _AfterFadeAction = EStreamAction.Nothing;
        }

        public void FadeAndPause(float targetVolume, float fadeTime)
        {
            Fade(targetVolume, fadeTime);
            _AfterFadeAction = EStreamAction.Pause;
        }

        public void FadeAndClose(float targetVolume, float fadeTime)
        {
            Fade(targetVolume, fadeTime);
            _AfterFadeAction = EStreamAction.Close;
        }

        public void FadeAndStop(float targetVolume, float fadeTime)
        {
            Fade(targetVolume, fadeTime);
            _AfterFadeAction = EStreamAction.Stop;
        }

        public void Play()
        {
            Paused = false;
            _AfterFadeAction = EStreamAction.Nothing;
            _PaHandle.CheckError("StartStream", PortAudioSharp.PortAudio.Pa_StartStream(_Stream));
        }

        public void Stop()
        {
            _PaHandle.CheckError("StopStream (playback)", PortAudioSharp.PortAudio.Pa_StopStream(_Stream));
            Skip(0f);
        }

        public bool Loop
        {
            get { return _SetLoop; }
            set { _SetLoop = value; }
        }

        public bool Open(string fileName)
        {
            if (_FileOpened)
                return false;

            if (!File.Exists(fileName))
                return false;

            try
            {
                _PaHandle = new CPortAudioHandle();

                int hostApi = _PaHandle.GetHostApi();
                _ApiInfo = PortAudioSharp.PortAudio.Pa_GetHostApiInfo(hostApi);
                _OutputDeviceInfo = PortAudioSharp.PortAudio.Pa_GetDeviceInfo(_ApiInfo.defaultOutputDevice);
                if (_OutputDeviceInfo.defaultLowOutputLatency < 0.1)
                    _OutputDeviceInfo.defaultLowOutputLatency = 0.1;

                _PaStreamCallback = _ProcessNewData;
            }
            catch (Exception)
            {
                if (_PaHandle != null)
                {
                    _PaHandle.Close();
                    _PaHandle = null;
                }
                CLog.LogError("Error Init PortAudio Playback");
                return false;
            }

            _Decoder = new CAudioDecoderFFmpeg();
            _Decoder.Init();
            _Decoder.Open(fileName);

            SFormatInfo format = _Decoder.GetFormatInfo();
            if (format.SamplesPerSecond == 0)
            {
                _PaHandle.Close();
                _PaHandle = null;
                _Decoder.Close();
                _Decoder = null;
                CLog.LogError("Error Init PortAudio Playback (samples=0)");
                return false;
            }

            _Duration = _Decoder.GetLength();
            _ByteCount = 2 * format.ChannelCount;
            _BytesPerSecond = format.SamplesPerSecond * _ByteCount;
            _CurrentTime = 0f;
            _SyncTimer.Pause();
            _SyncTimer.Time = _CurrentTime;

            PortAudioSharp.PortAudio.PaStreamParameters? outputParams = new PortAudioSharp.PortAudio.PaStreamParameters
                {
                    channelCount = format.ChannelCount,
                    device = _ApiInfo.defaultOutputDevice,
                    sampleFormat = PortAudioSharp.PortAudio.PaSampleFormat.paInt16,
                    suggestedLatency = _OutputDeviceInfo.defaultLowOutputLatency,
                    hostApiSpecificStreamInfo = IntPtr.Zero
                };

            if (!_PaHandle.OpenOutputStream(
                out _Stream,
                ref outputParams,
                format.SamplesPerSecond,
                (uint)CConfig.AudioBufferSize,
                PortAudioSharp.PortAudio.PaStreamFlags.paNoFlag,
                _PaStreamCallback,
                IntPtr.Zero) || _Stream == IntPtr.Zero)
            {
                _PaHandle.Close();
                _PaHandle = null;
                _Decoder.Close();
                _Decoder = null;
                return false;
            }

            _Latency = CConfig.AudioLatency / 1000f + (float)PortAudioSharp.PortAudio.Pa_GetStreamInfo(_Stream).outputLatency;

            //From now on closing the driver and the decoder is handled by the thread ONLY!

            _Paused = true;
            _FileOpened = true;
            _Data = new CRingBuffer(_Bufsize);
            _NoMoreData = false;
            _DecoderThread = new Thread(_Execute) {Priority = ThreadPriority.Normal, Name = Path.GetFileName(fileName)};
            _DecoderThread.Start();

            return true;
        }

        public void Skip(float time)
        {
            lock (_LockSyncSignals)
            {
                _SetStart = time;
                _SetSkip = true;
                _EventDecode.Set();
            }
        }

        #region Threading
        private void _DoSkip()
        {
            lock (_LockData)
            {
                _Decoder.SetPosition(_Start);
                _CurrentTime = _Start;
                _TimeCode = _Start;
                _Data.Reset();
                _NoMoreData = false;
                _SyncTimer.Time = _Start;
            }
        }

        private void _Execute()
        {
            while (!_Terminated)
            {
                lock (_LockSyncSignals)
                {
                    if (_SetSkip)
                    {
                        _Skip = true;
                        _SetSkip = false;
                    }

                    _Start = _SetStart;
                    _Loop = _SetLoop;
                }

                if (_Skip)
                {
                    _DoSkip();
                    _Skip = false;
                }

                _DoDecode();
                _Update();
                _EventDecode.WaitOne();
            }

            _DoFree();
        }

        private void _DoDecode()
        {
            if (_Paused)
                return;

            if (_Terminated)
                return;

            float timecode;
            byte[] buffer;

            bool doIt = false;
            lock (_LockData)
            {
                if (!_Skip && _Beginrefill > _Data.BytesNotRead)
                    doIt = true;
            }

            if (!doIt)
                return;

            _Decoder.Decode(out buffer, out timecode);

            if (buffer == null)
            {
                if (_Loop)
                {
                    lock (_LockSyncSignals)
                    {
                        _CurrentTime = 0f;
                        _Start = 0f;
                    }

                    _DoSkip();
                }
                else
                    _NoMoreData = true;
                return;
            }

            lock (_LockData)
            {
                _Data.Write(buffer);
                _TimeCode = timecode;
                if (_Data.BytesNotRead < _Beginrefill)
                    _EventDecode.Set();
            }
        }

        private void _DoFree()
        {
            _PaHandle.CloseStream(_Stream);
            _PaHandle.Close();
            _PaHandle = null;
            _Decoder.Close();
            _Decoder = null;

            _Closeproc(ID);
        }
        #endregion Threading

        #region Callbacks
        private PortAudioSharp.PortAudio.PaStreamCallbackResult _ProcessNewData(
            IntPtr input,
            IntPtr output,
            uint frameCount,
            ref PortAudioSharp.PortAudio.PaStreamCallbackTimeInfo timeInfo,
            PortAudioSharp.PortAudio.PaStreamCallbackFlags statusFlags,
            IntPtr userData)
        {
            var buf = new byte[frameCount * _ByteCount];

            if (_Paused)
            {
                try
                {
                    Marshal.Copy(buf, 0, output, buf.Length);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                return PortAudioSharp.PortAudio.PaStreamCallbackResult.paContinue;
            }

            lock (_LockData)
            {
                if (_NoMoreData || _Data.BytesNotRead >= buf.Length)
                {
                    _Data.Read(buf);
                    //We want to scale all values. No matter how many channels we have (_ByteCount=2 or 4) we have short values
                    //So just process 2 bytes a time
                    for (int i = 0; i < buf.Length; i += 2)
                    {
                        byte[] b = BitConverter.GetBytes((Int16)(BitConverter.ToInt16(buf, i) * _Volume * _VolumeMax));
                        buf[i] = b[0];
                        buf[i + 1] = b[1];
                    }
                }

                if (_Data.BytesNotRead < _Beginrefill && !_NoMoreData)
                    _EventDecode.Set();

                float latency = buf.Length / _BytesPerSecond + _Latency;
                float time = _TimeCode - _Data.BytesNotRead / _BytesPerSecond - latency;

                if (!_NoMoreData)
                    _CurrentTime = _SyncTimer.Update(time);
            }

            try
            {
                Marshal.Copy(buf, 0, output, (int)frameCount * _ByteCount);
            }
            catch (Exception e)
            {
                CLog.LogError("Error PortAudio.StreamCallback: " + e.Message);
            }

            return PortAudioSharp.PortAudio.PaStreamCallbackResult.paContinue;
        }
        #endregion Callbacks

        private void _Update()
        {
            if (_Fading != null)
            {
                bool finished;
                _Volume = _Fading.GetValue(out finished);
                if (finished)
                {
                    switch (_AfterFadeAction)
                    {
                        case EStreamAction.Close:
                            _Terminated = true;
                            break;
                        case EStreamAction.Stop:
                            Stop();
                            break;
                        case EStreamAction.Pause:
                            Paused = true;
                            break;
                    }
                    _Fading = null;
                }
            }
        }

        public void Dispose()
        {
            _Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void _Dispose(bool disposing)
        {
            Close();
            if (disposing)
            {
                if (_DecoderThread != null)
                {
                    _DecoderThread.Join();
                    _DecoderThread = null;
                }
                if (_EventDecode != null)
                {
                    _EventDecode.Close();
                    _EventDecode = null;
                }
            }
        }
    }
}