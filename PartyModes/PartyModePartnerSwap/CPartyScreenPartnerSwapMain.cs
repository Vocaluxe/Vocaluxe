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

namespace VocaluxeLib.PartyModes.PartnerSwap
{
    class CRankRow
    {
        public CSwapPlayer Player;
        public CStatic Highlight;
        public CStatic Avatar;
        public CText Pos;
        public CText Name;
        public CText Songs;
        public CText Average;
        public int OldRank;
        public int NewRank;
    }

    public class CPartyScreenPartnerSwapMain : CPartyScreenPartnerSwap
    {
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _TextPopupReallyExit = "TextPopupReallyExit";
        private const string _TextPass = "TextPass";
        private const string _TextRowPos = "TextRowPos";
        private const string _TextRowName = "TextRowName";
        private const string _TextRowSongs = "TextRowSongs";
        private const string _TextRowAverage = "TextRowAverage";
        private const string _TextPairName1 = "TextPairName1";
        private const string _TextPairName2 = "TextPairName2";
        private const string _TextAmp = "TextAmp";
        private const string _TextTogether = "TextTogether";
        private const string _TextPause = "TextPause";
        private const string _TextLastResult = "TextLastResult";
        private const string _TextHarmony = "TextHarmony";
        private const string _TextBestLabel = "TextBestLabel";
        private const string _TextBestNames = "TextBestNames";
        private const string _TextBestScore = "TextBestScore";
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
        private const string _StaticPairAvatar1 = "StaticPairAvatar1";
        private const string _StaticPairAvatar2 = "StaticPairAvatar2";
        private const string _StaticBestAvatar1 = "StaticBestAvatar1";
        private const string _StaticBestAvatar2 = "StaticBestAvatar2";

        private const string _ParticleEffectChampion = "ParticleEffectChampion";

        private const float _FirstRowY = 260f;
        private const float _RowStep = 54f;
        private const float _RankDelay = 300f;
        private const float _RankDuration = 700f;
        private const float _PopDuration = 700f;

        private readonly string[] _RankColors = new string[] { "Top1Color", "Top2Color", "Top3Color" };

        private bool _ExitPopupVisible;
        private List<CRankRow> _Rows;
        private readonly Stopwatch _AnimTimer = new Stopwatch();
        private bool _ShowNewResult;
        private int _LastSeenSongs = -1;
        private float _LastResultHeight;
        private float _HarmonyHeight;
        private float _ChampionHeight;
        private SRectF _PairAvatar1Rect;

        public override void Init()
        {
            base.Init();

            _ThemeTexts = new string[]
            {
                _TextPopupReallyExit, _TextPass, _TextRowPos, _TextRowName, _TextRowSongs, _TextRowAverage, _TextPairName1, _TextPairName2, _TextAmp, _TextTogether,
                _TextPause, _TextLastResult, _TextHarmony, _TextBestLabel, _TextBestNames, _TextBestScore, _TextChampionLabel, _TextChampion
            };
            _ThemeButtons = new string[] { _ButtonNextRound, _ButtonBack, _ButtonExit, _ButtonPopupYes, _ButtonPopupNo };
            _ThemeStatics = new string[]
            {
                _StaticPopupBG, _StaticRowHighlight, _StaticRowAvatar, _StaticPairAvatar1, _StaticPairAvatar2, _StaticBestAvatar1,
                _StaticBestAvatar2
            };
            _ThemeParticleEffects = new string[] { _ParticleEffectChampion };
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _Rows = new List<CRankRow>();
            for (var i = 0; i < _PartyMode.MaxPlayers; i++)
            {
                var row = new CRankRow
                {
                    Highlight = GetNewStatic(_Statics[_StaticRowHighlight]),
                    Avatar = GetNewStatic(_Statics[_StaticRowAvatar]),
                    Pos = GetNewText(_Texts[_TextRowPos]),
                    Name = GetNewText(_Texts[_TextRowName]),
                    Songs = GetNewText(_Texts[_TextRowSongs]),
                    Average = GetNewText(_Texts[_TextRowAverage])
                };
                row.Avatar.Aspect = EAspect.Crop;
                _AddStatic(row.Highlight);
                _AddStatic(row.Avatar);
                _AddText(row.Pos);
                _AddText(row.Name);
                _AddText(row.Songs);
                _AddText(row.Average);
                _Rows.Add(row);
            }

            _Statics[_StaticRowHighlight].Visible = false;
            _Statics[_StaticRowAvatar].Visible = false;
            _Texts[_TextRowPos].Visible = false;
            _Texts[_TextRowName].Visible = false;
            _Texts[_TextRowSongs].Visible = false;
            _Texts[_TextRowAverage].Visible = false;

            _Statics[_StaticPairAvatar1].Aspect = EAspect.Crop;
            _Statics[_StaticPairAvatar2].Aspect = EAspect.Crop;
            _Statics[_StaticBestAvatar1].Aspect = EAspect.Crop;
            _Statics[_StaticBestAvatar2].Aspect = EAspect.Crop;
            _PairAvatar1Rect = _Statics[_StaticPairAvatar1].Rect;

            _LastResultHeight = _Texts[_TextLastResult].Font.Height;
            _HarmonyHeight = _Texts[_TextHarmony].Font.Height;
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

            _ShowNewResult = _PartyMode.GameData.LastPair != null && _PartyMode.GameData.NumSongsSung != _LastSeenSongs;
            _LastSeenSongs = _PartyMode.GameData.NumSongsSung;

            _UpdateRanking();
            _UpdatePair();
            _UpdateBestPair();
            _UpdateLastResult();

            _AnimTimer.Reset();
            _AnimTimer.Start();
            _UpdateAnimations();

            var started = _PartyMode.GameData.NumSongsSung > 0;
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
            _UpdateAnimations();
            return true;
        }

