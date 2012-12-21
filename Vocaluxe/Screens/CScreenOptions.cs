using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Menu;


namespace Vocaluxe.Screens
{
    class CScreenOptions : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        private const string ButtonOptionsGame = "ButtonOptionsGame";
        private const string ButtonOptionsSound = "ButtonOptionsSound";
        private const string ButtonOptionsRecord = "ButtonOptionsRecord";
        private const string ButtonOptionsVideo = "ButtonOptionsVideo";
        private const string ButtonOptionsLyrics = "ButtonOptionsLyrics";
        private const string ButtonOptionsTheme = "ButtonOptionsTheme";

        public CScreenOptions()
        {
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "ScreenOptions";
            _ScreenVersion = ScreenVersion;
            _ThemeButtons = new string[] { ButtonOptionsGame, ButtonOptionsSound, ButtonOptionsRecord, ButtonOptionsVideo, ButtonOptionsLyrics, ButtonOptionsTheme };
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
                    case Keys.Escape:
                    case Keys.Back:
                        CGraphics.FadeTo(EScreens.ScreenMain);
                        break;

                    case Keys.S:
                        CParty.SetNormalGameMode();
                        CGraphics.FadeTo(EScreens.ScreenSong);
                        break;

                    case Keys.Enter:
                        if (Buttons[htButtons(ButtonOptionsGame)].Selected)
                            CGraphics.FadeTo(EScreens.ScreenOptionsGame);

                        if (Buttons[htButtons(ButtonOptionsSound)].Selected)
                            CGraphics.FadeTo(EScreens.ScreenOptionsSound);

                        if (Buttons[htButtons(ButtonOptionsRecord)].Selected)
                            CGraphics.FadeTo(EScreens.ScreenOptionsRecord);

                        if (Buttons[htButtons(ButtonOptionsVideo)].Selected)
                            CGraphics.FadeTo(EScreens.ScreenOptionsVideo);

                        if (Buttons[htButtons(ButtonOptionsLyrics)].Selected)
                            CGraphics.FadeTo(EScreens.ScreenOptionsLyrics);

                        if (Buttons[htButtons(ButtonOptionsTheme)].Selected)
                            CGraphics.FadeTo(EScreens.ScreenOptionsTheme);

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
                if (Buttons[htButtons(ButtonOptionsGame)].Selected)
                    CGraphics.FadeTo(EScreens.ScreenOptionsGame);

                if (Buttons[htButtons(ButtonOptionsSound)].Selected)
                    CGraphics.FadeTo(EScreens.ScreenOptionsSound);

                if (Buttons[htButtons(ButtonOptionsRecord)].Selected)
                    CGraphics.FadeTo(EScreens.ScreenOptionsRecord);

                if (Buttons[htButtons(ButtonOptionsVideo)].Selected)
                    CGraphics.FadeTo(EScreens.ScreenOptionsVideo);

                if (Buttons[htButtons(ButtonOptionsLyrics)].Selected)
                    CGraphics.FadeTo(EScreens.ScreenOptionsLyrics);

                if (Buttons[htButtons(ButtonOptionsTheme)].Selected)
                    CGraphics.FadeTo(EScreens.ScreenOptionsTheme);
            }

            if (MouseEvent.RB)
            {
                CGraphics.FadeTo(EScreens.ScreenMain);
            }
            return true;
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
    }
}
