using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Menu;

namespace Vocaluxe.PartyModes
{
    public class PartyScreenChallengeNames : CMenuParty
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        const string ButtonNext = "ButtonNext";
        const string ButtonBack = "ButtonBack";
        const string NameSelection = "NameSelection";

        private CStatic chooseAvatarStatic;
        private List<string> PlayerButtons;
        private bool SelectingMouseActive = false;
        private bool SelectingKeyboardActive = false;
        private bool SelectingKeyboardUnendless = false;
        private int OldMouseX = 0;
        private int OldMouseY = 0;
        private int SelectedPlayerNr = -1;
        private int SelectingKeyboardPlayerNr = -1;

        //TODO: Get this data from party-mode!
        private int NumPlayers = 5;
        private int[] ProfileIDs;


        private DataFromScreen Data;

        public PartyScreenChallengeNames()
        {
        }

        protected override void Init()
        {
            base.Init();

            chooseAvatarStatic = GetNewStatic();
            chooseAvatarStatic.Visible = false;

            PlayerButtons = new List<string>();
            for (int i = 1; i <=  _PartyMode.GetMaxPlayer(); i++)
            {
                PlayerButtons.Add("ButtonPlayer" + i);
            }

            _ThemeName = "PartyScreenChallengeNames";
            List<string> buttons = new List<string>();
            buttons.Add(ButtonNext);
            buttons.Add(ButtonBack);
            buttons.AddRange(PlayerButtons);
            _ThemeButtons = buttons.ToArray();
            _ThemeNameSelections = new string[] { NameSelection };
            _ScreenVersion = ScreenVersion;

            Data = new DataFromScreen();
            FromScreenNames names = new FromScreenNames();
            names.FadeToConfig = false;
            names.FadeToMain = false;
            names.ProfileIDs = new List<int>();
            Data.ScreenNames = names;
        }

        public override void LoadTheme(string XmlPath)
        {
			base.LoadTheme(XmlPath);
        }

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            base.HandleInput(KeyEvent);

            /**
            //Check if selecting with keyboard is active
            if (SelectingKeyboardActive)
            {
                //Handle left/right/up/down
                //TODO: Fix NameSelection for PartyMode
                NameSelections[htNameSelections(NameSelection)].HandleInput(KeyEvent);
                switch (KeyEvent.Key)
                {
                    case Keys.Enter:
                        //Check, if a player is selected
                        if (NameSelections[htNameSelections(NameSelection)].Selection > -1)
                        {
                            SelectedPlayerNr = NameSelections[htNameSelections(NameSelection)].Selection;
                            //TODO: Update texture and name
                            //Buttons[htButtons(PlayerButtons[i])]. = chooseAvatarStatic.Texture;
                            //Buttons[htButtons(PlayerButtons[i])].Text.Text = CProfiles.Profiles[SelectedPlayerNr].PlayerName;
                            //Add Player-ID to list.
                            ProfileIDs[SelectingKeyboardPlayerNr] = SelectedPlayerNr;
                            UpdateNextButton();
                            //Update Tiles-List
                            NameSelections[htNameSelections(NameSelection)].UpdateList();
                            if(PlayerButtons.Count > SelectingKeyboardPlayerNr)
                                SetInteractionToButton(Buttons[htButtons(PlayerButtons[SelectingKeyboardPlayerNr + 1])]);
                        }
                        //Started selecting with 'P'
                        if (SelectingKeyboardUnendless)
                        {
                            if (SelectingKeyboardPlayerNr == NumPlayers)
                            {
                                //Reset all values
                                SelectingKeyboardPlayerNr = 0;
                                SelectingKeyboardActive = false;
                                NameSelections[htNameSelections(NameSelection)].KeyboardSelection(false, -1);
                            }
                            else
                            {
                                SelectingKeyboardPlayerNr++;
                                NameSelections[htNameSelections(NameSelection)].KeyboardSelection(true, SelectingKeyboardPlayerNr);
                            }
                        }
                        else
                        {
                            //Reset all values
                            SelectingKeyboardPlayerNr = 0;
                            SelectingKeyboardActive = false;
                            NameSelections[htNameSelections(NameSelection)].KeyboardSelection(false, -1);
                        }
                        break; 
            }

            else
            {
            **/

