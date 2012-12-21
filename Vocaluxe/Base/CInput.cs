using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Lib.Input;
using Vocaluxe.Lib.Input.WiiMote;
using Vocaluxe.Menu;

namespace Vocaluxe.Base
{
    static class CInput
    {
        static IInput _Input;

        public static void Init()
        {
            _Input = new CWiiMote();

            _Input.Init();
        }

        public static void Close()
        {
            _Input.Close();
        }

        public static bool Connect()
        {
            return _Input.Connect();
        }

        public static bool Disconnect()
        {
            return _Input.Disconnect();
        }

        public static bool IsConnected()
        {
            return _Input.IsConnected();
        }

        public static void Update()
        {
            _Input.Update();
        }

        public static bool PollKeyEvent(ref KeyEvent KeyEvent)
        {
            return _Input.PollKeyEvent(ref KeyEvent);
        }

        public static bool PollMouseEvent(ref MouseEvent MouseEvent)
        {
            return _Input.PollMouseEvent(ref MouseEvent);
        }

        public static void SetRumble(float Duration)
        {
            _Input.SetRumble(Duration);
        }
    }
}
