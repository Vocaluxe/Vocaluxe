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
                _Timer.Reset();
                _Timer.Start();
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