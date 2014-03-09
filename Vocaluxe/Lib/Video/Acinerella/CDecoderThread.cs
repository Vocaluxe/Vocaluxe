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
        private readonly Object _BufferMutex = new Object();

        private IntPtr _Instance = IntPtr.Zero; // acinerella instance
        private IntPtr _Videodecoder = IntPtr.Zero; // acinerella video decoder instance

        private readonly CFramebuffer _Framebuffer;
        private float _LastDecodedTime; // time of last decoded frame
        // time of last requested frame aka current should-be position (_LastDecodedTime should be >=_RequestTime)
        //HAS to be < Length
        public float RequestTime { get; private set; }
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
            RequestTime = 0f;
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

        //Sets the position to the given time discarding all decoded frames
        public void Skip(float time)
        {
            //Problem: This is not threadsave
            //Scenario: Clear buffer from main thread, decoder is in _Decode or _Copy
            //Result: Cleared buffer contains 1 old item. This is a problem when skipping back: E.g. Loop:
            //Old item has time=Length->Will not get removed
            //Workaround: Check in _FindFrame to remove first old item
            //Fix: Use mutex (TODO: Check overhead)
            lock (_BufferMutex) //Cover clear, RequestTime=, _RequestSkip=
            {
                _Framebuffer.Clear();

                RequestTime = time;
                _RequestSkip = true;
            }
            _EvNoMoreFrames.Set();
        }

        private CFramebuffer.CFrame _FindFrame(float lastTime, float now)
        {
            CFramebuffer.CFrame result = null;
            CFramebuffer.CFrame frame;
            _Framebuffer.ResetStack();
            while ((frame = _Framebuffer.Pop()) != null)
            {
                float frameEnd = frame.Time + _FrameDuration;
                if (!Loop || lastTime < now)
                {
                    //Regular case: Frame has to be between last frame and now
                    if (frameEnd > now)
                        break; // Next frames are after this one
                    if (frame.Time < lastTime)
                    {
                        frame.SetRead(); //Frame is before last one, Discard
                        continue;
                    }
                }
                else
                {
                    //Loop case: time rolled over -> Frame has to be after last one OR before now (which is after last one but rolled over)
                    if (frame.Time < lastTime && frameEnd > now)
                        break;
                }
                //Get the last(newest) possible frame and skip the rest
                if (result != null)
                {
                    //Frame is to old -> Discard
                    result.SetRead();
                }
                result = frame;
                if (_Paused)
                    break; //Just get 1 frame if paused
            }
            return result;
        }

        public EFrameState GetFrame(ref CTexture frame, float lastTime, ref float time)
        {
            EFrameState result;
            CFramebuffer.CFrame curFrame = _FindFrame(lastTime, time);

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

        //Time should be < Length
        public void SyncTime(float time)
        {
            if (_Thread == null)
                return; //Not initialized
            while (time >= Length)
                time -= Length;
            if (RequestTime - time >= 1f / 1000f)
            {
                //Jump back more than 1ms
                //Will mostly happen in loops when looped through
                Skip(time);
            }
            else if (time - RequestTime >= 1f / 1000f)
            {
                //we were more than 1 ms to slow -> Jump forward
                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (Loop && RequestTime == 0f)
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

                RequestTime = time;
            }
        }

        private bool _OpenVideoStream()
        {
            int videoStreamIndex;
            SACDecoder decoder;
            try
            {
                _Videodecoder = CAcinerella.AcCreateVideoDecoder(_Instance);
                decoder = (SACDecoder)Marshal.PtrToStructure(_Videodecoder, typeof(SACDecoder));
                videoStreamIndex = decoder.StreamIndex;
            }
            catch (Exception)
            {
                CLog.LogError("Error opening video file (can't find decoder): " + _FileName);
                return false;
            }

            if (videoStreamIndex < 0)
                return false;

            _Width = decoder.StreamInfo.VideoInfo.FrameWidth;
            _Height = decoder.StreamInfo.VideoInfo.FrameHeight;

            if (decoder.StreamInfo.VideoInfo.FramesPerSecond > 0)
                _FrameDuration = 1f / (float)decoder.StreamInfo.VideoInfo.FramesPerSecond;

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
            float skipTime = RequestTime; //Copy to variable to have consistent checks
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

            float videoTime = RequestTime;
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
                    RequestTime = 0;
                    _Skip();
                }
                else
                    _NoMoreFrames = true;
            }
        }

        //Copies a frame to the buffer but does not set it as 'written'
        //Returns true if frame data is now in buffer
        private bool _CopyDecodedFrameToBuffer()
        {
            bool result;
            var decoder = (SACDecoder)Marshal.PtrToStructure(_Videodecoder, typeof(SACDecoder));
            if (decoder.Buffer != IntPtr.Zero)
            {
                _LastDecodedTime = (float)decoder.Timecode;
                result = _Framebuffer.Put(decoder.Buffer, _LastDecodedTime);
            }
            else
                result = false;
            _FrameAvailable = false;
            return result;
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

                //Bail out if we want to skip
                if (!_RequestSkip && _FrameAvailable)
                {
                    if (!_Framebuffer.IsFull())
                    {
                        if (_CopyDecodedFrameToBuffer())
                        {
                            //Do not write to buffer if we want to skip. So check and write have to be done atomicly
                            lock (_BufferMutex)
                            {
                                if (_RequestSkip)
                                    continue; //Frame is invalid if we want to skip
                                _Framebuffer.SetWritten();
                            }
                        }
                        _WaitCount = 0;
                        Thread.Sleep(5); //Sleep for a bit to give other threads an opportunity to run
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