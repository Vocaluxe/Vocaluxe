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
using Vocaluxe.Lib.Sound;

namespace Vocaluxe.Screens
{
    public class CScreenMain : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 3; }
        }

        private const string _ButtonSing = "ButtonSing";
        private const string _ButtonParty = "ButtonParty";
        private const string _ButtonOptions = "ButtonOptions";
        private const string _ButtonProfiles = "ButtonProfiles";
        private const string _ButtonExit = "ButtonExit";
        private const string _StaticWarningProfiles = "StaticWarningProfiles";
        private const string _TextWarningProfiles = "TextWarningProfiles";
        private const string _TextRelease = "TextRelease";
        private const string _TextLoading = "TextLoading";
        private const string _TextStatus = "TextStatus";

        private int _WarningStream = -1;
        private bool _HasPlayedWarningSound = false;
        
        private static int PlaySound(ESounds sound, int volume)
        {
            int streamId = CSound.PlaySound(sound, false);
            CSound.SetStreamVolume(streamId, volume);

            return streamId;
        }

        //CParticleEffect Snowflakes;
        public override void Init()
        {
            base.Init();

            _ThemeStatics = new string[] {"StaticMenuBar", _StaticWarningProfiles};
            _ThemeButtons = new string[] {_ButtonSing, _ButtonParty, _ButtonOptions, _ButtonProfiles, _ButtonExit};
            _ThemeTexts = new string[] {_TextRelease, _TextWarningProfiles, _TextLoading, _TextStatus };
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _Texts[_TextRelease].Text = CSettings.GetFullVersionText();
            _Texts[_TextRelease].Visible = true;
            _Statics[_StaticWarningProfiles].Visible = false;
            _Texts[_TextWarningProfiles].Visible = false;
            _Texts[_TextLoading].Text = "";
            _Texts[_TextStatus].Text = "";
            _SelectElement(_Buttons[_ButtonSing]);
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed) {}
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.O:
                        CGraphics.FadeTo(EScreen.Options);
                        _LeaveScreen();
                        break;

                    case Keys.S:
                        if (CProfiles.NumProfiles > 0)
                            CGraphics.FadeTo(EScreen.Song);
                        _LeaveScreen();
                        break;

                    case Keys.C:
                        CGraphics.FadeTo(EScreen.Credits);
                        _LeaveScreen();
                        break;

                    case Keys.T:
                        CGraphics.FadeTo(EScreen.Test);
                        _LeaveScreen();
                        break;

                    case Keys.Enter:
                        if (_Buttons[_ButtonSing].Selected)
                        {
                            CParty.SetNormalGameMode();
                            CGraphics.FadeTo(EScreen.Song);
                            _LeaveScreen();
                        }

                        if (_Buttons[_ButtonParty].Selected)
                        {
                            CGraphics.FadeTo(EScreen.Party);
                            _LeaveScreen();
                        }

                        if (_Buttons[_ButtonOptions].Selected)
                        {
                            CGraphics.FadeTo(EScreen.Options);
                            _LeaveScreen();
                        }

                        if (_Buttons[_ButtonProfiles].Selected)
                        {
                            CGraphics.FadeTo(EScreen.Profiles);
                            _LeaveScreen();
                        }

                        if (_Buttons[_ButtonExit].Selected)
                            return false;

                        break;

                    case Keys.Escape:
                        _SelectElement(_Buttons[_ButtonExit]);
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
                if (_Buttons[_ButtonSing].Selected)
                {
                    CParty.SetNormalGameMode();
                    CGraphics.FadeTo(EScreen.Song);
                    _LeaveScreen();
                }

                if (_Buttons[_ButtonParty].Selected)
                {
                    CGraphics.FadeTo(EScreen.Party);
                    _LeaveScreen();
                }

                if (_Buttons[_ButtonOptions].Selected)
                {
                    CGraphics.FadeTo(EScreen.Options);
                    _LeaveScreen();
                }

                if (_Buttons[_ButtonProfiles].Selected)
                {
                    CGraphics.FadeTo(EScreen.Profiles);
                    _LeaveScreen();
                }

                if (_Buttons[_ButtonExit].Selected)
                    return false;
            }

            return true;
        }

        public override bool UpdateGame()
        {
            bool profileOK = CProfiles.NumProfiles > 0;
            _Statics[_StaticWarningProfiles].Visible = !profileOK;
            _Texts[_TextWarningProfiles].Visible = !profileOK;
            if (CConfig.Config.Theme.SongLoading == ESongLoading.TR_CONFIG_SONGLOADING_DYNAMIC)
            {
                if (CSongs.SongsLoaded && CSongs.CoverLoaded)
                {
                    _Texts[_TextLoading].Text = "";
                    _Texts[_TextStatus].Text = "";
                }
                else
                {
                    _Texts[_TextLoading].Text = CLanguage.Translate("TR_SCREENLOAD_LOADING");
                    _Texts[_TextStatus].Text =
                        CLanguage.Translate("TR_SCREENLOAD_TOTAL") + ": " + CSongs.NumAllSongs + " " +
                        CLanguage.Translate("TR_SCREENLOAD_SONGS") + " (" + CSongs.NumSongsWithCoverLoaded + " " +
                        CLanguage.Translate("TR_SCREENLOAD_LOADED") + ")";
                }
            }
            _Buttons[_ButtonSing].Selectable = profileOK;
            _Buttons[_ButtonParty].Selectable = profileOK;

            if (_Texts[_TextWarningProfiles].Visible && !_HasPlayedWarningSound)
            {
                 _WarningStream = CScreenMain.PlaySound(ESounds.Warning, CConfig.SoundEffectVolume);
                 _HasPlayedWarningSound = true;
            }
            return true;
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
