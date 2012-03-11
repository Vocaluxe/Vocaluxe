﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using PortAudioSharp;

using Vocaluxe.Base;
using Vocaluxe.Lib.Sound.Decoder;

namespace Vocaluxe.Lib.Sound
{
    class CPortAudioPlay : IPlayback
    {
        private bool _Initialized = false;
        private List<PortAudioStream> _Decoder = new List<PortAudioStream>();
        private CLOSEPROC closeproc;
        private int _Count = 1;

        private Object MutexDecoder = new Object();

        private List<AudioStreams> _Streams;
               

        public CPortAudioPlay()
        {
            Init();
        }

        public bool Init()
        {
            if (_Initialized)
                CloseAll();

            closeproc = new CLOSEPROC(close_proc);
            _Initialized = true;

            _Streams = new List<AudioStreams>();
            return true;
        }

        public void CloseAll()
        {
            lock (MutexDecoder)
            {
                for (int i = 0; i < _Decoder.Count; i++)
                {
                    _Decoder[i].Free(closeproc, i + 1);
                }
            }   
        }

        public void SetGlobalVolume(float Volume)
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

            lock (MutexDecoder)
            {
                return _Streams.Count;
            }
        }

        public void Update()
        {
        }

        #region Stream Handling
        public int Load(string Media)
        {
            return Load(Media, false);
        }

        public int Load(string Media, bool Prescan)
        {
            AudioStreams stream = new AudioStreams(0);
            PortAudioStream decoder = new PortAudioStream();

            if (decoder.Open(Media) > -1)
            {
                lock (MutexDecoder)
                {
                    _Decoder.Add(decoder);
                    stream.handle = _Count++;
                    stream.file = Media;
                    _Streams.Add(stream);
                    return stream.handle;
                }

            }
            return 0;
        }

