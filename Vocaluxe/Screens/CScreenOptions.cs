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

using System.Linq;
using System.Collections.Generic;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;
using Vocaluxe.Lib.Sound;

namespace Vocaluxe.Screens
{
    public class CScreenOptions : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 4; }
        }
           
        private const string _ButtonOptionsBack = "ButtonOptionsBack";  
        private const string _ButtonOptionsGame = "ButtonOptionsGame";
        private const string _ButtonOptionsSound = "ButtonOptionsSound";
        private const string _ButtonOptionsRecord = "ButtonOptionsRecord";
        private const string _ButtonOptionsVideo = "ButtonOptionsVideo";
        private const string _ButtonOptionsLyrics = "ButtonOptionsLyrics";
        private const string _ButtonOptionsTheme = "ButtonOptionsTheme";
        private const string _ButtonOptionsCredits = "ButtonOptionsCredits";
        private const string _ButtonOptionsGraphics = "ButtonOptionsGraphics";
        private const string _ButtonOptionsServer = "ButtonOptionsServer";
        private const string _ButtonSelectSongFolder = "ButtonSongFolder";

        private const string _TextWarningRestart = "TextWarningRestart";
        private const string _StaticWarningRestart = "StaticWarningRestart";

        private int _WarningStream = -1;
        private bool _HasPlayedWarningSound = false;
        
        private static int PlaySound(ESounds sound, int volume)
        {
            int streamId = CSound.PlaySound(sound, false);
            CSound.SetStreamVolume(streamId, volume);

            return streamId;
        }

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[] {_ButtonOptionsBack, _ButtonOptionsGame, _ButtonOptionsSound, _ButtonOptionsRecord, _ButtonOptionsVideo, _ButtonOptionsLyrics, _ButtonOptionsTheme, _ButtonOptionsCredits, _ButtonOptionsGraphics, _ButtonOptionsServer, _ButtonSelectSongFolder};
            _ThemeTexts = new string[] {_TextWarningRestart};
            _ThemeStatics = new string[] {_StaticWarningRestart};
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

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
                        CGraphics.FadeTo(EScreen.Main);
                        _LeaveScreen();
                        break;

                    case Keys.S:
                        CParty.SetNormalGameMode();
                        CGraphics.FadeTo(EScreen.Song);
                        _LeaveScreen();
                        break;

                    case Keys.Enter:
                        if (_Buttons[_ButtonOptionsBack].Selected)
                        {
                            CGraphics.FadeTo(EScreen.Main);
                            _LeaveScreen();
                        }

                        if (_Buttons[_ButtonOptionsGame].Selected)
                        {
                            CGraphics.FadeTo(EScreen.OptionsGame);
                            _LeaveScreen();
                        }

                        if (_Buttons[_ButtonOptionsSound].Selected)
                        {
                            CGraphics.FadeTo(EScreen.OptionsSound);
                            _LeaveScreen();
                        }

                        if (_Buttons[_ButtonOptionsRecord].Selected)
                        {
                            CGraphics.FadeTo(EScreen.OptionsRecord);
                            _LeaveScreen();
                        }

                        if (_Buttons[_ButtonOptionsVideo].Selected)
                        {
                            CGraphics.FadeTo(EScreen.OptionsVideo);
                            _LeaveScreen();
                        }

                        if (_Buttons[_ButtonOptionsLyrics].Selected)
                        {
                            CGraphics.FadeTo(EScreen.OptionsLyrics);
                            _LeaveScreen();
                        }

                        if (_Buttons[_ButtonOptionsTheme].Selected)
                        {
                            CGraphics.FadeTo(EScreen.OptionsTheme);
                            _LeaveScreen();
                        }

                        if (_Buttons[_ButtonOptionsGraphics].Selected)
                        {
                            CGraphics.FadeTo(EScreen.OptionsGraphics);
                            _LeaveScreen();
                        }

                        if (_Buttons[_ButtonOptionsServer].Selected)
                        {
                            CGraphics.FadeTo(EScreen.OptionsServer);
                            _LeaveScreen();
                        }

                        if (_Buttons[_ButtonOptionsCredits].Selected)
                        {
                            CGraphics.FadeTo(EScreen.Credits);
                            _LeaveScreen();
                        }

                        if (_Buttons[_ButtonSelectSongFolder].Selected && CScreenOptions._OpenSongFolderDialog())
                        {
                            _Texts[_TextWarningRestart].Visible = true;
                            _Statics[_StaticWarningRestart].Visible = true;
                        }
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
                if (_Buttons[_ButtonOptionsBack].Selected)
                {
                    CGraphics.FadeTo(EScreen.Main);
                    _LeaveScreen();
                }

                if (_Buttons[_ButtonOptionsGame].Selected)
                {
                    CGraphics.FadeTo(EScreen.OptionsGame);
                    _LeaveScreen();
                }

                if (_Buttons[_ButtonOptionsSound].Selected)
                {
                    CGraphics.FadeTo(EScreen.OptionsSound);
                    _LeaveScreen();
                }

                if (_Buttons[_ButtonOptionsRecord].Selected)
                {
                    CGraphics.FadeTo(EScreen.OptionsRecord);
                    _LeaveScreen();
                }

                if (_Buttons[_ButtonOptionsVideo].Selected)
                {
                    CGraphics.FadeTo(EScreen.OptionsVideo);
                    _LeaveScreen();
                }

                if (_Buttons[_ButtonOptionsLyrics].Selected)
                {
                    CGraphics.FadeTo(EScreen.OptionsLyrics);
                    _LeaveScreen();
                }

                if (_Buttons[_ButtonOptionsTheme].Selected)
                {
                    CGraphics.FadeTo(EScreen.OptionsTheme);
                    _LeaveScreen();
                }

                if (_Buttons[_ButtonOptionsGraphics].Selected)
                {
                    CGraphics.FadeTo(EScreen.OptionsGraphics);
                    _LeaveScreen();
                }

                if (_Buttons[_ButtonOptionsServer].Selected)
                {
                    CGraphics.FadeTo(EScreen.OptionsServer);
                    _LeaveScreen();
                }

                if (_Buttons[_ButtonOptionsCredits].Selected)
                {
                    CGraphics.FadeTo(EScreen.Credits);
                    _LeaveScreen();
                }

                if (_Buttons[_ButtonSelectSongFolder].Selected && CScreenOptions._OpenSongFolderDialog())
                {
                    _Texts[_TextWarningRestart].Visible = true;
                    _Statics[_StaticWarningRestart].Visible = true;
                }
            }

            if (mouseEvent.RB)
            {
                CGraphics.FadeTo(EScreen.Main);
                _LeaveScreen();
            }
            return true;
        }

        public override bool UpdateGame()
        {
            if (_Texts[_TextWarningRestart].Visible && !_HasPlayedWarningSound)
            {
                _WarningStream = CScreenOptions.PlaySound(ESounds.Warning, CConfig.SoundEffectVolume);
                _HasPlayedWarningSound = true;
            }
                    
            return true;
        }

        private static bool _OpenSongFolderDialog()
        {
            // TODO(linux-port): the native folder picker used WinForms' FolderBrowserDialog,
            // which is unavailable on the cross-platform build. A portable folder dialog
            // (e.g. via a GTK/portal/NFD binding) still needs to be wired up. Song folders
            // can be edited in config.xml in the meantime.
            CLog.Error("Selecting a song folder via dialog is not yet supported on this platform; edit config.xml instead.");
            return false;
        }
        
        private void _LeaveScreen()
        {           
            if (_WarningStream != -1)
            {
                 CSound.Close(_WarningStream);
                _WarningStream = -1;
            }
        }
    }
}
