using System;
using System.Collections.Generic;
using System.Text;
using Vocaluxe.Lib.Video.Gstreamer;

namespace Vocaluxe.Lib.Video
{
    class CVideoDecoderGstreamer : IVideoDecoder
    {
        public bool Init()
        {
            return CGstreamerVideoWrapper.InitVideo();
        }

        public void CloseAll()
        {
            CGstreamerVideoWrapper.CloseAllVideos();
        }

        public int Load(string VideoFileName)
        {
            return CGstreamerVideoWrapper.LoadVideo(VideoFileName);
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

        public bool GetFrame(int StreamID, ref Menu.STexture Frame, float Time, ref float VideoTime)
        {
            IntPtr buffer = CGstreamerVideoWrapper.GetFrame(StreamID, Time, ref VideoTime);
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
    }
}
