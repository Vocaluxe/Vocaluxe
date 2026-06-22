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

#if LINUX && !MACOS
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using SkiaSharp;
using Vocaluxe.Base;
using VocaluxeLib.Draw;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.Webcam
{
    /// <summary>
    ///     Linux (V4L2) webcam backend, the cross-platform counterpart to the dropped Windows-only
    ///     CAForgeNet. Capture devices are enumerated with a single read-only VIDIOC_QUERYCAP ioctl
    ///     (to get the friendly name and skip non-capture /dev/video* nodes). Frames are streamed by
    ///     piping raw BGRA out of ffmpeg's v4l2 input - this reuses the FFmpeg dependency that is
    ///     already required for audio/video decoding instead of hand-rolling the full V4L2
    ///     mmap/streaming ioctl protocol. If ffmpeg or a capture device is missing it degrades
    ///     gracefully (reports no device), exactly like CNullWebcam.
    /// </summary>
    class CV4l2Webcam : IWebcam
    {
        #region libc / V4L2 (capability query only)
        [DllImport("libc", SetLastError = true)] private static extern int open(string path, int flags);
        [DllImport("libc", SetLastError = true)] private static extern int close(int fd);
        [DllImport("libc", SetLastError = true)] private static extern int ioctl(int fd, ulong request, IntPtr argp);

        private const int _ORdwr = 2;
        private const int _ONonblock = 2048; // O_NONBLOCK (04000 octal)
        // VIDIOC_QUERYCAP = _IOR('V', 0, struct v4l2_capability) ; sizeof(v4l2_capability) == 104
        private const ulong _VidiocQueryCap = (2UL << 30) | (104UL << 16) | ((ulong)'V' << 8) | 0UL;
        private const int _CapStructSize = 104;
        private const int _CapNameOffset = 16;  // char card[32]
        private const int _CapCapsOffset = 84;  // __u32 capabilities
        private const int _CapDevCapsOffset = 88; // __u32 device_caps
        private const uint _CapVideoCapture = 0x00000001;
        private const uint _CapDeviceCaps = 0x80000000;
        #endregion

        private SWebcamDevice[] _Devices = new SWebcamDevice[0];
        private SWebcamConfig _Config;
        private bool _ConfigSelected;

        private Process _Process;
        private Thread _Thread;
        private volatile bool _Running;
        private readonly object _Lock = new object();
        private byte[] _Frame; // latest captured frame, BGRA
        private bool _NewFrame;
        private int _Width;
        private int _Height;

        public bool Init()
        {
            _Devices = _Enumerate();
            return true;
        }

        public void Close()
        {
            Stop();
            lock (_Lock)
            {
                _Frame = null;
                _NewFrame = false;
            }
        }

        public void Start()
        {
            if (!_ConfigSelected || _Running)
                return;

            string ffmpeg = _FindFfmpeg();
            if (ffmpeg == null)
                return;

            _Width = _Config.Width > 0 ? _Config.Width : 640;
            _Height = _Config.Height > 0 ? _Config.Height : 480;
            int fps = _Config.Framerate > 0 ? _Config.Framerate : 30;

            var psi = new ProcessStartInfo
            {
                FileName = ffmpeg,
                Arguments = "-loglevel error -f v4l2 -framerate " + fps +
                            " -video_size " + _Width + "x" + _Height +
                            " -i " + _Config.MonikerString +
                            " -pix_fmt bgra -f rawvideo -",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                _Process = Process.Start(psi);
            }
            catch (Exception e)
            {
                CLog.Error("Webcam: could not start ffmpeg capture: " + e.Message);
                _Process = null;
                return;
            }

            _Running = true;
            _Thread = new Thread(_CaptureLoop) {IsBackground = true, Name = "WebcamCapture"};
            _Thread.Start();
        }

        public void Pause()
        {
            // No native pause for the ffmpeg pipe; stop the capture. Start() resumes it.
            Stop();
        }

        public void Stop()
        {
            _Running = false;

            Process p = _Process;
            _Process = null;
            if (p != null)
            {
                try
                {
                    if (!p.HasExited)
                        p.Kill();
                }
                catch (Exception) {}
                try { p.Dispose(); }
                catch (Exception) {}
            }

            Thread t = _Thread;
            _Thread = null;
            if (t != null && t.IsAlive)
            {
                try { t.Join(500); }
                catch (Exception) {}
            }
            // Keep the last frame so GetBitmap() right after Stop() (snapshot path) still works.
        }

        public bool GetFrame(ref CTextureRef frame)
        {
            byte[] data;
            lock (_Lock)
            {
                if (!_NewFrame || _Frame == null)
                    return false;
                data = (byte[])_Frame.Clone();
                _NewFrame = false;
            }

            if (frame == null)
                frame = CDraw.AddTexture(_Width, _Height, data);
            else
                CDraw.EnqueueTextureUpdate(frame, _Width, _Height, data);
            return true;
        }

        public SKBitmap GetBitmap()
        {
            byte[] data;
            int w, h;
            lock (_Lock)
            {
                if (_Frame == null)
                    return null;
                data = (byte[])_Frame.Clone();
                w = _Width;
                h = _Height;
            }

            var bmp = new SKBitmap(new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Unpremul));
            Marshal.Copy(data, 0, bmp.GetPixels(), Math.Min(data.Length, bmp.ByteCount));
            return bmp;
        }

        public SWebcamConfig GetConfig()
        {
            return _Config;
        }

        public SWebcamDevice[] GetDevices()
        {
            return _Devices;
        }

        public bool IsDeviceAvailable()
        {
            return _Devices.Length > 0;
        }

        public bool IsCapturing()
        {
            Process p = _Process;
            if (!_Running || p == null)
                return false;
            try { return !p.HasExited; }
            catch (Exception) { return false; }
        }

        public bool Select(SWebcamConfig webcamConfig)
        {
            if (_Devices.Length == 0)
                return false;

            // Fall back to the first device if the requested one is unset/unknown.
            bool known = false;
            if (!string.IsNullOrEmpty(webcamConfig.MonikerString))
            {
                foreach (SWebcamDevice d in _Devices)
                {
                    if (d.MonikerString == webcamConfig.MonikerString)
                    {
                        known = true;
                        break;
                    }
                }
            }
            if (!known)
            {
                webcamConfig.MonikerString = _Devices[0].MonikerString;
                if (_Devices[0].Capabilities.Count > 0)
                {
                    webcamConfig.Width = _Devices[0].Capabilities[0].Width;
                    webcamConfig.Height = _Devices[0].Capabilities[0].Height;
                    webcamConfig.Framerate = _Devices[0].Capabilities[0].Framerate;
                }
            }
            if (webcamConfig.Width <= 0 || webcamConfig.Height <= 0)
            {
                webcamConfig.Width = 640;
                webcamConfig.Height = 480;
            }
            if (webcamConfig.Framerate <= 0)
                webcamConfig.Framerate = 30;

            _Config = webcamConfig;
            _ConfigSelected = true;
            return true;
        }

        public void DeSelect()
        {
            Stop();
            _ConfigSelected = false;
            lock (_Lock)
            {
                _Frame = null;
                _NewFrame = false;
            }
        }

        #region helpers
        private void _CaptureLoop()
        {
            int frameSize = _Width * _Height * 4;
            var buf = new byte[frameSize];
            Stream stream;
            try
            {
                stream = _Process.StandardOutput.BaseStream;
            }
            catch (Exception)
            {
                _Running = false;
                return;
            }

            try
            {
                while (_Running)
                {
                    int off = 0;
                    while (off < frameSize)
                    {
                        int r = stream.Read(buf, off, frameSize - off);
                        if (r <= 0)
                        {
                            _Running = false; // EOF: process exited / device gone
                            break;
                        }
                        off += r;
                    }
                    if (off < frameSize)
                        break;

                    lock (_Lock)
                    {
                        if (_Frame == null || _Frame.Length != frameSize)
                            _Frame = new byte[frameSize];
                        Buffer.BlockCopy(buf, 0, _Frame, 0, frameSize);
                        _NewFrame = true;
                    }
                }
            }
            catch (Exception)
            {
                // stream closed because we're stopping - nothing to do
            }
        }

        private static SWebcamDevice[] _Enumerate()
        {
            var devices = new List<SWebcamDevice>();
            if (_FindFfmpeg() == null)
                return devices.ToArray(); // no ffmpeg -> webcam unavailable

            string[] nodes;
            try
            {
                nodes = Directory.GetFiles("/dev", "video*");
            }
            catch (Exception)
            {
                return devices.ToArray();
            }
            Array.Sort(nodes, StringComparer.Ordinal);

            foreach (string node in nodes)
            {
                string name;
                if (!_QueryCaptureDevice(node, out name))
                    continue;
                devices.Add(new SWebcamDevice
                {
                    Name = name,
                    MonikerString = node,
                    Capabilities = new List<SCapabilities>
                    {
                        new SCapabilities {Width = 1280, Height = 720, Framerate = 30},
                        new SCapabilities {Width = 640, Height = 480, Framerate = 30}
                    }
                });
            }
            return devices.ToArray();
        }

        /// <summary>
        ///     Opens the V4L2 node and runs VIDIOC_QUERYCAP. Returns true and the device's friendly
        ///     name when it is a video-capture device.
        /// </summary>
        private static bool _QueryCaptureDevice(string node, out string name)
        {
            name = node;
            int fd = open(node, _ORdwr | _ONonblock);
            if (fd < 0)
                return false;

            IntPtr buf = Marshal.AllocHGlobal(_CapStructSize);
            try
            {
                for (int i = 0; i < _CapStructSize; i++)
                    Marshal.WriteByte(buf, i, 0);

                if (ioctl(fd, _VidiocQueryCap, buf) != 0)
                    return false;

                uint capabilities = unchecked((uint)Marshal.ReadInt32(buf, _CapCapsOffset));
                uint deviceCaps = unchecked((uint)Marshal.ReadInt32(buf, _CapDevCapsOffset));
                uint caps = ((capabilities & _CapDeviceCaps) != 0) ? deviceCaps : capabilities;
                if ((caps & _CapVideoCapture) == 0)
                    return false;

                var nameBytes = new byte[32];
                Marshal.Copy(IntPtr.Add(buf, _CapNameOffset), nameBytes, 0, 32);
                int len = Array.IndexOf(nameBytes, (byte)0);
                if (len < 0)
                    len = nameBytes.Length;
                string card = System.Text.Encoding.UTF8.GetString(nameBytes, 0, len).Trim();
                name = string.IsNullOrEmpty(card) ? node : card + " (" + node + ")";
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
                close(fd);
            }
        }

        private static string _FindFfmpeg()
        {
            string path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(path))
                path = "/usr/bin:/bin:/usr/local/bin";
            foreach (string dir in path.Split(':'))
            {
                if (string.IsNullOrEmpty(dir))
                    continue;
                try
                {
                    string full = Path.Combine(dir, "ffmpeg");
                    if (File.Exists(full))
                        return full;
                }
                catch (Exception) {}
            }
            return null;
        }
        #endregion
    }
}
#endif
