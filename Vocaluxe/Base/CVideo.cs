using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Lib.Draw;
using Vocaluxe.Lib.Video;

namespace Vocaluxe.Base
{
    static class CVideo
    {
        #region VideoDecoder
        private static IVideoDecoder _VideoDecoder;

        #region Init
        public static void Init()
        {
            switch (CConfig.VideoDecoder)
            {
                case EVideoDecoder.FFmpeg:
                    _VideoDecoder = new CVideoDecoderFFmpeg();
                    break;

                default:
                    _VideoDecoder = new CVideoDecoderFFmpeg();
                    break;
            }

            _VideoDecoder.Init();
        }
        #endregion Init

        #region Interface
        public static bool VdInit()
        {
            return _VideoDecoder.Init();
        }

        public static void VdCloseAll()
        {
            _VideoDecoder.CloseAll();
        }

        public static int GetNumStreams()
        {
            return _VideoDecoder.GetNumStreams();
        }

        public static int VdLoad(string VideoFileName)
        {
            return _VideoDecoder.Load(VideoFileName);
        }

        public static bool VdClose(int StreamID)
        {
            return _VideoDecoder.Close(StreamID);
        }

        public static float VdGetLength(int StreamID)
        {
            return _VideoDecoder.GetLength(StreamID);
        }

        public static bool VdGetFrame(int StreamID, ref STexture Frame, float Time, ref float VideoTime)
        {
            return _VideoDecoder.GetFrame(StreamID, ref Frame, Time, ref VideoTime);
        }

        public static bool VdSkip(int StreamID, float Start, float Gap)
        {
            return _VideoDecoder.Skip(StreamID, Start, Gap);
        }

        public static void VdSetLoop(int StreamID, bool Loop)
        {
            _VideoDecoder.SetLoop(StreamID, Loop);
        }

        public static void VdPause(int StreamID)
        {
            _VideoDecoder.Pause(StreamID);
        }

        public static void VdResume(int StreamID)
        {
            _VideoDecoder.Resume(StreamID);
        }

        public static bool VdFinished(int StreamID)
        {
            return _VideoDecoder.Finished(StreamID);
        }

        #endregion Interface

        #endregion VideoDecoder
    }
}
