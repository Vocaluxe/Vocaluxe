using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Menu;


namespace Vocaluxe.Screens
{
    class CScreenOptionsVideo : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 2;

        private const string SelectSlideVideoBackgrounds = "SelectSlideVideoBackgrounds";
        private const string SelectSlideVideoPreview = "SelectSlideVideoPreview";
        private const string SelectSlideVideosInSongs = "SelectSlideVideosInSongs";
        private const string SelectSlideVideosToBackground = "SelectSlideVideosToBackground";

        private const string ButtonExit = "ButtonExit";

        public CScreenOptionsVideo()
        {
            Init();
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "ScreenOptionsVideo";
            _ScreenVersion = ScreenVersion;

            _ThemeButtons = new string[] { ButtonExit };
            _ThemeSelectSlides = new string[] { SelectSlideVideoBackgrounds, SelectSlideVideoPreview, SelectSlideVideosInSongs, SelectSlideVideosToBackground };
        }

        public override void LoadTheme()
        {
            base.LoadTheme();

            SelectSlides[htSelectSlides(SelectSlideVideoBackgrounds)].SetValues<EOffOn>((int)CConfig.VideoBackgrounds);
            SelectSlides[htSelectSlides(SelectSlideVideoPreview)].SetValues<EOffOn>((int)CConfig.VideoPreview);
            SelectSlides[htSelectSlides(SelectSlideVideosInSongs)].SetValues<EOffOn>((int)CConfig.VideosInSongs);
            SelectSlides[htSelectSlides(SelectSlideVideosToBackground)].SetValues<EOffOn>((int)CConfig.VideosToBackground);
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
            SelectSlides[htSelectSlides(SelectSlideVideosToBackground)].Selection = (int)CConfig.VideosToBackground;
            return true;
        }

        public override bool Draw()
        {
            base.Draw();
            return true;
        }

        private void SaveConfig()
        {
            CConfig.VideoBackgrounds = (EOffOn)SelectSlides[htSelectSlides(SelectSlideVideoBackgrounds)].Selection;
            CConfig.VideoPreview = (EOffOn)SelectSlides[htSelectSlides(SelectSlideVideoPreview)].Selection;
            CConfig.VideosInSongs = (EOffOn)SelectSlides[htSelectSlides(SelectSlideVideosInSongs)].Selection;
            CConfig.VideosToBackground = (EOffOn)SelectSlides[htSelectSlides(SelectSlideVideosToBackground)].Selection;
            CBackgroundMusic.ApplyBackgroundVideo();

            CConfig.SaveConfig();
        }
    }
}
