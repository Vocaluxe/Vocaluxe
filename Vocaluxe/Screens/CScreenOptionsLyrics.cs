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

using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    public class CScreenOptionsLyrics : CScreenOptionsBase
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 5; }
        }

        protected override EScreen _OptionsScreen
        {
            get { return EScreen.OptionsLyrics; }
        }

        private const string _SelectSlideLyricStyle = "SelectSlideLyricStyle";
        private const string _SelectSlideLyricsPosition = "SelectSlideLyricsPosition";
        private const string _ButtonExit = "ButtonExit";

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[] { _ButtonExit };
            _ThemeSelectSlides = new string[] { _SelectSlideLyricStyle, _SelectSlideLyricsPosition };
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);
            _SelectSlides[_SelectSlideLyricStyle].SetValues<ELyricStyle>((int)CConfig.Config.Theme.LyricStyle);
            _SelectSlides[_SelectSlideLyricsPosition].SetValues<ELyricsPosition>((int)CConfig.Config.Game.LyricsPosition);
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (_HandleNavigationInput(keyEvent))
            {
                return true;
            }

            base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed) { }
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                        _SaveConfig();
                        CGraphics.FadeTo(EScreen.Main);
                        break;

                    case Keys.S:
                        CParty.SetNormalGameMode();
                        _SaveConfig();
                        CGraphics.FadeTo(EScreen.Song);
                        break;

                    case Keys.Enter:
                        if (_Buttons[_ButtonExit].Selected)
                        {
                            _SaveConfig();
                            CGraphics.FadeTo(EScreen.Main);
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
            if (_HandleNavigationMouse(mouseEvent))
            {
                return true;
            }

            base.HandleMouse(mouseEvent);

            if (mouseEvent.RB)
            {
                _SaveConfig();
                CGraphics.FadeTo(EScreen.Main);
            }

            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                _SaveConfig();
                if (_Buttons[_ButtonExit].Selected)
                {
                    CGraphics.FadeTo(EScreen.Main);
                }
            }

            return true;
        }

        public override bool UpdateGame()
        {
            return true;
        }

        private void _SaveConfig()
        {
            CConfig.Config.Game.LyricsPosition = (ELyricsPosition)_SelectSlides[_SelectSlideLyricsPosition].Selection;
            CConfig.Config.Theme.LyricStyle = (ELyricStyle)_SelectSlides[_SelectSlideLyricStyle].Selection;
            CConfig.SaveConfig();
        }

        protected override void _SaveOptions()
        {
            _SaveConfig();
        }
    }
}