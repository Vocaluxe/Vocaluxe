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
        private const int MaxGamePads = 4;
        private const int PollSleepMs = 5;
        private const int ReconnectWaitMs = 1000;
        private const int ThreadJoinTimeoutMs = 2000;
        private const int KeyRepeatDelayMs = 100;

        private const float LeftStickDeadZone = 0.8f;
        private const float RightStickMouseDeadZone = 0.15f;
        private const float TriggerThreshold = 0.8f;
        private const float LimitFactor = 1.0f;
        private const float MouseSpeed = 25.0f;
        private const float MouseAxisEpsilon = 0.001f;

        private const int ConnectRumblePulseMs = 125;

        private readonly object _Sync = new object();

        private readonly Stopwatch _dpadDownTimer = new Stopwatch();
        private readonly Stopwatch _dpadUpTimer = new Stopwatch();
        private readonly Stopwatch _dpadLeftTimer = new Stopwatch();
        private readonly Stopwatch _dpadRightTimer = new Stopwatch();
        private readonly Stopwatch _leftStickDownTimer = new Stopwatch();
        private readonly Stopwatch _leftStickUpTimer = new Stopwatch();
        private readonly Stopwatch _leftStickLeftTimer = new Stopwatch();
        private readonly Stopwatch _leftStickRightTimer = new Stopwatch();
        private readonly Stopwatch _leftTriggerTimer = new Stopwatch();
        private readonly Stopwatch _rightTriggerTimer = new Stopwatch();

        private int _GamePadIndex = -1;
        private GamePadState _oldButtonStates;
        private Thread _handlerThread;
        private AutoResetEvent _evTerminate;
        private volatile bool _active;
        private CRumbleTimer _rumbleTimer;

        private float _mouseX;
        private float _mouseY;

        private bool Connected
        {
            get { return _GamePadIndex != -1; }
        }

        public override string GetName()
        {
            return "GamePad";
        }

        public override bool Init()
        {
            if (!base.Init())
                return false;

            _rumbleTimer = new CRumbleTimer();
            _evTerminate = new AutoResetEvent(false);
            _oldButtonStates = new GamePadState();
            _GamePadIndex = -1;
            _active = false;
            _handlerThread = null;

            _mouseX = CSettings.RenderW / 2.0f;
            _mouseY = CSettings.RenderH / 2.0f;

            return true;
        }

        public override void Connect()
        {
            if (_active)
                return;

            if (_evTerminate == null)
                _evTerminate = new AutoResetEvent(false);

            if (_handlerThread == null)
            {
                _handlerThread = new Thread(_MainLoop)
                {
                    Name = "GamePad",
                    Priority = ThreadPriority.BelowNormal,
                    IsBackground = true
                };
            }

            _active = true;
            _handlerThread.Start();
        }

        public override void Disconnect()
        {
            Close();
        }

        public override void Close()
        {
            _active = false;

            if (_evTerminate != null)
                _evTerminate.Set();

            if (_handlerThread != null)
            {
                if (!_handlerThread.Join(ThreadJoinTimeoutMs))
                    Debug.WriteLine("CGamePad: Handler thread did not terminate within timeout.");

                _handlerThread = null;
            }

            try
            {
                if (_GamePadIndex != -1)
                    GamePad.SetVibration(_GamePadIndex, 0.0f, 0.0f);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CGamePad: Failed to stop vibration while closing: " + ex);
            }

            _GamePadIndex = -1;

            if (_evTerminate != null)
            {
                _evTerminate.Dispose();
                _evTerminate = null;
            }

            base.Close();
        }

        public override bool IsConnected()
        {
            return Connected;
        }

        public override void SetRumble(float duration)
        {
            lock (_Sync)
            {
                if (_rumbleTimer != null)
                    _rumbleTimer.Set(duration);
            }
        }

        private void _MainLoop()
        {
            try
            {
                while (_active)
                {
                    Thread.Sleep(PollSleepMs);

                    if (!_EnsureConnected())
                        continue;

                    try
                    {
                        _ProcessCurrentGamePad();
                    }
                    catch (Exception ex)
                    {
                        _HandleLoopException(ex);
                    }
                }
            }
            finally
            {
                _StopVibrationBestEffort(_GamePadIndex);
                _GamePadIndex = -1;
            }
        }

        private bool _EnsureConnected()
        {
            if (Connected)
                return true;

            if (_DoConnect())
                return true;

            if (_evTerminate != null)
                _evTerminate.WaitOne(ReconnectWaitMs);

            return false;
        }

        private void _ProcessCurrentGamePad()
        {
            bool startRumble;
            bool stopRumble;

            lock (_Sync)
            {
                startRumble = _rumbleTimer != null && _rumbleTimer.ShouldStart;
                stopRumble = _rumbleTimer != null && _rumbleTimer.ShouldStop;
            }

            int currentIndex = _GamePadIndex;
            if (currentIndex == -1)
                return;

            if (startRumble)
                GamePad.SetVibration(currentIndex, 1.0f, 1.0f);
            else if (stopRumble)
                GamePad.SetVibration(currentIndex, 0.0f, 0.0f);

            GamePadState state = GamePad.GetState(currentIndex);

            if (!GamePad.GetCapabilities(currentIndex).IsConnected)
            {
                _HandleDisconnect(currentIndex);
                return;
            }

            _HandleButtons(state);
        }

        private void _HandleLoopException(Exception ex)
        {
            Debug.WriteLine("CGamePad: Exception in input loop: " + ex);
            _HandleDisconnect(_GamePadIndex);
        }

        private void _HandleDisconnect(int gamePadIndex)
        {
            _StopVibrationBestEffort(gamePadIndex);
            _GamePadIndex = -1;
            _oldButtonStates = new GamePadState();
            _ResetAllRepeatTimers();
        }

        private static void _StopVibrationBestEffort(int gamePadIndex)
        {
            if (gamePadIndex == -1)
                return;

            try
            {
                GamePad.SetVibration(gamePadIndex, 0.0f, 0.0f);
            }
            catch
            {
                // Ignore: stopping vibration during disconnect/reset is best-effort only.
            }
        }

        private void _HandleButtons(GamePadState buttonStates)
        {
            bool leftClickTriggered =
                (buttonStates.Buttons.LeftShoulder == OpenTK.Input.ButtonState.Pressed &&
                 _oldButtonStates.Buttons.LeftShoulder == OpenTK.Input.ButtonState.Released);

            bool rightClickTriggered =
                (buttonStates.Buttons.RightShoulder == OpenTK.Input.ButtonState.Pressed &&
                 _oldButtonStates.Buttons.RightShoulder == OpenTK.Input.ButtonState.Released);

            leftClickTriggered |=
                (buttonStates.Buttons.RightStick == OpenTK.Input.ButtonState.Pressed &&
                 _oldButtonStates.Buttons.RightStick == OpenTK.Input.ButtonState.Released);

            var keys = new List<Keys>();

            _AddRepeatedKey(
                keys,
                buttonStates.DPad.IsDown,
                _oldButtonStates.DPad.IsDown,
                _dpadDownTimer,
                Keys.Down);

            _AddRepeatedKey(
                keys,
                buttonStates.DPad.IsUp,
                _oldButtonStates.DPad.IsUp,
                _dpadUpTimer,
                Keys.Up);

            _AddRepeatedKey(
                keys,
                buttonStates.DPad.IsLeft,
                _oldButtonStates.DPad.IsLeft,
                _dpadLeftTimer,
                Keys.Left);

            _AddRepeatedKey(
                keys,
                buttonStates.DPad.IsRight,
                _oldButtonStates.DPad.IsRight,
                _dpadRightTimer,
                Keys.Right);

            _AddRepeatedKey(
                keys,
                buttonStates.ThumbSticks.Left.Y > LeftStickDeadZone,
                _oldButtonStates.ThumbSticks.Left.Y > LeftStickDeadZone,
                _leftStickUpTimer,
                Keys.Up);

            _AddRepeatedKey(
                keys,
                buttonStates.ThumbSticks.Left.Y < -LeftStickDeadZone,
                _oldButtonStates.ThumbSticks.Left.Y < -LeftStickDeadZone,
                _leftStickDownTimer,
                Keys.Down);

            _AddRepeatedKey(
                keys,
                buttonStates.ThumbSticks.Left.X < -LeftStickDeadZone,
                _oldButtonStates.ThumbSticks.Left.X < -LeftStickDeadZone,
                _leftStickLeftTimer,
                Keys.Left);

            _AddRepeatedKey(
                keys,
                buttonStates.ThumbSticks.Left.X > LeftStickDeadZone,
                _oldButtonStates.ThumbSticks.Left.X > LeftStickDeadZone,
                _leftStickRightTimer,
                Keys.Right);

            _AddRepeatedKey(
                keys,
                buttonStates.Triggers.Left >= TriggerThreshold,
                _oldButtonStates.Triggers.Left >= TriggerThreshold,
                _leftTriggerTimer,
                Keys.PageUp);

            _AddRepeatedKey(
                keys,
                buttonStates.Triggers.Right >= TriggerThreshold,
                _oldButtonStates.Triggers.Right >= TriggerThreshold,
                _rightTriggerTimer,
                Keys.PageDown);

            if (buttonStates.Buttons.Start == OpenTK.Input.ButtonState.Pressed &&
                _oldButtonStates.Buttons.Start == OpenTK.Input.ButtonState.Released)
            {
                keys.Add(Keys.Space);
            }
            else if (buttonStates.Buttons.A == OpenTK.Input.ButtonState.Pressed &&
                     _oldButtonStates.Buttons.A == OpenTK.Input.ButtonState.Released)
            {
                keys.Add(Keys.Enter);
            }
            else if (buttonStates.Buttons.B == OpenTK.Input.ButtonState.Pressed &&
                     _oldButtonStates.Buttons.B == OpenTK.Input.ButtonState.Released)
            {
                keys.Add(Keys.Escape);
            }
            else if (buttonStates.Buttons.Back == OpenTK.Input.ButtonState.Pressed &&
                     _oldButtonStates.Buttons.Back == OpenTK.Input.ButtonState.Released)
            {
                keys.Add(Keys.Back);
            }

            foreach (Keys key in keys)
            {
                AddKeyEvent(new SKeyEvent(
                    ESender.Gamepad,
                    false,
                    false,
                    false,
                    false,
                    char.MinValue,
                    key));
            }

            float rightX = buttonStates.ThumbSticks.Right.X;
            float rightY = buttonStates.ThumbSticks.Right.Y;

            if (Math.Abs(rightX) < RightStickMouseDeadZone)
                rightX = 0.0f;

            if (Math.Abs(rightY) < RightStickMouseDeadZone)
                rightY = 0.0f;

            bool hasMouseDelta =
                Math.Abs(rightX) > MouseAxisEpsilon ||
                Math.Abs(rightY) > MouseAxisEpsilon;

            if (hasMouseDelta)
            {
                _mouseX += rightX * MouseSpeed * LimitFactor;
                _mouseY -= rightY * MouseSpeed * LimitFactor;
            }

            _mouseX = Math.Min(CSettings.RenderW, Math.Max(0.0f, _mouseX));
            _mouseY = Math.Min(CSettings.RenderH, Math.Max(0.0f, _mouseY));

            bool mouseMoved =
                hasMouseDelta ||
                leftClickTriggered ||
                rightClickTriggered;

            if (mouseMoved)
            {
                AddMouseEvent(new SMouseEvent(
                    ESender.Gamepad,
                    EModifier.None,
                    (int)_mouseX,
                    (int)_mouseY,
                    leftClickTriggered,
                    false,
                    rightClickTriggered,
                    0,
                    false,
                    false,
                    false,
                    false));
            }

            _oldButtonStates = buttonStates;
        }

        private static void _AddRepeatedKey(
            ICollection<Keys> keys,
            bool isPressed,
            bool wasPressed,
            Stopwatch timer,
            Keys key)
        {
            if (isPressed)
            {
                if (!wasPressed || timer.ElapsedMilliseconds >= KeyRepeatDelayMs)
                {
                    keys.Add(key);
                    timer.Restart();
                }
            }
            else
            {
                timer.Reset();
            }
        }

        private void _ResetAllRepeatTimers()
        {
            _dpadDownTimer.Reset();
            _dpadUpTimer.Reset();
            _dpadLeftTimer.Reset();
            _dpadRightTimer.Reset();
            _leftStickDownTimer.Reset();
            _leftStickUpTimer.Reset();
            _leftStickLeftTimer.Reset();
            _leftStickRightTimer.Reset();
            _leftTriggerTimer.Reset();
            _rightTriggerTimer.Reset();
        }

        private bool _DoConnect()
        {
            _GamePadIndex = -1;

            for (int i = 0; i < MaxGamePads; i++)
            {
                try
                {
                    if (GamePad.GetCapabilities(i).IsConnected)
                    {
                        _GamePadIndex = i;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("CGamePad: Failed to query capabilities for pad " + i + ": " + ex);
                }
            }

            if (_GamePadIndex == -1)
                return false;

            _oldButtonStates = new GamePadState();
            _ResetAllRepeatTimers();

            _mouseX = Math.Min(CSettings.RenderW, Math.Max(0.0f, _mouseX));
            _mouseY = Math.Min(CSettings.RenderH, Math.Max(0.0f, _mouseY));

            try
            {
                GamePad.SetVibration(_GamePadIndex, 1.0f, 1.0f);
                Thread.Sleep(ConnectRumblePulseMs);
                GamePad.SetVibration(_GamePadIndex, 0.0f, 0.0f);
                Thread.Sleep(ConnectRumblePulseMs);
                GamePad.SetVibration(_GamePadIndex, 1.0f, 1.0f);
                Thread.Sleep(ConnectRumblePulseMs);
                GamePad.SetVibration(_GamePadIndex, 0.0f, 0.0f);
                Thread.Sleep(ConnectRumblePulseMs);
                GamePad.SetVibration(_GamePadIndex, 1.0f, 1.0f);
                Thread.Sleep(ConnectRumblePulseMs);
                GamePad.SetVibration(_GamePadIndex, 0.0f, 0.0f);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CGamePad: Failed during connect rumble sequence: " + ex);
            }

            return true;
        }
    }
}
