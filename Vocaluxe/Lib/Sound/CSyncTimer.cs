using System;
using System.Diagnostics;

namespace Vocaluxe.Lib.Sound
{
    class CSyncTimer
    {
        private readonly CPt1 _ExternTime;
        private readonly Stopwatch _Timer;
        private float _SetValue;

        public float Time
        {
            get
            {
                double nanosecPerTick = (1000.0 * 1000.0 * 1000.0) / Stopwatch.Frequency;
                long ticks = _Timer.ElapsedTicks;
                float dt = _Timer.ElapsedMilliseconds / 1000f;

                if (Stopwatch.IsHighResolution && ticks != 0)
                    dt = (float)(ticks * nanosecPerTick / 1000000000.0);

                return _SetValue + dt;
            }

            set
            {
                _ExternTime.Time = value;
                _SetValue = value;
                _Timer.Reset();
                _Timer.Start();
            }
        }

        public CSyncTimer(float currentTime, float k, float t)
        {
            _ExternTime = new CPt1(currentTime, k, t);
            _Timer = new Stopwatch();
            _SetValue = currentTime;
        }

        public float Update(float newTime)
        {
            float et = _ExternTime.Update(newTime);

            float dt = Time;

            float diff = et - dt;
            if (Math.Abs(diff) > 0.05f)
            {
                _Timer.Reset();
                _Timer.Start();
                _SetValue = dt + diff;
                dt = _SetValue;
                //Console.WriteLine("DRIFTED!!! " + diff.ToString());
            }
            else
            {
                if (diff > 0.01f)
                    _SetValue += 0.000025f;

                if (diff < -0.01f)
                    _SetValue -= 0.000025f;
            }
            //Console.WriteLine(diff.ToString());
            return dt;
        }

        public void Pause()
        {
            _ExternTime.Pause();
            _Timer.Stop();
        }

        public void Resume()
        {
            _ExternTime.Resume();
            _Timer.Start();
        }
    }
}