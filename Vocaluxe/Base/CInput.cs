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