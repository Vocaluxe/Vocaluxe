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
using VocaluxeLib.Menu;

namespace VocaluxeLib.PartyModes.TeamSurvival
{
    class CPlayerCard
    {
        public CSurvivalPlayer Player;
        public CStatic Active;
        public CStatic Box;
        public CStatic Avatar;
        public CText Name;
        public CStatic[] Hearts;
        public SRectF[] HeartRects;
    }

    public class CPartyScreenTeamSurvivalMain : CPartyScreenTeamSurvival
    {
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _TextPopupReallyExit = "TextPopupReallyExit";
        private const string _TextTeam1 = "TextTeam1";
        private const string _TextTeam2 = "TextTeam2";
        private const string _TextPass = "TextPass";
        private const string _TextCardName = "TextCardName";
        private const string _TextDuelName1 = "TextDuelName1";
        private const string _TextDuelName2 = "TextDuelName2";
        private const string _TextVersus = "TextVersus";
        private const string _TextRematch = "TextRematch";
        private const string _TextChampionLabel = "TextChampionLabel";
        private const string _TextChampion = "TextChampion";

        private const string _ButtonNextRound = "ButtonNextRound";
        private const string _ButtonBack = "ButtonBack";
        private const string _ButtonExit = "ButtonExit";
        private const string _ButtonPopupYes = "ButtonPopupYes";
        private const string _ButtonPopupNo = "ButtonPopupNo";

        private const string _StaticPopupBG = "StaticPopupBG";
        private const string _StaticCardBox = "StaticCardBox";
        private const string _StaticCardActive = "StaticCardActive";
        private const string _StaticCardAvatar = "StaticCardAvatar";
        private const string _StaticHeart = "StaticHeart";
        private const string _StaticDuelFrame1 = "StaticDuelFrame1";
        private const string _StaticDuelFrame2 = "StaticDuelFrame2";
        private const string _StaticDuelAvatar1 = "StaticDuelAvatar1";
        private const string _StaticDuelAvatar2 = "StaticDuelAvatar2";

        private const string _ParticleEffectChampion = "ParticleEffectChampion";

        private const float _CardFirstY = 265f;
        private const float _CardStep = 76f;
        private const float _ActiveBorder = 4f;
        private const float _HeartSize = 32f;
        private const float _HeartSpace = 6f;
        private const float _LostHeartAlpha = 0.18f;
        private const float _OutAlpha = 0.35f;
        private const float _SurvivorSize = 110f;
        private const float _SurvivorY = 470f;
        private const float _SurvivorSpace = 16f;

        private const float _HeartLossDelay = 400f;
        private const float _HeartLossDuration = 800f;
        private const float _ChampionPopDuration = 700f;

        private readonly float[] _CardX = new float[] { 60f, 1340f };

        private bool _ExitPopupVisible;
        private List<CPlayerCard> _Cards;
        private List<CStatic> _Survivors;
        private readonly Stopwatch _AnimTimer = new Stopwatch();
        private CPlayerCard _LosingCard;
        private int _LostHeart;
        private bool _LosingCardOut;
        private float _ChampionHeight;

