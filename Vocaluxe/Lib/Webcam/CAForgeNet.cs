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

using AForge.Video;
using AForge.Video.DirectShow;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Vocaluxe.Base;
using VocaluxeLib.Draw;

namespace Vocaluxe.Lib.Webcam
{
    class CAForgeNet : IWebcam
    {
        private readonly List<SWebcamDevice> _Devices = new List<SWebcamDevice>();
        private bool _Paused;
        private VideoCaptureDevice _Webcam;
        private FilterInfoCollection _WebcamDevices;
        private SWebcamConfig _Config;
        private byte[] _Data = new byte[1];
        private static readonly object _MutexData = new object();
        private int _Width, _Height;

        public void Close()
        {
            if (_Webcam == null)
                return;
            _Webcam.SignalToStop();
            _Webcam.WaitForStop();
            _Webcam.NewFrame -= _OnFrame;
            _Data = new byte[1];
        }

        public bool GetFrame(ref CTexture frame)
        {
            lock (_MutexData)
            {
                if (_Webcam != null && _Width > 0 && _Height > 0 && _Data.Length == _Width * _Height * 4)
                    CDraw.UpdateOrAddTexture(ref frame, _Width, _Height, _Data);
            }
            return false;
        }

        public Bitmap GetBitmap()
        {
            if (_Webcam != null && _Width > 0 && _Height > 0 && _Data.Length == _Height * _Width * 4)
            {
                lock (_MutexData)
                {
                    Bitmap bmp = new Bitmap(_Width, _Height);
                    BitmapData bitmapdata = bmp.LockBits(new Rectangle(0, 0, _Width, _Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(_Data, 0, bitmapdata.Scan0, _Data.Length);
                    bmp.UnlockBits(bitmapdata);
                    return bmp;
                }
            }
            return null;
        }

        public bool Init()
        {
            _WebcamDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo info in _WebcamDevices)
            {
                SWebcamDevice device = new SWebcamDevice
                    {
                        Name = info.Name,
                        MonikerString = info.MonikerString,
                        Capabilities = new List<SCapabilities>()
                    };
                VideoCaptureDevice tmpdev = new VideoCaptureDevice(info.MonikerString);

                foreach (VideoCapabilities capabilities in tmpdev.VideoCapabilities)
                {
                    SCapabilities item = new SCapabilities
                        {
                            Framerate = capabilities.FrameRate,
                            Height = capabilities.FrameSize.Height,
                            Width = capabilities.FrameSize.Width
                        };
                    device.Capabilities.Add(item);
                }
                _Devices.Add(device);
            }
            return true;
        }

        private void _OnFrame(object sender, NewFrameEventArgs e)
        {
            if (_Paused)
                return;
            lock (_MutexData)
            {
                if (_Width != e.Frame.Width || _Height != e.Frame.Height || _Data.Length != e.Frame.Width * e.Frame.Height * 4)
                    _Data = new byte[4 * e.Frame.Width * e.Frame.Height];

                _Width = e.Frame.Width;
                _Height = e.Frame.Height;
                BitmapData bitmapdata = e.Frame.LockBits(new Rectangle(0, 0, _Width, _Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                Marshal.Copy(bitmapdata.Scan0, _Data, 0, _Data.Length);
                e.Frame.UnlockBits(bitmapdata);
            }
        }

        public void Pause()
        {
            if (_Webcam != null)
                _Paused = true;
        }

        public void Start()
        {
            if (_Webcam == null)
                return;
            _Webcam.NewFrame -= _OnFrame;
            //Subscribe to NewFrame event
            _Webcam.NewFrame += _OnFrame;
            _Webcam.Start();
            _Paused = false;
        }

        public void Stop()
        {
            if (_Webcam != null)
                _Webcam.SignalToStop();
        }

        public SWebcamDevice[] GetDevices()
        {
            return _Devices.ToArray();
        }

        public bool IsDeviceAvailable()
        {
            return _Devices.Count > 0;
        }

        public bool IsCapturing()
        {
            return _Webcam.IsRunning;
        }

        public bool Select(SWebcamConfig config)
        {
            //Close old camera connection
            Close();

            if (_WebcamDevices == null)
                return false;
            if (_WebcamDevices.Count < 1)
                return false;

            //No MonikerString found, try first webcam
            _Webcam = config.MonikerString == "" ? new VideoCaptureDevice(_WebcamDevices[0].MonikerString) : new VideoCaptureDevice(config.MonikerString);

            if (_Webcam == null)
                return false;

            if (config.Framerate != 0 && config.Height != 0 && config.Width != 0)
            {
                _Webcam.DesiredFrameRate = config.Framerate;
                _Webcam.DesiredFrameSize = new Size(config.Width, config.Height);
            }

            _Config.Framerate = _Webcam.DesiredFrameRate;
            _Config.Height = _Webcam.DesiredFrameSize.Height;
            _Config.Width = _Webcam.DesiredFrameSize.Width;
            _Config.MonikerString = _Webcam.Source;

            return true;
        }

        public SWebcamConfig GetConfig()
        {
            return _Config;
        }
    }
}