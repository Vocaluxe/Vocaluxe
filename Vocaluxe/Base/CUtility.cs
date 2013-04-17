using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using VocaluxeLib.Menu;

namespace Vocaluxe.Base
{
    static class CTime
    {
        private static readonly Stopwatch _Stopwatch = new Stopwatch();
        private static float _Fps;
        private static readonly double _NanosecPerTick = (1000.0 * 1000.0 * 1000.0) / Stopwatch.Frequency;

        public static void Reset()
        {
            _Stopwatch.Reset();
        }

        public static void Start()
        {
            _Stopwatch.Start();
        }

        public static void Restart()
        {
            Reset();
            Start();
        }

        public static bool IsRunning()
        {
            return _Stopwatch.IsRunning;
        }

        public static float GetMilliseconds()
        {
            if (_Stopwatch.IsRunning)
            {
                long ticks = _Stopwatch.ElapsedTicks;
                if (Stopwatch.IsHighResolution && ticks != 0)
                    return (float)((_NanosecPerTick * ticks) / (1000.0 * 1000.0));
                else
                    return _Stopwatch.ElapsedMilliseconds;
            }
            else
                return 0f;
        }

        public static float CalculateFPS()
        {
            float ms = GetMilliseconds();

            if (ms > 0)
                _Fps = 1 / ms;

            return _Fps * 1000f;
        }

        public static double GetFPS()
        {
            return _Fps * 1000f;
        }
    }

    class CKeys
    {
        private readonly List<SKeyEvent> _KeysPool;
        private readonly List<SKeyEvent> _ActualPool;

        private readonly Object _CopyLock = new Object();

        private bool _ModAlt;
        private bool _ModCtrl;
        private bool _ModShift;
        private bool _KeyPressed;
        private Keys _Keys;
        private Char _Char;
        private readonly Stopwatch _Timer;

        public CKeys()
        {
            _ModAlt = false;
            _ModCtrl = false;
            _ModShift = false;
            _Char = ' ';
            _KeyPressed = false;
            _Keys = Keys.D0;

            _KeysPool = new List<SKeyEvent>();
            _ActualPool = new List<SKeyEvent>();
            _Timer = new Stopwatch();
        }

        private void _Add(bool alt, bool shift, bool ctrl, bool pressed, char unicode, Keys key)
        {
            bool keyRepeat = false;
            if ((_Char == unicode) && _KeyPressed)
                keyRepeat = true;
            else if (_Keys == key)
                keyRepeat = true;

            if (!_Timer.IsRunning || (_Timer.ElapsedMilliseconds > 75) || !keyRepeat)
            {
                SKeyEvent pool = new SKeyEvent(ESender.Keyboard, alt, shift, ctrl, pressed && (unicode != Char.MinValue), unicode, key);

                lock (_CopyLock)
                {
                    try
                    {
                        _KeysPool.Add(pool);
                    }
                    catch (Exception) {}
                }

                _Timer.Reset();
                _Timer.Start();
            }
        }

        private void _Del(int index)
        {
            _ActualPool.RemoveAt(index);
        }

        private void _CheckModifiers()
        {
            Keys keys = Control.ModifierKeys;

            _ModShift = (keys & Keys.Shift) == Keys.Shift;
            _ModAlt = (keys & Keys.Alt) == Keys.Alt;
            _ModCtrl = (keys & Keys.Control) == Keys.Control;
        }

        public void KeyDown(KeyEventArgs e)
        {
            _CheckModifiers();

            bool repeat = false;
            if (_Keys == e.KeyCode)
                repeat = true;

            if (!_Timer.IsRunning || (_Timer.ElapsedMilliseconds > 75) || !repeat)
            {
                _Keys = e.KeyCode;
                if (repeat)
                    _Add(_ModAlt, _ModShift, _ModCtrl, _KeyPressed, _Char, _Keys);
                else
                    _Add(_ModAlt, _ModShift, _ModCtrl, _KeyPressed, Char.MinValue, _Keys);
            }
        }

        public void KeyPress(KeyPressEventArgs e)
        {
            _CheckModifiers();

            _Add(_ModAlt, _ModShift, _ModCtrl, true, e.KeyChar, Keys.None);
            _Char = e.KeyChar;
            _KeyPressed = true;
        }

        public void KeyUp(KeyEventArgs e)
        {
            _CheckModifiers();
            _KeyPressed = false;
        }

