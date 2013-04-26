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

            _SelectSlides[_SelectSlideLanguage].AddValues(CLanguage.GetLanguageNames());
            _SelectSlides[_SelectSlideLanguage].Selection = CLanguage.LanguageId;

            _SelectSlides[_SelectSlideDebugLevel].SetValues<EDebugLevel>((int)CConfig.DebugLevel);
            _SelectSlides[_SelectSlideSongMenu].SetValues<ESongMenu>((int)CConfig.SongMenu);
            _SelectSlides[_SelectSlideSongSorting].SetValues<ESongSorting>((int)CConfig.SongSorting);
            _SelectSlides[_SelectSlideTabs].SetValues<EOffOn>((int)CConfig.Tabs);
            _SelectSlides[_SelectSlideTimerMode].SetValues<ETimerMode>((int)CConfig.TimerMode);
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
                        if (_Buttons[_ButtonExit].Selected)
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

            if (mouseEvent.LB && _IsMouseOver(mouseEvent))
            {
                _SaveConfig();
                if (_Buttons[_ButtonExit].Selected)
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
            CLanguage.LanguageId = _SelectSlides[_SelectSlideLanguage].Selection;
            CConfig.Language = CLanguage.GetLanguageName(CLanguage.LanguageId);
            CConfig.DebugLevel = (EDebugLevel)_SelectSlides[_SelectSlideDebugLevel].Selection;
            CConfig.SongMenu = (ESongMenu)_SelectSlides[_SelectSlideSongMenu].Selection;
            CConfig.SongSorting = (ESongSorting)_SelectSlides[_SelectSlideSongSorting].Selection;
            CConfig.Tabs = (EOffOn)_SelectSlides[_SelectSlideTabs].Selection;
            CConfig.TimerMode = (ETimerMode)_SelectSlides[_SelectSlideTimerMode].Selection;

            CConfig.SaveConfig();

            CSongs.Sorter.SongSorting = CConfig.SongSorting;
            CSongs.Categorizer.Tabs = CConfig.Tabs;
        }
    }
}