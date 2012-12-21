using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;


namespace Vocaluxe.Screens
{
    class CScreenOptionsGame : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        private const string SelectSlideLanguage = "SelectSlideLanguage";
        private const string SelectSlideDebugLevel = "SelectSlideDebugLevel";
        private const string SelectSlideSongMenu = "SelectSlideSongMenu";
        private const string SelectSlideSongSorting = "SelectSlideSongSorting";
        private const string SelectSlideTabs = "SelectSlideTabs";
        private const string SelectSlideTimerMode = "SelectSlideTimerMode";

        private const string ButtonExit = "ButtonExit";

        private ESongSorting _SongSortingOld;
        private EOffOn _TabsOld;
        private string _LanguageOld;
        private string[] _Languages;
        private int _CurrentLang = -1;

        public CScreenOptionsGame()
        {
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "ScreenOptionsGame";
            _ScreenVersion = ScreenVersion;
            _ThemeButtons = new string[] { ButtonExit };
            _ThemeSelectSlides = new string[] { SelectSlideLanguage, SelectSlideDebugLevel, SelectSlideSongMenu, SelectSlideSongSorting, SelectSlideTabs, SelectSlideTimerMode };

            _Languages = CLanguage.GetLanguages().ToArray();

            for (int i = 0; i < _Languages.Length; i++)
            {
                if (_Languages[i] == CConfig.Language)
                {
                    _CurrentLang = i;
                }
            }
        }

        public override void LoadTheme(string XmlPath)
        {
            base.LoadTheme(XmlPath);

            SelectSlides[htSelectSlides(SelectSlideLanguage)].AddValues(_Languages);
            SelectSlides[htSelectSlides(SelectSlideLanguage)].Selection = _CurrentLang;

            SelectSlides[htSelectSlides(SelectSlideDebugLevel)].SetValues<EDebugLevel>((int)CConfig.DebugLevel);
            SelectSlides[htSelectSlides(SelectSlideSongMenu)].SetValues<ESongMenu>((int)CConfig.SongMenu);
            SelectSlides[htSelectSlides(SelectSlideSongSorting)].SetValues<ESongSorting>((int)CConfig.SongSorting);
            SelectSlides[htSelectSlides(SelectSlideTabs)].SetValues<EOffOn>((int)CConfig.Tabs);
            SelectSlides[htSelectSlides(SelectSlideTimerMode)].SetValues<ETimerMode>((int)CConfig.TimerMode);
            
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
                        if (Buttons[htButtons(ButtonExit)].Selected)
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
                if (Buttons[htButtons(ButtonExit)].Selected)
                {
                    CGraphics.FadeTo(EScreens.ScreenOptions);
                }
            }
            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            _SongSortingOld = CConfig.SongSorting;
            _TabsOld = CConfig.Tabs;
            _LanguageOld = CConfig.Language;
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
            CConfig.Language = _Languages[SelectSlides[htSelectSlides(SelectSlideLanguage)].Selection];
            CConfig.DebugLevel = (EDebugLevel)SelectSlides[htSelectSlides(SelectSlideDebugLevel)].Selection;
            CConfig.SongMenu = (ESongMenu)SelectSlides[htSelectSlides(SelectSlideSongMenu)].Selection;
            CConfig.SongSorting = (ESongSorting)SelectSlides[htSelectSlides(SelectSlideSongSorting)].Selection;
            CConfig.Tabs = (EOffOn)SelectSlides[htSelectSlides(SelectSlideTabs)].Selection;
            CConfig.TimerMode = (ETimerMode)SelectSlides[htSelectSlides(SelectSlideTimerMode)].Selection;

            CConfig.SaveConfig();

            if (_SongSortingOld != CConfig.SongSorting || _TabsOld != CConfig.Tabs || _LanguageOld != CConfig.Language)
            {
                CSongs.Sort(CConfig.SongSorting, CConfig.Tabs, CConfig.IgnoreArticles, String.Empty);
                CSongs.Category = -1;
            }

            CLanguage.SetLanguage(CConfig.Language);

            _SongSortingOld = CConfig.SongSorting;
            _TabsOld = CConfig.Tabs;
            _LanguageOld = CConfig.Language;
        }
    }
}
