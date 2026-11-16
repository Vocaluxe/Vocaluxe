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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using FFmpeg.AutoGen.Abstractions;
using SkiaSharp;
using Vocaluxe.Base;
using Vocaluxe.Lib.Video.FFmpeg;
using VocaluxeLib.Draw;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.Webcam
{
    /// <summary>
    ///     Webcam capture with FFmpeg (libavdevice). Used on macOS (AVFoundation), where AForge/DirectShow is not available.
    ///     The camera is opened by its name, which AVFoundation accepts as input.
    /// </summary>
    sealed unsafe class CFFmpegWebcam : IWebcam
    {
        private const string _InputFormat = "avfoundation";

        // AVFoundation offers no list of the supported modes through the FFmpeg API, so the usual modes are offered.
        // If a mode is not supported, the camera is opened with its default mode instead.
        private static readonly SCapabilities[] _CommonCapabilities =
        {
            new SCapabilities { Width = 640, Height = 480, Framerate = 30 },
            new SCapabilities { Width = 1280, Height = 720, Framerate = 30 },
            new SCapabilities { Width = 1920, Height = 1080, Framerate = 30 }
        };

        private readonly List<SWebcamDevice> _Devices = new List<SWebcamDevice>();
        private readonly object _MutexData = new object();
        private SWebcamConfig _Config;
        private bool _Selected;
        private byte[] _Data;
        private int _Width, _Height;
        private bool _NewFrameAvailable;
        private volatile bool _Paused;
        private volatile bool _StopRequested;
        private Thread _Thread;

        public bool Init()
        {
            CFFmpegDecoder.Initialize();
            ffmpeg.avdevice_register_all();

            foreach (var name in _GetCameraNames())
            {
                _Devices.Add(new SWebcamDevice { Name = name, MonikerString = name, Capabilities = _CommonCapabilities.ToList() });
            }

            if (_Devices.Count == 0)
            {
                // camera list not available: use the default camera
                _Devices.Add(new SWebcamDevice { Name = "Default camera", MonikerString = "0", Capabilities = _CommonCapabilities.ToList() });
            }

            return true;
        }

        private static IEnumerable<string> _GetCameraNames()
        {
            var names = new List<string>();
            try
            {
                var info = new ProcessStartInfo("/usr/sbin/system_profiler", "SPCameraDataType -json")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var process = Process.Start(info))
                {
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(5000);
                    using (var json = JsonDocument.Parse(output))
                    {
                        if (json.RootElement.TryGetProperty("SPCameraDataType", out var cameras))
                        {
                            foreach (var camera in cameras.EnumerateArray())
                            {
                                if (camera.TryGetProperty("_name", out var name) && !string.IsNullOrEmpty(name.GetString()))
                                {
                                    names.Add(name.GetString());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                CLog.Warning(e, "Could not list the cameras");
            }

            return names;
        }

        public void Close()
        {
            DeSelect();
            _Devices.Clear();
        }

        public bool Select(SWebcamConfig config)
        {
            DeSelect();
            if (_Devices.Count < 1)
            {
                return false;
            }

            var device = _Devices.FirstOrDefault(d => d.MonikerString == config.MonikerString);
            if (device.MonikerString == null)
            {
                device = _Devices[0];
            }

            var capabilities = _CommonCapabilities[1];
            if (config.Width > 0 && config.Height > 0)
            {
                capabilities = _CommonCapabilities.OrderBy(c => Math.Abs(c.Width - config.Width) + Math.Abs(c.Height - config.Height)).First();
            }

            _Config = new SWebcamConfig
            {
                MonikerString = device.MonikerString,
                Width = capabilities.Width,
                Height = capabilities.Height,
                Framerate = capabilities.Framerate
            };
            _Selected = true;
            return true;
        }

        public void DeSelect()
        {
            Stop();
            _Selected = false;
            lock (_MutexData)
            {
                _Data = null;
                _NewFrameAvailable = false;
            }
        }

        public void Start()
        {
            if (!_Selected)
            {
                return;
            }

            _Paused = false;
            if (_Thread != null && _Thread.IsAlive)
            {
                return;
            }

            // a capture thread that ended (e.g. the camera could not be opened) is started again

            _StopRequested = false;
            _Thread = new Thread(_CaptureLoop) { Name = "Webcam", IsBackground = true };
            _Thread.Start();
        }

        public void Pause()
        {
            _Paused = true;
        }

        public void Stop()
        {
            var thread = _Thread;
            if (thread == null)
            {
                return;
            }

            _StopRequested = true;
            thread.Join(3000);
            _Thread = null;
        }

        public bool GetFrame(ref CTextureRef frame)
        {
            lock (_MutexData)
            {
                if (_Data == null || !_NewFrameAvailable || _Data.Length != _Width * _Height * 4)
                {
                    return false;
                }

                if (frame == null)
                {
                    frame = CDraw.AddTexture(_Width, _Height, _Data);
                }
                else
                {
                    CDraw.UpdateTexture(frame, _Width, _Height, _Data);
                }

                _NewFrameAvailable = false;
                return true;
            }
        }

        public SKBitmap GetBitmap()
        {
            lock (_MutexData)
            {
                if (_Data == null || _Data.Length != _Width * _Height * 4)
                {
                    return null;
                }

                var bmp = new SKBitmap(new SKImageInfo(_Width, _Height, SKColorType.Bgra8888, SKAlphaType.Unpremul));
                Marshal.Copy(_Data, 0, bmp.GetPixels(), _Data.Length);
                return bmp;
            }
        }

        public SWebcamConfig GetConfig()
        {
            return _Config;
        }

        public SWebcamDevice[] GetDevices()
        {
            return _Devices.ToArray();
        }

        public bool IsDeviceAvailable()
        {
            return _Selected;
        }

        public bool IsCapturing()
        {
            var thread = _Thread;
            return thread != null && thread.IsAlive && !_Paused;
        }

        #region Capture
        private void _CaptureLoop()
        {
            AVFormatContext* format = null;
            AVCodecContext* codec = null;
            AVPacket* packet = null;
            AVFrame* frame = null;
            SwsContext* sws = null;
            try
            {
                format = _Open(_Config);
                if (format == null)
                {
                    CLog.Error("Could not open the camera {Camera}", new object[] { _Config.MonikerString });
                    return;
                }

                var streamIndex = ffmpeg.av_find_best_stream(format, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, null, 0);
                if (streamIndex < 0)
                {
                    return;
                }

                var parameters = format->streams[streamIndex]->codecpar;
                var decoder = ffmpeg.avcodec_find_decoder(parameters->codec_id);
                codec = ffmpeg.avcodec_alloc_context3(decoder);
                if (decoder == null || codec == null || ffmpeg.avcodec_parameters_to_context(codec, parameters) < 0 || ffmpeg.avcodec_open2(codec, decoder, null) < 0)
                {
                    return;
                }

                packet = ffmpeg.av_packet_alloc();
                frame = ffmpeg.av_frame_alloc();
                byte[] buffer = null;

                while (!_StopRequested)
                {
                    if (ffmpeg.av_read_frame(format, packet) < 0)
                    {
                        Thread.Sleep(5);
                        continue;
                    }

                    try
                    {
                        if (packet->stream_index != streamIndex || _Paused || ffmpeg.avcodec_send_packet(codec, packet) < 0)
                        {
                            continue;
                        }

                        while (ffmpeg.avcodec_receive_frame(codec, frame) >= 0)
                        {
                            int width = frame->width, height = frame->height;
                            sws = ffmpeg.sws_getCachedContext(sws, width, height, (AVPixelFormat)frame->format, width, height, AVPixelFormat.AV_PIX_FMT_BGRA,
                                (int)SwsFlags.SWS_BILINEAR, null, null, null);
                            if (sws == null)
                            {
                                continue;
                            }

                            if (buffer == null || buffer.Length != width * height * 4)
                            {
                                buffer = new byte[width * height * 4];
                            }

                            fixed (byte* target = buffer)
                            {
                                ffmpeg.sws_scale(sws, frame->data.ToArray(), frame->linesize.ToArray(), 0, height, new[] { target }, new[] { width * 4 });
                            }

                            lock (_MutexData)
                            {
                                if (_Data == null || _Data.Length != buffer.Length)
                                {
                                    _Data = new byte[buffer.Length];
                                }

                                Buffer.BlockCopy(buffer, 0, _Data, 0, buffer.Length);
                                _Width = width;
                                _Height = height;
                                _NewFrameAvailable = true;
                            }
                        }
                    }
                    finally
                    {
                        ffmpeg.av_packet_unref(packet);
                    }
                }
            }
            catch (Exception e)
            {
                CLog.Error(e, "Webcam capture failed");
            }
            finally
            {
                if (sws != null)
                {
                    ffmpeg.sws_freeContext(sws);
                }

                if (frame != null)
                {
                    ffmpeg.av_frame_free(&frame);
                }

                if (packet != null)
                {
                    ffmpeg.av_packet_free(&packet);
                }

                if (codec != null)
                {
                    ffmpeg.avcodec_free_context(&codec);
                }

                if (format != null)
                {
                    ffmpeg.avformat_close_input(&format);
                }
            }
        }

        /// <summary>
        ///     Opens the camera with the requested mode; falls back to modes the camera is more likely to support.
        /// </summary>
        private static AVFormatContext* _Open(SWebcamConfig config)
        {
            var inputFormat = ffmpeg.av_find_input_format(_InputFormat);
            if (inputFormat == null)
            {
                return null;
            }

            // uyvy422 is supported by most cameras and avoids a conversion in AVFoundation;
            // the last attempt lets the camera choose the pixel format, the mode and the frame rate
            var attempts = new List<(string Size, string Framerate, string PixelFormat)>
            {
                (config.Width + "x" + config.Height, config.Framerate.ToString(), "uyvy422"),
                (null, "30", "uyvy422"),
                (null, "15", "uyvy422"),
                (null, "30", null),
                (null, null, null)
            };
            foreach (var (size, framerate, pixelFormat) in attempts)
            {
                AVDictionary* options = null;
                if (size != null)
                {
                    ffmpeg.av_dict_set(&options, "video_size", size, 0);
                }

                if (framerate != null)
                {
                    ffmpeg.av_dict_set(&options, "framerate", framerate, 0);
                }

                if (pixelFormat != null)
                {
                    ffmpeg.av_dict_set(&options, "pixel_format", pixelFormat, 0);
                }

                AVFormatContext* format = null;
                var result = ffmpeg.avformat_open_input(&format, config.MonikerString + ":none", inputFormat, &options);
                ffmpeg.av_dict_free(&options);
                if (result >= 0 && ffmpeg.avformat_find_stream_info(format, null) >= 0)
                {
                    return format;
                }

                if (format != null)
                {
                    ffmpeg.avformat_close_input(&format);
                }
            }

            return null;
        }
        #endregion
    }
}
