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
using Vocaluxe.Lib.Video.Acinerella;
using VocaluxeLib;

namespace Vocaluxe.Lib.Video
{
    class CVideoDecoderFFmpeg : IVideoDecoder
    {
        private readonly Dictionary<int, CDecoder> _Decoder = new Dictionary<int, CDecoder>();
        private int _LastID;

        public bool Init()
        {
            CloseAll();
            return true;
        }

        public void CloseAll()
        {
            foreach (CDecoder decoder in _Decoder.Values)
                decoder.Close();
            _Decoder.Clear();
        }

        public CVideoStream Load(string videoFileName)
        {
            var decoder = new CDecoder();

            if (decoder.Open(videoFileName))
            {
                int id = _LastID++;
                _Decoder.Add(id, decoder);
                return new CVideoStream(id);
            }
            return null;
        }

        public void Close(ref CVideoStream stream)
        {
            if (stream == null)
                return;
            CDecoder decoder;
            if (_TryGetDecoder(stream, out decoder))
            {
                decoder.Close();
                _Decoder.Remove(stream.ID);
            }
            stream.SetClosed();
            stream = null;
        }

        public int GetNumStreams()
        {
            return _Decoder.Count;
        }

        public bool GetFrame(CVideoStream stream, float time)
        {
            CDecoder decoder;
            if (_TryGetDecoder(stream, out decoder))
                return decoder.GetFrame(ref stream.Texture, time, out stream.VideoTime);
            stream.VideoTime = 0;
            return false;
        }

        public float GetLength(CVideoStream stream)
        {
            CDecoder decoder;
            if (_TryGetDecoder(stream, out decoder))
                return decoder.Length;
            return 0f;
        }

        public bool Skip(CVideoStream stream, float start, float gap)
        {
            CDecoder decoder;
            if (_TryGetDecoder(stream, out decoder))
                return decoder.Skip(start, gap);
            return false;
        }

        public void SetLoop(CVideoStream stream, bool loop)
        {
            CDecoder decoder;
            if (_TryGetDecoder(stream, out decoder))
                decoder.Loop = loop;
        }

        public void Pause(CVideoStream stream)
        {
            CDecoder decoder;
            if (_TryGetDecoder(stream, out decoder))
                decoder.Paused = true;
        }

        public void Resume(CVideoStream stream)
        {
            CDecoder decoder;
            if (_TryGetDecoder(stream, out decoder))
                decoder.Paused = false;
        }

        public bool Finished(CVideoStream stream)
        {
            CDecoder decoder;
            if (_TryGetDecoder(stream, out decoder))
                return decoder.Finished;
            return true;
        }

        public void Update() {}

        private bool _TryGetDecoder(CVideoStream stream, out CDecoder decoder)
        {
            if (stream == null)
            {
                decoder = null;
                return false;
            }
            return _Decoder.TryGetValue(stream.ID, out decoder);
        }
    }
}