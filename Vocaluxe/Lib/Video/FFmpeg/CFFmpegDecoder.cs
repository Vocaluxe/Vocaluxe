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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;

namespace Vocaluxe.Lib.Video.FFmpeg
{
    enum EFFmpegStreamType
    {
        Video,
        Audio
    }

    sealed unsafe class CFFmpegDecoder : IDisposable
    {
        private const AVSampleFormat _OutputSampleFormat = AVSampleFormat.AV_SAMPLE_FMT_S16;

        private static readonly AVRational _TimeBaseQ = new AVRational { num = 1, den = ffmpeg.AV_TIME_BASE };

        private readonly object _Lock = new object();
        private readonly EFFmpegStreamType _Type;
        private AVFormatContext* _FormatContext;
        private AVCodecContext* _CodecContext;
        private AVPacket* _Packet;
        private AVFrame* _Frame;
        private SwsContext* _SwsContext;
        private SwrContext* _SwrContext;
        private int _StreamIndex = -1;
        private AVPixelFormat _OutputPixelFormat = AVPixelFormat.AV_PIX_FMT_BGRA;
        private AVColorSpace _ColorSpace = AVColorSpace.AVCOL_SPC_NB;
        private bool _FullRange;
        private SwsContext* _ConfiguredSwsContext;
        private int _SwsColorSpace = -1;
        private int _SwsSourceRange = -1;
        private AVRational _TimeBase;
        private bool _EndOfFile;
        private int _BufferCapacity;
        private byte_ptr4 _ImageData;
        private int4 _ImageLineSize;
        private AVFrame* _TransferFrame;
        private AVPixelFormat _HardwarePixelFormat = AVPixelFormat.AV_PIX_FMT_NONE;
        private AVCodecContext_get_format _GetFormatCallback;

        private static readonly object _HardwareDevicesLock = new object();
        private static readonly Dictionary<AVHWDeviceType, IntPtr> _HardwareDevices = new Dictionary<AVHWDeviceType, IntPtr>();
        private static readonly HashSet<AVHWDeviceType> _FailedHardwareDevices = new HashSet<AVHWDeviceType>();

        public bool HardwareDecoding { get; set; }
        public bool OutputYuv { get; set; }
        public int MaxHeight { get; set; }
        public int DecoderThreads { get; set; }
        public float[] YuvToRgbRows { get; private set; }
        public string HardwareDeviceName { get; private set; }

        public float Length { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public double FramesPerSecond { get; private set; }
        public int SamplesPerSecond { get; private set; }
        public int ChannelCount { get; private set; }
        public double Timecode { get; private set; }
        public IntPtr Buffer { get; private set; }
        public int BufferSize { get; private set; }

        static CFFmpegDecoder()
        {
            DynamicallyLoadedBindings.LibrariesPath = _GetLibrariesPath();
            DynamicallyLoadedBindings.Initialize();
            ffmpeg.av_log_set_level(ffmpeg.AV_LOG_QUIET);
        }

        /// <summary>
        ///     Loads the FFmpeg libraries (done once by the static constructor), e.g. before using libavdevice.
        /// </summary>
        public static void Initialize() { }

        private static string _GetLibrariesPath()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
#if ARCH_X86
            var folder = "ffmpeg86";
#else
            var folder = "ffmpeg64";
#endif
            if (OperatingSystem.IsWindows())
            {
                return Path.Combine(baseDirectory, "libs", "unmanaged", folder);
            }

            var candidates = new[]
            {
                Path.Combine(baseDirectory, "libs", "unmanaged", folder),
                Path.Combine(baseDirectory, "libs", "unmanaged"),
                baseDirectory,
                "/opt/homebrew/opt/ffmpeg@8/lib",
                "/usr/local/opt/ffmpeg@8/lib"
            };
            var library = OperatingSystem.IsMacOS() ? "libavutil.60.dylib" : "libavutil.so.60";
            return candidates.FirstOrDefault(c => File.Exists(Path.Combine(c, library))) ?? "";
        }

        public CFFmpegDecoder(EFFmpegStreamType type)
        {
            _Type = type;
        }

