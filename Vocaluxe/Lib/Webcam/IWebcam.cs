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

using System.Collections.Generic;
using System.Drawing;
using VocaluxeLib.Draw;

namespace Vocaluxe.Lib.Webcam
{
    struct SWebcamDevice
    {
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

        bool GetFrame(ref CTexture frame);
        Bitmap GetBitmap();
        SWebcamConfig GetConfig();
        SWebcamDevice[] GetDevices();
        bool IsDeviceAvailable();
        bool IsCapturing();

        bool Select(SWebcamConfig webcamConfig);
    }
}