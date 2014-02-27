using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Vocaluxe.Base;
using VocaluxeLib.Draw;

namespace Vocaluxe.Lib.Video.Acinerella
{
    //This class describes a thread decoding a video
    //All public methods are meant to be called from "reader" thread only
    //Most others are to be called by this thread  (_Thread instance) only!
    class CDecoderThread
    {
        public enum EFrameState
        {
            ValidFrame,
            InvalidFrame,
            EndFrame
        }

        private Thread _Thread;

        private IntPtr _Instance = IntPtr.Zero; // acinerella instance
        private IntPtr _Videodecoder = IntPtr.Zero; // acinerella video decoder instance

        private readonly CFramebuffer _Framebuffer;
        private float _LastDecodedTime; // time of last decoded frame
        // time of last requested frame aka current should-be position (_LastDecodedTime should be >=_RequestTime)
        //HAS to be < Length
        public float _RequestTime { get; private set; }
        private bool _Paused;

        private String _FileName;
        private float _FrameDuration; // frame time in s
        private int _Width;
        private int _Height;

        private bool _RequestSkip;
        private bool _Terminated;
        private bool _FrameAvailable;
        private bool _NoMoreFrames;
        private readonly AutoResetEvent _EvWakeUp = new AutoResetEvent(false);
        private readonly AutoResetEvent _EvPause = new AutoResetEvent(false);
        private readonly AutoResetEvent _EvNoMoreFrames = new AutoResetEvent(false);
        private bool _IsSleeping;
        private int _WaitCount;

        public float Length { get; private set; }
        public bool Loop { get; set; }

        public CDecoderThread()
        {
            _Framebuffer = new CFramebuffer(10);
        }

        //Open the file and get the length.
        public bool LoadFile(String fileName)
        {
            _FileName = fileName;
            try
            {
                _Instance = CAcinerella.AcInit();
                CAcinerella.AcOpen2(_Instance, fileName);

                var instance = (SACInstance)Marshal.PtrToStructure(_Instance, typeof(SACInstance));
                Length = instance.Info.Duration / 1000f;
                bool ok = instance.Opened && Length > 0.001f;
                if (ok)
                    return true;
                _Free();
            }
            catch (Exception) {}
            CLog.LogError("Error opening video file: " + _FileName);
            _Instance = IntPtr.Zero;
            return false;
        }

        public bool Start()
        {
            if (_Instance == IntPtr.Zero)
            {
                CLog.LogError("Tried to start a video file that is not open: " + _FileName);
                return false;
            }
            if (_Thread != null)
            {
                CLog.LogError("Tried to start a video file that is already started: " + _FileName);
                return false;
            }
            _RequestTime = 0f;
            _Thread = new Thread(_Execute) {Priority = ThreadPriority.Normal, Name = Path.GetFileName(_FileName)};
            _Thread.Start();
            return true;
        }

        public void Stop()
        {
            _Terminated = true;
            _EvPause.Set();
            _EvWakeUp.Set();
            _EvNoMoreFrames.Set();
        }

        public void Pause()
        {
            if (_Paused)
                return;
            _EvPause.Reset();
            _Paused = true;
        }

        public void Resume()
        {
            if (!_Paused)
                return;
            _Paused = false;
            _EvPause.Set();
        }

        public void Skip(float time)
        {
            //TODO: Problem: This is not threadsave
            //Scenario: Clear buffer from main thread, decoder is in _Decode or _Copy
            //Result: Cleared buffer contains 1 old item. This is a problem when skipping back: E.g. Loop:
            //Old item has time=Length->Will not get removed
            //Workaround: Check in _FindFrame to remove first old item
            _Framebuffer.Clear();


            _RequestTime = time;
            _RequestSkip = true;
            _EvNoMoreFrames.Set();
        }

        private CFramebuffer.CFrame _FindFrame(float now)
        {
            CFramebuffer.CFrame result = null;
            float maxEnd = now - _FrameDuration * 2; //Get only frames younger than 2
            CFramebuffer.CFrame frame;
            bool first = true;
            _Framebuffer.ResetStack();
            while ((frame = _Framebuffer.Pop()) != null)
            {
                float frameEnd = frame.Time + _FrameDuration;
                //Don't show frames that are shown during or after now
                if (frameEnd >= now)
                {
                    //All following frames are after that one -> BREAK
                    if (!first)
                        break;
                    //Well... Should be. Workaround:
                    CFramebuffer.CFrame nextFrame = _Framebuffer.Pop();
                    if (nextFrame == null || nextFrame.Time > frame.Time)
                        break;
                    frame.SetRead(); //Discard wrong frame
                    frame = nextFrame;
                    //Replicate calculations
                    frameEnd = frame.Time + _FrameDuration;
                    if (frameEnd >= now)
                        break;
                }
                first = false;
                //Get the last(newest) possible frame and skip the rest to force in-order showing
                if (frameEnd <= maxEnd)
                {
                    //Frame is to old -> Discard
                    frame.SetRead();
                }
                else
                {
                    maxEnd = frameEnd;
                    result = frame;
                }
            }
            return result;
        }

