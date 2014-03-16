using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using OpenTK.Audio;
using Vocaluxe.Base;
using Vocaluxe.Lib.Sound.Playback.Decoder;
using VocaluxeLib;

namespace Vocaluxe.Lib.Sound.Playback.OpenAL
{
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

        private EStreamAction _AfterFadeAction;
        private CFading _Fading;

        private readonly Stopwatch _Timer = new Stopwatch();

        private Closeproc _Closeproc;
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

        private readonly Object _MutexData = new Object();
        private readonly Object _MutexSyncSignals = new Object();

        public void Free(Closeproc closeProc, int streamID, Object closeMutex)
        {
            _Closeproc = closeProc;
            _StreamID = streamID;
            _Terminated = true;
            _CloseMutex = closeMutex;
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
                lock (_MutexData)
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
                if (Finished)
                    _Timer.Stop();
                //TODO: Why not use Decoder.GetPosition()?
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
            targetVolume.Clamp(0f, 100f);
            _Fading = new CFading(_Volume, targetVolume / 100f, fadeTime);
            _AfterFadeAction = EStreamAction.Nothing;
        }

        public void FadeAndPause(float targetVolume, float fadeTime)
        {
            Fade(targetVolume, fadeTime);
            _AfterFadeAction = EStreamAction.Pause;
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

            _Decoder.Open(fileName);
            _Duration = _Decoder.GetLength();

            _Format = _Decoder.GetFormatInfo();
            _ByteCount = 2 * _Format.ChannelCount;
            _BytesPerSecond = _Format.SamplesPerSecond * _ByteCount;
            _CurrentTime = 0f;
            _Timer.Reset();

            var stream = new SAudioStreams(0) {Handle = _Buffers[0]};

            if (stream.Handle != 0)
            {
                _FileOpened = true;
                _Data = new CRingBuffer(_Bufsize);
                _NoMoreData = false;
                _DecoderThread = new Thread(_Execute) {Priority = ThreadPriority.Normal, Name = Path.GetFileName(fileName)};
                _DecoderThread.Start();

                return stream.Handle;
            }
            _Initialized = true;
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
                _Decoder.Close();
            }

            _Closeproc(_StreamID);
        }
        #endregion Threading

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
                        case EStreamAction.Stop:
                            _Terminated = true;
                            break;
                        case EStreamAction.Pause:
                            Paused = true;
                            break;
                    }
                    _Fading = null;
                }
            }
        }

        public void UploadData()
        {
            _Update();

            if (_Paused)
                return;

            int queuedCount;
            bool doit = true;
            AL.GetSource(_Source, ALGetSourcei.BuffersQueued, out queuedCount);

            int processedCount = _BufferCount;
            if (queuedCount > 0)
            {
                AL.GetSource(_Source, ALGetSourcei.BuffersProcessed, out processedCount);
                doit = false;
                //Console.WriteLine("Buffers Processed on Stream " + _Source + " = " + processedCount);
                if (processedCount < 1)
                    return;
            }

            var buf = new byte[_BufferSize];

            lock (_MutexData)
            {
                while (processedCount > 0)
                {
                    if (_Data.BytesNotRead >= buf.Length)
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

                        int buffer;
                        if (!doit)
                            buffer = AL.SourceUnqueueBuffer(_Source);
                        else
                        {
                            buffer = _Buffers[queuedCount];
                            queuedCount++;
                        }

                        if (buffer != 0)
                        {
                            ALFormat alFormat = (_Format.ChannelCount == 2) ? ALFormat.Stereo16 : ALFormat.Mono16;
                            AL.BufferData(buffer, alFormat, buf, buf.Length, _Format.SamplesPerSecond);
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
            _Timer.Restart();
        }

        public void Dispose()
        {
            _EventDecode.Close();
            _EventDecode = null;
        }
    }
}