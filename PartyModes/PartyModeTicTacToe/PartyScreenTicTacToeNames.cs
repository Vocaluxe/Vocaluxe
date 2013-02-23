using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Menu;

namespace Vocaluxe.PartyModes
{
    public class PartyScreenTicTacToeNames : CMenuParty
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        const string ButtonNext = "ButtonNext";
        const string ButtonBack = "ButtonBack";
        const string ButtonPlayerDestination = "ButtonPlayerDestination";
        const string ButtonPlayerChoose = "ButtonPlayerChoose";
        const string ButtonPlayerChooseScrollUp = "ButtonPlayerChooseScrollUp";
        const string ButtonPlayerChooseScrollDown = "ButtonPlayerChooseScrollDown";

        private List<CPlayerChooseButton> PlayerChooseButtons;
        private List<CButton> PlayerDestinationButtons;
        //PlayerDestinationButtons-Option
        private const int PlayerDestinationButtonsNumH = 10;
        private const int PlayerDestinationButtonsNumW = 2;
        private const int PlayerDestinationButtonsFirstX = 58;
        private const int PlayerDestinationButtonsFirstY = 380;
        private const int PlayerDestinationButtonsSpaceH = 15;
        private const int PlayerDestinationButtonsSpaceW = 25;
        //PlayerChooseButtons-Option
        private List<int> PlayerChooseButtonsVisibleProfiles;
        private const int PlayerChooseButtonsNumH = 10;
        private const int PlayerChooseButtonsNumW = 2;
        private const int PlayerChooseButtonsFirstX = 58;
        private const int PlayerChooseButtonsFirstY = 105;
        private const int PlayerChooseButtonsSpaceH = 15;
        private const int PlayerChooseButtonsSpaceW = 25;
        private int PlayerChooseButtonsOffset = 0;

        private CStatic chooseAvatarStatic;
        private bool SelectingMouseActive = false;
        private int OldMouseX = 0;
        private int OldMouseY = 0;
        private int SelectedPlayerNr = -1;
        private bool ButtonsAdded = false;

        private int NumPlayerTeam1 = 2;
        private int NumPlayerTeam2 = 2;
        
        private DataFromScreen Data;

        private class CPlayerChooseButton
        {
            public CButton Button;
            public int ProfileID;
        }

        public PartyScreenTicTacToeNames()
        {
        }

        protected override void Init()
        {
            base.Init();

            chooseAvatarStatic = GetNewStatic();
            chooseAvatarStatic.Visible = false;

            PlayerChooseButtonsVisibleProfiles = new List<int>();

            Data.ScreenNames.ProfileIDsTeam1 = new List<int>();
            Data.ScreenNames.ProfileIDsTeam2 = new List<int>();

            _ThemeName = "PartyScreenTicTacToeNames";
            List<string> buttons = new List<string>();
            _ThemeButtons = new string[] { ButtonBack, ButtonNext, ButtonPlayerDestination, ButtonPlayerChoose, ButtonPlayerChooseScrollUp, ButtonPlayerChooseScrollDown };
            _ScreenVersion = ScreenVersion;

            Data = new DataFromScreen();
            FromScreenNames names = new FromScreenNames();
            names.FadeToConfig = false;
            names.FadeToMain = false;
            names.ProfileIDsTeam1 = new List<int>();
            names.ProfileIDsTeam2 = new List<int>();
            Data.ScreenNames = names;
        }

        public override void LoadTheme(string XmlPath)
        {
			base.LoadTheme(XmlPath);
        }

