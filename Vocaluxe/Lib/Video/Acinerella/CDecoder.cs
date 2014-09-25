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

namespace Vocaluxe.Lib.Video.Acinerella
{
    class CDecoder
    {
        private readonly Stopwatch _LoopTimer = new Stopwatch();

        private float _Gap;
        private bool _Paused;
        private float _LoopTime;

        private CDecoderThread _Thread;
        public float Length { get; private set; }
        public bool Finished { get; private set; }

        public CDecoder()
        {
            Length = 0;
            Finished = true;
        }

        public bool Loop
        {
            get { return _Thread.Loop; }
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
                if (_Paused == value)
                    return;
                _Paused = value;
                if (_Paused)
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

        public bool Open(string fileName)
        {
            if (_Thread != null)
                return false;

            if (!File.Exists(fileName))
                return false;

            _Thread = new CDecoderThread();
            if (_Thread.LoadFile(fileName))
            {
                Length = _Thread.Length;
                Finished = false;
                return _Thread.Start();
            }
            _Thread = null;
            return false;
        }

        public void Close()
        {
            if (_Thread != null)
                _Thread.Stop();
            Length = 0;
            Finished = true;
        }

        public bool GetFrame(ref CTextureRef frame, float time, out float videoTime)
        {
            if (Finished)
            {
                videoTime = Length - _Gap;
                return false;
            }
            if (Loop)
            {
                time = _LoopTime + _LoopTimer.ElapsedMilliseconds / 1000f;
                if (time >= Length)
                {
                    do
                    {
                        time -= Length;
                    } while (time >= Length);
                    _LoopTime = time;
                    _LoopTimer.Restart();
                }
            }
            else
                time += _Gap;
            _Thread.SyncTime(time);

            bool finished;
            _Thread.GetFrame(ref frame, ref time, out finished);
            videoTime = time - _Gap;

            if (finished) //Only set, not reset
                Finished = true;
            return frame != null;
        }

        public bool Skip(float start, float gap)
        {
            Finished = false;
            _Gap = gap;
            _Thread.Skip(start + gap);
            if (Loop)
                Loop = true; //Reset loop (timer)
            return true;
        }
    }
}