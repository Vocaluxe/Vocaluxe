using System;
using System.IO;
using System.Runtime.InteropServices;
using Vocaluxe.Base;
using Vocaluxe.Lib.Video.Acinerella;

namespace Vocaluxe.Lib.Sound.Decoder
{
    class CAudioDecoderFFmpeg : CAudioDecoder, IDisposable
    {
        private IntPtr _InstancePtr = IntPtr.Zero;
        private IntPtr _Audiodecoder = IntPtr.Zero;

        private SACInstance _Instance;
        private SFormatInfo _FormatInfo;
        private float _CurrentTime;

        private string _FileName;
        private bool _FileOpened;

        public override void Init()
        {
            _FileOpened = false;
            _Initialized = true;
        }

        public override void Open(string fileName, bool loop)
        {
            if (!_Initialized)
                return;

            _FileName = fileName;

            try
            {
                _InstancePtr = CAcinerella.AcInit();
                CAcinerella.AcOpen2(_InstancePtr, _FileName);
                _Instance = (SACInstance)Marshal.PtrToStructure(_InstancePtr, typeof(SACInstance));
            }
            catch (Exception)
            {
                CLog.LogError("Error opening audio file: " + _FileName);
                return;
            }
            

            if (!_Instance.Opened)
            {
                //Free();
                return;
            }

            int audioStreamIndex = -1;
            SACDecoder audiodecoder;
            try
            {
                _Audiodecoder = CAcinerella.AcCreateAudioDecoder(_InstancePtr);
                audiodecoder = (SACDecoder)Marshal.PtrToStructure(_Audiodecoder, typeof(SACDecoder));
                audioStreamIndex = audiodecoder.StreamIndex;
            }
            catch (Exception)
            {
                CLog.LogError("Error opening audio file (can't find decoder): " + _FileName);
                return;
            }

            if (audioStreamIndex < 0)
            {
                //Free();
                return;
            }

            _FormatInfo = new SFormatInfo();

            _FormatInfo.SamplesPerSecond = audiodecoder.StreamInfo.AudioInfo.SamplesPerSecond;
            _FormatInfo.BitDepth = audiodecoder.StreamInfo.AudioInfo.BitDepth;
            _FormatInfo.ChannelCount = audiodecoder.StreamInfo.AudioInfo.ChannelCount;

            _CurrentTime = 0f;

            if (_FormatInfo.BitDepth != 16)
            {
                CLog.LogError("Unsupported BitDepth in file " + fileName);
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

            if (_Audiodecoder != IntPtr.Zero)
                CAcinerella.AcFreeDecoder(_Audiodecoder);

            if (_InstancePtr != IntPtr.Zero)
                CAcinerella.AcClose(_InstancePtr);

            if (_InstancePtr != IntPtr.Zero)
                CAcinerella.AcFree(_InstancePtr);

            _FileOpened = false;
        }

        public override SFormatInfo GetFormatInfo()
        {
            if (!_Initialized && !_FileOpened)
                return new SFormatInfo();

            return _FormatInfo;
        }

        public override float GetLength()
        {
            if (!_Initialized && !_FileOpened)
                return 0f;

            return _Instance.Info.Duration / 1000f;
        }

        public override void SetPosition(float time)
        {
            if (!_Initialized && !_FileOpened)
                return;

            try
            {
                CAcinerella.AcSeek(_Audiodecoder, 0, (Int64)(time * 1000f));
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

        public override void Decode(out byte[] buffer, out float timeStamp)
        {
            if (!_Initialized && !_FileOpened)
            {
                buffer = null;
                timeStamp = 0f;
                return;
            }

            int frameFinished = 0;
            try
            {
                frameFinished = CAcinerella.AcGetAudioFrame(_InstancePtr, _Audiodecoder);
            }
            catch (Exception)
            {
                frameFinished = 0;
            }

            if (frameFinished == 1)
            {
                SACDecoder decoder = (SACDecoder)Marshal.PtrToStructure(_Audiodecoder, typeof(SACDecoder));

                timeStamp = (float)decoder.Timecode;
                _CurrentTime = timeStamp;
                //Console.WriteLine(_CurrentTime.ToString("ReplacedStr:::0:::") + "ReplacedStr:::1:::" + Decoder.buffer_size.ToString());
                buffer = new byte[decoder.BufferSize];

                if (decoder.BufferSize > 0)
                    Marshal.Copy(decoder.Buffer, buffer, 0, buffer.Length);

                return;
            }

            buffer = null;
            timeStamp = 0f;
        }

        public void Dispose()
        {
        }
    }
}