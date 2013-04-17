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

        private const string _ButtonSing = "ButtonSing";
        private const string _ButtonParty = "ButtonParty";
        private const string _ButtonOptions = "ButtonOptions";
        private const string _ButtonProfiles = "ButtonProfiles";
        private const string _ButtonExit = "ButtonExit";

        //CParticleEffect Snowflakes;
        public override void Init()
        {
            base.Init();

            _ThemeStatics = new string[] {"StaticMenuBar"};
            _ThemeButtons = new string[] {_ButtonSing, _ButtonParty, _ButtonOptions, _ButtonProfiles, _ButtonExit};
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed) {}
            else
            {
                switch (keyEvent.Key)
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
                        if (Buttons[_ButtonSing].Selected)
                        {
                            CParty.SetNormalGameMode();
                            CGraphics.FadeTo(EScreens.ScreenSong);
                        }

                        if (Buttons[_ButtonParty].Selected)
                            CGraphics.FadeTo(EScreens.ScreenParty);

                        if (Buttons[_ButtonOptions].Selected)
                            CGraphics.FadeTo(EScreens.ScreenOptions);

                        if (Buttons[_ButtonProfiles].Selected)
                            CGraphics.FadeTo(EScreens.ScreenProfiles);

                        if (Buttons[_ButtonExit].Selected)
                            return false;

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
                if (Buttons[_ButtonSing].Selected)
                {
                    CParty.SetNormalGameMode();
                    CGraphics.FadeTo(EScreens.ScreenSong);
                }

                if (Buttons[_ButtonParty].Selected)
                    CGraphics.FadeTo(EScreens.ScreenParty);

                if (Buttons[_ButtonOptions].Selected)
                    CGraphics.FadeTo(EScreens.ScreenOptions);

                if (Buttons[_ButtonProfiles].Selected)
                    CGraphics.FadeTo(EScreens.ScreenProfiles);

                if (Buttons[_ButtonExit].Selected)
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
            DrawBG();

            //if (Snowflakes == null)
            //    Snowflakes = new CParticleEffect(300, new SColorF(1, 1, 1, 1), new SRectF(0, 0, CSettings.iRenderW, 0, 0.5f), "Snowflake", 25, EParticeType.Snow);

            //Snowflakes.Update();
            //Snowflakes.Draw();
            DrawFG();

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