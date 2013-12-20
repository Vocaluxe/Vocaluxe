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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using VocaluxeLib.PartyModes;
using VocaluxeLib.Draw;
using VocaluxeLib.Profile;

namespace VocaluxeLib.Menu
{
    public abstract class CMenuPartyNameSelection : CMenuParty
    {
        private bool _Teams;
        protected int _NumTeams = -1;
        protected int _NumPlayer = -1;
        protected int[] _NumPlayerTeams;
        private int _CurrentTeam = 0;

        private bool _AvatarsChanged;
        private bool _ProfilesChanged;

        private bool _SelectingKeyboardActive;
        private bool _SelectingFast;
        private int _SelectingSwitchNr = -1;
        private int _SelectingFastPlayerNr;
        private int _SelectedProfileID = -1;

        private CStatic _ChooseAvatarStatic;
        private int _OldMouseX;
        private int _OldMouseY;

        private const string _ButtonNext = "ButtonNext";
        private const string _ButtonBack = "ButtonBack";
        private const string _SelectSlideTeams = "SelectSlideTeams";
        private const string _SelectSlidePlayer = "SelectSlidePlayer";
        private const string _NameSelection = "NameSelection";

        private List<int>[] _TeamList;

        public override void Init()
        {
            base.Init();
            _Teams = _PartyMode.GetMinTeams() > 0;

            _ThemeButtons = new string[] { _ButtonBack, _ButtonNext };
            _ThemeSelectSlides = new string[] { _SelectSlideTeams, _SelectSlidePlayer };
            _ThemeNameSelections = new string[] { _NameSelection };

            _ChooseAvatarStatic = GetNewStatic();
            _ChooseAvatarStatic.Visible = false;
            _ChooseAvatarStatic.Aspect = EAspect.Crop;

            CBase.Profiles.AddProfileChangedCallback(_OnProfileChanged);
        }

