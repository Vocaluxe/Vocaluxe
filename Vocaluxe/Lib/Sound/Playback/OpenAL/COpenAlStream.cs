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
using System.Threading;
using OpenTK.Audio;
using Vocaluxe.Base;
using Vocaluxe.Lib.Sound.Playback.Decoder;

namespace Vocaluxe.Lib.Sound.Playback.OpenAL
{
    class COpenAlStream : CAudioStreamBase
    {
        private const int _BufferCount = 5;
        private const int _Bufsize = 500000;
        private const int _BeginRefill = 50000;

        private int[] _Buffers;
        private byte[] _SampleBuf;
        private int _Source;
        private SFormatInfo _Format;

        private readonly Stopwatch _Timer = new Stopwatch();

        private IAudioDecoder _Decoder;
        private int _ByteCount;
        private float _BytesPerSecond;
        private bool _NoMoreData;

        private bool _FileOpened;

        private bool _Skip;

        private float _CurrentTime;
        private float _TimeCode;

        private bool _Paused;

        private CRingBuffer _Data;
        private volatile float _SetStart;
        private float _Start;
        private volatile bool _SetSkip;
        private volatile bool _Terminated;

        private Thread _DecoderThread;

        private AutoResetEvent _EventDecode = new AutoResetEvent(false);

        private readonly Object _MutexData = new Object();
        private readonly Object _MutexSyncSignals = new Object();

        public override bool IsFinished
        {
            get
            {
                lock (_MutexData)
                {
                    return _NoMoreData && _Data.BytesNotRead == 0 && Position >= Length;
                }
            }
        }

        public override float Position
        {
            get
            {
                float time = _CurrentTime + _Timer.ElapsedMilliseconds / 1000f;
                if (time > Length)
                {
                    _Timer.Stop();
                    time = Length;
                }
                return time;
            }
            set
            {
                lock (_MutexSyncSignals)
                {
                    _SetStart = value;
                    _SetSkip = true;
                }
                //Set position here in case we immediately request it, it will be reset in Update method 
                _CurrentTime = value;
                _Timer.Restart();
                _EventDecode.Set();
            }
        }

        public override bool IsPaused
        {
            get { return _Paused; }
            set
            {
                _Paused = value;
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

        public COpenAlStream(int id, string medium, bool loop, EAudioEffect effect = EAudioEffect.None) : base(id, medium, loop, effect) {}

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
            IsPaused = false;
        }

        public override void Stop()
        {
            IsPaused = true;
            Position = 0f;
        }

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

            bool ok = true;
            try
            {
                _Source = AL.GenSource();
                _Buffers = new int[_BufferCount];
                for (int i = 0; i < _BufferCount; i++)
                {
                    _Buffers[i] = AL.GenBuffer();
                    ok = ok && _Buffers[i] != 0;
                }
            }
            catch (Exception)
            {
                ok = false;
            }
            if (!ok)
            {
                Dispose();
                CLog.LogError("Error Init OpenAL Playback");
                return false;
            }


            _Decoder = new CAudioDecoderFFmpeg();
            if (!_Decoder.Open(_Medium))
            {
                Dispose();
                CLog.LogError("Error opening audio file: " + _Medium);
                return false;
            }
            _Format = _Decoder.GetFormatInfo();
            if (_Format.SamplesPerSecond == 0)
            {
                Dispose();
                CLog.LogError("Error Init OpenAL Playback (samples=0)");
                return false;
            }

            Length = _Decoder.GetLength();

            _ByteCount = 2 * _Format.ChannelCount;
            _BytesPerSecond = _Format.SamplesPerSecond * _ByteCount;

            _CurrentTime = 0f;
            _TimeCode = 0f;
            _Timer.Reset();
            _Data = new CRingBuffer(_Bufsize);
            _NoMoreData = false;
            _SampleBuf = new byte[(int)CConfig.Config.Sound.AudioBufferSize];
            //From now on closing the driver and the decoder is handled by the thread ONLY!

            _DecoderThread = new Thread(_Execute) {Priority = ThreadPriority.Normal, Name = Path.GetFileName(_Medium)};
            _DecoderThread.Start();

            _FileOpened = true;
            return true;
        }

        #region Threading
        private void _DoSkip()
        {
            _Decoder.SetPosition(_Start);
            _TimeCode = _Start;

            lock (_MutexData)
            {
                _Data.Reset();
                _NoMoreData = false;
            }
        }

        private void _Execute()
        {
            while (!_Terminated)
            {
                lock (_MutexSyncSignals)
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

            lock (_MutexData)
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

            lock (_MutexData)
            {
                _Data.Write(buffer);
                _TimeCode = timecode;
                if (_Data.BytesNotRead < _BeginRefill)
                    _EventDecode.Set();
            }
        }

        private void _DoFree()
        {
            if (_Source != 0)
            {
                AL.SourceStop(_Source);
                if (_Buffers != null)
                {
                    AL.DeleteBuffers(_Buffers);
                    _Buffers = null;
                }
                AL.DeleteSource(_Source);
                _Source = 0;
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
            _SampleBuf = null;
            if (_EventDecode != null)
            {
                _EventDecode.Close();
                _EventDecode = null;
            }
            if (_CloseStreamListener != null)
                _CloseStreamListener.OnCloseStream(this);
        }
        #endregion Threading

        public override void Update()
        {
            base.Update();

            if (_Paused || _Terminated || IsFinished)
                return;

            int queuedCount;
            bool useQueuedBuffer = false;
            AL.GetSource(_Source, ALGetSourcei.BuffersQueued, out queuedCount);

            int freeBufferCt = _BufferCount;
            if (queuedCount > 0)
            {
                AL.GetSource(_Source, ALGetSourcei.BuffersProcessed, out freeBufferCt);
                useQueuedBuffer = true;
                //Console.WriteLine("Buffers Processed on Stream " + _Source + " = " + processedCount);
                if (freeBufferCt < 1)
                    return;
            }

            lock (_MutexData)
            {
                queuedCount = 0;
                for (int j = 0; j < freeBufferCt; j++)
                {
                    if (_Data.BytesNotRead < _SampleBuf.Length && !(_NoMoreData && _Data.BytesNotRead > 0))
                        break;
                    _Data.Read(_SampleBuf);

                    float volume = Volume * VolumeMax;
                    //We want to scale all values. No matter how many channels we have (_ByteCount=2 or 4) we have short values
                    //So just process 2 bytes a time
                    for (int i = 0; i < _SampleBuf.Length; i += 2)
                    {
                        byte[] b = BitConverter.GetBytes((Int16)(BitConverter.ToInt16(_SampleBuf, i) * volume));
                        _SampleBuf[i] = b[0];
                        _SampleBuf[i + 1] = b[1];
                    }

                    int buffer = useQueuedBuffer ? AL.SourceUnqueueBuffer(_Source) : _Buffers[queuedCount];


                    if (buffer != 0)
                    {
                        ALFormat alFormat = (_Format.ChannelCount == 2) ? ALFormat.Stereo16 : ALFormat.Mono16;
                        AL.BufferData(buffer, alFormat, _SampleBuf, _SampleBuf.Length, _Format.SamplesPerSecond);
                        AL.SourceQueueBuffer(_Source, buffer);
                    }
                    queuedCount++;
                }
            }

            float latency = CConfig.Config.Sound.AudioLatency / 1000f + queuedCount * _SampleBuf.Length / _BytesPerSecond + 0.1f;
            _CurrentTime = _TimeCode - _Data.BytesNotRead / _BytesPerSecond - latency;
            _Timer.Restart();
        }
    }
}