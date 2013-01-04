using System;
using System.Collections.Generic;
using System.Text;
using Vocaluxe.Base;
using AForge.Video;
using System.Drawing;
using AForge.Video.DirectShow;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Vocaluxe.Lib.Draw;
using System.Threading;

using Vocaluxe.Menu;

namespace Vocaluxe.Lib.Webcam
{
    class CAForgeNet : IWebcam
    {
        private List<SWebcamDevice> _Devices = new List<SWebcamDevice>();
        private bool _Paused;
        private VideoCaptureDevice _Webcam;
        private FilterInfoCollection _WebcamDevices;
        private SWebcamConfig _Config;
        byte[] data = new byte[1];
        int _Width, _Height;

        public void Close()
        {
            if (_Webcam != null)
            {
                _Webcam.SignalToStop();
                _Webcam.WaitForStop();
                _Webcam.NewFrame -= new NewFrameEventHandler(OnFrame);
                data = new byte[1];
            }
        }

        public bool GetFrame(ref STexture Frame)
        {
            if (_Webcam != null && _Width > 0 && _Height > 0 && data.Length == _Width * _Height * 4)
            {
                lock (data)
                {
                    if (Frame.index == -1 || _Width != Frame.width || _Height != Frame.height)
                    {
                        CDraw.RemoveTexture(ref Frame);
                        Frame = CDraw.AddTexture(_Width, _Height, ref data);
                    }
                    else
                    {
                        CDraw.UpdateTexture(ref Frame, ref data);
                    }
                }
            }
            return false;
        }

        public Bitmap GetBitmap()
        {
            if (_Webcam != null && _Width > 0 && _Height > 0 && data.Length == _Height * _Width * 4)
            {
                lock (data)
                {
                    Bitmap bmp = new Bitmap(_Width, _Height);
                    BitmapData bitmapdata = bmp.LockBits(new Rectangle(0, 0, _Width, _Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(data, 0, bitmapdata.Scan0, data.Length);
                    bmp.UnlockBits(bitmapdata);
                    return bmp;
                }
            }
            else
                return null;
        }

        public bool Init()
        {
            _WebcamDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            int num = 0;
            foreach (FilterInfo info in _WebcamDevices)
            {
                SWebcamDevice device = new SWebcamDevice {
                    ID = num,
                    Name = info.Name,
                    MonikerString = info.MonikerString,
                    Capabilities = new List<SCapabilities>()
                };
                num++;
                VideoCaptureDevice tmpdev = new VideoCaptureDevice(info.MonikerString);

                for (int i = 0; i < tmpdev.VideoCapabilities.Length; i++ )
                {
                    SCapabilities item = new SCapabilities
                    {
                        Framerate = tmpdev.VideoCapabilities[i].FrameRate,
                        Height = tmpdev.VideoCapabilities[i].FrameSize.Height,
                        Width = tmpdev.VideoCapabilities[i].FrameSize.Width
                    };
                    device.Capabilities.Add(item);
                }
                _Devices.Add(device);
            }
            return true;
        }

        private void OnFrame(object sender, NewFrameEventArgs e)
        {
            if (!_Paused)
            {
                lock (data)
                {
                    if (_Width != e.Frame.Width || _Height != e.Frame.Height || data.Length != e.Frame.Width * e.Frame.Height * 4)
                        data = new byte[4 * e.Frame.Width * e.Frame.Height];

                    _Width = e.Frame.Width;
                    _Height = e.Frame.Height;
                    BitmapData bitmapdata = e.Frame.LockBits(new Rectangle(0, 0, _Width, _Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(bitmapdata.Scan0, data, 0, data.Length);
                    e.Frame.UnlockBits(bitmapdata);
                }
            }
        }

        public void Pause()
        {
            if (_Webcam != null)
            {
                _Paused = true;
            }
        }

        public void Start()
        {
            if (_Webcam != null)
            {
                _Webcam.NewFrame -= new NewFrameEventHandler(OnFrame);
                //Subscribe to NewFrame event
                _Webcam.NewFrame += new NewFrameEventHandler(OnFrame);
                _Webcam.Start();
                _Paused = false;
            }
        }

        public void Stop()
        {
            if (_Webcam != null)
            {
                _Webcam.SignalToStop();
            }
        }

        public SWebcamDevice[] GetDevices()
        {
            return _Devices.ToArray();
        }

        public bool Select(SWebcamConfig Config)
        {
            //Close old camera connection
            Close();

            if (_WebcamDevices == null)
                return false;
            if (_WebcamDevices.Count < 1)
                return false;

            //No MonikerString found, try first webcam
            if (Config.MonikerString == String.Empty)
                _Webcam = new VideoCaptureDevice(_WebcamDevices[0].MonikerString);
            else //Found MonikerString
                _Webcam = new VideoCaptureDevice(Config.MonikerString);

            if (_Webcam == null)
                return false;

            if (Config.Framerate != 0 && Config.Height != 0 && Config.Width != 0)
            {
                _Webcam.DesiredFrameRate = Config.Framerate;
                _Webcam.DesiredFrameSize = new Size(Config.Width, Config.Height);
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
