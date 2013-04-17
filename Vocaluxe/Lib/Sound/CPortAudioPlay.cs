using PortAudioSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Vocaluxe.Base;
using Vocaluxe.Lib.Sound.Decoder;

namespace Vocaluxe.Lib.Sound
{
    class CPortAudioPlay : IPlayback
    {
        private bool _Initialized;
        private readonly List<CPortAudioStream> _Decoder = new List<CPortAudioStream>();
        private Closeproc _Closeproc;
        private int _Count = 1;

        private readonly Object _MutexDecoder = new Object();

        private List<SAudioStreams> _Streams;

        public CPortAudioPlay()
        {
            Init();
        }

        public bool Init()
        {
            if (_Initialized)
                CloseAll();

            _Closeproc = _CloseProc;
            _Initialized = true;

            _Streams = new List<SAudioStreams>();
            return true;
        }

        public void CloseAll()
        {
            lock (_MutexDecoder)
            {
                for (int i = 0; i < _Decoder.Count; i++)
                    _Decoder[i].Free(_Closeproc, i + 1);
            }
        }

        public void SetGlobalVolume(float volume)
        {
            if (_Initialized)
            {
                //Bass.BASS_SetVolume(Volume / 100f);
            }
        }

        public int GetStreamCount()
        {
            if (!_Initialized)
                return 0;

            lock (_MutexDecoder)
            {
                return _Streams.Count;
            }
        }

        public void Update() {}

        #region Stream Handling
        public int Load(string media)
        {
            return Load(media, false);
        }

        public int Load(string media, bool prescan)
        {
            SAudioStreams stream = new SAudioStreams(0);
            CPortAudioStream decoder = new CPortAudioStream();

            if (decoder.Open(media) > -1)
            {
                lock (_MutexDecoder)
                {
                    _Decoder.Add(decoder);
                    stream.Handle = _Count++;
                    stream.File = media;
                    _Streams.Add(stream);
                    return stream.Handle;
                }
            }
            return 0;
        }

