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

namespace VocaluxeLib.PartyModes.RuleRoulette
{
    class CWheelCard
    {
        public CStatic Active;
        public CStatic Box;
        public CText Name;
    }

    class CTableRow
    {
        public CStatic Highlight;
        public CStatic Avatar;
        public CText Pos;
        public CText Name;
        public CText Points;
    }

    public class CPartyScreenRuleRouletteMain : CPartyScreenRuleRoulette
    {
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _TextPopupReallyExit = "TextPopupReallyExit";
        private const string _TextDuel = "TextDuel";
        private const string _TextRowPos = "TextRowPos";
        private const string _TextRowName = "TextRowName";
        private const string _TextRowPoints = "TextRowPoints";
        private const string _TextCardName = "TextCardName";
        private const string _TextCardNameResult = "TextCardNameResult";
        private const string _TextName1 = "TextName1";
        private const string _TextName2 = "TextName2";
        private const string _TextVersus = "TextVersus";
        private const string _TextRuleTitle = "TextRuleTitle";
        private const string _TextRuleDescription = "TextRuleDescription";
        private const string _TextRuleInfo = "TextRuleInfo";
        private const string _TextLastResult = "TextLastResult";
        private const string _TextChampionLabel = "TextChampionLabel";
        private const string _TextChampion = "TextChampion";

        private const string _ButtonNextRound = "ButtonNextRound";
        private const string _ButtonBack = "ButtonBack";
        private const string _ButtonExit = "ButtonExit";
        private const string _ButtonPopupYes = "ButtonPopupYes";
        private const string _ButtonPopupNo = "ButtonPopupNo";

        private const string _StaticPopupBG = "StaticPopupBG";
        private const string _StaticRowHighlight = "StaticRowHighlight";
        private const string _StaticRowAvatar = "StaticRowAvatar";
        private const string _StaticCardBox = "StaticCardBox";
        private const string _StaticCardActive = "StaticCardActive";
        private const string _StaticAvatar1 = "StaticAvatar1";
        private const string _StaticAvatar2 = "StaticAvatar2";

        private const string _ParticleEffectChampion = "ParticleEffectChampion";

        private const float _WheelCenterX = 1240f;
        private const float _WheelCenterY = 530f;
        private const float _WheelRadiusX = 545f;
        private const float _WheelRadiusY = 320f;
        private const float _ActiveBorder = 4f;
        private const float _UsedAlpha = 0.35f;
        private const float _FirstRowY = 255f;
        private const float _RowStep = 52f;
        private const float _PopDuration = 700f;
        private const int _SpinRounds = 3;
        private const float _SpinMinStep = 50f;
        private const float _SpinMaxStep = 420f;

        private readonly string[] _RankColors = new string[] { "Top1Color", "Top2Color", "Top3Color" };

        private bool _ExitPopupVisible;
        private List<CWheelCard> _Cards;
        private List<CTableRow> _Rows;
        private readonly Stopwatch _AnimTimer = new Stopwatch();
        private readonly Stopwatch _SpinTimer = new Stopwatch();
        private List<float> _SpinSteps;
        private int _SpinTarget = -1;
        private bool _Spinning;
        private int _LastSeenDuels = -1;
        private bool _ShowNewResult;
        private float _LastResultHeight;
        private float _ChampionHeight;
        private SRectF _Avatar1Rect;

