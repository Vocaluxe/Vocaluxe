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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Vocaluxe.Base;
using Vocaluxe.Lib.Sound.Playback.Decoder;

namespace Vocaluxe.Lib.Sound.Playback.PortAudio
{
    class CPortAudioStream : CAudioStreamBase
    {
        private const int _Bufsize = 1000000;
        private const int _BeginRefill = 800000;

        private readonly CSyncTimer _SyncTimer = new CSyncTimer(0f, 1f, 0.02f);
        private int _ByteCount;

        private CPortAudioHandle _PaHandle;
        private PortAudioSharp.PortAudio.PaHostApiInfo _ApiInfo;
        private PortAudioSharp.PortAudio.PaDeviceInfo _OutputDeviceInfo;
        private IntPtr _Stream = IntPtr.Zero;

        private PortAudioSharp.PortAudio.PaStreamCallbackDelegate _PaStreamCallback;
        private IAudioDecoder _Decoder;
        private float _BytesPerSecond;
        private float _Latency;
        private bool _NoMoreData;

        private bool _FileOpened;

        private bool _Skip;

        private volatile float _TimeCode;

        private volatile bool _Paused;

        private CRingBuffer _Data;
        private volatile float _SetStart;
        private float _Start;
        private volatile bool _SetSkip;
        private volatile bool _Terminated;

        private Thread _DecoderThread;

        private AutoResetEvent _EventDecode = new AutoResetEvent(false);

        private readonly Object _LockData = new Object();
        private readonly Object _LockSyncSignals = new Object();

        public override float Position
        {
            get
            {
                lock (_LockData)
                {
                    // Decoder may return wrong timestamps. This is why we use the synctimer
                    // If you change this do exessive testing for monoton timestamps espacially for ogg files
                    float time = Math.Max(0f, _SyncTimer.Time);
                    if (time > Length)
                    {
                        _SyncTimer.Pause();
                        time = Length;
                    }
                    return time;
                }
            }
            set
            {
                lock (_LockSyncSignals)
                {
                    _SetStart = value;
                    _SetSkip = true;
                    _EventDecode.Set();
                }
            }
        }

        public override bool IsPaused
        {
            get { return _Paused; }
            set
            {
                if (_Paused == value)
                    return;
                if (!_FileOpened || _Terminated)
                    return;
                _Paused = value;
                lock (_LockData)
                {
                    if (_Paused)
                    {
                        _PaHandle.CheckError("StopStream (playback)", PortAudioSharp.PortAudio.Pa_StopStream(_Stream));
                        _SyncTimer.Pause();
                    }
                    else
                    {
                        _PaHandle.CheckError("StartStream", PortAudioSharp.PortAudio.Pa_StartStream(_Stream));
                        _SyncTimer.Resume();
                        _EventDecode.Set();
                    }
                }
            }
        }

        public override bool IsFinished
        {
            get
            {
                lock (_LockData)
                {
                    return _NoMoreData && _Data.BytesNotRead == 0 && _SyncTimer.Time >= Length;
                }
            }
        }

        public CPortAudioStream(int id, string medium, bool loop, EAudioEffect effect = EAudioEffect.None) : base(id, medium, loop, effect) {}

        public override bool Open(bool prescan)
        {
            Debug.Assert(!_FileOpened);
            if (_FileOpened)
                return false;

            if (!File.Exists(_Medium))
            {
                Dispose();
                return false;
            }

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
                Dispose();
                CLog.LogError("Error Init PortAudio Playback");
                return false;
            }

            _Decoder = new CAudioDecoderFFmpeg();
            if (!_Decoder.Open(_Medium))
            {
                Dispose();
                CLog.LogError("Error opening audio file: " + _Medium);
                return false;
            }

            SFormatInfo format = _Decoder.GetFormatInfo();
            if (format.SamplesPerSecond == 0)
            {
                Dispose();
                CLog.LogError("Error Init PortAudio Playback (samples=0)");
                return false;
            }

