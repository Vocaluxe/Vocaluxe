using System;
using System.Collections.Generic;
using System.Text;
using Vocaluxe.Lib.Draw;

namespace Vocaluxe.Lib.Webcam
{
    interface IWebcam
    {
        bool Init();

        void Start();
        void Pause();
        void Close();

        bool GetFrame(ref STexture Frame);
    }
}
