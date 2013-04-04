using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    class CScreenOptionsGame : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string SelectSlideLanguage = "SelectSlideLanguage";
        private const string SelectSlideDebugLevel = "SelectSlideDebugLevel";
        private const string SelectSlideSongMenu = "SelectSlideSongMenu";
        private const string SelectSlideSongSorting = "SelectSlideSongSorting";
        private const string SelectSlideTabs = "SelectSlideTabs";
        private const string SelectSlideTimerMode = "SelectSlideTimerMode";

        private const string ButtonExit = "ButtonExit";

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[] {ButtonExit};
            _ThemeSelectSlides = new string[] {SelectSlideLanguage, SelectSlideDebugLevel, SelectSlideSongMenu, SelectSlideSongSorting, SelectSlideTabs, SelectSlideTimerMode};
        }

        public override void LoadTheme(string XmlPath)
        {
            base.LoadTheme(XmlPath);

            SelectSlides[SelectSlideLanguage].AddValues(CLanguage.GetLanguageNames());
            SelectSlides[SelectSlideLanguage].Selection = CLanguage.LanguageId;

            SelectSlides[SelectSlideDebugLevel].SetValues<EDebugLevel>((int)CConfig.DebugLevel);
            SelectSlides[SelectSlideSongMenu].SetValues<ESongMenu>((int)CConfig.SongMenu);
            SelectSlides[SelectSlideSongSorting].SetValues<ESongSorting>((int)CConfig.SongSorting);
            SelectSlides[SelectSlideTabs].SetValues<EOffOn>((int)CConfig.Tabs);
            SelectSlides[SelectSlideTimerMode].SetValues<ETimerMode>((int)CConfig.TimerMode);
        }

        public override bool HandleInput(KeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (!keyEvent.KeyPressed)
            {
                switch (keyEvent.Key)
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

        public override bool HandleMouse(MouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.RB)
            {
                SaveConfig();
                CGraphics.FadeTo(EScreens.ScreenOptions);
            }

            if (mouseEvent.LB && IsMouseOver(mouseEvent))
            {
                SaveConfig();
                if (Buttons[ButtonExit].Selected)
                    CGraphics.FadeTo(EScreens.ScreenOptions);
            }
            return true;
        }

        public override bool UpdateGame()
        {
            return true;
        }

        public override bool Draw()
        {
            base.Draw();
            return true;
        }

        private void SaveConfig()
        {
            CLanguage.LanguageId = SelectSlides[SelectSlideLanguage].Selection;
            CConfig.Language = CLanguage.GetLanguageName(CLanguage.LanguageId);
            CConfig.DebugLevel = (EDebugLevel)SelectSlides[SelectSlideDebugLevel].Selection;
            CConfig.SongMenu = (ESongMenu)SelectSlides[SelectSlideSongMenu].Selection;
            CConfig.SongSorting = (ESongSorting)SelectSlides[SelectSlideSongSorting].Selection;
            CConfig.Tabs = (EOffOn)SelectSlides[SelectSlideTabs].Selection;
            CConfig.TimerMode = (ETimerMode)SelectSlides[SelectSlideTimerMode].Selection;

            CConfig.SaveConfig();

            CSongs.Sorter.SongSorting = CConfig.SongSorting;
            CSongs.Categorizer.Tabs = CConfig.Tabs;
        }
    }
}