        public void SetPartyModeData(int numTeams, int numPlayer, int[] numPlayerTeams) 
        {
            _NumTeams = numTeams;
            _NumPlayer = numPlayer;
            _NumPlayerTeams = numPlayerTeams;

            if (_NumTeams > 0)
                _TeamList = new List<int>[_NumTeams];
            else
                _TeamList = new List<int>[1];

            if (_NumTeams != _NumPlayerTeams.Length)
                _NumPlayerTeams = new int[_NumTeams];

            for (int i = 0; i < _TeamList.Length; i++)
                _TeamList[i] = new List<int>();

            _UpdateSlides();
            _LoadProfiles();
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            //Check if selecting with keyboard is active
            if (_SelectingKeyboardActive)
            {
                //Handle left/right/up/down
                _NameSelections[_NameSelection].HandleInput(keyEvent);
                int numberPressed = -1;
                bool resetSelection = false;
                switch (keyEvent.Key)
                {
                    case Keys.Enter:
                        //Check, if a player is selected
                        if (_NameSelections[_NameSelection].Selection > -1)
                        {
                            _SelectedProfileID = _NameSelections[_NameSelection].Selection;

                            if (!CBase.Profiles.IsProfileIDValid(_SelectedProfileID))
                                return true;

                            _AddPlayer(_CurrentTeam, _SelectedProfileID);
                        }
                        //Started selecting with 'P'
                        if (_SelectingFast &&_SelectSlides[_SelectSlidePlayer].NumValues == _NumPlayerTeams[_CurrentTeam])
                                resetSelection = true;
                        else if(!_SelectingFast)
                            resetSelection = true;
                        break;

                    case Keys.Escape:
                    case Keys.Back:
                        resetSelection = true;
                        break;
                }
                if (numberPressed > 0 || resetSelection)
                {
                    if (numberPressed == _SelectingFastPlayerNr || resetSelection)
                    {
                        //Reset all values
                        _SelectingFastPlayerNr = 0;
                        _SelectingKeyboardActive = false;
                        _NameSelections[_NameSelection].FastSelection(false, -1);
                    }
                    else if (numberPressed <= _NumPlayerTeams[_CurrentTeam])
                    {
                        _SelectingFastPlayerNr = numberPressed;
                        _NameSelections[_NameSelection].FastSelection(true, numberPressed);
                    }
                    _SelectingFast = false;
                }
            }
            else
            {
                base.HandleInput(keyEvent);

                switch (keyEvent.Key)
                {
                    case Keys.Back:
                    case Keys.Escape:
                        Back();
                        break;

                    case Keys.Enter:
                        if (_Buttons[_ButtonBack].Selected)
                            Back();

                        if (_Buttons[_ButtonNext].Selected)
                            Next();

                        break;

                    case Keys.Delete:
                        if (_SelectSlides[_SelectSlidePlayer].Selected && _SelectSlides[_SelectSlidePlayer].NumValues > 0)
                        {
                            int index = _SelectSlides[_SelectSlidePlayer].Selection;
                            _RemovePlayerByIndex(_CurrentTeam, index);
                            _UpdatePlayerSlide();
                        }
                        break;

                    case Keys.Left:
                    case Keys.Right:
                        if (_SelectSlides[_SelectSlideTeams].Selected)
                            _ChangeTeamSlide();
                        break;

                    case Keys.P:
                        if (!_SelectingKeyboardActive)
                        {
                            _SelectingFastPlayerNr = (_CurrentTeam + 1);
                            _SelectingFast = true;
                            //_ResetPlayerSelections();
                        }
                        break;
                }
            }
            if (_SelectingFastPlayerNr > 0 && _SelectingFastPlayerNr <= _NumPlayerTeams[_CurrentTeam])
            {
                _SelectingKeyboardActive = true;
                _NameSelections[_NameSelection].FastSelection(true, _SelectingFastPlayerNr);
            }
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            bool stopSelectingFast = false;

            if (_SelectingFast)
                _NameSelections[_NameSelection].HandleMouse(mouseEvent);
            else
                base.HandleMouse(mouseEvent);

            //Check if LeftButton is hold and Select-Mode inactive
            if (mouseEvent.LBH && _SelectedProfileID < 0 && !_SelectingFast)
            {
                //Save mouse-coords
                _OldMouseX = mouseEvent.X;
                _OldMouseY = mouseEvent.Y;
                //Check if mouse if over tile
                if (_NameSelections[_NameSelection].IsOverTile(mouseEvent))
                {
                    //Get player-number of tile
                    _SelectedProfileID = _NameSelections[_NameSelection].TilePlayerNr(mouseEvent);
                    if (_SelectedProfileID != -1)
                    {
                        //Update of Drag/Drop-Texture
                        CStatic selectedPlayer = _NameSelections[_NameSelection].TilePlayerAvatar(mouseEvent);
                        _ChooseAvatarStatic.Visible = true;
                        _ChooseAvatarStatic.Rect = selectedPlayer.Rect;
                        _ChooseAvatarStatic.Rect.Z = CBase.Settings.GetZNear();
                        _ChooseAvatarStatic.Color = new SColorF(1, 1, 1, 1);
                        _ChooseAvatarStatic.Texture = selectedPlayer.Texture;
                    }
                }
            }
            //Check if LeftButton is hold and Select-Mode active
            if (mouseEvent.LBH && _SelectedProfileID >= 0 && !_SelectingFast)
            {
                //Update coords for Drag/Drop-Texture
                _ChooseAvatarStatic.Rect.X += mouseEvent.X - _OldMouseX;
                _ChooseAvatarStatic.Rect.Y += mouseEvent.Y - _OldMouseY;
                _OldMouseX = mouseEvent.X;
                _OldMouseY = mouseEvent.Y;
            }
            // LeftButton isn't hold anymore, but Select-Mode is still active -> "Drop" of Avatar
            else if (_SelectedProfileID >= 0 && !_SelectingFast)
            {
                //Check if mouse is in drop-area
                if (CHelper.IsInBounds(_SelectSlides[_SelectSlidePlayer].Rect, mouseEvent))
                {
                    if (!CBase.Profiles.IsProfileIDValid(_SelectedProfileID))
                        return true;

                    _AddPlayer(_CurrentTeam, _SelectedProfileID);
                }

                //Reset variables
                _SelectingSwitchNr = -1;
                _SelectedProfileID = -1;
                _ChooseAvatarStatic.Visible = false;
            }
            if (mouseEvent.LB && _SelectingFast)
            {
                if (_NameSelections[_NameSelection].IsOverTile(mouseEvent))
                {
                    //Get player-number of tile
                    _SelectedProfileID = _NameSelections[_NameSelection].TilePlayerNr(mouseEvent);
                    if (_SelectedProfileID != -1)
                    {
                        if (!CBase.Profiles.IsProfileIDValid(_SelectedProfileID))
                            return true;

                        _AddPlayer(_CurrentTeam, _SelectedProfileID);

                        if (_SelectSlides[_SelectSlidePlayer].NumValues == _NumPlayerTeams[_CurrentTeam])
                            stopSelectingFast = true;
                    }
                    else
                        stopSelectingFast = true;
                }
            }

            else if (mouseEvent.LB && _IsMouseOver(mouseEvent))
            {
                if (_Buttons[_ButtonBack].Selected)
                    Back();

                if (_Buttons[_ButtonNext].Selected)
                    Next();

                if (_SelectSlides[_SelectSlideTeams].Selected)
                    _ChangeTeamSlide();

                //Update Tiles-List
                _NameSelections[_NameSelection].UpdateList();
            }

            if (mouseEvent.LD && _NameSelections[_NameSelection].IsOverTile(mouseEvent) && !_SelectingFast)
            {
                _SelectedProfileID = _NameSelections[_NameSelection].TilePlayerNr(mouseEvent);
                if (_SelectedProfileID > -1)
                {
                    if (!CBase.Profiles.IsProfileIDValid(_SelectedProfileID))
                        return true;

                    _AddPlayer(_CurrentTeam, _SelectedProfileID);
                }
            }

            if (mouseEvent.RB && _SelectingFast)
                stopSelectingFast = true;
            else if (mouseEvent.RB)
            {
                bool exit = true;
                if (_SelectSlides[_SelectSlidePlayer].Selected && _SelectSlides[_SelectSlidePlayer].NumValues > 0 )
                {
                    int currentSelection = _SelectSlides[_SelectSlidePlayer].Selection;
                    int id = _TeamList[_CurrentTeam][currentSelection];
                    _RemovePlayer(_CurrentTeam, id);
                    _UpdatePlayerSlide();
                    exit = false;
                }
                
                if (exit)
                    Back();
            }

            if (mouseEvent.MB && _SelectingFast)
            {
                if (_SelectSlides[_SelectSlidePlayer].NumValues == _NumPlayerTeams[_CurrentTeam])
                    stopSelectingFast = true;
            }
            else if (mouseEvent.MB)
            {
                _SelectingFast = true;
                _SelectingFastPlayerNr = (_CurrentTeam + 1);
                _SelectingKeyboardActive = true;
                _NameSelections[_NameSelection].FastSelection(true, _SelectingFastPlayerNr);
            }

            //Check mouse-wheel for scrolling
            if (mouseEvent.Wheel != 0)
            {
                if (CHelper.IsInBounds(_NameSelections[_NameSelection].Rect, mouseEvent))
                {
                    int offset = _NameSelections[_NameSelection].Offset + mouseEvent.Wheel;
                    _NameSelections[_NameSelection].UpdateList(offset);
                }
            }

            if (stopSelectingFast)
            {
                _SelectingFast = false;
                _SelectingFastPlayerNr = 0;
                _SelectingKeyboardActive = false;
                _NameSelections[_NameSelection].FastSelection(false, -1);
            }
            return true;
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);
            _SelectSlides[_SelectSlidePlayer].WithTextures = true;
            _SelectSlides[_SelectSlidePlayer].SelectionByHover = true;
        }

