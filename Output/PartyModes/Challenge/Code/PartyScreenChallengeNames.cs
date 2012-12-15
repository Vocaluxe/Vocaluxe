using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Menu;

namespace Vocaluxe.PartyModes
{
    public class PartyScreenChallengeNames : CMenuParty
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        const string ButtonNext = "ButtonNext";
        const string ButtonBack = "ButtonBack";
        const string NameSelection = "NameSelection";

        private List<string> PlayerButtons;

        //TODO: Get this data from party-mode!
        private int NumPlayers = 10;

        private DataFromScreen Data;

        public PartyScreenChallengeNames()
        {
        }

        protected override void Init()
        {
            base.Init();

            PlayerButtons = new List<string>();
            for (int i = 1; i <=  _PartyMode.GetMaxPlayer(); i++)
            {
                PlayerButtons.Add("ButtonPlayer" + i);
            }

            _ThemeName = "PartyScreenChallengeNames";
            List<string> buttons = new List<string>();
            buttons.Add(ButtonNext);
            buttons.Add(ButtonBack);
            buttons.AddRange(PlayerButtons);
            _ThemeButtons = buttons.ToArray();
            _ThemeNameSelections = new string[] { NameSelection };
            _ScreenVersion = ScreenVersion;

            Data = new DataFromScreen();
            FromScreenNames names = new FromScreenNames();
            names.FadeToConfig = false;
            names.FadeToMain = false;
            names.ProfileIDs = new List<int>();
            Data.ScreenNames = names;
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
            Buttons[htButtons(ButtonBack)].Text.Text = _PartyMode.GetMaxPlayer().ToString();
            /**
            for (int i = 1; i <= _PartyMode.GetMaxPlayer(); i++)
            {
                Buttons[htButtons("ButtonPlayer" + i)].Text.Text = "Player " + i;
                if (i <= NumPlayers)
                    Buttons[htButtons("ButtonPlayer" + i)].Visible = true;
                else
                    Buttons[htButtons("ButtonPlayer" + i)].Visible = false;
            }
             **/
            NameSelections[htNameSelections(NameSelection)].Init();
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
            Data.ScreenNames.FadeToConfig = true;
            Data.ScreenNames.FadeToMain = false;
            _PartyMode.DataFromScreen(_ThemeName, Data);
        }

        private void Next()
        {
            Data.ScreenNames.FadeToConfig = false;
            Data.ScreenNames.FadeToMain = true;
            _PartyMode.DataFromScreen(_ThemeName, Data);
        }
    }
}
