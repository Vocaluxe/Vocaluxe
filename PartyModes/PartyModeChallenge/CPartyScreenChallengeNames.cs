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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using VocaluxeLib.Menu;

namespace VocaluxeLib.PartyModes.Challenge
{
    // ReSharper disable UnusedMember.Global
    public class CPartyScreenChallengeNames : CMenuParty
        // ReSharper restore UnusedMember.Global
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _ButtonNext = "ButtonNext";
        private const string _ButtonBack = "ButtonBack";
        private const string _ButtonPlayerDestination = "ButtonPlayerDestination";
        private const string _ButtonPlayerChoose = "ButtonPlayerChoose";

        private List<CPlayerChooseButton> _PlayerChooseButtons;
        private List<CButton> _PlayerDestinationButtons;
        //PlayerDestinationButtons-Option
        private const int _PlayerDestinationButtonsNumH = 3;
        private const int _PlayerDestinationButtonsFirstX = 900;
        private const int _PlayerDestinationButtonsFirstY = 105;
        private const int _PlayerDestinationButtonsSpaceH = 15;
        private const int _PlayerDestinationButtonsSpaceW = 25;
        //PlayerChooseButtons-Option
        private List<int> _PlayerChooseButtonsVisibleProfiles;
        private const int _PlayerChooseButtonsNumH = 7;
        private const int _PlayerChooseButtonsNumW = 4;
        private const int _PlayerChooseButtonsFirstX = 52;
        private const int _PlayerChooseButtonsFirstY = 105;
        private const int _PlayerChooseButtonsSpaceH = 15;
        private const int _PlayerChooseButtonsSpaceW = 25;
        private const int _PlayerChooseButtonsOffset = 0;

        private CStatic _ChooseAvatarStatic;
        private bool _SelectingMouseActive;
        private int _OldMouseX;
        private int _OldMouseY;
        private bool _ButtonsAdded;

        private int _NumPlayer = 4;

        private SDataFromScreen _Data;

        private class CPlayerChooseButton
        {
            public CButton Button;
            public int ProfileID;
        }

        public override void Init()
        {
            base.Init();

            _ChooseAvatarStatic = GetNewStatic();
            _ChooseAvatarStatic.Visible = false;

            _PlayerChooseButtonsVisibleProfiles = new List<int>();

            _Data.ScreenNames.ProfileIDs = new List<int>();

            _ThemeButtons = new string[] {_ButtonBack, _ButtonNext, _ButtonPlayerDestination, _ButtonPlayerChoose};

            _Data = new SDataFromScreen();
            SFromScreenNames names = new SFromScreenNames {FadeBack = false, ProfileIDs = new List<int>()};
            _Data.ScreenNames = names;
        }

        public override void DataToScreen(object receivedData)
        {
            try
            {
                SDataToScreenNames config = (SDataToScreenNames)receivedData;
                _Data.ScreenNames.ProfileIDs = config.ProfileIDs ?? new List<int>();

                _NumPlayer = config.NumPlayer;

                while (_Data.ScreenNames.ProfileIDs.Count > _NumPlayer)
                    _Data.ScreenNames.ProfileIDs.RemoveAt(_Data.ScreenNames.ProfileIDs.Count - 1);
            }
            catch (Exception e)
            {
                CBase.Log.LogError("Error in party mode screen challenge names. Can't cast received data from game mode " + ThemeName + ". " + e.Message);
            }
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
                        _Back();
                        break;

                    case Keys.Enter:
                        if (_Buttons[_ButtonBack].Selected)
                            _Back();

                        if (_Buttons[_ButtonNext].Selected)
                            _Next();

                        if (!_OnAdd())
                            _OnRemove();
                        break;

                    case Keys.Delete:
                        _OnRemove();
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            //Check if LeftButton is hold and Select-Mode active
            if (mouseEvent.LBH && _SelectingMouseActive)
            {
                //Update coords for Drag/Drop-Texture
                _ChooseAvatarStatic.Rect.X += mouseEvent.X - _OldMouseX;
                _ChooseAvatarStatic.Rect.Y += mouseEvent.Y - _OldMouseY;
                _OldMouseX = mouseEvent.X;
                _OldMouseY = mouseEvent.Y;
            }
                // LeftButton isn't hold anymore, but Selec-Mode is still active -> "Drop" of Avatar
            else if (_SelectingMouseActive)
            {
                //Reset variables
                _SelectingMouseActive = false;
                _ChooseAvatarStatic.Visible = false;
            }

            if (mouseEvent.LB && _IsMouseOver(mouseEvent))
            {
                if (_Buttons[_ButtonBack].Selected)
                    _Back();

                if (_Buttons[_ButtonNext].Selected)
                    _Next();

                if (!_OnAdd())
                    _OnRemove();
            }

            if (mouseEvent.RB)
                _Back();

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            if (!_ButtonsAdded)
            {
                _AddButtonPlayerDestination();
                _AddButtonPlayerChoose();
                _ButtonsAdded = true;
            }

            _UpdateButtonPlayerDestination();
            _UpdateButtonPlayerChoose();

            _UpdateButtonNext();
        }

