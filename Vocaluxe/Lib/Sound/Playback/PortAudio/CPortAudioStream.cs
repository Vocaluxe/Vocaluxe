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
        private const long _Bufsize = 1000000L;
        private const long _Beginrefill = 800000L;

        private readonly CSyncTimer _SyncTimer;
        private static bool _Initialized;
        private static int _NumStreams;
        private static readonly Object _Mutex = new object();
        private int _ByteCount = 4;
        private float _Volume = 1f;
        private float _VolumeMax = 1f;

        private EStreamAction _AfterFadeAction;
        private CFading _Fading;

        private static PortAudioSharp.PortAudio.PaHostApiInfo _ApiInfo;
        private static PortAudioSharp.PortAudio.PaDeviceInfo _OutputDeviceInfo;
        private IntPtr _Ptr = new IntPtr(0);

        private Closeproc _Closeproc;
        private PortAudioSharp.PortAudio.PaStreamCallbackDelegate _PaStreamCallback;
        private int _StreamID;
        private IAudioDecoder _Decoder;
        private float _BytesPerSecond;
        private bool _NoMoreData;

        private bool _FileOpened;

        private bool _Waiting;
        private bool _Skip;

        private bool _Loop;
        private float _Duration;
        private float _CurrentTime;
        private float _TimeCode;

        private bool _Paused;

        private CRingBuffer _Data;
        private float _SetStart;
        private float _Start;
        private bool _SetLoop;
        private bool _SetSkip;
        private bool _Terminated;

        private Thread _DecoderThread;

        private AutoResetEvent _EventDecode = new AutoResetEvent(false);

        private readonly Object _LockData = new Object();
        private readonly Object _LockSyncSignals = new Object();

        public CPortAudioStream()
        {
            _SyncTimer = new CSyncTimer(0f, 1f, 0.02f);
        }

        ~CPortAudioStream()
        {
            Dispose();
        }

        public void Free(Closeproc closeProc, int streamID)
        {
            _Closeproc = closeProc;
            _StreamID = streamID;
            _Terminated = true;
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
                    return _NoMoreData && _Data.BytesNotRead == 0L && _SyncTimer.Time >= _Duration;
                }
            }
        }

        public float Volume
        {
            get { return _Volume * 100f; }
            set
            {
                lock (_LockData)
                {
                    value.Clamp(0f, 100f);
                    _Volume = value / 100f;
                }
            }
        }

        public float VolumeMax
        {
            get { return _VolumeMax * 100f; }
            set
            {
                lock (_LockData)
                {
                    value.Clamp(0f, 100f);
                    _VolumeMax = value / 100f;
                }
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
                    //TODO: Why not use Decoder.GetPosition()?
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

        public void FadeAndClose(float targetVolume, float fadeTime, Closeproc closeProc, int streamID)
        {
            _Closeproc = closeProc;
            _StreamID = streamID;
            Fade(targetVolume, fadeTime);
            _AfterFadeAction = EStreamAction.Close;
        }

        public void FadeAndStop(float targetVolume, float fadeTime, Closeproc closeProc, int streamID)
        {
            _Closeproc = closeProc;
            _StreamID = streamID;
            Fade(targetVolume, fadeTime);
            _AfterFadeAction = EStreamAction.Stop;
        }

        public void Play()
        {
            Paused = false;
            _AfterFadeAction = EStreamAction.Nothing;
            lock (_Mutex)
            {
                _ErrorCheck("StartStream", PortAudioSharp.PortAudio.Pa_StartStream(_Ptr));
            }
        }

        public void Stop()
        {
            lock (_Mutex)
            {
                _ErrorCheck("StopStream (playback)", PortAudioSharp.PortAudio.Pa_StopStream(_Ptr));
            }
            Skip(0f);
        }

        public bool Loop
        {
            get { return _SetLoop; }
            set { _SetLoop = value; }
        }

        public int Open(string fileName)
        {
            if (_FileOpened)
                return -1;

            if (!File.Exists(fileName))
                return -1;

            _Decoder = new CAudioDecoderFFmpeg();
            _Decoder.Init();

            try
            {
                lock (_Mutex)
                {
                    if (!_Initialized)
                    {
                        if (_ErrorCheck("Initialize", PortAudioSharp.PortAudio.Pa_Initialize()))
                            return -1;
                        _Initialized = true;

                        int hostApi = _ApiSelect();
                        _ApiInfo = PortAudioSharp.PortAudio.Pa_GetHostApiInfo(hostApi);
                        _OutputDeviceInfo = PortAudioSharp.PortAudio.Pa_GetDeviceInfo(_ApiInfo.defaultOutputDevice);
                        if (_OutputDeviceInfo.defaultLowOutputLatency < 0.1)
                            _OutputDeviceInfo.defaultLowOutputLatency = 0.1;
                    }
                }

                _PaStreamCallback = _ProcessNewData;
            }
            catch (Exception)
            {
                _Initialized = false;
                CLog.LogError("Error Init PortAudio Playback");
                return -1;
            }

            _Decoder.Open(fileName);
            _Duration = _Decoder.GetLength();

            SFormatInfo format = _Decoder.GetFormatInfo();
            if (format.SamplesPerSecond == 0)
                return -1;

            _ByteCount = 2 * format.ChannelCount;
            _BytesPerSecond = format.SamplesPerSecond * _ByteCount;
            _CurrentTime = 0f;
            _SyncTimer.Time = _CurrentTime;

            var stream = new SAudioStreams(0);

            var data = new IntPtr(0);

            PortAudioSharp.PortAudio.PaStreamParameters? outputParams = new PortAudioSharp.PortAudio.PaStreamParameters
                {
                    channelCount = format.ChannelCount,
                    device = _ApiInfo.defaultOutputDevice,
                    sampleFormat = PortAudioSharp.PortAudio.PaSampleFormat.paInt16,
                    suggestedLatency = _OutputDeviceInfo.defaultLowOutputLatency
                };
            PortAudioSharp.PortAudio.PaStreamParameters? inputParams = null;

            var bufsize = (uint)CConfig.AudioBufferSize;
            lock (_Mutex)
            {
                _ErrorCheck("OpenDefaultStream (playback)", PortAudioSharp.PortAudio.Pa_OpenStream(
                    out _Ptr,
                    ref inputParams,
                    ref outputParams,
                    format.SamplesPerSecond,
                    bufsize,
                    PortAudioSharp.PortAudio.PaStreamFlags.paNoFlag,
                    _PaStreamCallback,
                    data));
            }

            stream.Handle = _Ptr.ToInt32();

            if (stream.Handle != 0)
            {
                _NumStreams++;
                _Paused = true;
                _Waiting = true;
                _FileOpened = true;
                _Data = new CRingBuffer(_Bufsize);
                _NoMoreData = false;
                _DecoderThread = new Thread(_Execute) {Priority = ThreadPriority.Normal, Name = Path.GetFileName(fileName)};
                _DecoderThread.Start();

                return stream.Handle;
            }
            return -1;
        }

        public void Skip(float time)
        {
            lock (_LockSyncSignals)
            {
                _SetStart = time;
                _SetSkip = true;
                _Waiting = true;
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
                _Data = new CRingBuffer(_Bufsize);
                _NoMoreData = false;
                _EventDecode.Set();
                _Waiting = false;
                _SyncTimer.Time = _Start;
            }
        }

        private void _Execute()
        {
            while (!_Terminated)
            {
                if (_EventDecode.WaitOne(10))
                {
                    lock (_LockSyncSignals)
                    {
                        if (_SetSkip)
                        {
                            _Skip = true;
                            _EventDecode.Set();
                            _Waiting = true;
                        }

                        _SetSkip = false;

                        _Start = _SetStart;
                        _Loop = _SetLoop;
                    }

                    if (_Skip)
                    {
                        _DoSkip();
                        _Skip = false;
                    }

                    if (!_Waiting)
                    {
                        _DoDecode();
                        _Update();
                    }
                }
            }

            _DoFree();
        }

        private void _DoDecode()
        {
            if (!_FileOpened)
                return;

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
                {
                    _Waiting = false;
                    _EventDecode.Set();
                }
                else
                    _Waiting = true;
            }
        }

        private void _DoFree()
        {
            if (_Initialized)
            {
                Stop();
                _NumStreams--;
                if (_NumStreams == 0)
                {
                    lock (_Mutex)
                    {
                        PortAudioSharp.PortAudio.Pa_Terminate();
                        _Initialized = false;
                    }
                }
                _Decoder.Close();
            }

            _Closeproc(_StreamID);
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
                    Marshal.Copy(buf, 0, output, (int)frameCount * _ByteCount);
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

                if (_Data.BytesNotRead < _Beginrefill)
                {
                    _EventDecode.Set();
                    _Waiting = false;
                }
                else
                    _Waiting = true;

                float latency = buf.Length / _BytesPerSecond + CConfig.AudioLatency / 1000f;
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
                CLog.LogError("Error PortAudio.treamCallback: " + e.Message);
            }

            return PortAudioSharp.PortAudio.PaStreamCallbackResult.paContinue;
        }
        #endregion Callbacks

        private bool _ErrorCheck(String action, PortAudioSharp.PortAudio.PaError errorCode)
        {
            if (errorCode != PortAudioSharp.PortAudio.PaError.paNoError)
            {
                if (errorCode == PortAudioSharp.PortAudio.PaError.paStreamIsNotStopped)
                    return false;

                CLog.LogError(action + " error (playback): " + PortAudioSharp.PortAudio.Pa_GetErrorText(errorCode));
                if (errorCode == PortAudioSharp.PortAudio.PaError.paUnanticipatedHostError)
                {
                    PortAudioSharp.PortAudio.PaHostErrorInfo errorInfo = PortAudioSharp.PortAudio.Pa_GetLastHostErrorInfo();
                    CLog.LogError("- Host error API type: " + errorInfo.hostApiType);
                    CLog.LogError("- Host error code: " + errorInfo.errorCode);
                    CLog.LogError("- Host error text: " + errorInfo.errorText);
                }
                return true;
            }

            return false;
        }

        private int _ApiSelect()
        {
            if (!_Initialized)
                return 0;

            int selectedHostApi = PortAudioSharp.PortAudio.Pa_GetDefaultHostApi();
            int apiCount = PortAudioSharp.PortAudio.Pa_GetHostApiCount();
            for (int i = 0; i < apiCount; i++)
            {
                PortAudioSharp.PortAudio.PaHostApiInfo apiInfo = PortAudioSharp.PortAudio.Pa_GetHostApiInfo(i);
                if ((apiInfo.type == PortAudioSharp.PortAudio.PaHostApiTypeId.paDirectSound)
                    || (apiInfo.type == PortAudioSharp.PortAudio.PaHostApiTypeId.paALSA))
                    selectedHostApi = i;
            }
            return selectedHostApi;
        }

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
            if (_EventDecode != null)
            {
                _EventDecode.Close();
                _EventDecode = null;
                GC.SuppressFinalize(this);
            }
        }
    }
}