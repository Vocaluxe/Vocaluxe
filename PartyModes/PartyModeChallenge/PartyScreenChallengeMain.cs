using System;
using System.Collections.Generic;
using System.Windows.Forms;
using VocaluxeLib.Menu;

namespace VocaluxeLib.PartyModes.Challenge
{
    struct STableRow
    {
        public CText Pos;
        public CText Name;
        public CText Rounds;
        public CText Won;
        public CText SingPoints;
        public CText GamePoints;
    }

    class CRoundsTableRow
    {
        public CText Number;
        public List<CText> TextPlayer;
        public List<CText> TextScores;
    }

    public class CPartyScreenChallengeMain : CMenuParty
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _TextPosition = "TextPosition";
        private const string _TextPlayerName = "TextPlayerName";
        private const string _TextNumPlayed = "TextNumPlayed";
        private const string _TextWon = "TextWon";
        private const string _TextSingPoints = "TextSingPoints";
        private const string _TextGamePoints = "TextGamePoints";
        private const string _TextNextPlayer = "TextNextPlayer";
        private const string _TextPopupReallyExit = "TextPopupReallyExit";
        private const string _TextRoundPlayer = "TextRoundPlayer";
        private const string _TextRoundScore = "TextRoundScore";
        private const string _TextRoundNumber = "TextRoundNumber";
        private const string _TextFinishMessage = "TextFinishMessage";
        private const string _TextFinishPlayerWin = "TextFinishPlayerWin";
        private const string _TextNextPlayerMessage = "TextNextPlayerMessage";

        private const string _ButtonNextRound = "ButtonNextRound";
        private const string _ButtonBack = "ButtonBack";
        private const string _ButtonExit = "ButtonExit";
        private const string _ButtonPopupYes = "ButtonPopupYes";
        private const string _ButtonPopupNo = "ButtonPopupNo";
        private const string _ButtonPlayerScrollUp = "ButtonPlayerScrollUp";
        private const string _ButtonPlayerScrollDown = "ButtonPlayerScrollDown";
        private const string _ButtonRoundsScrollUp = "ButtonRoundsScrollUp";
        private const string _ButtonRoundsScrollDown = "ButtonRoundsScrollDown";

        private const string _StaticPopupBG = "StaticPopupBG";
        private const string _StaticNextPlayer = "StaticNextPlayer";

        private bool _ExitPopupVisible;

        private SDataFromScreen _Data;
        private SDataToScreenMain _GameState;
        private List<STableRow> _PlayerTable;
        private List<CRoundsTableRow> _RoundsTable;

        private List<CText> _NextPlayerTexts;
        private List<CStatic> _NextPlayerStatics;

        private SRectF _RoundsTableScrollArea;
        private SRectF _PlayerTableScrollArea;
        private int _RoundsTableOffset;
        private int _PlayerTableOffset;
        private int _NumPlayerVisible = 10;
        private int _NumRoundsVisible = 3;

        public CPartyScreenChallengeMain()
        {
            _Data = new SDataFromScreen();
            _Data.ScreenMain = new SFromScreenMain();
        }

        public override void Init()
        {
            base.Init();

            _ThemeTexts = new string[]
                {
                    _TextPosition, _TextPlayerName, _TextNumPlayed, _TextWon, _TextSingPoints, _TextGamePoints, _TextNextPlayer, _TextPopupReallyExit, _TextRoundNumber, _TextRoundPlayer,
                    _TextRoundScore, _TextFinishMessage, _TextFinishPlayerWin, _TextNextPlayerMessage
                };
            _ThemeButtons = new string[]
                {_ButtonNextRound, _ButtonBack, _ButtonExit, _ButtonPopupYes, _ButtonPopupNo, _ButtonPlayerScrollDown, _ButtonPlayerScrollUp, _ButtonRoundsScrollDown, _ButtonRoundsScrollUp};
            _ThemeStatics = new string[] {_StaticPopupBG, _StaticNextPlayer};
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _GameState = new SDataToScreenMain();
            _BuildPlayerTable();
            _CreateRoundsTable();
            _NextPlayerTexts = new List<CText>();
            _NextPlayerStatics = new List<CStatic>();

            for (int i = 0; i < _PartyMode.GetMaxPlayer(); i++)
            {
                _NextPlayerTexts.Add(GetNewText(Texts[_TextNextPlayer]));
                AddText(_NextPlayerTexts[_NextPlayerTexts.Count - 1]);
                _NextPlayerStatics.Add(GetNewStatic(Statics[_StaticNextPlayer]));
                AddStatic(_NextPlayerStatics[_NextPlayerStatics.Count - 1]);
            }
        }