        public override void Init()
        {
            base.Init();

            _ThemeTexts = new string[]
            {
                _TextPopupReallyExit, _TextTeam1, _TextTeam2, _TextPass, _TextCardName, _TextDuelName1, _TextDuelName2, _TextVersus, _TextRematch, _TextChampionLabel,
                _TextChampion
            };
            _ThemeButtons = new string[] { _ButtonNextRound, _ButtonBack, _ButtonExit, _ButtonPopupYes, _ButtonPopupNo };
            _ThemeStatics = new string[]
            {
                _StaticPopupBG, _StaticCardBox, _StaticCardActive, _StaticCardAvatar, _StaticHeart, _StaticDuelFrame1, _StaticDuelFrame2, _StaticDuelAvatar1,
                _StaticDuelAvatar2
            };
            _ThemeParticleEffects = new string[] { _ParticleEffectChampion };
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _Cards = new List<CPlayerCard>();
            for (var i = 0; i < CPartyModeTeamSurvival.TeamCount * CPartyModeTeamSurvival.MaxPlayersInTeam; i++)
            {
                var card = new CPlayerCard
                {
                    Active = GetNewStatic(_Statics[_StaticCardActive]),
                    Box = GetNewStatic(_Statics[_StaticCardBox]),
                    Avatar = GetNewStatic(_Statics[_StaticCardAvatar]),
                    Name = GetNewText(_Texts[_TextCardName]),
                    Hearts = new CStatic[CPartyModeTeamSurvival.MaxHearts],
                    HeartRects = new SRectF[CPartyModeTeamSurvival.MaxHearts]
                };
                card.Avatar.Aspect = EAspect.Crop;
                _AddStatic(card.Active);
                _AddStatic(card.Box);
                _AddStatic(card.Avatar);
                _AddText(card.Name);
                for (var h = 0; h < CPartyModeTeamSurvival.MaxHearts; h++)
                {
                    card.Hearts[h] = GetNewStatic(_Statics[_StaticHeart]);
                    _AddStatic(card.Hearts[h]);
                }

                _Cards.Add(card);
            }

            _Survivors = new List<CStatic>();
            for (var i = 0; i < CPartyModeTeamSurvival.MaxPlayersInTeam; i++)
            {
                var survivor = GetNewStatic(_Statics[_StaticCardAvatar]);
                survivor.Aspect = EAspect.Crop;
                _AddStatic(survivor);
                _Survivors.Add(survivor);
            }

            _Statics[_StaticCardBox].Visible = false;
            _Statics[_StaticCardActive].Visible = false;
            _Statics[_StaticCardAvatar].Visible = false;
            _Statics[_StaticHeart].Visible = false;
            _Texts[_TextCardName].Visible = false;
            _Statics[_StaticDuelAvatar1].Aspect = EAspect.Crop;
            _Statics[_StaticDuelAvatar2].Aspect = EAspect.Crop;

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

            _BuildCards();
            _UpdateCards();
            _UpdateDuel();

            _LosingCard = null;
            var decided = _PartyMode.GameData.LastDecidedDuel;
            _PartyMode.GameData.LastDecidedDuel = null;
            if (decided != null)
            {
                _LosingCard = _FindCard(decided.Players[decided.Loser]);
                _LostHeart = decided.Players[decided.Loser].Hearts;
                _LosingCardOut = decided.Players[decided.Loser].IsOut;
            }

            _AnimTimer.Reset();
            _AnimTimer.Start();
            _UpdateHeartLoss();

            var started = _PartyMode.GameData.NumDuelsPlayed > 0;
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
            _UpdateHeartLoss();

            if (_PartyMode.IsFinished)
            {
                _UpdateChampion();
            }

            return true;
        }

        private void _OnBack()
        {
            if (_ExitPopupVisible)
            {
                _ShowPopup(false);
            }
            else if (_PartyMode.GameData.NumDuelsPlayed == 0)
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

        private static float _EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * (float)Math.Pow(t - 1f, 3) + c1 * (float)Math.Pow(t - 1f, 2);
        }

        private static SColorF _WithAlpha(SColorF color, float alpha)
        {
            return new SColorF(color.R, color.G, color.B, color.A * alpha);
        }

        private string _GetTeamName(int team)
        {
            return CBase.Language.Translate("TR_TEAM", PartyModeId) + " " + (team + 1);
        }

        private CPlayerCard _FindCard(CSurvivalPlayer player)
        {
            foreach (var card in _Cards)
            {
                if (card.Player == player)
                {
                    return card;
                }
            }

            return null;
        }