        private void _OnBack()
        {
            if (_ExitPopupVisible)
            {
                _ShowPopup(false);
            }
            else if (_PartyMode.GameData.NumSongsSung == 0)
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

        private string _GetName(CSwapPlayer player)
        {
            return CBase.Profiles.GetPlayerName(player.ProfileId);
        }

        private string _GetPairNames(CSwapPair pair)
        {
            return _GetName(pair.Players[0]) + " & " + _GetName(pair.Players[1]);
        }

        private void _UpdateRanking()
        {
            var ranking = _PartyMode.GetRanking();
            var previous = _ShowNewResult ? _PartyMode.GameData.PreviousRanking : ranking;

            for (var i = 0; i < _Rows.Count; i++)
            {
                var row = _Rows[i];
                var visible = i < ranking.Count;
                row.Player = visible ? _PartyMode.GameData.Players[i] : null;
                row.Avatar.Visible = visible;
                row.Pos.Visible = visible;
                row.Name.Visible = visible;
                row.Songs.Visible = visible;
                row.Average.Visible = visible;
                row.Highlight.Visible = false;
                if (!visible)
                {
                    continue;
                }

                row.NewRank = ranking.IndexOf(row.Player);
                row.OldRank = previous.IndexOf(row.Player);
                if (row.OldRank < 0)
                {
                    row.OldRank = row.NewRank;
                }

                row.Avatar.Texture = CBase.Profiles.GetAvatar(row.Player.ProfileId);
                row.Name.Text = _GetName(row.Player);
                row.Songs.Text = row.Player.NumSongs.ToString();
                row.Average.Text = row.Player.Average.ToString("N0");
                row.Pos.Text = (row.NewRank + 1) + ".";
                row.Highlight.Visible = row.NewRank == 0 && row.Player.NumSongs > 0;

                SColorF rankColor;
                var color = _Texts[_TextRowName].Color;
                if (row.Player.NumSongs > 0 && row.NewRank < _RankColors.Length && CBase.Themes.GetColor(_RankColors[row.NewRank], PartyModeId, out rankColor))
                {
                    color = rankColor;
                }

                row.Pos.Color = color;
                row.Name.Color = color;
            }
        }

        private void _PlaceRow(CRankRow row, float y)
        {
            row.Highlight.Y = y + (_Texts[_TextRowName].H - row.Highlight.H) / 2;
            row.Avatar.Y = y + (_Texts[_TextRowName].H - row.Avatar.H) / 2;
            row.Pos.Y = y;
            row.Name.Y = y;
            row.Songs.Y = y;
            row.Average.Y = y;
        }

        private void _UpdatePair()
        {
            var finished = _PartyMode.IsFinished;
            var pair = _PartyMode.CurrentPair;
            var showPair = !finished && pair != null;

            _Statics[_StaticPairAvatar2].Visible = showPair;
            _Texts[_TextPairName1].Visible = showPair;
            _Texts[_TextPairName2].Visible = showPair;
            _Texts[_TextAmp].Visible = showPair;
            _Texts[_TextTogether].Visible = showPair;
            _Texts[_TextPass].Visible = showPair;

            var pause = _PartyMode.CurrentBreak;
            _Texts[_TextPause].Visible = showPair && pause != null;
            if (pause != null)
            {
                _Texts[_TextPause].Text = CBase.Language.Translate("TR_SCREENMAIN_PAUSE", PartyModeId) + " " + _GetName(pause);
            }

            _Statics[_StaticPairAvatar1].MaxRect = _PairAvatar1Rect;
            _Statics[_StaticPairAvatar1].Visible = showPair || finished;

            if (showPair)
            {
                _Statics[_StaticPairAvatar1].Texture = CBase.Profiles.GetAvatar(pair.Players[0].ProfileId);
                _Statics[_StaticPairAvatar2].Texture = CBase.Profiles.GetAvatar(pair.Players[1].ProfileId);
                _Texts[_TextPairName1].Text = _GetName(pair.Players[0]);
                _Texts[_TextPairName2].Text = _GetName(pair.Players[1]);

                var together = _PartyMode.GetTimesTogether(pair.Players[0], pair.Players[1]);
                _Texts[_TextTogether].Text = together == 0
                                                 ? CBase.Language.Translate("TR_SCREENMAIN_FIRST_TIME", PartyModeId)
                                                 : CBase.Language.Translate("TR_SCREENMAIN_TIMES_TOGETHER", PartyModeId).Replace("%n", (together + 1).ToString());

                _Texts[_TextPass].Text = CBase.Language.Translate("TR_SCREENMAIN_PASS", PartyModeId) + " " + (_PartyMode.GameData.PassIndex + 1) + " / " +
                                         _PartyMode.GameData.Passes.Count + "   ·   " + CBase.Language.Translate("TR_SCREENMAIN_SONG", PartyModeId) + " " +
                                         (_PartyMode.GameData.PairIndex + 1) + " / " + _PartyMode.GameData.Passes[_PartyMode.GameData.PassIndex].Count;
            }

            _Texts[_TextChampionLabel].Visible = finished;
            _Texts[_TextChampion].Visible = finished;
            _ParticleEffects[_ParticleEffectChampion].Visible = finished;

            if (finished)
            {
                var champion = _PartyMode.GetRanking()[0];
                _Texts[_TextChampion].Text = _GetName(champion);
                _Statics[_StaticPairAvatar1].Texture = CBase.Profiles.GetAvatar(champion.ProfileId);
                var centerX = (_PairAvatar1Rect.X + _Statics[_StaticPairAvatar2].Rect.X + _Statics[_StaticPairAvatar2].Rect.W) / 2;
                _Statics[_StaticPairAvatar1].X = centerX - _PairAvatar1Rect.W / 2;
                _Statics[_StaticPairAvatar1].Y = _PairAvatar1Rect.Y + 60f;
            }
        }

        private void _UpdateBestPair()
        {
            var best = _PartyMode.GameData.BestPair;
            var visible = best != null;
            _Statics[_StaticBestAvatar1].Visible = visible;
            _Statics[_StaticBestAvatar2].Visible = visible;
            _Texts[_TextBestNames].Visible = visible;
            _Texts[_TextBestScore].Visible = visible;
            _Texts[_TextBestLabel].Text = _PartyMode.IsFinished ? "TR_SCREENMAIN_BEST_PAIR_FINAL" : "TR_SCREENMAIN_BEST_PAIR";

            if (!visible)
            {
                return;
            }

            _Statics[_StaticBestAvatar1].Texture = CBase.Profiles.GetAvatar(best.Players[0].ProfileId);
            _Statics[_StaticBestAvatar2].Texture = CBase.Profiles.GetAvatar(best.Players[1].ProfileId);
            _Texts[_TextBestNames].Text = _GetPairNames(best);
            _Texts[_TextBestScore].Text = best.PairScore.ToString("N0");
        }

        private void _UpdateLastResult()
        {
            var last = _PartyMode.GameData.LastPair;
            _Texts[_TextLastResult].Visible = last != null && !_PartyMode.IsFinished;
            _Texts[_TextHarmony].Visible = last != null && last.Bonus > 0 && !_PartyMode.IsFinished;

            if (last == null)
            {
                return;
            }

            _Texts[_TextLastResult].Text = CBase.Language.Translate("TR_SCREENMAIN_LAST_SONG", PartyModeId) + " " + _GetPairNames(last) + "   ·   " +
                                           last.PairScore.ToString("N0");
            _Texts[_TextHarmony].Text = CBase.Language.Translate("TR_SCREENMAIN_HARMONY_BONUS", PartyModeId) + " +" + last.Bonus;
        }

        private void _UpdateAnimations()
        {
            var time = _AnimTimer.ElapsedMilliseconds;

            var rankProgress = _ShowNewResult ? _EaseInOut(Math.Max(0f, Math.Min(1f, (time - _RankDelay) / _RankDuration))) : 1f;
            foreach (var row in _Rows)
            {
                if (row.Player == null)
                {
                    continue;
                }

                var oldY = _FirstRowY + row.OldRank * _RowStep;
                var newY = _FirstRowY + row.NewRank * _RowStep;
                _PlaceRow(row, oldY + (newY - oldY) * rankProgress);
            }

            var popScale = _ShowNewResult && time < _PopDuration ? 0.3f + 0.7f * _EaseOutBack(time / _PopDuration) : 1f;
            _Texts[_TextLastResult].Font.Height = _LastResultHeight * popScale;
            _Texts[_TextHarmony].Font.Height = _HarmonyHeight * popScale;

            if (_PartyMode.IsFinished)
            {
                float scale;
                if (time < _PopDuration)
                {
                    scale = 0.3f + 0.7f * _EaseOutBack(time / _PopDuration);
                }
                else
                {
                    scale = 1f + 0.03f * (float)Math.Sin((time - _PopDuration) / 1000.0 * Math.PI * 1.4);
                }

                _Texts[_TextChampion].Font.Height = _ChampionHeight * scale;
            }
        }
    }
}
