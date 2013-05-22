using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Vocaluxe.Base;
using VocaluxeLib.Draw;

namespace Vocaluxe.Lib.Video.Acinerella
{
    class CDecoder
    {
        private IntPtr _Instance = IntPtr.Zero; // acinerella instance
        private IntPtr _Videodecoder = IntPtr.Zero; // acinerella video decoder instance

        private readonly Stopwatch _LoopTimer = new Stopwatch();
        private readonly Thread _Thread;
        //AutoResetEvent EventDecode = new AutoResetEvent(false);
        private readonly Object _MutexFramebuffer = new Object();
        private readonly SFrameBuffer[] _FrameBuffer = new SFrameBuffer[5];
        private readonly Object _MutexSyncSignals = new Object();

        private bool _FileOpened;

        private float _FrameDuration; // frame time in s
        private float _LastDecodedTime; // time of last decoded frame
        private float _LastShownTime; // current video position
        private bool _BufferFull; // buffer is full, waiting for free frame slot

        private float _Time;
        private float _Gap;
        private float _Start;
        private bool _Loop;

        private float _SetTime;
        private float _SetGap;
        private float _SetStart;
        private bool _SetSkip;

        private bool _Terminated;
        private bool _FrameAvailable;
        private bool _NoMoreFrames;

        private float _SkipTime; // = Start + VideoGap

        private string _FileName; // current video file name
        private int _Width;
        private int _Height;
        private bool _Paused;

        public CDecoder()
        {
            _Thread = new Thread(_Execute);
        }

        public float Length { get; private set; }

        public bool Paused
        {
            set
            {
                lock (_MutexSyncSignals)
                {
                    _Paused = value;
                    if (_Paused)
                        _LoopTimer.Stop();
                    else
                        _LoopTimer.Start();
                }
            }
        }

        public bool Loop { private get; set; }

        public bool Finished { get; private set; }

        public bool Open(string fileName)
        {
            if (_FileOpened)
                return false;

            if (!File.Exists(fileName))
                return false;

            _FileName = fileName;

            //Do this here as one may want to get the length afterwards!
            if (!_DoOpen())
                return false;

            _FileOpened = true;

            _Thread.Priority = ThreadPriority.Normal;
            _Thread.Name = Path.GetFileName(fileName);
            _Thread.Start();
            return true;
        }

        public void Close()
        {
            _Terminated = true;
            Length = 0;
        }

        public bool GetFrame(ref CTexture frame, float time, out float videoTime)
        {
            if (_Paused)
            {
                videoTime = 0;
                return false;
            }

            if (Loop)
            {
                lock (_MutexSyncSignals)
                {
                    _SetTime += _LoopTimer.ElapsedMilliseconds / 1000f;
                    _LoopTimer.Restart();
                }
            }
            else if (Math.Abs(_SetTime - time) > float.Epsilon)
            {
                lock (_MutexSyncSignals)
                {
                    _SetTime = time;
                }
            }
            else
            {
                videoTime = 0;
                return false;
            }
            _UploadNewFrame(ref frame);
            videoTime = _LastShownTime;
            return frame != null;
        }

        public bool Skip(float start, float gap)
        {
            lock (_MutexSyncSignals)
            {
                _SetStart = start;
                _SetGap = gap;
                _SetSkip = true;
                _NoMoreFrames = false;
                Finished = false;
            }
            return true;
        }

        #region Threading
        private void _Skip()
        {
            _SkipTime = _Start + _Gap;

            if (_SkipTime < 0)
                _SkipTime = 0;

            try
            {
                CAcinerella.AcSeek(_Videodecoder, (_LastDecodedTime > _SkipTime) ? -1 : 0, (Int64)(_SkipTime * 1000f));
            }
            catch (Exception e)
            {
                CLog.LogError("Error seeking video file \"" + _FileName + "\": " + e.Message);
            }
            _LastDecodedTime = _SkipTime;

            lock (_MutexSyncSignals)
            {
                _LastShownTime = _LastDecodedTime;
            }

            lock (_MutexFramebuffer)
            {
                for (int i = 0; i < _FrameBuffer.Length; i++)
                    _FrameBuffer[i].Displayed = true;
            }

            _BufferFull = false;
            _FrameAvailable = false;
        }

        private void _Execute()
        {
            while (!_Terminated)
            {
                bool skip;
                lock (_MutexSyncSignals)
                {
                    _Time = _SetTime;
                    skip = _SetSkip;
                    _SetSkip = false;
                    _Gap = _SetGap;
                    _Start = _SetStart;
                    _Loop = Loop;
                }

                if (skip)
                    _Skip();

                if (!_FrameAvailable)
                    _Decode();

                if (_FrameAvailable && !_BufferFull)
                    _Copy();
                Thread.Sleep(1);
            }

            _Free();
        }

