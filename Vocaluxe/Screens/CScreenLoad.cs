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

using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    class CScreenLoad : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 2; }
        }

        private const string _TextStatus = "TextStatus";
        private const string _TextProgramName = "TextProgramName";

        private readonly string[] _IntroVideo = new string[] {"IntroIn", "IntroMid", "IntroOut"};

        private Thread _SongLoaderThread;
        private Stopwatch _Timer;
        private bool _SkipIntro;
        private int _CurrentIntroVideoNr;
        private bool _IntroOutPlayed;
        private CVideoPlayer[] _Intros;

        private bool _BGMusicStarted;

        public override void Init()
        {
            base.Init();

            _ThemeTexts = new string[] {_TextStatus, _TextProgramName};
            _Intros = new CVideoPlayer[_IntroVideo.Length];
            for (int i = 0; i < _Intros.Length; i++)
                _Intros[i] = new CVideoPlayer();
            _Timer = new Stopwatch();
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed) {}
            else
            {
                switch (keyEvent.Key)
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

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.LB || mouseEvent.RB)
                _SkipIntro = true;

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

            _SongLoaderThread = new Thread(CSongs.LoadSongs) {Name = "SongLoader"};

            _Texts[_TextStatus].Text = CLanguage.Translate("TR_SCREENLOAD_TOTAL") + ": 0 " +
                                       CLanguage.Translate("TR_SCREENLOAD_SONGS") + " (0 " +
                                       CLanguage.Translate("TR_SCREENLOAD_LOADED") + ")";

            _SkipIntro = false;
            _CurrentIntroVideoNr = -1;
            _IntroOutPlayed = false;

            if (CConfig.VideoBackgrounds == EOffOn.TR_CONFIG_ON)
            {
                for (int i = 0; i < _Intros.Length; i++)
                    _Intros[i].Load(_IntroVideo[i]);
                _Texts[_TextProgramName].Visible = false;
            }

            _BGMusicStarted = false;
        }

        public override void OnShowFinish()
        {
            base.OnShowFinish();

            if (CConfig.VideoBackgrounds == EOffOn.TR_CONFIG_ON)
            {
                foreach (CVideoPlayer videoPlayer in _Intros)
                    videoPlayer.PreLoad();
            }

            CLog.StartBenchmark(0, "Load Songs Full");
            _SongLoaderThread.IsBackground = true;
            _SongLoaderThread.Start();
            _Timer.Start();

            if (CConfig.BackgroundMusic == EOffOn.TR_CONFIG_ON &&
                CConfig.BackgroundMusicSource == EBackgroundMusicSource.TR_CONFIG_NO_OWN_MUSIC && !_BGMusicStarted)
            {
                CBackgroundMusic.AddOwnMusic();

                if (!CBackgroundMusic.IsPlaying)
                    CBackgroundMusic.Next();

                _BGMusicStarted = true;
            }

            CBackgroundMusic.CanSing = false;
        }

        public override bool UpdateGame()
        {
            _CheckStartIntroVideos();

            if (CSettings.GameState == EGameState.EditTheme)
                _Timer.Stop();
            else
                _Timer.Start();

            bool next = CConfig.CoverLoading != ECoverLoading.TR_CONFIG_COVERLOADING_ATSTART || CSongs.CoverLoaded;

            if ((_IntroOutPlayed || _SkipIntro) && next && CSettings.GameState != EGameState.EditTheme && CSongs.SongsLoaded)
                CSettings.GameState = EGameState.Normal;

            if (CSettings.GameState == EGameState.Normal)
                CGraphics.FadeTo(EScreens.ScreenMain);

            _Texts[_TextStatus].Text =
                CLanguage.Translate("TR_SCREENLOAD_TOTAL") + ": " + CSongs.NumAllSongs + " " +
                CLanguage.Translate("TR_SCREENLOAD_SONGS") + " (" + CSongs.NumSongsWithCoverLoaded + " " +
                CLanguage.Translate("TR_SCREENLOAD_LOADED") + ")";

            if (CSongs.SongsLoaded && CConfig.BackgroundMusic == EOffOn.TR_CONFIG_ON &&
                CConfig.BackgroundMusicSource != EBackgroundMusicSource.TR_CONFIG_NO_OWN_MUSIC && !_BGMusicStarted)
            {
                CBackgroundMusic.AddOwnMusic();

                if (!CBackgroundMusic.IsPlaying)
                    CBackgroundMusic.Next();

                _BGMusicStarted = true;
            }

            return true;
        }

        public override bool Draw()
        {
            _DrawBG();

            foreach (CVideoPlayer videoPlayer in _Intros)
                videoPlayer.Draw();

            _DrawFG();
            return true;
        }

        public override void OnClose()
        {
            base.OnClose();
            _Timer.Stop();
            _Timer.Reset();

            foreach (CVideoPlayer videoPlayer in _Intros)
                videoPlayer.Close();

            CBackgroundMusic.CanSing = true;

            CLog.StopBenchmark(0, "Load Songs Full");

            //Init Playlists
            CLog.StartBenchmark(0, "Init Playlists");
            CPlaylists.Init();
            CLog.StopBenchmark(0, "Init Playlists");
        }

        private void _CheckStartIntroVideos()
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
            else if (_CurrentIntroVideoNr == 0 && _Intros[0].IsFinished && CConfig.CoverLoading == ECoverLoading.TR_CONFIG_COVERLOADING_ATSTART)
            {
                _CurrentIntroVideoNr = 1;
                _Intros[1].Loop = true;
                _Intros[1].Start();
            }
            else if ((_CurrentIntroVideoNr == 1 && CSongs.CoverLoaded) ||
                     (_CurrentIntroVideoNr == 0 && _Intros[0].IsFinished))
            {
                _CurrentIntroVideoNr = 2;
                _Intros[2].Start();
            }
            else if (_CurrentIntroVideoNr == 2 && _Intros[2].IsFinished)
                _IntroOutPlayed = true;
        }
    }
}