        public override bool UpdateGame()
        {
            return true;
        }

        public override bool Draw()
        {
            base.Draw();
            if (_ChooseAvatarStatic.Visible)
                _ChooseAvatarStatic.Draw();
            return true;
        }

        private void _AddButtonPlayerDestination()
        {
            _Buttons[_ButtonPlayerDestination].Visible = false;
            _PlayerDestinationButtons = new List<CButton>();
            int row = 0;
            int column = 0;
            for (int i = 1; i <= _PartyMode.GetMaxPlayer(); i++)
            {
                CButton b = GetNewButton(_Buttons[_ButtonPlayerDestination]);
                b.Rect.X = _PlayerDestinationButtonsFirstX + column * (b.Rect.W + _PlayerDestinationButtonsSpaceH);
                b.Rect.Y = _PlayerDestinationButtonsFirstY + row * (b.Rect.H + _PlayerDestinationButtonsSpaceW);
                _PlayerDestinationButtons.Add(b);
                column++;
                if (column >= _PlayerDestinationButtonsNumH)
                {
                    row++;
                    column = 0;
                }
                b.Visible = true;
                b.Enabled = false;
                _AddButton(b);
            }
        }

        private void _AddButtonPlayerChoose()
        {
            _Buttons[_ButtonPlayerChoose].Visible = false;
            _PlayerChooseButtons = new List<CPlayerChooseButton>();
            int row = 0;
            int column = 0;
            for (int i = 1; i <= _PlayerChooseButtonsNumH * _PlayerChooseButtonsNumW; i++)
            {
                CButton b = GetNewButton(_Buttons[_ButtonPlayerChoose]);
                b.Rect.X = _PlayerChooseButtonsFirstX + column * (b.Rect.W + _PlayerChooseButtonsSpaceH);
                b.Rect.Y = _PlayerChooseButtonsFirstY + row * (b.Rect.H + _PlayerChooseButtonsSpaceW);
                CPlayerChooseButton pcb = new CPlayerChooseButton {Button = b, ProfileID = -1};
                _PlayerChooseButtons.Add(pcb);
                column++;
                if (column >= _PlayerChooseButtonsNumH)
                {
                    row++;
                    column = 0;
                }
                b.Visible = true;
                b.Enabled = false;
                _AddButton(b);
            }
        }

        private void _UpdateButtonPlayerChoose()
        {
            _UpdateVisibleProfiles();
            if ((_PlayerChooseButtonsNumW * _PlayerChooseButtonsNumH) * (_PlayerChooseButtonsOffset + 1) - _PlayerChooseButtonsVisibleProfiles.Count >=
                (_PlayerChooseButtonsNumW * _PlayerChooseButtonsNumH) * _PlayerChooseButtonsOffset)
                _UpdateButtonPlayerChoose(_PlayerChooseButtonsOffset - 1);
            else
                _UpdateButtonPlayerChoose(_PlayerChooseButtonsOffset);
        }

        private void _UpdateButtonPlayerChoose(int offset)
        {
            const int numButtonPlayerChoose = _PlayerChooseButtonsNumW * _PlayerChooseButtonsNumH;
            if (offset < 0)
                offset = 0;

            if (numButtonPlayerChoose * (offset + 1) - _PlayerChooseButtonsVisibleProfiles.Count <= numButtonPlayerChoose)
            {
                for (int i = 0; i < numButtonPlayerChoose; i++)
                {
                    if ((i + offset * numButtonPlayerChoose) < _PlayerChooseButtonsVisibleProfiles.Count)
                    {
                        int id = _PlayerChooseButtonsVisibleProfiles[i + offset * numButtonPlayerChoose];
                        _PlayerChooseButtons[i].ProfileID = id;
                        _PlayerChooseButtons[i].Button.Text.Text = CBase.Profiles.GetPlayerName(id);
                        _PlayerChooseButtons[i].Button.Texture = CBase.Profiles.GetAvatar(id);
                        _PlayerChooseButtons[i].Button.SelTexture = CBase.Profiles.GetAvatar(id);
                        _PlayerChooseButtons[i].Button.Color = new SColorF(1, 1, 1, 0.6f);
                        _PlayerChooseButtons[i].Button.SelColor = new SColorF(1, 1, 1, 1);
                        _PlayerChooseButtons[i].Button.Enabled = true;
                    }
                    else
                    {
                        _PlayerChooseButtons[i].ProfileID = -1;
                        _PlayerChooseButtons[i].Button.Text.Text = String.Empty;
                        _PlayerChooseButtons[i].Button.Texture = _Buttons[_ButtonPlayerChoose].Texture;
                        _PlayerChooseButtons[i].Button.SelTexture = _Buttons[_ButtonPlayerChoose].SelTexture;
                        _PlayerChooseButtons[i].Button.Color = _Buttons[_ButtonPlayerChoose].Color;
                        _PlayerChooseButtons[i].Button.SelColor = _Buttons[_ButtonPlayerChoose].SelColor;
                        _PlayerChooseButtons[i].Button.Enabled = false;
                    }
                }
            }
        }