        private bool _DoOpen()
        {
            bool ok;
            try
            {
                _Instance = CAcinerella.AcInit();
                CAcinerella.AcOpen2(_Instance, _FileName);

                var instance = (SACInstance)Marshal.PtrToStructure(_Instance, typeof(SACInstance));
                Length = instance.Info.Duration / 1000f;
                ok = instance.Opened;
            }
            catch (Exception)
            {
                ok = false;
            }

            if (!ok)
            {
                CLog.LogError("Error opening video file: " + _FileName);
                return false;
            }

            int videoStreamIndex;
            SACDecoder videodecoder;
            try
            {
                _Videodecoder = CAcinerella.AcCreateVideoDecoder(_Instance);
                videodecoder = (SACDecoder)Marshal.PtrToStructure(_Videodecoder, typeof(SACDecoder));
                videoStreamIndex = videodecoder.StreamIndex;
            }
            catch (Exception)
            {
                CLog.LogError("Error opening video file (can't find decoder): " + _FileName);
                _Free();
                return false;
            }

            if (videoStreamIndex < 0)
            {
                _Free();
                return false;
            }

            _Width = videodecoder.StreamInfo.VideoInfo.FrameWidth;
            _Height = videodecoder.StreamInfo.VideoInfo.FrameHeight;

            if (videodecoder.StreamInfo.VideoInfo.FramesPerSecond > 0)
                _FrameDuration = 1f / (float)videodecoder.StreamInfo.VideoInfo.FramesPerSecond;

            _LastDecodedTime = 0f;
            _Time = 0f;

            for (int i = 0; i < _FrameBuffer.Length; i++)
            {
                _FrameBuffer[i].Displayed = true;
                _FrameBuffer[i].Data = new byte[_Width * _Height * 4];
            }
            _BufferFull = false;
            _FrameAvailable = false;
            return true;
        }

        private void _Decode()
        {
            const int framedropcount = 4;

            if (_Paused || _NoMoreFrames || _BufferFull)
                return;

            float myTime = _Time + _Gap;
            float timeDifference = myTime - _LastDecodedTime;

            bool dropFrame = timeDifference >= (framedropcount - 1) * _FrameDuration;

            if (_Terminated)
                return;

            bool frameFinished = false;
            if (dropFrame)
            {
                try
                {
                    frameFinished = CAcinerella.AcSkipFrames(_Instance, _Videodecoder, framedropcount - 1);
                }
                catch (Exception)
                {
                    CLog.LogError("Error AcSkipFrame " + _FileName);
                }
            }

            if (!frameFinished)
            {
                try
                {
                    frameFinished = CAcinerella.AcGetFrame(_Instance, _Videodecoder);
                }
                catch (Exception)
                {
                    CLog.LogError("Error AcGetFrame " + _FileName);
                }
            }
            if (frameFinished)
                _FrameAvailable = true;
            else
            {
                if (_Loop)
                {
                    lock (_MutexSyncSignals)
                    {
                        _Start = 0f;
                        _Gap = 0f;
                        _SetTime = 0f;
                    }
                    _Skip();
                }
                else
                    _NoMoreFrames = true;
            }
        }

        private void _Copy()
        {
            lock (_MutexFramebuffer)
            {
                int num = -1;
                for (int i = 0; i < _FrameBuffer.Length; i++)
                {
                    if (_FrameBuffer[i].Displayed)
                    {
                        num = i;
                        break;
                    }
                }

                if (num == -1)
                    _BufferFull = true; //Should not happen
                else
                {
                    var videodecoder = (SACDecoder)Marshal.PtrToStructure(_Videodecoder, typeof(SACDecoder));

                    _LastDecodedTime = (float)videodecoder.Timecode;
                    _FrameBuffer[num].Time = _LastDecodedTime;

                    if (videodecoder.Buffer != IntPtr.Zero)
                    {
                        Marshal.Copy(videodecoder.Buffer, _FrameBuffer[num].Data, 0, _Width * _Height * 4);
                        _FrameBuffer[num].Displayed = false;
                    }

                    _BufferFull = true;
                    for (int i = num + 1; i < _FrameBuffer.Length; i++)
                    {
                        if (_FrameBuffer[i].Displayed)
                        {
                            _BufferFull = false;
                            break;
                        }
                    }

                    _FrameAvailable = false;
                }
            }
        }

        private void _UploadNewFrame(ref CTexture frame)
        {
            lock (_MutexFramebuffer)
            {
                int num = _FindFrame();

                if (num >= 0)
                {
                    CDraw.UpdateOrAddTexture(ref frame, _Width, _Height, _FrameBuffer[num].Data);

                    lock (_MutexSyncSignals)
                    {
                        _LastShownTime = _FrameBuffer[num].Time;
                    }
                    Finished = false;
                }
                else
                {
                    if (_NoMoreFrames)
                        Finished = true;
                }
            }
        }

        private int _FindFrame()
        {
            int result = -1;
            float diff = 10000000f;

            for (int i = 0; i < _FrameBuffer.Length; i++)
            {
                if (_FrameBuffer[i].Displayed)
                    continue;

                // Time from frame till now (<0 --> Frame is not yet to be shown)
                float td = _SetTime + _Gap - _FrameBuffer[i].Time;

                if (td > _FrameDuration * 2f)
                {
                    _FrameBuffer[i].Displayed = true;
                    _BufferFull = false;
                }
                else if (td < diff && td > _FrameDuration)
                {
                    diff = Math.Abs(td);
                    result = i;
                }
            }

            if (result != -1)
            {
                _FrameBuffer[result].Displayed = true;
                _BufferFull = false;
            }

            return result;
        }

        private void _Free()
        {
            if (_Videodecoder != IntPtr.Zero)
                CAcinerella.AcFreeDecoder(_Videodecoder);

            if (_Instance != IntPtr.Zero)
            {
                CAcinerella.AcClose(_Instance);
                CAcinerella.AcFree(_Instance);
            }
        }
        #endregion Threading
    }
}