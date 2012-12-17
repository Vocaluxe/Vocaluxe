using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Menu;

namespace Vocaluxe.PartyModes
{
    public class PartyScreenChallengeMain : CMenuParty
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        const string ButtonNextRound = "ButtonNextRound";

        private DataFromScreen Data;

        public PartyScreenChallengeMain()
        {
            Data = new DataFromScreen();
            Data.ScreenMain = new FromScreenMain();
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "PartyScreenChallengeMain";
            _ThemeButtons = new string[] { "ButtonNextRound" };
            _ScreenVersion = ScreenVersion;
        }

        public override void LoadTheme(string XmlPath)
        {
			base.LoadTheme(XmlPath);
        }

        public override void DataToScreen(object ReceivedData)
        {
            DataToScreenMain data = new DataToScreenMain();

            try
            {
                data = (DataToScreenMain)ReceivedData;
                
            }
            catch (Exception e)
            {
                _Base.Log.LogError("Error in party mode screen challenge main. Can't cast received data from game mode " + _ThemeName + ". " + e.Message); ;
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
                        EndParty();
                        break;

                    case Keys.Enter:
                        if (Buttons[htButtons(ButtonNextRound)].Selected)
                            NextRound();
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
                if (Buttons[htButtons(ButtonNextRound)].Selected)
                    NextRound();
            }

            if (MouseEvent.RB)
            {
                EndParty();
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

        private void NextRound()
        {
            Data.ScreenMain.FadeToSongSelection = true;
            _PartyMode.DataFromScreen(_ThemeName, Data);
        }

        private void EndParty()
        {
            //TODO
        }
    }
}
