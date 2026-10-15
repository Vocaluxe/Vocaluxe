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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using OpenTK.Input;
using Vocaluxe.Base;
using VocaluxeLib;
using ButtonState = OpenTK.Input.ButtonState;

namespace Vocaluxe.Lib.Input
{
    class CGamePad : CControllerFramework
    {
        private const int MaxGamePads = 4;
        private const int PollIntervalMs = 5;
        private const int ReconnectWaitMs = 1000;
        private const int ErrorBackoffMs = 2000;
        private const int ThreadJoinTimeoutMs = 2000;

        private const int InitialRepeatDelayMs = 400;
        private const int KeyRepeatDelayMs = 100;

        private const float StickPressThreshold = 0.8f;
        private const float StickReleaseThreshold = 0.6f;
        private const float TriggerPressThreshold = 0.8f;
        private const float TriggerReleaseThreshold = 0.6f;

        private const float RightStickDeadZone = 0.15f;
        private const float MouseSpeedPxPerSecond = 1500.0f;
        private const float MaxMouseStepSeconds = 0.1f;

        private const int ConnectRumblePulses = 3;
        private const int ConnectRumblePulseMs = 125;

        private readonly object _Sync = new object();

        private readonly List<Keys> _PendingKeys = new List<Keys>();
        private readonly Stopwatch _MouseTimer = new Stopwatch();
        private readonly CRepeatingInput[] _RepeatingInputs =
        {
            new CRepeatingInput(Keys.Up, (s, was) => s.DPad.IsUp || _IsAxisPressed(s.ThumbSticks.Left.Y, was)),
            new CRepeatingInput(Keys.Down, (s, was) => s.DPad.IsDown || _IsAxisPressed(-s.ThumbSticks.Left.Y, was)),
            new CRepeatingInput(Keys.Left, (s, was) => s.DPad.IsLeft || _IsAxisPressed(-s.ThumbSticks.Left.X, was)),
            new CRepeatingInput(Keys.Right, (s, was) => s.DPad.IsRight || _IsAxisPressed(s.ThumbSticks.Left.X, was)),
            new CRepeatingInput(Keys.PageUp, (s, was) => _IsTriggerPressed(s.Triggers.Left, was)),
            new CRepeatingInput(Keys.PageDown, (s, was) => _IsTriggerPressed(s.Triggers.Right, was))
        };

        private volatile int _GamePadIndex = -1;
        private volatile bool _Active;
        private int _LastConnectedIndex = -1;
        private GamePadState _OldState;
        private Thread _HandlerThread;
        private ManualResetEvent _EvTerminate;
        private CRumbleTimer _RumbleTimer;

        private float _MouseX;
        private float _MouseY;

        public override string GetName()
        {
            return "GamePad";
        }

        public override bool Init()
        {
            _StopThread();

            if (!base.Init())
            {
                return false;
            }

            lock (_Sync)
            {
                _RumbleTimer = new CRumbleTimer();
            }

            _GamePadIndex = -1;
            _LastConnectedIndex = -1;
            _ResetInputState();

            _MouseX = CSettings.RenderW / 2.0f;
            _MouseY = CSettings.RenderH / 2.0f;

            return true;
        }

        public override void Connect()
        {
            if (_Active)
            {
                return;
            }

            if (_HandlerThread != null)
            {
                if (_HandlerThread.IsAlive)
                {
                    Debug.WriteLine("CGamePad: Previous handler thread is still running, cannot connect.");
                    return;
                }

                _HandlerThread = null;
            }

            if (_EvTerminate == null)
            {
                _EvTerminate = new ManualResetEvent(false);
            }
            else
            {
                _EvTerminate.Reset();
            }

            var evTerminate = _EvTerminate;
            _Active = true;
            _HandlerThread = new Thread(() => _MainLoop(evTerminate))
            {
                Name = "GamePad",
                Priority = ThreadPriority.BelowNormal,
                IsBackground = true
            };
            _HandlerThread.Start();
        }

        public override void Disconnect()
        {
            _StopThread();
        }

        public override void Close()
        {
            _StopThread();
            base.Close();
        }

        public override bool IsConnected()
        {
            return _GamePadIndex != -1;
        }

        public override void SetRumble(float duration)
        {
            lock (_Sync)
            {
                if (_RumbleTimer != null)
                {
                    _RumbleTimer.Set(duration);
                }
            }
        }

