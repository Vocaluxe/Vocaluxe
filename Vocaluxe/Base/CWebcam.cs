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
using Vocaluxe.Lib.Webcam;
using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Base
{
    static class CWebcam
    {
        private static IWebcam _Webcam;

        public static bool Init()
        {
            if (_Webcam != null)
                return false;
#if LINUX
            // Linux: V4L2 capture via ffmpeg (see CV4l2Webcam). Falls back to "no device" if neither
            // ffmpeg nor a capture node is present.
            _Webcam = new CV4l2Webcam();
#else
            switch (CConfig.Config.Video.WebcamLib)
            {
                // AForge.NET/DirectShow is Windows-only and was dropped in the cross-platform port;
                // until a Media Foundation backend exists, Windows uses the no-op webcam.
                case EWebcamLib.AForgeNet:
                default:
                    _Webcam = new CNullWebcam();
                    break;
            }
#endif
            if (!_Webcam.Init())
                return false;
            _Webcam.Select(CConfig.Config.Video.WebcamConfig.HasValue ? CConfig.Config.Video.WebcamConfig.Value : new SWebcamConfig());

            CConfig.Config.Video.WebcamConfig = _Webcam.GetConfig();
            CConfig.SaveConfig();
            return true;
        }

        public static void Close()
        {
            if (_Webcam == null)
                return;
            _Webcam.Close();
            _Webcam = null;
        }

        public static bool GetFrame(ref CTextureRef tex)
        {
            return _Webcam != null && _Webcam.GetFrame(ref tex);
        }

        public static SKBitmap GetBitmap()
        {
            return _Webcam == null ? null : _Webcam.GetBitmap();
        }

        public static void Pause()
        {
            if (_Webcam != null)
                _Webcam.Pause();
        }

        public static void Start()
        {
            if (_Webcam != null)
                _Webcam.Start();
        }

        public static void Stop()
        {
            if (_Webcam != null)
                _Webcam.Stop();
        }

        public static bool Select(SWebcamConfig c)
        {
            return _Webcam != null && _Webcam.Select(c);
        }

        public static void DeSelect()
        {
            if (_Webcam != null)
                _Webcam.DeSelect();
        }

        public static SWebcamDevice[] GetDevices()
        {
            return _Webcam == null ? null : _Webcam.GetDevices();
        }

        public static bool IsDeviceAvailable()
        {
            return (_Webcam != null) && _Webcam.IsDeviceAvailable();
        }

        public static bool IsCapturing()
        {
            return (_Webcam != null) && _Webcam.IsCapturing();
        }
    }
}