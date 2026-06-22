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

namespace Vocaluxe.Lib.Input
{
    /// <summary>
    ///     Gamepad support is temporarily stubbed out for the cross-platform port.
    ///     The previous implementation relied on OpenTK 1.x's <c>OpenTK.Input</c> (GamePad/GamePadState),
    ///     which was removed in OpenTK 4. A GLFW-joystick-based reimplementation is tracked under S4.
    /// </summary>
    class CGamePad : CControllerFramework
    {
        public override string GetName()
        {
            return "Gamepad (disabled)";
        }

        public override void Connect() {}

        public override void Disconnect() {}

        public override bool IsConnected()
        {
            return false;
        }

        public override void SetRumble(float duration) {}
    }
}
