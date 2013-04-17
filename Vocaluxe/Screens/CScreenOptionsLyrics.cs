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

        private const string _SelectSlideLyricStyle = "SelectSlideLyricStyle";
        private const string _SelectSlideLyricsOnTop = "SelectSlideLyricsOnTop";

        private const string _ButtonExit = "ButtonExit";

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[] {_ButtonExit};
            _ThemeSelectSlides = new string[] {_SelectSlideLyricStyle, _SelectSlideLyricsOnTop};
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);
            SelectSlides[_SelectSlideLyricStyle].SetValues<ELyricStyle>((int)CConfig.LyricStyle);
            SelectSlides[_SelectSlideLyricsOnTop].SetValues<EOffOn>((int)CConfig.LyricsOnTop);
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed) {}
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                        _SaveConfig();
                        CGraphics.FadeTo(EScreens.ScreenOptions);
                        break;

                    case Keys.S:
                        CParty.SetNormalGameMode();
                        CGraphics.FadeTo(EScreens.ScreenSong);
                        break;

                    case Keys.Enter:
                        if (Buttons[_ButtonExit].Selected)
                        {
                            _SaveConfig();
                            CGraphics.FadeTo(EScreens.ScreenOptions);
                        }
                        break;

                    case Keys.Left:
                        _SaveConfig();
                        break;

                    case Keys.Right:
                        _SaveConfig();
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.RB)
            {
                _SaveConfig();
                CGraphics.FadeTo(EScreens.ScreenOptions);
            }
            if (mouseEvent.LB && IsMouseOver(mouseEvent))
            {
                _SaveConfig();
                if (Buttons[_ButtonExit].Selected)
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

        private void _SaveConfig()
        {
            CConfig.LyricsOnTop = (EOffOn)SelectSlides[_SelectSlideLyricsOnTop].Selection;
            CConfig.LyricStyle = (ELyricStyle)SelectSlides[_SelectSlideLyricStyle].Selection;
            CConfig.SaveConfig();
        }
    }
}