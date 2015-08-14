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

namespace VocaluxeLib.PartyModes.Challenge
{
    // ReSharper disable UnusedMember.Global
    public class CPartyScreenChallengeConfig : CPartyScreenChallenge
        // ReSharper restore UnusedMember.Global
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 3; }
        }

        private const string _SelectSlideNumPlayers = "SelectSlideNumPlayers";
        private const string _SelectSlideNumMics = "SelectSlideNumMics";
        private const string _SelectSlideNumRounds = "SelectSlideNumRounds";
        private const string _SelectSlideNumJokers = "SelectSlideNumJokers";
        private const string _SelectSlideRefillJokers = "SelectSlideRefillJokers";
        private const string _ButtonNext = "ButtonNext";
        private const string _ButtonBack = "ButtonBack";

        private const int _MaxNumRounds = 100;
        private int _RoundSteps = 1;

        public override void Init()
        {
            base.Init();

            _ThemeSelectSlides = new string[] { _SelectSlideNumPlayers, _SelectSlideNumMics, _SelectSlideNumRounds, _SelectSlideNumJokers, _SelectSlideRefillJokers };
            _ThemeButtons = new string[] {_ButtonNext, _ButtonBack};
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
                        _PartyMode.Back();
                        break;

                    case Keys.Enter:
                        _UpdateSlides();

                        if (_Buttons[_ButtonBack].Selected)
                            _PartyMode.Back();

                        if (_Buttons[_ButtonNext].Selected)
                            _PartyMode.Next();
                        break;

                    case Keys.Left:
                        _UpdateSlides();
                        break;

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
                    _PartyMode.Back();

                if (_Buttons[_ButtonNext].Selected)
                    _PartyMode.Next();
            }

            if (mouseEvent.RB)
                _PartyMode.Back();

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            _RebuildSlides();
        }

        public override bool UpdateGame()
        {
            return true;
        }

        private void _RebuildSlides()
        {
            // build num player slide (min player ... max player);
            _SelectSlides[_SelectSlideNumPlayers].Clear();
            for (int i = _PartyMode.MinPlayers; i <= _PartyMode.MaxPlayers; i++)
                _SelectSlides[_SelectSlideNumPlayers].AddValue(i.ToString());
            _SelectSlides[_SelectSlideNumPlayers].SelectedValue = _PartyMode.GameData.NumPlayer.ToString();

            // build num joker slide 1 to 10
            _SelectSlides[_SelectSlideNumJokers].Clear();
            for (int i = 1; i <= 10; i++)
            {
                _SelectSlides[_SelectSlideNumJokers].AddValue(i.ToString());
            }
            _SelectSlides[_SelectSlideNumJokers].SelectedValue = "5";

            //build joker config slide
            _SelectSlides[_SelectSlideRefillJokers].Clear();
            _SelectSlides[_SelectSlideRefillJokers].AddValue(CBase.Language.Translate("TR_BUTTON_NO", PartyModeID));
            _SelectSlides[_SelectSlideRefillJokers].AddValue(CBase.Language.Translate("TR_BUTTON_YES", PartyModeID));
            _SelectSlides[_SelectSlideRefillJokers].SelectLastValue();

            _UpdateMicsAtOnce();
            _SetRoundSteps();
            _UpdateSlideRounds();
        }

        private void _UpdateSlides()
        {
            int player = _PartyMode.GameData.NumPlayer;
            int mics = _PartyMode.GameData.NumPlayerAtOnce;
            _PartyMode.GameData.NumPlayer = _SelectSlides[_SelectSlideNumPlayers].Selection + _PartyMode.MinPlayers;
            _PartyMode.GameData.NumPlayerAtOnce = _SelectSlides[_SelectSlideNumMics].Selection + _PartyMode.MinPlayers;
            _PartyMode.GameData.NumRounds = (_SelectSlides[_SelectSlideNumRounds].Selection + 1) * _RoundSteps;
            _PartyMode.GameData.NumJokers = _SelectSlides[_SelectSlideNumJokers].Selection + 1;
            _PartyMode.GameData.RefillJokers = (_SelectSlides[_SelectSlideRefillJokers].Selection == 1) ? true : false;

            _UpdateMicsAtOnce();
            _SetRoundSteps();

            if (player != _PartyMode.GameData.NumPlayer || mics != _PartyMode.GameData.NumPlayerAtOnce)
            {
                int num = CHelper.CombinationCount(_PartyMode.GameData.NumPlayer, _PartyMode.GameData.NumPlayerAtOnce);
                while (num > _MaxNumRounds)
                    num -= _RoundSteps;
                _PartyMode.GameData.NumRounds = num;
            }

            _UpdateSlideRounds();
        }

        private void _UpdateMicsAtOnce()
        {
            int maxNum = Math.Min(_PartyMode.MaxMics, _PartyMode.GameData.NumPlayer);

            if (_PartyMode.GameData.NumPlayerAtOnce > maxNum)
                _PartyMode.GameData.NumPlayerAtOnce = maxNum;

            // build mics at once slide
            _SelectSlides[_SelectSlideNumMics].Clear();
            for (int i = 1; i <= maxNum; i++)
                _SelectSlides[_SelectSlideNumMics].AddValue(i.ToString());
            _SelectSlides[_SelectSlideNumMics].Selection = _PartyMode.GameData.NumPlayerAtOnce - _PartyMode.MinPlayers;
        }

        private void _UpdateSlideRounds()
        {
            // build num rounds slide
            _SelectSlides[_SelectSlideNumRounds].Clear();
            for (int i = _RoundSteps; i <= _MaxNumRounds; i += _RoundSteps)
                _SelectSlides[_SelectSlideNumRounds].AddValue(i.ToString());
            _SelectSlides[_SelectSlideNumRounds].Selection = _PartyMode.GameData.NumRounds / _RoundSteps - 1;
        }

        private void _SetRoundSteps()
        {
            if (_PartyMode.GameData.NumPlayerAtOnce < 1 || _PartyMode.GameData.NumPlayer < 1 || _PartyMode.GameData.NumPlayerAtOnce > _PartyMode.GameData.NumPlayer)
            {
                _RoundSteps = 1;
                return;
            }

            int res = _PartyMode.GameData.NumPlayer / _PartyMode.GameData.NumPlayerAtOnce;
            int mod = _PartyMode.GameData.NumPlayer % _PartyMode.GameData.NumPlayerAtOnce;

            _RoundSteps = mod == 0 ? res : _PartyMode.GameData.NumPlayer;
        }
    }
}