        public override void DataToScreen(object ReceivedData)
        {
            DataToScreenNames config = new DataToScreenNames();

            try
            {
                config = (DataToScreenNames)ReceivedData;
                Data.ScreenNames.ProfileIDsTeam1 = config.ProfileIDsTeam1;
                Data.ScreenNames.ProfileIDsTeam2 = config.ProfileIDsTeam2;
                if (Data.ScreenNames.ProfileIDsTeam1 == null)
                    Data.ScreenNames.ProfileIDsTeam1 = new List<int>();
                if (Data.ScreenNames.ProfileIDsTeam2 == null)
                    Data.ScreenNames.ProfileIDsTeam2 = new List<int>();

                NumPlayerTeam1 = config.NumPlayerTeam1;
                NumPlayerTeam2 = config.NumPlayerTeam2;

                while (Data.ScreenNames.ProfileIDsTeam1.Count > NumPlayerTeam1)
                {
                    Data.ScreenNames.ProfileIDsTeam1.RemoveAt(Data.ScreenNames.ProfileIDsTeam1.Count - 1);
                }
                while (Data.ScreenNames.ProfileIDsTeam2.Count > NumPlayerTeam2)
                {
                    Data.ScreenNames.ProfileIDsTeam2.RemoveAt(Data.ScreenNames.ProfileIDsTeam2.Count - 1);
                }

                
            }
            catch (Exception e)
            {
                CBase.Log.LogError("Error in party mode screen TicTacToe names. Can't cast received data from game mode " + _ThemeName + ". " + e.Message); ;
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

                            if (Buttons[htButtons(ButtonPlayerChooseScrollUp)].Selected)
                                Scroll(-1);

                            if (Buttons[htButtons(ButtonPlayerChooseScrollDown)].Selected)
                                Scroll(1);

                            if (!OnAdd())
                                OnRemove();
                            break;

                        case Keys.Delete:
                            OnRemove();
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
                            chooseAvatarStatic.Rect = PlayerChooseButtons[i].Button.Rect;
                            chooseAvatarStatic.Rect.Z = -100;
                            chooseAvatarStatic.Color = new SColorF(1, 1, 1, 1);
                            chooseAvatarStatic.Texture = CBase.Profiles.GetProfiles()[SelectedPlayerNr].Avatar.Texture;
                        }
                    }
                }
                return true;
            }
            
            //Check if LeftButton is hold and Select-Mode active
            if (MouseEvent.LBH && SelectingMouseActive)
            {
                //Update coords for Drag/Drop-Texture
                chooseAvatarStatic.Rect.X += (MouseEvent.X - OldMouseX);
                chooseAvatarStatic.Rect.Y += (MouseEvent.Y - OldMouseY);
                OldMouseX = MouseEvent.X;
                OldMouseY = MouseEvent.Y;

                return true;
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
                                int added = -1;
                                //Add Player-ID to list.
                                if (i - PlayerDestinationButtonsNumH < 0)
                                {
                                    if (Data.ScreenNames.ProfileIDsTeam1.Count < (i + 1))
                                    {
                                        Data.ScreenNames.ProfileIDsTeam1.Add(SelectedPlayerNr);
                                        added = Data.ScreenNames.ProfileIDsTeam1.Count - 1;
                                    }
                                    else if (Data.ScreenNames.ProfileIDsTeam1.Count >= (i + 1))
                                    {
                                        Data.ScreenNames.ProfileIDsTeam1[i] = SelectedPlayerNr;
                                        added = i;
                                    }
                                }
                                else if (i - PlayerDestinationButtonsNumH >= 0)
                                {
                                    if (Data.ScreenNames.ProfileIDsTeam2.Count < ((i - PlayerDestinationButtonsNumH) + 1))
                                    {
                                        Data.ScreenNames.ProfileIDsTeam2.Add(SelectedPlayerNr);
                                        added = Data.ScreenNames.ProfileIDsTeam2.Count - 1 + PlayerDestinationButtonsNumH;
                                    }
                                    else if (Data.ScreenNames.ProfileIDsTeam2.Count >= ((i - PlayerDestinationButtonsNumH) + 1))
                                    {
                                        Data.ScreenNames.ProfileIDsTeam2[i - PlayerDestinationButtonsNumH] = SelectedPlayerNr;
                                        added = i;
                                    }
                                }
                                UpdateButtonNext();
                                //Update texture and name
                                PlayerDestinationButtons[added].Color = new SColorF(1, 1, 1, 0.6f);
                                PlayerDestinationButtons[added].SColor = new SColorF(1, 1, 1, 1);
                                PlayerDestinationButtons[added].Texture = chooseAvatarStatic.Texture;
                                PlayerDestinationButtons[added].STexture = chooseAvatarStatic.Texture;
                                PlayerDestinationButtons[added].Text.Text = CBase.Profiles.GetProfiles()[SelectedPlayerNr].PlayerName;
                                PlayerDestinationButtons[added].Enabled = true;
                                //Update Tiles-List
                                UpdateButtonPlayerChoose();
                            }
                        }
                    }
                    SelectedPlayerNr = -1;
                }
                //Reset variables
                SelectingMouseActive = false;
                chooseAvatarStatic.Visible = false;
                return true;
            }

            if (MouseEvent.LB && IsMouseOver(MouseEvent))
            {
                if (Buttons[htButtons(ButtonBack)].Selected)
                    Back();

                if (Buttons[htButtons(ButtonNext)].Selected)
                    Next();

                if (Buttons[htButtons(ButtonPlayerChooseScrollUp)].Selected)
                    Scroll(-1);

                if (Buttons[htButtons(ButtonPlayerChooseScrollDown)].Selected)
                    Scroll(1);
            }

            if(MouseEvent.LD && IsMouseOver(MouseEvent))
                if (!OnAdd())
                    OnRemove();

            if (MouseEvent.RB)
            {
                Back();
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            if (!ButtonsAdded)
            {
                AddButtonPlayerDestination();
                AddButtonPlayerChoose();
                ButtonsAdded = true;
            }

            UpdateButtonPlayerDestination();
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

        private void Scroll(int Offset)
        {
            if (Offset < 0 && PlayerChooseButtonsOffset > 0)
            {
                PlayerChooseButtonsOffset += Offset;
                UpdateButtonPlayerChoose();
            }
            else if (PlayerChooseButtonsVisibleProfiles.Count < PlayerChooseButtons.Count + (PlayerChooseButtonsOffset + Offset) * PlayerChooseButtonsNumH) 
            {
                PlayerChooseButtonsOffset += Offset;
                UpdateButtonPlayerChoose();
            }

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
                b.Enabled = false;
                AddButton(b);
            }
        }

        private void AddButtonPlayerChoose()
        {
            Buttons[htButtons(ButtonPlayerChoose)].Visible = false;
            PlayerChooseButtons = new List<CPlayerChooseButton>();
            int row = 0;
            int column = 0;
            for (int i = 1; i <= PlayerChooseButtonsNumH * PlayerChooseButtonsNumW; i++)
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
                b.Enabled = false;
                AddButton(b);
            }
        }

        private void UpdateButtonPlayerChoose()
        {
            UpdateVisibleProfiles();
            if ((PlayerChooseButtonsNumW * PlayerChooseButtonsNumH) * (PlayerChooseButtonsOffset + 1) - PlayerChooseButtonsVisibleProfiles.Count >= (PlayerChooseButtonsNumW * PlayerChooseButtonsNumH) * PlayerChooseButtonsOffset)
            {
                UpdateButtonPlayerChoose(PlayerChooseButtonsOffset - 1);
            }
            else
            {
                UpdateButtonPlayerChoose(PlayerChooseButtonsOffset);
            }
            Buttons[htButtons(ButtonPlayerChooseScrollUp)].Enabled = PlayerChooseButtonsOffset > 0;
            Buttons[htButtons(ButtonPlayerChooseScrollDown)].Enabled = PlayerChooseButtonsVisibleProfiles.Count > PlayerChooseButtons.Count + PlayerChooseButtonsOffset * PlayerChooseButtonsNumH;
        }

        private void UpdateButtonPlayerChoose(int Offset)
        {
            int NumButtonPlayerChoose = PlayerChooseButtonsNumW * PlayerChooseButtonsNumH;
            if (Offset < 0)
                Offset = 0;

            if (NumButtonPlayerChoose * (Offset + 1) - PlayerChooseButtonsVisibleProfiles.Count <= NumButtonPlayerChoose)
            {
                for (int i = 0; i < NumButtonPlayerChoose; i++)
                {
                    if ((i + Offset * NumButtonPlayerChoose) < PlayerChooseButtonsVisibleProfiles.Count)
                    {
                        PlayerChooseButtons[i].ProfileID = PlayerChooseButtonsVisibleProfiles[i + Offset * NumButtonPlayerChoose];
                        PlayerChooseButtons[i].Button.Text.Text = CBase.Profiles.GetProfiles()[PlayerChooseButtonsVisibleProfiles[i + Offset * NumButtonPlayerChoose]].PlayerName;
                        PlayerChooseButtons[i].Button.Texture = CBase.Profiles.GetProfiles()[PlayerChooseButtonsVisibleProfiles[i + Offset * NumButtonPlayerChoose]].Avatar.Texture;
                        PlayerChooseButtons[i].Button.STexture = CBase.Profiles.GetProfiles()[PlayerChooseButtonsVisibleProfiles[i + Offset * NumButtonPlayerChoose]].Avatar.Texture;
                        PlayerChooseButtons[i].Button.Color = new SColorF(1, 1, 1, 0.6f);
                        PlayerChooseButtons[i].Button.SColor = new SColorF(1, 1, 1, 1);
                        PlayerChooseButtons[i].Button.Enabled = true;
                    }
                    else
                    {
                        PlayerChooseButtons[i].ProfileID = -1;
                        PlayerChooseButtons[i].Button.Text.Text = String.Empty;
                        PlayerChooseButtons[i].Button.Texture = Buttons[htButtons(ButtonPlayerChoose)].Texture;
                        PlayerChooseButtons[i].Button.STexture = Buttons[htButtons(ButtonPlayerChoose)].STexture;
                        PlayerChooseButtons[i].Button.Color = Buttons[htButtons(ButtonPlayerChoose)].Color;
                        PlayerChooseButtons[i].Button.SColor = Buttons[htButtons(ButtonPlayerChoose)].SColor;
                        PlayerChooseButtons[i].Button.Enabled = false;
                    }
                }
            }
        }

        private void UpdateVisibleProfiles()
        {
            PlayerChooseButtonsVisibleProfiles.Clear();
            for (int i = 0; i < CBase.Profiles.GetProfiles().Length; i++)
            {
                bool visible = false;
                //Show profile only if active
                if (CBase.Profiles.GetProfiles()[i].Active == EOffOn.TR_CONFIG_ON)
                {
                    visible = true;

                    for (int p = 0; p < Data.ScreenNames.ProfileIDsTeam1.Count; p++)
                    {
                        //Don't show profile if is selected
                        if (Data.ScreenNames.ProfileIDsTeam1[p] == i)
                        {
                            visible = false;
                            break;
                        }
                    }

                    for (int p = 0; p < Data.ScreenNames.ProfileIDsTeam2.Count; p++)
                    {
                        //Don't show profile if is selected
                        if (Data.ScreenNames.ProfileIDsTeam2[p] == i)
                        {
                            visible = false;
                            break;
                        }
                    }
                }
                if (visible)
                {
                    PlayerChooseButtonsVisibleProfiles.Add(i);
                }
            }
        }

        private void UpdateButtonPlayerDestination()
        {
            for (int i = 0; i < PlayerDestinationButtons.Count / 2; i++)
            {
                if (NumPlayerTeam1 > i)
                {
                    PlayerDestinationButtons[i].Visible = true;
                    PlayerDestinationButtons[i].Enabled = true;
                }
                else
                {
                    PlayerDestinationButtons[i].Visible = false;
                    PlayerDestinationButtons[i].Enabled = false;
                }
            }
            for (int i = 0; i < PlayerDestinationButtons.Count / 2; i++)
            {
                if (NumPlayerTeam2 > i)
                {
                    PlayerDestinationButtons[i + PlayerDestinationButtonsNumH].Visible = true;
                    PlayerDestinationButtons[i + PlayerDestinationButtonsNumH].Enabled = true;
                }
                else
                {
                    PlayerDestinationButtons[i + PlayerDestinationButtonsNumH].Visible = false;
                    PlayerDestinationButtons[i + PlayerDestinationButtonsNumH].Enabled = false;
                }
            }
            for (int i = 0; i < NumPlayerTeam1; i++)
            {
                if (i < Data.ScreenNames.ProfileIDsTeam1.Count)
                {
                    if (Data.ScreenNames.ProfileIDsTeam1[i] != -1)
                    {
                        PlayerDestinationButtons[i].Color = new SColorF(1, 1, 1, 0.6f);
                        PlayerDestinationButtons[i].SColor = new SColorF(1, 1, 1, 1);
                        PlayerDestinationButtons[i].Texture = CBase.Profiles.GetProfiles()[Data.ScreenNames.ProfileIDsTeam1[i]].Avatar.Texture;
                        PlayerDestinationButtons[i].STexture = CBase.Profiles.GetProfiles()[Data.ScreenNames.ProfileIDsTeam1[i]].Avatar.Texture;
                        PlayerDestinationButtons[i].Text.Text = CBase.Profiles.GetProfiles()[Data.ScreenNames.ProfileIDsTeam1[i]].PlayerName;
                        PlayerDestinationButtons[i].Enabled = true;
                    }
                }
                else
                {
                    PlayerDestinationButtons[i].Color = Buttons[htButtons(ButtonPlayerDestination)].Color;
                    PlayerDestinationButtons[i].SColor = Buttons[htButtons(ButtonPlayerDestination)].SColor;
                    PlayerDestinationButtons[i].Texture = Buttons[htButtons(ButtonPlayerDestination)].Texture;
                    PlayerDestinationButtons[i].STexture = Buttons[htButtons(ButtonPlayerDestination)].STexture;
                    PlayerDestinationButtons[i].Text.Text = String.Empty;
                    PlayerDestinationButtons[i].Enabled = false;
                }
            }
            for (int i = 0; i < NumPlayerTeam2; i++)
            {
                if (i < Data.ScreenNames.ProfileIDsTeam2.Count)
                {
                    if (Data.ScreenNames.ProfileIDsTeam2[i] != -1)
                    {
                        PlayerDestinationButtons[i + PlayerDestinationButtonsNumH].Color = new SColorF(1, 1, 1, 0.6f);
                        PlayerDestinationButtons[i + PlayerDestinationButtonsNumH].SColor = new SColorF(1, 1, 1, 1);
                        PlayerDestinationButtons[i + PlayerDestinationButtonsNumH].Texture = CBase.Profiles.GetProfiles()[Data.ScreenNames.ProfileIDsTeam2[i]].Avatar.Texture;
                        PlayerDestinationButtons[i + PlayerDestinationButtonsNumH].STexture = CBase.Profiles.GetProfiles()[Data.ScreenNames.ProfileIDsTeam2[i]].Avatar.Texture;
                        PlayerDestinationButtons[i + PlayerDestinationButtonsNumH].Text.Text = CBase.Profiles.GetProfiles()[Data.ScreenNames.ProfileIDsTeam2[i]].PlayerName;
                        PlayerDestinationButtons[i + PlayerDestinationButtonsNumH].Enabled = true;
                    }
                }
                else
                {
                    PlayerDestinationButtons[i + PlayerDestinationButtonsNumH].Color = Buttons[htButtons(ButtonPlayerDestination)].Color;
                    PlayerDestinationButtons[i + PlayerDestinationButtonsNumH].SColor = Buttons[htButtons(ButtonPlayerDestination)].SColor;
                    PlayerDestinationButtons[i + PlayerDestinationButtonsNumH].Texture = Buttons[htButtons(ButtonPlayerDestination)].Texture;
                    PlayerDestinationButtons[i + PlayerDestinationButtonsNumH].STexture = Buttons[htButtons(ButtonPlayerDestination)].STexture;
                    PlayerDestinationButtons[i + PlayerDestinationButtonsNumH].Text.Text = String.Empty;
                    PlayerDestinationButtons[i + PlayerDestinationButtonsNumH].Enabled = false;
                }
            }
        }

        private void UpdateButtonNext()
        {
            if (Data.ScreenNames.ProfileIDsTeam1.Count == NumPlayerTeam1 && Data.ScreenNames.ProfileIDsTeam2.Count == NumPlayerTeam2)
            {
                Buttons[htButtons(ButtonNext)].Visible = true;
                SetInteractionToButton(Buttons[htButtons(ButtonNext)]);
            }
            else
                Buttons[htButtons(ButtonNext)].Visible = false;
        }

        private bool OnAdd()
        {
            for (int i = 0; i < PlayerChooseButtons.Count; i++)
            {
                if (PlayerChooseButtons[i].Button.Selected && PlayerChooseButtons[i].ProfileID != -1)
                {
                    if (Data.ScreenNames.ProfileIDsTeam1.Count < NumPlayerTeam1)
                    {
                        Data.ScreenNames.ProfileIDsTeam1.Add(PlayerChooseButtons[i].ProfileID);
                        int added = Data.ScreenNames.ProfileIDsTeam1.Count - 1;
                        UpdateButtonNext();
                        //Update texture and name
                        PlayerDestinationButtons[added].Color = new SColorF(1, 1, 1, 0.6f);
                        PlayerDestinationButtons[added].SColor = new SColorF(1, 1, 1, 1);
                        PlayerDestinationButtons[added].Texture = CBase.Profiles.GetProfiles()[PlayerChooseButtons[i].ProfileID].Avatar.Texture;
                        PlayerDestinationButtons[added].STexture = CBase.Profiles.GetProfiles()[PlayerChooseButtons[i].ProfileID].Avatar.Texture;
                        PlayerDestinationButtons[added].Text.Text = CBase.Profiles.GetProfiles()[PlayerChooseButtons[i].ProfileID].PlayerName;
                        PlayerDestinationButtons[added].Enabled = true;
                        //Update Tiles-List
                        UpdateButtonPlayerChoose();
                        CheckInteraction();
                        return true;
                    }
                    else if (Data.ScreenNames.ProfileIDsTeam2.Count < NumPlayerTeam2)
                    {
                        Data.ScreenNames.ProfileIDsTeam2.Add(PlayerChooseButtons[i].ProfileID);
                        int added = (Data.ScreenNames.ProfileIDsTeam2.Count - 1) + PlayerDestinationButtonsNumH;
                        UpdateButtonNext();
                        //Update texture and name
                        PlayerDestinationButtons[added].Color = new SColorF(1, 1, 1, 0.6f);
                        PlayerDestinationButtons[added].SColor = new SColorF(1, 1, 1, 1);
                        PlayerDestinationButtons[added].Texture = CBase.Profiles.GetProfiles()[PlayerChooseButtons[i].ProfileID].Avatar.Texture;
                        PlayerDestinationButtons[added].STexture = CBase.Profiles.GetProfiles()[PlayerChooseButtons[i].ProfileID].Avatar.Texture;
                        PlayerDestinationButtons[added].Text.Text = CBase.Profiles.GetProfiles()[PlayerChooseButtons[i].ProfileID].PlayerName;
                        PlayerDestinationButtons[added].Enabled = true;
                        //Update Tiles-List
                        UpdateButtonPlayerChoose();
                        CheckInteraction();
                        return true;
                    }
                }
            }
            return false;
        }

        private bool OnRemove()
        {
            for (int i = 0; i < PlayerDestinationButtonsNumH; i++)
            {
                if (PlayerDestinationButtons[i].Selected)
                {
                    if ((i + 1) <= Data.ScreenNames.ProfileIDsTeam1.Count)
                    {
                        Data.ScreenNames.ProfileIDsTeam1.RemoveAt(i);
                        UpdateButtonNext();
                        UpdateButtonPlayerDestination();
                        UpdateButtonPlayerChoose();
                        CheckInteraction();
                        return true;
                    }
                }
            }
            for (int i = PlayerDestinationButtonsNumH; i < PlayerDestinationButtonsNumH*2; i++)
            {
                if (PlayerDestinationButtons[i].Selected)
                {
                    if (((i-PlayerDestinationButtonsNumH) + 1) <= Data.ScreenNames.ProfileIDsTeam2.Count)
                    {
                        Data.ScreenNames.ProfileIDsTeam2.RemoveAt(i - PlayerDestinationButtonsNumH);
                        UpdateButtonNext();
                        UpdateButtonPlayerDestination();
                        UpdateButtonPlayerChoose();
                        CheckInteraction();
                        return true;
                    }
                }
            }
            return false;
        }

        private void Back()
        {
            Data.ScreenNames.FadeToConfig = true;
            Data.ScreenNames.FadeToMain = false;
            _PartyMode.DataFromScreen(_ThemeName, Data);
        }

        private void Next()
        {
            Data.ScreenNames.FadeToConfig = false;
            Data.ScreenNames.FadeToMain = true;
            _PartyMode.DataFromScreen(_ThemeName, Data);
        }
    }
}