        public override void Init()
        {
            base.Init();

            _ThemeTexts = new string[]
            {
                _TextPopupReallyExit, _TextDuel, _TextRowPos, _TextRowName, _TextRowPoints, _TextCardName, _TextCardNameResult, _TextName1, _TextName2, _TextVersus,
                _TextRuleTitle, _TextRuleDescription, _TextRuleInfo, _TextLastResult, _TextChampionLabel, _TextChampion
            };
            _ThemeButtons = new string[] { _ButtonNextRound, _ButtonBack, _ButtonExit, _ButtonPopupYes, _ButtonPopupNo };
            _ThemeStatics = new string[] { _StaticPopupBG, _StaticRowHighlight, _StaticRowAvatar, _StaticCardBox, _StaticCardActive, _StaticAvatar1, _StaticAvatar2 };
            _ThemeParticleEffects = new string[] { _ParticleEffectChampion };
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _Cards = new List<CWheelCard>();
            for (var i = 0; i < CPartyModeRuleRoulette.MaxRulesOnWheel; i++)
            {
                var card = new CWheelCard
                {
                    Active = GetNewStatic(_Statics[_StaticCardActive]),
                    Box = GetNewStatic(_Statics[_StaticCardBox]),
                    Name = GetNewText(_Texts[_TextCardName])
                };
                _AddStatic(card.Active);
                _AddStatic(card.Box);
                _AddText(card.Name);
                _Cards.Add(card);
            }

            _Rows = new List<CTableRow>();
            for (var i = 0; i < _PartyMode.MaxPlayers; i++)
            {
                var row = new CTableRow
                {
                    Highlight = GetNewStatic(_Statics[_StaticRowHighlight]),
                    Avatar = GetNewStatic(_Statics[_StaticRowAvatar]),
                    Pos = GetNewText(_Texts[_TextRowPos]),
                    Name = GetNewText(_Texts[_TextRowName]),
                    Points = GetNewText(_Texts[_TextRowPoints])
                };
                row.Avatar.Aspect = EAspect.Crop;
                var y = _FirstRowY + i * _RowStep;
                row.Highlight.Y = y + (row.Name.H - row.Highlight.H) / 2;
                row.Avatar.Y = y + (row.Name.H - row.Avatar.H) / 2;
                row.Pos.Y = y;
                row.Name.Y = y;
                row.Points.Y = y;
                _AddStatic(row.Highlight);
                _AddStatic(row.Avatar);
                _AddText(row.Pos);
                _AddText(row.Name);
                _AddText(row.Points);
                _Rows.Add(row);
            }

            _Statics[_StaticCardBox].Visible = false;
            _Statics[_StaticCardActive].Visible = false;
            _Statics[_StaticRowHighlight].Visible = false;
            _Statics[_StaticRowAvatar].Visible = false;
            _Texts[_TextCardName].Visible = false;
            _Texts[_TextCardNameResult].Visible = false;
            _Texts[_TextRowPos].Visible = false;
            _Texts[_TextRowName].Visible = false;
            _Texts[_TextRowPoints].Visible = false;
            _Statics[_StaticAvatar1].Aspect = EAspect.Crop;
            _Statics[_StaticAvatar2].Aspect = EAspect.Crop;
            _Avatar1Rect = _Statics[_StaticAvatar1].Rect;

            _LastResultHeight = _Texts[_TextLastResult].Font.Height;
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

            _ShowNewResult = _PartyMode.GameData.LastDuel != null && _PartyMode.GameData.NumDuelsPlayed != _LastSeenDuels;
            _LastSeenDuels = _PartyMode.GameData.NumDuelsPlayed;
            _Spinning = false;
            _SpinTarget = _PartyMode.GameData.RuleChosen && _PartyMode.CurrentDuel != null ? _PartyMode.GameData.Wheel.IndexOf(_PartyMode.CurrentDuel.Rule) : -1;

            _LayoutCards();
            _UpdateTable();
            _UpdateCenter();
            _UpdateCards(_SpinTarget);
            _UpdateButtons();

            _AnimTimer.Reset();
            _AnimTimer.Start();
            _UpdateAnimations();

            _ShowPopup(false);
        }

        public override bool UpdateGame()
        {
            if (_Spinning)
            {
                _UpdateSpin();
            }

            _UpdateAnimations();
            return true;
        }

