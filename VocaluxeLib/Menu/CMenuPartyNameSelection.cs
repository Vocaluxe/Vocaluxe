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

using System.Collections.Generic;
using System.Windows.Forms;
using VocaluxeLib.Draw;
using VocaluxeLib.Profile;

namespace VocaluxeLib.Menu
{
    public abstract class CMenuPartyNameSelection : CMenuParty
    {
        private bool _AllPlayerSelected
        {
            get
            {
                for (int team = 0; team < _NumTeams; team++)
                {
                    if (_TeamList[team].Count < _NumPlayerTeams[team])
                        return false;
                }
                return true;
            }
        }

        private bool _Teams;
        protected int _NumTeams = -1;
        protected int _NumPlayer = -1;
        protected int[] _NumPlayerTeams;
        protected bool _AllowChangePlayerNum = true;
        protected bool _AllowChangeTeamNum = true;
        protected bool _ChangePlayerNumDynamic = true;
        protected bool _ChangeTeamNumDynamic = true;
        private int _CurrentTeam;

        private bool _AvatarsChanged;
        private bool _ProfilesChanged;

        private bool _SelectingKeyboardActive;
        private bool _SelectingFast;
        private int _SelectingFastPlayerNr;
        private int _SelectedProfileID = -1;

        private CStatic _ChooseAvatarStatic;
        private int _OldMouseX;
        private int _OldMouseY;

        private const string _ButtonNext = "ButtonNext";
        private const string _ButtonBack = "ButtonBack";
        private const string _ButtonRandom = "ButtonRandom";
        private const string _ButtonIncreaseTeams = "ButtonIncreaseTeams";
        private const string _ButtonDecreaseTeams = "ButtonDecreaseTeams";
        private const string _ButtonIncreasePlayer = "ButtonIncreasePlayer";
        private const string _ButtonDecreasePlayer = "ButtonDecreasePlayer";
        private const string _SelectSlideTeams = "SelectSlideTeams";
        private const string _SelectSlidePlayer = "SelectSlidePlayer";
        private const string _NameSelection = "NameSelection";

        protected List<int>[] _TeamList;

        public override void Init()
        {
            base.Init();
            _ThemeButtons = new string[] {_ButtonBack, _ButtonNext, _ButtonRandom, _ButtonIncreaseTeams, _ButtonDecreaseTeams, _ButtonIncreasePlayer, _ButtonDecreasePlayer};
            _ThemeSelectSlides = new string[] {_SelectSlideTeams, _SelectSlidePlayer};
            _ThemeNameSelections = new string[] {_NameSelection};

            _ChooseAvatarStatic = GetNewStatic();
            _ChooseAvatarStatic.Visible = false;
            _ChooseAvatarStatic.Aspect = EAspect.Crop;

            CBase.Profiles.AddProfileChangedCallback(_OnProfileChanged);
        }

        public void SetPartyModeData(int numPlayer)
        {
            SetPartyModeData(1, numPlayer, new int[] {numPlayer});
            _CurrentTeam = 0;
        }

        public void SetPartyModeData(int numTeams, int numPlayer, int[] numPlayerTeams)
        {
            _NumTeams = numTeams;
            _NumPlayer = numPlayer;
            _NumPlayerTeams = numPlayerTeams;

            _TeamList = new List<int>[_NumTeams > 0 ? _NumTeams : 1];

            if (_NumTeams != _NumPlayerTeams.Length)
                _NumPlayerTeams = new int[_NumTeams];

            for (int i = 0; i < _TeamList.Length; i++)
                _TeamList[i] = new List<int>();

            _Teams = _NumTeams > 0;

            _UpdateSlides();
            _LoadProfiles();
            _UpdateButtonVisibility();
            _UpdateButtonState();
            _UpdateNextButtonVisibility();
        }

