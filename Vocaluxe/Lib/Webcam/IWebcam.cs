#region license
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
        void Close();

        void Start();
        void Pause();
        void Stop();

        /// <summary>
        ///     Gets last captured frame. Returns true if frame was updated<br />
        ///     Note that this will invalidate the last frame and not return it again with this function
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        bool GetFrame(ref CTextureRef frame);

        /// <summary>
        ///     Gets the last captured frame as a bitma
        /// </summary>
        /// <returns>Null if no frame was captured, the bitmap otherwise</returns>
        Bitmap GetBitmap();

        SWebcamConfig GetConfig();
        SWebcamDevice[] GetDevices();
        bool IsDeviceAvailable();
        bool IsCapturing();

        bool Select(SWebcamConfig webcamConfig);
        void DeSelect();
    }
}