        public bool Open(string fileName)
        {
            lock (_Lock)
            {
                AVFormatContext* formatContext = null;
                if (ffmpeg.avformat_open_input(&formatContext, fileName, null, null) < 0)
                {
                    return false;
                }

                _FormatContext = formatContext;
                if (ffmpeg.avformat_find_stream_info(_FormatContext, null) < 0)
                {
                    _Free();
                    return false;
                }

                Length = _FormatContext->duration > 0 ? _FormatContext->duration * 1000 / ffmpeg.AV_TIME_BASE / 1000f : 0f;

                var mediaType = _Type == EFFmpegStreamType.Video ? AVMediaType.AVMEDIA_TYPE_VIDEO : AVMediaType.AVMEDIA_TYPE_AUDIO;
                for (var i = 0; i < _FormatContext->nb_streams; i++)
                {
                    if (_FormatContext->streams[i]->codecpar->codec_type == mediaType)
                    {
                        _StreamIndex = i;
                        break;
                    }
                }

                if (_StreamIndex < 0 || !_OpenCodec())
                {
                    _Free();
                    return false;
                }

                return true;
            }
        }

        private bool _OpenCodec()
        {
            var stream = _FormatContext->streams[_StreamIndex];
            _TimeBase = stream->time_base;

            var codec = ffmpeg.avcodec_find_decoder(stream->codecpar->codec_id);
            if (codec == null)
            {
                return false;
            }

            if (_Type == EFFmpegStreamType.Video && HardwareDecoding && stream->codecpar->codec_id == AVCodecID.AV_CODEC_ID_AV1)
            {
                var hardwareCodec = ffmpeg.avcodec_find_decoder_by_name("av1");
                if (hardwareCodec != null && hardwareCodec != codec && _CreateCodecContext(hardwareCodec, stream) && _HardwarePixelFormat != AVPixelFormat.AV_PIX_FMT_NONE &&
                    _ProbeHardwareFrame(stream))
                {
                    return _InitOutput(stream);
                }

                _FreeCodecContext();
                _Rewind(stream);
            }

            return _CreateCodecContext(codec, stream) && _InitOutput(stream);
        }

        private bool _CreateCodecContext(AVCodec* codec, AVStream* stream)
        {
            _CodecContext = ffmpeg.avcodec_alloc_context3(codec);
            if (_CodecContext == null || ffmpeg.avcodec_parameters_to_context(_CodecContext, stream->codecpar) < 0)
            {
                return false;
            }

            if (_Type == EFFmpegStreamType.Video)
            {
                _CodecContext->thread_count = Math.Max(0, DecoderThreads);
                _CodecContext->thread_type = ffmpeg.FF_THREAD_FRAME | ffmpeg.FF_THREAD_SLICE;
                if (HardwareDecoding)
                {
                    _InitHardwareDecoding(codec);
                }
            }

            if (ffmpeg.avcodec_open2(_CodecContext, codec, null) < 0)
            {
                return false;
            }

            if (_Packet == null)
            {
                _Packet = ffmpeg.av_packet_alloc();
            }

            if (_Frame == null)
            {
                _Frame = ffmpeg.av_frame_alloc();
            }

            return true;
        }

        private bool _ProbeHardwareFrame(AVStream* stream)
        {
            var hardwareFrame = _ReceiveFrame() && _Frame->hw_frames_ctx != null;
            ffmpeg.av_frame_unref(_Frame);
            _Rewind(stream);
            ffmpeg.avcodec_flush_buffers(_CodecContext);
            return hardwareFrame;
        }

        private void _Rewind(AVStream* stream)
        {
            ffmpeg.av_seek_frame(_FormatContext, _StreamIndex, stream->start_time != ffmpeg.AV_NOPTS_VALUE ? stream->start_time : 0, ffmpeg.AVSEEK_FLAG_BACKWARD);
            _EndOfFile = false;
        }

        private void _FreeCodecContext()
        {
            if (_CodecContext != null)
            {
                var codecContext = _CodecContext;
                ffmpeg.avcodec_free_context(&codecContext);
                _CodecContext = null;
            }

            if (_TransferFrame != null)
            {
                var transferFrame = _TransferFrame;
                ffmpeg.av_frame_free(&transferFrame);
                _TransferFrame = null;
            }

            _HardwarePixelFormat = AVPixelFormat.AV_PIX_FMT_NONE;
            HardwareDeviceName = null;
        }

