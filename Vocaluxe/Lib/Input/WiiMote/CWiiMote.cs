using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib.Menu;

namespace Vocaluxe.Lib.Input.WiiMote
{
    class CWiiMote : IInput, IDisposable
    {
        private CWiiMoteLib _WiiMote;

        private List<SKeyEvent> _KeysPool;
        private List<SKeyEvent> _CurrentKeysPool;
        private readonly Object _KeyCopyLock = new Object();

        private List<SMouseEvent> _MousePool;
        private List<SMouseEvent> _CurrentMousePool;
        private readonly Object _MouseCopyLock = new Object();

        private bool[] _ButtonStates;
        private Point _OldPosition;
        private bool _Connected;

        private Thread _HandlerThread;
        private Object _Sync;
        private bool _Active;
        private CRumbleTimer _RumbleTimer;

        private CGesture _Gesture;

        public bool Init()
        {
            _Sync = new Object();
            _RumbleTimer = new CRumbleTimer();
            _Gesture = new CGesture();

            _Active = true;
            _HandlerThread = new Thread(_MainLoop);
            _HandlerThread.Priority = ThreadPriority.BelowNormal;

            _KeysPool = new List<SKeyEvent>();
            _CurrentKeysPool = new List<SKeyEvent>();

            _MousePool = new List<SMouseEvent>();
            _CurrentMousePool = new List<SMouseEvent>();

            _ButtonStates = new bool[11];
            _OldPosition = new Point();

            _HandlerThread.Start();

            return true;
        }

        public void Close()
        {
            _Active = false;
        }

        public bool Connect()
        {
            if (!_Active)
            {
                _Active = true;
                _HandlerThread.Start();
            }
            return true;
        }

        public bool Disconnect()
        {
            _Active = false;
            return true;
        }

        public bool IsConnected()
        {
            return _Connected;
        }

        public void Update()
        {
            _CopyEvents();
        }

        public bool PollKeyEvent(ref SKeyEvent keyEvent)
        {
            if (_CurrentKeysPool.Count > 0)
            {
                keyEvent = _CurrentKeysPool[0];
                _CurrentKeysPool.RemoveAt(0);
                return true;
            }
            else
                return false;
        }

        public bool PollMouseEvent(ref SMouseEvent mouseEvent)
        {
            if (_CurrentMousePool.Count > 0)
            {
                mouseEvent = _CurrentMousePool[0];
                _CurrentMousePool.RemoveAt(0);
                return true;
            }
            else
                return false;
        }

        public void SetRumble(float duration)
        {
            lock (_Sync)
            {
                _RumbleTimer.Set(duration);
            }
        }

        private void _MainLoop()
        {
            _WiiMote = new CWiiMoteLib();
            _WiiMote.WiiMoteChanged += _WmWiiMoteChanged;

            while (_Active)
            {
                Thread.Sleep(5);

                if (!_WiiMote.Connected)
                {
                    if (!_DoConnect())
                        Thread.Sleep(1000);
                }
                else
                {
                    bool startRumble = false;
                    bool stopRumble = false;
                    lock (_Sync)
                    {
                        startRumble = _RumbleTimer.ShouldStart;
                        stopRumble = _RumbleTimer.ShouldStop;
                    }

                    if (startRumble)
                        _WiiMote.SetRumble(true);

                    if (stopRumble)
                        _WiiMote.SetRumble(false);
                }
            }

            _WiiMote.SetRumble(false);
            _WiiMote.Disconnect();
            _Connected = false;
        }

        private bool _DoConnect()
        {
            try
            {
                if (!_WiiMote.Connect())
                    return false;
            }
            catch
            {
                return false;
            }

            _WiiMote.SetReportType(EInputReport.IRAccel, EIRSensitivity.Max, false);
            _WiiMote.SetLEDs(false, false, true, false);

            _WiiMote.SetRumble(true);
            Thread.Sleep(250);
            _WiiMote.SetRumble(false);
            Thread.Sleep(250);
            _WiiMote.SetRumble(true);
            Thread.Sleep(250);
            _WiiMote.SetRumble(false);

            _Connected = true;
            return true;
        }

