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
using System.Diagnostics;
using System.Windows.Forms;
using VocaluxeLib.Menu;

namespace VocaluxeLib.PartyModes.Tournament
{
    class CMatchBox
    {
        public CStatic Box;
        public CStatic Active;
        public CStatic[] Avatars;
        public CText[] Names;
        public CText[] Scores;
    }

    public class CPartyScreenTournamentMain : CPartyScreenTournament
    {
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _TextPopupReallyExit = "TextPopupReallyExit";
        private const string _TextRoundLabel = "TextRoundLabel";
        private const string _TextMatchName = "TextMatchName";
        private const string _TextMatchNameWinner = "TextMatchNameWinner";
        private const string _TextMatchNameLoser = "TextMatchNameLoser";
        private const string _TextMatchScore = "TextMatchScore";
        private const string _TextChampionLabel = "TextChampionLabel";
        private const string _TextChampion = "TextChampion";
        private const string _TextNextMatch = "TextNextMatch";
        private const string _TextRematch = "TextRematch";

        private const string _ButtonNextRound = "ButtonNextRound";
        private const string _ButtonBack = "ButtonBack";
        private const string _ButtonExit = "ButtonExit";
        private const string _ButtonPopupYes = "ButtonPopupYes";
        private const string _ButtonPopupNo = "ButtonPopupNo";

        private const string _StaticPopupBG = "StaticPopupBG";
        private const string _StaticMatchBox = "StaticMatchBox";
        private const string _StaticMatchActive = "StaticMatchActive";
        private const string _StaticMatchAvatar = "StaticMatchAvatar";
        private const string _StaticBracketLine = "StaticBracketLine";

        private const string _ParticleEffectChampion = "ParticleEffectChampion";

        private const int _MaxMatches = 15;
        private const int _MaxColumns = 7;
        private const int _MaxLines = 32;
        private const float _Margin = 40f;
        private const float _AreaTop = 210f;
        private const float _AreaBottom = 830f;
        private const float _MaxBoxW = 380f;
        private const float _ColumnGap = 24f;
        private const float _ScoreW = 70f;
        private const float _RowH = 46f;
        private const float _BoxPadding = 4f;
        private const float _ActiveBorder = 4f;
        private const float _LineThickness = 3f;
        private const float _AdvanceDelay = 600f;
        private const float _AdvanceDuration = 900f;
        private const float _ChampionPopDuration = 700f;
        private const float _FlyZ = -0.5f;

        private bool _ExitPopupVisible;

        private List<CMatchBox> _MatchBoxes;
        private List<CText> _RoundLabels;
        private List<CStatic> _Lines;
        private int _NumLinesUsed;

        private readonly Stopwatch _AnimTimer = new Stopwatch();
        private CTournamentMatch _AdvanceMatch;
        private CTournamentMatch _AdvanceTarget;
        private int _AdvanceSlot;
        private bool _ChampionShown;
        private float _ChampionHeight;

        private CStatic _FlyBorder;
        private CStatic _FlyBox;
        private CStatic _FlyAvatar;
        private CText _FlyName;