        public override void DataToScreen(object receivedData)
        {
            SDataToScreenMain data = new SDataToScreenMain();

            try
            {
                data = (SDataToScreenMain)receivedData;
                _GameState = data;
            }
            catch (Exception e)
            {
                CBase.Log.LogError("Error in party mode screen challenge main. Can't cast received data from game mode " + ThemeName + ". " + e.Message);
            }
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed) {}
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Back:
                    case Keys.Escape:
                        if (!_ExitPopupVisible)
                        {
                            if (_GameState.CurrentRoundNr == 1)
                                _Back();
                            else
                                _ShowPopup(true);
                        }
                        else
                            _ShowPopup(false);
                        break;

                    case Keys.Enter:
                        if (!_ExitPopupVisible)
                        {
                            if (Buttons[_ButtonNextRound].Selected)
                                _NextRound();
                            if (Buttons[_ButtonBack].Selected && _GameState.CurrentRoundNr == 1)
                                _Back();
                            if (Buttons[_ButtonExit].Selected && _GameState.CurrentRoundNr > 1)
                                _ShowPopup(true);
                            if (Buttons[_ButtonPlayerScrollUp].Selected)
                                _ScrollPlayerTable(-1);
                            if (Buttons[_ButtonPlayerScrollDown].Selected)
                                _ScrollPlayerTable(1);
                            if (Buttons[_ButtonRoundsScrollUp].Selected)
                                _ScrollRoundsTable(-1);
                            if (Buttons[_ButtonRoundsScrollDown].Selected)
                                _ScrollRoundsTable(1);
                        }
                        else
                        {
                            if (Buttons[_ButtonPopupYes].Selected)
                                _EndParty();
                            if (Buttons[_ButtonPopupNo].Selected)
                                _ShowPopup(false);
                        }
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.LB && IsMouseOver(mouseEvent))
            {
                if (!_ExitPopupVisible)
                {
                    if (Buttons[_ButtonNextRound].Selected)
                        _NextRound();
                    if (Buttons[_ButtonBack].Selected && _GameState.CurrentRoundNr == 1)
                        _Back();
                    if (Buttons[_ButtonExit].Selected && _GameState.CurrentRoundNr > 1)
                        _ShowPopup(true);
                    if (Buttons[_ButtonPlayerScrollUp].Selected)
                        _ScrollPlayerTable(-1);
                    if (Buttons[_ButtonPlayerScrollDown].Selected)
                        _ScrollPlayerTable(1);
                    if (Buttons[_ButtonRoundsScrollUp].Selected)
                        _ScrollRoundsTable(-1);
                    if (Buttons[_ButtonRoundsScrollDown].Selected)
                        _ScrollRoundsTable(1);
                }
                else
                {
                    if (Buttons[_ButtonPopupYes].Selected)
                        _EndParty();
                    if (Buttons[_ButtonPopupNo].Selected)
                        _ShowPopup(false);
                }
            }

            if (mouseEvent.RB)
            {
                if (!_ExitPopupVisible)
                {
                    if (_GameState.CurrentRoundNr == 1)
                        _Back();
                    else
                        _ShowPopup(true);
                }
                else
                    _ShowPopup(false);
            }

            if (mouseEvent.Wheel != 0)
            {
                if (CHelper.IsInBounds(_RoundsTableScrollArea, mouseEvent))
                    _ScrollRoundsTable(mouseEvent.Wheel);
                if (CHelper.IsInBounds(_PlayerTableScrollArea, mouseEvent))
                    _ScrollPlayerTable(mouseEvent.Wheel);
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            _PlayerTableOffset = 0;
            _RoundsTableOffset = 0;

            _UpdatePlayerTable();
            _UpdateNextPlayerPositions();
            _UpdateNextPlayerContents();
            if (_GameState.CurrentRoundNr == 1)
                _BuildRoundsTable();
            else
                _ScrollRoundsTable(_GameState.CurrentRoundNr - 2);
            _UpdateRoundsTable();

            if (_GameState.CurrentRoundNr == 1)
            {
                Buttons[_ButtonBack].Visible = true;
                Buttons[_ButtonExit].Visible = false;
            }
            else
            {
                Buttons[_ButtonBack].Visible = false;
                Buttons[_ButtonExit].Visible = true;
            }

            if (_GameState.CurrentRoundNr <= _GameState.Combs.Count)
            {
                Buttons[_ButtonNextRound].Visible = true;
                Texts[_TextFinishMessage].Visible = false;
                Texts[_TextFinishPlayerWin].Visible = false;
                SetInteractionToButton(Buttons[_ButtonNextRound]);
            }
            else
            {
                Buttons[_ButtonNextRound].Visible = false;
                Texts[_TextFinishMessage].Visible = true;
                Texts[_TextFinishPlayerWin].Visible = true;
                Texts[_TextFinishPlayerWin].Text = _GetPlayerWinString();
                SetInteractionToButton(Buttons[_ButtonExit]);
            }

            _ShowPopup(false);
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

        private void _NextRound()
        {
            _Data.ScreenMain.FadeToNameSelection = false;
            _Data.ScreenMain.FadeToSongSelection = true;
            _PartyMode.DataFromScreen(ThemeName, _Data);
        }

        private void _EndParty()
        {
            _FadeTo(EScreens.ScreenParty);
        }

        private void _ShowPopup(bool visible)
        {
            _ExitPopupVisible = visible;

            Statics[_StaticPopupBG].Visible = _ExitPopupVisible;
            Texts[_TextPopupReallyExit].Visible = _ExitPopupVisible;
            Buttons[_ButtonPopupYes].Visible = _ExitPopupVisible;
            Buttons[_ButtonPopupNo].Visible = _ExitPopupVisible;

            if (_ExitPopupVisible)
                SetInteractionToButton(Buttons[_ButtonPopupNo]);
        }

        private void _Back()
        {
            _Data.ScreenMain.FadeToNameSelection = true;
            _Data.ScreenMain.FadeToSongSelection = false;
            _PartyMode.DataFromScreen(ThemeName, _Data);
        }

        private void _UpdateNextPlayerPositions()
        {
            float x = CBase.Settings.GetRenderW() / 2 - ((_GameState.NumPlayerAtOnce * Statics[_StaticNextPlayer].Rect.W) + ((_GameState.NumPlayerAtOnce - 1) * 15)) / 2;
            float staticY = 590;
            float textY = 550;
            for (int i = 0; i < _GameState.NumPlayerAtOnce; i++)
            {
                //static
                _NextPlayerStatics[i].Rect.X = x;
                _NextPlayerStatics[i].Rect.Y = staticY;
                _NextPlayerStatics[i].Visible = true;
                //text
                _NextPlayerTexts[i].X = x + Statics[_StaticNextPlayer].Rect.W / 2;
                _NextPlayerTexts[i].Y = textY;
                _NextPlayerTexts[i].Visible = true;

                x += Statics[_StaticNextPlayer].Rect.W + 15;
            }
            for (int i = _GameState.NumPlayerAtOnce; i < _PartyMode.GetMaxPlayer(); i++)
            {
                _NextPlayerStatics[i].Visible = false;
                _NextPlayerTexts[i].Visible = false;
            }
        }

        private void _UpdateNextPlayerContents()
        {
            if (_GameState.CurrentRoundNr <= _GameState.Combs.Count)
            {
                Texts[_TextNextPlayerMessage].Visible = true;
                SProfile[] profiles = CBase.Profiles.GetProfiles();
                for (int i = 0; i < _GameState.NumPlayerAtOnce; i++)
                {
                    int pid = _GameState.Combs[_GameState.CurrentRoundNr - 1].Player[i];
                    _NextPlayerStatics[i].Texture = profiles[_GameState.ProfileIDs[pid]].Avatar.Texture;
                    _NextPlayerTexts[i].Text = profiles[_GameState.ProfileIDs[pid]].PlayerName;
                    _NextPlayerTexts[i].Color = CBase.Theme.GetPlayerColor(i + 1);
                }
            }
            else
            {
                Texts[_TextNextPlayerMessage].Visible = false;
                for (int i = 0; i < _GameState.NumPlayerAtOnce; i++)
                {
                    _NextPlayerStatics[i].Visible = false;
                    _NextPlayerTexts[i].Visible = false;
                }
            }
        }

        private void _CreateRoundsTable()
        {
            //Create lists
            _RoundsTable = new List<CRoundsTableRow>();
            for (int i = 0; i < 5; i++)
            {
                CRoundsTableRow rtr = new CRoundsTableRow();
                rtr.TextPlayer = new List<CText>();
                rtr.TextScores = new List<CText>();
                _RoundsTable.Add(rtr);
            }
            //Create statics and texts for rounds
            for (int round = 0; round < _RoundsTable.Count; round++)
            {
                //Round-number
                CText text = GetNewText(Texts[_TextRoundNumber]);
                AddText(text);
                _RoundsTable[round].Number = text;
                for (int row = 0; row < 2; row++)
                {
                    for (int column = 0; column < 3; column++)
                    {
                        //Player
                        text = GetNewText(Texts[_TextRoundPlayer]);
                        AddText(text);
                        _RoundsTable[round].TextPlayer.Add(text);
                        //Score
                        text = GetNewText(Texts[_TextRoundScore]);
                        AddText(text);
                        _RoundsTable[round].TextScores.Add(text);
                    }
                }
            }
        }

        private void _BuildRoundsTable()
        {
            _RoundsTableScrollArea = new SRectF();

            int numPlayerInOneRow = 3;
            if (_GameState.NumPlayerAtOnce <= numPlayerInOneRow)
                _NumRoundsVisible = 5;
            else
                _NumRoundsVisible = 3;

            if (_NumRoundsVisible > _GameState.Combs.Count)
                _NumRoundsVisible = _GameState.Combs.Count;

            float numberX = Texts[_TextRoundNumber].X;
            float numberY = Texts[_TextRoundNumber].Y;

            _RoundsTableScrollArea.X = numberX;
            _RoundsTableScrollArea.Y = numberY;
            _RoundsTableScrollArea.W = CBase.Settings.GetRenderW() - Texts[_TextRoundNumber].X - 20;

            float delta = Texts[_TextRoundNumber].Height;

            //Update statics and texts for rounds
            for (int round = 0; round < _RoundsTable.Count; round++)
            {
                //Round-number
                _RoundsTable[round].Number.X = numberX;
                _RoundsTable[round].Number.Y = numberY;
                int numInnerRows = (int)Math.Ceiling(_GameState.NumPlayerAtOnce / ((double)numPlayerInOneRow));
                for (int row = 0; row < numInnerRows; row++)
                {
                    int num = (row + 1) * numPlayerInOneRow;
                    int numPlayerInThisRow = numPlayerInOneRow;
                    if (num > _GameState.NumPlayerAtOnce)
                    {
                        num = _GameState.NumPlayerAtOnce;
                        numPlayerInThisRow = _GameState.NumPlayerAtOnce - (row * numPlayerInOneRow);
                    }
                    for (int column = row * numPlayerInOneRow; column < num; column++)
                    {
                        //Player
                        float x = numberX + 15 + (CBase.Settings.GetRenderW() - Texts[_TextRoundNumber].X - 20) / numPlayerInThisRow * (column - row * numPlayerInOneRow) +
                                   ((CBase.Settings.GetRenderW() - Texts[_TextRoundNumber].X - 20) / numPlayerInThisRow) / 2;
                        float maxw = ((CBase.Settings.GetRenderW() - Texts[_TextRoundNumber].X - 20) / numPlayerInThisRow) / 2 - 5;
                        _RoundsTable[round].TextPlayer[column].X = x;
                        _RoundsTable[round].TextPlayer[column].Y = numberY;
                        _RoundsTable[round].TextPlayer[column].MaxWidth = maxw;
                        //Score
                        _RoundsTable[round].TextScores[column] = _RoundsTable[round].TextScores[column];
                        _RoundsTable[round].TextScores[column].X = x;
                        _RoundsTable[round].TextScores[column].Y = numberY + delta;
                        _RoundsTable[round].TextScores[column].MaxWidth = maxw;
                    }
                    numberY = numberY + delta + delta;
                }
                numberY = numberY + delta / 2;
            }
            _RoundsTableScrollArea.H = numberY - _RoundsTableScrollArea.Y;
        }

        private void _UpdateRoundsTable()
        {
            SProfile[] profile = CBase.Profiles.GetProfiles();
            for (int i = 0; i < _RoundsTable.Count; i++)
            {
                for (int p = 0; p < _RoundsTable[i].TextPlayer.Count; p++)
                {
                    if (_GameState.Combs.Count > i + _RoundsTableOffset && _GameState.Combs[i + _RoundsTableOffset].Player.Count > p)
                    {
                        _RoundsTable[i].Number.Visible = true;
                        _RoundsTable[i].TextPlayer[p].Visible = true;
                        _RoundsTable[i].TextScores[p].Visible = true;
                        _RoundsTable[i].Number.Text = (i + 1 + _RoundsTableOffset).ToString() + ")";
                        int pID = _GameState.ProfileIDs[_GameState.Combs[i + _RoundsTableOffset].Player[p]];
                        _RoundsTable[i].TextPlayer[p].Text = profile[pID].PlayerName;
                        if ((_GameState.CurrentRoundNr - 1) > i + _RoundsTableOffset)
                            _RoundsTable[i].TextScores[p].Text = _GameState.Results[i + _RoundsTableOffset, p].ToString();
                        else
                            _RoundsTable[i].TextScores[p].Text = "";
                    }
                    else
                    {
                        _RoundsTable[i].TextPlayer[p].Visible = false;
                        _RoundsTable[i].TextScores[p].Visible = false;
                    }
                }
                if (_GameState.Combs.Count < i + _RoundsTableOffset || i + 1 > _NumRoundsVisible)
                {
                    _RoundsTable[i].Number.Visible = false;
                    for (int p = 0; p < _RoundsTable[i].TextPlayer.Count; p++)
                    {
                        _RoundsTable[i].TextPlayer[p].Visible = false;
                        _RoundsTable[i].TextScores[p].Visible = false;
                    }
                }
            }

            Buttons[_ButtonRoundsScrollUp].Visible = _RoundsTableOffset > 0;
            Buttons[_ButtonRoundsScrollDown].Visible = _GameState.Combs.Count - _NumRoundsVisible - _RoundsTableOffset > 0;
        }

        private void _BuildPlayerTable()
        {
            _PlayerTableScrollArea = new SRectF();
            _PlayerTableScrollArea.X = Texts[_TextPosition].X;
            _PlayerTableScrollArea.Y = Texts[_TextPosition].Y;
            _PlayerTableScrollArea.W = Texts[_TextGamePoints].X - Texts[_TextPosition].X;

            _PlayerTable = new List<STableRow>();
            float delta = Texts[_TextPosition].Height * 1.2f;

            float h = 0;

            for (int i = 0; i < 10; i++)
            {
                STableRow row = new STableRow();

                row.Pos = GetNewText(Texts[_TextPosition]);
                row.Name = GetNewText(Texts[_TextPlayerName]);
                row.Rounds = GetNewText(Texts[_TextNumPlayed]);
                row.Won = GetNewText(Texts[_TextWon]);
                row.SingPoints = GetNewText(Texts[_TextSingPoints]);
                row.GamePoints = GetNewText(Texts[_TextGamePoints]);

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

                _PlayerTable.Add(row);

                h = delta * (i + 1);
            }
            _PlayerTableScrollArea.H = h + delta;
        }

        private void _UpdatePlayerTable()
        {
            SProfile[] profiles = CBase.Profiles.GetProfiles();

            for (int i = 0; i < _PlayerTable.Count; i++)
            {
                STableRow row = _PlayerTable[i];

                if (i + _PlayerTableOffset < _GameState.ResultTable.Count)
                {
                    row.Pos.Visible = true;
                    row.Name.Visible = true;
                    row.Rounds.Visible = true;
                    row.Won.Visible = true;
                    row.SingPoints.Visible = true;
                    row.GamePoints.Visible = true;

                    row.Pos.Text = _GameState.ResultTable[i + _PlayerTableOffset].Position.ToString() + ".";
                    row.Name.Text = profiles[_GameState.ResultTable[i + _PlayerTableOffset].PlayerID].PlayerName;
                    row.Rounds.Text = _GameState.ResultTable[i + _PlayerTableOffset].NumRounds.ToString();
                    row.Won.Text = _GameState.ResultTable[i + _PlayerTableOffset].NumWon.ToString();
                    row.SingPoints.Text = _GameState.ResultTable[i + _PlayerTableOffset].NumSingPoints.ToString();
                    row.GamePoints.Text = _GameState.ResultTable[i + _PlayerTableOffset].NumGamePoints.ToString();
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

            Buttons[_ButtonPlayerScrollUp].Visible = _PlayerTableOffset > 0;
            Buttons[_ButtonPlayerScrollDown].Visible = _GameState.ProfileIDs.Count - _NumPlayerVisible - _PlayerTableOffset > 0;
        }

        private void _ScrollPlayerTable(int offset)
        {
            if (_GameState.ProfileIDs.Count <= _NumPlayerVisible)
                _PlayerTableOffset = 0;
            else if (offset < 0 && _PlayerTableOffset + offset >= 0)
                _PlayerTableOffset += offset;
            else if (offset < 0 && _PlayerTableOffset + offset < 0)
                _PlayerTableOffset = 0;
            else if (offset > 0 && _PlayerTableOffset + offset <= _GameState.ProfileIDs.Count - _NumPlayerVisible)
                _PlayerTableOffset += offset;
            else if (offset > 0 && _PlayerTableOffset + offset > _GameState.ProfileIDs.Count - _NumPlayerVisible)
                _PlayerTableOffset = _GameState.ProfileIDs.Count - _NumPlayerVisible;

            _UpdatePlayerTable();
        }

        private void _ScrollRoundsTable(int offset)
        {
            if (_GameState.Combs.Count <= _NumRoundsVisible)
                _RoundsTableOffset = 0;
            else if (offset < 0 && _RoundsTableOffset + offset >= 0)
                _RoundsTableOffset += offset;
            else if (offset < 0 && _RoundsTableOffset + offset < 0)
                _RoundsTableOffset = 0;
            else if (offset > 0 && _RoundsTableOffset + offset <= _GameState.Combs.Count - _NumRoundsVisible)
                _RoundsTableOffset += offset;
            else if (offset > 0 && _RoundsTableOffset + offset > _GameState.Combs.Count - _NumRoundsVisible)
                _RoundsTableOffset = _GameState.Combs.Count - _NumRoundsVisible;

            _UpdateRoundsTable();
        }

        private string _GetPlayerWinString()
        {
            string s = "";
            SProfile[] profiles = CBase.Profiles.GetProfiles();

            for (int i = 0; i < _GameState.ResultTable.Count; i++)
            {
                if (_GameState.ResultTable[i].Position == 1)
                {
                    if (i > 0)
                        s += ", ";
                    s += profiles[_GameState.ResultTable[i].PlayerID].PlayerName;
                }
                else
                    break;
            }

            return s;
        }
    }
}