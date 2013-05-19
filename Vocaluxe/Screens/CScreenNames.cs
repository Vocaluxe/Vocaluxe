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
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Menu;
using VocaluxeLib.Profile;

namespace Vocaluxe.Screens
{
    class CScreenNames : CMenu
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
        private readonly CTexture[] _OriginalPlayerAvatarTextures = new CTexture[CSettings.MaxNumPlayer];

        private bool _SelectingKeyboardActive;
        private bool _SelectingFast;
        private int _SelectingSwitchNr = -1;
        private int _SelectingFastPlayerNr;
        private int _SelectedPlayerNr = -1;
        private bool _AvatarsChanged;
        private bool _ProfilesChanged;

        #region public methods
        public override void Init()
        {
            base.Init();

            List<string> statics = new List<string>();
            statics.AddRange(_StaticPlayerAvatar);
            statics.AddRange(_StaticPlayer);
            statics.Add(_StaticWarningMics);
            statics.Add(_StaticWarningProfiles);
            _ThemeStatics = statics.ToArray();

            List<string> texts = new List<string> {_SelectSlidePlayerNumber};
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

            for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
                _Equalizers["EqualizerPlayer" + i].ScreenHandles = true;
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            switch (keyEvent.Key)
            {
                case Keys.Add:
                    if (CConfig.NumPlayer + 1 <= CSettings.MaxNumPlayer)
                    {
                        _SelectSlides[_SelectSlidePlayerNumber].Selection = CConfig.NumPlayer;
                        _UpdatePlayerNumber();
                        //Update Tiles-List
                        _NameSelections[_NameSelection].UpdateList();
                    }
                    break;

                case Keys.Subtract:
                    if (CConfig.NumPlayer - 1 > 0)
                    {
                        _SelectSlides[_SelectSlidePlayerNumber].Selection = CConfig.NumPlayer - 2;
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
                        if (_SelectingFastPlayerNr + 1 <= CGame.NumPlayer)
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
                        if (_NameSelections[_NameSelection].Selection > -1)
                        {
                            CProfile[] profiles = CProfiles.GetProfiles();
                            _SelectedPlayerNr = _NameSelections[_NameSelection].Selection;
                            if (profiles.Length <= _SelectedPlayerNr)
                                return true;

                            //Update Game-infos with new player
                            CGame.Players[_SelectingFastPlayerNr - 1].ProfileID = profiles[_SelectedPlayerNr].ID;
                            //Update config for default players.
                            CConfig.Players[_SelectingFastPlayerNr - 1] = profiles[_SelectedPlayerNr].FileName;
                            CConfig.SaveConfig();
                            //Update texture and name
                            _Statics[_StaticPlayerAvatar[_SelectingFastPlayerNr - 1]].Texture = profiles[_SelectedPlayerNr].Avatar.Texture;
                            _Texts[_TextPlayer[_SelectingFastPlayerNr - 1]].Text = profiles[_SelectedPlayerNr].PlayerName;
                            //Update profile-warning
                            _CheckPlayers();
                            //Update Tiles-List
                            _NameSelections[_NameSelection].UpdateList();
                            _SetInteractionToButton(_Buttons[_ButtonStart]);
                        }
                        //Started selecting with 'P'
                        if (_SelectingFast)
                        {
                            if (_SelectingFastPlayerNr == CGame.NumPlayer)
                                resetSelection = true;
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
                        break;

                    case Keys.Delete:
                        //Delete profile-selection
                        CGame.Players[_SelectingFastPlayerNr - 1].ProfileID = -1;
                        //Update config for default players.
                        CConfig.Players[_SelectingFastPlayerNr - 1] = String.Empty;
                        CConfig.SaveConfig();
                        //Update texture and name
                        _Statics[_StaticPlayerAvatar[_SelectingFastPlayerNr - 1]].Texture = _OriginalPlayerAvatarTextures[_SelectingFastPlayerNr - 1];
                        _Texts[_TextPlayer[_SelectingFastPlayerNr - 1]].Text = CProfiles.GetPlayerName(-1, _SelectingFastPlayerNr);
                        //Update profile-warning
                        _CheckPlayers();
                        //Reset all values
                        _SelectingFastPlayerNr = 0;
                        _SelectingKeyboardActive = false;
                        _NameSelections[_NameSelection].FastSelection(false, -1);
                        //Update Tiles-List
                        _NameSelections[_NameSelection].UpdateList();
                        break;

                    case Keys.F10:
                        if (CGame.GetNumSongs() == 1 && CGame.GetSong(1).IsDuet)
                        {
                            CSelectSlide selectSlideDuetPart = _SelectSlides[_SelectSlideDuetPlayer[_SelectingFastPlayerNr - 1]];
                            selectSlideDuetPart.Selection = (selectSlideDuetPart.Selection + 1) % 2;
                            //Reset all values
                            _SelectingFastPlayerNr = 0;
                            _SelectingKeyboardActive = false;
                            _SelectingFast = false;
                            _NameSelections[_NameSelection].FastSelection(false, -1);
                            _SetInteractionToButton(_Buttons[_ButtonStart]);
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
                        _NameSelections[_NameSelection].FastSelection(false, -1);
                    }
                    else if (numberPressed <= CConfig.NumPlayer)
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
                        CGraphics.FadeTo(EScreens.ScreenSong);
                        break;

                    case Keys.Enter:

                        if (_Buttons[_ButtonBack].Selected)
                            CGraphics.FadeTo(EScreens.ScreenSong);
                        else
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


                if (_SelectingFastPlayerNr > 0 && _SelectingFastPlayerNr <= CConfig.NumPlayer)
                {
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
            if (mouseEvent.LBH && _SelectedPlayerNr < 0 && !_SelectingFast)
            {
                //Save mouse-coords
                _OldMouseX = mouseEvent.X;
                _OldMouseY = mouseEvent.Y;
                //Check if mouse if over tile
                if (_NameSelections[_NameSelection].IsOverTile(mouseEvent))
                {
                    //Get player-number of tile
                    _SelectedPlayerNr = _NameSelections[_NameSelection].TilePlayerNr(mouseEvent);
                    if (_SelectedPlayerNr != -1)
                    {
                        //Update of Drag/Drop-Texture
                        CStatic selectedPlayer = _NameSelections[_NameSelection].TilePlayerAvatar(mouseEvent);
                        _ChooseAvatarStatic.Visible = true;
                        _ChooseAvatarStatic.Rect = selectedPlayer.Rect;
                        _ChooseAvatarStatic.Rect.Z = CSettings.ZNear;
                        _ChooseAvatarStatic.Color = new SColorF(1, 1, 1, 1);
                        _ChooseAvatarStatic.Texture = selectedPlayer.Texture;
                    }
                }
                else
                {
                    for (int i = 0; i < CGame.NumPlayer; i++)
                    {
                        if (CHelper.IsInBounds(_Statics[_StaticPlayer[i]].Rect, mouseEvent))
                        {
                            _SelectingSwitchNr = i;
                            _SelectedPlayerNr = CGame.Players[i].ProfileID;
                            //Update of Drag/Drop-Texture
                            CStatic selectedPlayer = _Statics[_StaticPlayerAvatar[i]];
                            _ChooseAvatarStatic.Visible = true;
                            _ChooseAvatarStatic.Rect = selectedPlayer.Rect;
                            _ChooseAvatarStatic.Rect.Z = CSettings.ZNear;
                            _ChooseAvatarStatic.Color = new SColorF(1, 1, 1, 1);
                            _ChooseAvatarStatic.Texture = selectedPlayer.Texture;
                            break;
                        }
                    }
                }
            }

            //Check if LeftButton is hold and Select-Mode active
            if (mouseEvent.LBH && _SelectedPlayerNr >= 0 && !_SelectingFast)
            {
                //Update coords for Drag/Drop-Texture
                _ChooseAvatarStatic.Rect.X += mouseEvent.X - _OldMouseX;
                _ChooseAvatarStatic.Rect.Y += mouseEvent.Y - _OldMouseY;
                _OldMouseX = mouseEvent.X;
                _OldMouseY = mouseEvent.Y;
            }
                // LeftButton isn't hold anymore, but Select-Mode is still active -> "Drop" of Avatar
            else if (_SelectedPlayerNr >= 0 && !_SelectingFast)
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
                        if (_SelectingSwitchNr > -1 && CGame.Players[i].ProfileID > -1)
                        {
                            //Update Game-infos with new player
                            CGame.Players[_SelectingSwitchNr].ProfileID = CGame.Players[i].ProfileID;
                            //Update config for default players.
                            CConfig.Players[_SelectingSwitchNr] = CProfiles.GetProfileFileName(CGame.Players[i].ProfileID);
                            //Update texture and name
                            _Statics[_StaticPlayerAvatar[_SelectingSwitchNr]].Texture = CProfiles.GetAvatarTextureFromProfile(CGame.Players[i].ProfileID);
                            _Texts[_TextPlayer[_SelectingSwitchNr]].Text = CProfiles.GetPlayerName(CGame.Players[i].ProfileID);
                        }
                        else if (_SelectingSwitchNr > -1)
                        {
                            //Update Game-infos with new player
                            CGame.Players[_SelectingSwitchNr].ProfileID = -1;
                            //Update config for default players.
                            CConfig.Players[_SelectingSwitchNr] = string.Empty;
                            //Update texture and name
                            _Statics[_StaticPlayerAvatar[_SelectingSwitchNr]].Texture = _OriginalPlayerAvatarTextures[_SelectingSwitchNr];
                            _Texts[_TextPlayer[_SelectingSwitchNr]].Text = CProfiles.GetPlayerName(-1, (_SelectingSwitchNr + 1));
                        }

                        CProfile[] profiles = CProfiles.GetProfiles();
                        if (profiles.Length <= _SelectedPlayerNr)
                            return true;

                        //Update Game-infos with new player
                        CGame.Players[i].ProfileID = profiles[_SelectedPlayerNr].ID;
                        //Update config for default players.
                        CConfig.Players[i] = profiles[_SelectedPlayerNr].FileName;
                        CConfig.SaveConfig();
                        //Update texture and name
                        _Statics[_StaticPlayerAvatar[i]].Texture = _ChooseAvatarStatic.Texture;
                        _Texts[_TextPlayer[i]].Text = profiles[_SelectedPlayerNr].PlayerName;
                        //Update profile-warning
                        _CheckPlayers();
                        //Update Tiles-List
                        _NameSelections[_NameSelection].UpdateList();
                        break;
                    }
                    //Selected player is dropped out of area
                    if (_SelectingSwitchNr > -1)
                    {
                        //Update Game-infos with new player
                        CGame.Players[_SelectingSwitchNr].ProfileID = -1;
                        //Update config for default players.
                        CConfig.Players[_SelectingSwitchNr] = string.Empty;
                        //Update texture and name
                        _Statics[_StaticPlayerAvatar[_SelectingSwitchNr]].Texture = _OriginalPlayerAvatarTextures[_SelectingSwitchNr];
                        _Texts[_TextPlayer[_SelectingSwitchNr]].Text = CProfiles.GetPlayerName(-1, (_SelectingSwitchNr + 1));
                        _NameSelections[_NameSelection].UpdateList();
                    }
                }
                _SelectingSwitchNr = -1;
                _SelectedPlayerNr = -1;
                //Reset variables
                _ChooseAvatarStatic.Visible = false;
            }

            if (mouseEvent.LB && _SelectingFast)
            {
                if (_NameSelections[_NameSelection].IsOverTile(mouseEvent))
                {
                    //Get player-number of tile
                    _SelectedPlayerNr = _NameSelections[_NameSelection].TilePlayerNr(mouseEvent);
                    if (_SelectedPlayerNr != -1)
                    {
                        CProfile[] profiles = CProfiles.GetProfiles();
                        if (profiles.Length <= _SelectedPlayerNr)
                            return true;

                        //Update Game-infos with new player
                        CGame.Players[_SelectingFastPlayerNr - 1].ProfileID = profiles[_SelectedPlayerNr].ID;
                        //Update config for default players.
                        CConfig.Players[_SelectingFastPlayerNr - 1] = profiles[_SelectedPlayerNr].FileName;
                        CConfig.SaveConfig();
                        //Update texture and name
                        _Statics[_StaticPlayerAvatar[_SelectingFastPlayerNr - 1]].Texture = profiles[_SelectedPlayerNr].Avatar.Texture;
                        _Texts[_TextPlayer[_SelectingFastPlayerNr - 1]].Text = profiles[_SelectedPlayerNr].PlayerName;
                        //Update profile-warning
                        _CheckPlayers();
                        //Update Tiles-List
                        _NameSelections[_NameSelection].UpdateList();
                        _SelectingFastPlayerNr++;
                        if (_SelectingFastPlayerNr <= CGame.NumPlayer)
                            _NameSelections[_NameSelection].FastSelection(true, _SelectingFastPlayerNr);
                        else
                            stopSelectingFast = true;
                    }
                    else
                        stopSelectingFast = true;
                }
            }
            else if (mouseEvent.LB && _IsMouseOver(mouseEvent))
            {
                if (_Buttons[_ButtonBack].Selected)
                    CGraphics.FadeTo(EScreens.ScreenSong);
                else if (_Buttons[_ButtonStart].Selected)
                    _StartSong();
                else
                    _UpdatePlayerNumber();
                //Update Tiles-List
                _NameSelections[_NameSelection].UpdateList();
            }

            if (mouseEvent.LD && _NameSelections[_NameSelection].IsOverTile(mouseEvent) && !_SelectingFast)
            {
                _SelectedPlayerNr = _NameSelections[_NameSelection].TilePlayerNr(mouseEvent);
                if (_SelectedPlayerNr > -1)
                {
                    for (int i = 0; i < CGame.NumPlayer; i++)
                    {
                        if (CGame.Players[i].ProfileID == -1)
                        {
                            CProfile[] profiles = CProfiles.GetProfiles();
                            if (profiles.Length <= _SelectedPlayerNr)
                                return true;

                            //Update Game-infos with new player
                            CGame.Players[i].ProfileID = profiles[_SelectedPlayerNr].ID;
                            //Update config for default players.
                            CConfig.Players[i] = profiles[_SelectedPlayerNr].FileName;
                            CConfig.SaveConfig();
                            //Update texture and name
                            _Statics[_StaticPlayerAvatar[i]].Texture = profiles[_SelectedPlayerNr].Avatar.Texture;
                            _Texts[_TextPlayer[i]].Text = profiles[_SelectedPlayerNr].PlayerName;
                            //Update profile-warning
                            _CheckPlayers();
                            //Update Tiles-List
                            _NameSelections[_NameSelection].UpdateList();
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
                for (int i = 0; i < CConfig.NumPlayer; i++)
                {
                    if (CHelper.IsInBounds(_Statics[_StaticPlayer[i]].Rect, mouseEvent))
                    {
                        CGame.Players[i].ProfileID = -1;
                        //Update config for default players.
                        CConfig.Players[i] = String.Empty;
                        CConfig.SaveConfig();
                        //Update texture and name
                        _Statics[_StaticPlayerAvatar[i]].Texture = _OriginalPlayerAvatarTextures[i];
                        _Texts[_TextPlayer[i]].Text = CProfiles.GetPlayerName(-1, i + 1);
                        //Update profile-warning
                        _CheckPlayers();
                        //Update Tiles-List
                        _NameSelections[_NameSelection].UpdateList();
                        exit = false;
                    }
                }
                if (exit)
                    CGraphics.FadeTo(EScreens.ScreenSong);
            }

            if (mouseEvent.MB && _SelectingFast)
            {
                _SelectingFastPlayerNr++;
                if (_SelectingFastPlayerNr <= CGame.NumPlayer)
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

            for (int i = 1; i <= CGame.NumPlayer; i++)
            {
                CSound.AnalyzeBuffer(i - 1);
                _Equalizers["EqualizerPlayer" + i].Update(CSound.ToneWeigth(i - 1));
            }
            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
            CSound.RecordStart();

            _NameSelections[_NameSelection].Init();
            _LoadProfiles();
            _SetInteractionToButton(_Buttons[_ButtonStart]);
        }

        public override void OnClose()
        {
            base.OnClose();
            CSound.RecordStop();
        }

        public override bool Draw()
        {
            base.Draw();

            if (_ChooseAvatarStatic.Visible)
                _ChooseAvatarStatic.Draw();
            for (int i = 1; i <= CGame.NumPlayer; i++)
                _Equalizers["EqualizerPlayer" + i].Draw();
            return true;
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

            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
            {
                _Statics[_StaticPlayerAvatar[i]].Texture = CProfiles.IsProfileIDValid(CGame.Players[i].ProfileID) ?
                                                               CProfiles.GetAvatarTextureFromProfile(CGame.Players[i].ProfileID) :
                                                               _OriginalPlayerAvatarTextures[i];
                _Texts[_TextPlayer[i]].Text = CProfiles.GetPlayerName(CGame.Players[i].ProfileID, i + 1);
                if (CGame.GetNumSongs() == 1 && CGame.GetSong(1).IsDuet)
                {
                    _SelectSlides[_SelectSlideDuetPlayer[i]].Clear();
                    _SelectSlides[_SelectSlideDuetPlayer[i]].Visible = i + 1 <= CGame.NumPlayer;
                    _SelectSlides[_SelectSlideDuetPlayer[i]].AddValue(CGame.GetSong(1).DuetPart1);
                    _SelectSlides[_SelectSlideDuetPlayer[i]].AddValue(CGame.GetSong(1).DuetPart2);
                    _SelectSlides[_SelectSlideDuetPlayer[i]].Selection = i % 2;
                }
                else
                    _SelectSlides[_SelectSlideDuetPlayer[i]].Visible = false;
            }

            _ProfilesChanged = false;
            _AvatarsChanged = false;
        }

        private void _StartSong()
        {
            for (int i = 0; i < CGame.NumPlayer; i++)
            {
                if (CGame.GetNumSongs() == 1 && CGame.GetSong(1).IsDuet)
                    CGame.Players[i].LineNr = _SelectSlides[_SelectSlideDuetPlayer[i]].Selection;
            }

            CGraphics.FadeTo(EScreens.ScreenSing);
        }

        private void _UpdateSlides()
        {
            _SelectSlides[_SelectSlidePlayerNumber].Clear();
            for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
                _SelectSlides[_SelectSlidePlayerNumber].AddValue(CLanguage.Translate("TR_SCREENNAMES_" + i + "PLAYER"));
            _SelectSlides[_SelectSlidePlayerNumber].Selection = CConfig.NumPlayer - 1;
        }

        private void _UpdatePlayerNumber()
        {
            CConfig.NumPlayer = _SelectSlides[_SelectSlidePlayerNumber].Selection + 1;
            CGame.NumPlayer = _SelectSlides[_SelectSlidePlayerNumber].Selection + 1;
            for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
            {
                if (i <= CGame.NumPlayer)
                {
                    _Statics["StaticPlayer" + i].Visible = true;
                    _Statics["StaticPlayerAvatar" + i].Visible = true;
                    _Texts["TextPlayer" + i].Visible = true;
                    if (_Texts["TextPlayer" + i].Text == "")
                        _Texts["TextPlayer" + i].Text = CProfiles.GetPlayerName(-1, i);
                    _Equalizers["EqualizerPlayer" + i].Visible = true;
                    if (CGame.GetNumSongs() == 1 && CGame.GetSong(1).IsDuet)
                        _SelectSlides["SelectSlideDuetPlayer" + i].Visible = true;
                }
                else
                {
                    _Statics["StaticPlayer" + i].Visible = false;
                    _Statics["StaticPlayerAvatar" + i].Visible = false;
                    _Texts["TextPlayer" + i].Visible = false;
                    _Equalizers["EqualizerPlayer" + i].Visible = false;
                    _SelectSlides["SelectSlideDuetPlayer" + i].Visible = false;
                }
            }
            CConfig.SaveConfig();
            _CheckMics();
            _CheckPlayers();
        }

        private void _ResetPlayerSelections()
        {
            for (int i = 0; i < CGame.NumPlayer; i++)
            {
                CGame.Players[i].ProfileID = -1;
                //Update config for default players.
                CConfig.Players[i] = String.Empty;
                //Update texture and name
                _Statics[_StaticPlayerAvatar[i]].Texture = _OriginalPlayerAvatarTextures[i];
                _Texts[_TextPlayer[i]].Text = CProfiles.GetPlayerName(-1, i + 1);
            }
            _NameSelections[_NameSelection].UpdateList();
            CConfig.SaveConfig();
        }

        private void _CheckMics()
        {
            List<int> playerWithoutMicro = new List<int>();
            for (int player = 0; player < CConfig.NumPlayer; player++)
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
            List<int> playerWithoutProfile = new List<int>();
            for (int player = 0; player < CConfig.NumPlayer; player++)
            {
                if (CGame.Players[player].ProfileID < 0)
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