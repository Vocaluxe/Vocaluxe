using System;
using System.Diagnostics;
using System.Drawing;
using VocaluxeLib.Menu;

namespace Vocaluxe.Lib.Input
{
    enum EGesture
    {
        None,
        Back,
        ScrollDown,
        ScrollUp
    }

    interface IInput
    {
        bool Init();
        void Close();
        bool Connect();
        bool Disconnect();

        bool IsConnected();
        void Update();

        bool PollKeyEvent(ref SKeyEvent keyEvent);
        bool PollMouseEvent(ref SMouseEvent mouseEvent);

        void SetRumble(float duration);
    }

    class CRumbleTimer
    {
        private readonly Stopwatch _Timer;
        private float _Duration;

        public bool ShouldStart
        {
            get
            {
                if (!_Timer.IsRunning && _Duration != 0f)
                {
                    _Timer.Start();
                    return true;
                }

                return false;
            }
        }

        public bool ShouldStop
        {
            get
            {
                if (_Timer.IsRunning && _Timer.ElapsedMilliseconds / 1000f >= _Duration)
                {
                    _Timer.Reset();
                    _Duration = 0f;
                    return true;
                }

                return false;
            }
        }

        public CRumbleTimer()
        {
            _Timer = new Stopwatch();
            _Duration = 0f;
        }

        public void Set(float duration)
        {
            if (!_Timer.IsRunning)
            {
                _Duration = duration;
                _Timer.Reset();
            }
        }

        public void Reset()
        {
            _Duration = 0f;
        }
    }

    class CGesture
    {
        private Point _Begin;
        private bool _Locked;

        public CGesture()
        {
            _Locked = false;
        }

        public void SetLockPosition(Point position)
        {
            _Locked = true;
            _Begin = new Point(position.X, position.Y);
        }

        public void Reset()
        {
            _Locked = false;
        }

        public EGesture GetGesture(Point newPosition)
        {
            if (!_Locked)
                return EGesture.None;

            int dx = newPosition.X - _Begin.X;
            int dy = newPosition.Y - _Begin.Y;

            //Back/Escape
            if (dx < -150 && Math.Abs(dy) < 150)
            {
                _Locked = false;
                return EGesture.Back;
            }

            //ScrollDown
            if (Math.Abs(dx) < 150 && dy > 30)
            {
                _Begin.Y = newPosition.Y;
                _Begin.X = newPosition.X;
                return EGesture.ScrollDown;
            }

            //ScrollUp
            if (Math.Abs(dx) < 150 && dy < -30)
            {
                _Begin.Y = newPosition.Y;
                _Begin.X = newPosition.X;
                return EGesture.ScrollUp;
            }

            return EGesture.None;
        }
    }
}