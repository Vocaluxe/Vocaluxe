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
using System.IO;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.Sound.Playback.FFmpeg
{
    internal sealed class CAudioDecoderFFmpeg : IDisposable
    {
        private CFFmpegAudioDecoderContext _DecoderContext;

        public bool Open(Stream sourceStream)
        {
            if (_DecoderContext != null)
            {
                CLog.Error("The decoder has already a stream loaded");
                return false;
            }

            _DecoderContext = new CFFmpegAudioDecoderContext();
            if (!_DecoderContext.Initialize(sourceStream))
            {
                Close();
                return false;
            }

            return true;
        }

        public void Close()
        {
            _DecoderContext?.Dispose();
        }

        public unsafe void Decode(out byte[] buffer, out float timeStamp)
        {
            buffer = null;
            timeStamp = -1;
            if (!_DecoderContext.GetFrame())
            {
                return;
            }

            buffer = new byte[_DecoderContext.BufferSize];
            for (var i = 0; i < _DecoderContext.BufferSize; i++)
            {
                buffer[i] = _DecoderContext.Buffer[i];
            }

            timeStamp = (float)_DecoderContext.Position.TotalSeconds;
        }

        public TimeSpan Duration => _DecoderContext?.Duration ?? TimeSpan.Zero;

        public int SampleRate => _DecoderContext?.SamplesRate ?? 0;

        public int ChannelCount => _DecoderContext?.ChannelCount ?? 0;

        public TimeSpan Position => _DecoderContext?.Position ?? TimeSpan.Zero;

        public void SetPosition(float time)
        {
            _DecoderContext?.Seek(false, TimeSpan.FromSeconds(time));
        }

        public void Dispose() { }
    }
}