            if (KeyEvent.KeyPressed)
                {

                }
                else
                {
                    switch (KeyEvent.Key)
                    {
                        case Keys.Back:
                        case Keys.Escape:
                            Back();
                            break;

                        case Keys.Enter:
                            if (Buttons[htButtons(ButtonBack)].Selected)
                                Back();

                            if (Buttons[htButtons(ButtonNext)].Selected)
                                Next();

                            for (int i = 0; i < PlayerButtons.Count; i++)
                            {
                                if (Buttons[htButtons(PlayerButtons[i])].Selected)
                                {
                                    //KeyboardSelectPlayerNr = i;
                                    //selectingKeyboardActive = true;
                                    //NameSelections[htNameSelections(NameSelection)].KeyboardSelection(true, i);
                                }
                            }
                            break;
                        /**
                        case Keys.P:
                            if (!SelectingKeyboardActive)
                            {
                                SelectingKeyboardPlayerNr = 1;
                                SelectingKeyboardUnendless = true;
                            }
                            else
                            {
                                if (SelectingKeyboardPlayerNr + 1 <= NumPlayers)
                                    SelectingKeyboardPlayerNr++;
                                else
                                    SelectingKeyboardPlayerNr = 1;
                                NameSelections[htNameSelections(NameSelection)].KeyboardSelection(true, SelectingKeyboardPlayerNr);
                            }
                            break;
                         **/
                    }
                }
            //}
            return true;
        }

        public override bool HandleMouse(MouseEvent MouseEvent)
        {
            base.HandleMouse(MouseEvent);

            //Check if LeftButton is hold and Select-Mode inactive
            if (MouseEvent.LBH && !SelectingMouseActive)
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
                        SelectingMouseActive = true;

                        //Update of Drag/Drop-Texture
                        CStatic SelectedPlayer = NameSelections[htNameSelections(NameSelection)].TilePlayerAvatar(MouseEvent);
                        chooseAvatarStatic.Visible = true;
                        chooseAvatarStatic.Rect = SelectedPlayer.Rect;
                        //TODO: Get nearest z-value!
                        //chooseAvatarStatic.Rect.Z = CSettings.zNear;
                        chooseAvatarStatic.Rect.Z = -100;
                        chooseAvatarStatic.Color = new SColorF(1, 1, 1, 1);
                        chooseAvatarStatic.Texture = SelectedPlayer.Texture;
                    }
                }
            }

            //Check if LeftButton is hold and Select-Mode active
            if (MouseEvent.LBH && SelectingMouseActive)
            {
                //Update coords for Drag/Drop-Texture
                chooseAvatarStatic.Rect.X += (MouseEvent.X - OldMouseX);
                chooseAvatarStatic.Rect.Y += (MouseEvent.Y - OldMouseY);
                OldMouseX = MouseEvent.X;
                OldMouseY = MouseEvent.Y;
            }
            // LeftButton isn't hold anymore, but Selec-Mode is still active -> "Drop" of Avatar
            else if (SelectingMouseActive)
            {
                //Check if really a player was selected
                if (SelectedPlayerNr != -1)
                {
                    //Foreach Drop-Area
                    for (int i = 0; i < PlayerButtons.Count; i++)
                    {
                        //Check first, if area is "active"
                        if (Buttons[htButtons(PlayerButtons[i])].Visible == true)
                        {
                            //Check if Mouse is in area
                            if (CHelper.IsInBounds(Buttons[htButtons(PlayerButtons[i])].Rect, MouseEvent))
                            {
                                //Add Player-ID to list.
                                ProfileIDs[i] = SelectedPlayerNr;
                                UpdateNextButton();
                                //TODO: Update texture and name
                                //Buttons[htButtons(PlayerButtons[i])]. = chooseAvatarStatic.Texture;
                                //Buttons[htButtons(PlayerButtons[i])].Text.Text = CProfiles.Profiles[SelectedPlayerNr].PlayerName;
                                //TODO: Just for testing, remove.
                                Buttons[htButtons(PlayerButtons[i])].Text.Text = "Gesetzt";
                                //Update Tiles-List
                                NameSelections[htNameSelections(NameSelection)].UpdateList();
                            }
                        }
                    }
                    SelectedPlayerNr = -1;
                }
                //Reset variables
                SelectingMouseActive = false;
                chooseAvatarStatic.Visible = false;
            }

            if (MouseEvent.LB && IsMouseOver(MouseEvent))
            {
                if (Buttons[htButtons(ButtonBack)].Selected)
                    Back();

                if (Buttons[htButtons(ButtonNext)].Selected)
                    Next();
            }

            if (MouseEvent.RB)
            {
                Back();
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            ProfileIDs = new int[NumPlayers];
            for (int i = 0; i < ProfileIDs.Length; i++ )
                ProfileIDs[i] = -1;

            for (int i = 1; i <= PlayerButtons.Count; i++)
            {
                Buttons[htButtons("ButtonPlayer" + i)].Text.Text = "Player " + i;
                if (i <= NumPlayers)
                    Buttons[htButtons("ButtonPlayer" + i)].Visible = true;
                else
                    Buttons[htButtons("ButtonPlayer" + i)].Visible = false;
            }

            NameSelections[htNameSelections(NameSelection)].Init();

            UpdateNextButton();
        }

        public override bool UpdateGame()
        {
            return true;
        }

        public override bool Draw()
        {
            base.Draw();
            if (chooseAvatarStatic.Visible)
                chooseAvatarStatic.Draw();
            return true;
        }

        public override void OnClose()
        {
            base.OnClose();
        }

        private void Back()
        {
            Data.ScreenNames.FadeToConfig = true;
            Data.ScreenNames.FadeToMain = false;
            _PartyMode.DataFromScreen(_ThemeName, Data);
        }

        private void UpdateNextButton()
        {
            Buttons[htButtons(ButtonNext)].Visible = true;
            for (int i = 0; i < ProfileIDs.Length; i++)
                if (ProfileIDs[i] == -1)
                    Buttons[htButtons(ButtonNext)].Visible = false;
        }

        private void Next()
        {
            List<int> list = new List<int>();
            list.AddRange(ProfileIDs);
            Data.ScreenNames.ProfileIDs = list;
            Data.ScreenNames.FadeToConfig = false;
            Data.ScreenNames.FadeToMain = true;
            _PartyMode.DataFromScreen(_ThemeName, Data);
        }
    }
}
