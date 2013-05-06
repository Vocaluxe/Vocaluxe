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
using VocaluxeLib;

namespace Vocaluxe.Base
{
    static class CController
    {
        private static IController _Controller;

        public static void Init()
        {
            _Controller = new CWiiMote();

            _Controller.Init();
        }

        public static void Close()
        {
            _Controller.Close();
            _Controller = null;
        }

        public static void Connect()
        {
            _Controller.Connect();
        }

        public static void Disconnect()
        {
            _Controller.Disconnect();
        }

        public static bool IsConnected()
        {
            return _Controller.IsConnected();
        }

        public static void Update()
        {
            _Controller.Update();
        }

        public static bool PollKeyEvent(ref SKeyEvent keyEvent)
        {
            return _Controller.PollKeyEvent(ref keyEvent);
        }

        public static bool PollMouseEvent(ref SMouseEvent mouseEvent)
        {
            return _Controller.PollMouseEvent(ref mouseEvent);
        }

        public static void SetRumble(float duration)
        {
            _Controller.SetRumble(duration);
        }
    }
}