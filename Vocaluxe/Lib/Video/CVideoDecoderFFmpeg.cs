#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using System.Collections.Generic;
using Vocaluxe.Lib.Video.Acinerella;
using VocaluxeLib.Draw;

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
            foreach (var decoder in _Decoder.Values)
                decoder.Close();
            _Decoder.Clear();
        }

        public int Load(string videoFileName)
        {
            var decoder = new CDecoder();

            if (decoder.Open(videoFileName))
            {
                int id = _LastID++;
                _Decoder.Add(id, decoder);
                return id;
            }
            return -1;
        }

        public bool Close(int streamID)
        {
            CDecoder decoder;
            if (_Decoder.TryGetValue(streamID, out decoder))
            {
                decoder.Close();
                _Decoder.Remove(streamID);
                return true;
            }
            return false;
        }

        public int GetNumStreams()
        {
            return _Decoder.Count;
        }

        public bool GetFrame(int streamID, ref CTexture frame, float time, out float videoTime)
        {
            CDecoder decoder;
            if (_Decoder.TryGetValue(streamID, out decoder))
                return decoder.GetFrame(ref frame, time, out videoTime);
            videoTime = 0;
            return false;
        }

        public float GetLength(int streamID)
        {
            CDecoder decoder;
            if (_Decoder.TryGetValue(streamID, out decoder))
                return decoder.Length;
            return 0f;
        }

        public bool Skip(int streamID, float start, float gap)
        {
            CDecoder decoder;
            if (_Decoder.TryGetValue(streamID, out decoder))
                return decoder.Skip(start, gap);
            return false;
        }

        public void SetLoop(int streamID, bool loop)
        {
            CDecoder decoder;
            if (_Decoder.TryGetValue(streamID, out decoder))
                decoder.Loop = loop;
        }

        public void Pause(int streamID)
        {
            CDecoder decoder;
            if (_Decoder.TryGetValue(streamID, out decoder))
                decoder.Paused = true;
        }

        public void Resume(int streamID)
        {
            CDecoder decoder;
            if (_Decoder.TryGetValue(streamID, out decoder))
                decoder.Paused = false;
        }

        public bool Finished(int streamID)
        {
            CDecoder decoder;
            if (_Decoder.TryGetValue(streamID, out decoder))
                return decoder.Finished;
            return true;
        }

        public void Update() {}
    }
}