        private void _OnBack()
        {
            if (_Spinning)
            {
                return;
            }

            if (_ExitPopupVisible)
            {
                _ShowPopup(false);
            }
            else if (_PartyMode.GameData.NumDuelsPlayed == 0 && !_PartyMode.GameData.RuleChosen)
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
            if (_Spinning)
            {
                return;
            }

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
                if (_PartyMode.GameData.RuleChosen)
                {
                    _PartyMode.Next();
                }
                else
                {
                    _StartSpin();
                }
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

        private void _UpdateButtons()
        {
            var started = _PartyMode.GameData.NumDuelsPlayed > 0 || _PartyMode.GameData.RuleChosen;
            _Buttons[_ButtonBack].Visible = !started;
            _Buttons[_ButtonExit].Visible = started;
            _Buttons[_ButtonNextRound].Visible = !_PartyMode.IsFinished;
            _Buttons[_ButtonNextRound].Text.Text = _PartyMode.GameData.RuleChosen ? "TR_BUTTON_START_DUEL" : "TR_BUTTON_SPIN";

            if (_PartyMode.IsFinished)
            {
                _SelectElement(_Buttons[_ButtonExit]);
            }
            else
            {
                _SelectElement(_Buttons[_ButtonNextRound]);
            }
        }

        private static float _EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * (float)Math.Pow(t - 1f, 3) + c1 * (float)Math.Pow(t - 1f, 2);
        }

        private string _GetName(CRoulettePlayer player)
        {
            return CBase.Profiles.GetPlayerName(player.ProfileId);
        }

        private string _Translate(string key)
        {
            return CBase.Language.Translate(key, PartyModeId);
        }

        private void _LayoutCards()
        {
            var wheel = _PartyMode.GameData.Wheel;
            var box = _Statics[_StaticCardBox].Rect;
            for (var i = 0; i < _Cards.Count; i++)
            {
                var card = _Cards[i];
                var visible = !_PartyMode.IsFinished && i < wheel.Count;
                card.Box.Visible = visible;
                card.Name.Visible = visible;
                card.Active.Visible = false;
                if (!visible)
                {
                    continue;
                }

                var angle = 2 * Math.PI * i / wheel.Count - Math.PI / 2;
                var centerX = _WheelCenterX + _WheelRadiusX * (float)Math.Cos(angle);
                var centerY = _WheelCenterY + _WheelRadiusY * (float)Math.Sin(angle);
                card.Box.MaxRect = new SRectF(centerX - box.W / 2, centerY - box.H / 2, box.W, box.H, box.Z);
                card.Active.MaxRect = new SRectF(centerX - box.W / 2 - _ActiveBorder, centerY - box.H / 2 - _ActiveBorder, box.W + 2 * _ActiveBorder,
                    box.H + 2 * _ActiveBorder, _Statics[_StaticCardActive].Rect.Z);
                card.Name.X = centerX;
                card.Name.Y = centerY - card.Name.H / 2;
                card.Name.Text = CPartyModeRuleRoulette.GetRuleKey(wheel[i]);
            }
        }

        private void _UpdateCards(int highlight)
        {
            var wheel = _PartyMode.GameData.Wheel;
            for (var i = 0; i < _Cards.Count && i < wheel.Count; i++)
            {
                var card = _Cards[i];
                if (!card.Box.Visible)
                {
                    continue;
                }

                var used = _PartyMode.GameData.UsedInCycle.Contains(wheel[i]) && i != _SpinTarget;
                var isResult = !_Spinning && i == _SpinTarget;
                var alpha = used && i != highlight ? _UsedAlpha : 1f;
                var boxColor = _Statics[_StaticCardBox].Color;
                card.Box.Color = new SColorF(boxColor.R, boxColor.G, boxColor.B, boxColor.A * alpha);
                var nameColor = isResult ? _Texts[_TextCardNameResult].Color : _Texts[_TextCardName].Color;
                card.Name.Color = new SColorF(nameColor.R, nameColor.G, nameColor.B, nameColor.A * alpha);
                card.Active.Visible = i == highlight;
            }
        }

