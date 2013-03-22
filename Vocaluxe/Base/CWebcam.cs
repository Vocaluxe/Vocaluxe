using System.Drawing;
using Vocaluxe.Lib.Webcam;
using VocaluxeLib.Menu;

namespace Vocaluxe.Base
{
    static class CWebcam
    {
        private static IWebcam _Webcam;

        public static void Init()
        {
            switch (CConfig.WebcamLib)
            {
                case EWebcamLib.AForgeNet:
                    _Webcam = new CAForgeNet();
                    break;

                default:
                    _Webcam = new CAForgeNet();
                    break;
            }
            _Webcam.Init();
            _Webcam.Select(CConfig.WebcamConfig);

            CConfig.WebcamConfig = _Webcam.GetConfig();
            CConfig.SaveConfig();
        }

        public static bool GetFrame(ref STexture tex)
        {
            _Webcam.GetFrame(ref tex);
            return true;
        }

        public static Bitmap GetBitmap()
        {
            return _Webcam.GetBitmap();
        }

        public static void Close()
        {
            _Webcam.Close();
        }

        public static void Pause()
        {
            _Webcam.Pause();
        }

        public static void Start()
        {
            _Webcam.Start();
        }

        public static void Stop()
        {
            _Webcam.Stop();
        }

        public static void Select(SWebcamConfig c)
        {
            _Webcam.Select(c);
        }

        public static SWebcamDevice[] GetDevices()
        {
            return _Webcam.GetDevices();
        }
    }
}