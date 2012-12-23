using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Menu;

namespace Vocaluxe.PartyModes
{
    struct TableRow
    {
        public CText Pos;
        public CText Name;
        public CText Rounds;
        public CText Won;
        public CText SingPoints;
        public CText GamePoints;
    }

    class RoundsTableRow
    {
        public CText Number;
        public List<CText> TextPlayer;
        public List<CText> TextScores;
    }


    public class PartyScreenChallengeMain : CMenuParty
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        const string TextPosition = "TextPosition";
        const string TextPlayerName = "TextPlayerName";
        const string TextNumPlayed = "TextNumPlayed";
        const string TextWon = "TextWon";
        const string TextSingPoints = "TextSingPoints";
        const string TextGamePoints = "TextGamePoints";
        const string TextNextPlayer = "TextNextPlayer";
        const string TextPopupReallyExit = "TextPopupReallyExit";
        const string TextRoundPlayer = "TextRoundPlayer";
        const string TextRoundScore = "TextRoundScore";
        const string TextRoundNumber = "TextRoundNumber";

        const string ButtonNextRound = "ButtonNextRound";
        const string ButtonBack = "ButtonBack";
        const string ButtonExit = "ButtonExit";
        const string ButtonPopupYes = "ButtonPopupYes";
        const string ButtonPopupNo = "ButtonPopupNo";

        const string StaticPopupBG = "StaticPopupBG";
        const string StaticNextPlayer = "StaticNextPlayer";

        private bool ExitPopupVisible = false;

        private DataFromScreen Data;
        private DataToScreenMain GameState;
        private List<TableRow> Table;
        private List<RoundsTableRow> RoundsTable;

        private List<CText> NextPlayerTexts;
        private List<CStatic> NextPlayerStatics;

        private int RoundsTableOffset = 0;

        public PartyScreenChallengeMain()
        {
            Data = new DataFromScreen();
            Data.ScreenMain = new FromScreenMain();
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "PartyScreenChallengeMain";
            _ThemeTexts = new string[] { TextPosition, TextPlayerName, TextNumPlayed, TextWon, TextSingPoints, TextGamePoints, TextNextPlayer, TextPopupReallyExit, TextRoundNumber, TextRoundPlayer, TextRoundScore };
            _ThemeButtons = new string[] { ButtonNextRound, ButtonBack, ButtonExit, ButtonPopupYes, ButtonPopupNo };
            _ThemeStatics = new string[] { StaticPopupBG, StaticNextPlayer };
            _ScreenVersion = ScreenVersion;
        }

        public override void LoadTheme(string XmlPath)
        {
			base.LoadTheme(XmlPath);

            GameState = new DataToScreenMain();
            BuildTable();

            NextPlayerTexts = new List<CText>();
            NextPlayerStatics = new List<CStatic>();

            for (int i = 0; i < _PartyMode.GetMaxPlayer(); i++)
            {
                NextPlayerTexts.Add(GetNewText(Texts[htTexts(TextNextPlayer)]));
                AddText(NextPlayerTexts[NextPlayerTexts.Count - 1]);
                NextPlayerStatics.Add(GetNewStatic(Statics[htStatics(StaticNextPlayer)]));
                AddStatic(NextPlayerStatics[NextPlayerStatics.Count - 1]);
            }
        }

