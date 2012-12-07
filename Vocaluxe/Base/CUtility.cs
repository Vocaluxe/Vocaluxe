using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;

using Vocaluxe.Lib.Draw;

namespace Vocaluxe.Base
{
    [Flags]
    public enum Modifier
    {
        None,
        Shift,
        Alt,
        Ctrl
    }

    public struct KeyEvent
    {
        public bool ModALT;
        public bool ModSHIFT;
        public bool ModCTRL;
        public bool KeyPressed;
        public bool Handled;
        public Keys Key;
        public Char Unicode;
        public Modifier Mod;
        
        public KeyEvent(bool alt, bool shift, bool ctrl, bool pressed, char unicode, Keys key)
        {
            ModALT = alt;
            ModSHIFT = shift;
            ModCTRL = ctrl;
            KeyPressed = pressed;
            Unicode = unicode;
            Key = key;
            Handled = false;

            Modifier mALT = Modifier.None;
            Modifier mSHIFT = Modifier.None;
            Modifier mCTRL = Modifier.None;

            if (alt)
                mALT = Modifier.Alt;

            if (shift)
                mSHIFT = Modifier.Shift;

            if (ctrl)
                mCTRL = Modifier.Ctrl;

            if (!alt && !shift && !ctrl)
                Mod = Modifier.None;
            else
                Mod = mALT | mSHIFT | mCTRL;
        }
    }

    public struct MouseEvent
    {
        public int X;
        public int Y;
        public bool LB;     //left button click
        public bool LD;     //left button double click
        public bool RB;     //right button click
        public bool MB;     //middle button click

        public bool LBH;    //left button hold (when moving)
        public bool RBH;    //right button hold (when moving)
        public bool MBH;    //middle button hold (when moving)

        public bool ModALT;
        public bool ModSHIFT;
        public bool ModCTRL;
        
        public Modifier Mod;
        public int Wheel;

        public MouseEvent(bool alt, bool shift, bool ctrl, int x, int y, bool lb, bool ld, bool rb, int wheel, bool lbh, bool rbh, bool mb, bool mbh)
        {
            X = x;
            Y = y;
            LB = lb;
            LD = ld;
            RB = rb;
            MB = mb;

            LBH = lbh;
            RBH = rbh;
            MBH = mbh;

            ModALT = alt;
            ModSHIFT = shift;
            ModCTRL = ctrl;
            
            Modifier mALT = Modifier.None;
            Modifier mSHIFT = Modifier.None;
            Modifier mCTRL = Modifier.None;

            if (alt)
                mALT = Modifier.Alt;

            if (shift)
                mSHIFT = Modifier.Shift;

            if (ctrl)
                mCTRL = Modifier.Ctrl;

            if (!alt && !shift && !ctrl)
                Mod = Modifier.None;
            else
                Mod = mALT | mSHIFT | mCTRL;

            Wheel = wheel;
        }
    }

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
                KeyEvent pool = new KeyEvent(alt, shift, ctrl, pressed && (unicode != Char.MinValue), unicode, key);

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

            MouseEvent pool = new MouseEvent(alt, shift, ctrl, x, y, lb, ld, rb, -wheel / 120, lbh, rbh, mb, mbh);

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

    class CHelper
    {
        public static int TryReadInt(StreamReader sr)
        {
            char chr = '0';
            string value = String.Empty;

            try
            {
                chr = (char)sr.Peek();
                while ((chr.CompareTo(' ') != 0) && ((int)chr != 19) && ((int)chr != 16) && ((int)chr != 13))
                {
                    chr = (char)sr.Read();
                    value += chr.ToString();
                    chr = (char)sr.Peek();
                }
            }
            catch (Exception)
            {
                return 0;
            }
            int result = 0;
            int.TryParse(value, out result);
            return result;
        }

        public static void SetRect(RectangleF Bounds, ref RectangleF Rect, float RectAspect, EAspect Aspect)
        {
            float rW = Bounds.Right - Bounds.Left;
            float rH = Bounds.Bottom - Bounds.Top;
            float rA = rW / rH;

            float ScaledWidth = rW;
            float ScaledHeight = rH;

            switch (Aspect)
            {
                case EAspect.Crop:
                    if (rA >= RectAspect)
                    {
                      ScaledWidth  = rW;
                      ScaledHeight = rH * rA / RectAspect;
                    }
                    else
                    {
                      ScaledHeight = rH;
                      ScaledWidth = rW * RectAspect / rA;
                    }
                    break;
                case EAspect.LetterBox:
                    if (RectAspect >= 1)
                    {
                        ScaledWidth = rW;
                        ScaledHeight = rH * rA / RectAspect;
                    }
                    else
                    {
                        ScaledHeight = rH;
                        ScaledWidth = rW * RectAspect / rA;
                    }
                    break;
                default:
                    ScaledWidth = rW;
                    ScaledHeight = rH;
                    break;
            }

            float Left = (rW - ScaledWidth) / 2 + Bounds.Left;
            float Rigth = Left + ScaledWidth;

            float Upper = (rH - ScaledHeight) / 2 + Bounds.Top;
            float Lower = Upper + ScaledHeight;

            Rect = new RectangleF(Left, Upper, Rigth - Left, Lower - Upper);
        }

