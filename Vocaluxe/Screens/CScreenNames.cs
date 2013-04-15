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

        private CStatic chooseAvatarStatic;
        private int OldMouseX;
        private int OldMouseY;

        private const string SelectSlidePlayerNumber = "SelectSlidePlayerNumber";
        private const string NameSelection = "NameSelection";
        private const string ButtonBack = "ButtonBack";
        private const string ButtonStart = "ButtonStart";
        private const string TextWarningMics = "TextWarningMics";
        private const string StaticWarningMics = "StaticWarningMics";
        private const string TextWarningProfiles = "TextWarningProfiles";
        private const string StaticWarningProfiles = "StaticWarningProfiles";
        private readonly string[] StaticPlayer = new string[] {"StaticPlayer1", "StaticPlayer2", "StaticPlayer3", "StaticPlayer4", "StaticPlayer5", "StaticPlayer6"};
        private readonly string[] StaticPlayerAvatar = new string[]
            {"StaticPlayerAvatar1", "StaticPlayerAvatar2", "StaticPlayerAvatar3", "StaticPlayerAvatar4", "StaticPlayerAvatar5", "StaticPlayerAvatar6"};
        private readonly string[] TextPlayer = new string[] {"TextPlayer1", "TextPlayer2", "TextPlayer3", "TextPlayer4", "TextPlayer5", "TextPlayer6"};
        private readonly string[] EqualizerPlayer = new string[]
            {"EqualizerPlayer1", "EqualizerPlayer2", "EqualizerPlayer3", "EqualizerPlayer4", "EqualizerPlayer5", "EqualizerPlayer6"};
        private readonly string[] SelectSlideDuetPlayer = new string[]
            {"SelectSlideDuetPlayer1", "SelectSlideDuetPlayer2", "SelectSlideDuetPlayer3", "SelectSlideDuetPlayer4", "SelectSlideDuetPlayer5", "SelectSlideDuetPlayer6"};
        private readonly STexture[] OriginalPlayerAvatarTextures = new STexture[CSettings.MaxNumPlayer];

        private bool selectingMouseActive;
        private bool selectingKeyboardActive;
        private bool selectingKeyboardUnendless;
        private int selectingKeyboardPlayerNr;
        private int SelectedPlayerNr;

        private int[] _PlayerNr;

        public override void Init()
        {
            base.Init();

            List<string> statics = new List<string>();
            foreach (string text in StaticPlayer)
                statics.Add(text);
            foreach (string text in StaticPlayerAvatar)
                statics.Add(text);
            statics.Add(StaticWarningMics);
            statics.Add(StaticWarningProfiles);
            _ThemeStatics = statics.ToArray();

            List<string> texts = new List<string>();
            texts.Add(SelectSlidePlayerNumber);
            texts.AddRange(SelectSlideDuetPlayer);
            _ThemeSelectSlides = texts.ToArray();

            texts.Clear();
            texts.Add(TextWarningMics);
            texts.Add(TextWarningProfiles);
            foreach (string text in TextPlayer)
                texts.Add(text);
            _ThemeTexts = texts.ToArray();

            texts.Clear();
            texts.Add(ButtonBack);
            texts.Add(ButtonStart);

            _ThemeButtons = texts.ToArray();

            texts.Clear();
            texts.Add(NameSelection);
            _ThemeNameSelections = texts.ToArray();

            texts.Clear();
            texts.AddRange(EqualizerPlayer);
            _ThemeEqualizers = texts.ToArray();

            chooseAvatarStatic = GetNewStatic();
            chooseAvatarStatic.Visible = false;
        }

        public override void LoadTheme(string XmlPath)
        {
            base.LoadTheme(XmlPath);

            _PlayerNr = new int[CSettings.MaxNumPlayer];
            for (int i = 0; i < _PlayerNr.Length; i++)
                _PlayerNr[i] = i;

            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
                OriginalPlayerAvatarTextures[i] = Statics[StaticPlayerAvatar[i]].Texture;

            for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
                Equalizers["EqualizerPlayer" + i].ScreenHandles = true;
        }

        public override bool HandleInput(KeyEvent keyEvent)
        {
            switch (keyEvent.Key)
            {
                case Keys.Add:
                    if (CConfig.NumPlayer + 1 <= CSettings.MaxNumPlayer)
                    {
                        SelectSlides[SelectSlidePlayerNumber].Selection = CConfig.NumPlayer;
                        UpdatePlayerNumber();
                        //Update Tiles-List
                        NameSelections[NameSelection].UpdateList();
                    }
                    break;

                case Keys.Subtract:
                    if (CConfig.NumPlayer - 1 > 0)
                    {
                        SelectSlides[SelectSlidePlayerNumber].Selection = CConfig.NumPlayer - 2;
                        UpdatePlayerNumber();
                        //Update Tiles-List
                        NameSelections[NameSelection].UpdateList();
                    }
                    break;

                case Keys.P:
                    if (!selectingKeyboardActive)
                    {
                        selectingKeyboardPlayerNr = 1;
                        selectingKeyboardUnendless = true;
                    }
                    else
                    {
                        if (selectingKeyboardPlayerNr + 1 <= CGame.NumPlayer)
                            selectingKeyboardPlayerNr++;
                        else
                            selectingKeyboardPlayerNr = 1;
                        NameSelections[NameSelection].KeyboardSelection(true, selectingKeyboardPlayerNr);
                    }
                    break;
            }
            //Check if selecting with keyboard is active
            if (selectingKeyboardActive)
            {
                //Handle left/right/up/down
                NameSelections[NameSelection].HandleInput(keyEvent);
                switch (keyEvent.Key)
                {
                    case Keys.Enter:
                        //Check, if a player is selected
                        if (NameSelections[NameSelection].Selection > -1)
                        {
                            SelectedPlayerNr = NameSelections[NameSelection].Selection;
                            //Update Game-infos with new player
                            CGame.Player[selectingKeyboardPlayerNr - 1].Name = CProfiles.Profiles[SelectedPlayerNr].PlayerName;
                            CGame.Player[selectingKeyboardPlayerNr - 1].Difficulty = CProfiles.Profiles[SelectedPlayerNr].Difficulty;
                            CGame.Player[selectingKeyboardPlayerNr - 1].ProfileID = SelectedPlayerNr;
                            //Update config for default players.
                            CConfig.Players[selectingKeyboardPlayerNr - 1] = CProfiles.Profiles[SelectedPlayerNr].ProfileFile;
                            CConfig.SaveConfig();
                            //Update texture and name
                            Statics[StaticPlayerAvatar[selectingKeyboardPlayerNr - 1]].Texture = CProfiles.Profiles[SelectedPlayerNr].Avatar.Texture;
                            Texts[TextPlayer[selectingKeyboardPlayerNr - 1]].Text = CProfiles.Profiles[SelectedPlayerNr].PlayerName;
                            //Update profile-warning
                            CheckPlayers();
                            //Update Tiles-List
                            NameSelections[NameSelection].UpdateList();
                            SetInteractionToButton(Buttons[ButtonStart]);
                        }
                        //Started selecting with 'P'
                        if (selectingKeyboardUnendless)
                        {
                            if (selectingKeyboardPlayerNr == CGame.NumPlayer)
                            {
                                //Reset all values
                                selectingKeyboardPlayerNr = 0;
                                selectingKeyboardActive = false;
                                NameSelections[NameSelection].KeyboardSelection(false, -1);
                            }
                            else
                            {
                                selectingKeyboardPlayerNr++;
                                NameSelections[NameSelection].KeyboardSelection(true, selectingKeyboardPlayerNr);
                            }
                        }
                        else
                        {
                            //Reset all values
                            selectingKeyboardPlayerNr = 0;
                            selectingKeyboardActive = false;
                            NameSelections[NameSelection].KeyboardSelection(false, -1);
                        }
                        break;

                    case Keys.D1:
                    case Keys.NumPad1:
                        if (selectingKeyboardPlayerNr == 1)
                        {
                            //Reset all values
                            selectingKeyboardPlayerNr = 0;
                            selectingKeyboardActive = false;
                            NameSelections[NameSelection].KeyboardSelection(false, -1);
                        }
                        else
                        {
                            selectingKeyboardPlayerNr = 1;
                            NameSelections[NameSelection].KeyboardSelection(true, 1);
                        }
                        selectingKeyboardUnendless = false;
                        break;
                    case Keys.D2:
                    case Keys.NumPad2:
                        if (selectingKeyboardPlayerNr == 2)
                        {
                            //Reset all values
                            selectingKeyboardPlayerNr = 0;
                            selectingKeyboardActive = false;
                            NameSelections[NameSelection].KeyboardSelection(false, -1);
                        }
                        else
                        {
                            selectingKeyboardPlayerNr = 2;
                            NameSelections[NameSelection].KeyboardSelection(true, 2);
                        }
                        selectingKeyboardUnendless = false;
                        break;
                    case Keys.D3:
                    case Keys.NumPad3:
                        if (selectingKeyboardPlayerNr == 3)
                        {
                            //Reset all values
                            selectingKeyboardPlayerNr = 0;
                            selectingKeyboardActive = false;
                            NameSelections[NameSelection].KeyboardSelection(false, -1);
                        }
                        else
                        {
                            selectingKeyboardPlayerNr = 3;
                            NameSelections[NameSelection].KeyboardSelection(true, 3);
                        }
                        selectingKeyboardUnendless = false;
                        break;
                    case Keys.D4:
                    case Keys.NumPad4:
                        if (selectingKeyboardPlayerNr == 4)
                        {
                            //Reset all values
                            selectingKeyboardPlayerNr = 0;
                            selectingKeyboardActive = false;
                            NameSelections[NameSelection].KeyboardSelection(false, -1);
                        }
                        else
                        {
                            selectingKeyboardPlayerNr = 4;
                            NameSelections[NameSelection].KeyboardSelection(true, 4);
                        }
                        selectingKeyboardUnendless = false;
                        break;
                    case Keys.D5:
                    case Keys.NumPad5:
                        if (selectingKeyboardPlayerNr == 5)
                        {
                            //Reset all values
                            selectingKeyboardPlayerNr = 0;
                            selectingKeyboardActive = false;
                            NameSelections[NameSelection].KeyboardSelection(false, -1);
                        }
                        else
                        {
                            selectingKeyboardPlayerNr = 5;
                            NameSelections[NameSelection].KeyboardSelection(true, 5);
                        }
                        selectingKeyboardUnendless = false;
                        break;
                    case Keys.D6:
                    case Keys.NumPad6:
                        if (selectingKeyboardPlayerNr == 6)
                        {
                            //Reset all values
                            selectingKeyboardPlayerNr = 0;
                            selectingKeyboardActive = false;
                            NameSelections[NameSelection].KeyboardSelection(false, -1);
                        }
                        else
                        {
                            selectingKeyboardPlayerNr = 6;
                            NameSelections[NameSelection].KeyboardSelection(true, 6);
                        }
                        selectingKeyboardUnendless = false;
                        break;

                    case Keys.Escape:
                        //Reset all values
                        selectingKeyboardPlayerNr = 0;
                        selectingKeyboardActive = false;
                        selectingKeyboardUnendless = false;
                        NameSelections[NameSelection].KeyboardSelection(false, -1);
                        break;

                    case Keys.Delete:
                        //Delete profile-selection
                        CGame.Player[selectingKeyboardPlayerNr - 1].ProfileID = -1;
                        CGame.Player[selectingKeyboardPlayerNr - 1].Name = String.Empty;
                        CGame.Player[selectingKeyboardPlayerNr - 1].Difficulty = EGameDifficulty.TR_CONFIG_EASY;
                        //Update config for default players.
                        CConfig.Players[selectingKeyboardPlayerNr - 1] = String.Empty;
                        CConfig.SaveConfig();
                        //Update texture and name
                        Statics[StaticPlayerAvatar[selectingKeyboardPlayerNr - 1]].Texture = OriginalPlayerAvatarTextures[selectingKeyboardPlayerNr - 1];
                        Texts[TextPlayer[selectingKeyboardPlayerNr - 1]].Text = CLanguage.Translate("TR_SCREENNAMES_PLAYER") + " " + selectingKeyboardPlayerNr.ToString();
                        //Update profile-warning
                        CheckPlayers();
                        //Reset all values
                        selectingKeyboardPlayerNr = 0;
                        selectingKeyboardActive = false;
                        NameSelections[NameSelection].KeyboardSelection(false, -1);
                        //Update Tiles-List
                        NameSelections[NameSelection].UpdateList();
                        break;

                    case Keys.F10:
                        if (CGame.GetNumSongs() == 1 && CGame.GetSong(1).IsDuet)
                        {
                            if (SelectSlides[SelectSlideDuetPlayer[selectingKeyboardPlayerNr - 1]].Selection == 0)
                                SelectSlides[SelectSlideDuetPlayer[selectingKeyboardPlayerNr - 1]].Selection = 1;
                            else
                                SelectSlides[SelectSlideDuetPlayer[selectingKeyboardPlayerNr - 1]].Selection = 0;
                            //Reset all values
                            selectingKeyboardPlayerNr = 0;
                            selectingKeyboardActive = false;
                            selectingKeyboardUnendless = false;
                            NameSelections[NameSelection].KeyboardSelection(false, -1);
                            SetInteractionToButton(Buttons[ButtonStart]);
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

                        if (!processed && Buttons[ButtonBack].Selected)
                        {
                            processed = true;
                            CGraphics.FadeTo(EScreens.ScreenSong);
                        }

                        if (!processed /* && Buttons[ButtonStart].Selected */)
                        {
                            processed = true;
                            StartSong();
                        }

                        break;

                    case Keys.D1:
                    case Keys.NumPad1:
                        selectingKeyboardPlayerNr = 1;
                        break;

                    case Keys.D2:
                    case Keys.NumPad2:
                        selectingKeyboardPlayerNr = 2;
                        break;

                    case Keys.D3:
                    case Keys.NumPad3:
                        selectingKeyboardPlayerNr = 3;
                        break;

                    case Keys.D4:
                    case Keys.NumPad4:
                        selectingKeyboardPlayerNr = 4;
                        break;

                    case Keys.D5:
                    case Keys.NumPad5:
                        selectingKeyboardPlayerNr = 5;
                        break;

                    case Keys.D6:
                    case Keys.NumPad6:
                        selectingKeyboardPlayerNr = 6;
                        break;
                }

                if (!processed)
                    UpdatePlayerNumber();

                if (selectingKeyboardPlayerNr > 0 && selectingKeyboardPlayerNr <= CConfig.NumPlayer)
                {
                    selectingKeyboardActive = true;
                    NameSelections[NameSelection].KeyboardSelection(true, selectingKeyboardPlayerNr);
                }
            }

            return true;
        }

        public override bool HandleMouse(MouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            //Check if LeftButton is hold and Select-Mode inactive
            if (mouseEvent.LBH && !selectingMouseActive)
            {
                //Save mouse-coords
                OldMouseX = mouseEvent.X;
                OldMouseY = mouseEvent.Y;
                //Check if mouse if over tile
                if (NameSelections[NameSelection].isOverTile(mouseEvent))
                {
                    //Get player-number of tile
                    SelectedPlayerNr = NameSelections[NameSelection].TilePlayerNr(mouseEvent);
                    if (SelectedPlayerNr != -1)
                    {
                        //Activate mouse-selecting
                        selectingMouseActive = true;

                        //Update of Drag/Drop-Texture
                        CStatic SelectedPlayer = NameSelections[NameSelection].TilePlayerAvatar(mouseEvent);
                        chooseAvatarStatic.Visible = true;
                        chooseAvatarStatic.Rect = SelectedPlayer.Rect;
                        chooseAvatarStatic.Rect.Z = CSettings.ZNear;
                        chooseAvatarStatic.Color = new SColorF(1, 1, 1, 1);
                        chooseAvatarStatic.Texture = SelectedPlayer.Texture;
                    }
                }
            }

            //Check if LeftButton is hold and Select-Mode active
            if (mouseEvent.LBH && selectingMouseActive)
            {
                //Update coords for Drag/Drop-Texture
                chooseAvatarStatic.Rect.X += mouseEvent.X - OldMouseX;
                chooseAvatarStatic.Rect.Y += mouseEvent.Y - OldMouseY;
                OldMouseX = mouseEvent.X;
                OldMouseY = mouseEvent.Y;
            }
                // LeftButton isn't hold anymore, but Selec-Mode is still active -> "Drop" of Avatar
            else if (selectingMouseActive)
            {
                //Check if really a player was selected
                if (SelectedPlayerNr != -1)
                {
                    //Foreach Drop-Area
                    for (int i = 0; i < StaticPlayer.Length; i++)
                    {
                        //Check first, if area is "active"
                        if (Statics[StaticPlayer[i]].Visible)
                        {
                            //Check if Mouse is in area
                            if (CHelper.IsInBounds(Statics[StaticPlayer[i]].Rect, mouseEvent))
                            {
                                //Update Game-infos with new player
                                CGame.Player[i].Name = CProfiles.Profiles[SelectedPlayerNr].PlayerName;
                                CGame.Player[i].Difficulty = CProfiles.Profiles[SelectedPlayerNr].Difficulty;
                                CGame.Player[i].ProfileID = SelectedPlayerNr;
                                //Update config for default players.
                                CConfig.Players[i] = CProfiles.Profiles[SelectedPlayerNr].ProfileFile;
                                CConfig.SaveConfig();
                                //Update texture and name
                                Statics[StaticPlayerAvatar[i]].Texture = chooseAvatarStatic.Texture;
                                Texts[TextPlayer[i]].Text = CProfiles.Profiles[SelectedPlayerNr].PlayerName;
                                //Update profile-warning
                                CheckPlayers();
                                //Update Tiles-List
                                NameSelections[NameSelection].UpdateList();
                            }
                        }
                    }
                    SelectedPlayerNr = -1;
                }
                //Reset variables
                selectingMouseActive = false;
                chooseAvatarStatic.Visible = false;
            }

            if (mouseEvent.LB && IsMouseOver(mouseEvent)) {}

            if (mouseEvent.LB && IsMouseOver(mouseEvent))
            {
                bool processed = false;

                if (!processed && Buttons[ButtonBack].Selected)
                {
                    processed = true;
                    CGraphics.FadeTo(EScreens.ScreenSong);
                }

                if (!processed && Buttons[ButtonStart].Selected)
                {
                    processed = true;
                    StartSong();
                }

                if (!processed)
                    UpdatePlayerNumber();
                //Update Tiles-List
                NameSelections[NameSelection].UpdateList();
            }

            if (mouseEvent.RB)
            {
                bool exit = true;
                //Remove profile-selection
                for (int i = 0; i < CConfig.NumPlayer; i++)
                {
                    if (CHelper.IsInBounds(Statics[StaticPlayer[i]].Rect, mouseEvent))
                    {
                        CGame.Player[i].ProfileID = -1;
                        CGame.Player[i].Name = String.Empty;
                        CGame.Player[i].Difficulty = EGameDifficulty.TR_CONFIG_EASY;
                        //Update config for default players.
                        CConfig.Players[i] = String.Empty;
                        CConfig.SaveConfig();
                        //Update texture and name
                        Statics[StaticPlayerAvatar[i]].Texture = OriginalPlayerAvatarTextures[i];
                        Texts[TextPlayer[i]].Text = CLanguage.Translate("TR_SCREENNAMES_PLAYER") + " " + (i + 1).ToString();
                        //Update profile-warning
                        CheckPlayers();
                        //Update Tiles-List
                        NameSelections[NameSelection].UpdateList();
                        exit = false;
                    }
                }
                if (exit)
                    CGraphics.FadeTo(EScreens.ScreenSong);
            }

            //Check mouse-wheel for scrolling
            if (mouseEvent.Wheel != 0)
            {
                if (CHelper.IsInBounds(NameSelections[NameSelection].Rect, mouseEvent))
                {
                    int offset = NameSelections[NameSelection]._Offset + mouseEvent.Wheel;
                    NameSelections[NameSelection].UpdateList(offset);
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

            NameSelections[NameSelection].Init();

            UpdateSlides();
            UpdatePlayerNumber();
            CheckMics();
            CheckPlayers();

            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
            {
                //Update texture and name
                if (CConfig.Players[i].Length > 0)
                {
                    if (CGame.Player[i].ProfileID > -1 && CProfiles.NumProfiles > CGame.Player[i].ProfileID)
                    {
                        Statics[StaticPlayerAvatar[i]].Texture = CProfiles.Profiles[CGame.Player[i].ProfileID].Avatar.Texture;
                        Texts[TextPlayer[i]].Text = CProfiles.Profiles[CGame.Player[i].ProfileID].PlayerName;
                    }
                }
                if (CGame.GetNumSongs() == 1 && CGame.GetSong(1).IsDuet)
                {
                    SelectSlides[SelectSlideDuetPlayer[i]].Clear();
                    if (i + 1 <= CGame.NumPlayer)
                        SelectSlides[SelectSlideDuetPlayer[i]].Visible = true;
                    else
                        SelectSlides[SelectSlideDuetPlayer[i]].Visible = false;
                    SelectSlides[SelectSlideDuetPlayer[i]].AddValue(CGame.GetSong(1).DuetPart1);
                    SelectSlides[SelectSlideDuetPlayer[i]].AddValue(CGame.GetSong(1).DuetPart2);
                    if ((i + 1) % 2 == 0)
                        SelectSlides[SelectSlideDuetPlayer[i]].Selection = 1;
                    else
                        SelectSlides[SelectSlideDuetPlayer[i]].Selection = 0;
                }
                else
                    SelectSlides[SelectSlideDuetPlayer[i]].Visible = false;
            }

            SetInteractionToButton(Buttons[ButtonStart]);
        }

        public override void OnClose()
        {
            base.OnClose();
            CSound.RecordStop();
        }

        public override bool Draw()
        {
            base.Draw();

            if (chooseAvatarStatic.Visible)
                chooseAvatarStatic.Draw();
            for (int i = 1; i <= CGame.NumPlayer; i++)
                Equalizers["EqualizerPlayer" + i].Draw();
            return true;
        }

        private void StartSong()
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
                    CGame.Player[i].LineNr = SelectSlides[SelectSlideDuetPlayer[i]].Selection;
            }

            CGraphics.FadeTo(EScreens.ScreenSing);
        }

        private void UpdateSlides()
        {
            SelectSlides[SelectSlidePlayerNumber].Clear();
            for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
                SelectSlides[SelectSlidePlayerNumber].AddValue(CLanguage.Translate("TR_SCREENNAMES_" + i + "PLAYER"));
            SelectSlides[SelectSlidePlayerNumber].Selection = CConfig.NumPlayer - 1;
        }

        private void UpdatePlayerNumber()
        {
            CConfig.NumPlayer = SelectSlides[SelectSlidePlayerNumber].Selection + 1;
            CGame.NumPlayer = SelectSlides[SelectSlidePlayerNumber].Selection + 1;
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
            CheckMics();
            CheckPlayers();
        }

        private void CheckMics()
        {
            List<int> _PlayerWithoutMicro = new List<int>();
            for (int player = 0; player < CConfig.NumPlayer; player++)
            {
                if (!CConfig.IsMicConfig(player + 1))
                    _PlayerWithoutMicro.Add(player + 1);
            }
            if (_PlayerWithoutMicro.Count > 0)
            {
                Statics[StaticWarningMics].Visible = true;
                Texts[TextWarningMics].Visible = true;

                if (_PlayerWithoutMicro.Count > 1)
                {
                    string PlayerNums = string.Empty;
                    for (int i = 0; i < _PlayerWithoutMicro.Count; i++)
                    {
                        if (_PlayerWithoutMicro.Count - 1 == i)
                            PlayerNums += _PlayerWithoutMicro[i].ToString();
                        else if (_PlayerWithoutMicro.Count - 2 == i)
                            PlayerNums += _PlayerWithoutMicro[i].ToString() + " " + CLanguage.Translate("TR_GENERAL_AND") + " ";
                        else
                            PlayerNums += _PlayerWithoutMicro[i].ToString() + ", ";
                    }

                    Texts[TextWarningMics].Text = CLanguage.Translate("TR_SCREENNAMES_WARNING_MICS_PL").Replace("%v", PlayerNums);
                }
                else
                    Texts[TextWarningMics].Text = CLanguage.Translate("TR_SCREENNAMES_WARNING_MICS_SG").Replace("%v", _PlayerWithoutMicro[0].ToString());
            }
            else
            {
                Statics[StaticWarningMics].Visible = false;
                Texts[TextWarningMics].Visible = false;
            }
        }

        private void CheckPlayers()
        {
            List<int> _PlayerWithoutProfile = new List<int>();
            for (int player = 0; player < CConfig.NumPlayer; player++)
            {
                if (CGame.Player[player].ProfileID < 0)
                    _PlayerWithoutProfile.Add(player + 1);
            }

            if (_PlayerWithoutProfile.Count > 0)
            {
                Statics[StaticWarningProfiles].Visible = true;
                Texts[TextWarningProfiles].Visible = true;

                if (_PlayerWithoutProfile.Count > 1)
                {
                    string PlayerNums = string.Empty;
                    for (int i = 0; i < _PlayerWithoutProfile.Count; i++)
                    {
                        if (_PlayerWithoutProfile.Count - 1 == i)
                            PlayerNums += _PlayerWithoutProfile[i].ToString();
                        else if (_PlayerWithoutProfile.Count - 2 == i)
                            PlayerNums += _PlayerWithoutProfile[i].ToString() + " " + CLanguage.Translate("TR_GENERAL_AND") + " ";
                        else
                            PlayerNums += _PlayerWithoutProfile[i].ToString() + ", ";
                    }

                    Texts[TextWarningProfiles].Text = CLanguage.Translate("TR_SCREENNAMES_WARNING_PROFILES_PL").Replace("%v", PlayerNums);
                }
                else
                    Texts[TextWarningProfiles].Text = CLanguage.Translate("TR_SCREENNAMES_WARNING_PROFILES_SG").Replace("%v", _PlayerWithoutProfile[0].ToString());
            }
            else
            {
                Statics[StaticWarningProfiles].Visible = false;
                Texts[TextWarningProfiles].Visible = false;
            }
        }
    }
}