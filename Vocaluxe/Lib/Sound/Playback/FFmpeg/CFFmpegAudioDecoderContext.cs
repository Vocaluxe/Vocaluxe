using FFmpeg.AutoGen;
using Vocaluxe.Lib.FFmpeg;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.Sound.Playback.FFmpeg
{
    internal sealed unsafe class CFFmpegAudioDecoderContext : CFFmpegDecoderContextBase
    {
        private const AVSampleFormat _OutputSampleFormat = AVSampleFormat.AV_SAMPLE_FMT_S16;

        private SwrContext* _SwrContext;
        private byte* _Buffer;
        private int _BufferLineSize;
        private AVChannelLayout* _ChannelLayout;

        public int SamplesRate { get; private set; }

        public int ChannelCount => _ChannelLayout != null ? _ChannelLayout->nb_channels : 0;

        public int BufferSize { get; private set; }

        public byte* Buffer => _Buffer;

        protected override AVMediaType _MediaType => AVMediaType.AVMEDIA_TYPE_AUDIO;

        protected override bool _InternalInit(AVStream* avStream, AVCodecContext* codecContext)
        {
            SamplesRate = codecContext->sample_rate;
            _ChannelLayout = &codecContext->ch_layout;

            fixed (SwrContext** swrContext = &_SwrContext)
            {
                if (ffmpeg.swr_alloc_set_opts2(swrContext,
                        _ChannelLayout,
                        _OutputSampleFormat,
                        SamplesRate,
                        _ChannelLayout,
                        codecContext->sample_fmt,
                        SamplesRate,
                        0, null) < 0)
                {
                    CLog.Error("Could not allocate resampler context");
                    return false;
                }
            }

            if (ffmpeg.swr_init(_SwrContext) < 0)
            {
                CLog.Error("Could not init resampler");
                return false;
            }

            fixed (byte** buffer = &_Buffer)
            fixed (int* bufferLineSize = &_BufferLineSize)
            {
                BufferSize =
                    ffmpeg.av_samples_alloc(buffer, bufferLineSize, _ChannelLayout->nb_channels, codecContext->frame_size, _OutputSampleFormat, 1);
            }

            if (BufferSize < 0)
            {
                CLog.Error("Unable to allocate the output buffer");
                return false;
            }
            return true;
        }
        protected override bool _ProcessFrame(AVFrame* frame)
        {
            fixed (byte** buffer = &_Buffer)
            {
                var status = ffmpeg.swr_convert(_SwrContext, buffer, frame->nb_samples, (byte**)&frame->data, frame->nb_samples);
                if (status < 0)
                {
                    return false;
                }
            }
            return true;
        }

        protected override void _InternalFree()
        {
            if (_SwrContext != null)
            {
                fixed (SwrContext** swrContext = &_SwrContext)
                {
                    ffmpeg.swr_free(swrContext);
                }
            }

            if (_Buffer != null)
            {
                fixed (byte** buffer = &_Buffer)
                {
                    ffmpeg.av_freep(buffer);
                }
            }

            _ChannelLayout = null;
        }
    }
}