        private bool _InitOutput(AVStream* stream)
        {
            if (_Type == EFFmpegStreamType.Video)
            {
                Width = _CodecContext->width;
                Height = _CodecContext->height;
                if (stream->r_frame_rate.den > 0)
                {
                    FramesPerSecond = ffmpeg.av_q2d(stream->r_frame_rate);
                }

                if (Width <= 0 || Height <= 0)
                {
                    return false;
                }

                if (MaxHeight > 0 && Height > MaxHeight)
                {
                    Width = Math.Max(1, (int)((long)Width * MaxHeight / Height));
                    Height = MaxHeight;
                }

                _OutputPixelFormat = OutputYuv ? AVPixelFormat.AV_PIX_FMT_YUV420P : AVPixelFormat.AV_PIX_FMT_BGRA;
                _EnsureBuffer(ffmpeg.av_image_get_buffer_size(_OutputPixelFormat, Width, Height, 1));
                BufferSize = ffmpeg.av_image_get_buffer_size(_OutputPixelFormat, Width, Height, 1);
                ffmpeg.av_image_fill_arrays(ref _ImageData, ref _ImageLineSize, (byte*)Buffer, _OutputPixelFormat, Width, Height, 1);
                _UpdateColorRows(AVColorSpace.AVCOL_SPC_UNSPECIFIED, false);
                return true;
            }

            SamplesPerSecond = _CodecContext->sample_rate;
            ChannelCount = _CodecContext->ch_layout.nb_channels;

            AVChannelLayout layout;
            if (_CodecContext->ch_layout.order != AVChannelOrder.AV_CHANNEL_ORDER_UNSPEC)
            {
                ffmpeg.av_channel_layout_copy(&layout, &_CodecContext->ch_layout);
            }
            else
            {
                ffmpeg.av_channel_layout_default(&layout, ChannelCount);
            }

            SwrContext* swrContext = null;
            var result = ffmpeg.swr_alloc_set_opts2(&swrContext, &layout, _OutputSampleFormat, SamplesPerSecond, &layout, _CodecContext->sample_fmt, SamplesPerSecond, 0,
                null);
            ffmpeg.av_channel_layout_uninit(&layout);
            _SwrContext = swrContext;
            return result >= 0 && ffmpeg.swr_init(_SwrContext) >= 0;
        }

        private int _GetSwsColorSpace(AVColorSpace colorSpace)
        {
            switch (colorSpace)
            {
                case AVColorSpace.AVCOL_SPC_BT709:
                    return ffmpeg.SWS_CS_ITU709;
                case AVColorSpace.AVCOL_SPC_BT2020_NCL:
                case AVColorSpace.AVCOL_SPC_BT2020_CL:
                    return ffmpeg.SWS_CS_BT2020;
                case AVColorSpace.AVCOL_SPC_UNSPECIFIED:
                    return Height >= 720 ? ffmpeg.SWS_CS_ITU709 : ffmpeg.SWS_CS_ITU601;
                default:
                    return ffmpeg.SWS_CS_ITU601;
            }
        }

