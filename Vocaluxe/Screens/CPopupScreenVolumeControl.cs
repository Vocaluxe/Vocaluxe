using Vocaluxe.Base;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    class CPopupScreenVolumeControl : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string StaticBG = "StaticBG";

        private const string SelectSlideVolume = "SelectSlideVolume";

        public override void Init()
        {
            base.Init();

            _ThemeStatics = new string[] {StaticBG};
            _ThemeSelectSlides = new string[] {SelectSlideVolume};
        }

        public override void LoadTheme(string XmlPath)
        {
            base.LoadTheme(XmlPath);

            _ScreenArea = Statics[StaticBG].Rect;
            SelectSlides[SelectSlideVolume].AddValues(new string[]
                {"0", "5", "10", "15", "20", "25", "30", "35", "40", "45", "50", "55", "60", "65", "70", "75", "80", "85", "90", "95", "100"});
        }

        public override bool HandleInput(KeyEvent keyEvent)
        {
            UpdateSlides();
            return true;
        }

        public override bool HandleMouse(MouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);
            if (mouseEvent.LB)
            {
                SaveConfig();
                return true;
            }
            else if (mouseEvent.Wheel > 0 && CHelper.IsInBounds(_ScreenArea, mouseEvent))
            {
                if (SelectSlides[SelectSlideVolume].Selection - mouseEvent.Wheel >= 0)
                    SelectSlides[SelectSlideVolume].Selection = SelectSlides[SelectSlideVolume].Selection - mouseEvent.Wheel;
                else if (SelectSlides[SelectSlideVolume].Selection - mouseEvent.Wheel < 0)
                    SelectSlides[SelectSlideVolume].Selection = 0;
                SaveConfig();
                return true;
            }
            else if (mouseEvent.Wheel < 0 && CHelper.IsInBounds(_ScreenArea, mouseEvent))
            {
                if (SelectSlides[SelectSlideVolume].Selection - mouseEvent.Wheel < SelectSlides[SelectSlideVolume].NumValues)
                    SelectSlides[SelectSlideVolume].Selection = SelectSlides[SelectSlideVolume].Selection - mouseEvent.Wheel;
                else if (SelectSlides[SelectSlideVolume].Selection - mouseEvent.Wheel >= SelectSlides[SelectSlideVolume].NumValues)
                    SelectSlides[SelectSlideVolume].Selection = SelectSlides[SelectSlideVolume].NumValues - 1;
                SaveConfig();
                return true;
            }
            else if (mouseEvent.RB)
                return false;
            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
            UpdateSlides();
        }

        public override bool UpdateGame()
        {
            UpdateSlides();
            return true;
        }

        public override bool Draw()
        {
            if (!_Active)
                return false;

            return base.Draw();
        }

        private void SaveConfig()
        {
            switch (CGraphics.CurrentScreen)
            {
                case EScreens.ScreenSong:
                    if (CSongs.IsInCategory)
                        CConfig.PreviewMusicVolume = SelectSlides[SelectSlideVolume].Selection * 5;
                    else
                    {
                        CConfig.BackgroundMusicVolume = SelectSlides[SelectSlideVolume].Selection * 5;
                        CBackgroundMusic.ApplyVolume();
                    }
                    break;

                case EScreens.ScreenSing:
                    CConfig.GameMusicVolume = SelectSlides[SelectSlideVolume].Selection * 5;
                    break;

                default:
                    CConfig.BackgroundMusicVolume = SelectSlides[SelectSlideVolume].Selection * 5;
                    CBackgroundMusic.ApplyVolume();
                    break;
            }
            CConfig.SaveConfig();
        }

        private void UpdateSlides()
        {
            switch (CGraphics.CurrentScreen)
            {
                case EScreens.ScreenSong:
                    if (CSongs.IsInCategory)
                        SelectSlides[SelectSlideVolume].Selection = CConfig.PreviewMusicVolume / 5;
                    else
                        SelectSlides[SelectSlideVolume].Selection = CConfig.BackgroundMusicVolume / 5;
                    break;

                case EScreens.ScreenSing:
                    SelectSlides[SelectSlideVolume].Selection = CConfig.GameMusicVolume / 5;
                    break;

                default:
                    SelectSlides[SelectSlideVolume].Selection = CConfig.BackgroundMusicVolume / 5;
                    break;
            }
        }
    }
}