using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    class CScreenMain : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string ButtonSing = "ButtonSing";
        private const string ButtonParty = "ButtonParty";
        private const string ButtonOptions = "ButtonOptions";
        private const string ButtonProfiles = "ButtonProfiles";
        private const string ButtonExit = "ButtonExit";

        //CParticleEffect Snowflakes;

        public override void Init()
        {
            base.Init();

            _ThemeStatics = new[] {"StaticMenuBar"};
            _ThemeButtons = new[] {ButtonSing, ButtonParty, ButtonOptions, ButtonProfiles, ButtonExit};
        }

        public override void LoadTheme(string XmlPath)
        {
            base.LoadTheme(XmlPath);
        }

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            base.HandleInput(KeyEvent);

            if (KeyEvent.KeyPressed) {}
            else
            {
                switch (KeyEvent.Key)
                {
                    case Keys.O:
                        CGraphics.FadeTo(EScreens.ScreenOptions);
                        break;

                    case Keys.S:
                        CGraphics.FadeTo(EScreens.ScreenSong);
                        break;

                    case Keys.C:
                        CGraphics.FadeTo(EScreens.ScreenCredits);
                        break;

                    case Keys.T:
                        CGraphics.FadeTo(EScreens.ScreenTest);
                        break;

                    case Keys.Enter:
                        if (Buttons[ButtonSing].Selected)
                        {
                            CParty.SetNormalGameMode();
                            CGraphics.FadeTo(EScreens.ScreenSong);
                        }

                        if (Buttons[ButtonParty].Selected)
                            CGraphics.FadeTo(EScreens.ScreenParty);

                        if (Buttons[ButtonOptions].Selected)
                            CGraphics.FadeTo(EScreens.ScreenOptions);

                        if (Buttons[ButtonProfiles].Selected)
                            CGraphics.FadeTo(EScreens.ScreenProfiles);

                        if (Buttons[ButtonExit].Selected)
                            return false;

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
                if (Buttons[ButtonSing].Selected)
                {
                    CParty.SetNormalGameMode();
                    CGraphics.FadeTo(EScreens.ScreenSong);
                }

                if (Buttons[ButtonParty].Selected)
                    CGraphics.FadeTo(EScreens.ScreenParty);

                if (Buttons[ButtonOptions].Selected)
                    CGraphics.FadeTo(EScreens.ScreenOptions);

                if (Buttons[ButtonProfiles].Selected)
                    CGraphics.FadeTo(EScreens.ScreenProfiles);

                if (Buttons[ButtonExit].Selected)
                    return false;
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            //if (Snowflakes != null)
            //    Snowflakes.Resume();
        }

        public override bool UpdateGame()
        {
            return true;
        }

        public override bool Draw()
        {
            base.DrawBG();

            //if (Snowflakes == null)
            //    Snowflakes = new CParticleEffect(300, new SColorF(1, 1, 1, 1), new SRectF(0, 0, CSettings.iRenderW, 0, 0.5f), "Snowflake", 25, EParticeType.Snow);

            //Snowflakes.Update();
            //Snowflakes.Draw();

            base.DrawFG();

            if (CSettings.VersionRevision != ERevision.Release)
            {
                CFonts.SetFont("Normal");
                CFonts.Style = EStyle.Normal;
                CDraw.DrawText(CSettings.GetFullVersionText(), 10, 680, 40);
            }

            return true;
        }

        public override void OnClose()
        {
            base.OnClose();

            //if (Snowflakes != null)
            //    Snowflakes.Pause();
        }
    }
}