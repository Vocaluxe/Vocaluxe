#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

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

            _ScreenArea = _Statics[_StaticBG].Rect;
            _SelectSlides[_SelectSlideVolume].AddValues(new string[]
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
            if (mouseEvent.Wheel > 0 && CHelper.IsInBounds(_ScreenArea, mouseEvent))
            {
                if (_SelectSlides[_SelectSlideVolume].Selection - mouseEvent.Wheel >= 0)
                    _SelectSlides[_SelectSlideVolume].Selection = _SelectSlides[_SelectSlideVolume].Selection - mouseEvent.Wheel;
                else if (_SelectSlides[_SelectSlideVolume].Selection - mouseEvent.Wheel < 0)
                    _SelectSlides[_SelectSlideVolume].Selection = 0;
                _SaveConfig();
                return true;
            }
            if (mouseEvent.Wheel < 0 && CHelper.IsInBounds(_ScreenArea, mouseEvent))
            {
                if (_SelectSlides[_SelectSlideVolume].Selection - mouseEvent.Wheel < _SelectSlides[_SelectSlideVolume].NumValues)
                    _SelectSlides[_SelectSlideVolume].Selection = _SelectSlides[_SelectSlideVolume].Selection - mouseEvent.Wheel;
                else if (_SelectSlides[_SelectSlideVolume].Selection - mouseEvent.Wheel >= _SelectSlides[_SelectSlideVolume].NumValues)
                    _SelectSlides[_SelectSlideVolume].Selection = _SelectSlides[_SelectSlideVolume].NumValues - 1;
                _SaveConfig();
                return true;
            }
            return !mouseEvent.RB;
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
                        CConfig.PreviewMusicVolume = _SelectSlides[_SelectSlideVolume].Selection * 5;
                    else
                    {
                        CConfig.BackgroundMusicVolume = _SelectSlides[_SelectSlideVolume].Selection * 5;
                        CBackgroundMusic.ApplyVolume();
                    }
                    break;

                case EScreens.ScreenSing:
                    CConfig.GameMusicVolume = _SelectSlides[_SelectSlideVolume].Selection * 5;
                    break;

                default:
                    CConfig.BackgroundMusicVolume = _SelectSlides[_SelectSlideVolume].Selection * 5;
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
                        _SelectSlides[_SelectSlideVolume].Selection = CConfig.PreviewMusicVolume / 5;
                    else
                        _SelectSlides[_SelectSlideVolume].Selection = CConfig.BackgroundMusicVolume / 5;
                    break;

                case EScreens.ScreenSing:
                    _SelectSlides[_SelectSlideVolume].Selection = CConfig.GameMusicVolume / 5;
                    break;

                default:
                    _SelectSlides[_SelectSlideVolume].Selection = CConfig.BackgroundMusicVolume / 5;
                    break;
            }
        }
    }
}