        private void _BuildCards()
        {
            var cardNr = 0;
            for (var team = 0; team < CPartyModeTeamSurvival.TeamCount; team++)
            {
                var players = _PartyMode.GetTeam(team);
                for (var i = 0; i < players.Count && cardNr < _Cards.Count; i++)
                {
                    var card = _Cards[cardNr++];
                    card.Player = players[i];

                    var boxTemplate = _Statics[_StaticCardBox].Rect;
                    var box = new SRectF(_CardX[team], _CardFirstY + i * _CardStep, boxTemplate.W, boxTemplate.H, boxTemplate.Z);
                    card.Box.MaxRect = box;
                    card.Active.MaxRect = new SRectF(box.X - _ActiveBorder, box.Y - _ActiveBorder, box.W + 2 * _ActiveBorder, box.H + 2 * _ActiveBorder,
                        _Statics[_StaticCardActive].Rect.Z);

                    var avatarTemplate = _Statics[_StaticCardAvatar].Rect;
                    card.Avatar.MaxRect = new SRectF(box.X + 6f, box.Y + (box.H - avatarTemplate.H) / 2, avatarTemplate.W, avatarTemplate.H, avatarTemplate.Z);
                    card.Avatar.Texture = CBase.Profiles.GetAvatar(card.Player.ProfileId);

                    card.Name.Text = CBase.Profiles.GetPlayerName(card.Player.ProfileId);
                    card.Name.X = box.X + avatarTemplate.W + 18f;
                    card.Name.Y = box.Y + (box.H - card.Name.H) / 2;
                    card.Name.W = box.W - avatarTemplate.W - 30f - CPartyModeTeamSurvival.MaxHearts * (_HeartSize + _HeartSpace);

                    var heartsX = box.X + box.W - 10f - card.Player.StartHearts * (_HeartSize + _HeartSpace);
                    for (var h = 0; h < card.Hearts.Length; h++)
                    {
                        card.HeartRects[h] = new SRectF(heartsX + h * (_HeartSize + _HeartSpace), box.Y + (box.H - _HeartSize) / 2, _HeartSize, _HeartSize,
                            _Statics[_StaticHeart].Rect.Z);
                        card.Hearts[h].MaxRect = card.HeartRects[h];
                    }
                }
            }

            for (var i = cardNr; i < _Cards.Count; i++)
            {
                _Cards[i].Player = null;
            }
        }

        private void _UpdateCards()
        {
            var duel = _PartyMode.CurrentDuel;
            foreach (var card in _Cards)
            {
                var visible = card.Player != null;
                card.Box.Visible = visible;
                card.Avatar.Visible = visible;
                card.Name.Visible = visible;
                card.Active.Visible = visible && duel != null && (duel.Players[0] == card.Player || duel.Players[1] == card.Player);
                for (var h = 0; h < card.Hearts.Length; h++)
                {
                    card.Hearts[h].Visible = visible && h < card.Player.StartHearts;
                }

                if (visible)
                {
                    _SetCardAlpha(card, card.Player.IsOut ? _OutAlpha : 1f);
                    _SetHeartAlphas(card, card.Player.Hearts);
                }
            }

            for (var team = 0; team < CPartyModeTeamSurvival.TeamCount; team++)
            {
                var text = _Texts[team == 0 ? _TextTeam1 : _TextTeam2];
                text.Text = _GetTeamName(team);
                text.Color = CBase.Themes.GetPlayerColor(team + 1);
            }
        }

        private void _SetCardAlpha(CPlayerCard card, float alpha)
        {
            card.Box.Color = _WithAlpha(_Statics[_StaticCardBox].Color, alpha);
            card.Avatar.Color = _WithAlpha(_Statics[_StaticCardAvatar].Color, alpha);
            card.Name.Color = _WithAlpha(_Texts[_TextCardName].Color, alpha);
        }

        private void _SetHeartAlphas(CPlayerCard card, int hearts)
        {
            for (var h = 0; h < card.Hearts.Length; h++)
            {
                card.Hearts[h].MaxRect = card.HeartRects[h];
                card.Hearts[h].Color = _WithAlpha(_Statics[_StaticHeart].Color, h < hearts ? 1f : _LostHeartAlpha);
            }
        }