        private void _WmWiiMoteChanged(object sender, CWiiMoteChangedEventArgs args)
        {
            if (!_Active)
                return;

            CWiiMoteStatus ws = args.WiiMoteState;

            Point p = ws.IRState.Position;
            p.X = 1023 - p.X;

            //key events
            bool alt = false;
            bool shift = false;
            bool ctrl = false;
            bool pressed = false;
            char unicode = char.MinValue;

            EGesture gesture = _Gesture.GetGesture(p);
            bool lb = false;
            bool rb = gesture == EGesture.Back;

            Keys key = Keys.None;

            if (ws.ButtonState.A && !_ButtonStates[0])
                lb = true;
            else if (ws.ButtonState.B && !_ButtonStates[1])
                _Gesture.SetLockPosition(p);
            else if (!ws.ButtonState.B && _ButtonStates[1])
                _Gesture.Reset();
            else if (ws.ButtonState.Down && !_ButtonStates[2])
                key = Keys.Right;
            else if (ws.ButtonState.Up && !_ButtonStates[3])
                key = Keys.Left;
            else if (ws.ButtonState.Left && !_ButtonStates[4])
                key = Keys.Down;
            else if (ws.ButtonState.Right && !_ButtonStates[5])
                key = Keys.Up;
            else if (ws.ButtonState.Home && !_ButtonStates[6])
                key = Keys.Space;
            else if (ws.ButtonState.Minus && !_ButtonStates[7])
                key = Keys.Subtract;
            else if (ws.ButtonState.Plus && !_ButtonStates[8])
                key = Keys.Add;
            else if (ws.ButtonState.One && !_ButtonStates[9])
                key = Keys.Enter;
            else if (ws.ButtonState.Two && !_ButtonStates[10])
                key = Keys.Escape;

            _ButtonStates[0] = ws.ButtonState.A;
            _ButtonStates[1] = ws.ButtonState.B;
            _ButtonStates[2] = ws.ButtonState.Down;
            _ButtonStates[3] = ws.ButtonState.Up;
            _ButtonStates[4] = ws.ButtonState.Left;
            _ButtonStates[5] = ws.ButtonState.Right;
            _ButtonStates[6] = ws.ButtonState.Home;
            _ButtonStates[7] = ws.ButtonState.Minus;
            _ButtonStates[8] = ws.ButtonState.Plus;
            _ButtonStates[9] = ws.ButtonState.One;
            _ButtonStates[10] = ws.ButtonState.Two;


            if (alt || shift || ctrl || pressed || unicode != char.MinValue || key != Keys.None)
            {
                SKeyEvent pool = new SKeyEvent(ESender.WiiMote, alt, shift, ctrl, pressed, unicode, key);

                lock (_KeyCopyLock)
                {
                    _KeysPool.Add(pool);
                }
            }

            //mouse events
            float reducing = 0.15f;
            float factor = 1f / (1f - reducing * 2f);
            float rx = ((p.X / 1024f) - reducing) * factor;
            float ry = ((p.Y / 768f) - reducing) * factor;

            int x = (int)(rx * CSettings.RenderW);
            int y = (int)(ry * CSettings.RenderH);


            bool ld = false;
            bool lbh = !lb && ws.ButtonState.A;
            bool rbh = false;
            bool mb = false;
            bool mbh = false;

            int wheel = 0;
            if (gesture == EGesture.ScrollUp)
                wheel = -1;
            if (gesture == EGesture.ScrollDown)
                wheel = 1;

            SMouseEvent mpool = new SMouseEvent();
            bool trigger = false;

            if (!lb && !rb && (p.X != _OldPosition.X || p.Y != _OldPosition.Y))
            {
                mpool = new SMouseEvent(ESender.WiiMote, alt, shift, ctrl, x, y, false, false, false, wheel, lbh, rbh, false, mbh);
                trigger = true;
            }
            else if (lb || rb)
            {
                mpool = new SMouseEvent(ESender.WiiMote, alt, shift, ctrl, x, y, lb, ld, rb, wheel, false, false, mb, false);
                trigger = true;
            }


            if (trigger)
            {
                lock (_MouseCopyLock)
                {
                    _MousePool.Add(mpool);
                }
            }

            _OldPosition.X = p.X;
            _OldPosition.Y = p.Y;
        }

        private void _CopyEvents()
        {
            lock (_KeyCopyLock)
            {
                foreach (SKeyEvent e in _KeysPool)
                    _CurrentKeysPool.Add(e);
                _KeysPool.Clear();
            }

            lock (_MouseCopyLock)
            {
                foreach (SMouseEvent e in _MousePool)
                    _CurrentMousePool.Add(e);
                _MousePool.Clear();
            }
        }

        public void Dispose()
        {
            if (_WiiMote != null)
            {
                _WiiMote.Dispose();
                _WiiMote = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}