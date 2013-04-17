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

        private const string _SelectSlideLanguage = "SelectSlideLanguage";
        private const string _SelectSlideDebugLevel = "SelectSlideDebugLevel";
        private const string _SelectSlideSongMenu = "SelectSlideSongMenu";
        private const string _SelectSlideSongSorting = "SelectSlideSongSorting";
        private const string _SelectSlideTabs = "SelectSlideTabs";
        private const string _SelectSlideTimerMode = "SelectSlideTimerMode";

        private const string _ButtonExit = "ButtonExit";

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[] {_ButtonExit};
            _ThemeSelectSlides = new string[] {_SelectSlideLanguage, _SelectSlideDebugLevel, _SelectSlideSongMenu, _SelectSlideSongSorting, _SelectSlideTabs, _SelectSlideTimerMode};
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            SelectSlides[_SelectSlideLanguage].AddValues(CLanguage.GetLanguageNames());
            SelectSlides[_SelectSlideLanguage].Selection = CLanguage.LanguageId;

            SelectSlides[_SelectSlideDebugLevel].SetValues<EDebugLevel>((int)CConfig.DebugLevel);
            SelectSlides[_SelectSlideSongMenu].SetValues<ESongMenu>((int)CConfig.SongMenu);
            SelectSlides[_SelectSlideSongSorting].SetValues<ESongSorting>((int)CConfig.SongSorting);
            SelectSlides[_SelectSlideTabs].SetValues<EOffOn>((int)CConfig.Tabs);
            SelectSlides[_SelectSlideTimerMode].SetValues<ETimerMode>((int)CConfig.TimerMode);
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (!keyEvent.KeyPressed)
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
            return true;
        }

        public override bool Draw()
        {
            base.Draw();
            return true;
        }

        private void _SaveConfig()
        {
            CLanguage.LanguageId = SelectSlides[_SelectSlideLanguage].Selection;
            CConfig.Language = CLanguage.GetLanguageName(CLanguage.LanguageId);
            CConfig.DebugLevel = (EDebugLevel)SelectSlides[_SelectSlideDebugLevel].Selection;
            CConfig.SongMenu = (ESongMenu)SelectSlides[_SelectSlideSongMenu].Selection;
            CConfig.SongSorting = (ESongSorting)SelectSlides[_SelectSlideSongSorting].Selection;
            CConfig.Tabs = (EOffOn)SelectSlides[_SelectSlideTabs].Selection;
            CConfig.TimerMode = (ETimerMode)SelectSlides[_SelectSlideTimerMode].Selection;

            CConfig.SaveConfig();

            CSongs.Sorter.SongSorting = CConfig.SongSorting;
            CSongs.Categorizer.Tabs = CConfig.Tabs;
        }
    }
}