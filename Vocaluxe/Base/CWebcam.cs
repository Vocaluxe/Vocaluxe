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

using System.Drawing;
using Vocaluxe.Lib.Webcam;
using VocaluxeLib;
using VocaluxeLib.Draw;

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

        public static bool GetFrame(ref CTexture tex)
        {
            return _Webcam.GetFrame(ref tex);
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

        public static bool IsDeviceAvailable()
        {
            return _Webcam.IsDeviceAvailable();
        }

        public static bool IsCapturing()
        {
            return _Webcam.IsCapturing();
        }
    }
}