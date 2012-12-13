using System;
using System.Collections.Generic;
using System.Text;
using Vocaluxe.Lib.Draw;
using System.Drawing;

using Vocaluxe.Menu;

namespace Vocaluxe.Lib.Webcam
{
    struct SWebcamDevice
    {
        public int ID;
        public string Name;
        public string MonikerString;
        public List<SCapabilities> Capabilities;
    }

    struct SCapabilities
    {
        public int Framerate;
        public int Width;
        public int Height;
    }

    struct SWebcamConfig
    {
        public string MonikerString;
        public int Framerate;
        public int Width;
        public int Height;
    }

    interface IWebcam
    {
        bool Init();

        void Start();
        void Pause();
        void Stop();
        void Close();

        bool GetFrame(ref STexture Frame);
        Bitmap GetBitmap();
        SWebcamConfig GetConfig();
        SWebcamDevice[] GetDevices();

        bool Select(SWebcamConfig sWebcamConfig);
    }
}
