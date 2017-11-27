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
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;
using VocaluxeLib.Profile;

namespace Vocaluxe.Screens
{
    public class CScreenNames : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 3; }
        }

        private CStatic _ChooseAvatarStatic;
        private int _OldMouseX;
        private int _OldMouseY;

        private const string _SelectSlidePlayerNumber = "SelectSlidePlayerNumber";
        private const string _NameSelection = "NameSelection";
        private const string _ButtonBack = "ButtonBack";
        private const string _ButtonStart = "ButtonStart";
        private const string _TextWarningMics = "TextWarningMics";
        private const string _StaticWarningMics = "StaticWarningMics";
        private const string _TextWarningProfiles = "TextWarningProfiles";
        private const string _StaticWarningProfiles = "StaticWarningProfiles";
        private readonly string[] _StaticPlayer = new string[] {"StaticPlayer1", "StaticPlayer2", "StaticPlayer3", "StaticPlayer4", "StaticPlayer5", "StaticPlayer6"};
        private readonly string[] _StaticPlayerAvatar = new string[]
            {"StaticPlayerAvatar1", "StaticPlayerAvatar2", "StaticPlayerAvatar3", "StaticPlayerAvatar4", "StaticPlayerAvatar5", "StaticPlayerAvatar6"};
        private readonly string[] _TextPlayer = new string[] {"TextPlayer1", "TextPlayer2", "TextPlayer3", "TextPlayer4", "TextPlayer5", "TextPlayer6"};
        private readonly string[] _EqualizerPlayer = new string[]
            {"EqualizerPlayer1", "EqualizerPlayer2", "EqualizerPlayer3", "EqualizerPlayer4", "EqualizerPlayer5", "EqualizerPlayer6"};
        private readonly string[] _SelectSlideDuetPlayer = new string[]
            {"SelectSlideDuetPlayer1", "SelectSlideDuetPlayer2", "SelectSlideDuetPlayer3", "SelectSlideDuetPlayer4", "SelectSlideDuetPlayer5", "SelectSlideDuetPlayer6"};
        private readonly CTextureRef[] _OriginalPlayerAvatarTextures = new CTextureRef[CSettings.MaxNumPlayer];

        private bool _SelectingKeyboardActive;
        private bool _SelectingFast;
        private int _SelectingSwitchNr = -1;
        private int _SelectingFastPlayerNr;
        private Guid _SelectedProfileID = Guid.Empty;
        private bool _AvatarsChanged;
        private bool _ProfilesChanged;

        public override EMusicType CurrentMusicType
        {
            get { return EMusicType.BackgroundPreview; }
        }

        #region public methods
        public override void Init()
        {
            base.Init();

            var statics = new List<string>();
            statics.AddRange(_StaticPlayerAvatar);
            statics.AddRange(_StaticPlayer);
            statics.Add(_StaticWarningMics);
            statics.Add(_StaticWarningProfiles);
            _ThemeStatics = statics.ToArray();

            var texts = new List<string> {_SelectSlidePlayerNumber};
            texts.AddRange(_SelectSlideDuetPlayer);
            _ThemeSelectSlides = texts.ToArray();

            texts.Clear();
            texts.Add(_TextWarningMics);
            texts.Add(_TextWarningProfiles);
            texts.AddRange(_TextPlayer);
            _ThemeTexts = texts.ToArray();

            texts.Clear();
            texts.Add(_ButtonBack);
            texts.Add(_ButtonStart);

            _ThemeButtons = texts.ToArray();

            texts.Clear();
            texts.Add(_NameSelection);
            _ThemeNameSelections = texts.ToArray();

            texts.Clear();
            texts.AddRange(_EqualizerPlayer);
            _ThemeEqualizers = texts.ToArray();

            _ChooseAvatarStatic = GetNewStatic();
            _ChooseAvatarStatic.Visible = false;
            _ChooseAvatarStatic.Aspect = EAspect.Crop;

            CProfiles.AddProfileChangedCallback(_OnProfileChanged);
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
            {
                _OriginalPlayerAvatarTextures[i] = _Statics[_StaticPlayerAvatar[i]].Texture;
                _Statics[_StaticPlayerAvatar[i]].Aspect = EAspect.Crop;
            }
            _AddStatic(_ChooseAvatarStatic);
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            switch (keyEvent.Key)
            {
                case Keys.Add:
                    if (CConfig.Config.Game.NumPlayers + 1 <= CSettings.MaxNumPlayer)
                    {
                        _SelectSlides[_SelectSlidePlayerNumber].Selection = CConfig.Config.Game.NumPlayers;
                        _UpdatePlayerNumber();
                        //Update Tiles-List
                        _NameSelections[_NameSelection].UpdateList();
                    }
                    break;

                case Keys.Subtract:
                    if (CConfig.Config.Game.NumPlayers - 1 > 0)
                    {
                        _SelectSlides[_SelectSlidePlayerNumber].Selection = CConfig.Config.Game.NumPlayers - 2;
                        _UpdatePlayerNumber();
                        //Update Tiles-List
                        _NameSelections[_NameSelection].UpdateList();
                    }
                    break;

                case Keys.P:
                    if (!_SelectingKeyboardActive)
                    {
                        _SelectingFastPlayerNr = 1;
                        _SelectingFast = true;
                        _ResetPlayerSelections();
                    }
                    else
                    {
                        if (_SelectingFastPlayerNr + 1 <= CGame.NumPlayers)
                            _SelectingFastPlayerNr++;
                        else
                            _SelectingFastPlayerNr = 1;
                        _NameSelections[_NameSelection].FastSelection(true, _SelectingFastPlayerNr);
                    }
                    break;
            }
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
                        if (_NameSelections[_NameSelection].SelectedID != Guid.Empty)
                        {
                            _SelectedProfileID = _NameSelections[_NameSelection].SelectedID;

                            if (!CProfiles.IsProfileIDValid(_SelectedProfileID))
                                return true;

                            _UpdateSelectedProfile(_SelectingFastPlayerNr - 1, _SelectedProfileID);
                        }
                        //Started selecting with 'P'
                        if (_SelectingFast)
                        {
                            if (_SelectingFastPlayerNr == CGame.NumPlayers)
                            {
                                resetSelection = true;
                                _SelectElement(_Buttons[_ButtonStart]);
                            }
                            else
                            {
                                _SelectingFastPlayerNr++;
                                _NameSelections[_NameSelection].FastSelection(true, _SelectingFastPlayerNr);
                            }
                        }
                        else
                            resetSelection = true;
                        break;

                    case Keys.D1:
                    case Keys.NumPad1:
                        numberPressed = 1;
                        break;
                    case Keys.D2:
                    case Keys.NumPad2:
                        numberPressed = 2;
                        break;
                    case Keys.D3:
                    case Keys.NumPad3:
                        numberPressed = 3;
                        break;
                    case Keys.D4:
                    case Keys.NumPad4:
                        numberPressed = 4;
                        break;
                    case Keys.D5:
                    case Keys.NumPad5:
                        numberPressed = 5;
                        break;
                    case Keys.D6:
                    case Keys.NumPad6:
                        numberPressed = 6;
                        break;

                    case Keys.Escape:
                        resetSelection = true;
                        _SelectElement(_SelectSlides[_SelectSlidePlayerNumber]);
                        break;

                    case Keys.Delete:
                        //Delete profile-selection
                        _ResetPlayerSelection(_SelectingFastPlayerNr - 1);
                        //Reset all values
                        _SelectingFastPlayerNr = 0;
                        _SelectingKeyboardActive = false;
                        _NameSelections[_NameSelection].FastSelection(false, -1);
                        //Update Tiles-List
                        _NameSelections[_NameSelection].UpdateList();
                        break;

                    case Keys.F10:
                        if (CGame.GetNumSongs() == 1 && CGame.GetSong(0).IsDuet)
                        {
                            CSelectSlide selectSlideDuetPart = _SelectSlides[_SelectSlideDuetPlayer[_SelectingFastPlayerNr - 1]];
                            selectSlideDuetPart.Selection = (selectSlideDuetPart.Selection + 1) % 2;
                            //Reset all values
                            _SelectingFastPlayerNr = 0;
                            _SelectingKeyboardActive = false;
                            _SelectingFast = false;
                            _NameSelections[_NameSelection].FastSelection(false, -1);
                            _SelectElement(_Buttons[_ButtonStart]);
                        }
                        break;
                }
                if (numberPressed > 0 || resetSelection)
                {
                    if (numberPressed == _SelectingFastPlayerNr || resetSelection)
                    {
                        //Reset all values
                        _SelectingFastPlayerNr = 0;
                        _SelectingKeyboardActive = false;
                        _SelectElement(_SelectSlides[_SelectSlidePlayerNumber]);
                        _NameSelections[_NameSelection].FastSelection(false, -1);
                    }
                    else if (numberPressed <= CConfig.Config.Game.NumPlayers)
                    {
                        _SelectingFastPlayerNr = numberPressed;
                        _NameSelections[_NameSelection].FastSelection(true, numberPressed);
                    }
                    _SelectingFast = false;
                }
            }
                //Normal Keyboard handling
            else
            {
                base.HandleInput(keyEvent);
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                        CGraphics.FadeTo(EScreen.Song);
                        break;

                    case Keys.Enter:

                        if (_Buttons[_ButtonBack].Selected)
                            CGraphics.FadeTo(EScreen.Song);
                        else if (_Buttons[_ButtonStart].Selected)
                            _StartSong();

                        break;

                    case Keys.D1:
                    case Keys.NumPad1:
                        _SelectingFastPlayerNr = 1;
                        break;

                    case Keys.D2:
                    case Keys.NumPad2:
                        _SelectingFastPlayerNr = 2;
                        break;

                    case Keys.D3:
                    case Keys.NumPad3:
                        _SelectingFastPlayerNr = 3;
                        break;

                    case Keys.D4:
                    case Keys.NumPad4:
                        _SelectingFastPlayerNr = 4;
                        break;

                    case Keys.D5:
                    case Keys.NumPad5:
                        _SelectingFastPlayerNr = 5;
                        break;

                    case Keys.D6:
                    case Keys.NumPad6:
                        _SelectingFastPlayerNr = 6;
                        break;
                    default:
                        _UpdatePlayerNumber();
                        break;
                }

                if (_SelectingFastPlayerNr > 0 && _SelectingFastPlayerNr <= CConfig.Config.Game.NumPlayers)
                {
                    _SelectingKeyboardActive = true;
                    _NameSelections[_NameSelection].FastSelection(true, _SelectingFastPlayerNr);
                }
                if (_NameSelections[_NameSelection].Selected && !_SelectingKeyboardActive)
                {
                    _SelectingKeyboardActive = true;
                    _SelectingFast = true;
                    _SelectingFastPlayerNr = 1;
                    _SelectingKeyboardActive = true;
                    _NameSelections[_NameSelection].FastSelection(true, _SelectingFastPlayerNr);
                }
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
            if (mouseEvent.LBH && _SelectedProfileID == Guid.Empty && !_SelectingFast)
            {
                //Save mouse-coords
                _OldMouseX = mouseEvent.X;
                _OldMouseY = mouseEvent.Y;
                //Check if mouse if over tile
                if (_NameSelections[_NameSelection].IsOverTile(mouseEvent))
                {
                    //Get player-number of tile
                    _SelectedProfileID = _NameSelections[_NameSelection].TilePlayerID(mouseEvent);
                    if (_SelectedProfileID != Guid.Empty)
                    {
                        //Update of Drag/Drop-Texture
                        CStatic selectedPlayer = _NameSelections[_NameSelection].TilePlayerAvatar(mouseEvent);
                        _ChooseAvatarStatic.Visible = true;
                        _ChooseAvatarStatic.MaxRect = selectedPlayer.Rect;
                        _ChooseAvatarStatic.Z = CSettings.ZNear;
                        _ChooseAvatarStatic.Color = new SColorF(1, 1, 1, 1);
                        _ChooseAvatarStatic.Texture = selectedPlayer.Texture;
                    }
                }
                else
                {
                    for (int i = 0; i < CGame.NumPlayers; i++)
                    {
                        if (CHelper.IsInBounds(_Statics[_StaticPlayer[i]].Rect, mouseEvent))
                        {
                            _SelectingSwitchNr = i;
                            _SelectedProfileID = CGame.Players[i].ProfileID;
                            //Update of Drag/Drop-Texture
                            CStatic selectedPlayer = _Statics[_StaticPlayerAvatar[i]];
                            _ChooseAvatarStatic.Visible = true;
                            _ChooseAvatarStatic.MaxRect = selectedPlayer.Rect;
                            _ChooseAvatarStatic.Z = CSettings.ZNear;
                            _ChooseAvatarStatic.Color = new SColorF(1, 1, 1, 1);
                            _ChooseAvatarStatic.Texture = selectedPlayer.Texture;
                            break;
                        }
                    }
                }
            }

            //Check if LeftButton is hold and Select-Mode active
            if (mouseEvent.LBH && _SelectedProfileID != Guid.Empty && !_SelectingFast)
            {
                //Update coords for Drag/Drop-Texture
                _ChooseAvatarStatic.X += mouseEvent.X - _OldMouseX;
                _ChooseAvatarStatic.Y += mouseEvent.Y - _OldMouseY;
                _OldMouseX = mouseEvent.X;
                _OldMouseY = mouseEvent.Y;
            }
                // LeftButton isn't hold anymore, but Select-Mode is still active -> "Drop" of Avatar
            else if (_SelectedProfileID != Guid.Empty && !_SelectingFast)
            {
                //Foreach Drop-Area
                for (int i = 0; i < _StaticPlayer.Length; i++)
                {
                    //Check first, if area is "Active"
                    if (!_Statics[_StaticPlayer[i]].Visible)
                        continue;
                    //Check if Mouse is in area
                    if (CHelper.IsInBounds(_Statics[_StaticPlayer[i]].Rect, mouseEvent))
                    {
                        if (_SelectingSwitchNr > -1 && CGame.Players[i].ProfileID != Guid.Empty)
                            _UpdateSelectedProfile(_SelectingSwitchNr, CGame.Players[i].ProfileID);
                        else if (_SelectingSwitchNr > -1)
                            _ResetPlayerSelection(_SelectingSwitchNr);

                        if (!CProfiles.IsProfileIDValid(_SelectedProfileID))
                            return true;

                        _UpdateSelectedProfile(i, _SelectedProfileID);
                        break;
                    }
                    //Selected player is dropped out of area
                    if (_SelectingSwitchNr > -1)
                        _ResetPlayerSelection(_SelectingSwitchNr);
                }
                _SelectingSwitchNr = -1;
                _SelectedProfileID = Guid.Empty;
                //Reset variables
                _ChooseAvatarStatic.Visible = false;
            }

            if (mouseEvent.LB && _SelectingFast)
            {
                if (_NameSelections[_NameSelection].IsOverTile(mouseEvent))
                {
                    //Get player-number of tile
                    _SelectedProfileID = _NameSelections[_NameSelection].TilePlayerID(mouseEvent);
                    if (_SelectedProfileID != Guid.Empty)
                    {
                        if (!CProfiles.IsProfileIDValid(_SelectedProfileID))
                            return true;

                        _UpdateSelectedProfile(_SelectingFastPlayerNr - 1, _SelectedProfileID);

                        _SelectingFastPlayerNr++;
                        if (_SelectingFastPlayerNr <= CGame.NumPlayers)
                            _NameSelections[_NameSelection].FastSelection(true, _SelectingFastPlayerNr);
                        else
                            stopSelectingFast = true;
                    }
                    else
                        stopSelectingFast = true;
                }
            }
            else if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                if (_Buttons[_ButtonBack].Selected)
                    CGraphics.FadeTo(EScreen.Song);
                else if (_Buttons[_ButtonStart].Selected)
                    _StartSong();
                else
                    _UpdatePlayerNumber();
                //Update Tiles-List
                _NameSelections[_NameSelection].UpdateList();
            }

            if (mouseEvent.LD && _NameSelections[_NameSelection].IsOverTile(mouseEvent) && !_SelectingFast)
            {
                _SelectedProfileID = _NameSelections[_NameSelection].TilePlayerID(mouseEvent);
                if (_SelectedProfileID != Guid.Empty)
                {
                    for (int i = 0; i < CGame.NumPlayers; i++)
                    {
                        if (CGame.Players[i].ProfileID == Guid.Empty)
                        {
                            if (!CProfiles.IsProfileIDValid(_SelectedProfileID))
                                return true;

                            _UpdateSelectedProfile(i, _SelectedProfileID);
                            break;
                        }
                    }
                }
            }

            if (mouseEvent.RB && _SelectingFast)
                stopSelectingFast = true;
            else if (mouseEvent.RB)
            {
                bool exit = true;
                //Remove profile-selection
                for (int i = 0; i < CConfig.Config.Game.NumPlayers; i++)
                {
                    if (CHelper.IsInBounds(_Statics[_StaticPlayer[i]].Rect, mouseEvent))
                    {
                        _ResetPlayerSelection(i);
                        exit = false;
                    }
                }
                if (exit)
                    CGraphics.FadeTo(EScreen.Song);
            }

            if (mouseEvent.MB && _SelectingFast)
            {
                _SelectingFastPlayerNr++;
                if (_SelectingFastPlayerNr <= CGame.NumPlayers)
                    _NameSelections[_NameSelection].FastSelection(true, _SelectingFastPlayerNr);
                else
                    stopSelectingFast = true;
            }
            else if (mouseEvent.MB)
            {
                _ResetPlayerSelections();
                _SelectingFast = true;
                _SelectingFastPlayerNr = 1;
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

        public override bool UpdateGame()
        {
            if (_ProfilesChanged || _AvatarsChanged)
                _LoadProfiles();

            for (int i = 1; i <= CGame.NumPlayers; i++)
            {
                CRecord.AnalyzeBuffer(i - 1);
                _Equalizers["EqualizerPlayer" + i].Update(CRecord.ToneWeigth(i - 1), CRecord.GetMaxVolume(i - 1));
            }
            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
            CRecord.Start();

            _NameSelections[_NameSelection].Init();
            _LoadProfiles();
            _SelectElement(_Buttons[_ButtonStart]);
        }

        public override void OnClose()
        {
            base.OnClose();
            CRecord.Stop();
        }
        #endregion public methods

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
            _UpdatePlayerNumber();
            _CheckMics();
            _CheckPlayers();

            CSong firstSong = CGame.GetSong(0);

            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
            {
                _NameSelections[_NameSelection].UseProfile(CGame.Players[i].ProfileID);
                _Statics[_StaticPlayerAvatar[i]].Texture = CProfiles.IsProfileIDValid(CGame.Players[i].ProfileID) ?
                                                               CProfiles.GetAvatarTextureFromProfile(CGame.Players[i].ProfileID) :
                                                               _OriginalPlayerAvatarTextures[i];
                _Texts[_TextPlayer[i]].Text = CProfiles.GetPlayerName(CGame.Players[i].ProfileID, i + 1);
                if (CGame.GetNumSongs() == 1 && firstSong.IsDuet)
                {
                    _SelectSlides[_SelectSlideDuetPlayer[i]].Clear();
                    _SelectSlides[_SelectSlideDuetPlayer[i]].Visible = i + 1 <= CGame.NumPlayers;

                    for (int j = 0; j < firstSong.Notes.VoiceCount; j++)
                        _SelectSlides[_SelectSlideDuetPlayer[i]].AddValue(firstSong.Notes.VoiceNames[j]);
                    _SelectSlides[_SelectSlideDuetPlayer[i]].Selection = i % 2;
                }
                else
                    _SelectSlides[_SelectSlideDuetPlayer[i]].Visible = false;
            }
            _NameSelections[_NameSelection].UpdateList();
            _ProfilesChanged = false;
            _AvatarsChanged = false;
        }

        private void _StartSong()
        {
            if (CGame.GetNumSongs() == 1 && CGame.GetSong(0).IsDuet)
            {
                for (int i = 0; i < CGame.NumPlayers; i++)
                    CGame.Players[i].VoiceNr = _SelectSlides[_SelectSlideDuetPlayer[i]].Selection;
            }
            CGraphics.FadeTo(EScreen.Sing);
        }

        private void _UpdateSlides()
        {
            _SelectSlides[_SelectSlidePlayerNumber].Clear();
            for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
                _SelectSlides[_SelectSlidePlayerNumber].AddValue(CLanguage.Translate("TR_SCREENNAMES_" + i + "PLAYER"));
            _SelectSlides[_SelectSlidePlayerNumber].Selection = CConfig.Config.Game.NumPlayers - 1;
        }

        private void _UpdatePlayerNumber()
        {
            CConfig.Config.Game.NumPlayers = _SelectSlides[_SelectSlidePlayerNumber].Selection + 1;
            CGame.NumPlayers = _SelectSlides[_SelectSlidePlayerNumber].Selection + 1;
            for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
            {
                if (i <= CGame.NumPlayers)
                {
                    _Statics["StaticPlayer" + i].Visible = true;
                    _Statics["StaticPlayerAvatar" + i].Visible = true;
                    _Texts["TextPlayer" + i].Visible = true;
                    if (_Texts["TextPlayer" + i].Text == "")
                        _Texts["TextPlayer" + i].Text = CProfiles.GetPlayerName(Guid.Empty, i);
                    _Equalizers["EqualizerPlayer" + i].Visible = true;
                    if (CGame.GetNumSongs() == 1 && CGame.GetSong(0).IsDuet)
                        _SelectSlides["SelectSlideDuetPlayer" + i].Visible = true;
                }
                else
                {
                    _Statics["StaticPlayer" + i].Visible = false;
                    _Statics["StaticPlayerAvatar" + i].Visible = false;
                    _Texts["TextPlayer" + i].Visible = false;
                    _Equalizers["EqualizerPlayer" + i].Visible = false;
                    _SelectSlides["SelectSlideDuetPlayer" + i].Visible = false;
                    _ResetPlayerSelection(i - 1);
                }
            }
            CConfig.SaveConfig();
            _CheckMics();
            _CheckPlayers();
        }

        private void _UpdateSelectedProfile(int playerNum, Guid profileId)
        {
            _NameSelections[_NameSelection].RemoveUsedProfile(CGame.Players[playerNum].ProfileID);
            _NameSelections[_NameSelection].UseProfile(profileId);
            //Update Game-infos with new player
            CGame.Players[playerNum].ProfileID = profileId;
            //Update config for default players.
            CConfig.Config.Game.Players[playerNum] = CProfiles.GetProfileFileName(profileId);
            CConfig.SaveConfig();
            //Update texture and name
            _Statics[_StaticPlayerAvatar[playerNum]].Texture = CProfiles.GetAvatarTextureFromProfile(profileId);
            _Texts[_TextPlayer[playerNum]].Text = CProfiles.GetPlayerName(profileId);
            //Update profile-warning
            _CheckPlayers();
            //Update Tiles-List
            _NameSelections[_NameSelection].UpdateList();
        }

        private void _ResetPlayerSelections()
        {
            for (int i = 0; i < CGame.NumPlayers; i++)
            {
                _NameSelections[_NameSelection].RemoveUsedProfile(CGame.Players[i].ProfileID);
                CGame.Players[i].ProfileID = Guid.Empty;
                //Update config for default players.
                CConfig.Config.Game.Players[i] = String.Empty;
                //Update texture and name
                _Statics[_StaticPlayerAvatar[i]].Texture = _OriginalPlayerAvatarTextures[i];
                _Texts[_TextPlayer[i]].Text = CProfiles.GetPlayerName(Guid.Empty, i + 1);
            }
            _NameSelections[_NameSelection].UpdateList();
            CConfig.SaveConfig();
        }

        private void _ResetPlayerSelection(int playerNum)
        {
            _NameSelections[_NameSelection].RemoveUsedProfile(CGame.Players[playerNum].ProfileID);
            CGame.Players[playerNum].ProfileID = Guid.Empty;
            //Update config for default players.
            CConfig.Config.Game.Players[playerNum] = String.Empty;
            CConfig.SaveConfig();
            //Update texture and name
            _Statics[_StaticPlayerAvatar[playerNum]].Texture = _OriginalPlayerAvatarTextures[playerNum];
            _Texts[_TextPlayer[playerNum]].Text = CProfiles.GetPlayerName(Guid.Empty, playerNum + 1);
            //Update profile-warning
            _CheckPlayers();
            //Update Tiles-List
            _NameSelections[_NameSelection].UpdateList();
        }

        private void _CheckMics()
        {
            var playerWithoutMicro = new List<int>();
            for (int player = 0; player < CConfig.Config.Game.NumPlayers; player++)
            {
                if (!CConfig.IsMicConfig(player + 1))
                    playerWithoutMicro.Add(player + 1);
            }
            if (playerWithoutMicro.Count > 0)
            {
                _Statics[_StaticWarningMics].Visible = true;
                _Texts[_TextWarningMics].Visible = true;

                if (playerWithoutMicro.Count > 1)
                {
                    string playerNums = string.Empty;
                    for (int i = 0; i < playerWithoutMicro.Count; i++)
                    {
                        if (playerWithoutMicro.Count - 1 == i)
                            playerNums += playerWithoutMicro[i].ToString();
                        else if (playerWithoutMicro.Count - 2 == i)
                            playerNums += playerWithoutMicro[i] + " " + CLanguage.Translate("TR_GENERAL_AND") + " ";
                        else
                            playerNums += playerWithoutMicro[i] + ", ";
                    }

                    _Texts[_TextWarningMics].Text = CLanguage.Translate("TR_SCREENNAMES_WARNING_MICS_PL").Replace("%v", playerNums);
                }
                else
                    _Texts[_TextWarningMics].Text = CLanguage.Translate("TR_SCREENNAMES_WARNING_MICS_SG").Replace("%v", playerWithoutMicro[0].ToString());
            }
            else
            {
                _Statics[_StaticWarningMics].Visible = false;
                _Texts[_TextWarningMics].Visible = false;
            }
        }

        private void _CheckPlayers()
        {
            var playerWithoutProfile = new List<int>();
            for (int player = 0; player < CConfig.Config.Game.NumPlayers; player++)
            {
                if (CGame.Players[player].ProfileID == Guid.Empty)
                    playerWithoutProfile.Add(player + 1);
            }

            if (playerWithoutProfile.Count > 0)
            {
                _Statics[_StaticWarningProfiles].Visible = true;
                _Texts[_TextWarningProfiles].Visible = true;

                if (playerWithoutProfile.Count > 1)
                {
                    string playerNums = string.Empty;
                    for (int i = 0; i < playerWithoutProfile.Count; i++)
                    {
                        if (playerWithoutProfile.Count - 1 == i)
                            playerNums += playerWithoutProfile[i].ToString();
                        else if (playerWithoutProfile.Count - 2 == i)
                            playerNums += playerWithoutProfile[i] + " " + CLanguage.Translate("TR_GENERAL_AND") + " ";
                        else
                            playerNums += playerWithoutProfile[i] + ", ";
                    }

                    _Texts[_TextWarningProfiles].Text = CLanguage.Translate("TR_SCREENNAMES_WARNING_PROFILES_PL").Replace("%v", playerNums);
                }
                else
                    _Texts[_TextWarningProfiles].Text = CLanguage.Translate("TR_SCREENNAMES_WARNING_PROFILES_SG").Replace("%v", playerWithoutProfile[0].ToString());
            }
            else
            {
                _Statics[_StaticWarningProfiles].Visible = false;
                _Texts[_TextWarningProfiles].Visible = false;
            }
        }
        #endregion private methods
    }
}