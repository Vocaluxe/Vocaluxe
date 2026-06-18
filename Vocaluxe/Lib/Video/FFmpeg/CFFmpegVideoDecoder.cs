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

using System.Diagnostics;
using System.IO;
using VocaluxeLib.Draw;

namespace Vocaluxe.Lib.Video.FFmpeg
{
    class CFFmpegVideoDecoder
    {
        private readonly Stopwatch _LoopTimer = new();

        private float _Gap;
        private float _LoopTime;

        private CFFmpegVideoDecoderThread _Thread;
        public float Duration { get; private set; }
        public bool Finished { get; private set; }

        public CFFmpegVideoDecoder()
        {
            Duration = 0;
            Finished = true;
        }

        public bool Loop
        {
            get => _Thread.Loop;
            set
            {
                {
                    _Thread.Loop = value;
                    if (value)
                    {
                        _LoopTime = _Thread.RequestTime;
                        _LoopTimer.Restart();
                    }
                }
            }
        }

        public bool Paused
        {
            set
            {
                if (field == value)
                {
                    return;
                }

                field = value;
                if (field)
                {
                    _LoopTimer.Stop();
                    _Thread.Pause();
                }
                else
                {
                    _LoopTimer.Start();
                    _Thread.Resume();
                }
            }
        }

        public bool LoadStream(Stream stream)
        {
            if (_Thread != null)
            {
                return false;
            }

            _Thread = new CFFmpegVideoDecoderThread();
            if (_Thread.LoadStream(stream))
            {
                Duration = (float)_Thread.Duration.TotalSeconds;
                Finished = false;
                return _Thread.Start();
            }

            _Thread = null;
            return false;
        }

        public void Close()
        {
            _Thread?.Stop();
            Duration = 0;
            Finished = true;
        }

        public bool GetFrame(ref CTextureRef frame, float time, out float videoTime)
        {
            if (Finished)
            {
                videoTime = Duration - _Gap;
                return false;
            }

            if (Loop)
            {
                time = _LoopTime + _LoopTimer.ElapsedMilliseconds / 1000f;
                if (time >= Duration)
                {
                    do
                    {
                        time -= Duration;
                    } while (time >= Duration);

                    _LoopTime = time;
                    _LoopTimer.Restart();
                }
            }
            else
            {
                time += _Gap;
            }

            _Thread.SyncTime(time);

            bool finished;
            _Thread.GetFrame(ref frame, ref time, out finished);
            videoTime = time - _Gap;

            if (finished) //Only set, not reset
            {
                Finished = true;
            }

            return frame != null;
        }

        public bool Skip(float start, float gap)
        {
            Finished = false;
            _Gap = gap;
            _Thread.Skip(start + gap);
            if (Loop)
            {
                Loop = true; //Reset loop (timer)
            }

            return true;
        }
    }
}