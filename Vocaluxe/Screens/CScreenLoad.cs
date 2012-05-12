using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Lib.Song;
using Vocaluxe.Menu;

namespace Vocaluxe.Screens
{
    class CScreenLoad: CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;
        const float WaitTime = 0.5f;    //wait time before starting first video

        const string TextStatus = "TextStatus";

        private readonly string[] IntroVideo = new string[] { "IntroIn", "IntroMid", "IntroOut" };
        
        private Thread _SongLoaderThread;
        private Stopwatch _timer;
        private bool _SkipIntro;
        private int _CurrentIntroVideoNr;
        private bool _IntroOutPlayed;
        private VideoPlayer[] _Intros;

        private bool _BGMusicStartet;
        
                
        public CScreenLoad()
        {
            Init();
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "ScreenLoad";
            _ScreenVersion = ScreenVersion;
            _ThemeTexts = new string[] { TextStatus };
            _Intros = new VideoPlayer[IntroVideo.Length];
            for (int i = 0; i < _Intros.Length; i++)
            {
                _Intros[i] = new VideoPlayer();
            }
            _timer = new Stopwatch();
        }

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            base.HandleInput(KeyEvent);

            if (KeyEvent.KeyPressed)
            {

            }
            else
            {
                switch (KeyEvent.Key)
                {
                    case Keys.Enter:
                    case Keys.Escape:
                    case Keys.Space:
                    case Keys.Back:
                        _SkipIntro = true;
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(MouseEvent MouseEvent)
        {
            base.HandleMouse(MouseEvent);

            if (MouseEvent.LB || MouseEvent.RB)
            {
                _SkipIntro = true;
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            //Check if there is a mic-configuration
            if (!CConfig.IsMicConfig())
            {
                //If not, try to assaign players 1 and 2 automatically to usb-mics
                if (CConfig.AutoAssignMics())
                {
                    //Save config
                    CConfig.SaveConfig();
                }
            }
            
            _SongLoaderThread = new Thread(new ThreadStart(CSongs.LoadSongs));
            _SongLoaderThread.Name = "SongLoader";

            Texts[htTexts(TextStatus)].Text = CLanguage.Translate("TR_SCREENLOAD_TOTAL") + ": 0 " +
                CLanguage.Translate("TR_SCREENLOAD_SONGS") + " (0 " +
                CLanguage.Translate("TR_SCREENLOAD_LOADED") + ")";

            _SkipIntro = false;
            _CurrentIntroVideoNr = -1;
            _IntroOutPlayed = false;

            if (CConfig.VideoBackgrounds == EOffOn.TR_CONFIG_ON)
            {
                for (int i = 0; i < _Intros.Length; i++)
                {
                    _Intros[i].Load(IntroVideo[i]);
                }
            }

            _BGMusicStartet = false;
        }

        public override void OnShowFinish()
        {
            base.OnShowFinish();

            if (CConfig.VideoBackgrounds == EOffOn.TR_CONFIG_ON)
            {
                for (int i = 0; i < _Intros.Length; i++)
                {
                    _Intros[i].PreLoad();
                }
            }

            CLog.StartBenchmark(3, "Load Cover");
            _SongLoaderThread.IsBackground = true;
            _SongLoaderThread.Start();
            _timer.Start();
        }

        public override bool UpdateGame()
        {
            CheckStartIntroVideos();

            if (CSettings.GameState == EGameState.EditTheme)
                _timer.Stop();
            else
                _timer.Start();            

            bool next = ((CConfig.CoverLoading == ECoverLoading.TR_CONFIG_COVERLOADING_ATSTART && CSongs.CoverLoaded) ||
                CConfig.CoverLoading != ECoverLoading.TR_CONFIG_COVERLOADING_ATSTART);

            if ((_IntroOutPlayed || _SkipIntro) && next && CSettings.GameState != EGameState.EditTheme && CSongs.SongsLoaded)
            {
                CSettings.GameState = EGameState.Normal;
            }

            if (CSettings.GameState == EGameState.Normal)
                CGraphics.FadeTo(EScreens.ScreenMain);

            Texts[htTexts(TextStatus)].Text =
                CLanguage.Translate("TR_SCREENLOAD_TOTAL") + ": " + CSongs.NumAllSongs.ToString() + " " +
                CLanguage.Translate("TR_SCREENLOAD_SONGS") + " (" + CSongs.NumSongsWithCoverLoaded + " " +
                CLanguage.Translate("TR_SCREENLOAD_LOADED") + ")";

            if (CSongs.SongsLoaded && CConfig.BackgroundMusic == EOffOn.TR_CONFIG_ON &&
                CConfig.BackgroundMusicSource != EBackgroundMusicSource.TR_CONFIG_NO_OWN_MUSIC && !_BGMusicStartet)
            {
                CBackgroundMusic.AddOwnMusic();

                if (!CBackgroundMusic.Playing)
                    CBackgroundMusic.Next();

                _BGMusicStartet = true;
            }
            CBackgroundMusic.CanSing = false;

            return true;
        }

        public override bool Draw()
        {
            DrawBG();

            for (int i = 0; i < _Intros.Length; i++)
            {
                _Intros[i].Draw();
            }

            DrawFG();
            return true;
        }

        public override void OnClose()
        {
            base.OnClose();
            _timer.Stop();
            _timer.Reset();

            for (int i = 0; i < _Intros.Length; i++)
            {
                _Intros[i].Close();
            }

            CBackgroundMusic.CanSing = true;

            CLog.StopBenchmark(3, "Load Cover");
        }

        private void CheckStartIntroVideos()
        {
            if (_IntroOutPlayed)
                return;

            if (CConfig.VideoBackgrounds == EOffOn.TR_CONFIG_OFF)
            {
                _IntroOutPlayed = true;
                return;
            }

            if (_CurrentIntroVideoNr < 0)
            {
                _CurrentIntroVideoNr = 0;
                _Intros[0].Start();
            }

            if (_CurrentIntroVideoNr == 0 && _Intros[0].IsFinished && CConfig.CoverLoading == ECoverLoading.TR_CONFIG_COVERLOADING_ATSTART)
            {
                _CurrentIntroVideoNr = 1;
                _Intros[1].Loop = true;
                _Intros[1].Start();
            }

            if ((_CurrentIntroVideoNr == 1 && CSongs.CoverLoaded) || 
                (_CurrentIntroVideoNr == 0 && _Intros[0].IsFinished && CConfig.CoverLoading != ECoverLoading.TR_CONFIG_COVERLOADING_ATSTART))
            {
                _CurrentIntroVideoNr = 2;
                _Intros[2].Start();
            }

            if (_CurrentIntroVideoNr == 2 && _Intros[2].IsFinished)
                _IntroOutPlayed = true;
        }
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
            _VideoStream = CVideo.VdLoad(CTheme.GetVideoFilePath(VideoName));
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
