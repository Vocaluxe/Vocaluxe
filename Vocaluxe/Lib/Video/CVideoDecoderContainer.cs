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

using System.Collections.Generic;
using System.IO;
using Vocaluxe.Lib.Video.FFmpeg;
using VocaluxeLib;

namespace Vocaluxe.Lib.Video
{
    class CVideoDecoderContainer : IVideoDecoderContainer
    {
        private readonly Dictionary<int, CFFmpegVideoDecoder> _Decoders = new();
        private int _LastId;

        public bool Init()
        {
            CloseAll();
            return true;
        }

        public void CloseAll()
        {
            foreach (var decoder in _Decoders.Values)
            {
                decoder.Close();
            }

            _Decoders.Clear();
        }

        public CVideoStream LoadStream(Stream stream)
        {
            var decoder = new CFFmpegVideoDecoder();
            if (!decoder.LoadStream(stream))
            {
                return null;
            }

            var id = _LastId++;
            _Decoders.Add(id, decoder);
            return new CVideoStream(id, stream);
        }

        public void Close(ref CVideoStream stream)
        {
            if (stream == null)
            {
                return;
            }

            if (_TryGetDecoder(stream, out var decoder))
            {
                decoder.Close();
                _Decoders.Remove(stream.Id);
            }

            stream.SetClosed();
            stream = null;
        }

        public int GetNumStreams()
        {
            return _Decoders.Count;
        }

        public bool GetFrame(CVideoStream stream, float time)
        {
            if (_TryGetDecoder(stream, out var decoder))
            {
                return decoder.GetFrame(ref stream.Texture, time, out stream.VideoTime);
            }

            stream.VideoTime = 0;
            return false;
        }

        public float GetDuration(CVideoStream stream)
        {
            return _TryGetDecoder(stream, out var decoder) ? decoder.Duration : 0f;
        }

        public bool Skip(CVideoStream stream, float start, float gap)
        {
            return _TryGetDecoder(stream, out var decoder) && decoder.Skip(start, gap);
        }

        public void SetLoop(CVideoStream stream, bool loop)
        {
            if (_TryGetDecoder(stream, out var decoder))
            {
                decoder.Loop = loop;
            }
        }

        public void Pause(CVideoStream stream)
        {
            if (_TryGetDecoder(stream, out var decoder))
            {
                decoder.Paused = true;
            }
        }

        public void Resume(CVideoStream stream)
        {
            if (_TryGetDecoder(stream, out var decoder))
            {
                decoder.Paused = false;
            }
        }

        public bool Finished(CVideoStream stream)
        {
            return !_TryGetDecoder(stream, out var decoder) || decoder.Finished;
        }

        public void Update() { }

        private bool _TryGetDecoder(CVideoStream stream, out CFFmpegVideoDecoder decoder)
        {
            if (stream != null)
            {
                return _Decoders.TryGetValue(stream.Id, out decoder);
            }

            decoder = null;
            return false;
        }
    }
}