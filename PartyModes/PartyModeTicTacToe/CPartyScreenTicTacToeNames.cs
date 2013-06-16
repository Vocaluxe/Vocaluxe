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

namespace VocaluxeLib.PartyModes.TicTacToe
{
    // ReSharper disable UnusedMember.Global
    public class CPartyScreenTicTacToeNames : CMenuParty
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
        private const string _ButtonPlayerChooseScrollUp = "ButtonPlayerChooseScrollUp";
        private const string _ButtonPlayerChooseScrollDown = "ButtonPlayerChooseScrollDown";

        private List<CPlayerChooseButton> _PlayerChooseButtons;
        private List<CButton> _PlayerDestinationButtons;
        //PlayerDestinationButtons-Option
        private const int _PlayerDestinationButtonsNumH = 10;
        private const int _PlayerDestinationButtonsFirstX = 58;
        private const int _PlayerDestinationButtonsFirstY = 380;
        private const int _PlayerDestinationButtonsSpaceH = 15;
        private const int _PlayerDestinationButtonsSpaceW = 25;
        //PlayerChooseButtons-Option
        private List<int> _PlayerChooseButtonsVisibleProfiles;
        private const int _PlayerChooseButtonsNumH = 10;
        private const int _PlayerChooseButtonsNumW = 2;
        private const int _PlayerChooseButtonsFirstX = 58;
        private const int _PlayerChooseButtonsFirstY = 105;
        private const int _PlayerChooseButtonsSpaceH = 15;
        private const int _PlayerChooseButtonsSpaceW = 25;
        private int _PlayerChooseButtonsOffset;

        private CStatic _ChooseAvatarStatic;
        private bool _SelectingMouseActive;
        private int _OldMouseX;
        private int _OldMouseY;
        private int _SelectedPlayerNr = -1;
        private bool _ButtonsAdded;

        private int _NumPlayerTeam1 = 2;
        private int _NumPlayerTeam2 = 2;

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

            _Data.ScreenNames.ProfileIDsTeam1 = new List<int>();
            _Data.ScreenNames.ProfileIDsTeam2 = new List<int>();

            _ThemeButtons = new string[] {_ButtonBack, _ButtonNext, _ButtonPlayerDestination, _ButtonPlayerChoose, _ButtonPlayerChooseScrollUp, _ButtonPlayerChooseScrollDown};

