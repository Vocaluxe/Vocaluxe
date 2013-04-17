using OpenTK.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Vocaluxe.Base;
using Vocaluxe.Lib.Sound.Decoder;

namespace Vocaluxe.Lib.Sound
{
    class COpenALPlay : IPlayback, IDisposable
    {
        private bool _Initialized;
        private readonly List<COpenAlStream> _Decoder = new List<COpenAlStream>();
        private Closeproc _Closeproc;
        private int _Count = 1;
        private AudioContext _Context;

        private readonly Object _MutexDecoder = new Object();

        private List<SAudioStreams> _Streams;

        public COpenALPlay()
        {
            Init();
        }

        public bool Init()
        {
            if (_Initialized)
                CloseAll();

            _Context = new AudioContext();

            _Context.MakeCurrent();


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
                    _Decoder[i].Free(_Closeproc, i + 1, _MutexDecoder);
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

        public void Update()
        {
            if (_Initialized)
            {
                lock (_MutexDecoder)
                {
                    foreach (COpenAlStream stream in _Decoder)
                        stream.UploadData();
                }
            }
        }

        #region Stream Handling
        public int Load(string media)
        {
            return Load(media, false);
        }

        public int Load(string media, bool prescan)
        {
            SAudioStreams stream = new SAudioStreams(0);
            COpenAlStream decoder = new COpenAlStream();

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
                        _Decoder[_GetStreamIndex(stream)].Free(_Closeproc, stream, _MutexDecoder);
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

        public void Dispose()
        {
            _Context.Dispose();
            _Context = null;
        }
    }

    class COpenAlStream : IDisposable
    {
        private const int _BufferSize = 2048;
        private const int _BufferCount = 5;
        private const long _Bufsize = 50000L;

        private Object _CloseMutex;

        private int[] _Buffers;
        private int _State;
        private int _Source;
        private SFormatInfo _Format;

        private bool _Initialized;
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

        private readonly Stopwatch _Timer = new Stopwatch();

        private Closeproc _Closeproc;
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

        private readonly Object _MutexData = new Object();
        private readonly Object _MutexSyncSignals = new Object();

        public COpenAlStream()
        {
            _Initialized = false;
            _DecoderThread = new Thread(_Execute);
        }

        public void Free(Closeproc closeProc, int streamID, Object closeMutex)
        {
            _Closeproc = closeProc;
            _StreamID = streamID;
            _Terminated = true;
            this._CloseMutex = closeMutex;
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
                lock (_MutexData)
                {
                    return _NoMoreData && _Data.BytesNotRead == 0L;
                }
            }
        }

        public float Volume
        {
            get { return _Volume * 100f; }
            set
            {
                lock (_MutexData)
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
                lock (_MutexData)
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
                if (Finished)
                    _Timer.Stop();

                return _CurrentTime + _Timer.ElapsedMilliseconds / 1000f;
            }
        }

        public bool Paused
        {
            get { return _Paused; }
            set
            {
                _Paused = value;
                lock (_MutexSyncSignals)
                {
                    if (_Paused)
                    {
                        _Timer.Stop();
                        AL.SourceStop(_Source);
                    }
                    else
                    {
                        _Timer.Start();
                        _EventDecode.Set();
                        AL.SourcePlay(_Source);
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
            AL.SourcePlay(_Source);
        }

        public void Stop()
        {
            AL.SourceStop(_Source);
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
                _Source = AL.GenSource();
                _Buffers = new int[_BufferCount];
                for (int i = 0; i < _BufferCount; i++)
                    _Buffers[i] = AL.GenBuffer();

                _State = 0;
                //AL.SourceQueueBuffers(_source, _buffers.Length, _buffers);
            }
            catch (Exception)
            {
                _Initialized = false;
                CLog.LogError("Error Init OpenAL Playback");
                return -1;
            }

            _FileName = fileName;
            _Decoder.Open(fileName);
            _Duration = _Decoder.GetLength();

            _Format = _Decoder.GetFormatInfo();
            _ByteCount = 2 * _Format.ChannelCount;
            _BytesPerSecond = _Format.SamplesPerSecond * _ByteCount;
            _CurrentTime = 0f;
            _Timer.Reset();

            SAudioStreams stream = new SAudioStreams(0);

            stream.Handle = _Buffers[0];

            if (stream.Handle != 0)
            {
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
            lock (_MutexSyncSignals)
            {
                _SetStart = time;
                _SetSkip = true;
            }
            _EventDecode.Set();

            return true;
        }

        #region Threading
        private void _DoSkip()
        {
            _Decoder.SetPosition(_Start);
            _CurrentTime = _Start;
            _TimeCode = _Start;
            _Timer.Reset();
            _Waiting = false;

            lock (_MutexData)
            {
                _Data = new CRingBuffer(_Bufsize);
                _NoMoreData = false;
            }
        }

        private void _Execute()
        {
            while (!_Terminated)
            {
                _Waiting = false;
                if (_EventDecode.WaitOne(1) || !_Waiting)
                {
                    if (_Skip)
                    {
                        _DoSkip();
                        _Skip = false;
                    }

                    _DoDecode();
                }
                if (!_Terminated)
                {
                    lock (_MutexSyncSignals)
                    {
                        if (_SetSkip)
                            _Skip = true;

                        _SetSkip = false;

                        _Start = _SetStart;
                        _Loop = _SetLoop;
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
            lock (_MutexData)
            {
                if (_Bufsize - 10000L > _Data.BytesNotRead)
                    doIt = true;
            }

            if (!doIt)
                return;

            _Decoder.Decode(out buffer, out timecode);

            if (buffer == null)
            {
                if (_Loop)
                {
                    lock (_MutexSyncSignals)
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

            lock (_MutexData)
            {
                _Data.Write(buffer);
                _TimeCode = timecode;
            }
        }

        private void _DoFree()
        {
            if (_Initialized)
            {
                lock (_CloseMutex)
                {
                    Stop();
                    AL.DeleteSource(_Source);
                    AL.DeleteBuffers(_Buffers);
                }
            }

            _Closeproc(_StreamID);
        }
        #endregion Threading

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

        public void UploadData()
        {
            _Update();

            if (_Paused)
                return;

            int queuedCount = 0;
            bool doit = true;
            AL.GetSource(_Source, ALGetSourcei.BuffersQueued, out queuedCount);

            int processedCount = _BufferCount;
            if (queuedCount > 0)
            {
                AL.GetSource(_Source, ALGetSourcei.BuffersProcessed, out processedCount);
                doit = false;
                Console.WriteLine("Buffers Processed on Stream " + _Source.ToString() + " = " + processedCount.ToString());
                if (processedCount < 1)
                    return;
            }

            byte[] buf = new byte[_BufferSize];

            lock (_MutexData)
            {
                while (processedCount > 0)
                {
                    if (_Data.BytesNotRead >= buf.Length)
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


                        int buffer = 0;
                        if (!doit)
                            buffer = AL.SourceUnqueueBuffer(_Source);
                        else
                        {
                            buffer = _Buffers[queuedCount];
                            queuedCount++;
                        }

                        if (buffer != 0)
                        {
                            if (_Format.ChannelCount == 2)
                                AL.BufferData(buffer, ALFormat.Stereo16, buf, buf.Length, _Format.SamplesPerSecond);
                            else
                                AL.BufferData(buffer, ALFormat.Mono16, buf, buf.Length, _Format.SamplesPerSecond);
                            Console.WriteLine("Write to Buffer: " + buffer.ToString());
                            AL.SourceQueueBuffer(_Source, buffer);
                        }
                    }
                    processedCount--;
                }
                AL.GetSource(_Source, ALGetSourcei.SourceState, out _State);
                if ((ALSourceState)_State != ALSourceState.Playing)
                    AL.SourcePlay(_Source);
            }

            _CurrentTime = _TimeCode - _Data.BytesNotRead / _BytesPerSecond - 0.1f;
            _Timer.Reset();
            _Timer.Start();
        }

        public void Dispose()
        {
            _EventDecode.Close();
            _EventDecode = null;
        }
    }
}