        //Time should be < Length
        public void SyncTime(float time)
        {
            if (_Thread == null)
                return; //Not initialized
            while (time >= Length)
                time -= Length;
            if (_RequestTime - time >= 1f / 1000f)
            {
                //Jump back more than 1ms
                //Will mostly happen in loops when looped through
                Skip(time);
            }
            else if (time - _RequestTime >= 1f / 1000f)
            {
                //we were more than 1 ms to slow -> Jump forward
                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (Loop && _RequestTime == 0f)
                    // ReSharper restore CompareOfFloatsByEqualityOperator
                {
                    //Check if we had a loop
                    _Framebuffer.ResetStack();
                    CFramebuffer.CFrame frame;
                    while ((frame = _Framebuffer.Pop()) != null)
                    {
                        if (frame.Time + _FrameDuration >= time)
                            return;
                    }
                }

                _RequestTime = time;
            }
        }

        public EFrameState GetFrame(ref CTexture frame, ref float time)
        {
            EFrameState result;
            CFramebuffer.CFrame curFrame = _FindFrame(time);

            if (curFrame != null)
            {
                CDraw.UpdateOrAddTexture(ref frame, _Width, _Height, curFrame.Data);
                if (!_Paused)
                    curFrame.SetRead();
                time = curFrame.Time;
                result = (frame != null) ? EFrameState.ValidFrame : EFrameState.InvalidFrame;
            }
            else
            {
                if (_NoMoreFrames && _Framebuffer.IsEmpty())
                    result = EFrameState.EndFrame;
                else
                    result = EFrameState.InvalidFrame;
            }
            if (_IsSleeping)
            {
                _IsSleeping = false;
                _EvWakeUp.Set();
            }
            return result;
        }

        private bool _OpenVideoStream()
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
                CLog.LogError("Error opening video file (can't find decoder): " + _FileName);
                return false;
            }

            if (videoStreamIndex < 0)
                return false;

            _Width = videodecoder.StreamInfo.VideoInfo.FrameWidth;
            _Height = videodecoder.StreamInfo.VideoInfo.FrameHeight;

            if (videodecoder.StreamInfo.VideoInfo.FramesPerSecond > 0)
                _FrameDuration = 1f / (float)videodecoder.StreamInfo.VideoInfo.FramesPerSecond;

            _Framebuffer.Init(_Width * _Height * 4);
            _FrameAvailable = false;
            return true;
        }

        //Just call this if thread is not alive
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

        // Skip to a given time (in s)
        private void _Skip()
        {
            float skipTime = _RequestTime;
            if (skipTime < 0 || skipTime >= Length)
                skipTime = 0;

            try
            {
                CAcinerella.AcSeek(_Videodecoder, -1, (Int64)(skipTime * 1000f));
            }
            catch (Exception e)
            {
                CLog.LogError("Error seeking video file \"" + _FileName + "\": " + e.Message);
            }
            _LastDecodedTime = skipTime;

            _FrameAvailable = false;
        }

        private void _Decode()
        {
            const int minFrameDropCount = 4;

            if (_Paused || _NoMoreFrames)
                return;

            float videoTime = _RequestTime;
            float timeDifference = videoTime - _LastDecodedTime;

            bool dropFrame = timeDifference >= (minFrameDropCount - 1) * _FrameDuration;

            bool hasFrameDecoded = false;
            if (dropFrame)
            {
                try
                {
                    var frameDropCount = (int)(timeDifference / _FrameDuration);
                    hasFrameDecoded = CAcinerella.AcSkipFrames(_Instance, _Videodecoder, frameDropCount);
                }
                catch (Exception)
                {
                    CLog.LogError("Error AcSkipFrame " + _FileName);
                }
            }

            if (!hasFrameDecoded)
            {
                try
                {
                    hasFrameDecoded = CAcinerella.AcGetFrame(_Instance, _Videodecoder);
                }
                catch (Exception)
                {
                    CLog.LogError("Error AcGetFrame " + _FileName);
                }
            }
            if (hasFrameDecoded)
                _FrameAvailable = true;
            else
            {
                if (Loop)
                {
                    _RequestTime = 0;
                    _Skip();
                }
                else
                    _NoMoreFrames = true;
            }
        }

        private void _CopyDecodedFrameToBuffer()
        {
            var videodecoder = (SACDecoder)Marshal.PtrToStructure(_Videodecoder, typeof(SACDecoder));
            if (videodecoder.Buffer != IntPtr.Zero)
            {
                _LastDecodedTime = (float)videodecoder.Timecode;
                _Framebuffer.Put(videodecoder.Buffer, _LastDecodedTime);
            }
            _FrameAvailable = false;
        }

        private void _Execute()
        {
            if (!_OpenVideoStream())
            {
                _Free();
                Stop();
            }

            while (!_Terminated)
            {
                if (_Paused)
                    _EvPause.WaitOne();
                if (_NoMoreFrames)
                    _EvNoMoreFrames.WaitOne();
                if (_RequestSkip)
                {
                    _RequestSkip = false;
                    _NoMoreFrames = false;
                    _Skip();
                }

                if (!_FrameAvailable)
                    _Decode();

                if (_FrameAvailable)
                {
                    if (!_Framebuffer.IsFull())
                    {
                        _CopyDecodedFrameToBuffer();
                        _WaitCount = 0;
                        Thread.Sleep(5);
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
    }
}