        public override void Init()
        {
            base.Init();

            _ThemeTexts = new string[]
            {
                _TextPopupReallyExit, _TextRoundLabel, _TextMatchName, _TextMatchNameWinner, _TextMatchNameLoser, _TextMatchScore, _TextChampionLabel, _TextChampion,
                _TextNextMatch, _TextRematch
            };
            _ThemeButtons = new string[] { _ButtonNextRound, _ButtonBack, _ButtonExit, _ButtonPopupYes, _ButtonPopupNo };
            _ThemeStatics = new string[] { _StaticPopupBG, _StaticMatchBox, _StaticMatchActive, _StaticMatchAvatar, _StaticBracketLine };
            _ThemeParticleEffects = new string[] { _ParticleEffectChampion };
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _Lines = new List<CStatic>();
            for (var i = 0; i < _MaxLines; i++)
            {
                var line = GetNewStatic(_Statics[_StaticBracketLine]);
                _AddStatic(line);
                _Lines.Add(line);
            }

            _MatchBoxes = new List<CMatchBox>();
            for (var i = 0; i < _MaxMatches; i++)
            {
                var matchBox = new CMatchBox
                {
                    Active = GetNewStatic(_Statics[_StaticMatchActive]),
                    Box = GetNewStatic(_Statics[_StaticMatchBox]),
                    Avatars = new CStatic[CPartyModeTournament.NumPlayerAtOnce],
                    Names = new CText[CPartyModeTournament.NumPlayerAtOnce],
                    Scores = new CText[CPartyModeTournament.NumPlayerAtOnce]
                };
                _AddStatic(matchBox.Active);
                _AddStatic(matchBox.Box);

                for (var p = 0; p < CPartyModeTournament.NumPlayerAtOnce; p++)
                {
                    matchBox.Avatars[p] = GetNewStatic(_Statics[_StaticMatchAvatar]);
                    matchBox.Avatars[p].Aspect = EAspect.Crop;
                    _AddStatic(matchBox.Avatars[p]);
                    matchBox.Names[p] = GetNewText(_Texts[_TextMatchName]);
                    _AddText(matchBox.Names[p]);
                    matchBox.Scores[p] = GetNewText(_Texts[_TextMatchScore]);
                    _AddText(matchBox.Scores[p]);
                }

                _MatchBoxes.Add(matchBox);
            }

            _RoundLabels = new List<CText>();
            for (var i = 0; i < _MaxColumns; i++)
            {
                var label = GetNewText(_Texts[_TextRoundLabel]);
                _AddText(label);
                _RoundLabels.Add(label);
            }

            _Statics[_StaticMatchBox].Visible = false;
            _Statics[_StaticMatchActive].Visible = false;
            _Statics[_StaticMatchAvatar].Visible = false;
            _Statics[_StaticBracketLine].Visible = false;
            _Texts[_TextRoundLabel].Visible = false;
            _Texts[_TextMatchName].Visible = false;
            _Texts[_TextMatchNameWinner].Visible = false;
            _Texts[_TextMatchNameLoser].Visible = false;
            _Texts[_TextMatchScore].Visible = false;

            _FlyBorder = GetNewStatic(_Statics[_StaticMatchActive]);
            _FlyBox = GetNewStatic(_Statics[_StaticMatchBox]);
            _FlyAvatar = GetNewStatic(_Statics[_StaticMatchAvatar]);
            _FlyAvatar.Aspect = EAspect.Crop;
            _FlyName = GetNewText(_Texts[_TextMatchNameWinner]);
            _FlyName.Z = _FlyZ - 0.15f;
            _AddStatic(_FlyBorder);
            _AddStatic(_FlyBox);
            _AddStatic(_FlyAvatar);
            _AddText(_FlyName);
            _SetFlyVisible(false);

            _ChampionHeight = _Texts[_TextChampion].Font.Height;
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (!keyEvent.KeyPressed)
            {
                switch (keyEvent.Key)
                {
                    case Keys.Back:
                    case Keys.Escape:
                        _OnBack();
                        break;

                    case Keys.Enter:
                        _OnSelect();
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
                _OnSelect();
            }

            if (mouseEvent.RB)
            {
                _OnBack();
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            _AdvanceMatch = _PartyMode.GameData.LastDecidedMatch;
            _PartyMode.GameData.LastDecidedMatch = null;
            _AdvanceTarget = null;
            if (_AdvanceMatch != null && _AdvanceMatch.Round + 1 < _PartyMode.NumRounds)
            {
                _AdvanceTarget = _PartyMode.GameData.Rounds[_AdvanceMatch.Round + 1][_AdvanceMatch.Index / 2];
                _AdvanceSlot = _AdvanceMatch.Index % 2;
            }

            _ChampionShown = _PartyMode.IsFinished && _AdvanceMatch == null;
            _AnimTimer.Reset();
            _AnimTimer.Start();
            _SetFlyVisible(false);

            _UpdateBracket();
            _UpdateInfo();

            var started = _PartyMode.GameData.NumMatchesPlayed > 0;
            _Buttons[_ButtonBack].Visible = !started;
            _Buttons[_ButtonExit].Visible = started;
            _Buttons[_ButtonNextRound].Visible = !_PartyMode.IsFinished;

            _ShowPopup(false);

            if (_PartyMode.IsFinished)
            {
                _SelectElement(_Buttons[_ButtonExit]);
            }
            else
            {
                _SelectElement(_Buttons[_ButtonNextRound]);
            }
        }

        public override bool UpdateGame()
        {
            if (_AdvanceMatch != null)
            {
                _UpdateAdvance();
            }

            if (_ChampionShown)
            {
                _UpdateChampion();
            }

            return true;
        }

        private static float _EaseInOut(float t)
        {
            return t < 0.5f ? 4f * t * t * t : 1f - (float)Math.Pow(-2f * t + 2f, 3) / 2f;
        }

        private static float _EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * (float)Math.Pow(t - 1f, 3) + c1 * (float)Math.Pow(t - 1f, 2);
        }

        private void _SetFlyVisible(bool visible)
        {
            _FlyBorder.Visible = visible;
            _FlyBox.Visible = visible;
            _FlyAvatar.Visible = visible;
            _FlyName.Visible = visible;
        }

        private SRectF _GetRowRect(int round, int index, int slot)
        {
            var box = _GetBoxRect(round, index);
            return new SRectF(box.X, box.Y + _BoxPadding + slot * _RowH, box.W, _RowH, 0f);
        }

        private void _UpdateAdvance()
        {
            var time = _AnimTimer.ElapsedMilliseconds - _AdvanceDelay;
            if (time < 0)
            {
                return;
            }

            if (_AdvanceTarget == null)
            {
                _AdvanceMatch = null;
                _ChampionShown = true;
                _AnimTimer.Reset();
                _AnimTimer.Start();
                _UpdateInfo();
                return;
            }

            var progress = Math.Min(1f, time / _AdvanceDuration);
            if (progress >= 1f)
            {
                _AdvanceMatch = null;
                _AdvanceTarget = null;
                _SetFlyVisible(false);
                _UpdateBracket();
                return;
            }

            var from = _GetRowRect(_AdvanceMatch.Round, _AdvanceMatch.Index, _AdvanceMatch.Winner);
            var to = _GetRowRect(_AdvanceTarget.Round, _AdvanceTarget.Index, _AdvanceSlot);
            var eased = _EaseInOut(progress);
            var row = new SRectF(from.X + (to.X - from.X) * eased, from.Y + (to.Y - from.Y) * eased, from.W + (to.W - from.W) * eased, _RowH, 0f);

            var avatarSize = _RowH - 8f;
            _FlyBox.MaxRect = new SRectF(row.X, row.Y, row.W, row.H, _FlyZ - 0.05f);
            _FlyBorder.MaxRect = new SRectF(row.X - _ActiveBorder, row.Y - _ActiveBorder, row.W + 2 * _ActiveBorder, row.H + 2 * _ActiveBorder, _FlyZ);
            _FlyAvatar.MaxRect = new SRectF(row.X + 8f, row.Y + 4f, avatarSize, avatarSize, _FlyZ - 0.1f);
            _FlyAvatar.Texture = CBase.Profiles.GetAvatar(_PartyMode.GameData.ProfileIds[_AdvanceMatch.WinnerPlayer]);
            _FlyName.Text = _GetName(_AdvanceMatch.WinnerPlayer);
            _FlyName.X = row.X + 16f + avatarSize;
            _FlyName.Y = row.Y + (_RowH - _FlyName.Rect.H) / 2;
            _FlyName.W = row.W - avatarSize - 24f;
            _SetFlyVisible(true);
        }

        private void _UpdateChampion()
        {
            var time = _AnimTimer.ElapsedMilliseconds;
            float scale;
            if (time < _ChampionPopDuration)
            {
                scale = 0.3f + 0.7f * _EaseOutBack(time / _ChampionPopDuration);
            }
            else
            {
                scale = 1f + 0.03f * (float)Math.Sin((time - _ChampionPopDuration) / 1000.0 * Math.PI * 1.4);
            }

            _Texts[_TextChampion].Font.Height = _ChampionHeight * scale;
        }

        private void _OnBack()
        {
            if (_ExitPopupVisible)
            {
                _ShowPopup(false);
            }
            else if (_PartyMode.GameData.NumMatchesPlayed == 0)
            {
                _PartyMode.Back();
            }
            else if (_PartyMode.IsFinished)
            {
                _EndParty();
            }
            else
            {
                _ShowPopup(true);
            }
        }

        private void _OnSelect()
        {
            if (_ExitPopupVisible)
            {
                if (_Buttons[_ButtonPopupYes].Selected)
                {
                    _EndParty();
                }

                if (_Buttons[_ButtonPopupNo].Selected)
                {
                    _ShowPopup(false);
                }

                return;
            }

            if (_Buttons[_ButtonNextRound].Selected && _Buttons[_ButtonNextRound].Visible)
            {
                _PartyMode.Next();
            }

            if (_Buttons[_ButtonBack].Selected && _Buttons[_ButtonBack].Visible)
            {
                _PartyMode.Back();
            }

            if (_Buttons[_ButtonExit].Selected && _Buttons[_ButtonExit].Visible)
            {
                if (_PartyMode.IsFinished)
                {
                    _EndParty();
                }
                else
                {
                    _ShowPopup(true);
                }
            }
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
            {
                _SelectElement(_Buttons[_ButtonPopupNo]);
            }
        }

        private string _GetName(int player)
        {
            return CBase.Profiles.GetPlayerName(_PartyMode.GameData.ProfileIds[player]);
        }

        private string _GetRoundName(int round)
        {
            var fromEnd = _PartyMode.NumRounds - 1 - round;
            switch (fromEnd)
            {
                case 0:
                    return "TR_SCREENMAIN_ROUND_FINAL";
                case 1:
                    return "TR_SCREENMAIN_ROUND_SEMIFINAL";
                case 2:
                    return "TR_SCREENMAIN_ROUND_QUARTERFINAL";
                default:
                    return "TR_SCREENMAIN_ROUND_EIGHTHFINAL";
            }
        }

        private int _GetColumn(int round, int index)
        {
            var numRounds = _PartyMode.NumRounds;
            if (round == numRounds - 1)
            {
                return round;
            }

            var count = _PartyMode.GameData.Rounds[round].Length;
            return index < count / 2 ? round : 2 * numRounds - 2 - round;
        }

        private float _GetColumnWidth()
        {
            var numColumns = 2 * _PartyMode.NumRounds - 1;
            return (CBase.Settings.GetRenderW() - 2 * _Margin) / numColumns;
        }

        private float _GetBoxWidth()
        {
            return Math.Min(_GetColumnWidth() - _ColumnGap, _MaxBoxW);
        }

        private float _GetBoxHeight()
        {
            return CPartyModeTournament.NumPlayerAtOnce * _RowH + 2 * _BoxPadding;
        }

        private SRectF _GetBoxRect(int round, int index)
        {
            var columnW = _GetColumnWidth();
            var boxW = _GetBoxWidth();
            var boxH = _GetBoxHeight();
            var x = _Margin + _GetColumn(round, index) * columnW + (columnW - boxW) / 2;

            var areaH = _AreaBottom - _AreaTop;
            float centerY;
            if (round == _PartyMode.NumRounds - 1)
            {
                centerY = _AreaTop + areaH / 2;
            }
            else
            {
                var perSide = _PartyMode.GameData.Rounds[round].Length / 2;
                var position = index % perSide;
                centerY = _AreaTop + areaH * (position + 0.5f) / perSide;
            }

            return new SRectF(x, centerY - boxH / 2, boxW, boxH, _Statics[_StaticMatchBox].Rect.Z);
        }

        private void _UpdateBracket()
        {
            var current = _PartyMode.CurrentMatch;
            var boxNr = 0;
            _NumLinesUsed = 0;

            foreach (var round in _PartyMode.GameData.Rounds)
            {
                foreach (var match in round)
                {
                    _UpdateMatchBox(_MatchBoxes[boxNr], match, match == current);
                    boxNr++;

                    if (match.Round < _PartyMode.NumRounds - 1)
                    {
                        _AddConnector(match);
                    }
                }
            }

            for (var i = boxNr; i < _MatchBoxes.Count; i++)
            {
                _SetMatchBoxVisible(_MatchBoxes[i], false);
            }

            for (var i = _NumLinesUsed; i < _Lines.Count; i++)
            {
                _Lines[i].Visible = false;
            }

            var numColumns = 2 * _PartyMode.NumRounds - 1;
            var columnW = _GetColumnWidth();
            for (var c = 0; c < _RoundLabels.Count; c++)
            {
                _RoundLabels[c].Visible = c < numColumns;
                if (c >= numColumns)
                {
                    continue;
                }

                var round = c < _PartyMode.NumRounds ? c : numColumns - 1 - c;
                _RoundLabels[c].X = _Margin + c * columnW + columnW / 2;
                _RoundLabels[c].Text = _GetRoundName(round);
            }

            var final = _GetBoxRect(_PartyMode.NumRounds - 1, 0);
            _Texts[_TextChampionLabel].X = final.X + final.W / 2;
            _Texts[_TextChampion].X = final.X + final.W / 2;
        }

        private void _SetMatchBoxVisible(CMatchBox matchBox, bool visible)
        {
            matchBox.Box.Visible = visible;
            matchBox.Active.Visible = false;
            for (var p = 0; p < CPartyModeTournament.NumPlayerAtOnce; p++)
            {
                matchBox.Avatars[p].Visible = visible;
                matchBox.Names[p].Visible = visible;
                matchBox.Scores[p].Visible = visible;
            }
        }

        private void _UpdateMatchBox(CMatchBox matchBox, CTournamentMatch match, bool isCurrent)
        {
            var rect = _GetBoxRect(match.Round, match.Index);
            _SetMatchBoxVisible(matchBox, true);

            matchBox.Box.MaxRect = rect;
            matchBox.Active.MaxRect = new SRectF(rect.X - _ActiveBorder, rect.Y - _ActiveBorder, rect.W + 2 * _ActiveBorder, rect.H + 2 * _ActiveBorder, _Statics[_StaticMatchActive].Rect.Z);
            matchBox.Active.Visible = isCurrent;

            var avatarSize = _RowH - 8f;
            var showScore = (match.Played && !match.Bye) || (_PartyMode.GameData.IsRematch && isCurrent);
            for (var p = 0; p < CPartyModeTournament.NumPlayerAtOnce; p++)
            {
                var player = p == 0 ? match.Player1 : match.Player2;
                if (match == _AdvanceTarget && p == _AdvanceSlot)
                {
                    player = -1;
                }

                var rowY = rect.Y + _BoxPadding + p * _RowH;

                var avatar = matchBox.Avatars[p];
                avatar.MaxRect = new SRectF(rect.X + 8f, rowY + 4f, avatarSize, avatarSize, _Statics[_StaticMatchAvatar].Rect.Z);
                avatar.Texture = player >= 0 ? CBase.Profiles.GetAvatar(_PartyMode.GameData.ProfileIds[player]) : null;
                avatar.Visible = player >= 0;

                var name = matchBox.Names[p];
                name.X = rect.X + 16f + avatarSize;
                name.Y = rowY + (_RowH - name.Rect.H) / 2;
                name.W = rect.W - avatarSize - 24f - (showScore ? _ScoreW : 0f);

                var score = matchBox.Scores[p];
                score.X = rect.X + rect.W - 10f;
                score.Y = name.Y;

                if (player >= 0)
                {
                    name.Text = _GetName(player);
                }
                else if (match.Bye)
                {
                    name.Text = "TR_SCREENMAIN_BYE";
                }
                else
                {
                    name.Text = "?";
                }

                score.Text = showScore ? (p == 0 ? match.Score1 : match.Score2).ToString() : string.Empty;

                if (match.Played)
                {
                    name.Color = match.Winner == p ? _Texts[_TextMatchNameWinner].Color : _Texts[_TextMatchNameLoser].Color;
                }
                else
                {
                    name.Color = player >= 0 ? _Texts[_TextMatchName].Color : _Texts[_TextMatchNameLoser].Color;
                }

                score.Color = name.Color;
            }
        }

        private void _AddLine(float x1, float y1, float x2, float y2)
        {
            if (_NumLinesUsed >= _Lines.Count)
            {
                return;
            }

            var line = _Lines[_NumLinesUsed++];
            var z = _Statics[_StaticBracketLine].Rect.Z;
            if (Math.Abs(y1 - y2) < 0.5f)
            {
                line.MaxRect = new SRectF(Math.Min(x1, x2), y1 - _LineThickness / 2, Math.Abs(x2 - x1), _LineThickness, z);
            }
            else
            {
                line.MaxRect = new SRectF(x1 - _LineThickness / 2, Math.Min(y1, y2), _LineThickness, Math.Abs(y2 - y1), z);
            }

            line.Visible = true;
        }

        private void _AddConnector(CTournamentMatch match)
        {
            var box = _GetBoxRect(match.Round, match.Index);
            var target = _GetBoxRect(match.Round + 1, match.Index / 2);
            var leftSide = _GetColumn(match.Round, match.Index) < _PartyMode.NumRounds - 1;
            var boxEdge = leftSide ? box.X + box.W : box.X;
            var targetEdge = leftSide ? target.X : target.X + target.W;
            var boxCenterY = box.Y + box.H / 2;
            var targetCenterY = target.Y + target.H / 2;

            if (match.Round + 1 == _PartyMode.NumRounds - 1)
            {
                _AddLine(boxEdge, boxCenterY, targetEdge, boxCenterY);
                return;
            }

            var middleX = (boxEdge + targetEdge) / 2;
            _AddLine(boxEdge, boxCenterY, middleX, boxCenterY);

            if (match.Index % 2 == 0)
            {
                var sibling = _GetBoxRect(match.Round, match.Index + 1);
                _AddLine(middleX, boxCenterY, middleX, sibling.Y + sibling.H / 2);
                _AddLine(middleX, targetCenterY, targetEdge, targetCenterY);
            }
        }

        private void _UpdateInfo()
        {
            var current = _PartyMode.CurrentMatch;
            var finished = _PartyMode.IsFinished;

            _Texts[_TextChampion].Visible = finished && _ChampionShown;
            _ParticleEffects[_ParticleEffectChampion].Visible = finished && _ChampionShown;
            _Texts[_TextNextMatch].Visible = !finished && current != null;
            _Texts[_TextRematch].Visible = !finished && _PartyMode.GameData.IsRematch;

            if (finished)
            {
                _Texts[_TextChampion].Text = _GetName(_PartyMode.FinalMatch.WinnerPlayer);
            }
            else if (current != null)
            {
                _Texts[_TextNextMatch].Text = CBase.Language.Translate("TR_SCREENMAIN_NEXT_MATCH", PartyModeId) + " " + _GetName(current.Player1) + " " +
                                              CBase.Language.Translate("TR_SCREENMAIN_VERSUS", PartyModeId) + " " + _GetName(current.Player2);
            }
        }
    }
}
