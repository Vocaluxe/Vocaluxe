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
using VocaluxeLib.Menu;

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
        private readonly STexture[] _OriginalPlayerAvatarTextures = new STexture[CSettings.MaxNumPlayer];

        private bool _SelectingKeyboardActive;
        private bool _SelectingKeyboardUnendless;
        private int _SelectingKeyboardPlayerNr;
        private int _SelectedPlayerNr;

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
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
                _OriginalPlayerAvatarTextures[i] = _Statics[_StaticPlayerAvatar[i]].Texture;

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
                        _SelectingKeyboardPlayerNr = 1;
                        _SelectingKeyboardUnendless = true;
                    }
                    else
                    {
                        if (_SelectingKeyboardPlayerNr + 1 <= CGame.NumPlayer)
                            _SelectingKeyboardPlayerNr++;
                        else
                            _SelectingKeyboardPlayerNr = 1;
                        _NameSelections[_NameSelection].KeyboardSelection(true, _SelectingKeyboardPlayerNr);
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
                            _SelectedPlayerNr = _NameSelections[_NameSelection].Selection;
                            //Update Game-infos with new player
                            CGame.Players[_SelectingKeyboardPlayerNr - 1].ProfileID = _SelectedPlayerNr;
                            //Update config for default players.
                            CConfig.Players[_SelectingKeyboardPlayerNr - 1] = CProfiles.Profiles[_SelectedPlayerNr].ProfileFile;
                            CConfig.SaveConfig();
                            //Update texture and name
                            _Statics[_StaticPlayerAvatar[_SelectingKeyboardPlayerNr - 1]].Texture = CProfiles.Profiles[_SelectedPlayerNr].Avatar.Texture;
                            _Texts[_TextPlayer[_SelectingKeyboardPlayerNr - 1]].Text = CProfiles.Profiles[_SelectedPlayerNr].PlayerName;
                            //Update profile-warning
                            _CheckPlayers();
                            //Update Tiles-List
                            _NameSelections[_NameSelection].UpdateList();
                            _SetInteractionToButton(_Buttons[_ButtonStart]);
                        }
                        //Started selecting with 'P'
                        if (_SelectingKeyboardUnendless)
                        {
                            if (_SelectingKeyboardPlayerNr == CGame.NumPlayer)
                                resetSelection = true;
                            else
                            {
                                _SelectingKeyboardPlayerNr++;
                                _NameSelections[_NameSelection].KeyboardSelection(true, _SelectingKeyboardPlayerNr);
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
                        CGame.Players[_SelectingKeyboardPlayerNr - 1].ProfileID = -1;
                        //Update config for default players.
                        CConfig.Players[_SelectingKeyboardPlayerNr - 1] = String.Empty;
                        CConfig.SaveConfig();
                        //Update texture and name
                        _Statics[_StaticPlayerAvatar[_SelectingKeyboardPlayerNr - 1]].Texture = _OriginalPlayerAvatarTextures[_SelectingKeyboardPlayerNr - 1];
                        _Texts[_TextPlayer[_SelectingKeyboardPlayerNr - 1]].Text = CProfiles.GetPlayerName(-1, _SelectingKeyboardPlayerNr);
                        //Update profile-warning
                        _CheckPlayers();
                        //Reset all values
                        _SelectingKeyboardPlayerNr = 0;
                        _SelectingKeyboardActive = false;
                        _NameSelections[_NameSelection].KeyboardSelection(false, -1);
                        //Update Tiles-List
                        _NameSelections[_NameSelection].UpdateList();
                        break;

                    case Keys.F10:
                        if (CGame.GetNumSongs() == 1 && CGame.GetSong(1).IsDuet)
                        {
                            CSelectSlide selectSlideDuetPart = _SelectSlides[_SelectSlideDuetPlayer[_SelectingKeyboardPlayerNr - 1]];
                            selectSlideDuetPart.Selection = (selectSlideDuetPart.Selection + 1) % 2;
                            //Reset all values
                            _SelectingKeyboardPlayerNr = 0;
                            _SelectingKeyboardActive = false;
                            _SelectingKeyboardUnendless = false;
                            _NameSelections[_NameSelection].KeyboardSelection(false, -1);
                            _SetInteractionToButton(_Buttons[_ButtonStart]);
                        }
                        break;
                }
                if (numberPressed > 0 || resetSelection)
                {
                    if (numberPressed == _SelectingKeyboardPlayerNr || resetSelection)
                    {
                        //Reset all values
                        _SelectingKeyboardPlayerNr = 0;
                        _SelectingKeyboardActive = false;
                        _NameSelections[_NameSelection].KeyboardSelection(false, -1);
                    }
                    else if (numberPressed <= CConfig.NumPlayer)
                    {
                        _SelectingKeyboardPlayerNr = numberPressed;
                        _NameSelections[_NameSelection].KeyboardSelection(true, numberPressed);
                    }
                    _SelectingKeyboardUnendless = false;
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
                        _SelectingKeyboardPlayerNr = 1;
                        break;

                    case Keys.D2:
                    case Keys.NumPad2:
                        _SelectingKeyboardPlayerNr = 2;
                        break;

                    case Keys.D3:
                    case Keys.NumPad3:
                        _SelectingKeyboardPlayerNr = 3;
                        break;

                    case Keys.D4:
                    case Keys.NumPad4:
                        _SelectingKeyboardPlayerNr = 4;
                        break;

                    case Keys.D5:
                    case Keys.NumPad5:
                        _SelectingKeyboardPlayerNr = 5;
                        break;

                    case Keys.D6:
                    case Keys.NumPad6:
                        _SelectingKeyboardPlayerNr = 6;
                        break;
                    default:
                        _UpdatePlayerNumber();
                        break;
                }


                if (_SelectingKeyboardPlayerNr > 0 && _SelectingKeyboardPlayerNr <= CConfig.NumPlayer)
                {
                    _SelectingKeyboardActive = true;
                    _NameSelections[_NameSelection].KeyboardSelection(true, _SelectingKeyboardPlayerNr);
                }
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            //Check if LeftButton is hold and Select-Mode inactive
            if (mouseEvent.LBH && _SelectedPlayerNr < 0)
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
            }

            //Check if LeftButton is hold and Select-Mode active
            if (mouseEvent.LBH && _SelectedPlayerNr >= 0)
            {
                //Update coords for Drag/Drop-Texture
                _ChooseAvatarStatic.Rect.X += mouseEvent.X - _OldMouseX;
                _ChooseAvatarStatic.Rect.Y += mouseEvent.Y - _OldMouseY;
                _OldMouseX = mouseEvent.X;
                _OldMouseY = mouseEvent.Y;
            }
                // LeftButton isn't hold anymore, but Selec-Mode is still active -> "Drop" of Avatar
            else if (_SelectedPlayerNr >= 0)
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
                        //Update Game-infos with new player
                        CGame.Players[i].ProfileID = _SelectedPlayerNr;
                        //Update config for default players.
                        CConfig.Players[i] = CProfiles.Profiles[_SelectedPlayerNr].ProfileFile;
                        CConfig.SaveConfig();
                        //Update texture and name
                        _Statics[_StaticPlayerAvatar[i]].Texture = _ChooseAvatarStatic.Texture;
                        _Texts[_TextPlayer[i]].Text = CProfiles.Profiles[_SelectedPlayerNr].PlayerName;
                        //Update profile-warning
                        _CheckPlayers();
                        //Update Tiles-List
                        _NameSelections[_NameSelection].UpdateList();
                    }
                }
                _SelectedPlayerNr = -1;
                //Reset variables
                _ChooseAvatarStatic.Visible = false;
            }

            if (mouseEvent.LB && _IsMouseOver(mouseEvent))
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

            if (mouseEvent.RB)
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

            //Check mouse-wheel for scrolling
            if (mouseEvent.Wheel != 0)
            {
                if (CHelper.IsInBounds(_NameSelections[_NameSelection].Rect, mouseEvent))
                {
                    int offset = _NameSelections[_NameSelection].Offset + mouseEvent.Wheel;
                    _NameSelections[_NameSelection].UpdateList(offset);
                }
            }
            return true;
        }

        public override bool UpdateGame()
        {
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

            _UpdateSlides();
            _UpdatePlayerNumber();
            _CheckMics();
            _CheckPlayers();

            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
            {
                _Statics[_StaticPlayerAvatar[i]].Texture = CProfiles.IsProfileIDValid(CGame.Players[i].ProfileID) ?
                                                               CProfiles.Profiles[CGame.Players[i].ProfileID].Avatar.Texture :
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
    }
}