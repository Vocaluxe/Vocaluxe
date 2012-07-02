using System;
using System.Collections.Generic;
using System.Text;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using Vocaluxe.Base;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Vocaluxe.Lib.Webcam
{
    class COpenCV : IWebcam
    {
        Image<Bgr, Byte> _CurrentFrame;
        Capture _CaptureDevice;

        Object Mutex = new Object();

        public bool Init()
        {
            //Create a new default Capture
            _CaptureDevice = new Capture();
            if (_CaptureDevice != null)
                _CaptureDevice.ImageGrabbed += OnImageGrabbed;

            return true;
        }

        public void Start()
        {
            if (_CaptureDevice != null)
                _CaptureDevice.Start();
        }

        public void Pause()
        {
            if (_CaptureDevice != null)
                _CaptureDevice.Pause();
        }

        public void Close()
        {
            if(_CaptureDevice != null)
                _CaptureDevice.Dispose();
        }

        public bool GetFrame(ref Draw.STexture Frame)
        {
            if (_CurrentFrame != null)
            {
                Bitmap b = _CurrentFrame.ToBitmap();
                BitmapData bd = b.LockBits(new Rectangle(0, 0, _CurrentFrame.Width, _CurrentFrame.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                byte[] data = new byte[4 * _CurrentFrame.Width * _CurrentFrame.Height];
                Marshal.Copy(bd.Scan0, data, 0, data.Length);

                if (Frame.index == -1 || _CurrentFrame.Width != Frame.width || _CurrentFrame.Height != Frame.height)
                {
                    CDraw.RemoveTexture(ref Frame);
                    Frame = CDraw.AddTexture(_CurrentFrame.Width, _CurrentFrame.Height, ref data);
                }
                else
                {
                    CDraw.UpdateTexture(ref Frame, ref data);
                }

                b.UnlockBits(bd);
                b.Dispose();
                return true;
            }
            return false;
        }

        public void OnImageGrabbed(object sender, EventArgs e)
        {
            _CurrentFrame = _CaptureDevice.RetrieveBgrFrame();
        }
    }
}
