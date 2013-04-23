#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using Vocaluxe.Lib.Input;
using Vocaluxe.Lib.Input.WiiMote;
using VocaluxeLib.Menu;

namespace Vocaluxe.Base
{
    static class CInput
    {
        private static IInput _Input;

        public static void Init()
        {
            _Input = new CWiiMote();

            _Input.Init();
        }

        public static void Close()
        {
            _Input.Close();
            _Input = null;
        }

        public static void Connect()
        {
            _Input.Connect();
        }

        public static void Disconnect()
        {
            _Input.Disconnect();
        }

        public static bool IsConnected()
        {
            return _Input.IsConnected();
        }

        public static void Update()
        {
            _Input.Update();
        }

        public static bool PollKeyEvent(ref SKeyEvent keyEvent)
        {
            return _Input.PollKeyEvent(ref keyEvent);
        }

        public static bool PollMouseEvent(ref SMouseEvent mouseEvent)
        {
            return _Input.PollMouseEvent(ref mouseEvent);
        }

        public static void SetRumble(float duration)
        {
            _Input.SetRumble(duration);
        }
    }
}