using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    class CScreenOptionsLyrics : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string SelectSlideLyricStyle = "SelectSlideLyricStyle";
        private const string SelectSlideLyricsOnTop = "SelectSlideLyricsOnTop";

        private const string ButtonExit = "ButtonExit";

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new[] {ButtonExit};
            _ThemeSelectSlides = new[] {SelectSlideLyricStyle, SelectSlideLyricsOnTop};
        }

        public override void LoadTheme(string XmlPath)
        {
            base.LoadTheme(XmlPath);
            SelectSlides[SelectSlideLyricStyle].SetValues<ELyricStyle>((int)CConfig.LyricStyle);
            SelectSlides[SelectSlideLyricsOnTop].SetValues<EOffOn>((int)CConfig.LyricsOnTop);
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
                        SaveConfig();
                        CGraphics.FadeTo(EScreens.ScreenOptions);
                        break;

                    case Keys.S:
                        CParty.SetNormalGameMode();
                        CGraphics.FadeTo(EScreens.ScreenSong);
                        break;

                    case Keys.Enter:
                        if (Buttons[ButtonExit].Selected)
                        {
                            SaveConfig();
                            CGraphics.FadeTo(EScreens.ScreenOptions);
                        }
                        break;

                    case Keys.Left:
                        SaveConfig();
                        break;

                    case Keys.Right:
                        SaveConfig();
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(MouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.RB)
            {
                SaveConfig();
                CGraphics.FadeTo(EScreens.ScreenOptions);
            }
            if (mouseEvent.LB && IsMouseOver(mouseEvent))
            {
                SaveConfig();
                if (Buttons[ButtonExit].Selected)
                    CGraphics.FadeTo(EScreens.ScreenOptions);
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

        private void SaveConfig()
        {
            CConfig.LyricsOnTop = (EOffOn)SelectSlides[SelectSlideLyricsOnTop].Selection;
            CConfig.LyricStyle = (ELyricStyle)SelectSlides[SelectSlideLyricStyle].Selection;
            CConfig.SaveConfig();
        }
    }
}