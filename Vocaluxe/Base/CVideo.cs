using System.Diagnostics;
using System.Drawing;
using Vocaluxe.Lib.Video;
using VocaluxeLib.Menu;

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
                case EVideoDecoder.Gstreamer:
                    _VideoDecoder = new CVideoDecoderGstreamer();
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

        public static int VdLoad(string videoFileName)
        {
            return _VideoDecoder.Load(videoFileName);
        }

        public static bool VdClose(int streamID)
        {
            return _VideoDecoder.Close(streamID);
        }

        public static float VdGetLength(int streamID)
        {
            return _VideoDecoder.GetLength(streamID);
        }

        public static bool VdGetFrame(int streamID, ref STexture frame, float time, ref float videoTime)
        {
            return _VideoDecoder.GetFrame(streamID, ref frame, time, ref videoTime);
        }

        public static bool VdSkip(int streamID, float start, float gap)
        {
            return _VideoDecoder.Skip(streamID, start, gap);
        }

        public static void VdSetLoop(int streamID, bool loop)
        {
            _VideoDecoder.SetLoop(streamID, loop);
        }

        public static void VdPause(int streamID)
        {
            _VideoDecoder.Pause(streamID);
        }

        public static void VdResume(int streamID)
        {
            _VideoDecoder.Resume(streamID);
        }

        public static bool VdFinished(int streamID)
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

    class CVideoPlayer
    {
        private STexture _VideoTexture;
        private int _VideoStream;
        private readonly Stopwatch _VideoTimer;
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

        public CVideoPlayer()
        {
            _VideoTimer = new Stopwatch();
            _VideoTexture = new STexture(-1);
            _Finished = false;
            _Loaded = false;
        }

        public void Load(string videoName)
        {
            _VideoStream = CVideo.VdLoad(CTheme.GetVideoFilePath(videoName, -1));
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
                float videoTime = _VideoTimer.ElapsedMilliseconds / 1000f;
                _Finished = CVideo.VdFinished(_VideoStream);

                STexture tex = new STexture(-1);
                tex.Height = 0f;
                CVideo.VdGetFrame(_VideoStream, ref tex, videoTime, ref videoTime);

                if (tex.Height > 0)
                {
                    CDraw.RemoveTexture(ref _VideoTexture);
                    _VideoTexture = tex;
                }
            }
            RectangleF bounds = new RectangleF(0f, 0f, CSettings.RenderW, CSettings.RenderH);
            RectangleF rect = new RectangleF(0f, 0f, _VideoTexture.Width, _VideoTexture.Height);
            CHelper.SetRect(bounds, ref rect, rect.Width / rect.Height, EAspect.Crop);

            CDraw.DrawTexture(_VideoTexture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, CSettings.ZFar / 4));
        }

        public void PreLoad()
        {
            float videoTime = 0f;
            while (_VideoTexture.Index == -1 && videoTime < 1f)
            {
                float dummy = 0f;
                CVideo.VdGetFrame(_VideoStream, ref _VideoTexture, 0, ref dummy);
                videoTime += 0.05f;
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