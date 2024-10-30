#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

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

    // ReSharper disable UnusedMember.Global
    public class CPartyScreenChallengeMain : CPartyScreenChallenge
        // ReSharper restore UnusedMember.Global
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

        private List<STableRow> _PlayerTable;
        private List<CRoundsTableRow> _RoundsTable;

        private List<CText> _NextPlayerTexts;
        private List<CStatic> _NextPlayerStatics;

        private SRectF _RoundsTableScrollArea;
        private SRectF _PlayerTableScrollArea;
        private int _RoundsTableOffset;
        private int _PlayerTableOffset;
        private const int _NumPlayerVisible = 10;
        private int _NumRoundsVisible = 3;

        public override void Init()
        {
            base.Init();

            _ThemeTexts = new string[]
                {
                    _TextPosition, _TextPlayerName, _TextNumPlayed, _TextWon, _TextSingPoints, _TextGamePoints, _TextNextPlayer, _TextPopupReallyExit, _TextRoundNumber,
                    _TextRoundPlayer,
                    _TextRoundScore, _TextFinishMessage, _TextFinishPlayerWin, _TextNextPlayerMessage
                };
            _ThemeButtons = new string[]
                {
                    _ButtonNextRound, _ButtonBack, _ButtonExit, _ButtonPopupYes, _ButtonPopupNo, _ButtonPlayerScrollDown, _ButtonPlayerScrollUp, _ButtonRoundsScrollDown,
                    _ButtonRoundsScrollUp
                };
            _ThemeStatics = new string[] {_StaticPopupBG, _StaticNextPlayer};
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _BuildPlayerTable();
            _CreateRoundsTable();
            _NextPlayerTexts = new List<CText>();
            _NextPlayerStatics = new List<CStatic>();

            for (int i = 0; i < _PartyMode.MaxPlayers; i++)
            {
                _NextPlayerTexts.Add(GetNewText(_Texts[_TextNextPlayer]));
                _AddText(_NextPlayerTexts[_NextPlayerTexts.Count - 1]);
                _NextPlayerStatics.Add(GetNewStatic(_Statics[_StaticNextPlayer]));
                _AddStatic(_NextPlayerStatics[_NextPlayerStatics.Count - 1]);
                _NextPlayerStatics[_NextPlayerStatics.Count - 1].Aspect = EAspect.Crop;
            }
            _Statics[_StaticNextPlayer].Visible = false;
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
                            if (_PartyMode.GameData.CurrentRoundNr == 1)
                                _PartyMode.Back();
                            else
                                _ShowPopup(true);
                        }
                        else
                            _ShowPopup(false);
                        break;

                    case Keys.Enter:
                        if (!_ExitPopupVisible)
                        {
                            if (_Buttons[_ButtonNextRound].Selected)
                                _PartyMode.Next();
                            if (_Buttons[_ButtonBack].Selected && _PartyMode.GameData.CurrentRoundNr == 1)
                                _PartyMode.Back();
                            if (_Buttons[_ButtonExit].Selected && _PartyMode.GameData.CurrentRoundNr > 1)
                                _ShowPopup(true);
                            if (_Buttons[_ButtonPlayerScrollUp].Selected)
                                _ScrollPlayerTable(-1);
                            if (_Buttons[_ButtonPlayerScrollDown].Selected)
                                _ScrollPlayerTable(1);
                            if (_Buttons[_ButtonRoundsScrollUp].Selected)
                                _ScrollRoundsTable(-1);
                            if (_Buttons[_ButtonRoundsScrollDown].Selected)
                                _ScrollRoundsTable(1);
                        }
                        else
                        {
                            if (_Buttons[_ButtonPopupYes].Selected)
                                _EndParty();
                            if (_Buttons[_ButtonPopupNo].Selected)
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

            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                if (!_ExitPopupVisible)
                {
                    if (_Buttons[_ButtonNextRound].Selected)
                        _PartyMode.Next();
                    if (_Buttons[_ButtonBack].Selected && _PartyMode.GameData.CurrentRoundNr == 1)
                        _PartyMode.Back();
                    if (_Buttons[_ButtonExit].Selected && _PartyMode.GameData.CurrentRoundNr > 1)
                        _ShowPopup(true);
                    if (_Buttons[_ButtonPlayerScrollUp].Selected)
                        _ScrollPlayerTable(-1);
                    if (_Buttons[_ButtonPlayerScrollDown].Selected)
                        _ScrollPlayerTable(1);
                    if (_Buttons[_ButtonRoundsScrollUp].Selected)
                        _ScrollRoundsTable(-1);
                    if (_Buttons[_ButtonRoundsScrollDown].Selected)
                        _ScrollRoundsTable(1);
                }
                else
                {
                    if (_Buttons[_ButtonPopupYes].Selected)
                        _EndParty();
                    if (_Buttons[_ButtonPopupNo].Selected)
                        _ShowPopup(false);
                }
            }

            if (mouseEvent.RB)
            {
                if (!_ExitPopupVisible)
                {
                    if (_PartyMode.GameData.CurrentRoundNr == 1)
                        _PartyMode.Back();
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
            if (_PartyMode.GameData.CurrentRoundNr == 1)
                _BuildRoundsTable();
            else
                _ScrollRoundsTable(_PartyMode.GameData.CurrentRoundNr - 2);
            _UpdateRoundsTable();

            if (_PartyMode.GameData.CurrentRoundNr == 1)
            {
                _Buttons[_ButtonBack].Visible = true;
                _Buttons[_ButtonExit].Visible = false;
            }
            else
            {
                _Buttons[_ButtonBack].Visible = false;
                _Buttons[_ButtonExit].Visible = true;
            }

            if (_PartyMode.GameData.CurrentRoundNr <= _PartyMode.GameData.Rounds.Count)
            {
                _Buttons[_ButtonNextRound].Visible = true;
                _Texts[_TextFinishMessage].Visible = false;
                _Texts[_TextFinishPlayerWin].Visible = false;
                _SelectElement(_Buttons[_ButtonNextRound]);
            }
            else
            {
                _Buttons[_ButtonNextRound].Visible = false;
                _Texts[_TextFinishMessage].Visible = true;
                _Texts[_TextFinishPlayerWin].Visible = true;
                _Texts[_TextFinishPlayerWin].Text = _GetPlayerWinString();
                _SelectElement(_Buttons[_ButtonExit]);
            }

            _ShowPopup(false);
        }

        public override bool UpdateGame()
        {
            return true;
        }

        private void _EndParty()
        {
            CBase.Graphics.FadeTo(EScreen.Party);
        }

        private void _ShowPopup(bool visible)
        {
            _ExitPopupVisible = visible;

            _Statics[_StaticPopupBG].Visible = _ExitPopupVisible;
            _Texts[_TextPopupReallyExit].Visible = _ExitPopupVisible;
            _Buttons[_ButtonPopupYes].Visible = _ExitPopupVisible;
            _Buttons[_ButtonPopupNo].Visible = _ExitPopupVisible;

            if (_ExitPopupVisible)
                _SelectElement(_Buttons[_ButtonPopupNo]);
        }

        private void _UpdateNextPlayerPositions()
        {
            float x = (float)CBase.Settings.GetRenderW() / 2 -
                      ((_PartyMode.GameData.NumPlayerAtOnce * _Statics[_StaticNextPlayer].Rect.W) + ((_PartyMode.GameData.NumPlayerAtOnce - 1) * 15)) / 2;
            const float staticY = 885;
            const float textY = 825;
            for (int i = 0; i < _PartyMode.GameData.NumPlayerAtOnce; i++)
            {
                //static
                _NextPlayerStatics[i].X = x;
                _NextPlayerStatics[i].Y = staticY;
                _NextPlayerStatics[i].Visible = true;
                //text
                _NextPlayerTexts[i].X = x + _Statics[_StaticNextPlayer].Rect.W / 2;
                _NextPlayerTexts[i].Y = textY;
                _NextPlayerTexts[i].Visible = true;

                x += _Statics[_StaticNextPlayer].Rect.W + 15;
            }
            for (int i = _PartyMode.GameData.NumPlayerAtOnce; i < _PartyMode.MaxPlayers; i++)
            {
                _NextPlayerStatics[i].Visible = false;
                _NextPlayerTexts[i].Visible = false;
            }
        }

        private void _UpdateNextPlayerContents()
        {
            if (_PartyMode.GameData.CurrentRoundNr <= _PartyMode.GameData.Rounds.Count)
            {
                _Texts[_TextNextPlayerMessage].Visible = true;
                for (int i = 0; i < _PartyMode.GameData.NumPlayerAtOnce; i++)
                {
                    Guid id = _PartyMode.GameData.ProfileIDs[_PartyMode.GameData.Rounds[_PartyMode.GameData.CurrentRoundNr - 1].Players[i]];
                    _NextPlayerStatics[i].Texture = CBase.Profiles.GetAvatar(id);
                    _NextPlayerTexts[i].Text = CBase.Profiles.GetPlayerName(id);
                    _NextPlayerTexts[i].Color = CBase.Themes.GetPlayerColor(i + 1);
                }
            }
            else
            {
                _Texts[_TextNextPlayerMessage].Visible = false;
                for (int i = 0; i < _PartyMode.GameData.NumPlayerAtOnce; i++)
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
                var rtr = new CRoundsTableRow {TextPlayer = new List<CText>(), TextScores = new List<CText>()};
                _RoundsTable.Add(rtr);
            }
            //Create statics and texts for rounds
            foreach (CRoundsTableRow roundRow in _RoundsTable)
            {
                //Round-number
                CText text = GetNewText(_Texts[_TextRoundNumber]);
                _AddText(text);
                roundRow.Number = text;
                for (int row = 0; row < 2; row++)
                {
                    for (int column = 0; column < 3; column++)
                    {
                        //Player
                        text = GetNewText(_Texts[_TextRoundPlayer]);
                        _AddText(text);
                        roundRow.TextPlayer.Add(text);
                        //Score
                        text = GetNewText(_Texts[_TextRoundScore]);
                        _AddText(text);
                        roundRow.TextScores.Add(text);
                    }
                }
            }
        }

        private void _BuildRoundsTable()
        {
            _RoundsTableScrollArea = new SRectF();

            const int numPlayerInOneRow = 3;
            _NumRoundsVisible = _PartyMode.GameData.NumPlayerAtOnce <= numPlayerInOneRow ? 5 : 3;

            if (_NumRoundsVisible > _PartyMode.GameData.Rounds.Count)
                _NumRoundsVisible = _PartyMode.GameData.Rounds.Count;

            float numberX = _Texts[_TextRoundNumber].X;
            float numberY = _Texts[_TextRoundNumber].Y;

            _RoundsTableScrollArea.X = numberX;
            _RoundsTableScrollArea.Y = numberY;
            _RoundsTableScrollArea.W = CBase.Settings.GetRenderW() - _Texts[_TextRoundNumber].X - 20;

            float delta = _Texts[_TextRoundNumber].Rect.H;

            //Update statics and texts for rounds
            foreach (CRoundsTableRow roundRow in _RoundsTable)
            {
                //Round-number
                roundRow.Number.X = numberX;
                roundRow.Number.Y = numberY;
                var numInnerRows = (int)Math.Ceiling(_PartyMode.GameData.NumPlayerAtOnce / ((double)numPlayerInOneRow));
                for (int row = 0; row < numInnerRows; row++)
                {
                    int num = (row + 1) * numPlayerInOneRow;
                    int numPlayerInThisRow = numPlayerInOneRow;
                    if (num > _PartyMode.GameData.NumPlayerAtOnce)
                    {
                        num = _PartyMode.GameData.NumPlayerAtOnce;
                        numPlayerInThisRow = _PartyMode.GameData.NumPlayerAtOnce - (row * numPlayerInOneRow);
                    }
                    for (int column = row * numPlayerInOneRow; column < num; column++)
                    {
                        //Player
                        float x = numberX + 15 + (CBase.Settings.GetRenderW() - _Texts[_TextRoundNumber].X - 20) / numPlayerInThisRow * (column - row * numPlayerInOneRow) +
                                  ((CBase.Settings.GetRenderW() - _Texts[_TextRoundNumber].X - 20) / numPlayerInThisRow) / 2;
                        float maxw = ((CBase.Settings.GetRenderW() - _Texts[_TextRoundNumber].X - 20) / numPlayerInThisRow) / 2 - 5;
                        roundRow.TextPlayer[column].X = x;
                        roundRow.TextPlayer[column].Y = numberY;
                        roundRow.TextPlayer[column].W = maxw;
                        //Score
                        roundRow.TextScores[column].X = x;
                        roundRow.TextScores[column].Y = numberY + delta;
                        roundRow.TextScores[column].W = maxw;
                    }
                    numberY = numberY + 2 * delta;
                }
                numberY = numberY + delta / 2;
            }
            _RoundsTableScrollArea.H = numberY - _RoundsTableScrollArea.Y;
        }

        private void _UpdateRoundsTable()
        {
            for (int i = 0; i < _RoundsTable.Count; i++)
            {
                for (int p = 0; p < _RoundsTable[i].TextPlayer.Count; p++)
                {
                    if (_PartyMode.GameData.Rounds.Count > i + _RoundsTableOffset && _PartyMode.GameData.Rounds[i + _RoundsTableOffset].Players.Count > p)
                    {
                        _RoundsTable[i].Number.Visible = true;
                        _RoundsTable[i].TextPlayer[p].Visible = true;
                        _RoundsTable[i].TextScores[p].Visible = true;
                        _RoundsTable[i].Number.Text = (i + 1 + _RoundsTableOffset) + ")";
                        _RoundsTable[i].TextPlayer[p].Text =
                            CBase.Profiles.GetPlayerName(_PartyMode.GameData.ProfileIDs[_PartyMode.GameData.Rounds[i + _RoundsTableOffset].Players[p]]);
                        // ReSharper disable ConvertIfStatementToConditionalTernaryExpression
                        if ((_PartyMode.GameData.CurrentRoundNr - 1) > i + _RoundsTableOffset)
                            // ReSharper restore ConvertIfStatementToConditionalTernaryExpression
                            _RoundsTable[i].TextScores[p].Text = _PartyMode.GameData.Results[i + _RoundsTableOffset, p].ToString();
                        else
                            _RoundsTable[i].TextScores[p].Text = "";
                    }
                    else
                    {
                        _RoundsTable[i].TextPlayer[p].Visible = false;
                        _RoundsTable[i].TextScores[p].Visible = false;
                    }
                }
                if (_PartyMode.GameData.Rounds.Count < i + _RoundsTableOffset || i + 1 > _NumRoundsVisible)
                {
                    _RoundsTable[i].Number.Visible = false;
                    for (int p = 0; p < _RoundsTable[i].TextPlayer.Count; p++)
                    {
                        _RoundsTable[i].TextPlayer[p].Visible = false;
                        _RoundsTable[i].TextScores[p].Visible = false;
                    }
                }
            }

            _Buttons[_ButtonRoundsScrollUp].Visible = _RoundsTableOffset > 0;
            _Buttons[_ButtonRoundsScrollDown].Visible = _PartyMode.GameData.Rounds.Count - _NumRoundsVisible - _RoundsTableOffset > 0;
        }

        private void _BuildPlayerTable()
        {
            _PlayerTableScrollArea = new SRectF {X = _Texts[_TextPosition].X, Y = _Texts[_TextPosition].Y, W = _Texts[_TextGamePoints].X - _Texts[_TextPosition].X};

            _PlayerTable = new List<STableRow>();
            float delta = _Texts[_TextPosition].Rect.H * 1.2f;

            float h = 0;

            for (int i = 0; i < 10; i++)
            {
                var row = new STableRow
                    {
                        Pos = GetNewText(_Texts[_TextPosition]),
                        Name = GetNewText(_Texts[_TextPlayerName]),
                        Rounds = GetNewText(_Texts[_TextNumPlayed]),
                        Won = GetNewText(_Texts[_TextWon]),
                        SingPoints = GetNewText(_Texts[_TextSingPoints]),
                        GamePoints = GetNewText(_Texts[_TextGamePoints])
                    };

                row.Pos.Y += delta * (i + 1);
                row.Name.Y += delta * (i + 1);
                row.Rounds.Y += delta * (i + 1);
                row.Won.Y += delta * (i + 1);
                row.SingPoints.Y += delta * (i + 1);
                row.GamePoints.Y += delta * (i + 1);

                row.Pos.Text = (i + 1) + ".";

                row.Pos.Visible = false;
                row.Name.Visible = false;
                row.Rounds.Visible = false;
                row.Won.Visible = false;
                row.SingPoints.Visible = false;
                row.GamePoints.Visible = false;

                _AddText(row.Pos);
                _AddText(row.Name);
                _AddText(row.Rounds);
                _AddText(row.Won);
                _AddText(row.SingPoints);
                _AddText(row.GamePoints);

                _PlayerTable.Add(row);

                h = delta * (i + 1);
            }
            _PlayerTableScrollArea.H = h + delta;
        }

        private void _UpdatePlayerTable()
        {
            for (int i = 0; i < _PlayerTable.Count; i++)
            {
                STableRow row = _PlayerTable[i];

                if (i + _PlayerTableOffset < _PartyMode.GameData.ResultTable.Count)
                {
                    row.Pos.Visible = true;
                    row.Name.Visible = true;
                    row.Rounds.Visible = true;
                    row.Won.Visible = true;
                    row.SingPoints.Visible = true;
                    row.GamePoints.Visible = true;

                    row.Pos.Text = _PartyMode.GameData.ResultTable[i + _PlayerTableOffset].Position + ".";
                    row.Name.Text = CBase.Profiles.GetPlayerName(_PartyMode.GameData.ResultTable[i + _PlayerTableOffset].PlayerID);
                    row.Rounds.Text = _PartyMode.GameData.ResultTable[i + _PlayerTableOffset].NumPlayed.ToString();
                    row.Won.Text = _PartyMode.GameData.ResultTable[i + _PlayerTableOffset].NumWon.ToString();
                    row.SingPoints.Text = _PartyMode.GameData.ResultTable[i + _PlayerTableOffset].NumSingPoints.ToString();
                    row.GamePoints.Text = _PartyMode.GameData.ResultTable[i + _PlayerTableOffset].NumGamePoints.ToString();
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

            _Buttons[_ButtonPlayerScrollUp].Visible = _PlayerTableOffset > 0;
            _Buttons[_ButtonPlayerScrollDown].Visible = _PartyMode.GameData.ProfileIDs.Count - _NumPlayerVisible - _PlayerTableOffset > 0;
        }

        private void _ScrollPlayerTable(int offset)
        {
            if (_PartyMode.GameData.ProfileIDs.Count <= _NumPlayerVisible)
                _PlayerTableOffset = 0;
            else if (offset < 0 && _PlayerTableOffset + offset >= 0)
                _PlayerTableOffset += offset;
            else if (offset < 0 && _PlayerTableOffset + offset < 0)
                _PlayerTableOffset = 0;
            else if (offset > 0 && _PlayerTableOffset + offset <= _PartyMode.GameData.ProfileIDs.Count - _NumPlayerVisible)
                _PlayerTableOffset += offset;
            else if (offset > 0 && _PlayerTableOffset + offset > _PartyMode.GameData.ProfileIDs.Count - _NumPlayerVisible)
                _PlayerTableOffset = _PartyMode.GameData.ProfileIDs.Count - _NumPlayerVisible;

            _UpdatePlayerTable();
        }

        private void _ScrollRoundsTable(int offset)
        {
            if (_PartyMode.GameData.Rounds.Count <= _NumRoundsVisible)
                _RoundsTableOffset = 0;
            else if (offset < 0 && _RoundsTableOffset + offset >= 0)
                _RoundsTableOffset += offset;
            else if (offset < 0 && _RoundsTableOffset + offset < 0)
                _RoundsTableOffset = 0;
            else if (offset > 0 && _RoundsTableOffset + offset <= _PartyMode.GameData.Rounds.Count - _NumRoundsVisible)
                _RoundsTableOffset += offset;
            else if (offset > 0 && _RoundsTableOffset + offset > _PartyMode.GameData.Rounds.Count - _NumRoundsVisible)
                _RoundsTableOffset = _PartyMode.GameData.Rounds.Count - _NumRoundsVisible;

            _UpdateRoundsTable();
        }

        private string _GetPlayerWinString()
        {
            string s = "";

            for (int i = 0; i < _PartyMode.GameData.ResultTable.Count; i++)
            {
                if (_PartyMode.GameData.ResultTable[i].Position == 1)
                {
                    if (i > 0)
                        s += ", ";
                    s += CBase.Profiles.GetPlayerName(_PartyMode.GameData.ResultTable[i].PlayerID);
                }
                else
                    break;
            }

            return s;
        }
    }
}
