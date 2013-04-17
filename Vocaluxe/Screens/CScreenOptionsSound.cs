using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    class CScreenOptionsSound : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 3; }
        }

        private const string _SelectSlideBackgroundMusic = "SelectSlideBackgroundMusic";
        private const string _SelectSlideBackgroundMusicVolume = "SelectSlideBackgroundMusicVolume";
        private const string _SelectSlideBackgroundMusicSource = "SelectSlideBackgroundMusicSource";
        private const string _SelectSlidePreviewMusicVolume = "SelectSlidePreviewMusicVolume";
        private const string _SelectSlideGameMusicVolume = "SelectSlideGameMusicVolume";

        private const string _ButtonExit = "ButtonExit";

        private int _BackgroundMusicVolume;
        private EOffOn _BackgroundMusic;

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[] {_ButtonExit};
            _ThemeSelectSlides = new string[]
                {_SelectSlideBackgroundMusic, _SelectSlideBackgroundMusicVolume, _SelectSlideBackgroundMusicSource, _SelectSlidePreviewMusicVolume, _SelectSlideGameMusicVolume};
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);
            SelectSlides[_SelectSlideBackgroundMusic].SetValues<EOffOn>((int)CConfig.BackgroundMusic);
            SelectSlides[_SelectSlideBackgroundMusicVolume].AddValues(new string[]
                {"0", "5", "10", "15", "20", "25", "30", "35", "40", "45", "50", "55", "60", "65", "70", "75", "80", "85", "90", "95", "100"});
            SelectSlides[_SelectSlideBackgroundMusicVolume].Selection = CConfig.BackgroundMusicVolume / 5;
            SelectSlides[_SelectSlideBackgroundMusicSource].SetValues<EBackgroundMusicSource>((int)CConfig.BackgroundMusicSource);
            SelectSlides[_SelectSlideBackgroundMusicSource].Selection = (int)CConfig.BackgroundMusicSource;
            SelectSlides[_SelectSlidePreviewMusicVolume].AddValues(new string[]
                {"0", "5", "10", "15", "20", "25", "30", "35", "40", "45", "50", "55", "60", "65", "70", "75", "80", "85", "90", "95", "100"});
            SelectSlides[_SelectSlidePreviewMusicVolume].Selection = CConfig.PreviewMusicVolume / 5;
            SelectSlides[_SelectSlideGameMusicVolume].AddValues(new string[]
                {"0", "5", "10", "15", "20", "25", "30", "35", "40", "45", "50", "55", "60", "65", "70", "75", "80", "85", "90", "95", "100"});
            SelectSlides[_SelectSlideGameMusicVolume].Selection = CConfig.GameMusicVolume / 5;
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed) {}
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                        _SaveConfig();
                        CGraphics.FadeTo(EScreens.ScreenOptions);
                        break;

                    case Keys.S:
                        CParty.SetNormalGameMode();
                        CGraphics.FadeTo(EScreens.ScreenSong);
                        break;

                    case Keys.Enter:
                        if (Buttons[_ButtonExit].Selected)
                        {
                            _SaveConfig();
                            CGraphics.FadeTo(EScreens.ScreenOptions);
                        }
                        break;

                    case Keys.Left:
                        _SaveConfig();
                        break;

                    case Keys.Right:
                        _SaveConfig();
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.RB)
            {
                _SaveConfig();
                CGraphics.FadeTo(EScreens.ScreenOptions);
            }
            if (mouseEvent.LB && IsMouseOver(mouseEvent))
            {
                _SaveConfig();
                if (Buttons[_ButtonExit].Selected)
                    CGraphics.FadeTo(EScreens.ScreenOptions);
            }
            return true;
        }

        public override bool UpdateGame()
        {
            if (_BackgroundMusicVolume != CConfig.BackgroundMusicVolume)
                SelectSlides[_SelectSlideBackgroundMusicVolume].Selection = CConfig.BackgroundMusicVolume / 5;
            return true;
        }

        public override bool Draw()
        {
            base.Draw();
            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
            _BackgroundMusic = CConfig.BackgroundMusic;
            _BackgroundMusicVolume = CConfig.BackgroundMusicVolume;

            SelectSlides[_SelectSlideGameMusicVolume].Selection = CConfig.GameMusicVolume / 5;
            SelectSlides[_SelectSlidePreviewMusicVolume].Selection = CConfig.PreviewMusicVolume / 5;
        }

        private void _SaveConfig()
        {
            CConfig.GameMusicVolume = SelectSlides[_SelectSlideGameMusicVolume].Selection * 5;
            CConfig.PreviewMusicVolume = SelectSlides[_SelectSlidePreviewMusicVolume].Selection * 5;
            CConfig.SaveConfig();

            EOffOn newOffOn = (EOffOn)SelectSlides[_SelectSlideBackgroundMusic].Selection;
            EBackgroundMusicSource newSource = (EBackgroundMusicSource)SelectSlides[_SelectSlideBackgroundMusicSource].Selection;
            float newVolume = SelectSlides[_SelectSlideBackgroundMusicVolume].Selection * 5;

            CBackgroundMusic.CheckAndApplyConfig(newOffOn, newSource, newVolume);
        }
    }
}