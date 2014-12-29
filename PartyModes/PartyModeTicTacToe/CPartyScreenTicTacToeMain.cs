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
using VocaluxeLib.Songs;

namespace VocaluxeLib.PartyModes.TicTacToe
{
    public enum EStatus
    {
        FieldChoosing,
        JokerRetry,
        FieldSelected,
        None
    }

    // ReSharper disable UnusedMember.Global
    public class CPartyScreenTicTacToeMain : CPartyScreenTicTacToe
        // ReSharper restore UnusedMember.Global
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _TextPopupReallyExit = "TextPopupReallyExit";
        private const string _TextTeamChoosing = "TextTeamChoosing";
        private const string _TextFinishMessage = "TextFinishMessage";
        private const string _TextNextPlayerT1 = "TextNextPlayerT1";
        private const string _TextNextPlayerT2 = "TextNextPlayerT2";
        private const string _TextNextPlayerNameT1 = "TextNextPlayerNameT1";
        private const string _TextNextPlayerNameT2 = "TextNextPlayerNameT2";

        private const string _ButtonNextRound = "ButtonNextRound";
        private const string _ButtonBack = "ButtonBack";
        private const string _ButtonExit = "ButtonExit";
        private const string _ButtonPopupYes = "ButtonPopupYes";
        private const string _ButtonPopupNo = "ButtonPopupNo";
        private const string _ButtonField = "ButtonField";

        private const string _ButtonJokerRandomT1 = "ButtonJokerRandomT1";
        private const string _ButtonJokerRandomT2 = "ButtonJokerRandomT2";
        private const string _ButtonJokerRetryT1 = "ButtonJokerRetryT1";
        private const string _ButtonJokerRetryT2 = "ButtonJokerRetryT2";

        private const string _StaticPopupBG = "StaticPopupBG";
        private const string _StaticAvatarT1 = "StaticAvatarT1";
        private const string _StaticAvatarT2 = "StaticAvatarT2";

        private bool _ExitPopupVisible;

        private List<CButton> _Fields = new List<CButton>();
        private float _FieldFirstX = 25;
        private float _FieldFirstY = 25;
        private const int _FieldSpace = 10;
        private float _FieldSize = 100;

        private int _OldSelectedField = -1;

        private int[,] _Possibilities;
        private EStatus _Status;

