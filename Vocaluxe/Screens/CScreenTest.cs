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

using System;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    class CScreenTest : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        /*
        private int _TestMusic = -1;
*/

        public override void Init()
        {
            base.Init();
            const string test = "Ö ÄÜabcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPGRSTUVWGXZ1234567890";
            SColorF color = new SColorF(1, 0, 0, 1);
            CText text = new CText(10, 50, 1, 32, 0, EAlignment.Left, EStyle.Normal, "Normal", color, "jÄNormal Text" + test, -1, 26, 1);
            _AddText(text);
            text = new CText(10, 90, 1, 32, 0, EAlignment.Left, EStyle.Bold, "Normal", color, "jÄBold Text" + test, -1, 26, 1);
            _AddText(text);
            text = new CText(10, 130, 1, 32, 0, EAlignment.Left, EStyle.Italic, "Normal", color, "jÄItalic Text" + test, -1, 26, 1);
            _AddText(text);
            text = new CText(10, 170, 1, 32, 0, EAlignment.Left, EStyle.Normal, "Outline", color, "jÄNormal Text" + test, -1, 50, 1);
            _AddText(text);
            text = new CText(10, 210, 1, 32, 0, EAlignment.Left, EStyle.Bold, "Outline", color, "jÄBold Text" + test, -1, 100, 1);
            _AddText(text);
            text = new CText(10, 250, 1, 32, 0, EAlignment.Left, EStyle.Italic, "Outline", color, "jÄItalic Text" + test, -1, 150, 1);
            _AddText(text);
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (keyEvent.KeyPressed && !Char.IsControl(keyEvent.Unicode)) {}
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                        CGraphics.FadeTo(EScreens.ScreenMain);
                        break;

                    case Keys.Enter:
                        CGraphics.FadeTo(EScreens.ScreenMain);
                        break;

                    case Keys.F:
                        //FadeAndPause();
                        break;

                    case Keys.S:
                        //PlayFile();
                        break;

                    case Keys.P:
                        //PauseFile();
                        break;
                }
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            if (mouseEvent.LB && _IsMouseOver(mouseEvent)) {}

            if (mouseEvent.LB)
                CGraphics.FadeTo(EScreens.ScreenMain);

            if (mouseEvent.RB)
                CGraphics.FadeTo(EScreens.ScreenMain);
            return true;
        }

        public override bool UpdateGame()
        {
            return true;
        }

        /*
                private void _PlayFile()
                {
                    if (_TestMusic == -1)
                        _TestMusic = CSound.Load(Path.Combine(Environment.CurrentDirectory, "Test.mp3"));

                    CSound.Play(_TestMusic);
                    CSound.Fade(_TestMusic, 100f, 2f);
                }


                private void _PauseFile()
                {
                    CSound.Pause(_TestMusic);
                }

                private void _FadeAndPause()
                {
                    CSound.FadeAndPause(_TestMusic, 0f, 2f);
                }*/
    }
}