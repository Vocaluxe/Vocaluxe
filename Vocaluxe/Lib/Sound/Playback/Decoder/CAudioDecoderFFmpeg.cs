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
using System.Runtime.InteropServices;
using Vocaluxe.Base;
using Vocaluxe.Lib.Video.Acinerella;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.Sound.Playback.Decoder
{
    class CAudioDecoderFFmpeg : IAudioDecoder, IDisposable
    {
        private IntPtr _InstancePtr = IntPtr.Zero;
        private IntPtr _Audiodecoder = IntPtr.Zero;

        private SACInstance _Instance;
        private SFormatInfo _FormatInfo;
        private float _CurrentTime;

        private string _FileName;
        private bool _FileOpened;

        public bool Open(string fileName)
        {
            _FileName = fileName;

            try
            {
                _InstancePtr = CAcinerella.AcInit();
                CAcinerella.AcOpen2(_InstancePtr, _FileName);
                _Instance = (SACInstance)Marshal.PtrToStructure(_InstancePtr, typeof(SACInstance));
            }
            catch (Exception)
            {
                CLog.Error("Error opening audio file: " + _FileName);
                Close();
                return false;
            }


            if (!_Instance.Opened)
            {
                Close();
                return false;
            }

            int audioStreamIndex;
            SACDecoder audiodecoder;
            try
            {
                _Audiodecoder = CAcinerella.AcCreateAudioDecoder(_InstancePtr);
                audiodecoder = (SACDecoder)Marshal.PtrToStructure(_Audiodecoder, typeof(SACDecoder));
                audioStreamIndex = audiodecoder.StreamIndex;
            }
            catch (Exception)
            {
                CLog.Error("Error opening audio file (can't find decoder): " + _FileName);
                Close();
                return false;
            }

            if (audioStreamIndex < 0)
            {
                Close();
                return false;
            }

            _FormatInfo = new SFormatInfo
                {
                    SamplesPerSecond = audiodecoder.StreamInfo.AudioInfo.SamplesPerSecond,
                    BitDepth = audiodecoder.StreamInfo.AudioInfo.BitDepth,
                    ChannelCount = audiodecoder.StreamInfo.AudioInfo.ChannelCount
                };

            _CurrentTime = 0f;

            if (_FormatInfo.BitDepth != 16)
            {
                CLog.Error("Unsupported BitDepth in file " + fileName);
                Close();
                return false;
            }
            _FileOpened = true;
            return true;
        }

        public void Close()
        {
            if (_Audiodecoder != IntPtr.Zero)
            {
                CAcinerella.AcFreeDecoder(_Audiodecoder);
                _Audiodecoder = IntPtr.Zero;
            }
            if (_InstancePtr != IntPtr.Zero)
            {
                CAcinerella.AcClose(_InstancePtr);
                CAcinerella.AcFree(_InstancePtr);
                _InstancePtr = IntPtr.Zero;
            }

            _FileOpened = false;
        }

        public SFormatInfo GetFormatInfo()
        {
            return _FileOpened ? _FormatInfo : new SFormatInfo();
        }

        public float GetLength()
        {
            return _FileOpened ? _Instance.Info.Duration / 1000f : 0;
        }

        public void SetPosition(float time)
        {
            if (!_FileOpened)
                return;

            try
            {
                CAcinerella.AcSeek(_Audiodecoder, (time > _CurrentTime) ? 0 : -1, (Int64)(time * 1000f));
            }
            catch (Exception)
            {
                CLog.Error("Error seeking in file: " + _FileName);
                Close();
            }
        }

        public float GetPosition()
        {
            return _FileOpened ? _CurrentTime : 0f;
        }

        public void Decode(out byte[] buffer, out float timeStamp)
        {
            if (!_FileOpened)
            {
                buffer = null;
                timeStamp = 0f;
                return;
            }

            bool frameFinished = CAcinerella.AcGetAudioFrame(_InstancePtr, _Audiodecoder);

            if (frameFinished)
            {
                var decoder = (SACDecoder)Marshal.PtrToStructure(_Audiodecoder, typeof(SACDecoder));

                timeStamp = (float)decoder.Timecode;
                _CurrentTime = timeStamp;
                //Console.WriteLine(_CurrentTime.ToString("#0.000") + " Buffer size: " + Decoder.buffer_size.ToString());
                buffer = new byte[decoder.BufferSize];

                if (decoder.BufferSize > 0)
                    Marshal.Copy(decoder.Buffer, buffer, 0, buffer.Length);

                return;
            }

            buffer = null;
            timeStamp = 0f;
        }

        public void Dispose() {}
    }
}