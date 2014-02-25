using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Vocaluxe.Base;
using VocaluxeLib.Draw;

namespace Vocaluxe.Lib.Video.Acinerella
{
    class CDecoderThread
    {
        private readonly Thread _Thread;
        private readonly Object _MutexFramebuffer = new Object();
        private bool isStarted;

        private IntPtr _Instance; // acinerella instance
        private IntPtr _Videodecoder = IntPtr.Zero; // acinerella video decoder instance

        private readonly CFramebuffer _Framebuffer;
        private float _LastDecodedTime; // time of last decoded frame

        private float _FrameDuration; // frame time in s
        private int _Width;
        private int _Height;

        private bool _Terminated;
        private bool _FrameAvailable;
        private bool _NoMoreFrames;
        private readonly AutoResetEvent _EvWakeUp = new AutoResetEvent(false);
        private bool _IsSleeping;
        private int _WaitCount;

        public CDecoderThread(String name, IntPtr instance)
        {
            _Framebuffer = new CFramebuffer(10);
            _Instance = instance;
            _Thread = new Thread(_Execute) {Priority = ThreadPriority.Normal, Name = name};
            _Thread.Start();
        }

        public void Stop()
        {
            _Terminated = true;
        }

        private bool _DoOpen()
        {
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
                CLog.LogError("Error opening video file (can't find decoder): " + _Thread.Name);
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
            if (!_DoOpen())
                Stop();
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
                    CLog.LogError("Error AcSkipFrame " + _Thread.Name);
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
                    CLog.LogError("Error AcGetFrame " + _Thread.Name);
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
    }
}