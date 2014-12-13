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

using System.Threading;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    public class CScreenLoad : CMenu
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
        private bool _SkipIntro;
        private int _CurrentIntroVideo;
        private bool _IntroOutPlayed;
        private CVideoPlayer[] _Intros;

        public override void Init()
        {
            base.Init();

            _ThemeTexts = new string[] {_TextStatus, _TextProgramName};
            _Intros = new CVideoPlayer[_IntroVideo.Length];
            for (int i = 0; i < _Intros.Length; i++)
                _Intros[i] = new CVideoPlayer();
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

            _Texts[_TextStatus].Text = CLanguage.Translate("TR_SCREENLOAD_TOTAL") + ": 0 " +
                                       CLanguage.Translate("TR_SCREENLOAD_SONGS") + " (0 " +
                                       CLanguage.Translate("TR_SCREENLOAD_LOADED") + ")";

            _SkipIntro = false;
            _CurrentIntroVideo = -1;
            _IntroOutPlayed = false;

            if (CConfig.Config.Video.VideoBackgrounds == EOffOn.TR_CONFIG_ON)
            {
                for (int i = 0; i < _Intros.Length; i++)
                    _Intros[i].Load(_IntroVideo[i]);
                _Texts[_TextProgramName].Visible = false;
            }
        }

        public override void OnShowFinish()
        {
            base.OnShowFinish();

            if (CConfig.Config.Video.VideoBackgrounds == EOffOn.TR_CONFIG_ON)
            {
                foreach (CVideoPlayer videoPlayer in _Intros)
                    videoPlayer.PreLoad();
            }

            CLog.StartBenchmark("Load Songs Full");
            _SongLoaderThread = new Thread(CSongs.LoadSongs) {Name = "SongLoader", IsBackground = true};
            _SongLoaderThread.Start();
            CBackgroundMusic.OwnSongsAvailable = false;

            CBackgroundMusic.Play();
        }

        public override bool UpdateGame()
        {
            _CheckStartIntroVideos();

            bool next = CConfig.Config.Theme.CoverLoading != ECoverLoading.TR_CONFIG_COVERLOADING_ATSTART || CSongs.CoverLoaded;

            if ((_IntroOutPlayed || _SkipIntro) && next && CSettings.ProgramState == EProgramState.Start && CSongs.SongsLoaded)
            {
                CSettings.ProgramState = EProgramState.Normal;
                CGraphics.FadeTo(EScreen.Main);
            }

            _Texts[_TextStatus].Text =
                CLanguage.Translate("TR_SCREENLOAD_TOTAL") + ": " + CSongs.NumAllSongs + " " +
                CLanguage.Translate("TR_SCREENLOAD_SONGS") + " (" + CSongs.NumSongsWithCoverLoaded + " " +
                CLanguage.Translate("TR_SCREENLOAD_LOADED") + ")";

            if (CSongs.SongsLoaded && !CBackgroundMusic.OwnSongsAvailable)
            {
                CBackgroundMusic.OwnSongsAvailable = true;

                if (CConfig.Config.Video.VideoBackgrounds == EOffOn.TR_CONFIG_ON || CConfig.Config.Video.VideosToBackground == EOffOn.TR_CONFIG_ON)
                    CBackgroundMusic.VideoEnabled = true;

                CBackgroundMusic.Play();
            }

            return true;
        }

        public override void Draw()
        {
            if (_CurrentIntroVideo >= 0 && _CurrentIntroVideo < _Intros.Length)
                _Intros[_CurrentIntroVideo].Draw();

            base.Draw();
        }

        public override void OnClose()
        {
            base.OnClose();

            foreach (CVideoPlayer videoPlayer in _Intros)
                videoPlayer.Close();

            CLog.StopBenchmark("Load Songs Full");

            //Init Playlists
            CLog.StartBenchmark("Init Playlists");
            CPlaylists.Init();
            CLog.StopBenchmark("Init Playlists");
        }

        private void _CheckStartIntroVideos()
        {
            if (_IntroOutPlayed)
                return;

            if (CConfig.Config.Video.VideoBackgrounds == EOffOn.TR_CONFIG_OFF)
            {
                _IntroOutPlayed = true;
                return;
            }

            if (_CurrentIntroVideo < 0)
            {
                _CurrentIntroVideo = 0;
                _Intros[0].Start();
            }
            else if (_CurrentIntroVideo == 0 && _Intros[0].IsFinished && CConfig.Config.Theme.CoverLoading == ECoverLoading.TR_CONFIG_COVERLOADING_ATSTART)
            {
                _Intros[_CurrentIntroVideo].Close();
                _CurrentIntroVideo = 1;
                _Intros[1].Loop = true;
                _Intros[1].Start();
            }
            else if ((_CurrentIntroVideo == 1 && CSongs.CoverLoaded) ||
                     (_CurrentIntroVideo == 0 && _Intros[0].IsFinished))
            {
                _Intros[_CurrentIntroVideo].Close();
                _CurrentIntroVideo = 2;
                _Intros[2].Start();
            }
            else if (_CurrentIntroVideo == 2 && _Intros[2].IsFinished)
                _IntroOutPlayed = true;
        }
    }
}