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
using Vocaluxe.Lib.Video;
using VocaluxeLib;

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
            switch (CConfig.Config.Video.VideoDecoder)
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

        public static CVideoStream Load(string videoFileName)
        {
            if (_VideoDecoder == null)
            {
                throw new NotSupportedException("_VideoDecoder is null (already closed?)");
            }
            return _VideoDecoder.Load(videoFileName);
        }

        public static void Close(ref CVideoStream stream)
        {
            //Check for null because the videostreams may close themselves on destroy (GC)
            if (_VideoDecoder != null)
                _VideoDecoder.Close(ref stream);
        }

        public static float GetLength(CVideoStream stream)
        {
            if (stream == null)
            {
                throw new ArgumentException("stream is null");
            }
            if (_VideoDecoder == null)
            {
                throw new NotSupportedException("_VideoDecoder is null (already closed?)");
            }

            return _VideoDecoder.GetLength(stream);
        }

        public static bool GetFrame(CVideoStream stream, float time)
        {
            if (stream == null)
            {
                throw new ArgumentException("stream is null");
            }
            if (_VideoDecoder == null)
            {
                throw new NotSupportedException("_VideoDecoder is null (already closed?)");
            }

            return _VideoDecoder.GetFrame(stream, time);
        }

        public static bool Skip(CVideoStream stream, float start, float gap)
        {
            if (stream == null)
            {
                throw new ArgumentException("stream is null");
            }
            if (_VideoDecoder == null)
            {
                throw new NotSupportedException("_VideoDecoder is null (already closed?)");
            }

            return _VideoDecoder.Skip(stream, start, gap);
        }

        public static void SetLoop(CVideoStream stream, bool loop)
        {
            if (stream == null)
            {
                throw new ArgumentException("stream is null");
            }
            if (_VideoDecoder == null)
            {
                throw new NotSupportedException("_VideoDecoder is null (already closed?)");
            }

            _VideoDecoder.SetLoop(stream, loop);
        }

        public static void Pause(CVideoStream stream)
        {
            if (stream == null)
            {
                throw new ArgumentException("stream is null");
            }
            if (_VideoDecoder == null)
            {
                throw new NotSupportedException("_VideoDecoder is null (already closed?)");
            }

            _VideoDecoder.Pause(stream);
        }

        public static void Resume(CVideoStream stream)
        {
            if (stream == null)
            {
                throw new ArgumentException("stream is null");
            }
            if (_VideoDecoder == null)
            {
                throw new NotSupportedException("_VideoDecoder is null (already closed?)");
            }

            _VideoDecoder.Resume(stream);
        }

        public static bool Finished(CVideoStream stream)
        {
            if (stream == null)
            {
                throw new ArgumentException("stream is null");
            }
            if (_VideoDecoder == null)
            {
                throw new NotSupportedException("_VideoDecoder is null (already closed?)");
            }

            return _VideoDecoder.Finished(stream);
        }

        public static void Update()
        {
            if (_VideoDecoder == null)
            {
                throw new NotSupportedException("_VideoDecoder is null (already closed?)");
            }

            _VideoDecoder.Update();
        }
        #endregion Interface

        #endregion VideoDecoder
    }
}