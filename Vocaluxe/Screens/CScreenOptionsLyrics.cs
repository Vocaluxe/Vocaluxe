using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Menu;

namespace Vocaluxe.Screens
{
    class CScreenOptionsLyrics : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        private const string SelectSlideLyricStyle = "SelectSlideLyricStyle";
        private const string SelectSlideLyricsOnTop = "SelectSlideLyricsOnTop";

        private const string ButtonExit = "ButtonExit";

        public CScreenOptionsLyrics()
        {
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "ScreenOptionsLyrics";
            _ScreenVersion = ScreenVersion;

            _ThemeButtons = new string[] { ButtonExit };
            _ThemeSelectSlides = new string[] { SelectSlideLyricStyle, SelectSlideLyricsOnTop };
        }

        public override void LoadTheme(string XmlPath)
        {
            base.LoadTheme(XmlPath);
            SelectSlides[htSelectSlides(SelectSlideLyricStyle)].SetValues<ELyricStyle>((int)CConfig.LyricStyle);
            SelectSlides[htSelectSlides(SelectSlideLyricsOnTop)].SetValues<EOffOn>((int)CConfig.LyricsOnTop);
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
                        SaveConfig();
                        CGraphics.FadeTo(EScreens.ScreenOptions);
                        break;

                    case Keys.S:
                        CParty.SetNormalGameMode();
                        CGraphics.FadeTo(EScreens.ScreenSong);
                        break;

                    case Keys.Enter:
                        if (Buttons[htButtons(ButtonExit)].Selected)
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

        public override bool HandleMouse(MouseEvent MouseEvent)
        {
            base.HandleMouse(MouseEvent);

            if (MouseEvent.RB)
            {
                SaveConfig();
                CGraphics.FadeTo(EScreens.ScreenOptions);
            }
            if (MouseEvent.LB && IsMouseOver(MouseEvent))
            {
                SaveConfig();
                if (Buttons[htButtons(ButtonExit)].Selected)
                {
                    CGraphics.FadeTo(EScreens.ScreenOptions);
                }
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
            CConfig.LyricsOnTop = (EOffOn)SelectSlides[htSelectSlides(SelectSlideLyricsOnTop)].Selection;
            CConfig.LyricStyle = (ELyricStyle)SelectSlides[htSelectSlides(SelectSlideLyricStyle)].Selection;
            CConfig.SaveConfig();
        }
    }
}
