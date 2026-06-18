using FFmpeg.AutoGen;
using Vocaluxe.Base;
using Vocaluxe.Lib.FFmpeg;
using VocaluxeLib;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.Video.FFmpeg
{
    internal unsafe class CFFmpegVideoDecoderContext : CFFmpegDecoderContextBase
    {
        public const AVPixelFormat OutputFormat = AVPixelFormat.AV_PIX_FMT_BGRA;
        private SwsContext* _SwsContext;
        private byte_ptrArray4 _Buffer;
        private int_array4 _BufferLineSize;

        public int FrameBufferSize { get; private set; }

        public byte* DecodedFrameBuffer => _Buffer[0];

        public int FrameWidth { get; private set; }

        public int FrameHeight { get; private set; }

        protected override AVMediaType _MediaType => AVMediaType.AVMEDIA_TYPE_VIDEO;

        private static (int, int) _ComputeFinalSize(AVStream* avStream)
        {
            decimal? maxHeight = CConfig.Config.Graphics.TextureQuality switch
            {
                ETextureQuality.TR_CONFIG_TEXTURE_LOWEST => 360,
                ETextureQuality.TR_CONFIG_TEXTURE_LOW => 480,
                ETextureQuality.TR_CONFIG_TEXTURE_MEDIUM => 720,
                ETextureQuality.TR_CONFIG_TEXTURE_HIGH => 1080,
                _ => null
            };

            decimal sizeFactor = 1;
            var streamHeight = avStream->codecpar->height;
            if (streamHeight > maxHeight)
            {
                sizeFactor = maxHeight.Value / streamHeight;
            }

            return ((int)(avStream->codecpar->width * sizeFactor), (int)(streamHeight * sizeFactor));
        }

        protected override bool _InternalInit(AVStream* avStream, AVCodecContext* codecContext)
        {
            var (width, height) = _ComputeFinalSize(avStream);
            FrameWidth = width;
            FrameHeight = height;

            FrameBufferSize = ffmpeg.av_image_alloc(ref _Buffer, ref _BufferLineSize, width,
                height, OutputFormat, 1);
            if (FrameBufferSize < 0)
            {
                CLog.Error("Unable to alloc frame buffer");
                return false;
            }

            _SwsContext = ffmpeg.sws_getContext(codecContext->width, codecContext->height, codecContext->pix_fmt,
                width, height, OutputFormat, (int)SwsFlags.SWS_BICUBIC, null, null, null);
            if (_SwsContext == null)
            {
                CLog.Error("Unable to alloc SWS context");
                return false;
            }

            return true;
        }

        protected override bool _ProcessFrame(AVFrame* frame)
        {
            if (ffmpeg.sws_scale(_SwsContext, frame->data,
                    frame->linesize,
                    0,
                    frame->height, _Buffer,
                    _BufferLineSize) < 0)
            {
                CLog.Error("Error when scaling the video frame");
                return false;
            }

            return true;
        }

        protected override void _InternalFree()
        {
            ffmpeg.sws_freeContext(_SwsContext);
            _SwsContext = null;


            if (_Buffer[0] != null)
            {
                fixed (byte_ptrArray4* buffer = &_Buffer)
                {
                    ffmpeg.av_freep(buffer);
                }
            }
        }
    }
}