        public override bool UpdateGame()
        {
            if (_ProfilesChanged || _AvatarsChanged)
                _LoadProfiles();

            return true;
        }

        public override void OnShow()
        {
            _NameSelections[_NameSelection].Init();

            base.OnShow();
        }

        public override bool Draw()
        {
            base.Draw();

            if (_ChooseAvatarStatic.Visible)
                _ChooseAvatarStatic.Draw();

            return true;
        }

        public abstract void Back();
        public abstract void Next();

        public List<int> GetTeamIDs(int team)
        {
            if (team < _TeamList.Length)
                return _TeamList[team];
            return null;
        }

        public List<int>[] GetTeamIDs()
        {
            return _TeamList;
        }
        #region private methods

        private void _OnProfileChanged(EProfileChangedFlags flags)
        {
            if (EProfileChangedFlags.Avatar == (EProfileChangedFlags.Avatar & flags))
                _AvatarsChanged = true;

            if (EProfileChangedFlags.Profile == (EProfileChangedFlags.Profile & flags))
                _ProfilesChanged = true;
        }

        private void _LoadProfiles()
        {
            _NameSelections[_NameSelection].UpdateList();

            _UpdateSlides();
            _ChangeTeamSlide();

            _ProfilesChanged = false;
            _AvatarsChanged = false;
        }

        private void _UpdateSlides()
        {
            _SelectSlides[_SelectSlideTeams].Clear();
            for (int i = 1; i <= _NumTeams; i++)
                _SelectSlides[_SelectSlideTeams].AddValue("Team " + i);

            _SelectSlides[_SelectSlidePlayer].Clear();
        }

        private void _ChangeTeamSlide()
        {
            if (_CurrentTeam == _SelectSlides[_SelectSlideTeams].Selection)
                return;

            _CurrentTeam = _SelectSlides[_SelectSlideTeams].Selection;
            _UpdatePlayerSlide();
        }

        private void _UpdatePlayerSlide()
        {
            _SelectSlides[_SelectSlidePlayer].Clear();
            for (int i = 0; i < _TeamList[_CurrentTeam].Count; i++)
            {
                string name = CBase.Profiles.GetPlayerName(_TeamList[_CurrentTeam][i]);
                CTexture avatar = CBase.Profiles.GetAvatar(_TeamList[_CurrentTeam][i]);
                _SelectSlides[_SelectSlidePlayer].AddValue(name, avatar);
            }
        }

        private void _AddPlayer(int team, int profileID)
        {
            if (_NumPlayerTeams[team] == _TeamList[team].Count)
                return;

            _NameSelections[_NameSelection].UseProfile(profileID);
            _TeamList[team].Add(profileID);

            if (team == _CurrentTeam)
            {
                string name = CBase.Profiles.GetPlayerName(profileID);
                CTexture avatar = CBase.Profiles.GetAvatar(profileID);
                _SelectSlides[_SelectSlidePlayer].AddValue(name, avatar);
            }
        }

        private void _RemovePlayerByIndex(int team, int index)
        {
            if (_TeamList[team].Count > index)
            {
                int id = _TeamList[team][index];
                _TeamList[team].RemoveAt(index);
                _NameSelections[_NameSelection].RemoveUsedProfile(id);
            }
        }

        private void _RemovePlayer(int team, int profileID)
        {
            _TeamList[team].Remove(profileID);
            _NameSelections[_NameSelection].RemoveUsedProfile(profileID);
        }

        #endregion 
    }
}
