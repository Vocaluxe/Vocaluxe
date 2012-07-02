using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Lib.Webcam;
using Vocaluxe.Lib.Draw;

namespace Vocaluxe.Base
{
    static class CWebcam
    {
        private static IWebcam _Webcam;

        public static void Init()
        {
            switch (CConfig.WebcamLib)
            {
                case EWebcamLib.OpenCV:
                    _Webcam = new COpenCV();
                    break;

                default:
                    _Webcam = new COpenCV();
                    break;
            }
            _Webcam.Init();
        }

        public static bool GetFrame(ref STexture tex)
        {
            _Webcam.GetFrame(ref tex);
            return true;
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
    }
}
