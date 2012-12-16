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

        private int MaxNumMics = 4;
        private int MaxNumRounds = 100;

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
                _Base.Log.LogError("Error in party mode screen challenge config. Can't cast received data from game mode " + _ThemeName + ". " + e.Message);;
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
            SelectSlides[htSelectSlides(SelectSlideNumPlayers)].Clear();
            for (int i = _PartyMode.GetMinPlayer(); i <= _PartyMode.GetMaxPlayer(); i++)
            {
                SelectSlides[htSelectSlides(SelectSlideNumPlayers)].AddValue(i.ToString());
            }
            SelectSlides[htSelectSlides(SelectSlideNumPlayers)].Selection = Data.ScreenConfig.NumPlayer - 1;


            //TODO: Max number of mics should be number of mics available
            int _MaxNumMics = MaxNumMics;
            SelectSlides[htSelectSlides(SelectSlideNumMics)].Clear();
            if (Data.ScreenConfig.NumPlayer < MaxNumMics)
                _MaxNumMics = Data.ScreenConfig.NumPlayer;
            for (int i = 1; i <= _MaxNumMics; i++)
            {
                SelectSlides[htSelectSlides(SelectSlideNumMics)].AddValue(i.ToString());
            }
            SelectSlides[htSelectSlides(SelectSlideNumMics)].Selection = Data.ScreenConfig.NumPlayerAtOnce - 1;

            //TODO: Max number ofs round should depend on number of players and mics
            SelectSlides[htSelectSlides(SelectSlideNumRounds)].Clear();
            for (int i = 1; i <= MaxNumRounds; i++)
            {
                SelectSlides[htSelectSlides(SelectSlideNumRounds)].AddValue(i.ToString());
            }
            SelectSlides[htSelectSlides(SelectSlideNumRounds)].Selection = Data.ScreenConfig.NumRounds - 1;

            UpdateSlides();
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

        private void UpdateSlides()
        {
            //Update slides when one value was changed
            if (Data.ScreenConfig.NumPlayer != (SelectSlides[htSelectSlides(SelectSlideNumPlayers)].Selection + _PartyMode.GetMinPlayer()))
            {
                if (Data.ScreenConfig.NumPlayerAtOnce != (SelectSlides[htSelectSlides(SelectSlideNumPlayers)].Selection + _PartyMode.GetMinPlayer()))
                {
                    int OldValueNumPlayer = Data.ScreenConfig.NumPlayer;
                    Data.ScreenConfig.NumPlayer = (SelectSlides[htSelectSlides(SelectSlideNumPlayers)].Selection + _PartyMode.GetMinPlayer());
                    int _MaxNumMics = MaxNumMics;
                    SelectSlides[htSelectSlides(SelectSlideNumMics)].Clear();
                    if (Data.ScreenConfig.NumPlayer < MaxNumMics)
                        _MaxNumMics = Data.ScreenConfig.NumPlayer;
                    for (int i = 1; i <= _MaxNumMics; i++)
                    {
                        SelectSlides[htSelectSlides(SelectSlideNumMics)].AddValue(i.ToString());
                    }
                    if (SelectSlides[htSelectSlides(SelectSlideNumMics)].NumValues >= Data.ScreenConfig.NumPlayerAtOnce)
                        SelectSlides[htSelectSlides(SelectSlideNumMics)].Selection = Data.ScreenConfig.NumPlayerAtOnce - 1;
                    if (OldValueNumPlayer == Data.ScreenConfig.NumPlayerAtOnce)
                        SelectSlides[htSelectSlides(SelectSlideNumMics)].Selection = SelectSlides[htSelectSlides(SelectSlideNumMics)].NumValues - 1;
                }
            }
            Data.ScreenConfig.NumPlayer = (SelectSlides[htSelectSlides(SelectSlideNumPlayers)].Selection + _PartyMode.GetMinPlayer());
            Data.ScreenConfig.NumPlayerAtOnce = SelectSlides[htSelectSlides(SelectSlideNumMics)].Selection + 1;
            Data.ScreenConfig.NumRounds = SelectSlides[htSelectSlides(SelectSlideNumRounds)].Selection + 1;
        }

        private void Back()
        {
            FadeTo(EScreens.ScreenParty);
        }

        private void Next()
        {
            Data.ScreenConfig.NumPlayer = (SelectSlides[htSelectSlides(SelectSlideNumPlayers)].Selection + _PartyMode.GetMinPlayer());
            Data.ScreenConfig.NumPlayerAtOnce = SelectSlides[htSelectSlides(SelectSlideNumMics)].Selection + 1;
            Data.ScreenConfig.NumRounds = SelectSlides[htSelectSlides(SelectSlideNumRounds)].Selection + 1;

            _PartyMode.DataFromScreen(_ThemeName, Data);
        }
    }
}
