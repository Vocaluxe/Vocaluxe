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

        const string ButtonNext = "ButtonNext";
        const string ButtonBack = "ButtonBack";

        DataFromScreen Data;

        public PartyScreenChallengeConfig()
        {
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "PartyScreenChallengeConfig";
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

        private void Back()
        {
            FadeTo(EScreens.ScreenParty);
        }

        private void Next()
        {
            Data.ScreenConfig.NumPlayers = 4;
            Data.ScreenConfig.NumPlayersAtOnce = 2;
            Data.ScreenConfig.NumRounds = 8;

            _PartyMode.DataFromScreen(_ThemeName, Data);
        }
    }
}
