using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using Vocaluxe.Base;
using Vocaluxe.Lib.Video.Acinerella;

namespace Vocaluxe.Lib.Sound.Decoder
{
    class CAudioDecoderFFmpeg: CAudioDecoder
    {
        private IntPtr _instance = IntPtr.Zero;
        private IntPtr _audiodecoder = IntPtr.Zero;
        
        private TAc_instance _Instance;
        private FormatInfo _FormatInfo;
        private float _CurrentTime;

        private string _FileName;
        private bool _FileOpened = false;

        public override void Init()
        {
            _FileOpened = false;
            _Initialized = true;
        }

        public override void Open(string FileName, bool Loop)
        {
            if (!_Initialized)
                return;

            _FileName = FileName;
            _instance = CAcinerella.ac_init();

            int ret = 0;
            if ((ret = CAcinerella.ac_open2(_instance, _FileName)) < 0)
            {
                CLog.LogError("Error opening sound file (Errorcode: " + ret.ToString() + "): " + _FileName);
                return;
            }

            _Instance = (TAc_instance)Marshal.PtrToStructure(_instance, typeof(TAc_instance));

            if (!_Instance.opened || _Instance.info.duration == 0)
            {
                if (!_Instance.opened)
                    CLog.LogError("Can't open sound file: " + _FileName);
                else
                    CLog.LogError("Can't open sound file (length = 0?): " + _FileName);
                return;
            }

            try
            {
                _audiodecoder = CAcinerella.ac_create_audio_decoder(_instance);
            }
            catch (Exception e)
            {
                CLog.LogError("Can't create audio decoder for file: " + _FileName + "; " + e.Message);
                return;
            }

            if (_audiodecoder == IntPtr.Zero)
            {
                CLog.LogError("Can't create audio decoder for file: " + _FileName);
                return;
            }

            TAc_decoder Audiodecoder = (TAc_decoder)Marshal.PtrToStructure(_audiodecoder, typeof(TAc_decoder));
            _FormatInfo = new FormatInfo();
            _FormatInfo.SamplesPerSecond = Audiodecoder.stream_info.audio_info.samples_per_second;
            _FormatInfo.BitDepth = Audiodecoder.stream_info.audio_info.bit_depth;
            _FormatInfo.ChannelCount = Audiodecoder.stream_info.audio_info.channel_count;

            _CurrentTime = 0f;
            _FileOpened = true;
        }

        public override void Close()
        {
            if (!_Initialized)
                return;

            _Initialized = false;

            if (!_FileOpened)
                return;

            if (_audiodecoder != IntPtr.Zero)
                CAcinerella.ac_free_decoder(_audiodecoder);

            if (_instance != IntPtr.Zero)
                CAcinerella.ac_close(_instance);

            if (_instance != IntPtr.Zero)
                CAcinerella.ac_free(_instance);

            _FileOpened = false;
        }

        public override FormatInfo GetFormatInfo()
        {
            if (!_Initialized && !_FileOpened)
                return new FormatInfo();

            return _FormatInfo;
        }

        public override float GetLength()
        {
            if (!_Initialized && !_FileOpened)
                return 0f;

            return (float)_Instance.info.duration / 1000f;
        }

        public override void SetPosition(float Time)
        {
            if (!_Initialized && !_FileOpened)
                return;

            try
            {
                CAcinerella.ac_seek(_audiodecoder, 0, (Int64)(Time * 1000f));
            }
            catch (Exception)
            {
                CLog.LogError("Error seeking in file: " + _FileName);
                Close();
            }
        }

        public override float GetPosition()
        {
            if (!_Initialized && !_FileOpened)
                return 0f;

            return _CurrentTime;
        }

        public override void Decode(out byte[] Buffer, out float TimeStamp)
        {
            if (!_Initialized && !_FileOpened)
            {
                Buffer = null;
                TimeStamp = 0f;
                return;
            }

            int FrameFinished = 0;
            try
            {
                FrameFinished = CAcinerella.ac_get_audio_frame(_instance, _audiodecoder);
            }
            catch (Exception)
            {
                FrameFinished = 0;
            }

            if (FrameFinished == 1)
            {
                TAc_decoder Decoder = (TAc_decoder)Marshal.PtrToStructure(_audiodecoder, typeof(TAc_decoder));

                TimeStamp = (float)Decoder.timecode;
                _CurrentTime = TimeStamp;
                //Console.WriteLine(_CurrentTime.ToString("#0.000") + " Buffer size: " + Decoder.buffer_size.ToString());
                
                Buffer = new byte[Decoder.buffer_size];

                if (Decoder.buffer_size > 0)
                    Marshal.Copy(Decoder.buffer, Buffer, 0, Buffer.Length);
                
                return;
            }

            Buffer = null;
            TimeStamp = 0f;
        }
    }
}
