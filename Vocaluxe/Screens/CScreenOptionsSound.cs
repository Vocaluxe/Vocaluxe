using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Menu;


namespace Vocaluxe.Screens
{
    class CScreenOptionsSound : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion { get { return 3; } }

        private const string SelectSlideBackgroundMusic = "SelectSlideBackgroundMusic";
        private const string SelectSlideBackgroundMusicVolume = "SelectSlideBackgroundMusicVolume";
        private const string SelectSlideBackgroundMusicSource = "SelectSlideBackgroundMusicSource";
        private const string SelectSlidePreviewMusicVolume = "SelectSlidePreviewMusicVolume";
        private const string SelectSlideGameMusicVolume = "SelectSlideGameMusicVolume";

        private const string ButtonExit = "ButtonExit";

        private int _BackgroundMusicVolume;
        private EOffOn _BackgroundMusic;

        public CScreenOptionsSound()
        {
        }

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[] { ButtonExit };
            _ThemeSelectSlides = new string[] { SelectSlideBackgroundMusic, SelectSlideBackgroundMusicVolume, SelectSlideBackgroundMusicSource, SelectSlidePreviewMusicVolume, SelectSlideGameMusicVolume };
        }

        public override void LoadTheme(string XmlPath)
        {
            base.LoadTheme(XmlPath);
            SelectSlides[SelectSlideBackgroundMusic].SetValues<EOffOn>((int)CConfig.BackgroundMusic);
            SelectSlides[SelectSlideBackgroundMusicVolume].AddValues(new string[] { "0", "5", "10", "15", "20", "25", "30", "35", "40", "45", "50", "55", "60", "65", "70", "75", "80", "85", "90", "95", "100"});
            SelectSlides[SelectSlideBackgroundMusicVolume].Selection = CConfig.BackgroundMusicVolume / 5;
            SelectSlides[SelectSlideBackgroundMusicSource].SetValues<EBackgroundMusicSource>((int)CConfig.BackgroundMusicSource);
            SelectSlides[SelectSlideBackgroundMusicSource].Selection = (int)CConfig.BackgroundMusicSource;
            SelectSlides[SelectSlidePreviewMusicVolume].AddValues(new string[] { "0", "5", "10", "15", "20", "25", "30", "35", "40", "45", "50", "55", "60", "65", "70", "75", "80", "85", "90", "95", "100" });
            SelectSlides[SelectSlidePreviewMusicVolume].Selection = CConfig.PreviewMusicVolume / 5;
            SelectSlides[SelectSlideGameMusicVolume].AddValues(new string[] { "0", "5", "10", "15", "20", "25", "30", "35", "40", "45", "50", "55", "60", "65", "70", "75", "80", "85", "90", "95", "100" });
            SelectSlides[SelectSlideGameMusicVolume].Selection = CConfig.GameMusicVolume / 5;

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
                    case Keys.Escape:
                    case Keys.Back:
                        SaveConfig();
                        CGraphics.FadeTo(EScreens.ScreenOptions);
                        break;

                    case Keys.S:
                        CParty.SetNormalGameMode();
                        CGraphics.FadeTo(EScreens.ScreenSong);
                        break;

                    case Keys.Enter:
                        if (Buttons[ButtonExit].Selected)
                        {
                            SaveConfig();
                            CGraphics.FadeTo(EScreens.ScreenOptions);
                        }   
                        break;

                    case Keys.Left:
                        SaveConfig();
                        break;

                    case Keys.Right:
                        SaveConfig();
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(MouseEvent MouseEvent)
        {
            base.HandleMouse(MouseEvent);

            if (MouseEvent.RB)
            {
                SaveConfig();
                CGraphics.FadeTo(EScreens.ScreenOptions);
            }
            if (MouseEvent.LB && IsMouseOver(MouseEvent))
            {
                SaveConfig();
                if (Buttons[ButtonExit].Selected)
                {
                    CGraphics.FadeTo(EScreens.ScreenOptions);
                }
            }
            return true;
        }

        public override bool UpdateGame()
        {
            if (_BackgroundMusicVolume != CConfig.BackgroundMusicVolume)
            {
                SelectSlides[SelectSlideBackgroundMusicVolume].Selection = CConfig.BackgroundMusicVolume / 5;
            }
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

            SelectSlides[SelectSlideGameMusicVolume].Selection = CConfig.GameMusicVolume / 5;
            SelectSlides[SelectSlidePreviewMusicVolume].Selection = CConfig.PreviewMusicVolume / 5;
        }
        private void SaveConfig()
        {
            CConfig.GameMusicVolume = SelectSlides[SelectSlideGameMusicVolume].Selection * 5;
            CConfig.PreviewMusicVolume = SelectSlides[SelectSlidePreviewMusicVolume].Selection * 5;
            CConfig.SaveConfig();

            EOffOn NewOffOn = (EOffOn)SelectSlides[SelectSlideBackgroundMusic].Selection;
            EBackgroundMusicSource NewSource = (EBackgroundMusicSource)SelectSlides[SelectSlideBackgroundMusicSource].Selection;
            float NewVolume = SelectSlides[SelectSlideBackgroundMusicVolume].Selection * 5;

            CBackgroundMusic.CheckAndApplyConfig(NewOffOn, NewSource, NewVolume);
        }
    }
}
