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
using System.IO;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    public class CScreenOptionsLyrics : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 3; }
        }

        private const string _SelectSlideLyricStyle = "SelectSlideLyricStyle";
        private const string _SelectSlideLyricsPosition = "SelectSlideLyricsPosition";
        private const string _SelectSlideTextureQuality = "SelectSlideTextureQuality";
        private const string _SelectSlideCoverSize = "SelectSlideCoverSize";
        private const string _SelectSlideFullScreen = "SelectSlideFullScreen";
        private const string _SelectSlideStretch = "SelectSlideStretch";
        private const string _TextWarningRestart = "TextWarningRestart";

        private const string _ButtonExit = "ButtonExit";

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[] {_ButtonExit};
            _ThemeSelectSlides = new string[] {_SelectSlideLyricStyle, _SelectSlideLyricsPosition, _SelectSlideTextureQuality, _SelectSlideCoverSize, _SelectSlideFullScreen, _SelectSlideStretch};
            _ThemeTexts = new string[] {_TextWarningRestart};
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);
            _SelectSlides[_SelectSlideLyricStyle].SetValues<ELyricStyle>((int)CConfig.Config.Theme.LyricStyle);
            _SelectSlides[_SelectSlideLyricsPosition].SetValues<ELyricsPosition>((int)CConfig.Config.Game.LyricsPosition);

            _SelectSlides[_SelectSlideTextureQuality].SetValues<ETextureQuality>((int)CConfig.Config.Graphics.TextureQuality);
            
            _SelectSlides[_SelectSlideCoverSize].AddValues(new string[] { "32", "64", "128", "256", "512", "1024" });
            int currentCoverSize = CConfig.Config.Graphics.CoverSize;
            string[] options = { "32", "64", "128", "256", "512", "1024" };
            int index = Array.IndexOf(options, currentCoverSize.ToString());
            _SelectSlides[_SelectSlideCoverSize].Selection = index;
            
            _SelectSlides[_SelectSlideFullScreen].SetValues<EOffOn>((int)CConfig.Config.Graphics.FullScreen);
            _SelectSlides[_SelectSlideFullScreen].Selection = (int)CConfig.Config.Graphics.FullScreen;

            _SelectSlides[_SelectSlideStretch].SetValues<EOffOn>((int)CConfig.Config.Graphics.Stretch);
            _SelectSlides[_SelectSlideStretch].Selection = (int)CConfig.Config.Graphics.Stretch;

            _Texts[_TextWarningRestart].Visible = true;
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
                _SaveConfig();
                if (_Buttons[_ButtonExit].Selected)
                    CGraphics.FadeTo(EScreen.Options);
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

            CConfig.Config.Graphics.TextureQuality = (ETextureQuality)_SelectSlides[_SelectSlideTextureQuality].Selection;
            
            string[] options = { "32", "64", "128", "256", "512", "1024" };
            string selectedValue = options[_SelectSlides[_SelectSlideCoverSize].Selection];
            
            // Detect cover size change
            int currentCoverSize = CConfig.Config.Graphics.CoverSize;
            int newCoverSize = int.Parse(selectedValue);
            if (currentCoverSize != newCoverSize)
            {
                string flagPath = Path.Combine(CSettings.DataFolder, "DeleteCoverDB.flag");
                File.Create(flagPath).Dispose();
    
                CConfig.Config.Graphics.CoverSize = newCoverSize;
            }
            else
            {
                CConfig.Config.Graphics.CoverSize = newCoverSize;
            }        
                        
            CConfig.Config.Graphics.FullScreen = (EOffOn)_SelectSlides[_SelectSlideFullScreen].Selection;
            CConfig.Config.Graphics.Stretch = (EOffOn)_SelectSlides[_SelectSlideStretch].Selection;
            CConfig.Config.Graphics.VSync = (EOffOn)_SelectSlides[_SelectSlideVSync].Selection;
            
            CConfig.SaveConfig();
        }
    }
}
