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

using Vocaluxe.Lib.Input;

namespace Vocaluxe.Base.Web
{
    /// Key events coming from the WebApp's remote keyboard.
    class CRemoteKeyboard : CControllerFramework
    {
        public override string GetName()
        {
            return "WebApp";
        }

        public override void Connect() { }

        public override void Disconnect() { }

        public override bool IsConnected()
        {
            return true;
        }

        public override void SetRumble(float duration) { }
    }
}