        public override void DataToScreen(object ReceivedData)
        {
            DataToScreenMain data = new DataToScreenMain();

            try
            {
                data = (DataToScreenMain)ReceivedData;
                GameState = data;
            }
            catch (Exception e)
            {
                _Base.Log.LogError("Error in party mode screen challenge main. Can't cast received data from game mode " + _ThemeName + ". " + e.Message); ;
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
                        if (!ExitPopupVisible)
                            if (GameState.CurrentRoundNr == 1)
                                Back();
                            else
                                ShowPopup(true);
                        else
                            ShowPopup(false);
                        break;

                    case Keys.Enter:
                        if (!ExitPopupVisible)
                        {
                            if (Buttons[htButtons(ButtonNextRound)].Selected)
                                NextRound();
                            if (Buttons[htButtons(ButtonBack)].Selected && GameState.CurrentRoundNr == 1)
                                Back();
                            if (Buttons[htButtons(ButtonExit)].Selected && GameState.CurrentRoundNr > 1)
                                ShowPopup(true);
                        }
                        else
                        {
                            if (Buttons[htButtons(ButtonPopupYes)].Selected)
                                EndParty();
                            if (Buttons[htButtons(ButtonPopupNo)].Selected)
                                ShowPopup(false);
                        }
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(MouseEvent MouseEvent)
        {
            base.HandleMouse(MouseEvent);

            if (MouseEvent.LB && IsMouseOver(MouseEvent))
            {
                if (!ExitPopupVisible)
                {
                    if (Buttons[htButtons(ButtonNextRound)].Selected)
                        NextRound();
                    if (Buttons[htButtons(ButtonBack)].Selected && GameState.CurrentRoundNr == 1)
                        Back();
                    if (Buttons[htButtons(ButtonExit)].Selected && GameState.CurrentRoundNr > 1)
                        ShowPopup(true);
                }
                else
                {
                    if (Buttons[htButtons(ButtonPopupYes)].Selected)
                        EndParty();
                    if (Buttons[htButtons(ButtonPopupNo)].Selected)
                        ShowPopup(false);
                }
            }

            if (MouseEvent.RB)
            {
                if (!ExitPopupVisible)
                    if (GameState.CurrentRoundNr == 1)
                        Back();
                    else
                        ShowPopup(true);
                else
                    ShowPopup(false);
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            Updatetable();
            UpdateNextPlayerPositions();
            UpdateNextPlayerContents();
            BuildRoundsTable();
            UpdateRoundsTable(RoundsTableOffset);

            if (GameState.CurrentRoundNr == 1)
            {
                Buttons[htButtons(ButtonBack)].Visible = true;
                Buttons[htButtons(ButtonExit)].Visible = false;
            }
            else
            {
                Buttons[htButtons(ButtonBack)].Visible = false;
                Buttons[htButtons(ButtonExit)].Visible = true;
            }

            SetInteractionToButton(Buttons[htButtons(ButtonNextRound)]);

            ShowPopup(false);
        }

        public override bool UpdateGame()
        {
            return true;
        }

        public override bool Draw()
        {
            base.Draw();
            return true;
        }

        public override void OnClose()
        {
            base.OnClose();
        }

        private void NextRound()
        {
            Data.ScreenMain.FadeToNameSelection = false;
            Data.ScreenMain.FadeToSongSelection = true;
            _PartyMode.DataFromScreen(_ThemeName, Data);
        }

        private void EndParty()
        {
            FadeTo(EScreens.ScreenParty);
        }

        private void ShowPopup(bool Visible)
        {
            ExitPopupVisible = Visible;

            Statics[htStatics(StaticPopupBG)].Visible = ExitPopupVisible;
            Texts[htTexts(TextPopupReallyExit)].Visible = ExitPopupVisible;
            Buttons[htButtons(ButtonPopupYes)].Visible = ExitPopupVisible;
            Buttons[htButtons(ButtonPopupNo)].Visible = ExitPopupVisible;

            if (ExitPopupVisible)
                SetInteractionToButton(Buttons[htButtons(ButtonPopupNo)]);
        }

        private void Back()
        {
            Data.ScreenMain.FadeToNameSelection = true;
            Data.ScreenMain.FadeToSongSelection = false;
            _PartyMode.DataFromScreen(_ThemeName, Data);
        }

        private void UpdateNextPlayerPositions()
        {
            float x = _Base.Settings.GetRenderW()/2 - ((GameState.NumPlayerAtOnce * Statics[htStatics(StaticNextPlayer)].Rect.W) + ((GameState.NumPlayerAtOnce-1) * 15))/2;
            float static_y = 590;
            float text_y = 550;
            for (int i = 0; i < GameState.NumPlayerAtOnce; i++)
            {
                //static
                NextPlayerStatics[i].Rect.X = x;
                NextPlayerStatics[i].Rect.Y = static_y;
                NextPlayerStatics[i].Visible = true;
                //text
                NextPlayerTexts[i].X = x + Statics[htStatics(StaticNextPlayer)].Rect.W / 2;
                NextPlayerTexts[i].Y = text_y;
                NextPlayerTexts[i].Visible = true;

                x += Statics[htStatics(StaticNextPlayer)].Rect.W + 15;
            }
            for (int i = GameState.NumPlayerAtOnce; i < _PartyMode.GetMaxPlayer(); i++)
            {
                NextPlayerStatics[i].Visible = false;
                NextPlayerTexts[i].Visible = false;
            }
        }

        private void UpdateNextPlayerContents()
        {
            if (GameState.CurrentRoundNr <= GameState.Combs.Count)
            {
                SProfile[] profiles = _Base.Profiles.GetProfiles();
                for (int i = 0; i < GameState.NumPlayerAtOnce; i++)
                {
                    int pid = GameState.Combs[GameState.CurrentRoundNr - 1].Player[i];
                    NextPlayerStatics[i].Texture = profiles[GameState.ProfileIDs[pid]].Avatar.Texture;
                    NextPlayerTexts[i].Text = profiles[GameState.ProfileIDs[pid]].PlayerName;
                    NextPlayerTexts[i].Color = _Base.Theme.GetPlayerColor((i + 1));
                }
            }
            else
            {
                for (int i = 0; i < GameState.NumPlayerAtOnce; i++)
                {
                    NextPlayerStatics[i].Visible = false;
                    NextPlayerTexts[i].Visible = false;
                }
            }
        }

        private void BuildRoundsTable()
        {
            int NumRoundsVisible = 3;
            int NumPlayerInOneRow = 3;
            if (GameState.NumPlayerAtOnce <= NumPlayerInOneRow)
                NumRoundsVisible = 5;
            if (NumRoundsVisible > GameState.Combs.Count)
                NumRoundsVisible = GameState.Combs.Count;

            float x = Texts[htTexts(TextRoundNumber)].X;
            float y = Texts[htTexts(TextRoundNumber)].Y;

            float delta = Texts[htTexts(TextRoundNumber)].Height;

            //Create lists
            RoundsTable = new List<RoundsTableRow>();
            for (int i = 0; i < NumRoundsVisible; i++)
            {
                RoundsTableRow rtr = new RoundsTableRow();
                rtr.TextPlayer = new List<CText>();
                rtr.TextScores = new List<CText>();
                RoundsTable.Add(rtr);
            }
            //Create statics and texts for rounds
            for (int round = 0; round < RoundsTable.Count; round++)
            {
                //Round-number
                CText text = GetNewText(Texts[htTexts(TextRoundNumber)]);
                text.X = x;
                text.Y = y;
                text.Text = (round + 1) + ")";
                AddText(text);
                RoundsTable[round].Number = text;
                int NumInnerRows = (int) Math.Ceiling(GameState.NumPlayerAtOnce / ((double)NumPlayerInOneRow));
                for (int row = 0; row < NumInnerRows; row++)
                {
                    int num = (row + 1) * NumPlayerInOneRow;
                    int NumPlayerInThisRow = NumPlayerInOneRow;
                    if (num > GameState.NumPlayerAtOnce)
                    {
                        num = GameState.NumPlayerAtOnce;
                        NumPlayerInThisRow = GameState.NumPlayerAtOnce - (row * NumPlayerInOneRow);
                    }

                    for (int column = row * NumPlayerInOneRow; column < num; column++)
                    {
                        //Player
                        float _x = x + 15 + (_Base.Settings.GetRenderW() - Texts[htTexts(TextRoundNumber)].X - 20) / NumPlayerInThisRow * (column - row * NumPlayerInOneRow) + ((_Base.Settings.GetRenderW() - Texts[htTexts(TextRoundNumber)].X - 20) / NumPlayerInThisRow) / 2;
                        float maxw = ((_Base.Settings.GetRenderW() - Texts[htTexts(TextRoundNumber)].X - 20) / NumPlayerInThisRow) - 5;
                        text = GetNewText(Texts[htTexts(TextRoundNumber)]);
                        text.X = _x;
                        text.Y = y;
                        text.MaxWidth = maxw;
                        AddText(text);
                        RoundsTable[round].TextPlayer.Add(text);
                        //Score
                        text = GetNewText(Texts[htTexts(TextRoundNumber)]);
                        text.X = _x;
                        text.Y = y + delta;
                        text.MaxWidth = maxw;
                        AddText(text);
                        RoundsTable[round].TextScores.Add(text);
                    }
                    y = y + delta + delta;
                }
                y = y + delta/2;
            }
        }

        private void UpdateRoundsTable(int Offset)
        {
            SProfile[] profile = _Base.Profiles.GetProfiles();
            for (int i = 0; i < RoundsTable.Count; i++)
            {
                for(int p = 0; p < RoundsTable[i].TextPlayer.Count; p++)
                {
                    int pID = GameState.ProfileIDs[GameState.Combs[i + Offset].Player[p]];
                    RoundsTable[i].TextPlayer[p].Text = profile[pID].PlayerName;
                }
            }
        }

        private void BuildTable()
        {
            Table = new List<TableRow>();
            float delta = Texts[htTexts(TextPosition)].Height * 1.2f;

            for (int i = 0; i < _PartyMode.GetMaxPlayer(); i++)
            {
                TableRow row = new TableRow();

                row.Pos = GetNewText(Texts[htTexts(TextPosition)]);
                row.Name = GetNewText(Texts[htTexts(TextPlayerName)]);
                row.Rounds = GetNewText(Texts[htTexts(TextNumPlayed)]);
                row.Won = GetNewText(Texts[htTexts(TextWon)]);
                row.SingPoints = GetNewText(Texts[htTexts(TextSingPoints)]);
                row.GamePoints = GetNewText(Texts[htTexts(TextGamePoints)]);

                row.Pos.Y += delta * (i + 1);
                row.Name.Y += delta * (i + 1);
                row.Rounds.Y += delta * (i + 1);
                row.Won.Y += delta * (i + 1);
                row.SingPoints.Y += delta * (i + 1);
                row.GamePoints.Y += delta * (i + 1);

                row.Pos.Text = (i + 1).ToString() + ".";

                row.Pos.Visible = false;
                row.Name.Visible = false;
                row.Rounds.Visible = false;
                row.Won.Visible = false;
                row.SingPoints.Visible = false;
                row.GamePoints.Visible = false;

                AddText(row.Pos);
                AddText(row.Name);
                AddText(row.Rounds);
                AddText(row.Won);
                AddText(row.SingPoints);
                AddText(row.GamePoints);

                Table.Add(row);
            }
        }

        private void Updatetable()
        {
            SProfile[] profiles = _Base.Profiles.GetProfiles();

            for (int i = 0; i < Table.Count; i++)
            {
                TableRow row = Table[i];

                if (i < GameState.ResultTable.Count)
                {
                    row.Pos.Visible = true;
                    row.Name.Visible = true;
                    row.Rounds.Visible = true;
                    row.Won.Visible = true;
                    row.SingPoints.Visible = true;
                    row.GamePoints.Visible = true;

                    row.Name.Text = profiles[GameState.ResultTable[i].PlayerID].PlayerName;
                    row.Rounds.Text = GameState.ResultTable[i].NumRounds.ToString();
                    row.Won.Text = GameState.ResultTable[i].NumWon.ToString();
                    row.SingPoints.Text = GameState.ResultTable[i].SumSingPoints.ToString();
                    row.GamePoints.Text = GameState.ResultTable[i].NumGamePoints.ToString();
                }
                else
                {
                    row.Pos.Visible = false;
                    row.Name.Visible = false;
                    row.Rounds.Visible = false;
                    row.Won.Visible = false;
                    row.SingPoints.Visible = false;
                    row.GamePoints.Visible = false;
                }
            }
        }
    }
}
