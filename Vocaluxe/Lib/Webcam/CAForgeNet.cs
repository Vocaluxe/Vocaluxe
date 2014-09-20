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

using System;
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
        private SWebcamConfig _Config;
        private byte[] _Data;
        private static readonly object _MutexData = new object();
        private int _Width, _Height;
        private bool _NewFrameAvailable;
        private bool _IsCapturing;

        public bool Init()
        {
            FilterInfoCollection webcams = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo info in webcams)
            {
                var tmpdev = new VideoCaptureDevice(info.MonikerString);
                if (tmpdev.VideoCapabilities.Length == 0)
                    continue;
                var device = new SWebcamDevice
                    {
                        Name = info.Name,
                        MonikerString = info.MonikerString,
                        Capabilities = new List<SCapabilities>(tmpdev.VideoCapabilities.Length)
                    };

                foreach (VideoCapabilities capabilities in tmpdev.VideoCapabilities)
                {
                    var item = new SCapabilities
                        {
                            Framerate = capabilities.AverageFrameRate,
                            Height = capabilities.FrameSize.Height,
                            Width = capabilities.FrameSize.Width
                        };
                    device.Capabilities.Add(item);
                }
                _Devices.Add(device);
            }
            return true;
        }

        public void Close()
        {
            DeSelect();
            _Devices.Clear();
        }

        public void DeSelect()
        {
            if (_Webcam == null)
                return;
            _Webcam.Stop();
            _Webcam.NewFrame -= _OnFrame;
            _IsCapturing = false;
            lock (_MutexData)
            {
                _Data = null;
                _Webcam = null;
            }
        }

        #region ConfigSelection
        private static float _GetScore(int value, int valueRequested)
        {
            if (valueRequested <= 0)
                return 1;
            return 1.0f - (float)Math.Abs(value - valueRequested) / Math.Max(value, valueRequested);
        }

        private static VideoCapabilities _SelectWebcamConfig(VideoCapabilities[] capabilities, SWebcamConfig config)
        {
            int configTaken = 0;
            if (config.Framerate != 0 && config.Height != 0 && config.Width != 0)
            {
                float bestMatchScore = 0;
                for (int i = 0; i < capabilities.Length; i++)
                {
                    float score = _GetScore(capabilities[i].AverageFrameRate, config.Framerate);
                    score += _GetScore(capabilities[i].FrameSize.Height, config.Height);
                    score += _GetScore(capabilities[i].FrameSize.Width, config.Width);
                    if (score >= bestMatchScore)
                    {
                        // Take the config with the best score, Higher indizes should indicate a better resolution
                        // so choose the last one with best score if 2 have equal scores
                        bestMatchScore = score;
                        configTaken = i;
                    }
                }
            }

            return capabilities[configTaken];
        }
        #endregion

        public bool Select(SWebcamConfig config)
        {
            //Close old camera connection
            DeSelect();

            if (_Devices.Count < 1)
                return false;

            string moniker = _Devices[0].MonikerString;
            if (config.MonikerString != "")
            {
                foreach (SWebcamDevice device in _Devices)
                {
                    if (device.MonikerString == config.MonikerString)
                    {
                        moniker = device.MonikerString;
                        break;
                    }
                }
            }
            _Webcam = new VideoCaptureDevice(moniker);

            _Webcam.VideoResolution = _SelectWebcamConfig(_Webcam.VideoCapabilities, config);

            _Config.Framerate = _Webcam.VideoResolution.AverageFrameRate;
            _Config.Height = _Webcam.VideoResolution.FrameSize.Height;
            _Config.Width = _Webcam.VideoResolution.FrameSize.Width;
            _Config.MonikerString = _Webcam.Source;

            return true;
        }

        public void Start()
        {
            if (_Webcam == null)
                return;
            if (_IsCapturing)
            {
                if (_Paused)
                {
                    _Webcam.NewFrame += _OnFrame;
                    _Paused = false;
                }
            }
            else
            {
                //Subscribe to NewFrame event
                _Webcam.NewFrame += _OnFrame;
                _Paused = false;
                _Webcam.Start();
                _IsCapturing = true;
            }
        }

        public void Pause()
        {
            if (_Webcam != null && !_Paused)
            {
                _Webcam.NewFrame -= _OnFrame;
                lock (_MutexData) //Make sure all events are finished or not yet startet for consistency
                {
                    _Paused = true;
                }
            }
        }

        public void Stop()
        {
            if (_Webcam != null)
            {
                _Webcam.SignalToStop();
                _Webcam.NewFrame -= _OnFrame;
                lock (_MutexData) //Make sure all events are finished or not yet startet for consistency
                {
                    _IsCapturing = false;
                }
            }
        }

        public bool GetFrame(ref CTextureRef frame)
        {
            lock (_MutexData)
            {
                if (_Data != null && _Data.Length == _Width * _Height * 4 && _NewFrameAvailable)
                {
                    if (frame == null)
                        frame = CDraw.AddTexture(_Width, _Height, _Data);
                    else
                        CDraw.UpdateTexture(frame, _Width, _Height, _Data);
                    _NewFrameAvailable = false;
                    return true;
                }
                return false;
            }
        }

        public Bitmap GetBitmap()
        {
            lock (_MutexData)
            {
                if (_Data != null && _Data.Length == _Width * _Height * 4)
                {
                    var bmp = new Bitmap(_Width, _Height);
                    BitmapData bitmapdata = bmp.LockBits(new Rectangle(0, 0, _Width, _Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(_Data, 0, bitmapdata.Scan0, _Data.Length);
                    bmp.UnlockBits(bitmapdata);
                    return bmp;
                }
                return null;
            }
        }

        private void _OnFrame(object sender, NewFrameEventArgs e)
        {
            if (_Paused)
                return;
            if (e.Frame.Width == 0 || e.Frame.Height == 0)
                return;
            lock (_MutexData)
            {
                if (!IsCapturing())
                    return;
                if (_Data == null || _Data.Length != e.Frame.Width * e.Frame.Height * 4)
                    _Data = new byte[e.Frame.Width * e.Frame.Height * 4];

                _Width = e.Frame.Width;
                _Height = e.Frame.Height;
                BitmapData bitmapdata = e.Frame.LockBits(new Rectangle(0, 0, _Width, _Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                Marshal.Copy(bitmapdata.Scan0, _Data, 0, _Data.Length);
                e.Frame.UnlockBits(bitmapdata);
                _NewFrameAvailable = true;
            }
        }

        public SWebcamDevice[] GetDevices()
        {
            return _Devices.ToArray();
        }

        public bool IsDeviceAvailable()
        {
            return _Webcam != null;
        }

        public bool IsCapturing()
        {
            return _IsCapturing && !_Paused;
        }

        public SWebcamConfig GetConfig()
        {
            return _Config;
        }
    }
}