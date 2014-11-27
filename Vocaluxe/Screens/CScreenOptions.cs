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

using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    public class CScreenOptions : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _ButtonOptionsGame = "ButtonOptionsGame";
        private const string _ButtonOptionsSound = "ButtonOptionsSound";
        private const string _ButtonOptionsRecord = "ButtonOptionsRecord";
        private const string _ButtonOptionsVideo = "ButtonOptionsVideo";
        private const string _ButtonOptionsLyrics = "ButtonOptionsLyrics";
        private const string _ButtonOptionsTheme = "ButtonOptionsTheme";

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[] {_ButtonOptionsGame, _ButtonOptionsSound, _ButtonOptionsRecord, _ButtonOptionsVideo, _ButtonOptionsLyrics, _ButtonOptionsTheme};
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
                        CGraphics.FadeTo(EScreen.Main);
                        break;

                    case Keys.S:
                        CParty.SetNormalGameMode();
                        CGraphics.FadeTo(EScreen.Song);
                        break;

                    case Keys.Enter:
                        if (_Buttons[_ButtonOptionsGame].Selected)
                            CGraphics.FadeTo(EScreen.OptionsGame);

                        if (_Buttons[_ButtonOptionsSound].Selected)
                            CGraphics.FadeTo(EScreen.OptionsSound);

                        if (_Buttons[_ButtonOptionsRecord].Selected)
                            CGraphics.FadeTo(EScreen.OptionsRecord);

                        if (_Buttons[_ButtonOptionsVideo].Selected)
                            CGraphics.FadeTo(EScreen.OptionsVideo);

                        if (_Buttons[_ButtonOptionsLyrics].Selected)
                            CGraphics.FadeTo(EScreen.OptionsLyrics);

                        if (_Buttons[_ButtonOptionsTheme].Selected)
                            CGraphics.FadeTo(EScreen.OptionsTheme);

                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                if (_Buttons[_ButtonOptionsGame].Selected)
                    CGraphics.FadeTo(EScreen.OptionsGame);

                if (_Buttons[_ButtonOptionsSound].Selected)
                    CGraphics.FadeTo(EScreen.OptionsSound);

                if (_Buttons[_ButtonOptionsRecord].Selected)
                    CGraphics.FadeTo(EScreen.OptionsRecord);

                if (_Buttons[_ButtonOptionsVideo].Selected)
                    CGraphics.FadeTo(EScreen.OptionsVideo);

                if (_Buttons[_ButtonOptionsLyrics].Selected)
                    CGraphics.FadeTo(EScreen.OptionsLyrics);

                if (_Buttons[_ButtonOptionsTheme].Selected)
                    CGraphics.FadeTo(EScreen.OptionsTheme);
            }

            if (mouseEvent.RB)
                CGraphics.FadeTo(EScreen.Main);
            return true;
        }

        public override bool UpdateGame()
        {
            return true;
        }
    }
}