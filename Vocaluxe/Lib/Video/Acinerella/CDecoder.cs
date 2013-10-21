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
        private readonly Object _MutexFramebuffer = new Object();
        private readonly CFramebuffer _Framebuffer;
        private readonly Object _MutexSyncSignals = new Object();

        private bool _FileOpened;

        private float _FrameDuration; // frame time in s
        private float _LastDecodedTime; // time of last decoded frame
        private float _LastShownTime; // time if the last shown frame in s

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
        private readonly AutoResetEvent _EvWakeUp = new AutoResetEvent(false);
        private bool _IsSleeping;
        private int _WaitCount;

        private float _SkipTime; // = Start + VideoGap

        private string _FileName; // current video file name
        private int _Width;
        private int _Height;
        private bool _Paused;

        public CDecoder()
        {
            _Thread = new Thread(_Execute);
            _Framebuffer = new CFramebuffer(10);
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
            if (Loop)
            {
                lock (_MutexSyncSignals)
                {
                    _SetTime += _LoopTimer.ElapsedMilliseconds / 1000f;
                    _LoopTimer.Restart();
                }
            }
            else if (Math.Abs(_SetTime - time) >= 1f / 1000f) //Check 1 ms difference
            {
                lock (_MutexSyncSignals)
                {
                    _SetTime = time;
                }
            }
            else if (Math.Abs(_LastShownTime - time) < 1f / 1000 && frame != null) //Check 1 ms difference
            {
                videoTime = _LastShownTime;
                return true;
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
                CAcinerella.AcSeek(_Videodecoder, -1, (Int64)(_SkipTime * 1000f));
            }
            catch (Exception e)
            {
                CLog.LogError("Error seeking video file \"" + _FileName + "\": " + e.Message);
            }
            _LastDecodedTime = _SkipTime;

            lock (_MutexFramebuffer)
            {
                _Framebuffer.Clear();
            }

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

                if (_FrameAvailable)
                {
                    if (!_Framebuffer.IsFull())
                    {
                        _Copy();
                        _WaitCount = 0;
                        Thread.Sleep(1);
                    }
                    else if (_WaitCount > 3)
                    {
                        _IsSleeping = true;
                        _EvWakeUp.WaitOne();
                    }
                    else
                    {
                        Thread.Sleep((int)((_Framebuffer.Size - 2) * _FrameDuration * 1000));
                        _WaitCount++;
                    }
                }
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
            _SetTime = 0f;

            _Framebuffer.Init(_Width * _Height * 4);
            _FrameAvailable = false;
            return true;
        }

        private void _Decode()
        {
            const int minFrameDropCount = 4;

            if (_Paused || _NoMoreFrames)
                return;

            float videoTime = _Time + _Gap;
            float timeDifference = videoTime - _LastDecodedTime;

            bool dropFrame = timeDifference >= (minFrameDropCount - 1) * _FrameDuration;

            if (_Terminated)
                return;

            bool frameFinished = false;
            if (dropFrame)
            {
                try
                {
                    if (videoTime >= Length)
                    {
                        if (_Loop)
                        {
                            lock (_MutexSyncSignals)
                            {
                                _Start = 0;
                                _Gap = 0f;
                                _SetTime = 0;
                            }
                            _Skip();
                        }
                        else
                        {
                            _NoMoreFrames = true;
                            return;
                        }
                    }
                    else
                    {
                        var frameDropCount = (int)(timeDifference / _FrameDuration);
                        frameFinished = CAcinerella.AcSkipFrames(_Instance, _Videodecoder, frameDropCount);
                    }
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
            var videodecoder = (SACDecoder)Marshal.PtrToStructure(_Videodecoder, typeof(SACDecoder));
            if (videodecoder.Buffer == IntPtr.Zero)
                return;
            _LastDecodedTime = (float)videodecoder.Timecode;
            _Framebuffer.Put(videodecoder.Buffer, _LastDecodedTime);
            _FrameAvailable = false;
        }

        private void _UploadNewFrame(ref CTexture frame)
        {
            lock (_MutexFramebuffer)
            {
                CFrame curFrame = _FindFrame();

                if (curFrame != null)
                {
                    CDraw.UpdateOrAddTexture(ref frame, _Width, _Height, curFrame.Data);

                    lock (_MutexSyncSignals)
                    {
                        _LastShownTime = curFrame.Time;
                    }
                }
                else
                {
                    if (_NoMoreFrames && _Framebuffer.IsEmpty())
                        Finished = true;
                }
            }
            if (!Finished && _IsSleeping)
            {
                _IsSleeping = false;
                _EvWakeUp.Set();
            }
        }

        private CFrame _FindFrame()
        {
            CFrame result = null;
            float now = _SetTime + _Gap;
            float maxEnd = now - _FrameDuration * 2; //Get only frames younger than 2
            CFrame frame;
            bool paused = _Paused;
            while ((frame = _Framebuffer.Get()) != null)
            {
                float frameEnd = frame.Time + _FrameDuration;
                //Don't show frames that are shown during or after now
                if (frameEnd >= now)
                    break; //All following frames are after that one
                if (paused) // Don't modify anything
                    break;
                _Framebuffer.SetRead();
                //Get the last(newest) possible frame and skip the rest to force in-order showing
                if (frameEnd <= maxEnd)
                    continue;
                maxEnd = frameEnd;
                result = frame;
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