        public void SetPartyModeProfiles(List<int>[] teamProfiles)
        {
            _TeamList = teamProfiles;
            _UpdateSlides();
            _UpdateNextButtonVisibility();
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            //Check if selecting with keyboard is active
            if (_SelectingKeyboardActive)
            {
                //Handle left/right/up/down
                _NameSelections[_NameSelection].HandleInput(keyEvent);
                int numPressed = -1;
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
                        if (_SelectingFast)
                        {
                            if (!_ChangePlayerNumDynamic && _TeamList[_CurrentTeam].Count == _NumPlayerTeams[_CurrentTeam])
                                resetSelection = true;
                            else if (_TeamList[_CurrentTeam].Count == _PartyMode.MaxPlayersPerTeam)
                                resetSelection = true;
                        }
                        else if (!_SelectingFast)
                            resetSelection = true;
                        break;

                    case Keys.Escape:
                    case Keys.Back:
                        resetSelection = true;
                        break;

                    case Keys.D1:
                    case Keys.NumPad1:
                        numPressed = 1;
                        break;

                    case Keys.D2:
                    case Keys.NumPad2:
                        numPressed = 2;
                        break;

                    case Keys.D3:
                    case Keys.NumPad3:
                        numPressed = 3;
                        break;

                    case Keys.D4:
                    case Keys.NumPad4:
                        numPressed = 4;
                        break;

                    case Keys.D5:
                    case Keys.NumPad5:
                        numPressed = 5;
                        break;

                    case Keys.D6:
                    case Keys.NumPad6:
                        numPressed = 6;
                        break;

                    case Keys.D7:
                    case Keys.NumPad7:
                        numPressed = 7;
                        break;

                    case Keys.D8:
                    case Keys.NumPad8:
                        numPressed = 8;
                        break;

                    case Keys.D9:
                    case Keys.NumPad9:
                        numPressed = 9;
                        break;
                }
                if (numPressed > 0 || resetSelection)
                {
                    if (numPressed == _SelectingFastPlayerNr || resetSelection)
                    {
                        //Reset all values
                        _SelectingFastPlayerNr = 0;
                        _SelectingKeyboardActive = false;
                        _NameSelections[_NameSelection].FastSelection(false, -1);
                    }
                    else if (numPressed <= _NumPlayerTeams[_CurrentTeam])
                    {
                        _SelectingFastPlayerNr = numPressed;
                        _NameSelections[_NameSelection].FastSelection(true, numPressed);
                    }
                    _SelectingFast = false;
                }
            }
            else
            {
                base.HandleInput(keyEvent);

                int numPressed = 0;

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

                        if (_Buttons[_ButtonRandom].Selected)
                            _SelectRandom();

                        if (_Buttons[_ButtonIncreaseTeams].Selected)
                            IncreaseTeamNum();

                        if (_Buttons[_ButtonDecreaseTeams].Selected)
                            DecreaseTeamNum();

                        if (_Buttons[_ButtonIncreasePlayer].Selected)
                            IncreasePlayerNum(_CurrentTeam);

                        if (_Buttons[_ButtonDecreasePlayer].Selected)
                            DecreasePlayerNum(_CurrentTeam);

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
                            _OnChangeTeamSlide();
                        break;

                    case Keys.P:
                        if (!_SelectingKeyboardActive)
                        {
                            _SelectingFastPlayerNr = (_CurrentTeam + 1);
                            _SelectingFast = true;
                            //_ResetPlayerSelections();
                        }
                        break;

                    case Keys.D1:
                    case Keys.NumPad1:
                        numPressed = 1;
                        break;

                    case Keys.D2:
                    case Keys.NumPad2:
                        numPressed = 2;
                        break;

                    case Keys.D3:
                    case Keys.NumPad3:
                        numPressed = 3;
                        break;

                    case Keys.D4:
                    case Keys.NumPad4:
                        numPressed = 4;
                        break;

                    case Keys.D5:
                    case Keys.NumPad5:
                        numPressed = 5;
                        break;

                    case Keys.D6:
                    case Keys.NumPad6:
                        numPressed = 6;
                        break;

                    case Keys.D7:
                    case Keys.NumPad7:
                        numPressed = 7;
                        break;

                    case Keys.D8:
                    case Keys.NumPad8:
                        numPressed = 8;
                        break;

                    case Keys.D9:
                    case Keys.NumPad9:
                        numPressed = 9;
                        break;

                    case Keys.Subtract:
                        if (_SelectSlides[_SelectSlideTeams].Selected)
                            DecreaseTeamNum();
                        else if (_SelectSlides[_SelectSlidePlayer].Selected)
                            DecreasePlayerNum(_CurrentTeam);
                        break;

                    case Keys.Add:
                        if (_SelectSlides[_SelectSlideTeams].Selected)
                            IncreaseTeamNum();
                        else if (_SelectSlides[_SelectSlidePlayer].Selected)
                            IncreasePlayerNum(_CurrentTeam);
                        break;
                }