            _Data = new SDataFromScreen();
            SFromScreenNames names = new SFromScreenNames {FadeToConfig = false, ProfileIDsTeam1 = new List<int>(), ProfileIDsTeam2 = new List<int>()};
            _Data.ScreenNames = names;
        }

        public override void DataToScreen(object receivedData)
        {
            try
            {
                SDataToScreenNames config = (SDataToScreenNames)receivedData;
                _Data.ScreenNames.ProfileIDsTeam1 = config.ProfileIDsTeam1;
                _Data.ScreenNames.ProfileIDsTeam2 = config.ProfileIDsTeam2;
                if (_Data.ScreenNames.ProfileIDsTeam1 == null)
                    _Data.ScreenNames.ProfileIDsTeam1 = new List<int>();
                if (_Data.ScreenNames.ProfileIDsTeam2 == null)
                    _Data.ScreenNames.ProfileIDsTeam2 = new List<int>();

                _NumPlayerTeam1 = config.NumPlayerTeam1;
                _NumPlayerTeam2 = config.NumPlayerTeam2;

                while (_Data.ScreenNames.ProfileIDsTeam1.Count > _NumPlayerTeam1)
                    _Data.ScreenNames.ProfileIDsTeam1.RemoveAt(_Data.ScreenNames.ProfileIDsTeam1.Count - 1);
                while (_Data.ScreenNames.ProfileIDsTeam2.Count > _NumPlayerTeam2)
                    _Data.ScreenNames.ProfileIDsTeam2.RemoveAt(_Data.ScreenNames.ProfileIDsTeam2.Count - 1);
            }
            catch (Exception e)
            {
                CBase.Log.LogError("Error in party mode screen TicTacToe names. Can't cast received data from game mode " + ThemeName + ". " + e.Message);
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

                        if (_Buttons[_ButtonPlayerChooseScrollUp].Selected)
                            _Scroll(-1);

                        if (_Buttons[_ButtonPlayerChooseScrollDown].Selected)
                            _Scroll(1);

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

            //Check if LeftButton is hold and Select-Mode inactive
            if (mouseEvent.LBH && !_SelectingMouseActive)
            {
                //Save mouse-coords
                _OldMouseX = mouseEvent.X;
                _OldMouseY = mouseEvent.Y;
                //Check if mouse if over tile
                foreach (CPlayerChooseButton chooseButton in _PlayerChooseButtons)
                {
                    if (chooseButton.Button.Selected)
                    {
                        _SelectedPlayerNr = chooseButton.ProfileID;
                        if (_SelectedPlayerNr != -1)
                        {
                            //Activate mouse-selecting
                            _SelectingMouseActive = true;
                            //Update of Drag/Drop-Texture
                            _ChooseAvatarStatic.Visible = true;
                            _ChooseAvatarStatic.Rect = chooseButton.Button.Rect;
                            _ChooseAvatarStatic.Rect.Z = -100;
                            _ChooseAvatarStatic.Color = new SColorF(1, 1, 1, 1);
                            _ChooseAvatarStatic.Texture = CBase.Profiles.GetAvatar(_SelectedPlayerNr);
                        }
                    }
                }
                return true;
            }

            //Check if LeftButton is hold and Select-Mode active
            if (mouseEvent.LBH && _SelectingMouseActive)
            {
                //Update coords for Drag/Drop-Texture
                _ChooseAvatarStatic.Rect.X += mouseEvent.X - _OldMouseX;
                _ChooseAvatarStatic.Rect.Y += mouseEvent.Y - _OldMouseY;
                _OldMouseX = mouseEvent.X;
                _OldMouseY = mouseEvent.Y;

                return true;
            }
            // LeftButton isn't hold anymore, but Selec-Mode is still active -> "Drop" of Avatar
            if (_SelectingMouseActive)
            {
                //Check if really a player was selected
                if (_SelectedPlayerNr != -1)
                {
                    //Foreach Drop-Area
                    for (int i = 0; i < _PlayerDestinationButtons.Count; i++)
                    {
                        //Check first, if area is "active"
                        if (_PlayerDestinationButtons[i].Visible)
                        {
                            //Check if Mouse is in area
                            if (CHelper.IsInBounds(_PlayerDestinationButtons[i].Rect, mouseEvent))
                            {
                                int added = -1;
                                //Add Player-ID to list.
                                if (i - _PlayerDestinationButtonsNumH < 0)
                                {
                                    if (_Data.ScreenNames.ProfileIDsTeam1.Count < (i + 1))
                                    {
                                        _Data.ScreenNames.ProfileIDsTeam1.Add(_SelectedPlayerNr);
                                        added = _Data.ScreenNames.ProfileIDsTeam1.Count - 1;
                                    }
                                    else if (_Data.ScreenNames.ProfileIDsTeam1.Count >= (i + 1))
                                    {
                                        _Data.ScreenNames.ProfileIDsTeam1[i] = _SelectedPlayerNr;
                                        added = i;
                                    }
                                }
                                else if (i - _PlayerDestinationButtonsNumH >= 0)
                                {
                                    if (_Data.ScreenNames.ProfileIDsTeam2.Count < ((i - _PlayerDestinationButtonsNumH) + 1))
                                    {
                                        _Data.ScreenNames.ProfileIDsTeam2.Add(_SelectedPlayerNr);
                                        added = _Data.ScreenNames.ProfileIDsTeam2.Count - 1 + _PlayerDestinationButtonsNumH;
                                    }
                                    else if (_Data.ScreenNames.ProfileIDsTeam2.Count >= ((i - _PlayerDestinationButtonsNumH) + 1))
                                    {
                                        _Data.ScreenNames.ProfileIDsTeam2[i - _PlayerDestinationButtonsNumH] = _SelectedPlayerNr;
                                        added = i;
                                    }
                                }
                                _UpdateButtonNext();
                                //Update texture and name
                                _PlayerDestinationButtons[added].Color = new SColorF(1, 1, 1, 0.6f);
                                _PlayerDestinationButtons[added].SelColor = new SColorF(1, 1, 1, 1);
                                _PlayerDestinationButtons[added].Texture = _ChooseAvatarStatic.Texture;
                                _PlayerDestinationButtons[added].SelTexture = _ChooseAvatarStatic.Texture;
                                _PlayerDestinationButtons[added].Text.Text = CBase.Profiles.GetPlayerName(_SelectedPlayerNr);
                                _PlayerDestinationButtons[added].Enabled = true;
                                //Update Tiles-List
                                _UpdateButtonPlayerChoose();
                            }
                        }
                    }
                    _SelectedPlayerNr = -1;
                }
                //Reset variables
                _SelectingMouseActive = false;
                _ChooseAvatarStatic.Visible = false;
                return true;
            }

            if (mouseEvent.LB && _IsMouseOver(mouseEvent))
            {
                if (_Buttons[_ButtonBack].Selected)
                    _Back();

                if (_Buttons[_ButtonNext].Selected)
                    _Next();

                if (_Buttons[_ButtonPlayerChooseScrollUp].Selected)
                    _Scroll(-1);

                if (_Buttons[_ButtonPlayerChooseScrollDown].Selected)
                    _Scroll(1);
            }

            if (mouseEvent.LD && _IsMouseOver(mouseEvent))
            {
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

        private void _Scroll(int offset)
        {
            if (offset < 0 && _PlayerChooseButtonsOffset > 0)
            {
                _PlayerChooseButtonsOffset += offset;
                _UpdateButtonPlayerChoose();
            }
            else if (_PlayerChooseButtonsVisibleProfiles.Count < _PlayerChooseButtons.Count + (_PlayerChooseButtonsOffset + offset) * _PlayerChooseButtonsNumH)
            {
                _PlayerChooseButtonsOffset += offset;
                _UpdateButtonPlayerChoose();
            }
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
            _Buttons[_ButtonPlayerChooseScrollUp].Enabled = _PlayerChooseButtonsOffset > 0;
            _Buttons[_ButtonPlayerChooseScrollDown].Enabled = _PlayerChooseButtonsVisibleProfiles.Count >
                                                              _PlayerChooseButtons.Count + _PlayerChooseButtonsOffset * _PlayerChooseButtonsNumH;
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
                    visible = _Data.ScreenNames.ProfileIDsTeam1.All(profileID => profileID != profiles[i].ID) && _Data.ScreenNames.ProfileIDsTeam2.All(profileID => profileID != profiles[i].ID);
                if (visible)
                    _PlayerChooseButtonsVisibleProfiles.Add(profiles[i].ID);
            }
        }

        private void _UpdateButtonPlayerDestination()
        {
            for (int i = 0; i < _PlayerDestinationButtons.Count / 2; i++)
            {
                if (_NumPlayerTeam1 > i)
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
            for (int i = 0; i < _PlayerDestinationButtons.Count / 2; i++)
            {
                if (_NumPlayerTeam2 > i)
                {
                    _PlayerDestinationButtons[i + _PlayerDestinationButtonsNumH].Visible = true;
                    _PlayerDestinationButtons[i + _PlayerDestinationButtonsNumH].Enabled = true;
                }
                else
                {
                    _PlayerDestinationButtons[i + _PlayerDestinationButtonsNumH].Visible = false;
                    _PlayerDestinationButtons[i + _PlayerDestinationButtonsNumH].Enabled = false;
                }
            }
            for (int i = 0; i < _NumPlayerTeam1; i++)
            {
                if (i < _Data.ScreenNames.ProfileIDsTeam1.Count)
                {
                    if (_Data.ScreenNames.ProfileIDsTeam1[i] != -1)
                    {
                        int id = _Data.ScreenNames.ProfileIDsTeam1[i]; 
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
            for (int i = 0; i < _NumPlayerTeam2; i++)
            {
                if (i < _Data.ScreenNames.ProfileIDsTeam2.Count)
                {
                    if (_Data.ScreenNames.ProfileIDsTeam2[i] != -1)
                    {
                        int id = _Data.ScreenNames.ProfileIDsTeam2[i]; 
                        _PlayerDestinationButtons[i + _PlayerDestinationButtonsNumH].Color = new SColorF(1, 1, 1, 0.6f);
                        _PlayerDestinationButtons[i + _PlayerDestinationButtonsNumH].SelColor = new SColorF(1, 1, 1, 1);
                        _PlayerDestinationButtons[i].Texture = CBase.Profiles.GetAvatar(id);
                        _PlayerDestinationButtons[i].SelTexture = CBase.Profiles.GetAvatar(id);
                        _PlayerDestinationButtons[i].Text.Text = CBase.Profiles.GetPlayerName(id);
                        _PlayerDestinationButtons[i + _PlayerDestinationButtonsNumH].Enabled = true;
                    }
                }
                else
                {
                    _PlayerDestinationButtons[i + _PlayerDestinationButtonsNumH].Color = _Buttons[_ButtonPlayerDestination].Color;
                    _PlayerDestinationButtons[i + _PlayerDestinationButtonsNumH].SelColor = _Buttons[_ButtonPlayerDestination].SelColor;
                    _PlayerDestinationButtons[i + _PlayerDestinationButtonsNumH].Texture = _Buttons[_ButtonPlayerDestination].Texture;
                    _PlayerDestinationButtons[i + _PlayerDestinationButtonsNumH].SelTexture = _Buttons[_ButtonPlayerDestination].SelTexture;
                    _PlayerDestinationButtons[i + _PlayerDestinationButtonsNumH].Text.Text = String.Empty;
                    _PlayerDestinationButtons[i + _PlayerDestinationButtonsNumH].Enabled = false;
                }
            }
        }

        private void _UpdateButtonNext()
        {
            if (_Data.ScreenNames.ProfileIDsTeam1.Count == _NumPlayerTeam1 && _Data.ScreenNames.ProfileIDsTeam2.Count == _NumPlayerTeam2)
            {
                _Buttons[_ButtonNext].Visible = true;
                _SetInteractionToButton(_Buttons[_ButtonNext]);
            }
            else
                _Buttons[_ButtonNext].Visible = false;
        }

        private bool _OnAdd()
        {
            foreach (CPlayerChooseButton chooseButton in _PlayerChooseButtons.Where(chooseButton => chooseButton.Button.Selected && chooseButton.ProfileID != -1))
            {
                if (_Data.ScreenNames.ProfileIDsTeam1.Count < _NumPlayerTeam1)
                {
                    _Data.ScreenNames.ProfileIDsTeam1.Add(chooseButton.ProfileID);
                    int added = _Data.ScreenNames.ProfileIDsTeam1.Count - 1;
                    _UpdateButtonNext();
                    //Update texture and name
                    _PlayerDestinationButtons[added].Color = new SColorF(1, 1, 1, 0.6f);
                    _PlayerDestinationButtons[added].SelColor = new SColorF(1, 1, 1, 1);
                    _PlayerDestinationButtons[added].Texture = CBase.Profiles.GetAvatar(chooseButton.ProfileID);
                    _PlayerDestinationButtons[added].SelTexture = CBase.Profiles.GetAvatar(chooseButton.ProfileID);
                    _PlayerDestinationButtons[added].Text.Text = CBase.Profiles.GetPlayerName(chooseButton.ProfileID);
                    _PlayerDestinationButtons[added].Enabled = true;
                    //Update Tiles-List
                    _UpdateButtonPlayerChoose();
                    _CheckInteraction();
                    return true;
                }
                if (_Data.ScreenNames.ProfileIDsTeam2.Count < _NumPlayerTeam2)
                {
                    _Data.ScreenNames.ProfileIDsTeam2.Add(chooseButton.ProfileID);
                    int added = (_Data.ScreenNames.ProfileIDsTeam2.Count - 1) + _PlayerDestinationButtonsNumH;
                    _UpdateButtonNext();
                    //Update texture and name
                    _PlayerDestinationButtons[added].Color = new SColorF(1, 1, 1, 0.6f);
                    _PlayerDestinationButtons[added].SelColor = new SColorF(1, 1, 1, 1);
                    _PlayerDestinationButtons[added].Texture = CBase.Profiles.GetAvatar(chooseButton.ProfileID);
                    _PlayerDestinationButtons[added].SelTexture = CBase.Profiles.GetAvatar(chooseButton.ProfileID);
                    _PlayerDestinationButtons[added].Text.Text = CBase.Profiles.GetPlayerName(chooseButton.ProfileID);
                    _PlayerDestinationButtons[added].Enabled = true;
                    //Update Tiles-List
                    _UpdateButtonPlayerChoose();
                    _CheckInteraction();
                    return true;
                }
            }
            return false;
        }

        private bool _OnRemove()
        {
            for (int i = 0; i < _PlayerDestinationButtonsNumH; i++)
            {
                if (_PlayerDestinationButtons[i].Selected)
                {
                    if ((i + 1) <= _Data.ScreenNames.ProfileIDsTeam1.Count)
                    {
                        _Data.ScreenNames.ProfileIDsTeam1.RemoveAt(i);
                        _UpdateButtonNext();
                        _UpdateButtonPlayerDestination();
                        _UpdateButtonPlayerChoose();
                        _CheckInteraction();
                        return true;
                    }
                }
            }
            for (int i = _PlayerDestinationButtonsNumH; i < _PlayerDestinationButtonsNumH * 2; i++)
            {
                if (_PlayerDestinationButtons[i].Selected)
                {
                    if (((i - _PlayerDestinationButtonsNumH) + 1) <= _Data.ScreenNames.ProfileIDsTeam2.Count)
                    {
                        _Data.ScreenNames.ProfileIDsTeam2.RemoveAt(i - _PlayerDestinationButtonsNumH);
                        _UpdateButtonNext();
                        _UpdateButtonPlayerDestination();
                        _UpdateButtonPlayerChoose();
                        _CheckInteraction();
                        return true;
                    }
                }
            }
            return false;
        }

        private void _Back()
        {
            _Data.ScreenNames.FadeToConfig = true;
            _PartyMode.DataFromScreen(ThemeName, _Data);
        }

        private void _Next()
        {
            _Data.ScreenNames.FadeToConfig = false;
            _PartyMode.DataFromScreen(ThemeName, _Data);
        }
    }
}