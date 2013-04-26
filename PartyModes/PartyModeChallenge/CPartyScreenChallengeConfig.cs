#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using System;
using System.Windows.Forms;
using VocaluxeLib.Menu;

namespace VocaluxeLib.PartyModes.Challenge
{
    // ReSharper disable UnusedMember.Global
    public class CPartyScreenChallengeConfig : CMenuParty
        // ReSharper restore UnusedMember.Global
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _SelectSlideNumPlayers = "SelectSlideNumPlayers";
        private const string _SelectSlideNumMics = "SelectSlideNumMics";
        private const string _SelectSlideNumRounds = "SelectSlideNumRounds";
        private const string _ButtonNext = "ButtonNext";
        private const string _ButtonBack = "ButtonBack";

        private int _MaxNumMics = 2;
        private int _MaxNumRounds = 100;
        private int _RoundSteps = 1;

        private SDataFromScreen _Data;

        public override void Init()
        {
            base.Init();

            _ThemeSelectSlides = new string[] {_SelectSlideNumPlayers, _SelectSlideNumMics, _SelectSlideNumRounds};
            _ThemeButtons = new string[] {_ButtonNext, _ButtonBack};

            _Data = new SDataFromScreen();
            SFromScreenConfig config = new SFromScreenConfig {NumPlayer = 4, NumPlayerAtOnce = 2, NumRounds = 12};
            _Data.ScreenConfig = config;
        }

        public override void DataToScreen(object receivedData)
        {
            try
            {
                SDataToScreenConfig config = (SDataToScreenConfig)receivedData;
                _Data.ScreenConfig.NumPlayer = config.NumPlayer;
                _Data.ScreenConfig.NumPlayerAtOnce = config.NumPlayerAtOnce;
                _Data.ScreenConfig.NumRounds = config.NumRounds;
            }
            catch (Exception e)
            {
                CBase.Log.LogError("Error in party mode screen challenge config. Can't cast received data from game mode " + ThemeName + ". " + e.Message);
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
                        _Back();
                        break;

                    case Keys.Enter:
                        _UpdateSlides();

                        if (_Buttons[_ButtonBack].Selected)
                            _Back();

                        if (_Buttons[_ButtonNext].Selected)
                            _Next();
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

            if (mouseEvent.LB && _IsMouseOver(mouseEvent))
            {
                _UpdateSlides();
                if (_Buttons[_ButtonBack].Selected)
                    _Back();

                if (_Buttons[_ButtonNext].Selected)
                    _Next();
            }

            if (mouseEvent.RB)
                _Back();

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            _MaxNumMics = CBase.Config.GetMaxNumMics();
            if (_MaxNumMics > 6)
                _MaxNumMics = 6;

            _MaxNumRounds = _PartyMode.GetMaxNumRounds();

            _RebuildSlides();
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

        private void _RebuildSlides()
        {
            // build num player slide (min player ... max player);
            _SelectSlides[_SelectSlideNumPlayers].Clear();
            for (int i = _PartyMode.GetMinPlayer(); i <= _PartyMode.GetMaxPlayer(); i++)
                _SelectSlides[_SelectSlideNumPlayers].AddValue(i.ToString());
            _SelectSlides[_SelectSlideNumPlayers].Selection = _Data.ScreenConfig.NumPlayer - _PartyMode.GetMinPlayer();

            _UpdateMicsAtOnce();
            _SetRoundSteps();
            _UpdateSlideRounds();
        }

        private void _UpdateSlides()
        {
            int player = _Data.ScreenConfig.NumPlayer;
            int mics = _Data.ScreenConfig.NumPlayerAtOnce;
            _Data.ScreenConfig.NumPlayer = _SelectSlides[_SelectSlideNumPlayers].Selection + _PartyMode.GetMinPlayer();
            _Data.ScreenConfig.NumPlayerAtOnce = _SelectSlides[_SelectSlideNumMics].Selection + _PartyMode.GetMinPlayer();
            _Data.ScreenConfig.NumRounds = (_SelectSlides[_SelectSlideNumRounds].Selection + 1) * _RoundSteps;

            _UpdateMicsAtOnce();
            _SetRoundSteps();

            if (player != _Data.ScreenConfig.NumPlayer || mics != _Data.ScreenConfig.NumPlayerAtOnce)
            {
                int num = CHelper.CombinationCount(_Data.ScreenConfig.NumPlayer, _Data.ScreenConfig.NumPlayerAtOnce);
                while (num > _MaxNumRounds)
                    num -= _RoundSteps;
                _Data.ScreenConfig.NumRounds = num;
            }

            _UpdateSlideRounds();
        }

        private void _UpdateMicsAtOnce()
        {
            //Data.ScreenConfig.NumPlayerAtOnce
            int maxNum = _MaxNumMics;
            if (_Data.ScreenConfig.NumPlayer < _MaxNumMics)
                maxNum = _Data.ScreenConfig.NumPlayer;

            if (_Data.ScreenConfig.NumPlayerAtOnce > maxNum)
                _Data.ScreenConfig.NumPlayerAtOnce = maxNum;

            // build mics at once slide
            _SelectSlides[_SelectSlideNumMics].Clear();
            for (int i = 1; i <= maxNum; i++)
                _SelectSlides[_SelectSlideNumMics].AddValue(i.ToString());
            _SelectSlides[_SelectSlideNumMics].Selection = _Data.ScreenConfig.NumPlayerAtOnce - _PartyMode.GetMinPlayer();
        }

        private void _UpdateSlideRounds()
        {
            // build num rounds slide
            _SelectSlides[_SelectSlideNumRounds].Clear();
            for (int i = _RoundSteps; i <= _MaxNumRounds; i += _RoundSteps)
                _SelectSlides[_SelectSlideNumRounds].AddValue(i.ToString());
            _SelectSlides[_SelectSlideNumRounds].Selection = _Data.ScreenConfig.NumRounds / _RoundSteps - 1;
        }

        private void _SetRoundSteps()
        {
            if (_Data.ScreenConfig.NumPlayerAtOnce < 1 || _Data.ScreenConfig.NumPlayer < 1 || _Data.ScreenConfig.NumPlayerAtOnce > _Data.ScreenConfig.NumPlayer)
            {
                _RoundSteps = 1;
                return;
            }

            int res = _Data.ScreenConfig.NumPlayer / _Data.ScreenConfig.NumPlayerAtOnce;
            int mod = _Data.ScreenConfig.NumPlayer % _Data.ScreenConfig.NumPlayerAtOnce;

            _RoundSteps = mod == 0 ? res : _Data.ScreenConfig.NumPlayer;
        }

        private void _Back()
        {
            _FadeTo(EScreens.ScreenParty);
        }

        private void _Next()
        {
            _PartyMode.DataFromScreen(ThemeName, _Data);
        }
    }
}