        public void Close(int Stream)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(Stream))
                    {
                        _Decoder[GetStreamIndex(Stream)].Free(closeproc, Stream);
                    }
                }

            }
        }

        public void Play(int Stream)
        {
            Play(Stream, false);
        }

        public void Play(int Stream, bool Loop)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(Stream))
                    {
                        _Decoder[GetStreamIndex(Stream)].Loop = Loop;
                        _Decoder[GetStreamIndex(Stream)].Play();                        
                    }
                }

            }
        }

        public void Pause(int Stream)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(Stream))
                    {
                        _Decoder[GetStreamIndex(Stream)].Paused = true;
                    }
                }

            }
        }

        public void Stop(int Stream)
        {
            Pause(Stream);
        }

        public void Fade(int Stream, float TargetVolume, float Seconds)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(Stream))
                    {
                        _Decoder[GetStreamIndex(Stream)].Fade(TargetVolume, Seconds);
                    }
                }

            }
        }

        public void FadeAndPause(int Stream, float TargetVolume, float Seconds)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(Stream))
                    {
                        _Decoder[GetStreamIndex(Stream)].FadeAndPause(TargetVolume, Seconds);
                    }
                }

            }
        }

        public void FadeAndStop(int Stream, float TargetVolume, float Seconds)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(Stream))
                    {
                        _Decoder[GetStreamIndex(Stream)].FadeAndStop(TargetVolume, Seconds, closeproc, Stream);
                    }
                }

            }
        }

        public void SetStreamVolume(int Stream, float Volume)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(Stream))
                    {
                        _Decoder[GetStreamIndex(Stream)].Volume = Volume;
                    }
                }

            }
        }

        public float GetLength(int Stream)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(Stream))
                    {
                        return _Decoder[GetStreamIndex(Stream)].Length;
                    }
                }

            }
            return 0f;
        }

        public float GetPosition(int Stream)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(Stream))
                    {
                        return _Decoder[GetStreamIndex(Stream)].Position;
                    }
                }

                return 0f;
            }
            return 0f;
        }

        public bool IsPlaying(int Stream)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(Stream))
                    {
                        return !_Decoder[GetStreamIndex(Stream)].Paused && !_Decoder[GetStreamIndex(Stream)].Finished;
                    }
                }

            }
            return false;
        }

        public bool IsPaused(int Stream)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(Stream))
                    {
                        return _Decoder[GetStreamIndex(Stream)].Paused;
                    }
                }

            }
            return false;
        }

        public bool IsFinished(int Stream)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(Stream))
                    {
                        return _Decoder[GetStreamIndex(Stream)].Finished;
                    }
                }

            }
            return true;
        }

        public void SetPosition(int Stream, float Position)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(Stream))
                    {
                        _Decoder[GetStreamIndex(Stream)].Skip(Position);
                    }
                }

            }
        }
        #endregion Stream Handling


        private bool AlreadyAdded(int Stream)
        {
            foreach (AudioStreams st in _Streams)
            {
                if (st.handle == Stream)
                {
                    return true;
                }
            }
            return false;
        }

        private int GetStreamIndex(int Stream)
        {
            for (int i = 0; i < _Streams.Count; i++)
            {
                if (_Streams[i].handle == Stream)
                    return i;
            }
            return -1;
        }

        private void EndSync(int handle, int Stream, int data, IntPtr user)
        {
            if (_Initialized)
            {
                if (AlreadyAdded(Stream))
                {
                    Close(Stream);
                }
            }
        }

        private void close_proc(int StreamID)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(StreamID))
                    {
                        int Index = GetStreamIndex(StreamID);
                        _Decoder.RemoveAt(Index);
                        _Streams.RemoveAt(Index);
                    }
                }

            }
        }
    }

    class PortAudioStream
    {
        const long BUFSIZE = 50000L;

        private CSyncTimer _SyncTimer;
        private bool _Initialized;
        private int _ByteCount = 4;
        private float _Volume = 1f;
        
        private Stopwatch _fadeTimer = new Stopwatch();

        private float _fadeTime = 0f;
        private float _targetVolume = 1f;
        private float _startVolume = 1f;
        private bool _closeStreamAfterFade = false;
        private bool _pauseStreamAfterFade = false;
        private bool _fading = false;


        private PortAudio.PaHostApiInfo _apiInfo;
        private PortAudio.PaDeviceInfo _outputDeviceInfo;
        private IntPtr _Ptr = new IntPtr(0);

        private Stopwatch _FadeTimer = new Stopwatch();

        private CLOSEPROC _Closeproc;
        private PortAudio.PaStreamCallbackDelegate _paStreamCallback;
        private int _StreamID;
        private string _FileName;
        private IAudioDecoder _Decoder;
        private float _BytesPerSecond;
        private bool _NoMoreData = false;
        

        private bool _FileOpened = false;

        private bool _waiting = false;
        private bool _skip = false;
        
        private bool _Loop = false;
        private float _Duration = 0f;
        private float _CurrentTime = 0f;
        private float _TimeCode = 0f;
        
        private bool _Paused = false;

        private RingBuffer _data;
        private float _SetStart = 0f;
        private float _Start = 0f;
        private bool _SetLoop = false;
        private bool _SetSkip = false;
        private bool _terminated = false;

        private Thread _DecoderThread;

        AutoResetEvent EventDecode = new AutoResetEvent(false);
        
        Object MutexData = new Object();
        Object MutexSyncSignals = new Object();

        public PortAudioStream()
        {
            _Initialized = false;
            _SyncTimer = new CSyncTimer(0f, 1f, 0.02f);
            _DecoderThread = new Thread(Execute);
        }

        public void Free(CLOSEPROC close_proc, int StreamID)
        {
            _Closeproc = close_proc;
            _StreamID = StreamID;
            _terminated = true;
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
                lock (MutexData)
                {
                    return _NoMoreData && _data.BytesNotRead == 0L;
                }
            }
        }

        public float Volume
        {
            get { return _Volume * 100f; }
            set
            {
                lock (MutexData)
                {
                    _Volume = value / 100f;
                    if (_Volume < 0f)
                        _Volume = 0f;

                    if (_Volume > 1f)
                        _Volume = 1f;
                }
            }
        }

        public float Position
        {
            get
            {
                lock (MutexData)
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
                lock (MutexSyncSignals)
                {
                    if (_Paused)
                        _SyncTimer.Pause();
                    else
                    {
                        _SyncTimer.Resume();
                        EventDecode.Set();
                    }
                }
            }
        }

        public void Fade(float TargetVolume, float FadeTime)
        {         
            _fading = true;
            _fadeTimer.Stop();
            _fadeTimer.Reset();
            _startVolume = _Volume;
            _targetVolume = TargetVolume / 100f;
            _fadeTime = FadeTime;
            _fadeTimer.Start();
        }

        public void FadeAndPause(float TargetVolume, float FadeTime)
        {
            _pauseStreamAfterFade = true;

            Fade(TargetVolume, FadeTime);
        }

        public void FadeAndStop(float TargetVolume, float FadeTime, CLOSEPROC close_proc, int StreamID)
        {
            _Closeproc = close_proc;
            _StreamID = StreamID;
            _closeStreamAfterFade = true;

            Fade(TargetVolume, FadeTime);
        }

        public void Play()
        {
            Paused = false;
            _pauseStreamAfterFade = false;
            errorCheck("StartStream", PortAudio.Pa_StartStream(_Ptr));
        }

        public void Stop()
        {
            errorCheck("StartStream", PortAudio.Pa_StopStream(_Ptr));
            Skip(0f);
        }

        public bool Loop
        {
            get { return _SetLoop; }
            set { _SetLoop = value; }
        }

        public int Open(string FileName)
        {
            if (_FileOpened)
                return -1;

            if (!System.IO.File.Exists(FileName))
                return -1;

            if (_FileOpened)
                return -1;

            _Decoder = new CAudioDecoderFFmpeg();
            _Decoder.Init();

            try
            {
                if (errorCheck("Initialize", PortAudio.Pa_Initialize()))
                    return -1;

                _Initialized = true;
                int hostApi = apiSelect();
                _apiInfo = PortAudio.Pa_GetHostApiInfo(hostApi);
                _outputDeviceInfo = PortAudio.Pa_GetDeviceInfo(_apiInfo.defaultOutputDevice);
                _paStreamCallback = new PortAudio.PaStreamCallbackDelegate(_PaStreamCallback);

                if (_outputDeviceInfo.defaultLowOutputLatency < 0.1)
                    _outputDeviceInfo.defaultLowOutputLatency = 0.1;
            }

            catch (Exception)
            {
                _Initialized = false;
                CLog.LogError("Error Init PortAudio Playback");
                return -1;
            }
            
            _FileName = FileName;
            _Decoder.Open(FileName);
            _Duration = _Decoder.GetLength();

            FormatInfo format = _Decoder.GetFormatInfo();
            _ByteCount = 2 * format.ChannelCount;
            _BytesPerSecond = format.SamplesPerSecond * _ByteCount;
            _CurrentTime = 0f;
            _SyncTimer.Time = _CurrentTime;

            AudioStreams stream = new AudioStreams(0);
            
            IntPtr data = new IntPtr(0);

            PortAudio.PaStreamParameters outputParams = new PortAudio.PaStreamParameters();
            outputParams.channelCount = format.ChannelCount;
            outputParams.device = _apiInfo.defaultOutputDevice;
            outputParams.sampleFormat = PortAudio.PaSampleFormat.paInt16;
            outputParams.suggestedLatency = _outputDeviceInfo.defaultLowOutputLatency;

            errorCheck("OpenDefaultStream", PortAudio.Pa_OpenStream(
                out _Ptr,
                IntPtr.Zero,
                ref outputParams,
                format.SamplesPerSecond,
                (uint)CConfig.AudioBufferSize,
                PortAudio.PaStreamFlags.paNoFlag,
                _paStreamCallback,
                data));

            stream.handle = _Ptr.ToInt32();

            if (stream.handle != 0)
            {
                _Paused = true;
                _waiting = true;
                _FileOpened = true;
                _data = new RingBuffer(BUFSIZE);
                _NoMoreData = false;
                _DecoderThread.Priority = ThreadPriority.Normal;
                _DecoderThread.Name = Path.GetFileName(FileName);
                _DecoderThread.Start();
                
                return stream.handle;
            }
            return -1;
        }

        public bool Skip(float Time)
        {
            lock (MutexSyncSignals)
            {
                _SetStart = Time;
                _SetSkip = true;
                _waiting = true;
            }

            return true;
        }

        #region Threading
        private void DoSkip()
        {
            lock (MutexData)
            {
                _Decoder.SetPosition(_Start);
                _CurrentTime = _Start;
                _TimeCode = _Start;
                _data = new RingBuffer(BUFSIZE);
                _NoMoreData = false;
                EventDecode.Set();
                _waiting = false;
                _SyncTimer.Time = _Start;
            }
        }

        private void Execute()
        {
            while (!_terminated)
            {
                if (EventDecode.WaitOne(10))
                {
                    lock (MutexSyncSignals)
                    {
                        if (_SetSkip)
                        {
                            _skip = true;
                            EventDecode.Set();
                            _waiting = true;
                        }

                        _SetSkip = false;

                        _Start = _SetStart;
                        _Loop = _SetLoop;
                    }

                    if (_skip)
                    {
                        DoSkip();
                        _skip = false;
                    }

                    if (!_waiting)
                    {
                        DoDecode();
                        Update();
                    }
                }
            }

            DoFree();
        }

        private void DoDecode()
        {
            if (!_FileOpened)
                return;

            if (_Paused)
                return;

            if (_terminated)
                return;

            float Timecode;
            byte[] Buffer;

            bool DoIt = false;
            lock (MutexData)
            {
                if (!_skip && BUFSIZE - 10000L > _data.BytesNotRead)
                    DoIt = true;
            }

            if (!DoIt)
                return;

            _Decoder.Decode(out Buffer, out Timecode);

            if (Buffer == null)
            {
                if (_Loop)
                {
                    lock (MutexSyncSignals)
                    {
                        _CurrentTime = 0f;
                        _Start = 0f;
                    }

                    DoSkip();
                }
                else
                {
                    _NoMoreData = true;
                }
                return;
            }

            lock (MutexData)
            {
                _data.Write(Buffer);
                _TimeCode = Timecode;
                if (_data.BytesNotRead < BUFSIZE - 10000L)
                {
                    _waiting = false;
                    EventDecode.Set();
                }
                else
                    _waiting = true;
            }
        }

        private void DoFree()
        {
            if (_Initialized)
            {
                Stop();
                PortAudio.Pa_Terminate();
            }

            _Closeproc(_StreamID);
        }
        #endregion Threading

        #region Callbacks
        private PortAudio.PaStreamCallbackResult _PaStreamCallback(
            IntPtr input,
            IntPtr output,
            uint frameCount,
            ref PortAudio.PaStreamCallbackTimeInfo timeInfo,
            PortAudio.PaStreamCallbackFlags statusFlags,
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
                return PortAudio.PaStreamCallbackResult.paContinue;
            }

            lock (MutexData)
            {
                if (_NoMoreData || _data.BytesNotRead >= buf.Length)
                {
                    _data.Read(ref buf);

                    byte[] b = new byte[2];
                    for (int i = 0; i < buf.Length; i += _ByteCount)
                    {
                        b[0] = buf[i];
                        b[1] = buf[i + 1];

                        b = BitConverter.GetBytes((Int16)(BitConverter.ToInt16(b, 0) * _Volume));
                        buf[i] = b[0];
                        buf[i + 1] = b[1];

                        if (_ByteCount == 4)
                        {
                            b[0] = buf[i + 2];
                            b[1] = buf[i + 3];

                            b = BitConverter.GetBytes((Int16)(BitConverter.ToInt16(b, 0) * _Volume));
                            buf[i + 2] = b[0];
                            buf[i + 3] = b[1];
                        }
                    }
                }

                if (_data.BytesNotRead < BUFSIZE - 10000L)
                {
                    EventDecode.Set();
                    _waiting = false;
                }
                else
                    _waiting = true;

                float latency = buf.Length / _BytesPerSecond + CConfig.AudioLatency/1000f;
                float time = _TimeCode - _data.BytesNotRead / _BytesPerSecond - latency;

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

            return PortAudio.PaStreamCallbackResult.paContinue;
        }
        #endregion Callbacks

        private bool errorCheck(String action, PortAudio.PaError errorCode)
        {
            if (errorCode != PortAudio.PaError.paNoError)
            {
                if (errorCode == PortAudio.PaError.paStreamIsNotStopped)
                    return false;

                CLog.LogError(action + " error: " + PortAudio.Pa_GetErrorText(errorCode));
                if (errorCode == PortAudio.PaError.paUnanticipatedHostError)
                {
                    PortAudio.PaHostErrorInfo errorInfo = PortAudio.Pa_GetLastHostErrorInfo();
                    CLog.LogError("- Host error API type: " + errorInfo.hostApiType);
                    CLog.LogError("- Host error code: " + errorInfo.errorCode);
                    CLog.LogError("- Host error text: " + errorInfo.errorText);
                }
                return true;
            }

            return false;
        }

        private int apiSelect()
        {
            if (!_Initialized)
                return 0;

            int selectedHostApi = PortAudio.Pa_GetDefaultHostApi();
            int apiCount = PortAudio.Pa_GetHostApiCount();
            for (int i = 0; i < apiCount; i++)
            {
                PortAudio.PaHostApiInfo apiInfo = PortAudio.Pa_GetHostApiInfo(i);
                if ((apiInfo.type == PortAudio.PaHostApiTypeId.paDirectSound)
                    || (apiInfo.type == PortAudio.PaHostApiTypeId.paALSA))
                    selectedHostApi = i;
            }
            return selectedHostApi;
        }

        private void Update()
        {
            if (_fading)
            {
                if ((float)_fadeTimer.ElapsedMilliseconds / 1000f < _fadeTime)
                    _Volume = _startVolume + (_targetVolume - _startVolume) * ((_fadeTimer.ElapsedMilliseconds / 1000f) / _fadeTime);
                else
                {
                    _Volume = _targetVolume;
                    _fadeTimer.Stop();
                    _fading = false;

                    if (_closeStreamAfterFade)
                        _terminated = true;

                    if (_pauseStreamAfterFade)
                        Paused = true;
                }
            }
        }
    }
}
