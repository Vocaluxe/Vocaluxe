using System.Diagnostics;
using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Base
{
    class CVideoPlayer
    {
        private CTextureRef _VideoTexture;
        private int _VideoStream;
        private readonly Stopwatch _VideoTimer = new Stopwatch();
        private bool _Finished;
        private bool _Loaded;

        public bool IsFinished
        {
            get { return _Finished || !_Loaded; }
        }

        public bool Loop
        {
            set { CVideo.SetLoop(_VideoStream, value); }
        }

        public void Load(string videoName)
        {
            _VideoStream = CTheme.GetSkinVideo(videoName, -1, false);
            _Loaded = true;
            CVideo.Pause(_VideoStream);
        }

        public void Start()
        {
            _VideoTimer.Reset();
            _Finished = false;
            //CVideo.VdSkip(_VideoStream, 0f, 0f);
            _VideoTimer.Start();
            CVideo.Resume(_VideoStream);
        }

        public void Pause()
        {
            CVideo.Pause(_VideoStream);
            _VideoTimer.Stop();
        }

        public void Resume()
        {
            CVideo.Resume(_VideoStream);
            _VideoTimer.Start();
        }

        public void Draw()
        {
            if (!_Finished)
            {
                float videoTime = _VideoTimer.ElapsedMilliseconds / 1000f;
                _Finished = CVideo.Finished(_VideoStream);

                CVideo.GetFrame(_VideoStream, ref _VideoTexture, videoTime, out videoTime);
            }
            if (_VideoTexture == null)
                return;

            CDraw.DrawTexture(_VideoTexture, CSettings.RenderRect, EAspect.Crop);
        }

        public void PreLoad()
        {
            bool paused = _VideoTimer.IsRunning;
            if (paused)
                CVideo.Resume(_VideoStream);
            float videoTime = 0f;
            while (_VideoTexture == null && videoTime < 1f)
            {
                float dummy;
                CVideo.GetFrame(_VideoStream, ref _VideoTexture, 0, out dummy);
                videoTime += 0.05f;
            }
            if (paused)
                CVideo.Pause(_VideoStream);
        }

        public void Close()
        {
            CVideo.Close(_VideoStream);
            CDraw.RemoveTexture(ref _VideoTexture);
            _Loaded = false;
            _Finished = false;
            _VideoTimer.Reset();
        }
    }
}