                if (numPressed > 0)
                {
                    if (_ChangeTeamNumDynamic && numPressed < _PartyMode.MaxTeams && numPressed > _SelectSlides[_SelectSlideTeams].NumValues)
                    {
                        while (numPressed < _PartyMode.MaxTeams)
                            IncreaseTeamNum();
                    }
                    if (numPressed <= _SelectSlides[_SelectSlideTeams].NumValues)
                        _SelectSlides[_SelectSlideTeams].SelectedTag = numPressed;
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
                        _ChooseAvatarStatic.MaxRect = selectedPlayer.Rect;
                        _ChooseAvatarStatic.Z = CBase.Settings.GetZNear();
                        _ChooseAvatarStatic.Color = new SColorF(1, 1, 1, 1);
                        _ChooseAvatarStatic.Texture = selectedPlayer.Texture;
                    }
                }
            }
            //Check if LeftButton is hold and Select-Mode active
            if (mouseEvent.LBH && _SelectedProfileID >= 0 && !_SelectingFast)
            {
                //Update coords for Drag/Drop-Texture
                _ChooseAvatarStatic.X += mouseEvent.X - _OldMouseX;
                _ChooseAvatarStatic.Y += mouseEvent.Y - _OldMouseY;
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

                        if (!_ChangePlayerNumDynamic && _TeamList[_CurrentTeam].Count == _NumPlayerTeams[_CurrentTeam])
                            stopSelectingFast = true;
                        else if (_TeamList[_CurrentTeam].Count == _PartyMode.MaxPlayersPerTeam)
                            stopSelectingFast = true;
                    }
                    else
                        stopSelectingFast = true;
                }
            }

            else if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                if (_Buttons[_ButtonBack].Selected)
                    Back();

                if (_Buttons[_ButtonNext].Selected)
                    Next();

                if (_Buttons[_ButtonRandom].Selected)
                    _SelectRandom();

                if (_Buttons[_ButtonIncreaseTeams].Selected)
                    IncreaseTeamNum();

                if (_Buttons[_ButtonDecreaseTeams].Selected)
                    DecreaseTeamNum();

                if (_Buttons[_ButtonIncreasePlayer].Selected)
                    IncreasePlayerNum(_CurrentTeam);

                if (_Buttons[_ButtonDecreasePlayer].Selected)
                    DecreasePlayerNum(_CurrentTeam);

                if (_SelectSlides[_SelectSlideTeams].Selected)
                    _OnChangeTeamSlide();

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
                if (_SelectSlides[_SelectSlidePlayer].Selected && _TeamList[_CurrentTeam].Count > _SelectSlides[_SelectSlidePlayer].Selection)
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
                if (!_ChangePlayerNumDynamic && _TeamList[_CurrentTeam].Count == _NumPlayerTeams[_CurrentTeam])
                    stopSelectingFast = true;
                else if (_TeamList[_CurrentTeam].Count == _PartyMode.MaxPlayersPerTeam)
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
            _SelectSlides[_SelectSlidePlayer].DrawTextures = true;
            _SelectSlides[_SelectSlidePlayer].SelectByHovering = true;
            _AddStatic(_ChooseAvatarStatic);
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

            _UpdateButtonVisibility();
            _UpdateButtonState();
            _UpdateNextButtonVisibility();
        }

        public abstract void Back();
        public abstract void Next();

        public void IncreaseTeamNum()
        {
            if (!_AllowChangeTeamNum)
                return;

            if (_NumTeams + 1 <= _PartyMode.MaxTeams)
            {
                _NumTeams++;
                int[] numPlayerTeams = _NumPlayerTeams;
                List<int>[] teamList = _TeamList;
                _NumPlayerTeams = new int[_NumTeams];
                _TeamList = new List<int>[_NumTeams];
                for (int i = 0; i < _NumPlayerTeams.Length; i++)
                {
                    if (i < numPlayerTeams.Length)
                    {
                        _NumPlayerTeams[i] = numPlayerTeams[i];
                        _TeamList[i] = teamList[i];
                    }
                    else
                    {
                        _NumPlayerTeams[i] = _NumPlayer;
                        _TeamList[i] = new List<int>();
                    }
                }
            }
            _UpdateButtonState();
        }

        public void DecreaseTeamNum()
        {
            if (!_AllowChangeTeamNum)
                return;

            if (_NumTeams - 1 >= _PartyMode.MinTeams)
                _NumTeams--;
            _UpdateButtonState();
        }

        public void IncreasePlayerNum(int team)
        {
            if (!_AllowChangePlayerNum)
                return;
            if (_NumPlayerTeams.Length > team)
            {
                if (_NumPlayerTeams[team] + 1 <= _PartyMode.MaxPlayersPerTeam)
                    _NumPlayerTeams[team]++;
                _UpdatePlayerSlide();
            }
            _UpdateButtonState();
            _UpdateNextButtonVisibility();
        }

        public void DecreasePlayerNum(int team)
        {
            if (!_AllowChangePlayerNum)
                return;
            if (_NumPlayerTeams.Length > team)
            {
                if (_NumPlayerTeams[team] - 1 >= _PartyMode.MinPlayersPerTeam)
                    _NumPlayerTeams[team]--;
                if (_TeamList[team].Count > _NumPlayerTeams[team])
                    _RemovePlayerByIndex(team, _NumPlayerTeams[team] - 1);
                _UpdatePlayerSlide();
            }
            _UpdateButtonState();
            _UpdateNextButtonVisibility();
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
            _OnChangeTeamSlide();

            _ProfilesChanged = false;
            _AvatarsChanged = false;
        }

        private void _UpdateSlides()
        {
            _UpdateTeamSlide();
            _UpdatePlayerSlide();
        }

        private void _OnChangeTeamSlide()
        {
            if (_CurrentTeam == _SelectSlides[_SelectSlideTeams].Selection)
                return;

            _CurrentTeam = _SelectSlides[_SelectSlideTeams].Selection;
            _UpdatePlayerSlide();
            _UpdateButtonState();
        }

        private void _UpdatePlayerSlide()
        {
            int selection = _SelectSlides[_SelectSlidePlayer].Selection;
            _SelectSlides[_SelectSlidePlayer].Clear();
            for (int i = 0; i < _TeamList[_CurrentTeam].Count; i++)
            {
                string name = CBase.Profiles.GetPlayerName(_TeamList[_CurrentTeam][i]);
                CTextureRef avatar = CBase.Profiles.GetAvatar(_TeamList[_CurrentTeam][i]);
                _SelectSlides[_SelectSlidePlayer].AddValue(name, avatar);
            }
            for (int i = _TeamList[_CurrentTeam].Count; i < _NumPlayerTeams[_CurrentTeam]; i++)
                _SelectSlides[_SelectSlidePlayer].AddValue("", _NameSelections[_NameSelection].TextureEmptyTile);
            if (selection >= _TeamList[_CurrentTeam].Count)
                selection = _TeamList[_CurrentTeam].Count - 1;
            _SelectSlides[_SelectSlidePlayer].Selection = selection;
        }

        private void _UpdateTeamSlide()
        {
            _SelectSlides[_SelectSlideTeams].Visible = _Teams;
            _SelectSlides[_SelectSlideTeams].Clear();
            for (int i = 1; i <= _NumTeams; i++)
                _SelectSlides[_SelectSlideTeams].AddValue("Team " + i, null, i);
        }

        private void _UpdateButtonVisibility()
        {
            _Buttons[_ButtonIncreaseTeams].Visible = _AllowChangePlayerNum && _Teams && _PartyMode.MinTeams != _PartyMode.MaxTeams;
            _Buttons[_ButtonDecreaseTeams].Visible = _AllowChangePlayerNum && _Teams && _PartyMode.MinTeams != _PartyMode.MaxTeams;
            _Buttons[_ButtonIncreasePlayer].Visible = _AllowChangePlayerNum;
            _Buttons[_ButtonDecreasePlayer].Visible = _AllowChangePlayerNum;
        }

        private void _UpdateButtonState()
        {
            _Buttons[_ButtonIncreaseTeams].Selectable = _NumTeams < _PartyMode.MaxTeams;
            _Buttons[_ButtonDecreaseTeams].Selectable = _NumTeams > _PartyMode.MinTeams;
            if (_NumPlayerTeams != null && _NumPlayerTeams.Length > _CurrentTeam)
            {
                _Buttons[_ButtonIncreasePlayer].Selectable = _NumPlayerTeams[_CurrentTeam] < _PartyMode.MaxPlayersPerTeam;
                _Buttons[_ButtonDecreasePlayer].Selectable = _NumPlayerTeams[_CurrentTeam] > _PartyMode.MinPlayersPerTeam;
            }
            else
            {
                _Buttons[_ButtonIncreasePlayer].Selectable = false;
                _Buttons[_ButtonDecreasePlayer].Selectable = false;
            }
        }

        private void _AddPlayer(int team, int profileID)
        {
            if (_NumPlayerTeams[team] == _TeamList[team].Count && !_ChangePlayerNumDynamic)
                return;
            if (_NumPlayerTeams[team] > _PartyMode.MaxPlayersPerTeam)
                return;

            _NameSelections[_NameSelection].UseProfile(profileID);
            _TeamList[team].Add(profileID);

            _UpdatePlayerSlide();
            _UpdateNextButtonVisibility();
        }

        private void _RemoveAllPlayer()
        {
            for (int t = 0; t < _TeamList.Length; t++)
            {
                List<int> ids = new List<int>();
                ids.AddRange(_TeamList[t]);
                foreach (int id in ids)
                    _RemovePlayer(t, id);
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

            _UpdateNextButtonVisibility();
        }

        private void _RemovePlayer(int team, int profileID)
        {
            _TeamList[team].Remove(profileID);
            _NameSelections[_NameSelection].RemoveUsedProfile(profileID);

            _UpdateNextButtonVisibility();
        }

        private void _SelectRandom()
        {
            _RemoveAllPlayer();
            for (int t = 0; t < _NumPlayerTeams.Length; t++)
            {
                for (int p = 0; p < _NumPlayerTeams[t]; p++)
                {
                    int profileID = _NameSelections[_NameSelection].GetRandomUnusedProfile();
                    if(profileID >= 0) //only add valid profiles
                        _AddPlayer(t, profileID);
                }
            }
        }

        private void _UpdateNextButtonVisibility()
        {
            _Buttons[_ButtonNext].Visible = _AllPlayerSelected;
        }
        #endregion
    }
}