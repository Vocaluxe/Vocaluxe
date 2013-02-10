using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;

namespace Vocaluxe.Screens
{
    class CScreenNames : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 3;

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
        private readonly string[] StaticPlayer = new string[] { "StaticPlayer1", "StaticPlayer2", "StaticPlayer3", "StaticPlayer4", "StaticPlayer5", "StaticPlayer6" };
        private readonly string[] StaticPlayerAvatar = new string[] { "StaticPlayerAvatar1", "StaticPlayerAvatar2", "StaticPlayerAvatar3", "StaticPlayerAvatar4", "StaticPlayerAvatar5", "StaticPlayerAvatar6" };
        private readonly string[] TextPlayer = new string[] { "TextPlayer1", "TextPlayer2", "TextPlayer3", "TextPlayer4", "TextPlayer5", "TextPlayer6" };
        private readonly string[] EqualizerPlayer = new string[] { "EqualizerPlayer1", "EqualizerPlayer2", "EqualizerPlayer3", "EqualizerPlayer4", "EqualizerPlayer5", "EqualizerPlayer6" };
        private readonly string[] SelectSlideDuetPlayer = new string[] { "SelectSlideDuetPlayer1", "SelectSlideDuetPlayer2", "SelectSlideDuetPlayer3", "SelectSlideDuetPlayer4", "SelectSlideDuetPlayer5", "SelectSlideDuetPlayer6" };
        private STexture[] OriginalPlayerAvatarTextures = new STexture[CSettings.MaxNumPlayer];

        private bool selectingMouseActive = false;
        private bool selectingKeyboardActive = false;
        private bool selectingKeyboardUnendless = false;
        private int selectingKeyboardPlayerNr = 0;
        private int SelectedPlayerNr;
        
        private int[] _PlayerNr;
        