        public bool PollEvent(ref SKeyEvent keyEvent)
        {
            if (_ActualPool.Count > 0)
            {
                keyEvent = _ActualPool[0];
                _Del(0);
                return true;
            }
            else
                return false;
        }

        public void CopyEvents()
        {
            lock (_CopyLock)
            {
                foreach (SKeyEvent e in _KeysPool)
                    _ActualPool.Add(e);
                _KeysPool.Clear();
            }
        }
    }

    class CMouse
    {
        private readonly List<SMouseEvent> _EventsPool;
        private readonly List<SMouseEvent> _CurrentPool;

        private int _X;
        private int _Y;

        private readonly Object _CopyLock = new Object();

        private bool _ModAlt;
        private bool _ModCtrl;
        private bool _ModShift;

        private readonly Stopwatch _Timer;

        public int X
        {
            get { return _X; }
        }

        public int Y
        {
            get { return _Y; }
        }

        public bool Visible = false;

        public CMouse()
        {
            _ModAlt = false;
            _ModCtrl = false;
            _ModShift = false;

            _EventsPool = new List<SMouseEvent>();
            _CurrentPool = new List<SMouseEvent>();

            _Timer = new Stopwatch();
        }

        private void _Add(bool alt, bool shift, bool ctrl, int x, int y, bool lb, bool ld, bool rb, int wheel, bool lbh, bool rbh, bool mb, bool mbh)
        {
            x = (int)(x * (float)CSettings.RenderW / CDraw.GetScreenWidth());
            y = (int)(y * (float)CSettings.RenderH / CDraw.GetScreenHeight());

            SMouseEvent pool = new SMouseEvent(ESender.Mouse, alt, shift, ctrl, x, y, lb, ld, rb, -wheel / 120, lbh, rbh, mb, mbh);

            lock (_CopyLock)
            {
                _EventsPool.Add(pool);
            }
            _X = x;
            _Y = y;
        }

        private void _Del(int index)
        {
            _CurrentPool.RemoveAt(index);
        }

        private void _CheckModifiers()
        {
            Keys keys = Control.ModifierKeys;

            _ModShift = (keys & Keys.Shift) == Keys.Shift;
            _ModAlt = (keys & Keys.Alt) == Keys.Alt;
            _ModCtrl = (keys & Keys.Control) == Keys.Control;
        }

        public void MouseMove(MouseEventArgs e)
        {
            _CheckModifiers();
            _Add(_ModAlt, _ModShift, _ModCtrl, e.X, e.Y, false, false, false, e.Delta, e.Button == MouseButtons.Left, e.Button == MouseButtons.Right,
                false, e.Button == MouseButtons.Middle);
        }

        public void MouseWheel(MouseEventArgs e)
        {
            _CheckModifiers();
            _Add(_ModAlt, _ModShift, _ModCtrl, e.X, e.Y, false, false, false, e.Delta, e.Button == MouseButtons.Left, e.Button == MouseButtons.Right,
                false, e.Button == MouseButtons.Middle);
        }

        public void MouseDown(MouseEventArgs e)
        {
            _CheckModifiers();

            bool lb = e.Button == MouseButtons.Left;
            bool ld = false;
            if (lb)
            {
                if (_Timer.IsRunning && _Timer.ElapsedMilliseconds < 450)
                {
                    ld = true;
                    _Timer.Reset();
                }
                else
                {
                    _Timer.Reset();
                    _Timer.Start();
                }
            }
            else
                _Timer.Reset();

            _Add(_ModAlt, _ModShift, _ModCtrl, e.X, e.Y, lb, ld, e.Button == MouseButtons.Right, e.Delta, false, false,
                e.Button == MouseButtons.Middle, false);
        }

        public void MouseUp(MouseEventArgs e)
        {
            //CheckModifiers();
            //Add(_ModALT, _ModSHIFT, _ModCTRL, e.X, e.Y, e.Button == MouseButtons.Left, e.Button == MouseButtons.Right, e.Delta);
        }

        public bool PollEvent(ref SMouseEvent mouseEvent)
        {
            if (_CurrentPool.Count > 0)
            {
                mouseEvent = _CurrentPool[0];
                _Del(0);
                return true;
            }
            else
                return false;
        }

        public void CopyEvents()
        {
            lock (_CopyLock)
            {
                foreach (SMouseEvent e in _EventsPool)
                    _CurrentPool.Add(e);
                _EventsPool.Clear();
            }
        }
    }
}