        private void _UpdateColorRows(AVColorSpace colorSpace, bool fullRange)
        {
            if (colorSpace == _ColorSpace && fullRange == _FullRange && YuvToRgbRows != null)
            {
                return;
            }

            _ColorSpace = colorSpace;
            _FullRange = fullRange;

            double kr, kb;
            switch (_GetSwsColorSpace(colorSpace))
            {
                case ffmpeg.SWS_CS_ITU709:
                    kr = 0.2126;
                    kb = 0.0722;
                    break;
                case ffmpeg.SWS_CS_BT2020:
                    kr = 0.2627;
                    kb = 0.0593;
                    break;
                default:
                    kr = 0.299;
                    kb = 0.114;
                    break;
            }

            var kg = 1 - kr - kb;
            var yScale = fullRange ? 1.0 : 255.0 / 219.0;
            var yOffset = fullRange ? 0.0 : -16.0 / 255.0 * yScale;
            var cScale = fullRange ? 1.0 : 255.0 / 224.0;
            const double cCenter = 128.0 / 255.0;

            var rv = 2 * (1 - kr) * cScale;
            var gu = -2 * kb * (1 - kb) / kg * cScale;
            var gv = -2 * kr * (1 - kr) / kg * cScale;
            var bu = 2 * (1 - kb) * cScale;

            YuvToRgbRows = new[]
            {
                (float)yScale, 0f, (float)rv, (float)(yOffset - rv * cCenter),
                (float)yScale, (float)gu, (float)gv, (float)(yOffset - (gu + gv) * cCenter),
                (float)yScale, (float)bu, 0f, (float)(yOffset - bu * cCenter)
            };
        }

        private static AVHWDeviceType[] _GetHardwareDeviceTypes()
        {
            if (OperatingSystem.IsWindows())
            {
                return new[] { AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA, AVHWDeviceType.AV_HWDEVICE_TYPE_DXVA2 };
            }

            if (OperatingSystem.IsMacOS())
            {
                return new[] { AVHWDeviceType.AV_HWDEVICE_TYPE_VIDEOTOOLBOX };
            }

            return new[] { AVHWDeviceType.AV_HWDEVICE_TYPE_VAAPI, AVHWDeviceType.AV_HWDEVICE_TYPE_VDPAU };
        }

        private static AVPixelFormat _GetHardwarePixelFormat(AVCodec* codec, AVHWDeviceType type)
        {
            const int hwConfigMethodHwDeviceContext = 0x01;
            AVCodecHWConfig* config;
            for (var i = 0; (config = ffmpeg.avcodec_get_hw_config(codec, i)) != null; i++)
            {
                if ((config->methods & hwConfigMethodHwDeviceContext) != 0 && config->device_type == type)
                {
                    return config->pix_fmt;
                }
            }

            return AVPixelFormat.AV_PIX_FMT_NONE;
        }

        private static AVBufferRef* _GetHardwareDevice(AVHWDeviceType type)
        {
            lock (_HardwareDevicesLock)
            {
                if (_FailedHardwareDevices.Contains(type))
                {
                    return null;
                }

                IntPtr device;
                if (!_HardwareDevices.TryGetValue(type, out device))
                {
                    AVBufferRef* deviceContext = null;
                    if (ffmpeg.av_hwdevice_ctx_create(&deviceContext, type, null, null, 0) < 0)
                    {
                        _FailedHardwareDevices.Add(type);
                        return null;
                    }

                    device = (IntPtr)deviceContext;
                    _HardwareDevices.Add(type, device);
                }

                return ffmpeg.av_buffer_ref((AVBufferRef*)device);
            }
        }

        private void _InitHardwareDecoding(AVCodec* codec)
        {
            foreach (var type in _GetHardwareDeviceTypes())
            {
                var pixelFormat = _GetHardwarePixelFormat(codec, type);
                if (pixelFormat == AVPixelFormat.AV_PIX_FMT_NONE)
                {
                    continue;
                }

                var device = _GetHardwareDevice(type);
                if (device == null)
                {
                    continue;
                }

                _HardwarePixelFormat = pixelFormat;
                _CodecContext->hw_device_ctx = device;
                _GetFormatCallback = _GetFormat;
                _CodecContext->get_format = _GetFormatCallback;
                _TransferFrame = ffmpeg.av_frame_alloc();
                HardwareDeviceName = ffmpeg.av_hwdevice_get_type_name(type);
                return;
            }
        }

        private AVPixelFormat _GetFormat(AVCodecContext* context, AVPixelFormat* formats)
        {
            var fallback = AVPixelFormat.AV_PIX_FMT_NONE;
            for (var format = formats; *format != AVPixelFormat.AV_PIX_FMT_NONE; format++)
            {
                if (*format == _HardwarePixelFormat)
                {
                    return *format;
                }

                if (fallback == AVPixelFormat.AV_PIX_FMT_NONE)
                {
                    var descriptor = ffmpeg.av_pix_fmt_desc_get(*format);
                    if (descriptor != null && (descriptor->flags & ffmpeg.AV_PIX_FMT_FLAG_HWACCEL) == 0)
                    {
                        fallback = *format;
                    }
                }
            }

            HardwareDeviceName = null;
            return fallback;
        }

