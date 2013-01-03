using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using OpenTK.Audio;

using Vocaluxe.Base;
using Vocaluxe.Lib.Sound.Decoder;

namespace Vocaluxe.Lib.Sound
{
    class COpenALPlay : IPlayback
    {
        private bool _Initialized = false;
        private List<OpenAlStream> _Decoder = new List<OpenAlStream>();
        private CLOSEPROC closeproc;
        private int _Count = 1;
        private AudioContext AC;

        private Object MutexDecoder = new Object();

        private List<AudioStreams> _Streams;
               

        public COpenALPlay()
        {
            Init();
        }

        public bool Init()
        {
            if (_Initialized)
                CloseAll();

            AC = new AudioContext();
            
            AC.MakeCurrent();
            

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
                    _Decoder[i].Free(closeproc, i + 1, MutexDecoder);
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
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    foreach (OpenAlStream stream in _Decoder)
                    {
                        stream.UploadData();
                    }
                }

            }
        }

        #region Stream Handling
        public int Load(string Media)
        {
            return Load(Media, false);
        }

        public int Load(string Media, bool Prescan)
        {
            AudioStreams stream = new AudioStreams(0);
            OpenAlStream decoder = new OpenAlStream();

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
                        _Decoder[GetStreamIndex(Stream)].Free(closeproc, Stream, MutexDecoder);
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

        public void SetStreamVolumeMax(int Stream, float Volume)
        {
            if (_Initialized)
            {
                lock (MutexDecoder)
                {
                    if (AlreadyAdded(Stream))
                    {
                        _Decoder[GetStreamIndex(Stream)].VolumeMax = Volume;
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

    class OpenAlStream
    {
        const int buffer_size = 2048;
        const int buffer_count = 5;
        const long BUFSIZE = 50000L;

        Object CloseMutex;

        private int[] _buffers;
        private int _state;
        private int _source;
        private FormatInfo _format;
                
        private bool _Initialized;
        private int _ByteCount = 4;
        private float _Volume = 1f;
        private float _VolumeMax = 1f;

        private Stopwatch _fadeTimer = new Stopwatch();

        private float _fadeTime = 0f;
        private float _targetVolume = 1f;
        private float _startVolume = 1f;
        private bool _closeStreamAfterFade = false;
        private bool _pauseStreamAfterFade = false;
        private bool _fading = false;


        private Stopwatch _FadeTimer = new Stopwatch();
        private Stopwatch _Timer = new Stopwatch();

        private CLOSEPROC _Closeproc;
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

        public OpenAlStream()
        {
            _Initialized = false;
            _DecoderThread = new Thread(Execute);
        }

        public void Free(CLOSEPROC close_proc, int StreamID, Object CloseMutex)
        {
            _Closeproc = close_proc;
            _StreamID = StreamID;
            _terminated = true;
            this.CloseMutex = CloseMutex;
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

        public float VolumeMax
        {
            get { return _VolumeMax * 100f; }
            set
            {
                lock (MutexData)
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
                lock (MutexSyncSignals)
                {
                    if (_Paused)
                    {
                        _Timer.Stop();
                        AL.SourceStop(_source);
                    }
                    else
                    {
                        _Timer.Start();
                        EventDecode.Set();
                        AL.SourcePlay(_source);
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
            AL.SourcePlay(_source);
        }

        public void Stop()
        {
            AL.SourceStop(_source);
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
                _source = AL.GenSource();
                _buffers = new int[buffer_count];
                for (int i = 0; i < buffer_count; i++)
			    {
                    _buffers[i] = AL.GenBuffer();
			    }
               
                _state = 0;
                //AL.SourceQueueBuffers(_source, _buffers.Length, _buffers);
            }

            catch (Exception)
            {
                _Initialized = false;
                CLog.LogError("Error Init OpenAL Playback");
                return -1;
            }
            
            _FileName = FileName;
            _Decoder.Open(FileName);
            _Duration = _Decoder.GetLength();

            _format = _Decoder.GetFormatInfo();
            _ByteCount = 2 * _format.ChannelCount;
            _BytesPerSecond = _format.SamplesPerSecond * _ByteCount;
            _CurrentTime = 0f;
            _Timer.Reset();

            AudioStreams stream = new AudioStreams(0);

            stream.handle = _buffers[0];

            if (stream.handle != 0)
            {
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
            }
            EventDecode.Set();

            return true;
        }

        #region Threading
        private void DoSkip()
        {
            _Decoder.SetPosition(_Start);
            _CurrentTime = _Start;
            _TimeCode = _Start;
            _Timer.Reset();
            _waiting = false;

            lock (MutexData)
            {
                _data = new RingBuffer(BUFSIZE);
                _NoMoreData = false;
            }
        }

        private void Execute()
        {
            while (!_terminated)
            {
                _waiting = false;
                if (EventDecode.WaitOne(1) || !_waiting)
                {
                    if (_skip)
                    {
                        DoSkip();
                        _skip = false;
                    }

                    DoDecode();
                    
                }
                if (!_terminated)
                {
                    lock (MutexSyncSignals)
                    {
                        if (_SetSkip)
                            _skip = true;

                        _SetSkip = false;
                        
                        _Start = _SetStart;
                        _Loop = _SetLoop;
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
                if (BUFSIZE - 10000L > _data.BytesNotRead)
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
            }
        }

        private void DoFree()
        {
            if (_Initialized)
            {
                lock (CloseMutex)
                {
                    Stop();
                    AL.DeleteSource(_source);
                    AL.DeleteBuffers(_buffers);
                }  
            }

            _Closeproc(_StreamID);
        }
        #endregion Threading

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

        public void UploadData()
        {
            Update();

            if (_Paused)
                return;

            int queued_count = 0;
            bool doit = true;
            AL.GetSource(_source, ALGetSourcei.BuffersQueued, out queued_count);

            int processed_count = buffer_count;
            if (queued_count > 0)
            {
                AL.GetSource(_source, ALGetSourcei.BuffersProcessed, out processed_count);
                doit = false;
                Console.WriteLine("Buffers Processed on Stream " + _source.ToString() + " = " + processed_count.ToString());
                if (processed_count < 1)
                    return;
            }

            byte[] buf = new byte[buffer_size];

            lock (MutexData)
            {
                while (processed_count > 0)
                {
                    if (_data.BytesNotRead >= buf.Length)
                    {
                        _data.Read(ref buf);

                        
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
                            buffer = AL.SourceUnqueueBuffer(_source);
                        else
                        {
                            buffer = _buffers[queued_count];
                            queued_count++;
                        }

                        if (buffer != 0)
                        {
                            if (_format.ChannelCount == 2)
                                AL.BufferData(buffer, ALFormat.Stereo16, buf, buf.Length, _format.SamplesPerSecond);
                            else
                                AL.BufferData(buffer, ALFormat.Mono16, buf, buf.Length, _format.SamplesPerSecond);
                            Console.WriteLine("Write to Buffer: " + buffer.ToString());
                            AL.SourceQueueBuffer(_source, buffer);
                        }
                    }
                    processed_count--;
                }
                AL.GetSource(_source, ALGetSourcei.SourceState, out _state);
                if ((ALSourceState)_state != ALSourceState.Playing)
                {
                    AL.SourcePlay(_source);
                }
            }

            _CurrentTime = _TimeCode - _data.BytesNotRead / _BytesPerSecond - 0.1f;
            _Timer.Reset();
            _Timer.Start();
        }
    }
}

