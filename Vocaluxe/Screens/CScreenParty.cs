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

using System.Collections.Generic;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    class CScreenParty : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _TextDescription = "TextDescription";
        private const string _TextTargetAudience = "TextTargetAudience";
        private const string _TextNumTeams = "TextNumTeams";
        private const string _TextNumPlayers = "TextNumPlayers";
        private const string _TextAuthor = "TextAuthor";
        private const string _TextVersion = "TextVersion";
        private const string _TextError = "TextError";
        private const string _ButtonStart = "ButtonStart";
        private const string _ButtonExit = "ButtonExit";
        private const string _SelectSlideModes = "SelectSlideModes";

        private List<SPartyModeInfos> _PartyModeInfos;

        public override void Init()
        {
            base.Init();

            _ThemeTexts = new string[] {_TextDescription, _TextTargetAudience, _TextNumTeams, _TextNumPlayers, _TextAuthor, _TextVersion, _TextError};
            _ThemeButtons = new string[] {_ButtonStart, _ButtonExit};
            _ThemeSelectSlides = new string[] {_SelectSlideModes};
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed) {}
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Back:
                    case Keys.Escape:
                        CGraphics.FadeTo(EScreens.ScreenMain);
                        break;

                    case Keys.Enter:
                        if (_Buttons[_ButtonStart].Selected)
                            _StartPartyMode();

                        if (_Buttons[_ButtonExit].Selected)
                            CGraphics.FadeTo(EScreens.ScreenMain);
                        break;

                    case Keys.Left:
                    case Keys.Right:
                        if (_SelectSlides[_SelectSlideModes].Selected)
                            _UpdateSelection();
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
                if (_Buttons[_ButtonStart].Selected)
                    _StartPartyMode();

                if (_Buttons[_ButtonExit].Selected)
                    CGraphics.FadeTo(EScreens.ScreenMain);

                if (_SelectSlides[_SelectSlideModes].Selected)
                    _UpdateSelection();
            }

            if (mouseEvent.RB)
                CGraphics.FadeTo(EScreens.ScreenMain);

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            _PartyModeInfos = CParty.GetPartyModeInfos();

            _SelectSlides[_SelectSlideModes].Clear();
            foreach (SPartyModeInfos info in _PartyModeInfos)
                _SelectSlides[_SelectSlideModes].AddValue(info.Name, info.PartyModeID);
            _SelectSlides[_SelectSlideModes].Selection = 0;
            _UpdateSelection();

            _SetInteractionToSelectSlide(_SelectSlides[_SelectSlideModes]);
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

        private void _UpdateSelection()
        {
            if (_PartyModeInfos.Count == 0)
                return;

            int index = _SelectSlides[_SelectSlideModes].Selection;
            if (index >= _PartyModeInfos.Count)
                return;

            //Description    
            _Texts[_TextDescription].Text = _PartyModeInfos[index].Description;
            _Texts[_TextDescription].TranslationID = _PartyModeInfos[index].PartyModeID;

            //TargetAudience
            _Texts[_TextTargetAudience].TranslationID = _PartyModeInfos[index].PartyModeID;
            _Texts[_TextTargetAudience].Text = _PartyModeInfos[index].TargetAudience;

            //NumTeams
            if (_PartyModeInfos[index].MaxTeams == 0)
                _Texts[_TextNumTeams].Text = "TR_SCREENPARTY_NOTEAMS";
            else if (_PartyModeInfos[index].MaxTeams == _PartyModeInfos[index].MinTeams)
                _Texts[_TextNumTeams].Text = _PartyModeInfos[index].MaxTeams.ToString();
            else if (_PartyModeInfos[index].MaxTeams > _PartyModeInfos[index].MinTeams)
                _Texts[_TextNumTeams].Text = _PartyModeInfos[index].MinTeams + " - " + _PartyModeInfos[index].MaxTeams;

            //NumPlayers
            if (_PartyModeInfos[index].MaxPlayers == _PartyModeInfos[index].MinPlayers)
                _Texts[_TextNumPlayers].Text = _PartyModeInfos[index].MaxTeams.ToString();
            else if (_PartyModeInfos[index].MaxPlayers > _PartyModeInfos[index].MinPlayers)
                _Texts[_TextNumPlayers].Text = _PartyModeInfos[index].MinPlayers + " - " + _PartyModeInfos[index].MaxPlayers;

            //Author
            _Texts[_TextAuthor].Text = _PartyModeInfos[index].Author;
            _Texts[_TextAuthor].TranslationID = _PartyModeInfos[index].PartyModeID;

            //Version
            _Texts[_TextVersion].Text = _PartyModeInfos[index].VersionMajor + "." + _PartyModeInfos[index].VersionMinor;
            _Texts[_TextVersion].TranslationID = _PartyModeInfos[index].PartyModeID;

            if (!_PartyModeInfos[index].Playable)
            {
                _Buttons[_ButtonStart].Visible = false;
                _Texts[_TextError].Visible = true;
            }
            else
            {
                _Buttons[_ButtonStart].Visible = true;
                _Texts[_TextError].Visible = false;
            }
        }

        private void _StartPartyMode()
        {
            if (_PartyModeInfos.Count == 0)
                return;

            int index = _SelectSlides[_SelectSlideModes].Selection;
            if (index >= _PartyModeInfos.Count)
                return;

            if (CConfig.GetMaxNumMics() == 0)
                return; //TODO: Add message!

            CParty.SetPartyMode(_PartyModeInfos[index].PartyModeID);
            CGraphics.FadeTo(EScreens.ScreenPartyDummy);
        }
    }
}