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
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Forms;
using OpenTK.Input;
using Vocaluxe.Base;
using VocaluxeLib;

namespace Vocaluxe.Lib.Input
{
    class CGamePad : CControllerFramework
    {
        private int _GamePadIndex;
        private const float _LimitFactor = 1.0f;
        private const int _KeyRepeatDelay = 100; // Delay in milliseconds for key repeat

        private GamePadState _OldButtonStates;

        // Variables to track key repeat timing
        private readonly Stopwatch _DownKeyPressTimer = new Stopwatch();
        private readonly Stopwatch _UpKeyPressTimer = new Stopwatch();
        private readonly Stopwatch _LeftKeyPressTimer = new Stopwatch();
        private readonly Stopwatch _RightKeyPressTimer = new Stopwatch();
        private readonly Stopwatch _LeftStickDownKeyPressTimer = new Stopwatch();
        private readonly Stopwatch _LeftStickUpKeyPressTimer = new Stopwatch();
        private readonly Stopwatch _LeftStickLeftKeyPressTimer = new Stopwatch();
        private readonly Stopwatch _LeftStickRightKeyPressTimer = new Stopwatch();
        private readonly Stopwatch _LeftTriggerPressTimer = new Stopwatch();
        private readonly Stopwatch _RightTriggerPressTimer = new Stopwatch();
        
        private bool _Connected
        {
            get { return _GamePadIndex != -1; }
        }

        private Thread _HandlerThread;
        private AutoResetEvent _EvTerminate;
        private Object _Sync;
        private bool _Active;
        private CRumbleTimer _RumbleTimer;

        public override string GetName()
        {
            return "GamePad";
        }

        public override bool Init()
        {
            if (!base.Init())
                return false;

            _Sync = new Object();
            _RumbleTimer = new CRumbleTimer();

            _HandlerThread = new Thread(_MainLoop) {Name = "GamePad", Priority = ThreadPriority.BelowNormal};
            _EvTerminate = new AutoResetEvent(false);

            _OldButtonStates = new GamePadState();
            return true;
        }

        public override void Close()
        {
            _Active = false;
            if (_HandlerThread != null)
            {
                //Join before freeing stuff
                //This also ensures, that no other thread is created till the current one is terminated
                _EvTerminate.Set();
                _HandlerThread.Join();
                _HandlerThread = null;
                _EvTerminate.Dispose();
                _EvTerminate = null;
            }
            base.Close();
        }

        public override void Connect()
        {
            if (_Active || _HandlerThread == null)
                return;
            _Active = true;
            _HandlerThread.Start();
        }