        private void _EnsureBuffer(int size)
        {
            if (size <= _BufferCapacity)
            {
                return;
            }

            Buffer = Buffer == IntPtr.Zero ? Marshal.AllocHGlobal(size) : Marshal.ReAllocHGlobal(Buffer, (IntPtr)size);
            _BufferCapacity = size;
        }

        private bool _ReceiveFrame()
        {
            while (true)
            {
                var result = ffmpeg.avcodec_receive_frame(_CodecContext, _Frame);
                if (result >= 0)
                {
                    _UpdateTimecode();
                    return true;
                }

                if (result != ffmpeg.AVERROR(ffmpeg.EAGAIN) || _EndOfFile)
                {
                    return false;
                }

                while (true)
                {
                    if (ffmpeg.av_read_frame(_FormatContext, _Packet) < 0)
                    {
                        _EndOfFile = true;
                        ffmpeg.avcodec_send_packet(_CodecContext, null);
                        break;
                    }

                    var isOwnStream = _Packet->stream_index == _StreamIndex;
                    if (isOwnStream)
                    {
                        ffmpeg.avcodec_send_packet(_CodecContext, _Packet);
                    }

                    ffmpeg.av_packet_unref(_Packet);
                    if (isOwnStream)
                    {
                        break;
                    }
                }
            }
        }

        private void _UpdateTimecode()
        {
            var timestamp = _Frame->best_effort_timestamp;
            if (timestamp != ffmpeg.AV_NOPTS_VALUE)
            {
                Timecode = timestamp * ffmpeg.av_q2d(_TimeBase);
            }
        }

        private bool _ConvertVideoFrame()
        {
            var frame = _Frame;
            if (frame->hw_frames_ctx != null)
            {
                ffmpeg.av_frame_unref(_TransferFrame);
                if (ffmpeg.av_hwframe_transfer_data(_TransferFrame, frame, 0) < 0)
                {
                    return false;
                }

                frame = _TransferFrame;
            }

            var format = (AVPixelFormat)frame->format;
            if (OutputYuv && (format == AVPixelFormat.AV_PIX_FMT_YUV420P || format == AVPixelFormat.AV_PIX_FMT_YUVJ420P) && frame->width == Width && frame->height == Height)
            {
                _UpdateColorRows(_Frame->colorspace, _Frame->color_range == AVColorRange.AVCOL_RANGE_JPEG || format == AVPixelFormat.AV_PIX_FMT_YUVJ420P);
                var data = new byte_ptr4();
                var lineSize = new int4();
                for (uint i = 0; i < 4; i++)
                {
                    data[i] = frame->data[i];
                    lineSize[i] = frame->linesize[i];
                }

                return ffmpeg.av_image_copy_to_buffer((byte*)Buffer, BufferSize, data, lineSize, AVPixelFormat.AV_PIX_FMT_YUV420P, Width, Height, 1) >= 0;
            }

            _UpdateColorRows(_Frame->colorspace, false);
            _SwsContext = ffmpeg.sws_getCachedContext(_SwsContext, frame->width, frame->height, format, Width, Height, _OutputPixelFormat,
                (int)SwsFlags.SWS_BICUBIC, null, null, null);
            if (_SwsContext == null)
            {
                return false;
            }

            if (_OutputPixelFormat == AVPixelFormat.AV_PIX_FMT_BGRA)
            {
                var colorSpace = _GetSwsColorSpace(_Frame->colorspace);
                var sourceRange = _Frame->color_range == AVColorRange.AVCOL_RANGE_JPEG || format == AVPixelFormat.AV_PIX_FMT_YUVJ420P ||
                                  format == AVPixelFormat.AV_PIX_FMT_YUVJ422P || format == AVPixelFormat.AV_PIX_FMT_YUVJ444P ? 1 : 0;
                if (_SwsContext != _ConfiguredSwsContext || colorSpace != _SwsColorSpace || sourceRange != _SwsSourceRange)
                {
                    var sourceTable = *(int4*)ffmpeg.sws_getCoefficients(colorSpace);
                    var targetTable = *(int4*)ffmpeg.sws_getCoefficients(ffmpeg.SWS_CS_DEFAULT);
                    ffmpeg.sws_setColorspaceDetails(_SwsContext, sourceTable, sourceRange, targetTable, 1, 0, 1 << 16, 1 << 16);
                    _ConfiguredSwsContext = _SwsContext;
                    _SwsColorSpace = colorSpace;
                    _SwsSourceRange = sourceRange;
                }
            }

            return ffmpeg.sws_scale(_SwsContext, frame->data.ToArray(), frame->linesize.ToArray(), 0, frame->height, _ImageData.ToArray(), _ImageLineSize.ToArray()) > 0;
        }

