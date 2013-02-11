using System;
using System.Collections.Generic;
using System.Text;
using Vocaluxe.Base;
using Vocaluxe.Lib.Video.Gstreamer;
using Vocaluxe.Menu;

namespace Vocaluxe.Lib.Video
{
    class CVideoDecoderGstreamer : IVideoDecoder
    {
        #region log
        public CGstreamerVideoWrapper.LogCallback Log;

        private void LogHandler(string text)
        {
            CLog.LogError(text);

        }
        #endregion log

        public bool Init()
        {
            bool retval = CGstreamerVideoWrapper.InitVideo();
            Log = new CGstreamerVideoWrapper.LogCallback(LogHandler);
            GC.SuppressFinalize(Log);
            CGstreamerVideoWrapper.SetVideoLogCallback(Log);
            return retval;
        }

        public void CloseAll()
        {
            CGstreamerVideoWrapper.CloseAllVideos();
        }

        public int Load(string VideoFileName)
        {
            int i = -1;
            try
            {
                Uri u = new Uri(VideoFileName);
                i = CGstreamerVideoWrapper.LoadVideo(u.AbsoluteUri);
                return i;
            }
            catch (Exception e) { }
            return i;
        }

        public bool Close(int StreamID)
        {
            return CGstreamerVideoWrapper.CloseVideo(StreamID);
        }

        public int GetNumStreams()
        {
            return CGstreamerVideoWrapper.GetVideoNumStreams();
        }

        public float GetLength(int StreamID)
        {
            return CGstreamerVideoWrapper.GetVideoLength(StreamID);
        }

        public bool GetFrame(int StreamID, ref STexture Frame, float Time, ref float VideoTime)
        {
            //IntPtr buffer = CGstreamerVideoWrapper.GetFrame(StreamID, Time, ref VideoTime);
            byte[] data = new byte[4 * 4 * 4];
            UploadNewFrame(ref Frame, ref data, 4, 4);
            return true;
        }

        public bool Skip(int StreamID, float Start, float Gap)
        {
            return CGstreamerVideoWrapper.Skip(StreamID, Start, Gap);
        }

        public void SetLoop(int StreamID, bool Loop)
        {
            CGstreamerVideoWrapper.SetVideoLoop(StreamID, Loop);
        }

        public void Pause(int StreamID)
        {
            CGstreamerVideoWrapper.PauseVideo(StreamID);
        }

        public void Resume(int StreamID)
        {
            CGstreamerVideoWrapper.ResumeVideo(StreamID);
        }

        public bool Finished(int StreamID)
        {
            return CGstreamerVideoWrapper.Finished(StreamID);
        }

        public void Update()
        {
            CGstreamerVideoWrapper.UpdateVideo();
        }

        private void UploadNewFrame(ref STexture frame, ref byte[] Data, int Width, int Height)
        {
            if (frame.index == -1 || Width != frame.width || Height != frame.height)
            {
                CDraw.RemoveTexture(ref frame);
                frame = CDraw.AddTexture(Width, Height, ref Data);
            }
            else
            {
                CDraw.UpdateTexture(ref frame, ref Data);
            }
        }
    }
}
