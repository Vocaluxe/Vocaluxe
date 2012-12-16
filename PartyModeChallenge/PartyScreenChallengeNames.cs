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
        const string ButtonPlayerDestination = "ButtonPlayerDestination";
        const string ButtonPlayerChoose = "ButtonPlayerChoose";

        private List<CPlayerChooseButton> PlayerChooseButtons;
        private List<CButton> PlayerDestinationButtons;
        //PlayerDestinationButtons-Option
        private const int PlayerDestinationButtonsNumH = 3;
        private const int PlayerDestinationButtonsNumW = 4;
        private const int PlayerDestinationButtonsFirstX = 900;
        private const int PlayerDestinationButtonsFirstY = 105;
        private const int PlayerDestinationButtonsSpaceH = 15;
        private const int PlayerDestinationButtonsSpaceW = 25;
        //PlayerChooseButtons-Option
        private const int PlayerChooseButtonsNumH = 7;
        private const int PlayerChooseButtonsNumW = 4;
        private const int PlayerChooseButtonsFirstX = 52;
        private const int PlayerChooseButtonsFirstY = 105;
        private const int PlayerChooseButtonsSpaceH = 15;
        private const int PlayerChooseButtonsSpaceW = 25;
        private int PlayerChooseButtonsOffset = 0;

        private CStatic chooseAvatarStatic;
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

        private class CPlayerChooseButton
        {
            public CButton Button;
            public int ProfileID;
        }

        public PartyScreenChallengeNames()
        {
        }

        protected override void Init()
        {
            base.Init();

            chooseAvatarStatic = GetNewStatic();
            chooseAvatarStatic.Visible = false;

            _ThemeName = "PartyScreenChallengeNames";
            List<string> buttons = new List<string>();
            _ThemeButtons = new string[] { ButtonBack, ButtonNext, ButtonPlayerDestination, ButtonPlayerChoose };
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
            AddButtonPlayerDestination();
            AddButtonPlayerChoose();
        }

        private void AddButtonPlayerDestination()
        {
            Buttons[htButtons(ButtonPlayerDestination)].Visible = false;
            PlayerDestinationButtons = new List<CButton>();
            int row = 0;
            int column = 0;
            for (int i = 1; i <= _PartyMode.GetMaxPlayer(); i++)
            {
                CButton b = GetNewButton(Buttons[htButtons(ButtonPlayerDestination)]);
                b.Rect.X = PlayerDestinationButtonsFirstX + column * (b.Rect.W + PlayerDestinationButtonsSpaceH);
                b.Rect.Y = PlayerDestinationButtonsFirstY + row * (b.Rect.H + PlayerDestinationButtonsSpaceW);
                PlayerDestinationButtons.Add(b);
                column++;
                if (column >= PlayerDestinationButtonsNumH)
                {
                    row++;
                    column = 0;
                }
                b.Visible = true;
                AddButton(b);
            }
        }

        private void AddButtonPlayerChoose()
        {
            Buttons[htButtons(ButtonPlayerChoose)].Visible = false;
            PlayerChooseButtons = new List<CPlayerChooseButton>();
            int row = 0;
            int column = 0;
            for (int i = 1; i <= PlayerChooseButtonsNumH*PlayerChooseButtonsNumW; i++)
            {
                CButton b = GetNewButton(Buttons[htButtons(ButtonPlayerChoose)]);
                b.Rect.X = PlayerChooseButtonsFirstX + column * (b.Rect.W + PlayerChooseButtonsSpaceH);
                b.Rect.Y = PlayerChooseButtonsFirstY + row * (b.Rect.H + PlayerChooseButtonsSpaceW);
                CPlayerChooseButton pcb = new CPlayerChooseButton();
                pcb.Button = b;
                pcb.ProfileID = -1;
                PlayerChooseButtons.Add(pcb);
                column++;
                if (column >= PlayerChooseButtonsNumH)
                {
                    row++;
                    column = 0;
                }
                b.Visible = true;
                AddButton(b);
            }
        }

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            base.HandleInput(KeyEvent);

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

                            for (int i = 0; i < PlayerDestinationButtons.Count; i++)
                            {
                                if (PlayerDestinationButtons[i].Selected)
                                {
                                }
                            }

                            for (int i = 0; i < PlayerChooseButtons.Count; i++)
                            {
                                if (PlayerChooseButtons[i].Button.Selected)
                                {
                                }
                            }
                            break;
                    }
                }
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
                for (int i = 0; i < PlayerChooseButtons.Count; i++)
                {
                    if (PlayerChooseButtons[i].Button.Selected)
                    {
                        SelectedPlayerNr = PlayerChooseButtons[i].ProfileID;
                        if (SelectedPlayerNr != -1)
                        {
                            //Activate mouse-selecting
                            SelectingMouseActive = true;
                            //Update of Drag/Drop-Texture
                            chooseAvatarStatic.Visible = true;
                            chooseAvatarStatic.Rect = PlayerChooseButtons[SelectedPlayerNr].Button.Rect;
                            chooseAvatarStatic.Rect.Z = -100;
                            chooseAvatarStatic.Color = new SColorF(1, 1, 1, 1);
                            chooseAvatarStatic.Texture = _Base.Profiles.GetProfiles()[SelectedPlayerNr].Avatar.Texture;
                        }
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
                    for (int i = 0; i < PlayerDestinationButtons.Count; i++)
                    {
                        //Check first, if area is "active"
                        if (PlayerDestinationButtons[i].Visible == true)
                        {
                            //Check if Mouse is in area
                            if (CHelper.IsInBounds(PlayerDestinationButtons[i].Rect, MouseEvent))
                            {
                                //Add Player-ID to list.
                                ProfileIDs[i] = SelectedPlayerNr;
                                UpdateButtonNext();
                                //TODO: Update texture and name
                                //Buttons[htButtons(PlayerButtons[i])]. = chooseAvatarStatic.Texture;
                                PlayerDestinationButtons[i].Text.Text = _Base.Profiles.GetProfiles()[SelectedPlayerNr].PlayerName;
                                //Update Tiles-List
                                
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

            for (int i = 0; i < PlayerDestinationButtons.Count; i++)
            {
                PlayerDestinationButtons[i].Text.Text = "Player " + i;
                if (i <= NumPlayers)
                    PlayerDestinationButtons[i].Visible = true;
                else
                    PlayerDestinationButtons[i].Visible = false;
            }

            UpdateButtonPlayerChoose();

            UpdateButtonNext();
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

        private void UpdateButtonPlayerChoose()
        {
            for (int i = (PlayerChooseButtonsOffset * PlayerChooseButtonsNumW); i < _Base.Profiles.GetProfiles().Length; i++)
            {
                if (i < PlayerChooseButtons.Count)
                {
                    PlayerChooseButtons[i].ProfileID = i;
                    PlayerChooseButtons[i].Button.Text.Text = _Base.Profiles.GetProfiles()[i].PlayerName;
                }
            }
        }

        private void UpdateButtonNext()
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
