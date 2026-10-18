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
using System.Windows.Forms;

namespace VocaluxeLib.PartyModes.PartnerSwap
{
    public class CPartyScreenPartnerSwapConfig : CPartyScreenPartnerSwap
    {
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _SelectSlideNumPlayers = "SelectSlideNumPlayers";
        private const string _SelectSlideNumPasses = "SelectSlideNumPasses";
        private const string _SelectSlideHarmonyBonus = "SelectSlideHarmonyBonus";
        private const string _SelectSlideDuetSongs = "SelectSlideDuetSongs";
        private const string _SelectSlideNumJokers = "SelectSlideNumJokers";
        private const string _ButtonNext = "ButtonNext";
        private const string _ButtonBack = "ButtonBack";

        public override void Init()
        {
            base.Init();

            _ThemeSelectSlides = new string[] { _SelectSlideNumPlayers, _SelectSlideNumPasses, _SelectSlideHarmonyBonus, _SelectSlideDuetSongs, _SelectSlideNumJokers };
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

            _UpdatePassesSlide();

            _SelectSlides[_SelectSlideHarmonyBonus].Clear();
            _SelectSlides[_SelectSlideHarmonyBonus].AddValue(CBase.Language.Translate("TR_BUTTON_NO", PartyModeId));
            _SelectSlides[_SelectSlideHarmonyBonus].AddValue(CBase.Language.Translate("TR_BUTTON_YES", PartyModeId));
            _SelectSlides[_SelectSlideHarmonyBonus].Selection = _PartyMode.GameData.HarmonyBonus ? 1 : 0;

            _SelectSlides[_SelectSlideDuetSongs].Clear();
            _SelectSlides[_SelectSlideDuetSongs].AddValue(CBase.Language.Translate("TR_SCREENCONFIG_DUETS_MIXED", PartyModeId));
            _SelectSlides[_SelectSlideDuetSongs].AddValue(CBase.Language.Translate("TR_SCREENCONFIG_DUETS_ONLY", PartyModeId));
            _SelectSlides[_SelectSlideDuetSongs].AddValue(CBase.Language.Translate("TR_SCREENCONFIG_DUETS_NONE", PartyModeId));
            _SelectSlides[_SelectSlideDuetSongs].Selection = (int)_PartyMode.GameData.DuetSongs;

            _SelectSlides[_SelectSlideNumJokers].Clear();
            for (var i = 0; i <= CPartyModePartnerSwap.MaxJokers; i++)
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
            _PartyMode.GameData.NumPasses = _SelectSlides[_SelectSlideNumPasses].Selection + 1;
            _PartyMode.GameData.HarmonyBonus = _SelectSlides[_SelectSlideHarmonyBonus].Selection == 1;
            _PartyMode.GameData.DuetSongs = (EDuetSongs)_SelectSlides[_SelectSlideDuetSongs].Selection;
            _PartyMode.GameData.NumJokers = _SelectSlides[_SelectSlideNumJokers].Selection;
            _UpdatePassesSlide();
        }

        private void _UpdatePassesSlide()
        {
            var maxPasses = CPartyModePartnerSwap.GetMaxPasses(_PartyMode.GameData.NumPlayer);
            _PartyMode.GameData.NumPasses = Math.Max(1, Math.Min(maxPasses, _PartyMode.GameData.NumPasses));

            _SelectSlides[_SelectSlideNumPasses].Clear();
            for (var i = 1; i <= maxPasses; i++)
            {
                _SelectSlides[_SelectSlideNumPasses].AddValue(i.ToString());
            }

            _SelectSlides[_SelectSlideNumPasses].Selection = _PartyMode.GameData.NumPasses - 1;
        }
    }
}
