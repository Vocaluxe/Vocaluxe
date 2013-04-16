using System;
using System.Windows.Forms;
using VocaluxeLib.Menu;

namespace VocaluxeLib.PartyModes.ChallengeMedley
{
    public class CPartyScreenChallengeMedleyConfig : CMenuParty
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
            SFromScreenConfig config = new SFromScreenConfig();
            config.NumPlayer = 4;
            config.NumPlayerAtOnce = 2;
            config.NumRounds = 12;
            _Data.ScreenConfig = config;
        }

        public override void DataToScreen(object receivedData)
        {
            SDataToScreenConfig config = new SDataToScreenConfig();

            try
            {
                config = (SDataToScreenConfig)receivedData;
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

                        if (Buttons[_ButtonBack].Selected)
                            _Back();

                        if (Buttons[_ButtonNext].Selected)
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

            if (mouseEvent.LB && IsMouseOver(mouseEvent))
            {
                _UpdateSlides();
                if (Buttons[_ButtonBack].Selected)
                    _Back();

                if (Buttons[_ButtonNext].Selected)
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
            SelectSlides[_SelectSlideNumPlayers].Clear();
            for (int i = _PartyMode.GetMinPlayer(); i <= _PartyMode.GetMaxPlayer(); i++)
                SelectSlides[_SelectSlideNumPlayers].AddValue(i.ToString());
            SelectSlides[_SelectSlideNumPlayers].Selection = _Data.ScreenConfig.NumPlayer - _PartyMode.GetMinPlayer();

            _UpdateMicsAtOnce();
            _SetRoundSteps();
            _UpdateSlideRounds();
        }

        private void _UpdateSlides()
        {
            int player = _Data.ScreenConfig.NumPlayer;
            int mics = _Data.ScreenConfig.NumPlayerAtOnce;
            _Data.ScreenConfig.NumPlayer = SelectSlides[_SelectSlideNumPlayers].Selection + _PartyMode.GetMinPlayer();
            _Data.ScreenConfig.NumPlayerAtOnce = SelectSlides[_SelectSlideNumMics].Selection + _PartyMode.GetMinPlayer();
            _Data.ScreenConfig.NumRounds = (SelectSlides[_SelectSlideNumRounds].Selection + 1) * _RoundSteps;

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
            SelectSlides[_SelectSlideNumMics].Clear();
            for (int i = 1; i <= maxNum; i++)
                SelectSlides[_SelectSlideNumMics].AddValue(i.ToString());
            SelectSlides[_SelectSlideNumMics].Selection = _Data.ScreenConfig.NumPlayerAtOnce - _PartyMode.GetMinPlayer();
        }

        private void _UpdateSlideRounds()
        {
            // build num rounds slide
            SelectSlides[_SelectSlideNumRounds].Clear();
            for (int i = _RoundSteps; i <= _MaxNumRounds; i += _RoundSteps)
                SelectSlides[_SelectSlideNumRounds].AddValue(i.ToString());
            SelectSlides[_SelectSlideNumRounds].Selection = _Data.ScreenConfig.NumRounds / _RoundSteps - 1;
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

            if (mod == 0)
                _RoundSteps = res;
            else
                _RoundSteps = _Data.ScreenConfig.NumPlayer;
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