        private void _StartSpin()
        {
            int target;
            _PartyMode.SpinWheel(out target);
            if (target < 0)
            {
                return;
            }

            _SpinTarget = target;
            var count = _PartyMode.GameData.Wheel.Count;
            var steps = _SpinRounds * count + target;
            _SpinSteps = new List<float>();
            var time = 0f;
            for (var i = 0; i < steps; i++)
            {
                var progress = (float)i / steps;
                time += _SpinMinStep + (_SpinMaxStep - _SpinMinStep) * progress * progress;
                _SpinSteps.Add(time);
            }

            _Spinning = true;
            _SpinTimer.Reset();
            _SpinTimer.Start();
            _UpdateCenter();
            _Buttons[_ButtonNextRound].Visible = false;
            _Buttons[_ButtonBack].Visible = false;
            _Buttons[_ButtonExit].Visible = true;
        }

        private void _UpdateSpin()
        {
            var elapsed = _SpinTimer.ElapsedMilliseconds;
            var step = 0;
            while (step < _SpinSteps.Count && _SpinSteps[step] <= elapsed)
            {
                step++;
            }

            var count = _PartyMode.GameData.Wheel.Count;
            if (step >= _SpinSteps.Count && elapsed >= _SpinSteps[_SpinSteps.Count - 1] + 300f)
            {
                _Spinning = false;
                _UpdateCards(_SpinTarget);
                _UpdateCenter();
                _UpdateButtons();
                _AnimTimer.Reset();
                _AnimTimer.Start();
                return;
            }

            _UpdateCards(step % count);
        }

        private void _UpdateTable()
        {
            var ranking = _PartyMode.GetRanking();
            for (var i = 0; i < _Rows.Count; i++)
            {
                var row = _Rows[i];
                var visible = i < ranking.Count;
                row.Avatar.Visible = visible;
                row.Pos.Visible = visible;
                row.Name.Visible = visible;
                row.Points.Visible = visible;
                row.Highlight.Visible = visible && i == 0 && ranking[0].TablePoints > 0;
                if (!visible)
                {
                    continue;
                }

                var player = ranking[i];
                row.Avatar.Texture = CBase.Profiles.GetAvatar(player.ProfileId);
                row.Pos.Text = (i + 1) + ".";
                row.Name.Text = _GetName(player);
                row.Points.Text = player.TablePoints.ToString();

                SColorF rankColor;
                var color = _Texts[_TextRowName].Color;
                if (_PartyMode.GameData.NumDuelsPlayed > 0 && i < _RankColors.Length && CBase.Themes.GetColor(_RankColors[i], PartyModeId, out rankColor))
                {
                    color = rankColor;
                }

                row.Pos.Color = color;
                row.Name.Color = color;
            }
        }