        public void Close(int stream)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        _Decoder[_GetStreamIndex(stream)].Free(_Closeproc, stream);
                }
            }
        }

        public void Play(int stream)
        {
            Play(stream, false);
        }

        public void Play(int stream, bool loop)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                    {
                        _Decoder[_GetStreamIndex(stream)].Loop = loop;
                        _Decoder[_GetStreamIndex(stream)].Play();
                    }
                }
            }
        }

        public void Pause(int stream)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        _Decoder[_GetStreamIndex(stream)].Paused = true;
                }
            }
        }

        public void Stop(int stream)
        {
            Pause(stream);
        }

        public void Fade(int stream, float targetVolume, float seconds)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        _Decoder[_GetStreamIndex(stream)].Fade(targetVolume, seconds);
                }
            }
        }

        public void FadeAndPause(int stream, float targetVolume, float seconds)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        _Decoder[_GetStreamIndex(stream)].FadeAndPause(targetVolume, seconds);
                }
            }
        }

        public void FadeAndStop(int stream, float targetVolume, float seconds)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        _Decoder[_GetStreamIndex(stream)].FadeAndStop(targetVolume, seconds, _Closeproc, stream);
                }
            }
        }

        public void SetStreamVolume(int stream, float volume)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        _Decoder[_GetStreamIndex(stream)].Volume = volume;
                }
            }
        }

        public void SetStreamVolumeMax(int stream, float volume)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        _Decoder[_GetStreamIndex(stream)].VolumeMax = volume;
                }
            }
        }

        public float GetLength(int stream)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        return _Decoder[_GetStreamIndex(stream)].Length;
                }
            }
            return 0f;
        }

        public float GetPosition(int stream)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        return _Decoder[_GetStreamIndex(stream)].Position;
                }

                return 0f;
            }
            return 0f;
        }

        public bool IsPlaying(int stream)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        return !_Decoder[_GetStreamIndex(stream)].Paused && !_Decoder[_GetStreamIndex(stream)].Finished;
                }
            }
            return false;
        }

        public bool IsPaused(int stream)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        return _Decoder[_GetStreamIndex(stream)].Paused;
                }
            }
            return false;
        }

        public bool IsFinished(int stream)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        return _Decoder[_GetStreamIndex(stream)].Finished;
                }
            }
            return true;
        }

        public void SetPosition(int stream, float position)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(stream))
                        _Decoder[_GetStreamIndex(stream)].Skip(position);
                }
            }
        }
        #endregion Stream Handling

        private bool _AlreadyAdded(int stream)
        {
            foreach (SAudioStreams st in _Streams)
            {
                if (st.Handle == stream)
                    return true;
            }
            return false;
        }

        private int _GetStreamIndex(int stream)
        {
            for (int i = 0; i < _Streams.Count; i++)
            {
                if (_Streams[i].Handle == stream)
                    return i;
            }
            return -1;
        }

        private void _EndSync(int handle, int stream, int data, IntPtr user)
        {
            if (_Initialized)
            {
                if (_AlreadyAdded(stream))
                    Close(stream);
            }
        }

        private void _CloseProc(int streamID)
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    if (_AlreadyAdded(streamID))
                    {
                        int index = _GetStreamIndex(streamID);
                        _Decoder.RemoveAt(index);
                        _Streams.RemoveAt(index);
                    }
                }
            }
        }
    }

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

        private readonly Stopwatch _FadeTimer = new Stopwatch();

        private float _FadeTime;
        private float _TargetVolume = 1f;
        private float _StartVolume = 1f;
        private bool _CloseStreamAfterFade;
        private bool _PauseStreamAfterFade;
        private bool _Fading;

        private static CPortAudio.SPaHostApiInfo _ApiInfo;
        private static CPortAudio.SPaDeviceInfo _OutputDeviceInfo;
        private IntPtr _Ptr = new IntPtr(0);


        private Closeproc _Closeproc;
        private CPortAudio.PaStreamCallbackDelegate _PaStreamCallback;
        private int _StreamID;
        private string _FileName;
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

        private readonly Thread _DecoderThread;

        private AutoResetEvent _EventDecode = new AutoResetEvent(false);

        private readonly Object _LockData = new Object();
        private readonly Object _LockSyncSignals = new Object();

        public CPortAudioStream()
        {
            _SyncTimer = new CSyncTimer(0f, 1f, 0.02f);
            _DecoderThread = new Thread(_Execute);
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
                    _Volume = value / 100f;
                    if (_Volume < 0f)
                        _Volume = 0f;

                    if (_Volume > 1f)
                        _Volume = 1f;
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
                    _VolumeMax = value / 100f;
                    if (_VolumeMax < 0f)
                        _VolumeMax = 0f;

                    if (_VolumeMax > 1f)
                        _VolumeMax = 1f;
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
            _Fading = true;
            _FadeTimer.Stop();
            _FadeTimer.Reset();
            _StartVolume = _Volume;
            _TargetVolume = targetVolume / 100f;
            _FadeTime = fadeTime;
            _FadeTimer.Start();
        }

        public void FadeAndPause(float targetVolume, float fadeTime)
        {
            _PauseStreamAfterFade = true;

            Fade(targetVolume, fadeTime);
        }

        public void FadeAndStop(float targetVolume, float fadeTime, Closeproc closeProc, int streamID)
        {
            _Closeproc = closeProc;
            _StreamID = streamID;
            _CloseStreamAfterFade = true;

            Fade(targetVolume, fadeTime);
        }

        public void Play()
        {
            Paused = false;
            _PauseStreamAfterFade = false;
            lock (_Mutex)
            {
                _ErrorCheck("StartStream", CPortAudio.Pa_StartStream(_Ptr));
            }
        }

        public void Stop()
        {
            lock (_Mutex)
            {
                _ErrorCheck("StopStream (playback)", CPortAudio.Pa_StopStream(_Ptr));
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

            if (_FileOpened)
                return -1;

            _Decoder = new CAudioDecoderFFmpeg();
            _Decoder.Init();

            try
            {
                lock (_Mutex)
                {
                    if (!_Initialized)
                    {
                        if (_ErrorCheck("Initialize", CPortAudio.Pa_Initialize()))
                            return -1;
                        _Initialized = true;

                        int hostApi = _ApiSelect();
                        _ApiInfo = CPortAudio.PaGetHostApiInfo(hostApi);
                        _OutputDeviceInfo = CPortAudio.PaGetDeviceInfo(_ApiInfo.DefaultOutputDevice);
                        if (_OutputDeviceInfo.DefaultLowOutputLatency < 0.1)
                            _OutputDeviceInfo.DefaultLowOutputLatency = 0.1;
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

            _FileName = fileName;
            _Decoder.Open(fileName);
            _Duration = _Decoder.GetLength();

            SFormatInfo format = _Decoder.GetFormatInfo();
            if (format.SamplesPerSecond == 0)
                return -1;

            _ByteCount = 2 * format.ChannelCount;
            _BytesPerSecond = format.SamplesPerSecond * _ByteCount;
            _CurrentTime = 0f;
            _SyncTimer.Time = _CurrentTime;

            SAudioStreams stream = new SAudioStreams(0);

            IntPtr data = new IntPtr(0);

            CPortAudio.SPaStreamParameters outputParams = new CPortAudio.SPaStreamParameters();
            outputParams.ChannelCount = format.ChannelCount;
            outputParams.Device = _ApiInfo.DefaultOutputDevice;
            outputParams.SampleFormat = CPortAudio.EPaSampleFormat.PaInt16;
            outputParams.SuggestedLatency = _OutputDeviceInfo.DefaultLowOutputLatency;

            uint bufsize = (uint)CConfig.AudioBufferSize;
            lock (_Mutex)
            {
                _ErrorCheck("OpenDefaultStream (playback)", CPortAudio.Pa_OpenStream(
                    out _Ptr,
                    IntPtr.Zero,
                    ref outputParams,
                    format.SamplesPerSecond,
                    bufsize,
                    CPortAudio.EPaStreamFlags.PaNoFlag,
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
                _DecoderThread.Priority = ThreadPriority.Normal;
                _DecoderThread.Name = Path.GetFileName(fileName);
                _DecoderThread.Start();

                return stream.Handle;
            }
            return -1;
        }

        public bool Skip(float time)
        {
            lock (_LockSyncSignals)
            {
                _SetStart = time;
                _SetSkip = true;
                _Waiting = true;
            }

            return true;
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
                        CPortAudio.Pa_Terminate();
                        _Initialized = false;
                    }
                }
            }

            _Closeproc(_StreamID);
        }
        #endregion Threading

        #region Callbacks
        private CPortAudio.EPaStreamCallbackResult _ProcessNewData(
            IntPtr input,
            IntPtr output,
            uint frameCount,
            ref CPortAudio.SPaStreamCallbackTimeInfo timeInfo,
            CPortAudio.EPaStreamCallbackFlags statusFlags,
            IntPtr userData)
        {
            byte[] buf = new byte[frameCount * _ByteCount];

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
                return CPortAudio.EPaStreamCallbackResult.PaContinue;
            }

            lock (_LockData)
            {
                if (_NoMoreData || _Data.BytesNotRead >= buf.Length)
                {
                    _Data.Read(ref buf);

                    byte[] b = new byte[2];
                    for (int i = 0; i < buf.Length; i += _ByteCount)
                    {
                        b[0] = buf[i];
                        b[1] = buf[i + 1];

                        b = BitConverter.GetBytes((Int16)(BitConverter.ToInt16(b, 0) * _Volume * _VolumeMax));
                        buf[i] = b[0];
                        buf[i + 1] = b[1];

                        if (_ByteCount == 4)
                        {
                            b[0] = buf[i + 2];
                            b[1] = buf[i + 3];

                            b = BitConverter.GetBytes((Int16)(BitConverter.ToInt16(b, 0) * _Volume * _VolumeMax));
                            buf[i + 2] = b[0];
                            buf[i + 3] = b[1];
                        }
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
                CLog.LogError("Error PortAudio.StreamCallback: " + e.Message);
            }

            return CPortAudio.EPaStreamCallbackResult.PaContinue;
        }
        #endregion Callbacks

        private bool _ErrorCheck(String action, CPortAudio.EPaError errorCode)
        {
            if (errorCode != CPortAudio.EPaError.PaNoError)
            {
                if (errorCode == CPortAudio.EPaError.PaStreamIsNotStopped)
                    return false;

                CLog.LogError(action + " error (playback): " + CPortAudio.PaGetErrorText(errorCode));
                if (errorCode == CPortAudio.EPaError.PaUnanticipatedHostError)
                {
                    CPortAudio.SPaHostErrorInfo errorInfo = CPortAudio.PaGetLastHostErrorInfo();
                    CLog.LogError("- Host error API type: " + errorInfo.HostApiType);
                    CLog.LogError("- Host error code: " + errorInfo.ErrorCode);
                    CLog.LogError("- Host error text: " + errorInfo.ErrorText);
                }
                return true;
            }

            return false;
        }

        private int _ApiSelect()
        {
            if (!_Initialized)
                return 0;

            int selectedHostApi = CPortAudio.Pa_GetDefaultHostApi();
            int apiCount = CPortAudio.Pa_GetHostApiCount();
            for (int i = 0; i < apiCount; i++)
            {
                CPortAudio.SPaHostApiInfo apiInfo = CPortAudio.PaGetHostApiInfo(i);
                if ((apiInfo.Type == CPortAudio.EPaHostApiTypeId.PaDirectSound)
                    || (apiInfo.Type == CPortAudio.EPaHostApiTypeId.PaALSA))
                    selectedHostApi = i;
            }
            return selectedHostApi;
        }

        private void _Update()
        {
            if (_Fading)
            {
                if (_FadeTimer.ElapsedMilliseconds / 1000f < _FadeTime)
                    _Volume = _StartVolume + (_TargetVolume - _StartVolume) * ((_FadeTimer.ElapsedMilliseconds / 1000f) / _FadeTime);
                else
                {
                    _Volume = _TargetVolume;
                    _FadeTimer.Stop();
                    _Fading = false;

                    if (_CloseStreamAfterFade)
                        _Terminated = true;

                    if (_PauseStreamAfterFade)
                        Paused = true;
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