        public override void Disconnect()
        {
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

        private void _MainLoop()
        {
            
            while (_Active)
            {
                Thread.Sleep(5);

                if (!_Connected)
                {
                    if (!_DoConnect())
                        _EvTerminate.WaitOne(1000);
                }
                else
                {
                    bool startRumble;
                    bool stopRumble;
                    lock (_Sync)
                    {
                        startRumble = _RumbleTimer.ShouldStart;
                        stopRumble = _RumbleTimer.ShouldStop;
                    }

                    if (startRumble)
                        GamePad.SetVibration(_GamePadIndex, 1.0f, 1.0f);
                    else if (stopRumble)
                        GamePad.SetVibration(_GamePadIndex, 0.0f, 0.0f);

                    _HandleButtons(GamePad.GetState(_GamePadIndex));
                }
            }

            GamePad.SetVibration(_GamePadIndex, 0.0f, 0.0f);

            _GamePadIndex = -1;
        }

        private void _HandleButtons(GamePadState buttonStates)
        {
            bool lb = (buttonStates.Buttons.LeftShoulder == OpenTK.Input.ButtonState.Pressed && _OldButtonStates.Buttons.LeftShoulder == OpenTK.Input.ButtonState.Released);
            bool rb = (buttonStates.Buttons.RightShoulder == OpenTK.Input.ButtonState.Pressed && _OldButtonStates.Buttons.RightShoulder == OpenTK.Input.ButtonState.Released);
            lb |= (buttonStates.Buttons.RightStick == OpenTK.Input.ButtonState.Pressed && _OldButtonStates.Buttons.RightStick == OpenTK.Input.ButtonState.Released);


            var keys = new List<Keys>();

            // Handle DPad
            if (buttonStates.DPad.IsDown)
            {
                if (!_OldButtonStates.DPad.IsDown || _DownKeyPressTimer.ElapsedMilliseconds >= _KeyRepeatDelay)
                {
                    keys.Add(Keys.Down);
                    _DownKeyPressTimer.Restart();
                }
            }
            else
            {
                _DownKeyPressTimer.Reset(); // Reset timer when button is released
            }

            if (buttonStates.DPad.IsUp)
            {
                if (!_OldButtonStates.DPad.IsUp || _UpKeyPressTimer.ElapsedMilliseconds >= _KeyRepeatDelay)
                {
                    keys.Add(Keys.Up);
                    _UpKeyPressTimer.Restart();
                }
            }
            else
            {
                _UpKeyPressTimer.Reset();
            }

            if (buttonStates.DPad.IsLeft)
            {
                if (!_OldButtonStates.DPad.IsLeft || _LeftKeyPressTimer.ElapsedMilliseconds >= _KeyRepeatDelay)
                {
                    keys.Add(Keys.Left);
                    _LeftKeyPressTimer.Restart();
                }
            }
            else
            {
                _LeftKeyPressTimer.Reset();
            }

            if (buttonStates.DPad.IsRight)
            {
                if (!_OldButtonStates.DPad.IsRight || _RightKeyPressTimer.ElapsedMilliseconds >= _KeyRepeatDelay)
                {
                    keys.Add(Keys.Right);
                    _RightKeyPressTimer.Restart();
                }
            }
            else
            {
                _RightKeyPressTimer.Reset();
            }

            // Handle Left Stick
            float deadZone = 0.8f; // Adjust dead zone as needed

            if (buttonStates.ThumbSticks.Left.Y > deadZone)
            {
                if (_OldButtonStates.ThumbSticks.Left.Y <= deadZone || _LeftStickUpKeyPressTimer.ElapsedMilliseconds >= _KeyRepeatDelay)
                {
                    keys.Add(Keys.Up);
                    _LeftStickUpKeyPressTimer.Restart();
                }
            }
            else
            {
                _LeftStickUpKeyPressTimer.Reset();
            }

            if (buttonStates.ThumbSticks.Left.Y < -deadZone)
            {
                if (_OldButtonStates.ThumbSticks.Left.Y >= -deadZone || _LeftStickDownKeyPressTimer.ElapsedMilliseconds >= _KeyRepeatDelay)
                {
                    keys.Add(Keys.Down);
                    _LeftStickDownKeyPressTimer.Restart();
                }
            }
            else
            {
                _LeftStickDownKeyPressTimer.Reset();
            }

            if (buttonStates.ThumbSticks.Left.X < -deadZone)
            {
                if (_OldButtonStates.ThumbSticks.Left.X >= -deadZone || _LeftStickLeftKeyPressTimer.ElapsedMilliseconds >= _KeyRepeatDelay)
                {
                    keys.Add(Keys.Left);
                    _LeftStickLeftKeyPressTimer.Restart();
                }
            }
            else
            {
                _LeftStickLeftKeyPressTimer.Reset();
            }

            if (buttonStates.ThumbSticks.Left.X > deadZone)
            {
                if (_OldButtonStates.ThumbSticks.Left.X <= deadZone || _LeftStickRightKeyPressTimer.ElapsedMilliseconds >= _KeyRepeatDelay)
                {
                    keys.Add(Keys.Right);
                    _LeftStickRightKeyPressTimer.Restart();
                }
            }
            else
            {
                _LeftStickRightKeyPressTimer.Reset();
            }

            // Handle Triggers
            if (buttonStates.Triggers.Left >= 0.8f)
            {
                if (_OldButtonStates.Triggers.Left < 0.8f || _LeftTriggerPressTimer.ElapsedMilliseconds >= _KeyRepeatDelay)
                {
                    keys.Add(Keys.PageUp);
                    _LeftTriggerPressTimer.Restart();
                }
            }
            else
            {
                _LeftTriggerPressTimer.Reset();
            }

            if (buttonStates.Triggers.Right >= 0.8f)
            {
                if (_OldButtonStates.Triggers.Right < 0.8f || _RightTriggerPressTimer.ElapsedMilliseconds >= _KeyRepeatDelay)
                {
                    keys.Add(Keys.PageDown);
                    _RightTriggerPressTimer.Restart();
                }
            }
            else
            {
                _RightTriggerPressTimer.Reset();
            }

            // Handle other buttons
            if (buttonStates.Buttons.Start == OpenTK.Input.ButtonState.Pressed && _OldButtonStates.Buttons.Start == OpenTK.Input.ButtonState.Released)
                keys.Add(Keys.Space);
            else if (buttonStates.Buttons.A == OpenTK.Input.ButtonState.Pressed && _OldButtonStates.Buttons.A == OpenTK.Input.ButtonState.Released)
                keys.Add(Keys.Enter);
            else if (buttonStates.Buttons.B == OpenTK.Input.ButtonState.Pressed && _OldButtonStates.Buttons.B == OpenTK.Input.ButtonState.Released)
                keys.Add(Keys.Escape);
            else if (buttonStates.Buttons.Back == OpenTK.Input.ButtonState.Pressed && _OldButtonStates.Buttons.Back == OpenTK.Input.ButtonState.Released)
                keys.Add(Keys.Back);

            foreach (var key in keys)
                AddKeyEvent(new SKeyEvent(ESender.Gamepad, false, false, false, false, char.MinValue, key));

            if (Math.Abs(buttonStates.ThumbSticks.Right.X - _OldButtonStates.ThumbSticks.Right.X) > 0.01
                || Math.Abs(buttonStates.ThumbSticks.Right.Y - _OldButtonStates.ThumbSticks.Right.Y) > 0.01
                || lb || rb)
            {
                var x = Math.Min(CSettings.RenderW, Math.Max(0, (int)(CSettings.RenderW  * (buttonStates.ThumbSticks.Right.X / 2.0 * _LimitFactor + 0.5f))));
                var y = Math.Min(CSettings.RenderH, Math.Max(0, (int)(CSettings.RenderH  * (buttonStates.ThumbSticks.Right.Y / 2.0 * _LimitFactor * (-1) + 0.5f))));

                AddMouseEvent(new SMouseEvent(ESender.Gamepad, EModifier.None, x, y, lb, false, rb, 0, false, false, false, false));
            }
           
            _OldButtonStates = buttonStates;
        }

        private bool _DoConnect()
        {
            _GamePadIndex = -1;
            for (int i = 0; i < 4; i++)
            {
                if (GamePad.GetCapabilities(i).IsConnected)
                {
                    _GamePadIndex = i;
                    break;
                }
            }


            GamePad.SetVibration(_GamePadIndex, 1.0f, 1.0f);
            Thread.Sleep(125);
            GamePad.SetVibration(_GamePadIndex, 0.0f, 0.0f);
            Thread.Sleep(125);
            GamePad.SetVibration(_GamePadIndex, 1.0f, 1.0f);
            Thread.Sleep(125);
            GamePad.SetVibration(_GamePadIndex, 0.0f, 0.0f);
            Thread.Sleep(125);
            GamePad.SetVibration(_GamePadIndex, 1.0f, 1.0f);
            Thread.Sleep(125);
            GamePad.SetVibration(_GamePadIndex, 0.0f, 0.0f);
          

            return _GamePadIndex != -1;
        }

        
    }
}
