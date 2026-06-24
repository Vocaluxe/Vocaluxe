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
using OpenTK.Windowing.GraphicsLibraryFramework;
using VocaluxeLib;
using Keys = VocaluxeLib.Keys; // disambiguate from OpenTK ...GraphicsLibraryFramework.Keys

namespace Vocaluxe.Lib.Input
{
    /// <summary>
    ///     Gamepad / joystick controller backed by GLFW (via OpenTK 4). Replaces the OpenTK 1.x
    ///     <c>OpenTK.Input.GamePad</c> API that was removed in OpenTK 4. Every frame it polls all
    ///     connected joysticks and turns the D-pad, the left stick and the first two buttons into
    ///     menu-navigation key events. It uses the raw joystick API (hats/axes/buttons), so it works
    ///     with any joystick GLFW reports - no per-device gamepad mapping is required for navigation.
    /// </summary>
    class CGamePad : CControllerFramework
    {
        private const int _MaxJoysticks = 16;   // GLFW_JOYSTICK_1 .. GLFW_JOYSTICK_LAST (0..15)
        private const float _AxisThreshold = 0.6f;

        // Latched state per joystick so each push produces exactly one key event (rising edge).
        private readonly bool[,] _DirDown = new bool[_MaxJoysticks, 4]; // 0=up 1=down 2=left 3=right
        private readonly bool[] _EnterDown = new bool[_MaxJoysticks];
        private readonly bool[] _EscapeDown = new bool[_MaxJoysticks];

        public override string GetName()
        {
            return "Gamepad (GLFW)";
        }

        public override void Connect() {}
        public override void Disconnect() {}

        public override bool IsConnected()
        {
            try
            {
                for (int j = 0; j < _MaxJoysticks; j++)
                {
                    if (GLFW.JoystickPresent(j))
                        return true;
                }
            }
            catch (Exception) { /* best-effort probe: any GLFW error here just means no gamepad is present */ }
            return false;
        }

        // GLFW exposes no rumble API.
        public override void SetRumble(float duration) {}

        public override void Update()
        {
            if (!_Initialized)
                return;
            try
            {
                _Poll();
            }
            catch (Exception)
            {
                // GLFW not ready / no joystick subsystem - ignore and try again next frame.
            }
            base.Update(); // moves queued key events into the poll pool
        }

        private void _Poll()
        {
            for (int j = 0; j < _MaxJoysticks; j++)
            {
                if (!GLFW.JoystickPresent(j))
                {
                    _ResetJoystick(j);
                    continue;
                }

                bool up = false, down = false, left = false, right = false, enter = false, escape = false;

                // D-pad via hats
                ReadOnlySpan<JoystickHats> hats = GLFW.GetJoystickHats(j);
                foreach (JoystickHats h in hats)
                {
                    if ((h & JoystickHats.Up) != 0) up = true;
                    if ((h & JoystickHats.Down) != 0) down = true;
                    if ((h & JoystickHats.Left) != 0) left = true;
                    if ((h & JoystickHats.Right) != 0) right = true;
                }

                // Left stick: axis 0 = X, axis 1 = Y (down-positive, the common convention)
                ReadOnlySpan<float> axes = GLFW.GetJoystickAxes(j);
                if (axes.Length >= 2)
                {
                    if (axes[0] < -_AxisThreshold) left = true;
                    else if (axes[0] > _AxisThreshold) right = true;
                    if (axes[1] < -_AxisThreshold) up = true;
                    else if (axes[1] > _AxisThreshold) down = true;
                }

                // Buttons: 0 = A (confirm), 1 = B (back) - SDL/XInput button order
                ReadOnlySpan<JoystickInputAction> buttons = GLFW.GetJoystickButtons(j);
                if (buttons.Length > 0 && buttons[0] == JoystickInputAction.Press) enter = true;
                if (buttons.Length > 1 && buttons[1] == JoystickInputAction.Press) escape = true;

                _EmitDir(j, 0, up, Keys.Up);
                _EmitDir(j, 1, down, Keys.Down);
                _EmitDir(j, 2, left, Keys.Left);
                _EmitDir(j, 3, right, Keys.Right);
                _EmitButton(ref _EnterDown[j], enter, Keys.Enter);
                _EmitButton(ref _EscapeDown[j], escape, Keys.Escape);
            }
        }

        private void _EmitDir(int j, int dir, bool pressed, Keys key)
        {
            if (pressed && !_DirDown[j, dir])
                _Send(key);
            _DirDown[j, dir] = pressed;
        }

        private void _EmitButton(ref bool last, bool pressed, Keys key)
        {
            if (pressed && !last)
                _Send(key);
            last = pressed;
        }

        private void _Send(Keys key)
        {
            // KeyPressed must be false: in Vocaluxe it flags a text-input keypress, and menu command
            // keys (Enter/Escape/arrows) are only handled in the "!KeyPressed" branch (matching how
            // CKeys delivers non-character keys). With it true, Enter/Escape would be ignored.
            AddKeyEvent(new SKeyEvent(ESender.Gamepad, false, false, false, false, char.MinValue, key));
        }

        private void _ResetJoystick(int j)
        {
            for (int d = 0; d < 4; d++)
                _DirDown[j, d] = false;
            _EnterDown[j] = false;
            _EscapeDown[j] = false;
        }
    }
}
