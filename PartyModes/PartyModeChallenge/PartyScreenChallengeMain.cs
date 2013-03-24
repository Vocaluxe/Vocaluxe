using System;
using System.Collections.Generic;
using System.Windows.Forms;
using VocaluxeLib.Menu;

namespace VocaluxeLib.PartyModes.Challenge
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
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string TextPosition = "TextPosition";
        private const string TextPlayerName = "TextPlayerName";
        private const string TextNumPlayed = "TextNumPlayed";
        private const string TextWon = "TextWon";
        private const string TextSingPoints = "TextSingPoints";
        private const string TextGamePoints = "TextGamePoints";
        private const string TextNextPlayer = "TextNextPlayer";
        private const string TextPopupReallyExit = "TextPopupReallyExit";
        private const string TextRoundPlayer = "TextRoundPlayer";
        private const string TextRoundScore = "TextRoundScore";
        private const string TextRoundNumber = "TextRoundNumber";
        private const string TextFinishMessage = "TextFinishMessage";
        private const string TextFinishPlayerWin = "TextFinishPlayerWin";
        private const string TextNextPlayerMessage = "TextNextPlayerMessage";

        private const string ButtonNextRound = "ButtonNextRound";
        private const string ButtonBack = "ButtonBack";
        private const string ButtonExit = "ButtonExit";
        private const string ButtonPopupYes = "ButtonPopupYes";
        private const string ButtonPopupNo = "ButtonPopupNo";
        private const string ButtonPlayerScrollUp = "ButtonPlayerScrollUp";
        private const string ButtonPlayerScrollDown = "ButtonPlayerScrollDown";
        private const string ButtonRoundsScrollUp = "ButtonRoundsScrollUp";
        private const string ButtonRoundsScrollDown = "ButtonRoundsScrollDown";

        private const string StaticPopupBG = "StaticPopupBG";
        private const string StaticNextPlayer = "StaticNextPlayer";

        private bool ExitPopupVisible;

        private DataFromScreen Data;
        private DataToScreenMain GameState;
        private List<TableRow> PlayerTable;
        private List<RoundsTableRow> RoundsTable;

        private List<CText> NextPlayerTexts;
        private List<CStatic> NextPlayerStatics;

        private SRectF RoundsTableScrollArea;
        private SRectF PlayerTableScrollArea;
        private int RoundsTableOffset;
        private int PlayerTableOffset;
        private int NumPlayerVisible = 10;
        private int NumRoundsVisible = 3;

        public PartyScreenChallengeMain()
        {
            Data = new DataFromScreen();
            Data.ScreenMain = new FromScreenMain();
        }

        public override void Init()
        {
            base.Init();

            _ThemeTexts = new string[]
                {
                    TextPosition, TextPlayerName, TextNumPlayed, TextWon, TextSingPoints, TextGamePoints, TextNextPlayer, TextPopupReallyExit, TextRoundNumber, TextRoundPlayer,
                    TextRoundScore, TextFinishMessage, TextFinishPlayerWin, TextNextPlayerMessage
                };
            _ThemeButtons = new string[]
                {ButtonNextRound, ButtonBack, ButtonExit, ButtonPopupYes, ButtonPopupNo, ButtonPlayerScrollDown, ButtonPlayerScrollUp, ButtonRoundsScrollDown, ButtonRoundsScrollUp};
            _ThemeStatics = new string[] {StaticPopupBG, StaticNextPlayer};
        }

        public override void LoadTheme(string XmlPath)
        {
            base.LoadTheme(XmlPath);

            GameState = new DataToScreenMain();
            BuildPlayerTable();
            CreateRoundsTable();
            NextPlayerTexts = new List<CText>();
            NextPlayerStatics = new List<CStatic>();

            for (int i = 0; i < _PartyMode.GetMaxPlayer(); i++)
            {
                NextPlayerTexts.Add(GetNewText(Texts[TextNextPlayer]));
                AddText(NextPlayerTexts[NextPlayerTexts.Count - 1]);
                NextPlayerStatics.Add(GetNewStatic(Statics[StaticNextPlayer]));
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
                CBase.Log.LogError("Error in party mode screen challenge main. Can't cast received data from game mode " + ThemeName + ". " + e.Message);
            }
        }

        public override bool HandleInput(KeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed) {}
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Back:
                    case Keys.Escape:
                        if (!ExitPopupVisible)
                        {
                            if (GameState.CurrentRoundNr == 1)
                                Back();
                            else
                                ShowPopup(true);
                        }
                        else
                            ShowPopup(false);
                        break;

                    case Keys.Enter:
                        if (!ExitPopupVisible)
                        {
                            if (Buttons[ButtonNextRound].Selected)
                                NextRound();
                            if (Buttons[ButtonBack].Selected && GameState.CurrentRoundNr == 1)
                                Back();
                            if (Buttons[ButtonExit].Selected && GameState.CurrentRoundNr > 1)
                                ShowPopup(true);
                            if (Buttons[ButtonPlayerScrollUp].Selected)
                                ScrollPlayerTable(-1);
                            if (Buttons[ButtonPlayerScrollDown].Selected)
                                ScrollPlayerTable(1);
                            if (Buttons[ButtonRoundsScrollUp].Selected)
                                ScrollRoundsTable(-1);
                            if (Buttons[ButtonRoundsScrollDown].Selected)
                                ScrollRoundsTable(1);
                        }
                        else
                        {
                            if (Buttons[ButtonPopupYes].Selected)
                                EndParty();
                            if (Buttons[ButtonPopupNo].Selected)
                                ShowPopup(false);
                        }
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(MouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.LB && IsMouseOver(mouseEvent))
            {
                if (!ExitPopupVisible)
                {
                    if (Buttons[ButtonNextRound].Selected)
                        NextRound();
                    if (Buttons[ButtonBack].Selected && GameState.CurrentRoundNr == 1)
                        Back();
                    if (Buttons[ButtonExit].Selected && GameState.CurrentRoundNr > 1)
                        ShowPopup(true);
                    if (Buttons[ButtonPlayerScrollUp].Selected)
                        ScrollPlayerTable(-1);
                    if (Buttons[ButtonPlayerScrollDown].Selected)
                        ScrollPlayerTable(1);
                    if (Buttons[ButtonRoundsScrollUp].Selected)
                        ScrollRoundsTable(-1);
                    if (Buttons[ButtonRoundsScrollDown].Selected)
                        ScrollRoundsTable(1);
                }
                else
                {
                    if (Buttons[ButtonPopupYes].Selected)
                        EndParty();
                    if (Buttons[ButtonPopupNo].Selected)
                        ShowPopup(false);
                }
            }

            if (mouseEvent.RB)
            {
                if (!ExitPopupVisible)
                {
                    if (GameState.CurrentRoundNr == 1)
                        Back();
                    else
                        ShowPopup(true);
                }
                else
                    ShowPopup(false);
            }

            if (mouseEvent.Wheel != 0)
            {
                if (CHelper.IsInBounds(RoundsTableScrollArea, mouseEvent))
                    ScrollRoundsTable(mouseEvent.Wheel);
                if (CHelper.IsInBounds(PlayerTableScrollArea, mouseEvent))
                    ScrollPlayerTable(mouseEvent.Wheel);
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            PlayerTableOffset = 0;
            RoundsTableOffset = 0;

            UpdatePlayerTable();
            UpdateNextPlayerPositions();
            UpdateNextPlayerContents();
            if (GameState.CurrentRoundNr == 1)
                BuildRoundsTable();
            else
                ScrollRoundsTable(GameState.CurrentRoundNr - 2);
            UpdateRoundsTable();

            if (GameState.CurrentRoundNr == 1)
            {
                Buttons[ButtonBack].Visible = true;
                Buttons[ButtonExit].Visible = false;
            }
            else
            {
                Buttons[ButtonBack].Visible = false;
                Buttons[ButtonExit].Visible = true;
            }

            if (GameState.CurrentRoundNr <= GameState.Combs.Count)
            {
                Buttons[ButtonNextRound].Visible = true;
                Texts[TextFinishMessage].Visible = false;
                Texts[TextFinishPlayerWin].Visible = false;
                SetInteractionToButton(Buttons[ButtonNextRound]);
            }
            else
            {
                Buttons[ButtonNextRound].Visible = false;
                Texts[TextFinishMessage].Visible = true;
                Texts[TextFinishPlayerWin].Visible = true;
                Texts[TextFinishPlayerWin].Text = GetPlayerWinString();
                SetInteractionToButton(Buttons[ButtonExit]);
            }

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

        private void NextRound()
        {
            Data.ScreenMain.FadeToNameSelection = false;
            Data.ScreenMain.FadeToSongSelection = true;
            _PartyMode.DataFromScreen(ThemeName, Data);
        }

        private void EndParty()
        {
            FadeTo(EScreens.ScreenParty);
        }

        private void ShowPopup(bool Visible)
        {
            ExitPopupVisible = Visible;

            Statics[StaticPopupBG].Visible = ExitPopupVisible;
            Texts[TextPopupReallyExit].Visible = ExitPopupVisible;
            Buttons[ButtonPopupYes].Visible = ExitPopupVisible;
            Buttons[ButtonPopupNo].Visible = ExitPopupVisible;

            if (ExitPopupVisible)
                SetInteractionToButton(Buttons[ButtonPopupNo]);
        }

        private void Back()
        {
            Data.ScreenMain.FadeToNameSelection = true;
            Data.ScreenMain.FadeToSongSelection = false;
            _PartyMode.DataFromScreen(ThemeName, Data);
        }

        private void UpdateNextPlayerPositions()
        {
            float x = CBase.Settings.GetRenderW() / 2 - ((GameState.NumPlayerAtOnce * Statics[StaticNextPlayer].Rect.W) + ((GameState.NumPlayerAtOnce - 1) * 15)) / 2;
            float static_y = 590;
            float text_y = 550;
            for (int i = 0; i < GameState.NumPlayerAtOnce; i++)
            {
                //static
                NextPlayerStatics[i].Rect.X = x;
                NextPlayerStatics[i].Rect.Y = static_y;
                NextPlayerStatics[i].Visible = true;
                //text
                NextPlayerTexts[i].X = x + Statics[StaticNextPlayer].Rect.W / 2;
                NextPlayerTexts[i].Y = text_y;
                NextPlayerTexts[i].Visible = true;

                x += Statics[StaticNextPlayer].Rect.W + 15;
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
                Texts[TextNextPlayerMessage].Visible = true;
                SProfile[] profiles = CBase.Profiles.GetProfiles();
                for (int i = 0; i < GameState.NumPlayerAtOnce; i++)
                {
                    int pid = GameState.Combs[GameState.CurrentRoundNr - 1].Player[i];
                    NextPlayerStatics[i].Texture = profiles[GameState.ProfileIDs[pid]].Avatar.Texture;
                    NextPlayerTexts[i].Text = profiles[GameState.ProfileIDs[pid]].PlayerName;
                    NextPlayerTexts[i].Color = CBase.Theme.GetPlayerColor(i + 1);
                }
            }
            else
            {
                Texts[TextNextPlayerMessage].Visible = false;
                for (int i = 0; i < GameState.NumPlayerAtOnce; i++)
                {
                    NextPlayerStatics[i].Visible = false;
                    NextPlayerTexts[i].Visible = false;
                }
            }
        }

        private void CreateRoundsTable()
        {
            //Create lists
            RoundsTable = new List<RoundsTableRow>();
            for (int i = 0; i < 5; i++)
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
                CText text = GetNewText(Texts[TextRoundNumber]);
                AddText(text);
                RoundsTable[round].Number = text;
                for (int row = 0; row < 2; row++)
                {
                    for (int column = 0; column < 3; column++)
                    {
                        //Player
                        text = GetNewText(Texts[TextRoundPlayer]);
                        AddText(text);
                        RoundsTable[round].TextPlayer.Add(text);
                        //Score
                        text = GetNewText(Texts[TextRoundScore]);
                        AddText(text);
                        RoundsTable[round].TextScores.Add(text);
                    }
                }
            }
        }

        private void BuildRoundsTable()
        {
            RoundsTableScrollArea = new SRectF();

            int NumPlayerInOneRow = 3;
            if (GameState.NumPlayerAtOnce <= NumPlayerInOneRow)
                NumRoundsVisible = 5;
            else
                NumRoundsVisible = 3;

            if (NumRoundsVisible > GameState.Combs.Count)
                NumRoundsVisible = GameState.Combs.Count;

            float x = Texts[TextRoundNumber].X;
            float y = Texts[TextRoundNumber].Y;

            RoundsTableScrollArea.X = x;
            RoundsTableScrollArea.Y = y;
            RoundsTableScrollArea.W = CBase.Settings.GetRenderW() - Texts[TextRoundNumber].X - 20;

            float delta = Texts[TextRoundNumber].Height;

            //Update statics and texts for rounds
            for (int round = 0; round < RoundsTable.Count; round++)
            {
                //Round-number
                RoundsTable[round].Number.X = x;
                RoundsTable[round].Number.Y = y;
                int NumInnerRows = (int)Math.Ceiling(GameState.NumPlayerAtOnce / ((double)NumPlayerInOneRow));
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
                        float _x = x + 15 + (CBase.Settings.GetRenderW() - Texts[TextRoundNumber].X - 20) / NumPlayerInThisRow * (column - row * NumPlayerInOneRow) +
                                   ((CBase.Settings.GetRenderW() - Texts[TextRoundNumber].X - 20) / NumPlayerInThisRow) / 2;
                        float maxw = ((CBase.Settings.GetRenderW() - Texts[TextRoundNumber].X - 20) / NumPlayerInThisRow) / 2 - 5;
                        RoundsTable[round].TextPlayer[column].X = _x;
                        RoundsTable[round].TextPlayer[column].Y = y;
                        RoundsTable[round].TextPlayer[column].MaxWidth = maxw;
                        //Score
                        RoundsTable[round].TextScores[column] = RoundsTable[round].TextScores[column];
                        RoundsTable[round].TextScores[column].X = _x;
                        RoundsTable[round].TextScores[column].Y = y + delta;
                        RoundsTable[round].TextScores[column].MaxWidth = maxw;
                    }
                    y = y + delta + delta;
                }
                y = y + delta / 2;
            }
            RoundsTableScrollArea.H = y - RoundsTableScrollArea.Y;
        }

        private void UpdateRoundsTable()
        {
            SProfile[] profile = CBase.Profiles.GetProfiles();
            for (int i = 0; i < RoundsTable.Count; i++)
            {
                for (int p = 0; p < RoundsTable[i].TextPlayer.Count; p++)
                {
                    if (GameState.Combs.Count > i + RoundsTableOffset && GameState.Combs[i + RoundsTableOffset].Player.Count > p)
                    {
                        RoundsTable[i].Number.Visible = true;
                        RoundsTable[i].TextPlayer[p].Visible = true;
                        RoundsTable[i].TextScores[p].Visible = true;
                        RoundsTable[i].Number.Text = (i + 1 + RoundsTableOffset).ToString() + ")";
                        int pID = GameState.ProfileIDs[GameState.Combs[i + RoundsTableOffset].Player[p]];
                        RoundsTable[i].TextPlayer[p].Text = profile[pID].PlayerName;
                        if ((GameState.CurrentRoundNr - 1) > i + RoundsTableOffset)
                            RoundsTable[i].TextScores[p].Text = GameState.Results[i + RoundsTableOffset, p].ToString();
                        else
                            RoundsTable[i].TextScores[p].Text = "";
                    }
                    else
                    {
                        RoundsTable[i].TextPlayer[p].Visible = false;
                        RoundsTable[i].TextScores[p].Visible = false;
                    }
                }
                if (GameState.Combs.Count < i + RoundsTableOffset || i + 1 > NumRoundsVisible)
                {
                    RoundsTable[i].Number.Visible = false;
                    for (int p = 0; p < RoundsTable[i].TextPlayer.Count; p++)
                    {
                        RoundsTable[i].TextPlayer[p].Visible = false;
                        RoundsTable[i].TextScores[p].Visible = false;
                    }
                }
            }

            Buttons[ButtonRoundsScrollUp].Visible = RoundsTableOffset > 0;
            Buttons[ButtonRoundsScrollDown].Visible = GameState.Combs.Count - NumRoundsVisible - RoundsTableOffset > 0;
        }

        private void BuildPlayerTable()
        {
            PlayerTableScrollArea = new SRectF();
            PlayerTableScrollArea.X = Texts[TextPosition].X;
            PlayerTableScrollArea.Y = Texts[TextPosition].Y;
            PlayerTableScrollArea.W = Texts[TextGamePoints].X - Texts[TextPosition].X;

            PlayerTable = new List<TableRow>();
            float delta = Texts[TextPosition].Height * 1.2f;

            float h = 0;

            for (int i = 0; i < 10; i++)
            {
                TableRow row = new TableRow();

                row.Pos = GetNewText(Texts[TextPosition]);
                row.Name = GetNewText(Texts[TextPlayerName]);
                row.Rounds = GetNewText(Texts[TextNumPlayed]);
                row.Won = GetNewText(Texts[TextWon]);
                row.SingPoints = GetNewText(Texts[TextSingPoints]);
                row.GamePoints = GetNewText(Texts[TextGamePoints]);

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

                PlayerTable.Add(row);

                h = delta * (i + 1);
            }
            PlayerTableScrollArea.H = h + delta;
        }

        private void UpdatePlayerTable()
        {
            SProfile[] profiles = CBase.Profiles.GetProfiles();

            for (int i = 0; i < PlayerTable.Count; i++)
            {
                TableRow row = PlayerTable[i];

                if (i + PlayerTableOffset < GameState.ResultTable.Count)
                {
                    row.Pos.Visible = true;
                    row.Name.Visible = true;
                    row.Rounds.Visible = true;
                    row.Won.Visible = true;
                    row.SingPoints.Visible = true;
                    row.GamePoints.Visible = true;

                    row.Pos.Text = GameState.ResultTable[i + PlayerTableOffset].Position.ToString() + ".";
                    row.Name.Text = profiles[GameState.ResultTable[i + PlayerTableOffset].PlayerID].PlayerName;
                    row.Rounds.Text = GameState.ResultTable[i + PlayerTableOffset].NumRounds.ToString();
                    row.Won.Text = GameState.ResultTable[i + PlayerTableOffset].NumWon.ToString();
                    row.SingPoints.Text = GameState.ResultTable[i + PlayerTableOffset].NumSingPoints.ToString();
                    row.GamePoints.Text = GameState.ResultTable[i + PlayerTableOffset].NumGamePoints.ToString();
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

            Buttons[ButtonPlayerScrollUp].Visible = PlayerTableOffset > 0;
            Buttons[ButtonPlayerScrollDown].Visible = GameState.ProfileIDs.Count - NumPlayerVisible - PlayerTableOffset > 0;
        }

        private void ScrollPlayerTable(int Offset)
        {
            if (GameState.ProfileIDs.Count <= NumPlayerVisible)
                PlayerTableOffset = 0;
            else if (Offset < 0 && PlayerTableOffset + Offset >= 0)
                PlayerTableOffset += Offset;
            else if (Offset < 0 && PlayerTableOffset + Offset < 0)
                PlayerTableOffset = 0;
            else if (Offset > 0 && PlayerTableOffset + Offset <= GameState.ProfileIDs.Count - NumPlayerVisible)
                PlayerTableOffset += Offset;
            else if (Offset > 0 && PlayerTableOffset + Offset > GameState.ProfileIDs.Count - NumPlayerVisible)
                PlayerTableOffset = GameState.ProfileIDs.Count - NumPlayerVisible;

            UpdatePlayerTable();
        }

        private void ScrollRoundsTable(int Offset)
        {
            if (GameState.Combs.Count <= NumRoundsVisible)
                RoundsTableOffset = 0;
            else if (Offset < 0 && RoundsTableOffset + Offset >= 0)
                RoundsTableOffset += Offset;
            else if (Offset < 0 && RoundsTableOffset + Offset < 0)
                RoundsTableOffset = 0;
            else if (Offset > 0 && RoundsTableOffset + Offset <= GameState.Combs.Count - NumRoundsVisible)
                RoundsTableOffset += Offset;
            else if (Offset > 0 && RoundsTableOffset + Offset > GameState.Combs.Count - NumRoundsVisible)
                RoundsTableOffset = GameState.Combs.Count - NumRoundsVisible;

            UpdateRoundsTable();
        }

        private string GetPlayerWinString()
        {
            string s = "";
            SProfile[] profiles = CBase.Profiles.GetProfiles();

            for (int i = 0; i < GameState.ResultTable.Count; i++)
            {
                if (GameState.ResultTable[i].Position == 1)
                {
                    if (i > 0)
                        s += ", ";
                    s += profiles[GameState.ResultTable[i].PlayerID].PlayerName;
                }
                else
                    break;
            }

            return s;
        }
    }
}