using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;

using Vocaluxe.Base;
using Vocaluxe.Menu;

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

        bool PollKeyEvent(ref KeyEvent KeyEvent);
        bool PollMouseEvent(ref MouseEvent MouseEvent);

        void SetRumble(float Duration);
    }

    class RumbleTimer
    {
        private Stopwatch _timer;
        private float _duration;

        public bool ShouldStart
        {
            get
            {
                if (!_timer.IsRunning && _duration != 0f)
                {
                    _timer.Start();
                    return true;
                }

                return false;
            }
        }

        public bool ShouldStop
        {
            get
            {
                if (_timer.IsRunning && _timer.ElapsedMilliseconds / 1000f >= _duration)
                {
                    _timer.Reset();
                    _duration = 0f;
                    return true;
                }

                return false;
            }
        }

        public RumbleTimer()
        {
            _timer = new Stopwatch();
            _duration = 0f;
        }

        public void Set(float Duration)
        {
            if (!_timer.IsRunning)
            {
                _duration = Duration;
                _timer.Reset();
            }
        }

        public void Reset()
        {
            _duration = 0f;
        }
    }

    class CGesture
    {
        Point _Begin;
        bool _locked;

        public CGesture()
        {
            _locked = false;
        }

        public void SetLockPosition(Point Position)
        {
            _locked = true;
            _Begin = new Point(Position.X, Position.Y);
        }

        public void Reset()
        {
            _locked = false;
        }

        public EGesture GetGesture(Point NewPosition)
        {
            if (!_locked)
                return EGesture.None;

            int dx = NewPosition.X - _Begin.X;
            int dy = NewPosition.Y - _Begin.Y;

            //Back/Escape
            if (dx < -150 && Math.Abs(dy) < 150)
            {
                _locked = false;
                return EGesture.Back;
            }

            //ScrollDown
            if (Math.Abs(dx) < 150 && dy > 30)
            {
                _Begin.Y = NewPosition.Y;
                _Begin.X = NewPosition.X;
                return EGesture.ScrollDown;
            }

            //ScrollUp
            if (Math.Abs(dx) < 150 && dy < -30)
            {
                _Begin.Y = NewPosition.Y;
                _Begin.X = NewPosition.X;
                return EGesture.ScrollUp;
            }

            return EGesture.None;
        }
    }
}
