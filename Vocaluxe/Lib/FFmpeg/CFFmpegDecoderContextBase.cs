using FFmpeg.AutoGen;
using System;
using System.IO;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.FFmpeg
{
    internal abstract unsafe class CFFmpegDecoderContextBase : IDisposable
    {
        private bool _Initialized;
        private Stream _SourceStream;

        private avio_alloc_context_read_packet _ReadSourceStreamCallback;
        private avio_alloc_context_seek _SeekSourceStreamCallback;
        private AVIOContext* _IOContext;
        private AVFormatContext* _FormatContext;
        private AVCodecContext* _CodecContext;
        private AVStream* _AvStream;

        protected abstract AVMediaType _MediaType { get; }

        public TimeSpan Duration => _AvStream == null ? TimeSpan.Zero :
            TimeSpan.FromMilliseconds(ffmpeg.av_rescale_q(_AvStream->duration, _AvStream->time_base, new AVRational { num = 1, den = 1000 }));

        public float FrameRate => _AvStream != null ? _AvStream->r_frame_rate.num /
                                                            (float)_AvStream->r_frame_rate.den : 0;

        public TimeSpan Position { get; private set; } = TimeSpan.Zero;

        #region Init

        public bool Initialize(Stream sourceStream)
        {
            if (_Initialized)
            {
                CLog.Error("Decoder context recycled");
                return false;
            }

            _SourceStream = sourceStream;

            if (!_InitIOContext())
            {
                _Free();
                return false;
            }

            if (!_InitFormatContext())
            {
                _Free();
                return false;
            }

            AVCodec* codec;
            if (!_ChooseStream(&codec))
            {
                _Free();
                return false;
            }

            if (!_InitCodeCodecContext(codec))
            {
                _Free();
                return false;
            }

            if (!_InternalInit(_AvStream, _CodecContext))
            {
                _Free();
                return false;
            }

            _Initialized = true;
            return true;
        }

        protected abstract bool _InternalInit(AVStream* avStream, AVCodecContext* codecContext);

        private int _ReadSourceStream(void* opaque, byte* buffer, int bufferSize)
        {
            if (_SourceStream == null)
            {
                return ffmpeg.AVERROR_EOF;
            }

            lock (_SourceStream)
            {
                var finalBufferSize = (int)Math.Min(bufferSize, _SourceStream.Length - _SourceStream.Position);
                if (finalBufferSize == 0)
                {
                    return ffmpeg.AVERROR_EOF;
                }

                for (var i = 0; i < finalBufferSize; i++)
                {
                    buffer[i] = (byte)_SourceStream.ReadByte();
                }

                return finalBufferSize;
            }
        } 
        
        private long _SeekSourceStream(void* opaque, long offset, int whence)
        {
            if (_SourceStream == null)
            {
                return -1;
            }

            lock (_SourceStream)
            {
                return whence == ffmpeg.AVSEEK_SIZE ?
                    _SourceStream.Length :
                    _SourceStream.Seek(offset, (SeekOrigin)whence);
            }
        }

        private bool _InitIOContext()
        {
            if (!_SourceStream.CanRead)
            {
                CLog.Error("The source stream is not readable");
                return false;
            }

            _ReadSourceStreamCallback = _ReadSourceStream;
            _SeekSourceStreamCallback = _SourceStream.CanSeek ? _SeekSourceStream : null;

            var ioBuffer = (byte*)ffmpeg.av_malloc(FFmpegHelper.IOBufferSize);
            if (ioBuffer == null)
            {
                CLog.Error("Unable to alloc the AVIO buffer");
                return false;
            }

            _IOContext = ffmpeg.avio_alloc_context(ioBuffer, FFmpegHelper.IOBufferSize, 0, null, _ReadSourceStreamCallback, null, _SeekSourceStreamCallback);
            if (_IOContext == null)
            {
                CLog.Error("Unable to alloc the AVIO context");
                return false;
            }
            return true;
        }

        private bool _InitFormatContext()
        {
            _FormatContext = ffmpeg.avformat_alloc_context();
            if (_FormatContext == null)
            {
                CLog.Error("Unable to alloc the format context");
                return false;
            }

            _FormatContext->pb = _IOContext;
            fixed (AVFormatContext** formatContext = &_FormatContext)
            {
                if (ffmpeg.avformat_open_input(formatContext, null, null, null) < 0)
                {
                    CLog.Error("Error getting format context from video stream");
                    return false;
                }
            }

            return true;
        }

        private bool _ChooseStream(AVCodec** codec)
        {
            if (ffmpeg.avformat_find_stream_info(_FormatContext, null) < 0)
            {
                CLog.Error("Could not find stream info in video stream");
                return false;
            }

            var matchingStreamIndex = ffmpeg.av_find_best_stream(_FormatContext, _MediaType, -1, -1, codec, 0);
            if (matchingStreamIndex < 0)
            {
                CLog.Error($"Unable to find a stream with {_MediaType}");
                return false;
            }

            _AvStream = _FormatContext->streams[matchingStreamIndex];
            return true;
        }

        private bool _InitCodeCodecContext(AVCodec* codec)
        {
            _CodecContext = ffmpeg.avcodec_alloc_context3(codec);
            if (_CodecContext == null)
            {
                CLog.Error("Unable to alloc the codec context");
                return false;
            }

            if (ffmpeg.avcodec_parameters_to_context(_CodecContext, _AvStream->codecpar) < 0)
            {
                CLog.Error("Unable to fill codec parameters");
                return false;
            }

            if (ffmpeg.avcodec_open2(_CodecContext, codec, null) < 0)
            {
                CLog.Error("Unable to open the codec");
                return false;
            }

            return true;
        }

        #endregion

        #region Process

        private static AVPacket* _ReadPacket(AVFormatContext* formatContext)
        {
            var packet = ffmpeg.av_packet_alloc();
            if (packet == null)
            {
                return null;
            }

            var readFrameStatus = ffmpeg.av_read_frame(formatContext, packet);
            if (readFrameStatus >= 0)
            {
                if (packet->dts != ffmpeg.AV_NOPTS_VALUE)
                {
                    packet->pts = (int)packet->dts;
                }
                return packet;
            }

            ffmpeg.av_packet_free(&packet);
            return null;
        }

        protected abstract bool _ProcessFrame(AVFrame* frame);

        private int _DecodePacket(AVPacket* packet)
        {
            var packetStatus = ffmpeg.avcodec_send_packet(_CodecContext, packet);
            if (packetStatus < 0)
            {
                CLog.Error("Error sending packet to decoder");
                return -1;
            }

            var frame = ffmpeg.av_frame_alloc();
            if (frame == null)
            {
                CLog.Error("Unable to alloc frame");
                return -1;
            }

            try
            {
                int avCodecReceiveFrameResponse;
                do
                {
                    avCodecReceiveFrameResponse = ffmpeg.avcodec_receive_frame(_CodecContext, frame);
                    if (avCodecReceiveFrameResponse == ffmpeg.AVERROR(ffmpeg.EAGAIN) ||
                        avCodecReceiveFrameResponse == ffmpeg.AVERROR_EOF)
                    {
                        return avCodecReceiveFrameResponse;
                    }

                } while (avCodecReceiveFrameResponse > 0);

                _ComputePosition(frame);
                if (!_ProcessFrame(frame))
                {
                    return -1;
                }

                return 0;
            }
            finally
            {
                ffmpeg.av_frame_free(&frame);
            }
        }

        private void _ComputePosition(AVFrame* frame)
        {
            var elapsedMs = ffmpeg.av_rescale_q(frame->best_effort_timestamp, _AvStream->time_base, new AVRational { num = 1, den = 1000 });
            Position = TimeSpan.FromMilliseconds(elapsedMs);
        }

        public bool GetFrame()
        {
            do
            {
                var packet = _ReadPacket(_FormatContext);
                if (packet == null)
                {
                    return false;
                }

                if (packet->stream_index != _AvStream->index)
                {
                    continue;
                }

                // The packet is for the video stream, try to decode it
                var status = _DecodePacket(packet);
                ffmpeg.av_packet_free(&packet);

                if (status > 0 || status == ffmpeg.AVERROR(ffmpeg.EAGAIN))
                {
                    continue;
                }

                return status == 0;

            } while (true);
        }

        public bool Seek(bool backward, TimeSpan targetPos)
        {
            var timebase = _AvStream->time_base;

            var flags = backward ? ffmpeg.AVSEEK_FLAG_BACKWARD : 0;

            var pos = ffmpeg.av_rescale((long)targetPos.TotalMilliseconds, ffmpeg.AV_TIME_BASE, 1000);
            Position = targetPos;

            return ffmpeg.av_seek_frame(_FormatContext,
                _AvStream->index,
                ffmpeg.av_rescale_q(pos, new AVRational { num = 1, den = ffmpeg.AV_TIME_BASE }, timebase),
                flags) >= 0;
        }

        private bool _SkipPacket(AVPacket* packet)
        {
            if (ffmpeg.avcodec_send_packet(_CodecContext, packet) < 0)
            {
                return false;
            }

            var frame = ffmpeg.av_frame_alloc();
            if (frame == null)
            {
                CLog.Error("Unable to alloc frame");
                return false;
            }

            try
            {
                if (ffmpeg.avcodec_receive_frame(_CodecContext, frame) < 0)
                {
                    return false;
                }

                _ComputePosition(frame);
                return true;
            }
            finally
            {
                ffmpeg.av_frame_free(&frame);
            }
        }

        public bool DropWithSkip(int frameDropCount)
        {
            // Add 1 dropped frame per 16 frames (Power of 2 -> Div is fast) as skipping takes time too and we don't want to skip again
            frameDropCount += frameDropCount / 16;
            try
            {
                if (_SourceStream == null)
                {
                    return false;
                }

                var skippedFrames = 0;
                while (skippedFrames < frameDropCount)
                {
                    var packet = _ReadPacket(_FormatContext);
                    if (packet != null)
                    {
                        // Decode the package to figure out if its completing a frame
                        if (packet->stream_index == _AvStream->index && _SkipPacket(packet))
                        {
                            skippedFrames++;
                        }

                        ffmpeg.av_packet_free(&packet);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                CLog.Error("Error skipping frame");
                return false;
            }

            return true;
        }

        #endregion

        #region Free

        protected abstract void _InternalFree();

        private void _Free()
        {
            if (_FormatContext != null)
            {
                fixed (AVFormatContext** formatContext = &_FormatContext)
                {
                    ffmpeg.avformat_close_input(formatContext);
                }
            }

            _InternalFree();

            if (_CodecContext != null)
            {
                fixed (AVCodecContext** codecContext = &_CodecContext)
                {
                    ffmpeg.avcodec_free_context(codecContext);
                }
            }

            if (_IOContext != null)
            {
                ffmpeg.av_freep(&_IOContext->buffer);
                fixed (AVIOContext** ioContext = &_IOContext)
                {
                    ffmpeg.avio_context_free(ioContext);
                }
            }

            _AvStream = null;
            _ReadSourceStreamCallback = null;
            _SourceStream = null;
        }

        ~CFFmpegDecoderContextBase()
        {
            Dispose();
        }

        public void Dispose()
        {
            _Free();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
