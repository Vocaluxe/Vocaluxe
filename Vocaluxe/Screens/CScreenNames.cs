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

        private bool _SelectingMouseActive;
        private bool _SelectingKeyboardActive;
        private bool _SelectingKeyboardUnendless;
        private int _SelectingKeyboardPlayerNr;
        private int _SelectedPlayerNr;

        private int[] _PlayerNr;

        public override void Init()
        {
            base.Init();

            List<string> statics = new List<string>();
            foreach (string text in _StaticPlayer)
                statics.Add(text);
            foreach (string text in _StaticPlayerAvatar)
                statics.Add(text);
            statics.Add(_StaticWarningMics);
            statics.Add(_StaticWarningProfiles);
            _ThemeStatics = statics.ToArray();

            List<string> texts = new List<string>();
            texts.Add(_SelectSlidePlayerNumber);
            texts.AddRange(_SelectSlideDuetPlayer);
            _ThemeSelectSlides = texts.ToArray();

            texts.Clear();
            texts.Add(_TextWarningMics);
            texts.Add(_TextWarningProfiles);
            foreach (string text in _TextPlayer)
                texts.Add(text);
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

            _PlayerNr = new int[CSettings.MaxNumPlayer];
            for (int i = 0; i < _PlayerNr.Length; i++)
                _PlayerNr[i] = i;

            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
                _OriginalPlayerAvatarTextures[i] = Statics[_StaticPlayerAvatar[i]].Texture;

            for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
                Equalizers["EqualizerPlayer" + i].ScreenHandles = true;
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            switch (keyEvent.Key)
            {
                case Keys.Add:
                    if (CConfig.NumPlayer + 1 <= CSettings.MaxNumPlayer)
                    {
                        SelectSlides[_SelectSlidePlayerNumber].Selection = CConfig.NumPlayer;
                        _UpdatePlayerNumber();
                        //Update Tiles-List
                        NameSelections[_NameSelection].UpdateList();
                    }
                    break;

                case Keys.Subtract:
                    if (CConfig.NumPlayer - 1 > 0)
                    {
                        SelectSlides[_SelectSlidePlayerNumber].Selection = CConfig.NumPlayer - 2;
                        _UpdatePlayerNumber();
                        //Update Tiles-List
                        NameSelections[_NameSelection].UpdateList();
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
                        NameSelections[_NameSelection].KeyboardSelection(true, _SelectingKeyboardPlayerNr);
                    }
                    break;
            }
            //Check if selecting with keyboard is active
            if (_SelectingKeyboardActive)
            {
                //Handle left/right/up/down
                NameSelections[_NameSelection].HandleInput(keyEvent);
                switch (keyEvent.Key)
                {
                    case Keys.Enter:
                        //Check, if a player is selected
                        if (NameSelections[_NameSelection].Selection > -1)
                        {
                            _SelectedPlayerNr = NameSelections[_NameSelection].Selection;
                            //Update Game-infos with new player
                            CGame.Player[_SelectingKeyboardPlayerNr - 1].Name = CProfiles.Profiles[_SelectedPlayerNr].PlayerName;
                            CGame.Player[_SelectingKeyboardPlayerNr - 1].Difficulty = CProfiles.Profiles[_SelectedPlayerNr].Difficulty;
                            CGame.Player[_SelectingKeyboardPlayerNr - 1].ProfileID = _SelectedPlayerNr;
                            //Update config for default players.
                            CConfig.Players[_SelectingKeyboardPlayerNr - 1] = CProfiles.Profiles[_SelectedPlayerNr].ProfileFile;
                            CConfig.SaveConfig();
                            //Update texture and name
                            Statics[_StaticPlayerAvatar[_SelectingKeyboardPlayerNr - 1]].Texture = CProfiles.Profiles[_SelectedPlayerNr].Avatar.Texture;
                            Texts[_TextPlayer[_SelectingKeyboardPlayerNr - 1]].Text = CProfiles.Profiles[_SelectedPlayerNr].PlayerName;
                            //Update profile-warning
                            _CheckPlayers();
                            //Update Tiles-List
                            NameSelections[_NameSelection].UpdateList();
                            SetInteractionToButton(Buttons[_ButtonStart]);
                        }
                        //Started selecting with 'P'
                        if (_SelectingKeyboardUnendless)
                        {
                            if (_SelectingKeyboardPlayerNr == CGame.NumPlayer)
                            {
                                //Reset all values
                                _SelectingKeyboardPlayerNr = 0;
                                _SelectingKeyboardActive = false;
                                NameSelections[_NameSelection].KeyboardSelection(false, -1);
                            }
                            else
                            {
                                _SelectingKeyboardPlayerNr++;
                                NameSelections[_NameSelection].KeyboardSelection(true, _SelectingKeyboardPlayerNr);
                            }
                        }
                        else
                        {
                            //Reset all values
                            _SelectingKeyboardPlayerNr = 0;
                            _SelectingKeyboardActive = false;
                            NameSelections[_NameSelection].KeyboardSelection(false, -1);
                        }
                        break;

                    case Keys.D1:
                    case Keys.NumPad1:
                        if (_SelectingKeyboardPlayerNr == 1)
                        {
                            //Reset all values
                            _SelectingKeyboardPlayerNr = 0;
                            _SelectingKeyboardActive = false;
                            NameSelections[_NameSelection].KeyboardSelection(false, -1);
                        }
                        else
                        {
                            _SelectingKeyboardPlayerNr = 1;
                            NameSelections[_NameSelection].KeyboardSelection(true, 1);
                        }
                        _SelectingKeyboardUnendless = false;
                        break;
                    case Keys.D2:
                    case Keys.NumPad2:
                        if (_SelectingKeyboardPlayerNr == 2)
                        {
                            //Reset all values
                            _SelectingKeyboardPlayerNr = 0;
                            _SelectingKeyboardActive = false;
                            NameSelections[_NameSelection].KeyboardSelection(false, -1);
                        }
                        else
                        {
                            _SelectingKeyboardPlayerNr = 2;
                            NameSelections[_NameSelection].KeyboardSelection(true, 2);
                        }
                        _SelectingKeyboardUnendless = false;
                        break;
                    case Keys.D3:
                    case Keys.NumPad3:
                        if (_SelectingKeyboardPlayerNr == 3)
                        {
                            //Reset all values
                            _SelectingKeyboardPlayerNr = 0;
                            _SelectingKeyboardActive = false;
                            NameSelections[_NameSelection].KeyboardSelection(false, -1);
                        }
                        else
                        {
                            _SelectingKeyboardPlayerNr = 3;
                            NameSelections[_NameSelection].KeyboardSelection(true, 3);
                        }
                        _SelectingKeyboardUnendless = false;
                        break;
                    case Keys.D4:
                    case Keys.NumPad4:
                        if (_SelectingKeyboardPlayerNr == 4)
                        {
                            //Reset all values
                            _SelectingKeyboardPlayerNr = 0;
                            _SelectingKeyboardActive = false;
                            NameSelections[_NameSelection].KeyboardSelection(false, -1);
                        }
                        else
                        {
                            _SelectingKeyboardPlayerNr = 4;
                            NameSelections[_NameSelection].KeyboardSelection(true, 4);
                        }
                        _SelectingKeyboardUnendless = false;
                        break;
                    case Keys.D5:
                    case Keys.NumPad5:
                        if (_SelectingKeyboardPlayerNr == 5)
                        {
                            //Reset all values
                            _SelectingKeyboardPlayerNr = 0;
                            _SelectingKeyboardActive = false;
                            NameSelections[_NameSelection].KeyboardSelection(false, -1);
                        }
                        else
                        {
                            _SelectingKeyboardPlayerNr = 5;
                            NameSelections[_NameSelection].KeyboardSelection(true, 5);
                        }
                        _SelectingKeyboardUnendless = false;
                        break;
                    case Keys.D6:
                    case Keys.NumPad6:
                        if (_SelectingKeyboardPlayerNr == 6)
                        {
                            //Reset all values
                            _SelectingKeyboardPlayerNr = 0;
                            _SelectingKeyboardActive = false;
                            NameSelections[_NameSelection].KeyboardSelection(false, -1);
                        }
                        else
                        {
                            _SelectingKeyboardPlayerNr = 6;
                            NameSelections[_NameSelection].KeyboardSelection(true, 6);
                        }
                        _SelectingKeyboardUnendless = false;
                        break;

                    case Keys.Escape:
                        //Reset all values
                        _SelectingKeyboardPlayerNr = 0;
                        _SelectingKeyboardActive = false;
                        _SelectingKeyboardUnendless = false;
                        NameSelections[_NameSelection].KeyboardSelection(false, -1);
                        break;

                    case Keys.Delete:
                        //Delete profile-selection
                        CGame.Player[_SelectingKeyboardPlayerNr - 1].ProfileID = -1;
                        CGame.Player[_SelectingKeyboardPlayerNr - 1].Name = String.Empty;
                        CGame.Player[_SelectingKeyboardPlayerNr - 1].Difficulty = EGameDifficulty.TR_CONFIG_EASY;
                        //Update config for default players.
                        CConfig.Players[_SelectingKeyboardPlayerNr - 1] = String.Empty;
                        CConfig.SaveConfig();
                        //Update texture and name
                        Statics[_StaticPlayerAvatar[_SelectingKeyboardPlayerNr - 1]].Texture = _OriginalPlayerAvatarTextures[_SelectingKeyboardPlayerNr - 1];
                        Texts[_TextPlayer[_SelectingKeyboardPlayerNr - 1]].Text = CLanguage.Translate("TR_SCREENNAMES_PLAYER") + " " + _SelectingKeyboardPlayerNr.ToString();
                        //Update profile-warning
                        _CheckPlayers();
                        //Reset all values
                        _SelectingKeyboardPlayerNr = 0;
                        _SelectingKeyboardActive = false;
                        NameSelections[_NameSelection].KeyboardSelection(false, -1);
                        //Update Tiles-List
                        NameSelections[_NameSelection].UpdateList();
                        break;

                    case Keys.F10:
                        if (CGame.GetNumSongs() == 1 && CGame.GetSong(1).IsDuet)
                        {
                            if (SelectSlides[_SelectSlideDuetPlayer[_SelectingKeyboardPlayerNr - 1]].Selection == 0)
                                SelectSlides[_SelectSlideDuetPlayer[_SelectingKeyboardPlayerNr - 1]].Selection = 1;
                            else
                                SelectSlides[_SelectSlideDuetPlayer[_SelectingKeyboardPlayerNr - 1]].Selection = 0;
                            //Reset all values
                            _SelectingKeyboardPlayerNr = 0;
                            _SelectingKeyboardActive = false;
                            _SelectingKeyboardUnendless = false;
                            NameSelections[_NameSelection].KeyboardSelection(false, -1);
                            SetInteractionToButton(Buttons[_ButtonStart]);
                        }
                        break;
                }
            }
                //Normal Keyboard handling
            else
            {
                base.HandleInput(keyEvent);
                bool processed = false;
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                        CGraphics.FadeTo(EScreens.ScreenSong);
                        break;

                    case Keys.Enter:

                        if (!processed && Buttons[_ButtonBack].Selected)
                        {
                            processed = true;
                            CGraphics.FadeTo(EScreens.ScreenSong);
                        }

                        if (!processed /* && Buttons[ButtonStart].Selected */)
                        {
                            processed = true;
                            _StartSong();
                        }

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
                }

                if (!processed)
                    _UpdatePlayerNumber();

                if (_SelectingKeyboardPlayerNr > 0 && _SelectingKeyboardPlayerNr <= CConfig.NumPlayer)
                {
                    _SelectingKeyboardActive = true;
                    NameSelections[_NameSelection].KeyboardSelection(true, _SelectingKeyboardPlayerNr);
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
                if (NameSelections[_NameSelection].IsOverTile(mouseEvent))
                {
                    //Get player-number of tile
                    _SelectedPlayerNr = NameSelections[_NameSelection].TilePlayerNr(mouseEvent);
                    if (_SelectedPlayerNr != -1)
                    {
                        //Activate mouse-selecting
                        _SelectingMouseActive = true;

                        //Update of Drag/Drop-Texture
                        CStatic selectedPlayer = NameSelections[_NameSelection].TilePlayerAvatar(mouseEvent);
                        _ChooseAvatarStatic.Visible = true;
                        _ChooseAvatarStatic.Rect = selectedPlayer.Rect;
                        _ChooseAvatarStatic.Rect.Z = CSettings.ZNear;
                        _ChooseAvatarStatic.Color = new SColorF(1, 1, 1, 1);
                        _ChooseAvatarStatic.Texture = selectedPlayer.Texture;
                    }
                }
            }

            //Check if LeftButton is hold and Select-Mode active
            if (mouseEvent.LBH && _SelectingMouseActive)
            {
                //Update coords for Drag/Drop-Texture
                _ChooseAvatarStatic.Rect.X += mouseEvent.X - _OldMouseX;
                _ChooseAvatarStatic.Rect.Y += mouseEvent.Y - _OldMouseY;
                _OldMouseX = mouseEvent.X;
                _OldMouseY = mouseEvent.Y;
            }
                // LeftButton isn't hold anymore, but Selec-Mode is still active -> "ReplacedStr:::0:::" of Avatar
            else if (_SelectingMouseActive)
            {
                //Check if really a player was selected
                if (_SelectedPlayerNr != -1)
                {
                    //Foreach Drop-Area
                    for (int i = 0; i < _StaticPlayer.Length; i++)
                    {
                        //Check first, if area is "ReplacedStr:::1:::"
                        if (Statics[_StaticPlayer[i]].Visible)
                        {
                            //Check if Mouse is in area
                            if (CHelper.IsInBounds(Statics[_StaticPlayer[i]].Rect, mouseEvent))
                            {
                                //Update Game-infos with new player
                                CGame.Player[i].Name = CProfiles.Profiles[_SelectedPlayerNr].PlayerName;
                                CGame.Player[i].Difficulty = CProfiles.Profiles[_SelectedPlayerNr].Difficulty;
                                CGame.Player[i].ProfileID = _SelectedPlayerNr;
                                //Update config for default players.
                                CConfig.Players[i] = CProfiles.Profiles[_SelectedPlayerNr].ProfileFile;
                                CConfig.SaveConfig();
                                //Update texture and name
                                Statics[_StaticPlayerAvatar[i]].Texture = _ChooseAvatarStatic.Texture;
                                Texts[_TextPlayer[i]].Text = CProfiles.Profiles[_SelectedPlayerNr].PlayerName;
                                //Update profile-warning
                                _CheckPlayers();
                                //Update Tiles-List
                                NameSelections[_NameSelection].UpdateList();
                            }
                        }
                    }
                    _SelectedPlayerNr = -1;
                }
                //Reset variables
                _SelectingMouseActive = false;
                _ChooseAvatarStatic.Visible = false;
            }

            if (mouseEvent.LB && IsMouseOver(mouseEvent)) {}

            if (mouseEvent.LB && IsMouseOver(mouseEvent))
            {
                bool processed = false;

                if (!processed && Buttons[_ButtonBack].Selected)
                {
                    processed = true;
                    CGraphics.FadeTo(EScreens.ScreenSong);
                }

                if (!processed && Buttons[_ButtonStart].Selected)
                {
                    processed = true;
                    _StartSong();
                }

                if (!processed)
                    _UpdatePlayerNumber();
                //Update Tiles-List
                NameSelections[_NameSelection].UpdateList();
            }

            if (mouseEvent.RB)
            {
                bool exit = true;
                //Remove profile-selection
                for (int i = 0; i < CConfig.NumPlayer; i++)
                {
                    if (CHelper.IsInBounds(Statics[_StaticPlayer[i]].Rect, mouseEvent))
                    {
                        CGame.Player[i].ProfileID = -1;
                        CGame.Player[i].Name = String.Empty;
                        CGame.Player[i].Difficulty = EGameDifficulty.TR_CONFIG_EASY;
                        //Update config for default players.
                        CConfig.Players[i] = String.Empty;
                        CConfig.SaveConfig();
                        //Update texture and name
                        Statics[_StaticPlayerAvatar[i]].Texture = _OriginalPlayerAvatarTextures[i];
                        Texts[_TextPlayer[i]].Text = CLanguage.Translate("TR_SCREENNAMES_PLAYER") + " " + (i + 1).ToString();
                        //Update profile-warning
                        _CheckPlayers();
                        //Update Tiles-List
                        NameSelections[_NameSelection].UpdateList();
                        exit = false;
                    }
                }
                if (exit)
                    CGraphics.FadeTo(EScreens.ScreenSong);
            }

            //Check mouse-wheel for scrolling
            if (mouseEvent.Wheel != 0)
            {
                if (CHelper.IsInBounds(NameSelections[_NameSelection].Rect, mouseEvent))
                {
                    int offset = NameSelections[_NameSelection].Offset + mouseEvent.Wheel;
                    NameSelections[_NameSelection].UpdateList(offset);
                }
            }
            return true;
        }

        public override bool UpdateGame()
        {
            for (int i = 1; i <= CGame.NumPlayer; i++)
            {
                CSound.AnalyzeBuffer(i - 1);
                Equalizers["EqualizerPlayer" + i].Update(CSound.ToneWeigth(i - 1));
            }
            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
            CSound.RecordStart();

            NameSelections[_NameSelection].Init();

            _UpdateSlides();
            _UpdatePlayerNumber();
            _CheckMics();
            _CheckPlayers();

            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
            {
                if (CGame.Player[i].ProfileID > -1 && CProfiles.NumProfiles > CGame.Player[i].ProfileID)
                {
                    Statics[_StaticPlayerAvatar[i]].Texture = CProfiles.Profiles[CGame.Player[i].ProfileID].Avatar.Texture;
                    Texts[_TextPlayer[i]].Text = CProfiles.Profiles[CGame.Player[i].ProfileID].PlayerName;
                }
                else
                {
                    Statics[_StaticPlayerAvatar[i]].Texture = _OriginalPlayerAvatarTextures[i];
                    Texts[_TextPlayer[i]].Text = CLanguage.Translate("TR_SCREENNAMES_PLAYER") + " " + (i + 1).ToString();
                }
                if (CGame.GetNumSongs() == 1 && CGame.GetSong(1).IsDuet)
                {
                    SelectSlides[_SelectSlideDuetPlayer[i]].Clear();
                    if (i + 1 <= CGame.NumPlayer)
                        SelectSlides[_SelectSlideDuetPlayer[i]].Visible = true;
                    else
                        SelectSlides[_SelectSlideDuetPlayer[i]].Visible = false;
                    SelectSlides[_SelectSlideDuetPlayer[i]].AddValue(CGame.GetSong(1).DuetPart1);
                    SelectSlides[_SelectSlideDuetPlayer[i]].AddValue(CGame.GetSong(1).DuetPart2);
                    if ((i + 1) % 2 == 0)
                        SelectSlides[_SelectSlideDuetPlayer[i]].Selection = 1;
                    else
                        SelectSlides[_SelectSlideDuetPlayer[i]].Selection = 0;
                }
                else
                    SelectSlides[_SelectSlideDuetPlayer[i]].Visible = false;
            }

            SetInteractionToButton(Buttons[_ButtonStart]);
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
                Equalizers["EqualizerPlayer" + i].Draw();
            return true;
        }

        private void _StartSong()
        {
            for (int i = 0; i < CGame.NumPlayer; i++)
            {
                if (CGame.Player[i].ProfileID < 0)
                {
                    CGame.Player[i].Name = "Player " + (i + 1).ToString();
                    CGame.Player[i].Difficulty = EGameDifficulty.TR_CONFIG_EASY;
                    CGame.Player[i].ProfileID = -1;
                }
                else
                {
                    //Just a work-around for not crashing the game
                    if (CGame.Player[i].Name == null)
                        CGame.Player[i].Name = "";
                }
                if (CGame.GetNumSongs() == 1 && CGame.GetSong(1).IsDuet)
                    CGame.Player[i].LineNr = SelectSlides[_SelectSlideDuetPlayer[i]].Selection;
            }

            CGraphics.FadeTo(EScreens.ScreenSing);
        }

        private void _UpdateSlides()
        {
            SelectSlides[_SelectSlidePlayerNumber].Clear();
            for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
                SelectSlides[_SelectSlidePlayerNumber].AddValue(CLanguage.Translate("TR_SCREENNAMES_" + i + "PLAYER"));
            SelectSlides[_SelectSlidePlayerNumber].Selection = CConfig.NumPlayer - 1;
        }

        private void _UpdatePlayerNumber()
        {
            CConfig.NumPlayer = SelectSlides[_SelectSlidePlayerNumber].Selection + 1;
            CGame.NumPlayer = SelectSlides[_SelectSlidePlayerNumber].Selection + 1;
            for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
            {
                if (i <= CGame.NumPlayer)
                {
                    Statics["StaticPlayer" + i].Visible = true;
                    Statics["StaticPlayerAvatar" + i].Visible = true;
                    Texts["TextPlayer" + i].Visible = true;
                    if (Texts["TextPlayer" + i].Text.Length == 0)
                        Texts["TextPlayer" + i].Text = CLanguage.Translate("TR_SCREENNAMES_PLAYER") + " " + i.ToString();
                    Equalizers["EqualizerPlayer" + i].Visible = true;
                    if (CGame.GetNumSongs() == 1 && CGame.GetSong(1).IsDuet)
                        SelectSlides["SelectSlideDuetPlayer" + i].Visible = true;
                }
                else
                {
                    Statics["StaticPlayer" + i].Visible = false;
                    Statics["StaticPlayerAvatar" + i].Visible = false;
                    Texts["TextPlayer" + i].Visible = false;
                    Equalizers["EqualizerPlayer" + i].Visible = false;
                    SelectSlides["SelectSlideDuetPlayer" + i].Visible = false;
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
                Statics[_StaticWarningMics].Visible = true;
                Texts[_TextWarningMics].Visible = true;

                if (playerWithoutMicro.Count > 1)
                {
                    string playerNums = string.Empty;
                    for (int i = 0; i < playerWithoutMicro.Count; i++)
                    {
                        if (playerWithoutMicro.Count - 1 == i)
                            playerNums += playerWithoutMicro[i].ToString();
                        else if (playerWithoutMicro.Count - 2 == i)
                            playerNums += playerWithoutMicro[i].ToString() + " " + CLanguage.Translate("TR_GENERAL_AND") + " ";
                        else
                            playerNums += playerWithoutMicro[i].ToString() + ", ";
                    }

                    Texts[_TextWarningMics].Text = CLanguage.Translate("TR_SCREENNAMES_WARNING_MICS_PL").Replace("%v", playerNums);
                }
                else
                    Texts[_TextWarningMics].Text = CLanguage.Translate("TR_SCREENNAMES_WARNING_MICS_SG").Replace("%v", playerWithoutMicro[0].ToString());
            }
            else
            {
                Statics[_StaticWarningMics].Visible = false;
                Texts[_TextWarningMics].Visible = false;
            }
        }

        private void _CheckPlayers()
        {
            List<int> playerWithoutProfile = new List<int>();
            for (int player = 0; player < CConfig.NumPlayer; player++)
            {
                if (CGame.Player[player].ProfileID < 0)
                    playerWithoutProfile.Add(player + 1);
            }

            if (playerWithoutProfile.Count > 0)
            {
                Statics[_StaticWarningProfiles].Visible = true;
                Texts[_TextWarningProfiles].Visible = true;

                if (playerWithoutProfile.Count > 1)
                {
                    string playerNums = string.Empty;
                    for (int i = 0; i < playerWithoutProfile.Count; i++)
                    {
                        if (playerWithoutProfile.Count - 1 == i)
                            playerNums += playerWithoutProfile[i].ToString();
                        else if (playerWithoutProfile.Count - 2 == i)
                            playerNums += playerWithoutProfile[i].ToString() + " " + CLanguage.Translate("TR_GENERAL_AND") + " ";
                        else
                            playerNums += playerWithoutProfile[i].ToString() + ", ";
                    }

                    Texts[_TextWarningProfiles].Text = CLanguage.Translate("TR_SCREENNAMES_WARNING_PROFILES_PL").Replace("%v", playerNums);
                }
                else
                    Texts[_TextWarningProfiles].Text = CLanguage.Translate("TR_SCREENNAMES_WARNING_PROFILES_SG").Replace("%v", playerWithoutProfile[0].ToString());
            }
            else
            {
                Statics[_StaticWarningProfiles].Visible = false;
                Texts[_TextWarningProfiles].Visible = false;
            }
        }
    }
}