        private void _UpdateVisibleProfiles()
        {
            _PlayerChooseButtonsVisibleProfiles.Clear();
            Profile.CProfile[] profiles = CBase.Profiles.GetProfiles();
            for (int i = 0; i < profiles.Length; i++)
            {
                bool visible = false;
                //Show profile only if active
                if (profiles[i].Active == EOffOn.TR_CONFIG_ON)
                    visible = _Data.ScreenNames.ProfileIDs.All(profileID => profileID != profiles[i].ID);
                if (visible)
                    _PlayerChooseButtonsVisibleProfiles.Add(profiles[i].ID);
            }
        }

        private void _UpdateButtonPlayerDestination()
        {
            for (int i = 0; i < _PlayerDestinationButtons.Count; i++)
            {
                if (_NumPlayer > i)
                {
                    _PlayerDestinationButtons[i].Visible = true;
                    _PlayerDestinationButtons[i].Enabled = true;
                }
                else
                {
                    _PlayerDestinationButtons[i].Visible = false;
                    _PlayerDestinationButtons[i].Enabled = false;
                }
            }
            for (int i = 0; i < _NumPlayer; i++)
            {
                if (i < _Data.ScreenNames.ProfileIDs.Count)
                {
                    if (_Data.ScreenNames.ProfileIDs[i] != -1)
                    {
                        int id = _Data.ScreenNames.ProfileIDs[i];
                        _PlayerDestinationButtons[i].Color = new SColorF(1, 1, 1, 0.6f);
                        _PlayerDestinationButtons[i].SelColor = new SColorF(1, 1, 1, 1);
                        _PlayerDestinationButtons[i].Texture = CBase.Profiles.GetAvatar(id);
                        _PlayerDestinationButtons[i].SelTexture = CBase.Profiles.GetAvatar(id);
                        _PlayerDestinationButtons[i].Text.Text = CBase.Profiles.GetPlayerName(id);
                        _PlayerDestinationButtons[i].Enabled = true;
                    }
                }
                else
                {
                    _PlayerDestinationButtons[i].Color = _Buttons[_ButtonPlayerDestination].Color;
                    _PlayerDestinationButtons[i].SelColor = _Buttons[_ButtonPlayerDestination].SelColor;
                    _PlayerDestinationButtons[i].Texture = _Buttons[_ButtonPlayerDestination].Texture;
                    _PlayerDestinationButtons[i].SelTexture = _Buttons[_ButtonPlayerDestination].SelTexture;
                    _PlayerDestinationButtons[i].Text.Text = String.Empty;
                    _PlayerDestinationButtons[i].Enabled = false;
                }
            }
        }

        private void _UpdateButtonNext()
        {
            if (_Data.ScreenNames.ProfileIDs.Count == _NumPlayer)
            {
                _Buttons[_ButtonNext].Visible = true;
                _SetInteractionToButton(_Buttons[_ButtonNext]);
            }
            else
                _Buttons[_ButtonNext].Visible = false;
        }

        private bool _OnAdd()
        {
            foreach (CPlayerChooseButton button in _PlayerChooseButtons)
            {
                int id = button.ProfileID;
                if (!button.Button.Selected || id == -1 || _Data.ScreenNames.ProfileIDs.Count >= _NumPlayer)
                    continue;
                _Data.ScreenNames.ProfileIDs.Add(id);
                int added = _Data.ScreenNames.ProfileIDs.Count - 1;
                _UpdateButtonNext();
                //Update texture and name
                _PlayerDestinationButtons[added].Color = new SColorF(1, 1, 1, 0.6f);
                _PlayerDestinationButtons[added].SelColor = new SColorF(1, 1, 1, 1);
                _PlayerDestinationButtons[added].Texture = CBase.Profiles.GetAvatar(id);
                _PlayerDestinationButtons[added].SelTexture = CBase.Profiles.GetAvatar(id);
                _PlayerDestinationButtons[added].Text.Text = CBase.Profiles.GetPlayerName(id);
                _PlayerDestinationButtons[added].Enabled = true;
                //Update Tiles-List
                _UpdateButtonPlayerChoose();
                _CheckInteraction();
                return true;
            }
            return false;
        }

        private void _OnRemove()
        {
            for (int i = 0; i < _PlayerDestinationButtons.Count; i++)
            {
                if (!_PlayerDestinationButtons[i].Selected)
                    continue;
                if ((i + 1) > _Data.ScreenNames.ProfileIDs.Count)
                    continue;
                _Data.ScreenNames.ProfileIDs.RemoveAt(i);
                _UpdateButtonNext();
                _UpdateButtonPlayerDestination();
                _UpdateButtonPlayerChoose();
                _CheckInteraction();
                return;
            }
        }

        private void _Back()
        {
            _Data.ScreenNames.FadeBack = true;
            _PartyMode.DataFromScreen(ThemeName, _Data);
        }

        private void _Next()
        {
            _Data.ScreenNames.FadeBack = false;
            _PartyMode.DataFromScreen(ThemeName, _Data);
        }
    }
}