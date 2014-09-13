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

using Vocaluxe.Lib.Video;
using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Base
{
    static class CVideo
    {
        #region VideoDecoder
        private static IVideoDecoder _VideoDecoder;

        #region Init
        public static bool Init()
        {
            if (_VideoDecoder != null)
                return false;
            switch (CConfig.VideoDecoder)
            {
                case EVideoDecoder.FFmpeg:
                    _VideoDecoder = new CVideoDecoderFFmpeg();
                    break;
                default:
                    _VideoDecoder = new CVideoDecoderFFmpeg();
                    break;
            }

            return _VideoDecoder.Init();
        }
        #endregion Init

        #region Interface
        public static void Close()
        {
            if (_VideoDecoder != null)
            {
                _VideoDecoder.CloseAll();
                _VideoDecoder = null;
            }
        }

        public static int GetNumStreams()
        {
            return _VideoDecoder.GetNumStreams();
        }

        public static int Load(string videoFileName)
        {
            return _VideoDecoder.Load(videoFileName);
        }

        public static bool Close(int streamID)
        {
            return _VideoDecoder.Close(streamID);
        }

        public static float GetLength(int streamID)
        {
            return _VideoDecoder.GetLength(streamID);
        }

        public static bool GetFrame(int streamID, ref CTextureRef frame, float time, out float videoTime)
        {
            return _VideoDecoder.GetFrame(streamID, ref frame, time, out videoTime);
        }

        public static bool Skip(int streamID, float start, float gap)
        {
            return _VideoDecoder.Skip(streamID, start, gap);
        }

        public static void SetLoop(int streamID, bool loop)
        {
            _VideoDecoder.SetLoop(streamID, loop);
        }

        public static void Pause(int streamID)
        {
            _VideoDecoder.Pause(streamID);
        }

        public static void Resume(int streamID)
        {
            _VideoDecoder.Resume(streamID);
        }

        public static bool Finished(int streamID)
        {
            return _VideoDecoder.Finished(streamID);
        }

        public static void Update()
        {
            _VideoDecoder.Update();
        }
        #endregion Interface

        #endregion VideoDecoder
    }
}