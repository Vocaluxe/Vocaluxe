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

namespace Vocaluxe.Lib.Sound.Playback.PortAudio
{
    class CPt1
    {
        private float _CurrentTime;
        private float _OldTime;

        private readonly Stopwatch _Timer;
        private readonly float _K;
        private readonly float _T;

        public float Time
        {
            get
            {
                double nanosecPerTick = (1000.0 * 1000.0 * 1000.0) / Stopwatch.Frequency;
                long ticks = _Timer.ElapsedTicks;
                float dt = _Timer.ElapsedMilliseconds / 1000f;

                if (Stopwatch.IsHighResolution && ticks != 0)
                    dt = (float)(ticks * nanosecPerTick / 1000000000.0);

                return _CurrentTime + dt;
            }

            set
            {
                if (value < 0f)
                    return;

                _CurrentTime = value;
                _OldTime = value;
                _Timer.Restart();
            }
        }

        public CPt1(float currentTime, float k, float t)
        {
            _CurrentTime = currentTime;
            _OldTime = currentTime;

            _Timer = new Stopwatch();
            _K = k;
            _T = t;
        }

        public float Update(float newTime)
        {
            _Timer.Stop();

            double nanosecPerTick = (1000.0 * 1000.0 * 1000.0) / Stopwatch.Frequency;
            long ticks = _Timer.ElapsedTicks;
            float dt = _Timer.ElapsedMilliseconds / 1000f;

            if (Stopwatch.IsHighResolution && ticks != 0)
                dt = (float)(ticks * nanosecPerTick / 1000000000.0);

            float ts = 0f;
            if (dt > 0)
                ts = 1 / (_T / dt + 1);

            _CurrentTime = ts * (_K * newTime - _OldTime) + _OldTime;
            _OldTime = _CurrentTime;

            _Timer.Restart();

            return _CurrentTime;
        }

        public void Pause()
        {
            _Timer.Stop();
        }

        public void Resume()
        {
            _Timer.Start();
        }
    }
}