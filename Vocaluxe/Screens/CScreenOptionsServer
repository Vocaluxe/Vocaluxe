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

using System;
using System.Windows.Forms;
using System.IO;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    public class CScreenOptionsServer : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _SelectSlideLyricStyle = "SelectSlideServerActive";
        private const string _SelectSlideLyricsPosition = "SelectSlideServerEncryption";

        private const string _ButtonServer = "ButtonServer";
        private const string _ButtonExit = "ButtonExit";

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[] {_ButtonExit, _ButtonServer};
            _ThemeSelectSlides = new string[] {_SelectSlideServerActive, _SelectSlideServerEncryption};
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);
            _SelectSlides[_SelectSlideServerActive].SetValues<EOffOn>((int)CConfig.Config.Server.ServerActive);
            _SelectSlides[_SelectSlideServerActive].SetValues<EOffOn>((int)CConfig.Config.Server.ServerEncryption);
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
                        CGraphics.FadeTo(EScreen.Options);
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
                            CGraphics.FadeTo(EScreen.Options);
                            _LeaveScreen();
                        }
                        else if (_Buttons[_ButtonServer].Selected)
                        {
                            CGraphics.ShowPopup(EPopupScreens.PopupServerQR);
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
                CGraphics.FadeTo(EScreen.Options);
            }
            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                if (_Buttons[_ButtonExit].Selected)
                {
                    CGraphics.FadeTo(EScreen.Options);
                    _SaveConfig();
                    _LeaveScreen();
                }
                else if (_Buttons[_ButtonServer].Selected)
                {
                    CGraphics.ShowPopup(EPopupScreens.PopupServerQR);
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
            CConfig.Config.Server.ServerActive = (EOffOn)_SelectSlides[_SelectSlideServerActive].Selection;
            CConfig.Config.Server.ServerEncryption = (EOffOn)_SelectSlides[_SelectSlideServerEncryption].Selection;
            CConfig.SaveConfig();
        }
    }
}
