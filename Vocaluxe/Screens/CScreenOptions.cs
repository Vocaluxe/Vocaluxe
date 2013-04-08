using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    class CScreenOptions : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string ButtonOptionsGame = "ButtonOptionsGame";
        private const string ButtonOptionsSound = "ButtonOptionsSound";
        private const string ButtonOptionsRecord = "ButtonOptionsRecord";
        private const string ButtonOptionsVideo = "ButtonOptionsVideo";
        private const string ButtonOptionsLyrics = "ButtonOptionsLyrics";
        private const string ButtonOptionsTheme = "ButtonOptionsTheme";

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[] {ButtonOptionsGame, ButtonOptionsSound, ButtonOptionsRecord, ButtonOptionsVideo, ButtonOptionsLyrics, ButtonOptionsTheme};
        }

        public override bool HandleInput(KeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed) {}
            else
            {
                switch (keyEvent.Key)
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
                        if (Buttons[ButtonOptionsGame].Selected)
                            CGraphics.FadeTo(EScreens.ScreenOptionsGame);

                        if (Buttons[ButtonOptionsSound].Selected)
                            CGraphics.FadeTo(EScreens.ScreenOptionsSound);

                        if (Buttons[ButtonOptionsRecord].Selected)
                            CGraphics.FadeTo(EScreens.ScreenOptionsRecord);

                        if (Buttons[ButtonOptionsVideo].Selected)
                            CGraphics.FadeTo(EScreens.ScreenOptionsVideo);

                        if (Buttons[ButtonOptionsLyrics].Selected)
                            CGraphics.FadeTo(EScreens.ScreenOptionsLyrics);

                        if (Buttons[ButtonOptionsTheme].Selected)
                            CGraphics.FadeTo(EScreens.ScreenOptionsTheme);

                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(MouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.LB && IsMouseOver(mouseEvent))
            {
                if (Buttons[ButtonOptionsGame].Selected)
                    CGraphics.FadeTo(EScreens.ScreenOptionsGame);

                if (Buttons[ButtonOptionsSound].Selected)
                    CGraphics.FadeTo(EScreens.ScreenOptionsSound);

                if (Buttons[ButtonOptionsRecord].Selected)
                    CGraphics.FadeTo(EScreens.ScreenOptionsRecord);

                if (Buttons[ButtonOptionsVideo].Selected)
                    CGraphics.FadeTo(EScreens.ScreenOptionsVideo);

                if (Buttons[ButtonOptionsLyrics].Selected)
                    CGraphics.FadeTo(EScreens.ScreenOptionsLyrics);

                if (Buttons[ButtonOptionsTheme].Selected)
                    CGraphics.FadeTo(EScreens.ScreenOptionsTheme);
            }

            if (mouseEvent.RB)
                CGraphics.FadeTo(EScreens.ScreenMain);
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