        public CScreenNames()
        {
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "ScreenNames";
            _ScreenVersion = ScreenVersion;

            List<string> statics = new List<string>();
            foreach(string text in StaticPlayer){
                statics.Add(text);
            }
            foreach (string text in StaticPlayerAvatar)
            {
                statics.Add(text);
            }
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
            foreach(string text in TextPlayer){
                texts.Add(text);
            }
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
            {
                _PlayerNr[i] = i;
            }

            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
            {
                OriginalPlayerAvatarTextures[i] = Statics[htStatics(StaticPlayerAvatar[i])].Texture;
            }

            for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
            {
                Equalizers[htEqualizer("EqualizerPlayer" + i)].ScreenHandles = true;
            }
        }

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            switch (KeyEvent.Key)
            {
                case Keys.Add:
                    if (CConfig.NumPlayer + 1 <= CSettings.MaxNumPlayer)
                    {
                        SelectSlides[htSelectSlides(SelectSlidePlayerNumber)].Selection = CConfig.NumPlayer;
                        UpdatePlayerNumber();
                        //Update Tiles-List
                        NameSelections[htNameSelections(NameSelection)].UpdateList();
                    }
                    break;

                case Keys.Subtract:
                    if (CConfig.NumPlayer - 1 > 0)
                    {
                        SelectSlides[htSelectSlides(SelectSlidePlayerNumber)].Selection = CConfig.NumPlayer - 2;
                        UpdatePlayerNumber();
                        //Update Tiles-List
                        NameSelections[htNameSelections(NameSelection)].UpdateList();
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
                        NameSelections[htNameSelections(NameSelection)].KeyboardSelection(true, selectingKeyboardPlayerNr);
                    }
                    break;
            }
            //Check if selecting with keyboard is active
            if (selectingKeyboardActive)
            {
                //Handle left/right/up/down
                NameSelections[htNameSelections(NameSelection)].HandleInput(KeyEvent);
                switch (KeyEvent.Key)
                {
                    case Keys.Enter:
                        //Check, if a player is selected
                        if (NameSelections[htNameSelections(NameSelection)].Selection > -1)
                        {
                            SelectedPlayerNr = NameSelections[htNameSelections(NameSelection)].Selection;
                            //Update Game-infos with new player
                            CGame.Player[selectingKeyboardPlayerNr-1].Name = CProfiles.Profiles[SelectedPlayerNr].PlayerName;
                            CGame.Player[selectingKeyboardPlayerNr-1].Difficulty = CProfiles.Profiles[SelectedPlayerNr].Difficulty;
                            CGame.Player[selectingKeyboardPlayerNr-1].ProfileID = SelectedPlayerNr;
                            //Update config for default players.
                            CConfig.Players[selectingKeyboardPlayerNr - 1] = CProfiles.Profiles[SelectedPlayerNr].ProfileFile;
                            CConfig.SaveConfig();
                            //Update texture and name
                            Statics[htStatics(StaticPlayerAvatar[selectingKeyboardPlayerNr-1])].Texture = CProfiles.Profiles[SelectedPlayerNr].Avatar.Texture;
                            Texts[htTexts(TextPlayer[selectingKeyboardPlayerNr-1])].Text = CProfiles.Profiles[SelectedPlayerNr].PlayerName;
                            //Update profile-warning
                            CheckPlayers();
                            //Update Tiles-List
                            NameSelections[htNameSelections(NameSelection)].UpdateList();
                            SetInteractionToButton(Buttons[htButtons(ButtonStart)]);
                        }
                        //Started selecting with 'P'
                        if (selectingKeyboardUnendless)
                        {
                            if (selectingKeyboardPlayerNr == CGame.NumPlayer)
                            {
                                //Reset all values
                                selectingKeyboardPlayerNr = 0;
                                selectingKeyboardActive = false;
                                NameSelections[htNameSelections(NameSelection)].KeyboardSelection(false, -1);
                            }
                            else
                            {
                                selectingKeyboardPlayerNr++;
                                NameSelections[htNameSelections(NameSelection)].KeyboardSelection(true, selectingKeyboardPlayerNr);
                            }
                        }
                        else
                        {
                            //Reset all values
                            selectingKeyboardPlayerNr = 0;
                            selectingKeyboardActive = false;
                            NameSelections[htNameSelections(NameSelection)].KeyboardSelection(false, -1);
                        }
                        break;

                    case Keys.D1:
                    case Keys.NumPad1:
                        if (selectingKeyboardPlayerNr == 1)
                        {
                            //Reset all values
                            selectingKeyboardPlayerNr = 0;
                            selectingKeyboardActive = false;
                            NameSelections[htNameSelections(NameSelection)].KeyboardSelection(false, -1);
                        }
                        else
                        {
                            selectingKeyboardPlayerNr = 1;
                            NameSelections[htNameSelections(NameSelection)].KeyboardSelection(true, 1);
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
                            NameSelections[htNameSelections(NameSelection)].KeyboardSelection(false, -1);
                        }
                        else
                        {
                            selectingKeyboardPlayerNr = 2;
                            NameSelections[htNameSelections(NameSelection)].KeyboardSelection(true, 2);
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
                            NameSelections[htNameSelections(NameSelection)].KeyboardSelection(false, -1);
                        }
                        else
                        {
                            selectingKeyboardPlayerNr = 3;
                            NameSelections[htNameSelections(NameSelection)].KeyboardSelection(true, 3);
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
                            NameSelections[htNameSelections(NameSelection)].KeyboardSelection(false, -1);
                        }
                        else
                        {
                            selectingKeyboardPlayerNr = 4;
                            NameSelections[htNameSelections(NameSelection)].KeyboardSelection(true, 4);
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
                            NameSelections[htNameSelections(NameSelection)].KeyboardSelection(false, -1);
                        }
                        else
                        {
                            selectingKeyboardPlayerNr = 5;
                            NameSelections[htNameSelections(NameSelection)].KeyboardSelection(true, 5);
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
                            NameSelections[htNameSelections(NameSelection)].KeyboardSelection(false, -1);
                        }
                        else
                        {
                            selectingKeyboardPlayerNr = 6;
                            NameSelections[htNameSelections(NameSelection)].KeyboardSelection(true, 6);
                        }
                        selectingKeyboardUnendless = false;
                        break;

                    case Keys.Escape:
                        //Reset all values
                        selectingKeyboardPlayerNr = 0;
                        selectingKeyboardActive = false;
                        selectingKeyboardUnendless = false;
                        NameSelections[htNameSelections(NameSelection)].KeyboardSelection(false, -1);
                        break;
                    
                    case Keys.Delete:
                    //Delete profile-selection
                        CGame.Player[selectingKeyboardPlayerNr-1].ProfileID = -1;
                        CGame.Player[selectingKeyboardPlayerNr-1].Name = String.Empty;
                        CGame.Player[selectingKeyboardPlayerNr-1].Difficulty = EGameDifficulty.TR_CONFIG_EASY;
                        //Update config for default players.
                        CConfig.Players[selectingKeyboardPlayerNr - 1] = String.Empty;
                        CConfig.SaveConfig();
                        //Update texture and name
                        Statics[htStatics(StaticPlayerAvatar[selectingKeyboardPlayerNr - 1])].Texture = OriginalPlayerAvatarTextures[selectingKeyboardPlayerNr - 1];
                        Texts[htTexts(TextPlayer[selectingKeyboardPlayerNr - 1])].Text = CLanguage.Translate("TR_SCREENNAMES_PLAYER") + " " + selectingKeyboardPlayerNr.ToString();
                        //Update profile-warning
                        CheckPlayers();
                        //Reset all values
                        selectingKeyboardPlayerNr = 0;
                        selectingKeyboardActive = false;
                        NameSelections[htNameSelections(NameSelection)].KeyboardSelection(false, -1);
                        //Update Tiles-List
                        NameSelections[htNameSelections(NameSelection)].UpdateList();
                        break;
                    
                    case Keys.F10:
                        if (CGame.GetNumSongs() == 1 && CGame.GetSong(1).IsDuet)
                        {
                            if (SelectSlides[htSelectSlides(SelectSlideDuetPlayer[selectingKeyboardPlayerNr - 1])].Selection == 0)
                                SelectSlides[htSelectSlides(SelectSlideDuetPlayer[selectingKeyboardPlayerNr - 1])].Selection = 1;
                            else
                                SelectSlides[htSelectSlides(SelectSlideDuetPlayer[selectingKeyboardPlayerNr - 1])].Selection = 0;
                            //Reset all values
                            selectingKeyboardPlayerNr = 0;
                            selectingKeyboardActive = false;
                            selectingKeyboardUnendless = false;
                            NameSelections[htNameSelections(NameSelection)].KeyboardSelection(false, -1);
                            SetInteractionToButton(Buttons[htButtons(ButtonStart)]);
                        }
                        break;

                }
            }
            //Normal Keyboard handling
            else
            {
                base.HandleInput(KeyEvent);
                bool processed = false;
                switch (KeyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                        CGraphics.FadeTo(EScreens.ScreenSong);
                        break;

                    case Keys.Enter:

                        if (!processed && Buttons[htButtons(ButtonBack)].Selected)
                        {
                            processed = true;
                            CGraphics.FadeTo(EScreens.ScreenSong);
                        }

                        if (!processed /* && Buttons[htButtons(ButtonStart)].Selected */)
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
                    NameSelections[htNameSelections(NameSelection)].KeyboardSelection(true, selectingKeyboardPlayerNr);
                }
            }

