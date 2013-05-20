﻿#region license
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
using VocaluxeLib;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    class CScreenMain : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _ButtonSing = "ButtonSing";
        private const string _ButtonParty = "ButtonParty";
        private const string _ButtonOptions = "ButtonOptions";
        private const string _ButtonProfiles = "ButtonProfiles";
        private const string _ButtonExit = "ButtonExit";

        private CText _ReleaseText;

        //CParticleEffect Snowflakes;
        public override void Init()
        {
            base.Init();

            _ThemeStatics = new string[] {"StaticMenuBar"};
            _ThemeButtons = new string[] {_ButtonSing, _ButtonParty, _ButtonOptions, _ButtonProfiles, _ButtonExit};
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _ReleaseText = GetNewText(10, 690, -1, 15, -1, EAlignment.Left, EStyle.Normal, "Normal", new SColorF(1f, 1f, 1f, 1f), CSettings.GetFullVersionText());
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
                        CGraphics.FadeTo(EScreens.ScreenOptions);
                        break;

                    case Keys.S:
                        CGraphics.FadeTo(EScreens.ScreenSong);
                        break;

                    case Keys.C:
                        CGraphics.FadeTo(EScreens.ScreenCredits);
                        break;

                    case Keys.T:
                        CGraphics.FadeTo(EScreens.ScreenTest);
                        break;

                    case Keys.Enter:
                        if (_Buttons[_ButtonSing].Selected)
                        {
                            CParty.SetNormalGameMode();
                            CGraphics.FadeTo(EScreens.ScreenSong);
                        }

                        if (_Buttons[_ButtonParty].Selected)
                            CGraphics.FadeTo(EScreens.ScreenParty);

                        if (_Buttons[_ButtonOptions].Selected)
                            CGraphics.FadeTo(EScreens.ScreenOptions);

                        if (_Buttons[_ButtonProfiles].Selected)
                            CGraphics.FadeTo(EScreens.ScreenProfiles);

                        if (_Buttons[_ButtonExit].Selected)
                            return false;

                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.LB && _IsMouseOver(mouseEvent))
            {
                if (_Buttons[_ButtonSing].Selected)
                {
                    CParty.SetNormalGameMode();
                    CGraphics.FadeTo(EScreens.ScreenSong);
                }

                if (_Buttons[_ButtonParty].Selected)
                    CGraphics.FadeTo(EScreens.ScreenParty);

                if (_Buttons[_ButtonOptions].Selected)
                    CGraphics.FadeTo(EScreens.ScreenOptions);

                if (_Buttons[_ButtonProfiles].Selected)
                    CGraphics.FadeTo(EScreens.ScreenProfiles);

                if (_Buttons[_ButtonExit].Selected)
                    return false;
            }

            return true;
        }

        // ReSharper disable RedundantOverridenMember
        public override void OnShow()
        {
            base.OnShow();

            //if (Snowflakes != null)
            //    Snowflakes.Resume();
        }

        // ReSharper restore RedundantOverridenMember

        public override bool UpdateGame()
        {
            return true;
        }

        public override bool Draw()
        {
            _DrawBG();

            //if (Snowflakes == null)
            //    Snowflakes = new CParticleEffect(300, new SColorF(1, 1, 1, 1), new SRectF(0, 0, CSettings.iRenderW, 0, 0.5f), "Snowflake", 25, EParticeType.Snow);

            //Snowflakes.Update();
            //Snowflakes.Draw();
            _DrawFG();

            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            if (CSettings.VersionRevision != ERevision.Release)
                // ReSharper restore ConditionIsAlwaysTrueOrFalse
                _ReleaseText.Draw();

            return true;
        }

        // ReSharper disable RedundantOverridenMember
        public override void OnClose()
        {
            base.OnClose();

            //if (Snowflakes != null)
            //    Snowflakes.Pause();
        }

        // ReSharper restore RedundantOverridenMember
    }
}