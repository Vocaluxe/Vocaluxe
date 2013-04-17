﻿using Vocaluxe.Base;
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

        private const string _StaticBG = "StaticBG";

        private const string _SelectSlideVolume = "SelectSlideVolume";

        public override void Init()
        {
            base.Init();

            _ThemeStatics = new string[] {_StaticBG};
            _ThemeSelectSlides = new string[] {_SelectSlideVolume};
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _ScreenArea = Statics[_StaticBG].Rect;
            SelectSlides[_SelectSlideVolume].AddValues(new string[]
                {"0", "5", "10", "15", "20", "25", "30", "35", "40", "45", "50", "55", "60", "65", "70", "75", "80", "85", "90", "95", "100"});
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            _UpdateSlides();
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);
            if (mouseEvent.LB)
            {
                _SaveConfig();
                return true;
            }
            else if (mouseEvent.Wheel > 0 && CHelper.IsInBounds(_ScreenArea, mouseEvent))
            {
                if (SelectSlides[_SelectSlideVolume].Selection - mouseEvent.Wheel >= 0)
                    SelectSlides[_SelectSlideVolume].Selection = SelectSlides[_SelectSlideVolume].Selection - mouseEvent.Wheel;
                else if (SelectSlides[_SelectSlideVolume].Selection - mouseEvent.Wheel < 0)
                    SelectSlides[_SelectSlideVolume].Selection = 0;
                _SaveConfig();
                return true;
            }
            else if (mouseEvent.Wheel < 0 && CHelper.IsInBounds(_ScreenArea, mouseEvent))
            {
                if (SelectSlides[_SelectSlideVolume].Selection - mouseEvent.Wheel < SelectSlides[_SelectSlideVolume].NumValues)
                    SelectSlides[_SelectSlideVolume].Selection = SelectSlides[_SelectSlideVolume].Selection - mouseEvent.Wheel;
                else if (SelectSlides[_SelectSlideVolume].Selection - mouseEvent.Wheel >= SelectSlides[_SelectSlideVolume].NumValues)
                    SelectSlides[_SelectSlideVolume].Selection = SelectSlides[_SelectSlideVolume].NumValues - 1;
                _SaveConfig();
                return true;
            }
            else if (mouseEvent.RB)
                return false;
            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
            _UpdateSlides();
        }

        public override bool UpdateGame()
        {
            _UpdateSlides();
            return true;
        }

        public override bool Draw()
        {
            if (!_Active)
                return false;

            return base.Draw();
        }

        private void _SaveConfig()
        {
            switch (CGraphics.CurrentScreen)
            {
                case EScreens.ScreenSong:
                    if (CSongs.IsInCategory)
                        CConfig.PreviewMusicVolume = SelectSlides[_SelectSlideVolume].Selection * 5;
                    else
                    {
                        CConfig.BackgroundMusicVolume = SelectSlides[_SelectSlideVolume].Selection * 5;
                        CBackgroundMusic.ApplyVolume();
                    }
                    break;

                case EScreens.ScreenSing:
                    CConfig.GameMusicVolume = SelectSlides[_SelectSlideVolume].Selection * 5;
                    break;

                default:
                    CConfig.BackgroundMusicVolume = SelectSlides[_SelectSlideVolume].Selection * 5;
                    CBackgroundMusic.ApplyVolume();
                    break;
            }
            CConfig.SaveConfig();
        }

        private void _UpdateSlides()
        {
            switch (CGraphics.CurrentScreen)
            {
                case EScreens.ScreenSong:
                    if (CSongs.IsInCategory)
                        SelectSlides[_SelectSlideVolume].Selection = CConfig.PreviewMusicVolume / 5;
                    else
                        SelectSlides[_SelectSlideVolume].Selection = CConfig.BackgroundMusicVolume / 5;
                    break;

                case EScreens.ScreenSing:
                    SelectSlides[_SelectSlideVolume].Selection = CConfig.GameMusicVolume / 5;
                    break;

                default:
                    SelectSlides[_SelectSlideVolume].Selection = CConfig.BackgroundMusicVolume / 5;
                    break;
            }
        }
    }
}