            return true;
        }

        public override bool HandleMouse(MouseEvent MouseEvent)
        {
            base.HandleMouse(MouseEvent);

            //Check if LeftButton is hold and Select-Mode inactive
            if (MouseEvent.LBH && !selectingMouseActive)
            {
                //Save mouse-coords
                OldMouseX = MouseEvent.X;
                OldMouseY = MouseEvent.Y;
                //Check if mouse if over tile
                if (NameSelections[htNameSelections(NameSelection)].isOverTile(MouseEvent))
                {
                    //Get player-number of tile
                    SelectedPlayerNr = NameSelections[htNameSelections(NameSelection)].TilePlayerNr(MouseEvent);
                    if (SelectedPlayerNr != -1)
                    {
                        //Activate mouse-selecting
                        selectingMouseActive = true;

                        //Update of Drag/Drop-Texture
                        CStatic SelectedPlayer = NameSelections[htNameSelections(NameSelection)].TilePlayerAvatar(MouseEvent);
                        chooseAvatarStatic.Visible = true;
                        chooseAvatarStatic.Rect = SelectedPlayer.Rect;
                        chooseAvatarStatic.Rect.Z = CSettings.zNear;
                        chooseAvatarStatic.Color = new SColorF(1, 1, 1, 1);
                        chooseAvatarStatic.Texture = SelectedPlayer.Texture;
                    }
                }
            }

            //Check if LeftButton is hold and Select-Mode active
            if (MouseEvent.LBH && selectingMouseActive)
            {
                //Update coords for Drag/Drop-Texture
                chooseAvatarStatic.Rect.X += (MouseEvent.X - OldMouseX);
                chooseAvatarStatic.Rect.Y += (MouseEvent.Y - OldMouseY);
                OldMouseX = MouseEvent.X;
                OldMouseY = MouseEvent.Y;
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
                        if (Statics[htStatics(StaticPlayer[i])].Visible == true)
                        {
                            //Check if Mouse is in area
                            if (CHelper.IsInBounds(Statics[htStatics(StaticPlayer[i])].Rect, MouseEvent))
                            {
                                //Update Game-infos with new player
                                CGame.Player[i].Name = CProfiles.Profiles[SelectedPlayerNr].PlayerName;
                                CGame.Player[i].Difficulty = CProfiles.Profiles[SelectedPlayerNr].Difficulty;
                                CGame.Player[i].ProfileID = SelectedPlayerNr;
                                //Update config for default players.
                                CConfig.Players[i] = CProfiles.Profiles[SelectedPlayerNr].ProfileFile;
                                CConfig.SaveConfig();
                                //Update texture and name
                                Statics[htStatics(StaticPlayerAvatar[i])].Texture = chooseAvatarStatic.Texture;
                                Texts[htTexts(TextPlayer[i])].Text = CProfiles.Profiles[SelectedPlayerNr].PlayerName;
                                //Update profile-warning
                                CheckPlayers();
                                //Update Tiles-List
                                NameSelections[htNameSelections(NameSelection)].UpdateList();
                            }
                        }
                    }
                    SelectedPlayerNr = -1;
                }
                //Reset variables
                selectingMouseActive = false;
                chooseAvatarStatic.Visible = false;
            }

            if (MouseEvent.LB && IsMouseOver(MouseEvent))
            {

            }

            if (MouseEvent.LB && IsMouseOver(MouseEvent))
            {
                bool processed = false;

                if (!processed && Buttons[htButtons(ButtonBack)].Selected)
                {
                    processed = true;
                    CGraphics.FadeTo(EScreens.ScreenSong);
                }

                if (!processed && Buttons[htButtons(ButtonStart)].Selected)
                {
                    processed = true;
                    StartSong();
                }

                if (!processed)
                    UpdatePlayerNumber();
                    //Update Tiles-List
                    NameSelections[htNameSelections(NameSelection)].UpdateList();
            }

            if (MouseEvent.RB)
            {
                
                bool exit = true;
                //Remove profile-selection
                for (int i = 0; i < CConfig.NumPlayer; i++)
                {
                    if (CHelper.IsInBounds(Statics[htStatics(StaticPlayer[i])].Rect, MouseEvent))
                    {
                        CGame.Player[i].ProfileID = -1;
                        CGame.Player[i].Name = String.Empty;
                        CGame.Player[i].Difficulty = EGameDifficulty.TR_CONFIG_EASY;
                        //Update config for default players.
                        CConfig.Players[i] = String.Empty;
                        CConfig.SaveConfig();
                        //Update texture and name
                        Statics[htStatics(StaticPlayerAvatar[i])].Texture = OriginalPlayerAvatarTextures[i];
                        Texts[htTexts(TextPlayer[i])].Text = CLanguage.Translate("TR_SCREENNAMES_PLAYER") + " " + (i+1).ToString();
                        //Update profile-warning
                        CheckPlayers();
                        //Update Tiles-List
                        NameSelections[htNameSelections(NameSelection)].UpdateList();
                        exit = false;
                    }
                }
                if(exit)
                    CGraphics.FadeTo(EScreens.ScreenSong);
            }

            //Check mouse-wheel for scrolling
            if (MouseEvent.Wheel != 0)
            {
                if (CHelper.IsInBounds(NameSelections[htNameSelections(NameSelection)].Rect, MouseEvent))
                {
                    int offset = NameSelections[htNameSelections(NameSelection)]._Offset + MouseEvent.Wheel;
                    NameSelections[htNameSelections(NameSelection)].UpdateList(offset);
                }
            }
            return true;
        }

        public override bool UpdateGame()
        {
            for (int i = 1; i <= CGame.NumPlayer; i++)
            {
                CSound.AnalyzeBuffer((i-1));
                Equalizers[htEqualizer("EqualizerPlayer" + i)].Update(CSound.ToneWeigth((i-1)));
            }
            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
            CSound.RecordStart();

            NameSelections[htNameSelections(NameSelection)].Init();

            UpdateSlides();
            UpdatePlayerNumber();
            CheckMics();
            CheckPlayers();

            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
            {
                //Update texture and name
                if (CConfig.Players[i] != String.Empty)
                {
                    Statics[htStatics(StaticPlayerAvatar[i])].Texture = CProfiles.Profiles[CGame.Player[i].ProfileID].Avatar.Texture;
                    Texts[htTexts(TextPlayer[i])].Text = CProfiles.Profiles[CGame.Player[i].ProfileID].PlayerName;
                }
                if (CGame.GetNumSongs() == 1 && CGame.GetSong(1).IsDuet)
                {
                    SelectSlides[htSelectSlides(SelectSlideDuetPlayer[i])].Clear();
                    if (i + 1 <= CGame.NumPlayer)
                        SelectSlides[htSelectSlides(SelectSlideDuetPlayer[i])].Visible = true;
                    else
                        SelectSlides[htSelectSlides(SelectSlideDuetPlayer[i])].Visible = false;
                    SelectSlides[htSelectSlides(SelectSlideDuetPlayer[i])].AddValue(CGame.GetSong(1).DuetPart1);
                    SelectSlides[htSelectSlides(SelectSlideDuetPlayer[i])].AddValue(CGame.GetSong(1).DuetPart2);
                    if ((i + 1) % 2 == 0)
                        SelectSlides[htSelectSlides(SelectSlideDuetPlayer[i])].Selection = 1;
                    else
                        SelectSlides[htSelectSlides(SelectSlideDuetPlayer[i])].Selection = 0;
                }
                else
                    SelectSlides[htSelectSlides(SelectSlideDuetPlayer[i])].Visible = false;
            }

            SetInteractionToButton(Buttons[htButtons(ButtonStart)]);
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
            {
                chooseAvatarStatic.Draw();
            }
            for (int i = 1; i <= CGame.NumPlayer; i++)
            {
                Equalizers[htEqualizer("EqualizerPlayer" + i)].Draw();
            }
            return true;           
        }

        private void StartSong()
        {
            for (int i = 0; i < CGame.NumPlayer; i++)
            {
                if (CGame.Player[i].ProfileID < 0)
                {
                    CGame.Player[i].Name = "Player " + (i+1).ToString();
                    CGame.Player[i].Difficulty = EGameDifficulty.TR_CONFIG_EASY;
                    CGame.Player[i].ProfileID = -1;
                }
                else
                {
                    //Just a work-around for not crashing the game
                    if (CGame.Player[i].Name == null)
                    {
                        CGame.Player[i].Name = "";
                    }
                }
                if (CGame.GetNumSongs() == 1 && CGame.GetSong(1).IsDuet)
                {
                    CGame.Player[i].LineNr = SelectSlides[htSelectSlides(SelectSlideDuetPlayer[i])].Selection;
                }
            }

            CGraphics.FadeTo(EScreens.ScreenSing);
        }

        private void UpdateSlides()
        {
            SelectSlides[htSelectSlides(SelectSlidePlayerNumber)].Clear();
            for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
            {
                SelectSlides[htSelectSlides(SelectSlidePlayerNumber)].AddValue(CLanguage.Translate("TR_SCREENNAMES_" + i + "PLAYER"));
            }
            SelectSlides[htSelectSlides(SelectSlidePlayerNumber)].Selection = CConfig.NumPlayer - 1;
        }

        private void UpdatePlayerNumber()
        {
            CConfig.NumPlayer = (int)SelectSlides[htSelectSlides(SelectSlidePlayerNumber)].Selection + 1;
            CGame.NumPlayer = (int)SelectSlides[htSelectSlides(SelectSlidePlayerNumber)].Selection + 1;
            for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
            {
                if (i <= CGame.NumPlayer)
                {
                    Statics[htStatics("StaticPlayer" + i)].Visible = true;
                    Statics[htStatics("StaticPlayerAvatar" + i)].Visible = true;
                    Texts[htTexts("TextPlayer" + i)].Visible = true;
                    if (Texts[htTexts("TextPlayer" + i)].Text == string.Empty)
                    {
                        Texts[htTexts("TextPlayer" + i)].Text = CLanguage.Translate("TR_SCREENNAMES_PLAYER") + " " + i.ToString();
                    }
                    Equalizers[htEqualizer("EqualizerPlayer" + i)].Visible = true;
                    if (CGame.GetNumSongs() == 1 && CGame.GetSong(1).IsDuet)
                        SelectSlides[htSelectSlides("SelectSlideDuetPlayer" + i)].Visible = true;
                }
                else
                {
                    Statics[htStatics("StaticPlayer" + i)].Visible = false;
                    Statics[htStatics("StaticPlayerAvatar" + i)].Visible = false;
                    Texts[htTexts("TextPlayer" + i)].Visible = false;
                    Equalizers[htEqualizer("EqualizerPlayer" + i)].Visible = false;
                    SelectSlides[htSelectSlides("SelectSlideDuetPlayer" + i)].Visible = false;
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
                {
                    _PlayerWithoutMicro.Add(player + 1);
                }
            }
            if (_PlayerWithoutMicro.Count > 0)
            {
                Statics[htStatics(StaticWarningMics)].Visible = true;
                Texts[htTexts(TextWarningMics)].Visible = true;

                if (_PlayerWithoutMicro.Count > 1)
                {
                    string PlayerNums = string.Empty;
                    for (int i = 0; i < _PlayerWithoutMicro.Count; i++)
                    {
                        if (_PlayerWithoutMicro.Count - 1 == i)
                        {
                            PlayerNums += _PlayerWithoutMicro[i].ToString();
                        }
                        else if (_PlayerWithoutMicro.Count - 2 == i)
                        {
                            PlayerNums += _PlayerWithoutMicro[i].ToString() + " " + CLanguage.Translate("TR_GENERAL_AND") + " ";
                        }
                        else
                        {
                            PlayerNums += _PlayerWithoutMicro[i].ToString() + ", ";
                        }
                    }

                    Texts[htTexts(TextWarningMics)].Text = CLanguage.Translate("TR_SCREENNAMES_WARNING_MICS_PL").Replace("%v", PlayerNums);
                }
                else
                {
                    Texts[htTexts(TextWarningMics)].Text = CLanguage.Translate("TR_SCREENNAMES_WARNING_MICS_SG").Replace("%v", _PlayerWithoutMicro[0].ToString());
                }
            }
            else
            {
                Statics[htStatics(StaticWarningMics)].Visible = false;
                Texts[htTexts(TextWarningMics)].Visible = false;
            }
        }

        private void CheckPlayers()
        {
            List<int> _PlayerWithoutProfile = new List<int>();
            for (int player = 0; player < CConfig.NumPlayer; player++)
            {
                if (CGame.Player[player].ProfileID < 0)
                {
                    _PlayerWithoutProfile.Add(player + 1);
                }
            }

            if (_PlayerWithoutProfile.Count > 0)
            {
                Statics[htStatics(StaticWarningProfiles)].Visible = true;
                Texts[htTexts(TextWarningProfiles)].Visible = true;

                if (_PlayerWithoutProfile.Count > 1)
                {
                    string PlayerNums = string.Empty;
                    for (int i = 0; i < _PlayerWithoutProfile.Count; i++)
                    {
                        if (_PlayerWithoutProfile.Count - 1 == i)
                        {
                            PlayerNums += _PlayerWithoutProfile[i].ToString();
                        }
                        else if (_PlayerWithoutProfile.Count - 2 == i)
                        {
                            PlayerNums += _PlayerWithoutProfile[i].ToString() + " " + CLanguage.Translate("TR_GENERAL_AND") + " ";
                        }
                        else
                        {
                            PlayerNums += _PlayerWithoutProfile[i].ToString() + ", ";
                        }
                    }

                    Texts[htTexts(TextWarningProfiles)].Text = CLanguage.Translate("TR_SCREENNAMES_WARNING_PROFILES_PL").Replace("%v", PlayerNums);
                }
                else
                {
                    Texts[htTexts(TextWarningProfiles)].Text = CLanguage.Translate("TR_SCREENNAMES_WARNING_PROFILES_SG").Replace("%v", _PlayerWithoutProfile[0].ToString());
                }
            }
            else
            {
                Statics[htStatics(StaticWarningProfiles)].Visible = false;
                Texts[htTexts(TextWarningProfiles)].Visible = false;
            }
        }
    }
}
