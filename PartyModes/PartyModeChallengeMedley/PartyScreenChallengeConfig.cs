using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Menu;

namespace Vocaluxe.PartyModes
{
    public class PartyScreenChallengeConfig : CMenuParty
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        const string SelectSlideNumPlayers = "SelectSlideNumPlayers";
        const string SelectSlideNumMics = "SelectSlideNumMics";
        const string SelectSlideNumRounds = "SelectSlideNumRounds";
        const string ButtonNext = "ButtonNext";
        const string ButtonBack = "ButtonBack";

        private int _MaxNumMics = 2;
        private int _MaxNumRounds = 100;
        private int _RoundSteps = 1;

        DataFromScreen Data;

        public PartyScreenChallengeConfig()
        {
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "PartyScreenChallengeConfig";
            _ThemeSelectSlides = new string[] { SelectSlideNumPlayers, SelectSlideNumMics, SelectSlideNumRounds };
            _ThemeButtons = new string[] { ButtonNext, ButtonBack };
            _ScreenVersion = ScreenVersion;

            Data = new DataFromScreen();
            FromScreenConfig config = new FromScreenConfig();
            config.NumPlayer = 4;
            config.NumPlayerAtOnce = 2;
            config.NumRounds = 12;
            Data.ScreenConfig = config;
        }

        public override void LoadTheme(string XmlPath)
        {
			base.LoadTheme(XmlPath);
        }

        public override void DataToScreen(object ReceivedData)
        {
            DataToScreenConfig config = new DataToScreenConfig();

            try
            {
                config = (DataToScreenConfig)ReceivedData;
                Data.ScreenConfig.NumPlayer = config.NumPlayer;
                Data.ScreenConfig.NumPlayerAtOnce = config.NumPlayerAtOnce;
                Data.ScreenConfig.NumRounds = config.NumRounds;
            }
            catch (Exception e)
            {
                CBase.Log.LogError("Error in party mode screen challenge config. Can't cast received data from game mode " + _ThemeName + ". " + e.Message);;
            }

        }

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            base.HandleInput(KeyEvent);

            if (KeyEvent.KeyPressed)
            {
             
            }
            else
            {
                switch (KeyEvent.Key)
                {
                    case Keys.Back:
                    case Keys.Escape:
                        Back();
                        break;

                    case Keys.Enter:
                        UpdateSlides();

                        if (Buttons[htButtons(ButtonBack)].Selected)
                            Back();

                        if (Buttons[htButtons(ButtonNext)].Selected)
                            Next();
                        break;

                    case Keys.Left:
                        UpdateSlides();
                        break;

                    case Keys.Right:
                        UpdateSlides();
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(MouseEvent MouseEvent)
        {
            base.HandleMouse(MouseEvent);

            if (MouseEvent.LB && IsMouseOver(MouseEvent))
            {
                UpdateSlides();
                if (Buttons[htButtons(ButtonBack)].Selected)
                    Back();

                if (Buttons[htButtons(ButtonNext)].Selected)
                    Next();
            }

            if (MouseEvent.RB)
            {
                Back();
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            _MaxNumMics = CBase.Config.GetMaxNumMics();
            if (_MaxNumMics > 6)
                _MaxNumMics = 6;

            _MaxNumRounds = _PartyMode.GetMaxNumRounds();

            RebuildSlides();
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

        public override void OnClose()
        {
            base.OnClose();
        }

        private void RebuildSlides()
        {
            // build num player slide (min player ... max player);
            SelectSlides[htSelectSlides(SelectSlideNumPlayers)].Clear();
            for (int i = _PartyMode.GetMinPlayer(); i <= _PartyMode.GetMaxPlayer(); i++)
            {
                SelectSlides[htSelectSlides(SelectSlideNumPlayers)].AddValue(i.ToString());
            }
            SelectSlides[htSelectSlides(SelectSlideNumPlayers)].Selection = Data.ScreenConfig.NumPlayer - _PartyMode.GetMinPlayer();

            UpdateMicsAtOnce();
            SetRoundSteps();
            UpdateSlideRounds();
        }    

        private void UpdateSlides()
        {
            int player = Data.ScreenConfig.NumPlayer;
            int mics = Data.ScreenConfig.NumPlayerAtOnce;
            Data.ScreenConfig.NumPlayer = SelectSlides[htSelectSlides(SelectSlideNumPlayers)].Selection + _PartyMode.GetMinPlayer();
            Data.ScreenConfig.NumPlayerAtOnce = SelectSlides[htSelectSlides(SelectSlideNumMics)].Selection + _PartyMode.GetMinPlayer();
            Data.ScreenConfig.NumRounds = (SelectSlides[htSelectSlides(SelectSlideNumRounds)].Selection + 1) * _RoundSteps;

            UpdateMicsAtOnce();
            SetRoundSteps();

            if (player != Data.ScreenConfig.NumPlayer || mics != Data.ScreenConfig.NumPlayerAtOnce)
            {
                int num = CHelper.nCk(Data.ScreenConfig.NumPlayer, Data.ScreenConfig.NumPlayerAtOnce);
                while (num > _MaxNumRounds)
                {
                    num -= _RoundSteps;
                }
                Data.ScreenConfig.NumRounds = num;
            }

            UpdateSlideRounds();         
        }

        private void UpdateMicsAtOnce()
        {
            //Data.ScreenConfig.NumPlayerAtOnce
            int MaxNum = _MaxNumMics;
            if (Data.ScreenConfig.NumPlayer < _MaxNumMics)
                MaxNum = Data.ScreenConfig.NumPlayer;

            if (Data.ScreenConfig.NumPlayerAtOnce > MaxNum)
                Data.ScreenConfig.NumPlayerAtOnce = MaxNum;

            // build mics at once slide
            SelectSlides[htSelectSlides(SelectSlideNumMics)].Clear();
            for (int i = 1; i <= MaxNum; i++)
            {
                SelectSlides[htSelectSlides(SelectSlideNumMics)].AddValue(i.ToString());
            }
            SelectSlides[htSelectSlides(SelectSlideNumMics)].Selection = Data.ScreenConfig.NumPlayerAtOnce - _PartyMode.GetMinPlayer();
        }

        private void UpdateSlideRounds()
        {
            // build num rounds slide
            SelectSlides[htSelectSlides(SelectSlideNumRounds)].Clear();
            for (int i = _RoundSteps; i <= _MaxNumRounds; i += _RoundSteps)
            {
                SelectSlides[htSelectSlides(SelectSlideNumRounds)].AddValue(i.ToString());
            }
            SelectSlides[htSelectSlides(SelectSlideNumRounds)].Selection = Data.ScreenConfig.NumRounds / _RoundSteps - 1;
        }

        private void SetRoundSteps()
        {
            if (Data.ScreenConfig.NumPlayerAtOnce < 1 || Data.ScreenConfig.NumPlayer < 1 || Data.ScreenConfig.NumPlayerAtOnce > Data.ScreenConfig.NumPlayer)
            {
                _RoundSteps = 1;
                return;
            }

            int res = Data.ScreenConfig.NumPlayer / Data.ScreenConfig.NumPlayerAtOnce;
            int mod = Data.ScreenConfig.NumPlayer % Data.ScreenConfig.NumPlayerAtOnce;

            if (mod == 0)
                _RoundSteps = res;
            else
                _RoundSteps = Data.ScreenConfig.NumPlayer; 
        }

        private void Back()
        {
            FadeTo(EScreens.ScreenParty);
        }

        private void Next()
        {
            _PartyMode.DataFromScreen(_ThemeName, Data);
        }
    }
}
