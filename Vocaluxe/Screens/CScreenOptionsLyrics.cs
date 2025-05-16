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
    public class CScreenOptionsLyrics : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 4; }
        }

        private const string _SelectSlideLyricStyle = "SelectSlideLyricStyle";
        private const string _SelectSlideLyricsPosition = "SelectSlideLyricsPosition";
        private const string _SelectSlideTextureQuality = "SelectSlideTextureQuality";
        private const string _SelectSlideCoverSize = "SelectSlideCoverSize";
        private const string _SelectSlideFullScreen = "SelectSlideFullScreen";
        private const string _SelectSlideStretch = "SelectSlideStretch";
        private const string _TextWarningRestart = "TextWarningRestart";
        private const string _StaticWarningRestart = "StaticWarningRestart";
        private const string _ButtonOptionsBack = "ButtonOptionsBack";  
        private const string _ButtonOptionsGame = "ButtonOptionsGame";
        private const string _ButtonOptionsSound = "ButtonOptionsSound";
        private const string _ButtonOptionsRecord = "ButtonOptionsRecord";
        private const string _ButtonOptionsVideo = "ButtonOptionsVideo";
        private const string _ButtonOptionsLyrics = "ButtonOptionsLyrics";
        private const string _ButtonOptionsTheme = "ButtonOptionsTheme";
        private const string _ButtonOptionsCredits = "ButtonOptionsCredits";
        private static readonly string[] CoverSizes = { "32", "64", "128", "256", "512", "1024" };

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[] {_ButtonOptionsBack, _ButtonOptionsGame, _ButtonOptionsSound, _ButtonOptionsRecord, _ButtonOptionsVideo, _ButtonOptionsLyrics, _ButtonOptionsTheme};
            _ThemeSelectSlides = new string[] {_SelectSlideLyricStyle, _SelectSlideLyricsPosition, _SelectSlideTextureQuality, _SelectSlideCoverSize, _SelectSlideFullScreen, _SelectSlideStretch};
            _ThemeTexts = new string[] {_TextWarningRestart};
            _ThemeStatics = new string[] {_StaticWarningRestart};
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);
            _SelectSlides[_SelectSlideLyricStyle].SetValues<ELyricStyle>((int)CConfig.Config.Theme.LyricStyle);
            _SelectSlides[_SelectSlideLyricsPosition].SetValues<ELyricsPosition>((int)CConfig.Config.Game.LyricsPosition);

            _SelectSlides[_SelectSlideTextureQuality].SetValues<ETextureQuality>((int)CConfig.Config.Graphics.TextureQuality);
            
            _SelectSlides[_SelectSlideCoverSize].AddValues(CoverSizes);
            int currentCoverSize = CConfig.Config.Graphics.CoverSize;
            int index = Array.IndexOf(CoverSizes, currentCoverSize.ToString());
            _SelectSlides[_SelectSlideCoverSize].Selection = index;
            
            _SelectSlides[_SelectSlideFullScreen].SetValues<EOffOn>((int)CConfig.Config.Graphics.FullScreen);
            _SelectSlides[_SelectSlideFullScreen].Selection = (int)CConfig.Config.Graphics.FullScreen;

            _SelectSlides[_SelectSlideStretch].SetValues<EOffOn>((int)CConfig.Config.Graphics.Stretch);
            _SelectSlides[_SelectSlideStretch].Selection = (int)CConfig.Config.Graphics.Stretch;

            _Texts[_TextWarningRestart].Visible = false;
            _Statics[_StaticWarningRestart].Visible = false;
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
                        CGraphics.FadeTo(EScreen.Main);
                        break;

                    case Keys.S:
                        CParty.SetNormalGameMode();
                        _SaveConfig();
                        CGraphics.FadeTo(EScreen.Song);
                        break;

                    case Keys.Enter:
                        if (_Buttons[_ButtonOptionsBack].Selected)
                        {
                            _SaveConfig();
                            CGraphics.FadeTo(EScreen.Main);
                        }
                        else if (_Buttons[_ButtonOptionsGame].Selected)
                        {
                            _SaveConfig();
                            CGraphics.FadeTo(EScreen.OptionsGame);
                        }
                        else if (_Buttons[_ButtonOptionsSound].Selected)
                        {
                            _SaveConfig();
                            CGraphics.FadeTo(EScreen.OptionsSound);
                        }
                        else if (_Buttons[_ButtonOptionsRecord].Selected)
                        {
                            _SaveConfig();
                            CGraphics.FadeTo(EScreen.OptionsRecord);
                        }
                        else if (_Buttons[_ButtonOptionsVideo].Selected)
                        {
                            _SaveConfig();
                            CGraphics.FadeTo(EScreen.OptionsVideo);
                        }
                        else if (_Buttons[_ButtonOptionsLyrics].Selected)
                        {
                            _SaveConfig();
                        }
                        else if (_Buttons[_ButtonOptionsTheme].Selected)
                        {
                            _SaveConfig();
                            CGraphics.FadeTo(EScreen.OptionsTheme);
                        }
                        else if (_Buttons[_ButtonOptionsCredits].Selected)
                        {
                            _SaveConfig();
                            CGraphics.FadeTo(EScreen.Credits);
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
                CGraphics.FadeTo(EScreen.Main);
            }
            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                if (_Buttons[_ButtonOptionsBack].Selected)
                        {
                            _SaveConfig();
                            CGraphics.FadeTo(EScreen.Main);
                        }
                        else if (_Buttons[_ButtonOptionsGame].Selected)
                        {
                            _SaveConfig();
                            CGraphics.FadeTo(EScreen.OptionsGame);
                        }
                        else if (_Buttons[_ButtonOptionsSound].Selected)
                        {
                            _SaveConfig();
                            CGraphics.FadeTo(EScreen.OptionsSound);
                        }
                        else if (_Buttons[_ButtonOptionsRecord].Selected)
                        {
                            _SaveConfig();
                            CGraphics.FadeTo(EScreen.OptionsRecord);
                        }
                        else if (_Buttons[_ButtonOptionsVideo].Selected)
                        {
                            _SaveConfig();
                            CGraphics.FadeTo(EScreen.OptionsVideo);
                        }
                        else if (_Buttons[_ButtonOptionsLyrics].Selected)
                        {
                            _SaveConfig();
                        }
                        else if (_Buttons[_ButtonOptionsTheme].Selected)
                        {
                            _SaveConfig();
                            CGraphics.FadeTo(EScreen.OptionsTheme);
                        }
                        else if (_Buttons[_ButtonOptionsCredits].Selected)
                        {
                            _SaveConfig();
                            CGraphics.FadeTo(EScreen.Credits);
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

            // Detect Texture quality change
            ETextureQuality _currentTextureQuality = CConfig.Config.Graphics.TextureQuality;
            ETextureQuality _newTextureQuality = (ETextureQuality)_SelectSlides[_SelectSlideTextureQuality].Selection;
            if (_currentTextureQuality != _newTextureQuality)
            {
                _Texts[_TextWarningRestart].Visible = true;
                _Statics[_StaticWarningRestart].Visible = true;    
                CConfig.Config.Graphics.TextureQuality = _newTextureQuality;
            }
            else
            {
                CConfig.Config.Graphics.TextureQuality = _newTextureQuality;
            }           

            // Detect Cover size change
            string selectedValue = CoverSizes[_SelectSlides[_SelectSlideCoverSize].Selection];
            int currentCoverSize = CConfig.Config.Graphics.CoverSize;
            int newCoverSize = int.Parse(selectedValue);
            if (currentCoverSize != newCoverSize)
            {
                string flagPath = Path.Combine(CSettings.DataFolder, "DeleteCoverDB.flag");
                File.Create(flagPath).Dispose();
                _Texts[_TextWarningRestart].Visible = true;
               _Statics[_StaticWarningRestart].Visible = true;    
                CConfig.Config.Graphics.CoverSize = newCoverSize;
            }
            else
            {
                CConfig.Config.Graphics.CoverSize = newCoverSize;
            }        
                        
            CConfig.Config.Graphics.FullScreen = (EOffOn)_SelectSlides[_SelectSlideFullScreen].Selection;
            CConfig.Config.Graphics.Stretch = (EOffOn)_SelectSlides[_SelectSlideStretch].Selection;
            
            CConfig.SaveConfig();
        }
    }
}