        private void _StopThread()
        {
            _Active = false;

            if (_EvTerminate != null)
            {
                _EvTerminate.Set();
            }

            if (_HandlerThread != null)
            {
                if (!_HandlerThread.Join(ThreadJoinTimeoutMs))
                {
                    Debug.WriteLine("CGamePad: Handler thread did not terminate within timeout.");
                    return;
                }

                _HandlerThread = null;
            }

            if (_EvTerminate != null)
            {
                _EvTerminate.Dispose();
                _EvTerminate = null;
            }
        }

        private void _MainLoop(WaitHandle evTerminate)
        {
            try
            {
                while (_Active && !evTerminate.WaitOne(PollIntervalMs))
                {
                    if (!_EnsureConnected(evTerminate))
                    {
                        continue;
                    }

                    try
                    {
                        _ProcessCurrentGamePad();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("CGamePad: Exception in input loop: " + ex);
                        _HandleDisconnect(false);
                        evTerminate.WaitOne(ErrorBackoffMs);
                    }
                }
            }
            finally
            {
                _StopVibration(_GamePadIndex);
                _GamePadIndex = -1;
            }
        }

        private bool _EnsureConnected(WaitHandle evTerminate)
        {
            if (_GamePadIndex != -1)
            {
                return true;
            }

            if (_DoConnect(evTerminate))
            {
                return true;
            }

            evTerminate.WaitOne(ReconnectWaitMs);
            return false;
        }

        private bool _DoConnect(WaitHandle evTerminate)
        {
            var index = _FindConnectedPad();
            if (index == -1)
            {
                return false;
            }

            _ResetInputState();
            _GamePadIndex = index;

            var isNewPad = index != _LastConnectedIndex;
            _LastConnectedIndex = index;
            if (isNewPad)
            {
                _PlayConnectRumble(index, evTerminate);
            }

            return true;
        }

