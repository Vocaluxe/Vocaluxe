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
using Vocaluxe.Lib.Video.FFmpeg;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.Sound.Playback.Decoder
{
    class CAudioDecoderFFmpeg : IAudioDecoder, IDisposable
    {
        private CFFmpegDecoder _Decoder;

        private SFormatInfo _FormatInfo;
        private float _CurrentTime;
        private const int _MaxDecodeTries = 10;

        private string _FileName;
        private bool _FileOpened;

        public bool Open(string fileName)
        {
            _FileName = fileName;

            try
            {
                _Decoder = new CFFmpegDecoder(EFFmpegStreamType.Audio);
                if (!_Decoder.Open(_FileName))
                {
                    CLog.Error("Error opening audio file: " + _FileName);
                    Close();
                    return false;
                }
            }
            catch (Exception)
            {
                CLog.Error("Error opening audio file: " + _FileName);
                Close();
                return false;
            }

            _FormatInfo = new SFormatInfo
            {
                SamplesPerSecond = _Decoder.SamplesPerSecond,
                BitDepth = 16,
                ChannelCount = _Decoder.ChannelCount
            };

            _CurrentTime = 0f;

            _FileOpened = true;
            return true;
        }

        public void Close()
        {
            if (_Decoder != null)
            {
                _Decoder.Dispose();
                _Decoder = null;
            }

            _FileOpened = false;
        }

        public SFormatInfo GetFormatInfo()
        {
            return _FileOpened ? _FormatInfo : new SFormatInfo();
        }

        public float GetLength()
        {
            return _FileOpened ? _Decoder.Length : 0;
        }

        public void SetPosition(float time)
        {
            if (!_FileOpened)
            {
                return;
            }

            try
            {
                _Decoder.Seek(time <= _CurrentTime, (Int64)(time * 1000f));
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

            var frameFinished = false;
            for (var i = 0; i < _MaxDecodeTries && !frameFinished; i++)
            {
                frameFinished = _Decoder.GetFrame();
            }

            if (frameFinished)
            {
                timeStamp = (float)_Decoder.Timecode;
                _CurrentTime = timeStamp;
                buffer = new byte[_Decoder.BufferSize];

                if (_Decoder.BufferSize > 0)
                {
                    Marshal.Copy(_Decoder.Buffer, buffer, 0, buffer.Length);
                }

                return;
            }

            buffer = null;
            timeStamp = 0f;
        }

        public void Dispose() { }
    }
}
