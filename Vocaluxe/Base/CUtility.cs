using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;

namespace Vocaluxe.Base
{
    static class CTime
    {
        private static Stopwatch _Stopwatch = new Stopwatch();
        private static float _fps = 0.0f;
        private static double nanosecPerTick = (1000.0 * 1000.0 * 1000.0) / Stopwatch.Frequency;

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
                    return (float)((nanosecPerTick * ticks) / (1000.0 * 1000.0));
                else
                    return (float)_Stopwatch.ElapsedMilliseconds;
            }
            else
                return 0f;
        }

        public static float CalculateFPS()
        {
            float ms = GetMilliseconds();

            if (ms > 0)
            {
                _fps = 1 / ms;
            }

            return _fps * 1000f;
        }

        
        public static double GetFPS()
        {
            return _fps * 1000f;
        }
    }

    class CKeys
    {
        private List<KeyEvent> _KeysPool;
        private List<KeyEvent> _ActualPool;

        private Object _CopyLock = new Object();

        private bool _ModALT;
        private bool _ModCTRL;
        private bool _ModSHIFT;
        private bool _KeyPressed;
        private Keys _Keys;
        private Char _char;
        private System.Diagnostics.Stopwatch _timer;


        public CKeys()
        {
            _ModALT = false;
            _ModCTRL = false;
            _ModSHIFT = false;
            _char = ' ';
            _KeyPressed = false;
            _Keys = Keys.D0;

            _KeysPool = new List<KeyEvent>();
            _ActualPool = new List<KeyEvent>();
            _timer = new System.Diagnostics.Stopwatch();
        }

        private void Add(bool alt, bool shift, bool ctrl, bool pressed, char unicode, Keys key)
        {
            bool KeyRepeat = false;
            if ((_char == unicode) && _KeyPressed)
                KeyRepeat = true;
            else if (_Keys == key)
                KeyRepeat = true;

            if (!_timer.IsRunning || (_timer.ElapsedMilliseconds > 75) || !KeyRepeat)
            {
                KeyEvent pool = new KeyEvent(ESender.Keyboard, alt, shift, ctrl, pressed && (unicode != Char.MinValue), unicode, key);

                lock (_CopyLock)
                {
                    try
                    {
                        _KeysPool.Add(pool);
                    }
                    catch (Exception)
                    {

                    }
                }
                
                _timer.Reset();
                _timer.Start();
            }
        }

        private void Del(int index)
        {
            _ActualPool.RemoveAt(index);
        }

        private void CheckModifiers()
        {
            Keys keys = Control.ModifierKeys;

            _ModSHIFT = ((keys & Keys.Shift) == Keys.Shift);
            _ModALT = ((keys & Keys.Alt) == Keys.Alt);
            _ModCTRL = ((keys & Keys.Control) == Keys.Control);
        }
        

        public void KeyDown(KeyEventArgs e)
        {
            CheckModifiers();

            bool Repeat = false;
            if (_Keys == e.KeyCode)
                Repeat = true;

            if (!_timer.IsRunning || (_timer.ElapsedMilliseconds > 75) || !Repeat)
            {
                _Keys = e.KeyCode;
                if (Repeat)
                    Add(_ModALT, _ModSHIFT, _ModCTRL, _KeyPressed, _char, _Keys);
                else
                    Add(_ModALT, _ModSHIFT, _ModCTRL, _KeyPressed, Char.MinValue, _Keys);
            }
        }

        public void KeyPress(KeyPressEventArgs e)
        {
            CheckModifiers();
            
            Add(_ModALT, _ModSHIFT, _ModCTRL, true, e.KeyChar, Keys.None);
            _char = e.KeyChar;
            _KeyPressed = true;
        }

        public void KeyUp(KeyEventArgs e)
        {
            CheckModifiers();
            _KeyPressed = false;
        }

        public bool PollEvent(ref KeyEvent KeyEvent)
        {
            if (_ActualPool.Count > 0)
            {
                KeyEvent = _ActualPool[0];
                Del(0);
                return true;
            }
            else return false;
        }

        public void CopyEvents()
        {
            lock (_CopyLock)
            {
                foreach (KeyEvent e in _KeysPool)
                {
                    _ActualPool.Add(e);
                }
                _KeysPool.Clear();
            }
        }

    }

    class CMouse
    {
        private List<MouseEvent> _EventsPool;
        private List<MouseEvent> _CurrentPool;

        private int _x = 0;
        private int _y = 0;
        
        private Object _CopyLock = new Object();

        private bool _ModALT;
        private bool _ModCTRL;
        private bool _ModSHIFT;

        private System.Diagnostics.Stopwatch _timer;

        public int X
        {
            get { return _x; }
        }

        public int Y
        {
            get { return _y; }
        }

        public bool Visible = false;

        public CMouse()
        {
            _ModALT = false;
            _ModCTRL = false;
            _ModSHIFT = false;

            _EventsPool = new List<MouseEvent>();
            _CurrentPool = new List<MouseEvent>();

            _timer = new System.Diagnostics.Stopwatch();
        }

        private void Add(bool alt, bool shift, bool ctrl, int x, int y, bool lb, bool ld, bool rb, int wheel, bool lbh, bool rbh, bool mb, bool mbh)
        {
            x = (int)((float)x * (float)CSettings.iRenderW / (float)CDraw.GetScreenWidth());
            y = (int)((float)y * (float)CSettings.iRenderH / (float)CDraw.GetScreenHeight());

            MouseEvent pool = new MouseEvent(ESender.Mouse, alt, shift, ctrl, x, y, lb, ld, rb, -wheel / 120, lbh, rbh, mb, mbh);

            lock (_CopyLock)
            {
                _EventsPool.Add(pool);   
            }
            _x = x;
            _y = y;
        }

        private void Del(int index)
        {
            _CurrentPool.RemoveAt(index);
        }

        private void CheckModifiers()
        {
            Keys keys = Control.ModifierKeys;

            _ModSHIFT = ((keys & Keys.Shift) == Keys.Shift);
            _ModALT = ((keys & Keys.Alt) == Keys.Alt);
            _ModCTRL = ((keys & Keys.Control) == Keys.Control);
        }

        public void MouseMove(MouseEventArgs e)
        {
            CheckModifiers();
            Add(_ModALT, _ModSHIFT, _ModCTRL, e.X, e.Y, false, false, false, e.Delta, e.Button == MouseButtons.Left, e.Button == MouseButtons.Right,
                false, e.Button == MouseButtons.Middle);
        }

        public void MouseWheel(MouseEventArgs e)
        {
            CheckModifiers();
            Add(_ModALT, _ModSHIFT, _ModCTRL, e.X, e.Y, false, false, false, e.Delta, e.Button == MouseButtons.Left, e.Button == MouseButtons.Right,
                false, e.Button == MouseButtons.Middle);
        }

        public void MouseDown(MouseEventArgs e)
        {
            CheckModifiers();

            bool lb = e.Button == MouseButtons.Left;
            bool ld = false;
            if (lb)
            {
                if (_timer.IsRunning && _timer.ElapsedMilliseconds < 450)
                {
                    ld = true;
                    _timer.Reset();
                }
                else
                {
                    _timer.Reset();
                    _timer.Start();
                }
            }
            else
            {
                _timer.Reset();
            }

            Add(_ModALT, _ModSHIFT, _ModCTRL, e.X, e.Y, lb, ld, e.Button == MouseButtons.Right, e.Delta, false, false,
                e.Button == MouseButtons.Middle, false);
        }

        public void MouseUp(MouseEventArgs e)
        {
            //CheckModifiers();
            //Add(_ModALT, _ModSHIFT, _ModCTRL, e.X, e.Y, e.Button == MouseButtons.Left, e.Button == MouseButtons.Right, e.Delta);
        }

        public bool PollEvent(ref MouseEvent MouseEvent)
        {
            if (_CurrentPool.Count > 0)
            {
                MouseEvent = _CurrentPool[0];
                Del(0);
                return true;
            }
            else return false;
        }

        public void CopyEvents()
        {
            lock (_CopyLock)
            {
                foreach (MouseEvent e in _EventsPool)
                {
                    _CurrentPool.Add(e);
                }
                _EventsPool.Clear();
            }
        }

    }
}