        public static bool TryGetEnumValueFromXML<T>(string Cast, XPathNavigator Navigator, ref T value)
            where T : struct
        {
            string val = String.Empty;
            if (GetValueFromXML(Cast, Navigator, ref val, Enum.GetName(typeof(T), value)))
            {
                TryParse<T>(val, out value, true);
                return true;
            }
            return false;
        }

        public static bool TryGetIntValueFromXML(string Cast, XPathNavigator Navigator, ref int value)
        {
            string val = String.Empty;
            if (GetValueFromXML(Cast, Navigator, ref val, value.ToString()))
            {
                int res = 0;
                if (int.TryParse(val, out res))
                {
                    value = res;
                    return true;
                }
            }
            return false;
        }

        public static bool TryGetFloatValueFromXML(string Cast, XPathNavigator Navigator, ref float value)
        {
            string val = String.Empty;
            if (GetValueFromXML(Cast, Navigator, ref val, value.ToString()))
            {
                float res = 0;
                if (TryParse(val, out res))
                {
                    value = res;
                    return true;
                }
            }
            return false;
        }

        public static bool GetValueFromXML(string Cast, XPathNavigator Navigator, ref string Value, string DefaultValue)
        {
            XPathNodeIterator iterator;
            int results = 0;
            string val = string.Empty;

            try
            {
                Navigator.MoveToFirstChild();
                iterator = Navigator.Select(Cast);
                                
                while (iterator.MoveNext())
                {
                    val = iterator.Current.Value;
                    results++;
                }
            }
            catch (Exception)
            {
                results = 0;
            }
            
            if ((results == 0) || (results > 1))
            {
                Value = DefaultValue;
                return false;
            }
            else
            {
                Value = val;
                return true;
            }

        }

        public static List<string> GetValuesFromXML(string Cast, XPathNavigator Navigator)
        {
            List<string> values = new List<string>();

            try
            {
                Navigator.MoveToRoot();
                Navigator.MoveToFirstChild();
                Navigator.MoveToFirstChild();
                
                while (Navigator.Name != Cast)
                    Navigator.MoveToNext();

                Navigator.MoveToFirstChild();

                values.Add(Navigator.LocalName);
                while(Navigator.MoveToNext())
                    values.Add(Navigator.LocalName);
                
            }
            catch (Exception)
            {
                
            }

            return values;
        }

        public static bool ItemExistsInXML(string Cast, XPathNavigator Navigator)
        {
            XPathNodeIterator iterator;
            int results = 0;
            
            try
            {
                Navigator.MoveToFirstChild();
                iterator = Navigator.Select(Cast);

                while (iterator.MoveNext())
                    results++;
            }
            catch (Exception)
            {
                results = 0;
            }

            if (results == 0)
                return false;

            return true;
        }

        public List<string> ListFiles(string path, string cast)
        {
            return ListFiles(path, cast, false, false);
        }

        public List<string> ListFiles(string path, string cast, bool recursive)
        {
            return ListFiles(path, cast, recursive, false);
        }

        public List<string> ListFiles(string path, string cast, bool recursive, bool fullpath)
        {
            List<string> files = new List<string>();
            DirectoryInfo dir = new DirectoryInfo(path);
			
			try
			{
                
				foreach (FileInfo file in dir.GetFiles(cast))
	            {
	                if (!fullpath)
	                    files.Add(file.Name);
	                else
	                    //files.Add(Path.Combine(file.DirectoryName, file.Name));
	                    files.Add(file.FullName);
	            }
	
	            if (recursive)
	            {
	                foreach (DirectoryInfo di in dir.GetDirectories())
	                {
	                    files.AddRange(ListFiles(di.FullName, cast, recursive, fullpath));
	                }
	            }
			} catch (Exception)
			{
				
			}
            
            return files;
        }

        public static bool TryParse<T>(string value, out T result)
            where T : struct
        {
            return TryParse<T>(value, out result, false);
        }

        public static bool TryParse<T>(string value, out T result, bool ignoreCase)
           where T : struct
        {
            result = default(T);
            try
            {
                result = (T)Enum.Parse(typeof(T), value, ignoreCase);
                return true;
            }
            catch { }

            return false;
        }

        public static bool TryParse(string value, out float result)
        {
            value = value.Replace(',', '.');
            return float.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo, out result);
        }

        public static bool IsInBounds(SRectF bounds, MouseEvent MouseEvent)
        {
            return IsInBounds(bounds, MouseEvent.X, MouseEvent.Y);
        }

        public static bool IsInBounds(SRectF bounds, int x, int y)
        {
            return ((bounds.X <= x) && (bounds.X + bounds.W >= x) && (bounds.Y <= y) && (bounds.Y + bounds.H >= y));
        }
    }

    static class CEncoding
    {
        public static Encoding GetEncoding(string EncodingName)
        {
            switch (EncodingName)
            {
                case "AUTO":
                    return Encoding.Default;
                case "CP1250":
                    return Encoding.GetEncoding(1250);
                case "CP1252":
                    return Encoding.GetEncoding(1252);
                case "LOCALE":
                    return Encoding.Default;
                case "UTF8":
                    return Encoding.UTF8;
                default:
                    return Encoding.UTF8;
            }
        }

        public static string GetEncodingName(Encoding Enc)
        {
            string Result = "UTF8";

            if (Enc.CodePage == 1250)
                Result = "CP1250";

            if (Enc.CodePage == 1252)
                Result = "CP1252";

            return Result;
        }
    }
}
