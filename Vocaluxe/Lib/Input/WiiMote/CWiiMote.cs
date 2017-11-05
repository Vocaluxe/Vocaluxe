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

using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;

namespace Vocaluxe.Lib.Input.WiiMote
{
    class CWiiMote : CControllerFramework
    {
        private CWiiMoteLib _WiiMote;

        private bool[] _ButtonStates;
        private Point _OldPosition;
        private bool _Connected;
        
        private Object _Sync;
        private bool _Active;
        private CRumbleTimer _RumbleTimer;

        private CGesture _Gesture;

        public override string GetName()
        {
            return "WiiMote";
        }

        public override bool Init()
        {
            if (!base.Init())
                return false;
            _Sync = new Object();
            _RumbleTimer = new CRumbleTimer();
            _Gesture = new CGesture();

            _ButtonStates = new bool[11];
            _OldPosition = new Point();

            _WiiMote = new CWiiMoteLib();
            _WiiMote.WiiMoteChanged += _WmWiiMoteChanged;
            _WiiMote.WiiMoteConnectionChanged += _WiiMote_WiiMoteConnectionChanged;

            return true;
        }

        public override void Close()
        {
            _Active = false;
            _WiiMote.SetRumble(false);
            _WiiMote.Disconnect();
            _Connected = false;

            base.Close();
        }

        public override void Connect()
        {
            if (_Active)
                return;
            _Active = true;
          

            _WiiMote.Connect();
        }

        public override void Disconnect()
        {
            //TODO: Allow reconnect
            Close();
        }

        public override bool IsConnected()
        {
            return _Connected;
        }

        public override void SetRumble(float duration)
        {
            lock (_Sync)
            {
                _RumbleTimer.Set(duration);
            }
        }

        private void _WiiMote_WiiMoteConnectionChanged(object sender, CWiiMoteConnectionChangedEventArgs e)
        {
            if (!e.Connected)
            {
                _Connected = false;
            }
            else
            {
                _WiiMote.SetReportType(EInputReport.IRAccel, EIRSensitivity.Max, false);
                _WiiMote.SetLEDs(false, false, true, false);

                _WiiMote.SetRumble(true);
                Thread.Sleep(250);
                _WiiMote.SetRumble(false);
                Thread.Sleep(250);
                _WiiMote.SetRumble(true);
                Thread.Sleep(250);
                _WiiMote.SetRumble(false);
                

                bool startRumble;
                bool stopRumble;
                lock (_Sync)
                {
                    startRumble = _RumbleTimer.ShouldStart;
                    stopRumble = _RumbleTimer.ShouldStop;
                }

                if (startRumble)
                    _WiiMote.SetRumble(true);
                else if (stopRumble)
                    _WiiMote.SetRumble(false);

                _Connected = true;
            }
        }

        private void _WmWiiMoteChanged(object sender, CWiiMoteChangedEventArgs args)
        {
            if (!_Active)
                return;

            CWiiMoteStatus ws = args.WiiMoteState;

            Point p = ws.IRState.Position;
            p.X = 1023 - p.X;

            EGesture gesture = _Gesture.GetGesture(p);
            bool lb = false;
            bool rb = gesture == EGesture.Back;

            var key = Keys.None;

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


            if (key != Keys.None)
                AddKeyEvent(new SKeyEvent(ESender.WiiMote, false, false, false, false, char.MinValue, key));

            //mouse events
            const float reducing = 0.15f;
            const float factor = 1f / (1f - reducing * 2f);
            float rx = ((p.X / 1024f) - reducing) * factor;
            float ry = ((p.Y / 768f) - reducing) * factor;

            var x = (int)(rx * CSettings.RenderW);
            var y = (int)(ry * CSettings.RenderH);


            bool lbh = !lb && ws.ButtonState.A;

            int wheel = 0;
            if (gesture == EGesture.ScrollUp)
                wheel = -1;
            if (gesture == EGesture.ScrollDown)
                wheel = 1;

            if (!lb && !rb && (p.X != _OldPosition.X || p.Y != _OldPosition.Y))
                AddMouseEvent(new SMouseEvent(ESender.WiiMote, EModifier.None, x, y, false, false, false, wheel, lbh, false, false, false));
            else if (lb || rb)
                AddMouseEvent(new SMouseEvent(ESender.WiiMote, EModifier.None, x, y, lb, false, rb, wheel, false, false, false, false));

            _OldPosition.X = p.X;
            _OldPosition.Y = p.Y;
        }
    }
}