            Length = _Decoder.GetLength();
            _ByteCount = 2 * format.ChannelCount;
            _BytesPerSecond = format.SamplesPerSecond * _ByteCount;
            _SyncTimer.Pause();
            _SyncTimer.Time = 0f;

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
                (uint)CConfig.Config.Sound.AudioBufferSize / 2,
                PortAudioSharp.PortAudio.PaStreamFlags.paNoFlag,
                _PaStreamCallback,
                IntPtr.Zero) || _Stream == IntPtr.Zero)
            {
                Dispose();
                return false;
            }

            _Latency = CConfig.Config.Sound.AudioLatency / 1000f + (float)PortAudioSharp.PortAudio.Pa_GetStreamInfo(_Stream).outputLatency;

            //From now on closing the driver and the decoder is handled by the thread ONLY!

            _Paused = true;
            _FileOpened = true;
            _Data = new CRingBuffer(_Bufsize);
            _NoMoreData = false;
            _DecoderThread = new Thread(_Execute) {Priority = ThreadPriority.Normal, Name = Path.GetFileName(_Medium)};
            _DecoderThread.Start();

            return true;
        }

        protected override void _Dispose(bool disposing)
        {
            base._Dispose(disposing);
            if (!_Terminated)
            {
                _Terminated = true;
                if (_DecoderThread != null)
                    _EventDecode.Set();
                else
                    _DoFree();
            }
        }

        public override void Play()
        {
            if (!_FileOpened || _Terminated || !IsPaused)
                return;
            IsPaused = false;
        }

        public override void Stop()
        {
            if (!_FileOpened || _Terminated)
                return;
            IsPaused = true;
            Position = 0f;
        }

        #region Threading
        private void _DoSkip()
        {
            _Decoder.SetPosition(_Start);
            lock (_LockData)
            {
                _SyncTimer.Time = _Start;
                _TimeCode = _Start;
                _Data.Reset();
                _NoMoreData = false;
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
                }

                if (_Skip)
                {
                    _DoSkip();
                    _Skip = false;
                }

                _DoDecode();
                _EventDecode.WaitOne();
            }

            _DoFree();
        }

        private void _DoDecode()
        {
            if (_Paused || _Terminated || _NoMoreData)
                return;

            float timecode;
            byte[] buffer;

            lock (_LockData)
            {
                if (_Data.BytesNotRead > _BeginRefill)
                    return;
            }

            _Decoder.Decode(out buffer, out timecode);

            if (buffer == null)
            {
                if (_Loop)
                {
                    _Start = 0f;
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
                if (_Data.BytesNotRead < _BeginRefill)
                    _EventDecode.Set();
            }
        }

        private void _DoFree()
        {
            if (_PaHandle != null)
            {
                if (_Stream != IntPtr.Zero)
                    _PaHandle.CloseStream(_Stream);
                _PaHandle.Close();
                _PaHandle = null;
            }
            if (_DecoderThread != null)
            {
                if (Thread.CurrentThread.ManagedThreadId != _DecoderThread.ManagedThreadId)
                    throw new Exception("Another thread should never free the decoder thread!");
                _DecoderThread = null;
            }
            if (_Decoder != null)
            {
                _Decoder.Close();
                _Decoder = null;
            }
            if (_EventDecode != null)
            {
                _EventDecode.Close();
                _EventDecode = null;
            }
            if (_CloseStreamListener != null)
                _CloseStreamListener.OnCloseStream(this);
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
                int dataLen = _Data.BytesNotRead;
                if ((_NoMoreData && dataLen > 0) || dataLen >= buf.Length)
                {
                    dataLen = Math.Min(dataLen, buf.Length);
                    _Data.Read(buf);
                    //We want to scale all values. No matter how many channels we have (_ByteCount=2 or 4) we have short values
                    //So just process 2 bytes a time
                    float volume = Volume * VolumeMax;
                    for (int i = 0; i < dataLen; i += 2)
                    {
                        byte[] b = BitConverter.GetBytes((Int16)(BitConverter.ToInt16(buf, i) * volume));
                        buf[i] = b[0];
                        buf[i + 1] = b[1];
                    }
                    float latency = buf.Length / _BytesPerSecond + _Latency;
                    float time = _TimeCode - _Data.BytesNotRead / _BytesPerSecond - latency;
                    _SyncTimer.Update(time);
                }

                if (_Data.BytesNotRead < _BeginRefill && !_NoMoreData)
                    _EventDecode.Set();
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
    }
}