        public override void Init()
        {
            base.Init();

            _ThemeTexts = new string[]
                {_TextPopupReallyExit, _TextTeamChoosing, _TextFinishMessage, _TextNextPlayerT1, _TextNextPlayerT2, _TextNextPlayerNameT1, _TextNextPlayerNameT2};
            _ThemeButtons = new string[]
                {
                    _ButtonNextRound, _ButtonBack, _ButtonExit, _ButtonPopupYes, _ButtonPopupNo, _ButtonField, _ButtonJokerRandomT1, _ButtonJokerRandomT2, _ButtonJokerRetryT1,
                    _ButtonJokerRetryT2
                };
            _ThemeStatics = new string[] {_StaticPopupBG, _StaticAvatarT1, _StaticAvatarT2};
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _CreateFields();
            _Buttons[_ButtonField].Visible = false;
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
                            if (_PartyMode.GameData.CurrentRoundNr == 1 && _Status != EStatus.FieldSelected)
                                _Back();
                            else if (_Status == EStatus.None)
                                _EndParty();
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
                                _NextRound();
                            if (_Buttons[_ButtonBack].Selected && _PartyMode.GameData.CurrentRoundNr == 1 && _Status != EStatus.FieldSelected)
                                _Back();
                            if (_Buttons[_ButtonExit].Selected && (_PartyMode.GameData.CurrentRoundNr > 1 || _Status == EStatus.FieldSelected) && _Status != EStatus.None)
                                _ShowPopup(true);
                            else if (_Status == EStatus.None)
                                _EndParty();
                            for (int i = 0; i < _PartyMode.GameData.NumFields; i++)
                            {
                                switch (_Status)
                                {
                                    case EStatus.FieldChoosing:
                                        if (_Fields[i].Selected)
                                        {
                                            _PartyMode.GameData.FieldNr = i;
                                            _FieldSelected();
                                        }
                                        break;

                                    case EStatus.JokerRetry:
                                        if (_Fields[i].Selected)
                                        {
                                            _PartyMode.GameData.FieldNr = i;
                                            _FieldSelectedAgain();
                                        }
                                        break;
                                }
                            }
                            if (_Status == EStatus.FieldSelected)
                            {
                                if (_Buttons[_ButtonJokerRandomT1].Selected)
                                    _UseJoker(0, 0);
                                if (_Buttons[_ButtonJokerRandomT2].Selected)
                                    _UseJoker(1, 0);
                                if (_Buttons[_ButtonJokerRetryT1].Selected)
                                    _UseJoker(0, 1);
                                if (_Buttons[_ButtonJokerRetryT2].Selected)
                                    _UseJoker(1, 1);
                            }
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
                        _NextRound();
                    if (_Buttons[_ButtonBack].Selected)
                        _Back();
                    if (_Buttons[_ButtonExit].Selected)
                    {
                        if (_Status == EStatus.None)
                            _EndParty();
                        else
                            _ShowPopup(true);
                    }
                    for (int i = 0; i < _PartyMode.GameData.NumFields; i++)
                    {
                        switch (_Status)
                        {
                            case EStatus.FieldChoosing:
                                if (_Fields[i].Selected)
                                {
                                    _PartyMode.GameData.FieldNr = i;
                                    _FieldSelected();
                                }
                                break;

                            case EStatus.JokerRetry:
                                if (_Fields[i].Selected)
                                {
                                    _PartyMode.GameData.FieldNr = i;
                                    _FieldSelectedAgain();
                                }
                                break;
                        }
                    }
                    if (_Status == EStatus.FieldSelected)
                    {
                        if (_Buttons[_ButtonJokerRandomT1].Selected)
                            _UseJoker(0, 0);
                        if (_Buttons[_ButtonJokerRandomT2].Selected)
                            _UseJoker(1, 0);
                        if (_Buttons[_ButtonJokerRetryT1].Selected)
                            _UseJoker(0, 1);
                        if (_Buttons[_ButtonJokerRetryT2].Selected)
                            _UseJoker(1, 1);
                    }
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
                    if (_PartyMode.GameData.CurrentRoundNr == 1 && _Status != EStatus.FieldSelected)
                        _Back();
                    else if (_Status == EStatus.None)
                        _EndParty();
                    else
                        _ShowPopup(true);
                }
                else
                    _ShowPopup(false);
            }

            if (mouseEvent.Wheel != 0) {}

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            if (_PartyMode.GameData.CurrentRoundNr == 1)
            {
                _BuildWinnerPossibilities();
                _PartyMode.GameData.FieldNr = -1;
                _Buttons[_ButtonBack].Visible = true;
                _Buttons[_ButtonExit].Visible = false;
                _SelectElement(_Buttons[_ButtonBack]);
            }
            else
            {
                _Buttons[_ButtonBack].Visible = false;
                _Buttons[_ButtonExit].Visible = true;
                _SelectElement(_Buttons[_ButtonExit]);
            }

            _Status = EStatus.FieldChoosing;

            _UpdateFields();
            _UpdateFieldContents();

            int winner = _GetWinner();
            if (_PartyMode.GameData.CurrentRoundNr <= _PartyMode.GameData.NumFields && winner == 0)
            {
                _UpdateTeamChoosingMessage();
                _Texts[_TextNextPlayerT1].Visible = false;
                _Texts[_TextNextPlayerT2].Visible = false;
                _Texts[_TextNextPlayerNameT1].Visible = false;
                _Texts[_TextNextPlayerNameT2].Visible = false;
                _Statics[_StaticAvatarT1].Visible = false;
                _Statics[_StaticAvatarT2].Visible = false;
                _Buttons[_ButtonJokerRandomT1].Visible = false;
                _Buttons[_ButtonJokerRandomT2].Visible = false;
                _Buttons[_ButtonJokerRetryT1].Visible = false;
                _Buttons[_ButtonJokerRetryT2].Visible = false;
                _Buttons[_ButtonNextRound].Visible = false;
                _Texts[_TextFinishMessage].Visible = false;
            }
            else
            {
                _Status = EStatus.None;
                _Buttons[_ButtonNextRound].Visible = false;
                _Texts[_TextFinishMessage].Visible = true;
                _Texts[_TextTeamChoosing].Visible = false;
                if (winner > 0)
                {
                    _Texts[_TextFinishMessage].Color = CBase.Themes.GetPlayerColor(winner);
                    _Texts[_TextFinishMessage].Text = CBase.Language.Translate("TR_SCREENMAIN_WINNER", PartyModeID) + " " + CBase.Language.Translate("TR_TEAM", PartyModeID) + " " +
                                                      winner;
                }
                else
                {
                    _Texts[_TextFinishMessage].Color = new SColorF(1, 1, 1, 1);
                    _Texts[_TextFinishMessage].Text = CBase.Language.Translate("TR_SCREENMAIN_NOWINNER", PartyModeID);
                }
                _SelectElement(_Buttons[_ButtonExit]);
            }

            _ShowPopup(false);
        }

        public override bool UpdateGame()
        {
            return true;
        }

        private void _CreateFields()
        {
            for (int i = 0; i < 25; i++)
            {
                CButton f = GetNewButton(_Buttons[_ButtonField]);
                f.Visible = false;
                _AddButton(f);
                _Fields.Add(f);
            }
        }

        private void _UpdateFields()
        {
            var numOneRow = (int)Math.Sqrt(_PartyMode.GameData.NumFields);
            float fieldSizeY = ((float)CBase.Settings.GetRenderH() - 150 - numOneRow * _FieldSpace) / numOneRow;
            float fieldSizeX = ((float)CBase.Settings.GetRenderW() - 300 - numOneRow * _FieldSpace) / numOneRow;
            _FieldSize = Math.Min(fieldSizeX, fieldSizeY);
            _FieldFirstX = (float)CBase.Settings.GetRenderW() / 2 - (numOneRow * _FieldSize + (numOneRow - 1) * _FieldSpace) / 2;
            _FieldFirstY = 140 + (CBase.Settings.GetRenderH() - 140) / 2 - (numOneRow * _FieldSize + numOneRow * _FieldSpace) / 2;
            int row = 0;
            int column = 0;
            float x = _FieldFirstX;
            float y = _FieldFirstY;
            for (int i = 0; i < _Fields.Count; i++)
            {
                if (i < _PartyMode.GameData.NumFields)
                {
                    _Fields[i].W = _FieldSize;
                    _Fields[i].H = _FieldSize;
                    _Fields[i].X = x;
                    _Fields[i].Y = y;
                    _Fields[i].Visible = true;
                    _Fields[i].Selectable = true;
                    column++;
                    if ((i + 1) >= numOneRow * (row + 1))
                    {
                        column = 0;
                        row++;
                        y = _FieldFirstY + _FieldSize * row + _FieldSpace * row;
                        x = _FieldFirstX;
                    }
                    else
                        x = _FieldFirstX + _FieldSize * column + _FieldSpace * column;
                }
                else
                {
                    _Fields[i].Visible = false;
                    _Fields[i].Selectable = false;
                }
            }
        }

        private void _UpdateFieldContents()
        {
            for (int i = 0; i < _PartyMode.GameData.Rounds.Count; i++)
            {
                _Fields[i].Selectable = true;
                _Fields[i].Texture = _Buttons[_ButtonField].Texture;
                _Fields[i].Color = _Buttons[_ButtonField].Color;
                _Fields[i].SelColor = _Buttons[_ButtonField].SelColor;
                if (_PartyMode.GameData.Rounds[i].Finished)
                {
                    _Fields[i].Selectable = false;
                    _Fields[i].Texture = CBase.Songs.GetSongByID(_PartyMode.GameData.Rounds[i].SongID).CoverTextureBig;
                    _Fields[i].Color = CBase.Themes.GetPlayerColor(_PartyMode.GameData.Rounds[i].Winner);
                    _Fields[i].SelColor = CBase.Themes.GetPlayerColor(_PartyMode.GameData.Rounds[i].Winner);
                }
                if (_Status == EStatus.FieldSelected && _PartyMode.GameData.FieldNr == i)
                {
                    _Fields[i].Texture = CBase.Songs.GetSongByID(_PartyMode.GameData.Rounds[i].SongID).CoverTextureBig;
                    _Fields[i].Color = new SColorF(1, 1, 1, 1);
                    _Fields[i].SelColor = new SColorF(1, 1, 1, 1);
                    _Fields[i].Selectable = false;
                }
                if (_Status == EStatus.JokerRetry && _PartyMode.GameData.Rounds[i].Finished)
                {
                    _Fields[i].SelColor = CBase.Themes.GetPlayerColor(_PartyMode.GameData.Team + 1);
                    _Fields[i].Selectable = true;
                }
                if (_Status == EStatus.JokerRetry && !_PartyMode.GameData.Rounds[i].Finished)
                    _Fields[i].Selectable = false;
                if (_Status == EStatus.FieldSelected)
                    _Fields[i].Selectable = false;
            }
        }

        private void _FieldSelected(bool jokerUsed = false)
        {
            if (!jokerUsed)
            {
                int singerTeam1 = 0;
                int singerTeam2 = 0;

                if (_PartyMode.GameData.PlayerTeam1.Count > 0)
                {
                    singerTeam1 = _PartyMode.GameData.PlayerTeam1[0];
                    _PartyMode.GameData.PlayerTeam1.RemoveAt(0);
                }

                if (_PartyMode.GameData.PlayerTeam2.Count > 0)
                {
                    singerTeam2 = _PartyMode.GameData.PlayerTeam2[0];
                    _PartyMode.GameData.PlayerTeam2.RemoveAt(0);
                }
                _PartyMode.GameData.Rounds[_PartyMode.GameData.FieldNr].SingerTeam1 = singerTeam1;
                _PartyMode.GameData.Rounds[_PartyMode.GameData.FieldNr].SingerTeam2 = singerTeam2;

                _UpdatePlayerInformation();
            }
            _PartyMode.UpdateSongList();
            int songID = _PartyMode.GameData.Songs[0];
            _PartyMode.GameData.Songs.RemoveAt(0);

            _StartPreview(songID);
            _Status = EStatus.FieldSelected;
            _PartyMode.GameData.Rounds[_PartyMode.GameData.FieldNr].SongID = songID;
            _UpdateFieldContents();
            _UpdateJokerButtons();

            _Buttons[_ButtonNextRound].Visible = true;
            _Buttons[_ButtonExit].Visible = true;
            _Buttons[_ButtonBack].Visible = false;

            _SelectElement(_Buttons[_ButtonNextRound]);
        }

        private void _FieldSelectedAgain()
        {
            int songID = _PartyMode.GameData.Rounds[_OldSelectedField].SongID;
            _StartPreview(songID);
            _Status = EStatus.FieldSelected;
            _PartyMode.GameData.Rounds[_PartyMode.GameData.FieldNr].SongID = songID;
            _PartyMode.GameData.Rounds[_PartyMode.GameData.FieldNr].SingerTeam1 = _PartyMode.GameData.Rounds[_OldSelectedField].SingerTeam1;
            _PartyMode.GameData.Rounds[_PartyMode.GameData.FieldNr].SingerTeam2 = _PartyMode.GameData.Rounds[_OldSelectedField].SingerTeam2;
            _UpdateFieldContents();

            _UpdatePlayerInformation();
            _UpdateJokerButtons();

            _Buttons[_ButtonNextRound].Visible = true;
            _Buttons[_ButtonExit].Visible = true;
            _Buttons[_ButtonBack].Visible = false;

            _SelectElement(_Buttons[_ButtonNextRound]);
        }

        private void _StartPreview(int songID)
        {
            CSong song = CBase.Songs.GetSongByID(songID);
            CBase.BackgroundMusic.LoadPreview(song);
        }

        private void _UpdatePlayerInformation()
        {
            _Texts[_TextNextPlayerT1].Visible = true;
            _Texts[_TextNextPlayerT2].Visible = true;
            _Texts[_TextNextPlayerNameT1].Visible = true;
            _Texts[_TextNextPlayerNameT2].Visible = true;
            _Texts[_TextNextPlayerNameT1].Text =
                CBase.Profiles.GetPlayerName(_PartyMode.GameData.ProfileIDsTeam1[_PartyMode.GameData.Rounds[_PartyMode.GameData.FieldNr].SingerTeam1]);
            _Texts[_TextNextPlayerNameT2].Text =
                CBase.Profiles.GetPlayerName(_PartyMode.GameData.ProfileIDsTeam2[_PartyMode.GameData.Rounds[_PartyMode.GameData.FieldNr].SingerTeam2]);
            _Statics[_StaticAvatarT1].Visible = true;
            _Statics[_StaticAvatarT2].Visible = true;
            _Statics[_StaticAvatarT1].Texture = CBase.Profiles.GetAvatar(_PartyMode.GameData.ProfileIDsTeam1[_PartyMode.GameData.Rounds[_PartyMode.GameData.FieldNr].SingerTeam1]);
            _Statics[_StaticAvatarT2].Texture = CBase.Profiles.GetAvatar(_PartyMode.GameData.ProfileIDsTeam2[_PartyMode.GameData.Rounds[_PartyMode.GameData.FieldNr].SingerTeam2]);
        }

        private void _UseJoker(int teamNr, int jokerNum)
        {
            switch (jokerNum)
            {
                    //Random-Joker
                case 0:
                    if (_PartyMode.GameData.NumJokerRandom[teamNr] > 0)
                    {
                        _PartyMode.GameData.NumJokerRandom[teamNr]--;
                        _FieldSelected(true);
                    }
                    break;

                    //Retry-Joker
                case 1:
                    if (_PartyMode.GameData.NumJokerRetry[teamNr] > 0 && _PartyMode.GameData.CurrentRoundNr > 1)
                    {
                        _PartyMode.GameData.NumJokerRetry[teamNr]--;
                        _PartyMode.GameData.Team = teamNr;
                        _Status = EStatus.JokerRetry;
                        _OldSelectedField = _PartyMode.GameData.FieldNr;
                        _PartyMode.GameData.FieldNr = -1;
                        _UpdateFieldContents();
                        _Buttons[_ButtonNextRound].Visible = false;
                    }
                    break;
            }
            _UpdateJokerButtons();
        }

        private void _UpdateTeamChoosingMessage()
        {
            _Texts[_TextTeamChoosing].Color = CBase.Themes.GetPlayerColor(_PartyMode.GameData.Team + 1);
            _Texts[_TextTeamChoosing].Text = CBase.Language.Translate("TR_TEAM", PartyModeID) + " " + (_PartyMode.GameData.Team + 1) + "! " +
                                             CBase.Language.Translate("TR_SCREENMAIN_TEAM_CHOOSE", PartyModeID);
            if (_Status == EStatus.JokerRetry || _Status == EStatus.FieldChoosing)
                _Texts[_TextTeamChoosing].Visible = true;
            else
                _Texts[_TextTeamChoosing].Visible = false;
        }

        private void _UpdateJokerButtons()
        {
            _Buttons[_ButtonJokerRandomT1].Visible = true;
            _Buttons[_ButtonJokerRandomT2].Visible = true;
            _Buttons[_ButtonJokerRandomT1].Text.Text = _PartyMode.GameData.NumJokerRandom[0].ToString();
            _Buttons[_ButtonJokerRandomT2].Text.Text = _PartyMode.GameData.NumJokerRandom[1].ToString();
            _Buttons[_ButtonJokerRandomT1].Selectable = _PartyMode.GameData.NumJokerRandom[0] > 0;
            _Buttons[_ButtonJokerRandomT2].Selectable = _PartyMode.GameData.NumJokerRandom[1] > 0;
            _Buttons[_ButtonJokerRetryT1].Visible = true;
            _Buttons[_ButtonJokerRetryT2].Visible = true;
            _Buttons[_ButtonJokerRetryT1].Text.Text = _PartyMode.GameData.NumJokerRetry[0].ToString();
            _Buttons[_ButtonJokerRetryT2].Text.Text = _PartyMode.GameData.NumJokerRetry[1].ToString();
            _Buttons[_ButtonJokerRetryT1].Selectable = _PartyMode.GameData.NumJokerRetry[0] > 0 && _PartyMode.GameData.CurrentRoundNr > 1;
            _Buttons[_ButtonJokerRetryT2].Selectable = _PartyMode.GameData.NumJokerRetry[1] > 0 && _PartyMode.GameData.CurrentRoundNr > 1;
        }

        private void _NextRound()
        {
            _PartyMode.Next();
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

        private void _Back()
        {
            _PartyMode.Back();
        }

        private void _BuildWinnerPossibilities()
        {
            var numOneRow = (int)Math.Sqrt(_PartyMode.GameData.NumFields);
            _Possibilities = new int[(numOneRow * 2) + 2,numOneRow];
            for (int i = 0; i < _Possibilities.GetLength(0); i++)
            {
                if (i < numOneRow)
                {
                    for (int c = 0; c < numOneRow; c++)
                        _Possibilities[i, c] = i * numOneRow + c;
                }
                else if (i < numOneRow * 2)
                {
                    for (int c = 0; c < numOneRow; c++)
                        _Possibilities[i, c] = (i - numOneRow) + (c * numOneRow);
                }
                else if (i == _Possibilities.GetLength(0) - 2)
                {
                    for (int c = 0; c < numOneRow; c++)
                        _Possibilities[i, c] = (numOneRow + 1) * c;
                }
                else if (i == _Possibilities.GetLength(0) - 1)
                {
                    for (int c = 0; c < numOneRow; c++)
                        _Possibilities[i, c] = (numOneRow - 1) * c + (numOneRow - 1);
                }
            }
        }

        private int _GetWinner()
        {
            for (int i = 0; i < _Possibilities.GetLength(0); i++)
            {
                var check = new List<int>();
                for (int j = 0; j < _Possibilities.GetLength(1); j++)
                {
                    if (_PartyMode.GameData.Rounds[_Possibilities[i, j]].Winner > 0)
                        check.Add(_PartyMode.GameData.Rounds[_Possibilities[i, j]].Winner);
                }
                if (check.Count != _Possibilities.GetLength(1))
                    continue;
                //Check for winner
                if (check.Contains(1) && !check.Contains(2))
                    return 1;
                if (check.Contains(2) && !check.Contains(1))
                    return 2;
            }
            return 0;
        }
    }
}