        private static int _FindConnectedPad()
        {
            for (var i = 0; i < MaxGamePads; i++)
            {
                try
                {
                    if (GamePad.GetCapabilities(i).IsConnected)
                    {
                        return i;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("CGamePad: Failed to query capabilities for pad " + i + ": " + ex);
                }
            }

            return -1;
        }

        private static void _PlayConnectRumble(int index, WaitHandle evTerminate)
        {
            try
            {
                for (var i = 0; i < ConnectRumblePulses; i++)
                {
                    GamePad.SetVibration(index, 1.0f, 1.0f);
                    if (evTerminate.WaitOne(ConnectRumblePulseMs))
                    {
                        break;
                    }

                    GamePad.SetVibration(index, 0.0f, 0.0f);
                    if (evTerminate.WaitOne(ConnectRumblePulseMs))
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CGamePad: Failed during connect rumble sequence: " + ex);
            }
            finally
            {
                _StopVibration(index);
            }
        }

        private void _ProcessCurrentGamePad()
        {
            var index = _GamePadIndex;
            if (index == -1)
            {
                return;
            }

            var state = GamePad.GetState(index);
            if (!state.IsConnected)
            {
                _HandleDisconnect(true);
                return;
            }

            _UpdateRumble(index);
            _HandleInput(state);
        }

        private void _UpdateRumble(int index)
        {
            bool startRumble;
            bool stopRumble;

            lock (_Sync)
            {
                startRumble = _RumbleTimer != null && _RumbleTimer.ShouldStart;
                stopRumble = _RumbleTimer != null && _RumbleTimer.ShouldStop;
            }

            if (startRumble)
            {
                GamePad.SetVibration(index, 1.0f, 1.0f);
            }
            else if (stopRumble)
            {
                GamePad.SetVibration(index, 0.0f, 0.0f);
            }
        }

        private void _HandleDisconnect(bool padRemoved)
        {
            _StopVibration(_GamePadIndex);
            _GamePadIndex = -1;

            if (padRemoved)
            {
                _LastConnectedIndex = -1;
            }

            _ResetInputState();
        }

        private static void _StopVibration(int gamePadIndex)
        {
            if (gamePadIndex == -1)
            {
                return;
            }

            try
            {
                GamePad.SetVibration(gamePadIndex, 0.0f, 0.0f);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CGamePad: Failed to stop vibration: " + ex);
            }
        }

        private void _ResetInputState()
        {
            _OldState = new GamePadState();
            foreach (var input in _RepeatingInputs)
            {
                input.Reset();
            }

            _MouseTimer.Reset();
        }

        private void _HandleInput(GamePadState state)
        {
            _PendingKeys.Clear();

            foreach (var input in _RepeatingInputs)
            {
                input.Update(state, _PendingKeys);
            }

            if (_IsNewlyPressed(state.Buttons.Start, _OldState.Buttons.Start))
            {
                _PendingKeys.Add(Keys.Space);
            }

            if (_IsNewlyPressed(state.Buttons.A, _OldState.Buttons.A))
            {
                _PendingKeys.Add(Keys.Enter);
            }

            if (_IsNewlyPressed(state.Buttons.B, _OldState.Buttons.B))
            {
                _PendingKeys.Add(Keys.Escape);
            }

            if (_IsNewlyPressed(state.Buttons.Back, _OldState.Buttons.Back))
            {
                _PendingKeys.Add(Keys.Back);
            }

            foreach (var key in _PendingKeys)
            {
                AddKeyEvent(new SKeyEvent(ESender.Gamepad, false, false, false, false, char.MinValue, key));
            }

            _HandleMouse(state);

            _OldState = state;
        }

        private void _HandleMouse(GamePadState state)
        {
            var leftClick =
                _IsNewlyPressed(state.Buttons.LeftShoulder, _OldState.Buttons.LeftShoulder) ||
                _IsNewlyPressed(state.Buttons.RightStick, _OldState.Buttons.RightStick);
            var rightClick = _IsNewlyPressed(state.Buttons.RightShoulder, _OldState.Buttons.RightShoulder);

            var elapsedSeconds = _MouseTimer.IsRunning ? Math.Min((float)_MouseTimer.Elapsed.TotalSeconds, MaxMouseStepSeconds) : 0.0f;
            _MouseTimer.Restart();

            var x = state.ThumbSticks.Right.X;
            var y = state.ThumbSticks.Right.Y;
            _ApplyRadialDeadZone(ref x, ref y);

            var oldX = (int)_MouseX;
            var oldY = (int)_MouseY;

            var step = MouseSpeedPxPerSecond * elapsedSeconds;
            _MouseX = _Clamp(_MouseX + x * step, 0.0f, CSettings.RenderW - 1);
            _MouseY = _Clamp(_MouseY - y * step, 0.0f, CSettings.RenderH - 1);

            var moved = (int)_MouseX != oldX || (int)_MouseY != oldY;
            if (!moved && !leftClick && !rightClick)
            {
                return;
            }

            AddMouseEvent(new SMouseEvent(ESender.Gamepad, EModifier.None, (int)_MouseX, (int)_MouseY, leftClick, false, rightClick, 0, false, false, false, false));
        }

        private static void _ApplyRadialDeadZone(ref float x, ref float y)
        {
            var length = (float)Math.Sqrt(x * x + y * y);
            if (length < RightStickDeadZone)
            {
                x = 0.0f;
                y = 0.0f;
                return;
            }

            var scaledLength = (Math.Min(length, 1.0f) - RightStickDeadZone) / (1.0f - RightStickDeadZone);
            var factor = scaledLength / length;
            x *= factor;
            y *= factor;
        }

        private static float _Clamp(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private static bool _IsNewlyPressed(ButtonState now, ButtonState old)
        {
            return now == ButtonState.Pressed && old == ButtonState.Released;
        }

        private static bool _IsAxisPressed(float value, bool wasPressed)
        {
            return value > (wasPressed ? StickReleaseThreshold : StickPressThreshold);
        }

        private static bool _IsTriggerPressed(float value, bool wasPressed)
        {
            return value >= (wasPressed ? TriggerReleaseThreshold : TriggerPressThreshold);
        }

        private sealed class CRepeatingInput
        {
            private readonly Keys _Key;
            private readonly Func<GamePadState, bool, bool> _IsPressed;
            private readonly Stopwatch _Timer = new Stopwatch();
            private bool _Pressed;
            private bool _Repeating;

            public CRepeatingInput(Keys key, Func<GamePadState, bool, bool> isPressed)
            {
                _Key = key;
                _IsPressed = isPressed;
            }

            public void Update(GamePadState state, ICollection<Keys> keys)
            {
                if (!_IsPressed(state, _Pressed))
                {
                    Reset();
                    return;
                }

                if (!_Pressed)
                {
                    keys.Add(_Key);
                    _Pressed = true;
                    _Repeating = false;
                    _Timer.Restart();
                    return;
                }

                var delay = _Repeating ? KeyRepeatDelayMs : InitialRepeatDelayMs;
                if (_Timer.ElapsedMilliseconds >= delay)
                {
                    keys.Add(_Key);
                    _Repeating = true;
                    _Timer.Restart();
                }
            }

            public void Reset()
            {
                _Pressed = false;
                _Repeating = false;
                _Timer.Reset();
            }
        }
    }
}
