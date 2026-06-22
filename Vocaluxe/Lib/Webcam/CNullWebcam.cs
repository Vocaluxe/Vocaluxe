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

using SkiaSharp;
using VocaluxeLib.Draw;

namespace Vocaluxe.Lib.Webcam
{
    /// <summary>
    ///     No-op webcam backend for the cross-platform build. The previous backend (CAForgeNet)
    ///     relied on AForge.NET/DirectShow, which is Windows-only. This reports no device so the
    ///     webcam-related features are gracefully unavailable. A portable backend (e.g. via
    ///     GStreamer or libuvc) is a follow-up.
    /// </summary>
    class CNullWebcam : IWebcam
    {
        public bool Init()
        {
            return true;
        }

        public void Close() {}
        public void Start() {}
        public void Pause() {}
        public void Stop() {}

        public bool GetFrame(ref CTextureRef frame)
        {
            return false;
        }

        public SKBitmap GetBitmap()
        {
            return null;
        }

        public SWebcamConfig GetConfig()
        {
            return new SWebcamConfig();
        }

        public SWebcamDevice[] GetDevices()
        {
            return new SWebcamDevice[0];
        }

        public bool IsDeviceAvailable()
        {
            return false;
        }

        public bool IsCapturing()
        {
            return false;
        }

        public bool Select(SWebcamConfig webcamConfig)
        {
            return false;
        }

        public void DeSelect() {}
    }
}
