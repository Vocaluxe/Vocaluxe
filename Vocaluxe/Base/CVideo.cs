using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;

using Vocaluxe.Menu;
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

    class VideoPlayer
    {
        private STexture _VideoTexture;
        private int _VideoStream;
        private Stopwatch _VideoTimer;
        private bool _Finished;
        private bool _Loaded;

        public bool IsFinished
        {
            get { return _Finished || !_Loaded; }
        }

        public bool Loop
        {
            set { CVideo.VdSetLoop(_VideoStream, value); }
        }

        public VideoPlayer()
        {
            _VideoTimer = new Stopwatch();
            _VideoTexture = new STexture(-1);
            _Finished = false;
            _Loaded = false;
        }

        public void Load(string VideoName)
        {
            _VideoStream = CVideo.VdLoad(CTheme.GetVideoFilePath(VideoName, -1));
            _Loaded = true;
        }

        public void Start()
        {
            _VideoTimer.Reset();
            _Finished = false;
            //CVideo.VdSkip(_VideoStream, 0f, 0f);
            _VideoTimer.Start();
        }

        public void Pause()
        {
            _VideoTimer.Stop();
        }

        public void Resume()
        {
            _VideoTimer.Start();
        }

        public void Draw()
        {
            if (!_Finished)
            {

                float VideoTime = _VideoTimer.ElapsedMilliseconds / 1000f;
                _Finished = CVideo.VdFinished(_VideoStream);

                STexture tex = new STexture(-1);
                tex.height = 0f;
                CVideo.VdGetFrame(_VideoStream, ref tex, VideoTime, ref VideoTime);

                if (tex.height > 0)
                {
                    CDraw.RemoveTexture(ref _VideoTexture);
                    _VideoTexture = tex;
                }
            }
            RectangleF bounds = new RectangleF(0f, 0f, CSettings.iRenderW, CSettings.iRenderH);
            RectangleF rect = new RectangleF(0f, 0f, _VideoTexture.width, _VideoTexture.height);
            CHelper.SetRect(bounds, ref rect, rect.Width / rect.Height, EAspect.Crop);

            CDraw.DrawTexture(_VideoTexture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, CSettings.zFar / 4));
        }

        public void PreLoad()
        {
            float VideoTime = 0f;
            while (_VideoTexture.index == -1 && VideoTime < 1f)
            {
                float dummy = 0f;
                CVideo.VdGetFrame(_VideoStream, ref _VideoTexture, 0, ref dummy);
                VideoTime += 0.05f;
            }
        }

        public void Close()
        {
            CVideo.VdClose(_VideoStream);
            CDraw.RemoveTexture(ref _VideoTexture);
            _Loaded = false;
            _Finished = false;
            _VideoTimer.Reset();
        }
    }
}
