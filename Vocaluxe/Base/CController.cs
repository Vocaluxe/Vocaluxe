﻿#region license
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

using System.Collections.Generic;
using Vocaluxe.Base.Server;
using Vocaluxe.Lib.Input;
using Vocaluxe.Lib.Input.Buzz;
using Vocaluxe.Lib.Input.WiiMote;
using VocaluxeLib;

namespace Vocaluxe.Base
{
    static class CController
    {
        private static List<IController> _Controller;

        private static List<SKeyEvent> _KeysPool;
        private static List<SMouseEvent> _MousePool;

        public static void Init()
        {
            _Controller = new List<IController>();
            _KeysPool = new List<SKeyEvent>();
            _MousePool = new List<SMouseEvent>();

            _AddController(new CWiiMote());
            _AddController(new CGamePad());
            _AddController(CVocaluxeServer.Controller);
            _AddController(new CBuzz());
        }

        private static void _AddController(IController controller)
        {
            if (controller.Init())
                _Controller.Add(controller);
        }

        public static void Close()
        {
            if (_Controller != null)
            {
                foreach (IController controller in _Controller)
                    controller.Close();
                _Controller = null;
                _KeysPool = null;
                _MousePool = null;
            }
        }

        public static void Connect()
        {
            foreach (IController controller in _Controller)
                controller.Connect();
        }

        public static void Disconnect()
        {
            foreach (IController controller in _Controller)
                controller.Disconnect();
        }

        public static bool IsConnected()
        {
            //return _Controller.IsConnected();
            return false; //should be changed
        }

        public static void Update()
        {
            foreach (IController controller in _Controller)
            {
                controller.Update();

                var ke = new SKeyEvent();
                while (controller.PollKeyEvent(ref ke))
                    _KeysPool.Add(ke);

                var me = new SMouseEvent();
                while (controller.PollMouseEvent(ref me))
                    _MousePool.Add(me);
            }
        }

        public static bool PollKeyEvent(ref SKeyEvent keyEvent)
        {
            if (_KeysPool.Count > 0)
            {
                keyEvent = _KeysPool[0];
                _KeysPool.RemoveAt(0);
                return true;
            }
            return false;
        }

        public static bool PollMouseEvent(ref SMouseEvent mouseEvent)
        {
            if (_MousePool.Count > 0)
            {
                mouseEvent = _MousePool[0];
                _MousePool.RemoveAt(0);
                return true;
            }
            return false;
        }

        public static void SetRumble(float duration)
        {
            foreach (IController controller in _Controller)
                controller.SetRumble(duration);
        }

        public static void SetLEDs(bool led1, bool led2, bool led3, bool led4)
        {
            foreach (IController controller in _Controller)
                controller.SetLEDs(led1, led2, led3, led4);
        }
    }
}