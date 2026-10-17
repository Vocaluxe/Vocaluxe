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

using System.Windows.Forms;

namespace VocaluxeLib.PartyModes.RuleRoulette
{
    public class CPartyScreenRuleRouletteConfig : CPartyScreenRuleRoulette
    {
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _SelectSlideNumPlayers = "SelectSlideNumPlayers";
        private const string _SelectSlideSongsPerPlayer = "SelectSlideSongsPerPlayer";
        private const string _SelectSlideNumJokers = "SelectSlideNumJokers";
        private const string _SelectSlideRuleSelection = "SelectSlideRuleSelection";
        private const string _SelectSlideNumRules = "SelectSlideNumRules";
        private const string _ButtonNext = "ButtonNext";
        private const string _ButtonBack = "ButtonBack";

        public override void Init()
        {
            base.Init();

            _ThemeSelectSlides = new string[] { _SelectSlideNumPlayers, _SelectSlideSongsPerPlayer, _SelectSlideNumJokers, _SelectSlideRuleSelection, _SelectSlideNumRules };
            _ThemeButtons = new string[] { _ButtonNext, _ButtonBack };
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
                        _PartyMode.Back();
                        break;

                    case Keys.Enter:
                        _UpdateSlides();

                        if (_Buttons[_ButtonBack].Selected)
                        {
                            _PartyMode.Back();
                        }

                        if (_Buttons[_ButtonNext].Selected)
                        {
                            _PartyMode.Next();
                        }

                        break;

                    case Keys.Left:
                    case Keys.Right:
                        _UpdateSlides();
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
                _UpdateSlides();
                if (_Buttons[_ButtonBack].Selected)
                {
                    _PartyMode.Back();
                }

                if (_Buttons[_ButtonNext].Selected)
                {
                    _PartyMode.Next();
                }
            }

            if (mouseEvent.RB)
            {
                _PartyMode.Back();
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            _SelectSlides[_SelectSlideNumPlayers].Clear();
            for (var i = _PartyMode.MinPlayers; i <= _PartyMode.MaxPlayers; i++)
            {
                _SelectSlides[_SelectSlideNumPlayers].AddValue(i.ToString());
            }

            _SelectSlides[_SelectSlideNumPlayers].SelectedValue = _PartyMode.GameData.NumPlayer.ToString();

            _SelectSlides[_SelectSlideSongsPerPlayer].Clear();
            for (var i = 1; i <= CPartyModeRuleRoulette.MaxSongsPerPlayer; i++)
            {
                _SelectSlides[_SelectSlideSongsPerPlayer].AddValue(i.ToString());
            }

            _SelectSlides[_SelectSlideSongsPerPlayer].SelectedValue = _PartyMode.GameData.SongsPerPlayer.ToString();

            _SelectSlides[_SelectSlideRuleSelection].Clear();
            _SelectSlides[_SelectSlideRuleSelection].AddValue(CBase.Language.Translate("TR_SCREENCONFIG_RULES_RANDOM", PartyModeId));
            _SelectSlides[_SelectSlideRuleSelection].AddValue(CBase.Language.Translate("TR_SCREENCONFIG_RULES_MANUAL", PartyModeId));
            _SelectSlides[_SelectSlideRuleSelection].Selection = _PartyMode.GameData.ManualRules ? 1 : 0;

            _SelectSlides[_SelectSlideNumRules].Clear();
            for (var i = CPartyModeRuleRoulette.MinRulesOnWheel; i <= CPartyModeRuleRoulette.MaxRulesOnWheel; i++)
            {
                _SelectSlides[_SelectSlideNumRules].AddValue(i.ToString());
            }

            _SelectSlides[_SelectSlideNumRules].SelectedValue = _PartyMode.GameData.NumRulesOnWheel.ToString();

            _SelectSlides[_SelectSlideNumJokers].Clear();
            for (var i = 0; i <= CPartyModeRuleRoulette.MaxJokers; i++)
            {
                _SelectSlides[_SelectSlideNumJokers].AddValue(i.ToString());
            }

            _SelectSlides[_SelectSlideNumJokers].SelectedValue = _PartyMode.GameData.NumJokers.ToString();
        }

        public override bool UpdateGame()
        {
            return true;
        }

        private void _UpdateSlides()
        {
            _PartyMode.GameData.NumPlayer = _SelectSlides[_SelectSlideNumPlayers].Selection + _PartyMode.MinPlayers;
            _PartyMode.GameData.NumJokers = _SelectSlides[_SelectSlideNumJokers].Selection;
            _PartyMode.GameData.SongsPerPlayer = _SelectSlides[_SelectSlideSongsPerPlayer].Selection + 1;
            _PartyMode.GameData.ManualRules = _SelectSlides[_SelectSlideRuleSelection].Selection == 1;
            _PartyMode.GameData.NumRulesOnWheel = _SelectSlides[_SelectSlideNumRules].Selection + CPartyModeRuleRoulette.MinRulesOnWheel;
        }
    }
}
