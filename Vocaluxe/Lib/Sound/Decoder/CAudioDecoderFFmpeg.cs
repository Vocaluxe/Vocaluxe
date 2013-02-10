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
        private TAc_read_callback _rc;
        private TAc_seek_callback _sc;
        private FileStream _fs;

        private IntPtr _instance = IntPtr.Zero;
        private IntPtr _audiodecoder = IntPtr.Zero;
        
        private TAc_instance _Instance;
        private FormatInfo _FormatInfo;
        private float _CurrentTime;

        private string _FileName;
        private bool _FileOpened = false;

        public override void Init()
        {
            _rc = new TAc_read_callback(read_proc);
            _sc = new TAc_seek_callback(seek_proc);

            _FileOpened = false;

            _Initialized = true;
        }

        public override void Open(string FileName, bool Loop)
        {
            if (!_Initialized)
                return;

            _FileName = FileName;
            _fs = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read);

            _instance = CAcinerella.ac_init();
            CAcinerella.ac_open(_instance, IntPtr.Zero, null, _rc, _sc, null, IntPtr.Zero);

            _Instance = (TAc_instance)Marshal.PtrToStructure(_instance, typeof(TAc_instance));

            if (!_Instance.opened)
            {
                //Free();
                return;
            }

            int AudioStreamIndex = -1;

            TAc_stream_info Info = new TAc_stream_info();
            for (int i = 0; i < _Instance.stream_count; i++)
            {
                CAcinerella.ac_get_stream_info(_instance, i, out Info);

                if (Info.stream_type == TAc_stream_type.AC_STREAM_TYPE_AUDIO)
                {
                    try
                    {
                        _audiodecoder = CAcinerella.ac_create_decoder(_instance, i);
                    }
                    catch (Exception)
                    {
                        return;                        
                    }
                    
                    AudioStreamIndex = i;
                    break;
                }
            }

            if (AudioStreamIndex < 0)
            {
                //Free();
                return;
            }

            TAc_decoder Audiodecoder = (TAc_decoder)Marshal.PtrToStructure(_audiodecoder, typeof(TAc_decoder));

            _FormatInfo = new FormatInfo();

            _FormatInfo.SamplesPerSecond = Audiodecoder.stream_info.audio_info.samples_per_second;
            _FormatInfo.BitDepth = Audiodecoder.stream_info.audio_info.bit_depth;
            _FormatInfo.ChannelCount = Audiodecoder.stream_info.audio_info.channel_count;

            _CurrentTime = 0f;

            if (_FormatInfo.BitDepth != 16)
            {
                CLog.LogError("Unsupported BitDepth in file " + FileName);
                return;
            }
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

        #region Callbacks
        private Int32 read_proc(IntPtr sender, IntPtr buf, Int32 size)
        {
            Int32 r = 0;

            byte[] bb = new byte[size];
            r = _fs.Read(bb, 0, size);
            Marshal.Copy(bb, 0, buf, size);

            return r;
        }

        private Int64 seek_proc(IntPtr sender, Int64 pos, Int32 whence)
        {
            return (Int64)_fs.Seek((long)pos, (SeekOrigin)whence);
        }
        #endregion Callbacks
    }
}
