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
using System.IO;
using System.Threading;
using Vocaluxe.Base;
using VocaluxeLib.Draw;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.Video.FFmpeg
{
    //This class describes a thread decoding a video
    //All public methods are meant to be called from "reader" thread only
    //Most others are to be called by this thread  (_Thread instance) only!
    unsafe class CFFmpegVideoDecoderThread
    {
        private const float _LoopedRequestTime = -0.001f; //Magic const to detect if decoder looped automaticly

        private CFFmpegVideoDecoderContext _DecoderContext;

        private Thread _Thread;
        private readonly object _BufferMutex = new();

        private readonly CVideoFramebuffer _VideoFramebuffer;
        private float _LastDecodedTime; // time of last decoded frame
        private float _LastShownTime = -1f; // time if the last shown frame in s IMPORTANT: Write only in context of reader
        // time of last requested frame aka current should-be position (_LastDecodedTime should be >=_RequestTime)
        //HAS to be < Length
        public float RequestTime { get; private set; }
        private bool _Paused;

        private float _FrameDuration; // frame time in s

        private bool _RequestSkip;
        private bool _Terminated;
        private bool _FrameAvailable;
        private bool _NoMoreFrames;
        private readonly AutoResetEvent _EvWakeUp = new(false);
        private readonly AutoResetEvent _EvNoMoreFrames = new(false);
        private bool _IsSleeping;
        private int _WaitCount;
        private bool _DropSeekEnabled = true; // Used to fallback to frame skipping if seek is failing once on this file

        public TimeSpan Duration => _DecoderContext?.Duration ?? TimeSpan.Zero;
        public bool Loop { get; set; }

        public CFFmpegVideoDecoderThread()
        {
            _VideoFramebuffer = new CVideoFramebuffer(10);
            _FrameDuration = 0.02f; // Set a reasonable standard till correct value is set
        }

        // Open the stream and get the length.
        // We now use stream because it can also handle http files ;)
        public bool LoadStream(Stream sourceStream)
        {
            if (_DecoderContext != null)
            {
                CLog.Error("A stream is already opened in the thread");
                return false;
            }

            _DecoderContext = new CFFmpegVideoDecoderContext();
            if (!_DecoderContext.Initialize(sourceStream))
            {
                _Free();
                return false;
            }

            return true;
        }

        public bool Start()
        {
            if (_DecoderContext == null)
            {
                CLog.Error("No decoder was init for for this thread");
                return false;
            }

            if (_Thread != null)
            {
                CLog.Error("Tried to start a video file that is already started");
                return false;
            }

            RequestTime = 0f;
            _Thread = new Thread(_Execute) { Priority = ThreadPriority.Normal };
            _Thread.Start();
            return true;
        }

        public void Stop()
        {
            _Terminated = true;
            _EvWakeUp.Set();
            _EvNoMoreFrames.Set();
        }

        public void Pause()
        {
            if (_Paused)
            {
                return;
            }

            _Paused = true;
        }

        public void Resume()
        {
            if (!_Paused)
            {
                return;
            }

            _Paused = false;
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
                _VideoFramebuffer.Clear();
                RequestTime = time;
                _LastShownTime = time - _FrameDuration; //Set this to time to detect overflow of time in FindFrame but subtract FrameDuration so GetFrame will get the first frame
                _RequestSkip = true;
            }

            _EvNoMoreFrames.Set();
        }

        private CVideoFramebuffer.CFrame _FindFrame(float now)
        {
            CVideoFramebuffer.CFrame result = null;
            _VideoFramebuffer.ResetStack();
            while (_VideoFramebuffer.Pop() is { } frame)
            {
                //float frameEnd = frame.Time + _FrameDuration;
                var frameTime = frame.Time;

                if (frameTime > now)
                {
                    //2 Cases: all following frames are after this one, or we have a loop and 'now' wrapped over
                    //First case is if we have no loop or we did not wrap or frame is before last one (last is the case if frame is already one of the new iterations, e.g. Last=19 now=1 frame=2)
                    if (!Loop || _LastShownTime <= now || frameTime < _LastShownTime)
                    {
                        break; //Following frames (incl this one) are after now, so do not consider any of them
                    }
                }
                // ReSharper disable CompareOfFloatsByEqualityOperator
                else if (Loop && RequestTime == _LoopedRequestTime)
                    // ReSharper restore CompareOfFloatsByEqualityOperator
                {
                    //Frame time might have wrapped but now did not
                    if (frameTime < _LastShownTime && _LastShownTime <= now)
                    {
                        break; //Following frames (incl this one) are after now, so do not consider any of them
                    }
                }

                //Get the last(newest) possible frame and skip the rest
                //Frame is to old -> Discard
                result?.SetRead();

                result = frame;
                if (_Paused)
                {
                    break; //Just get 1 frame if paused otherwise a paused movie could move a bit
                }
            }

            return result;
        }

        /// <summary>
        ///     Gets a frame
        /// </summary>
        /// <param name="frame">Referenz to texture where frame should be put in (can be null, then texture is created)</param>
        /// <param name="time">Maximum start time for frame</param>
        /// <param name="finished">Set to whether there are no more frames (stream finished, no loop, future calls will always return false)</param>
        /// <returns>True if a new frame was gotten</returns>
        public bool GetFrame(ref CTextureRef frame, ref float time, out bool finished)
        {
            bool result;
            if (Math.Abs(_LastShownTime - time) < _FrameDuration && frame != null) //Check 1 frame difference
            {
                time = _LastShownTime;
                result = false;
            }
            else
            {
                var curFrame = _FindFrame(time);
                if (curFrame != null)
                {
                    if (frame == null)
                    {
                        frame = CDraw.AddTexture(_DecoderContext.FrameWidth, _DecoderContext.FrameHeight, curFrame.Data);
                    }
                    else
                    {
                        CDraw.UpdateTexture(frame, _DecoderContext.FrameWidth, _DecoderContext.FrameHeight, curFrame.Data);
                    }

                    if (!_Paused)
                    {
                        curFrame.SetRead();
                    }

                    time = curFrame.Time;
                    _LastShownTime = time;
                    result = frame != null;
                }
                else
                {
                    result = false;
                }

                if (_IsSleeping)
                {
                    _IsSleeping = false;
                    _EvWakeUp.Set();
                }
            }

            finished = _NoMoreFrames && _VideoFramebuffer.IsEmpty();

            return result;
        }

        //Time should be < Length
        public void SyncTime(float time)
        {
            if (_Thread == null)
            {
                return; //Not initialized
            }

            if (RequestTime - time >= _FrameDuration)
            {
                //Jump back more than 1 frame. To guarantee the order in the buffer use Skip() which clears the buffer
                Skip(time);
            }
            else if (time - RequestTime >= _FrameDuration)
            {
                //we were more than 1 frame to slow -> Jump forward (This is save, as the decoder will skip frames if necessary)
                // ReSharper disable CompareOfFloatsByEqualityOperator
                if (Loop && RequestTime == _LoopedRequestTime)
                    // ReSharper restore CompareOfFloatsByEqualityOperator
                {
                    //In a loop our decoder may have reset RequestTime to 0 but we want a frame from the end of the video
                    //Skipping forward is fatal as it resets the decoder to decode already decoded frames causing lags
                    //So first check if we have a valid frame in our buffer
                    _VideoFramebuffer.ResetStack();
                    while (_VideoFramebuffer.Pop() is { } frame)
                    {
                        if (frame.Time + _FrameDuration >= time)
                        {
                            return;
                        }
                    }

                    //If we don't the Length might be inaccurate (e.g. last frame ends at 19.98 but Length=20)
                    if (time >= Duration.TotalSeconds - 2 * _FrameDuration)
                    {
                        return;
                    }
                }

                RequestTime = time;
            }
        }

        private bool _OpenVideoStream()
        {
            var framePerSeconds = _DecoderContext.FrameRate;
            if (framePerSeconds > 0)
            {
                _FrameDuration = 1 / framePerSeconds;
            }

            _VideoFramebuffer.Init(_DecoderContext.FrameBufferSize);
            _FrameAvailable = false;
            return true;
        }

        //Just call this if thread is not alive
        private void _Free()
        {
            _DecoderContext?.Dispose();
        }

        // Skip to a given time (in s)
        private void _Skip()
        {
            var skipTime = RequestTime; //Copy to variable to have consistent checks
            if (skipTime < 0 || skipTime >= Duration.TotalSeconds)
            {
                skipTime = 0;
            }

            try
            {
               _DecoderContext.Seek(true, TimeSpan.FromSeconds(skipTime));
            }
            catch (Exception e)
            {
              CLog.Error("Error skipping video: " + e.Message);
            }

            _LastDecodedTime = skipTime;
            _FrameAvailable = false;
        }

        

        private void _Decode()
        {
            const int minFrameDropCount = 4;
            // With => seekThreshold frames to drop use seek instead of skip
            const int seekThreshold = 25; // 25 frames = 0.5 second with _FrameDuration = 0.02f

            if (_NoMoreFrames)
            {
                return;
            }

            var videoTime = RequestTime;
            var timeDifference = videoTime - _LastDecodedTime;

            var dropFrame = timeDifference >= (minFrameDropCount - 1) * _FrameDuration;

            var hasFrameDecoded = false;
            if (dropFrame)
            {
                var frameDropCount = (int)Math.Ceiling(timeDifference / _FrameDuration);
                if (!_DropSeekEnabled || frameDropCount < seekThreshold)
                {
                    hasFrameDecoded = _DecoderContext.DropWithSkip(frameDropCount);
                }
                else
                {
                    hasFrameDecoded = _DropWithSeek(TimeSpan.FromSeconds(videoTime), frameDropCount);
                }
            }

            if (!hasFrameDecoded)
            {
                try
                {
                    hasFrameDecoded = _DecoderContext.GetFrame();
                }
                catch (Exception ex)
                {
                    CLog.Error("Unable to get frame " + ex);
                }
            }

            if (hasFrameDecoded)
            {
                _FrameAvailable = true;
            }
            else
            {
                if (Loop)
                {
                    RequestTime = _LoopedRequestTime;
                    _Skip();
                }
                else
                {
                    _NoMoreFrames = true;
                }
            }
        }

        private bool _DropWithSeek(TimeSpan videoTime, int frameDropCount)
        {
            var hasFrameDecoded = false;
            try
            {
               hasFrameDecoded = _DecoderContext.Seek(false, videoTime);
            }
            catch (Exception)
            {
               CLog.Error("Error seeking frame");
            }

            if (!hasFrameDecoded)
            {
                // Fallback to frame skipping
                _DropSeekEnabled = false;
                hasFrameDecoded = _DecoderContext.DropWithSkip(frameDropCount);
            }

            return hasFrameDecoded;
        }

        //Copies a frame to the buffer but does not set it as 'written'
        //Returns true if frame data is now in buffer
        private bool _CopyDecodedFrameToBuffer()
        {
            _LastDecodedTime = (float)_DecoderContext.Position.TotalSeconds;
            var result = _VideoFramebuffer.Put(_DecoderContext.DecodedFrameBuffer, _LastDecodedTime);
            _FrameAvailable = false;
            return result;
        }

        private void _Execute()
        {
            if (!_OpenVideoStream())
            {
                _Free();
                Stop();
                return;
            }

            while (!_Terminated)
            {
                if (_NoMoreFrames)
                {
                    _EvNoMoreFrames.WaitOne();
                }

                if (_RequestSkip)
                {
                    _RequestSkip = false;
                    _NoMoreFrames = false;
                    _Skip();
                }

                if (!_FrameAvailable)
                {
                    _Decode();
                }

                //Bail out if we want to skip
                if (!_RequestSkip && _FrameAvailable)
                {
                    if (!_VideoFramebuffer.IsFull())
                    {
                        if (_CopyDecodedFrameToBuffer())
                        {
                            //Do not write to buffer if we want to skip. So check and write have to be done atomicly
                            lock (_BufferMutex)
                            {
                                if (_RequestSkip)
                                {
                                    continue; //Frame is invalid if we want to skip
                                }

                                _VideoFramebuffer.SetWritten();
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
                        Thread.Sleep((int)(_VideoFramebuffer.Size * _FrameDuration * 1000 / 2));
                        _WaitCount++;
                    }
                }
            }

            _Free();
        }
    }
}