        private bool _ConvertAudioFrame()
        {
            var outSamples = ffmpeg.swr_get_out_samples(_SwrContext, _Frame->nb_samples);
            if (outSamples <= 0)
            {
                BufferSize = 0;
                return true;
            }

            var bytesPerSample = ChannelCount * sizeof(short);
            _EnsureBuffer(outSamples * bytesPerSample);
            var output = (byte*)Buffer;
            var input = _Frame->extended_data;
            var converted = ffmpeg.swr_convert(_SwrContext, &output, outSamples, input, _Frame->nb_samples);
            if (converted < 0)
            {
                return false;
            }

            BufferSize = converted * bytesPerSample;
            return true;
        }

        public bool GetFrame()
        {
            lock (_Lock)
            {
                if (_CodecContext == null || !_ReceiveFrame())
                {
                    return false;
                }

                return _Type == EFFmpegStreamType.Video ? _ConvertVideoFrame() : _ConvertAudioFrame();
            }
        }

        public bool SkipFrames(int count)
        {
            lock (_Lock)
            {
                if (_CodecContext == null)
                {
                    return false;
                }

                for (var i = 0; i < count; i++)
                {
                    if (!_ReceiveFrame())
                    {
                        return false;
                    }
                }

                return _Type != EFFmpegStreamType.Video || _ConvertVideoFrame();
            }
        }

        public bool Seek(bool backward, long targetMilliseconds)
        {
            lock (_Lock)
            {
                if (_CodecContext == null)
                {
                    return false;
                }

                Timecode = targetMilliseconds / 1000.0;
                var position = ffmpeg.av_rescale_q(targetMilliseconds * 1000, _TimeBaseQ, _TimeBase);
                if (ffmpeg.av_seek_frame(_FormatContext, _StreamIndex, position, backward ? ffmpeg.AVSEEK_FLAG_BACKWARD : 0) < 0)
                {
                    return false;
                }

                ffmpeg.avcodec_flush_buffers(_CodecContext);
                if (_SwrContext != null)
                {
                    ffmpeg.swr_init(_SwrContext);
                }

                _EndOfFile = false;
                return true;
            }
        }

        private void _Free()
        {
            if (_SwsContext != null)
            {
                ffmpeg.sws_freeContext(_SwsContext);
                _SwsContext = null;
            }

            if (_SwrContext != null)
            {
                var swrContext = _SwrContext;
                ffmpeg.swr_free(&swrContext);
                _SwrContext = null;
            }

            if (_Frame != null)
            {
                var frame = _Frame;
                ffmpeg.av_frame_free(&frame);
                _Frame = null;
            }

            if (_Packet != null)
            {
                var packet = _Packet;
                ffmpeg.av_packet_free(&packet);
                _Packet = null;
            }

            _FreeCodecContext();

            if (_FormatContext != null)
            {
                var formatContext = _FormatContext;
                ffmpeg.avformat_close_input(&formatContext);
                _FormatContext = null;
            }

            if (Buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Buffer);
                Buffer = IntPtr.Zero;
                _BufferCapacity = 0;
                BufferSize = 0;
            }
        }

        public void Dispose()
        {
            lock (_Lock)
            {
                _Free();
            }
        }
    }
}
