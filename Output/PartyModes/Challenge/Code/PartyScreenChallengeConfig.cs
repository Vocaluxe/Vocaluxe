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

        private int MaxNumMics = 2;
        private int MaxNumRounds = 10;

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
            config.NumPlayers = 0;
            config.NumPlayersAtOnce = 0;
            config.NumRounds = 0;
            Data.ScreenConfig = config;
        }

        public override void LoadTheme(string XmlPath)
        {
			base.LoadTheme(XmlPath);
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
                        if (Buttons[htButtons(ButtonBack)].Selected)
                            Back();

                        if (Buttons[htButtons(ButtonNext)].Selected)
                            Next();
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
            //TODO: Max number of mics should be number of mics available
            SelectSlides[htSelectSlides(SelectSlideNumMics)].Clear();
            for (int i = 1; i <= MaxNumMics; i++)
            {
                SelectSlides[htSelectSlides(SelectSlideNumMics)].AddValue(i.ToString());
            }
            //TODO: Max number ofs round should depend on number of players and mics
            SelectSlides[htSelectSlides(SelectSlideNumRounds)].Clear();
            for (int i = 1; i <= MaxNumRounds; i++)
            {
                SelectSlides[htSelectSlides(SelectSlideNumRounds)].AddValue(i.ToString());
            }
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
        }

        private void Back()
        {
            FadeTo(EScreens.ScreenParty);
        }

        private void Next()
        {
            Data.ScreenConfig.NumPlayers = (SelectSlides[htSelectSlides(SelectSlideNumPlayers)].Selection + _PartyMode.GetMinPlayer());
            Data.ScreenConfig.NumPlayersAtOnce = SelectSlides[htSelectSlides(SelectSlideNumMics)].Selection;
            Data.ScreenConfig.NumRounds = SelectSlides[htSelectSlides(SelectSlideNumRounds)].Selection;

            _PartyMode.DataFromScreen(_ThemeName, Data);
        }
    }
}