        private void _UpdateCenter()
        {
            var finished = _PartyMode.IsFinished;
            var duel = _PartyMode.CurrentDuel;
            var showDuel = !finished && duel != null;
            var showRule = showDuel && _PartyMode.GameData.RuleChosen && !_Spinning;

            _Statics[_StaticAvatar2].Visible = showDuel;
            _Texts[_TextName1].Visible = showDuel;
            _Texts[_TextName2].Visible = showDuel;
            _Texts[_TextVersus].Visible = showDuel;
            _Texts[_TextDuel].Visible = showDuel;
            _Texts[_TextRuleTitle].Visible = showRule;
            _Texts[_TextRuleDescription].Visible = showRule;
            _Texts[_TextRuleInfo].Visible = false;
            _Texts[_TextLastResult].Visible = !finished && _PartyMode.GameData.LastDuel != null && !showRule && !_Spinning;

            _Statics[_StaticAvatar1].MaxRect = _Avatar1Rect;
            _Statics[_StaticAvatar1].Visible = showDuel || finished;

            if (showDuel)
            {
                _Statics[_StaticAvatar1].Texture = CBase.Profiles.GetAvatar(duel.Players[0].ProfileId);
                _Statics[_StaticAvatar2].Texture = CBase.Profiles.GetAvatar(duel.Players[1].ProfileId);
                _Texts[_TextName1].Text = _GetName(duel.Players[0]);
                _Texts[_TextName2].Text = _GetName(duel.Players[1]);
                _Texts[_TextDuel].Text = _Translate("TR_SCREENMAIN_DUEL") + " " + (_PartyMode.GameData.DuelIndex + 1) + " / " + _PartyMode.GameData.Duels.Count;
            }

            if (showRule)
            {
                var key = CPartyModeRuleRoulette.GetRuleKey(duel.Rule);
                _Texts[_TextRuleTitle].Text = key;
                _Texts[_TextRuleDescription].Text = key + "_DESC";
                if (duel.Rule == ERule.TargetScore)
                {
                    _Texts[_TextRuleInfo].Visible = true;
                    _Texts[_TextRuleInfo].Text = _Translate("TR_SCREENMAIN_TARGET") + " " + duel.TargetScore.ToString("N0");
                }
                else if (duel.Rule == ERule.UnderdogChoice && duel.Chooser != null)
                {
                    _Texts[_TextRuleInfo].Visible = true;
                    _Texts[_TextRuleInfo].Text = _Translate("TR_SCREENMAIN_CHOOSER").Replace("%s", _GetName(duel.Chooser));
                }
                else if (duel.Rule == ERule.JokerRain)
                {
                    _Texts[_TextRuleInfo].Visible = true;
                    _Texts[_TextRuleInfo].Text = _Translate("TR_SCREENMAIN_JOKERS") + " " + _GetName(duel.Players[0]) + " " + duel.Players[0].Jokers + "  ·  " +
                                                 _GetName(duel.Players[1]) + " " + duel.Players[1].Jokers;
                }
            }

            var last = _PartyMode.GameData.LastDuel;
            if (last != null)
            {
                _Texts[_TextLastResult].Text = _Translate("TR_SCREENMAIN_LAST_DUEL") + " " + _GetName(last.Players[0]) + " " + last.Scores[0].ToString("N0") + " (" +
                                               _FormatAward(last.Awarded[0]) + ")  ·  " + _GetName(last.Players[1]) + " " + last.Scores[1].ToString("N0") + " (" +
                                               _FormatAward(last.Awarded[1]) + ")";
            }

            _Texts[_TextChampionLabel].Visible = finished;
            _Texts[_TextChampion].Visible = finished;
            _ParticleEffects[_ParticleEffectChampion].Visible = finished;
            if (finished)
            {
                var champion = _PartyMode.GetRanking()[0];
                _Texts[_TextChampion].Text = _GetName(champion);
                _Statics[_StaticAvatar1].Texture = CBase.Profiles.GetAvatar(champion.ProfileId);
                _Statics[_StaticAvatar1].X = _WheelCenterX - _Avatar1Rect.W / 2;
                _Statics[_StaticAvatar1].Y = _Avatar1Rect.Y + 80f;
            }
        }

        private static string _FormatAward(int award)
        {
            return award > 0 ? "+" + award : award.ToString();
        }

        private void _UpdateAnimations()
        {
            var time = _AnimTimer.ElapsedMilliseconds;
            var pop = time < _PopDuration ? 0.3f + 0.7f * _EaseOutBack(time / _PopDuration) : 1f;

            _Texts[_TextLastResult].Font.Height = _LastResultHeight * (_ShowNewResult ? pop : 1f);

            if (_PartyMode.IsFinished)
            {
                var scale = time < _PopDuration ? pop : 1f + 0.03f * (float)Math.Sin((time - _PopDuration) / 1000.0 * Math.PI * 1.4);
                _Texts[_TextChampion].Font.Height = _ChampionHeight * scale;
            }
        }
    }
}