        private void _UpdateHeartLoss()
        {
            if (_LosingCard == null || _LostHeart < 0 || _LostHeart >= _LosingCard.Hearts.Length)
            {
                return;
            }

            var progress = Math.Max(0f, Math.Min(1f, (_AnimTimer.ElapsedMilliseconds - _HeartLossDelay) / _HeartLossDuration));
            var heart = _LosingCard.Hearts[_LostHeart];
            var rect = _LosingCard.HeartRects[_LostHeart];
            var scale = 1f + progress;
            var size = rect.W * scale;
            heart.MaxRect = new SRectF(rect.X + (rect.W - size) / 2, rect.Y + (rect.H - size) / 2, size, size, rect.Z);
            heart.Color = _WithAlpha(_Statics[_StaticHeart].Color, 1f - (1f - _LostHeartAlpha) * progress);

            if (_LosingCardOut)
            {
                _SetCardAlpha(_LosingCard, 1f - (1f - _OutAlpha) * progress);
            }

            if (progress >= 1f)
            {
                heart.MaxRect = rect;
                _LosingCard = null;
            }
        }

        private void _UpdateDuel()
        {
            var finished = _PartyMode.IsFinished;
            var duel = _PartyMode.CurrentDuel;
            var showDuel = !finished && duel != null;

            _Statics[_StaticDuelFrame1].Visible = showDuel;
            _Statics[_StaticDuelFrame2].Visible = showDuel;
            _Statics[_StaticDuelAvatar1].Visible = showDuel;
            _Statics[_StaticDuelAvatar2].Visible = showDuel;
            _Texts[_TextDuelName1].Visible = showDuel;
            _Texts[_TextDuelName2].Visible = showDuel;
            _Texts[_TextVersus].Visible = showDuel;
            _Texts[_TextPass].Visible = showDuel;
            _Texts[_TextRematch].Visible = showDuel && _PartyMode.GameData.IsRematch;

            if (showDuel)
            {
                _Statics[_StaticDuelFrame1].Color = CBase.Themes.GetPlayerColor(1);
                _Statics[_StaticDuelFrame2].Color = CBase.Themes.GetPlayerColor(2);
                _Statics[_StaticDuelAvatar1].Texture = CBase.Profiles.GetAvatar(duel.Players[0].ProfileId);
                _Statics[_StaticDuelAvatar2].Texture = CBase.Profiles.GetAvatar(duel.Players[1].ProfileId);
                _Texts[_TextDuelName1].Text = CBase.Profiles.GetPlayerName(duel.Players[0].ProfileId);
                _Texts[_TextDuelName2].Text = CBase.Profiles.GetPlayerName(duel.Players[1].ProfileId);
                _Texts[_TextPass].Text = CBase.Language.Translate("TR_SCREENMAIN_PASS", PartyModeId) + " " + _PartyMode.GameData.PassNr + "   ·   " +
                                         CBase.Language.Translate("TR_SCREENMAIN_DUEL", PartyModeId) + " " + (_PartyMode.GameData.DuelIndex + 1) + " / " +
                                         _PartyMode.GameData.PassDuels.Count;
            }

            _Texts[_TextChampionLabel].Visible = finished;
            _Texts[_TextChampion].Visible = finished;
            _ParticleEffects[_ParticleEffectChampion].Visible = finished;

            var survivors = new List<CSurvivalPlayer>();
            if (finished)
            {
                _Texts[_TextChampion].Text = _GetTeamName(_PartyMode.GameData.WinnerTeam);
                foreach (var player in _PartyMode.GetTeam(_PartyMode.GameData.WinnerTeam))
                {
                    if (!player.IsOut)
                    {
                        survivors.Add(player);
                    }
                }
            }

            var x = (CBase.Settings.GetRenderW() - survivors.Count * _SurvivorSize - (survivors.Count - 1) * _SurvivorSpace) / 2;
            for (var i = 0; i < _Survivors.Count; i++)
            {
                _Survivors[i].Visible = i < survivors.Count;
                if (i >= survivors.Count)
                {
                    continue;
                }

                _Survivors[i].Texture = CBase.Profiles.GetAvatar(survivors[i].ProfileId);
                _Survivors[i].MaxRect = new SRectF(x, _SurvivorY, _SurvivorSize, _SurvivorSize, _Statics[_StaticCardAvatar].Rect.Z);
                x += _SurvivorSize + _SurvivorSpace;
            }
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
    }
}
