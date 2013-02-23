using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.GameModes;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;
using Vocaluxe.Menu.SongMenu;

namespace Vocaluxe.Screens
{
    class CPopupScreenVolumeControl : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        private const string StaticBG = "StaticBG";

        private const string SelectSlideVolume = "SelectSlideVolume";


        public CPopupScreenVolumeControl()
        {
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "PopupScreenVolumeControl";
            _ScreenVersion = ScreenVersion;

            _ThemeStatics = new string[] { StaticBG };
            _ThemeSelectSlides = new string[] { SelectSlideVolume };

        }

        public override void LoadTheme(string XmlPath)
        {
            base.LoadTheme(XmlPath);

            _ScreenArea = Statics[htStatics(StaticBG)].Rect;
            SelectSlides[htSelectSlides(SelectSlideVolume)].AddValues(new string[] { "0", "5", "10", "15", "20", "25", "30", "35", "40", "45", "50", "55", "60", "65", "70", "75", "80", "85", "90", "95", "100" });
        }

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            UpdateSlides();
            return true;
        }

        public override bool HandleMouse(MouseEvent MouseEvent)
        {
            base.HandleMouse(MouseEvent);
            if (MouseEvent.LB)
            {
                SaveConfig();
                return true;
            }
            else if (MouseEvent.Wheel > 0 && CHelper.IsInBounds(_ScreenArea, MouseEvent))
            {
                if (SelectSlides[htSelectSlides(SelectSlideVolume)].Selection - MouseEvent.Wheel >= 0)
                    SelectSlides[htSelectSlides(SelectSlideVolume)].Selection = SelectSlides[htSelectSlides(SelectSlideVolume)].Selection - MouseEvent.Wheel;
                else if (SelectSlides[htSelectSlides(SelectSlideVolume)].Selection - MouseEvent.Wheel < 0)
                    SelectSlides[htSelectSlides(SelectSlideVolume)].Selection = 0;
                SaveConfig();
                return true;
            }
            else if (MouseEvent.Wheel < 0 && CHelper.IsInBounds(_ScreenArea, MouseEvent))
            {
                if (SelectSlides[htSelectSlides(SelectSlideVolume)].Selection - MouseEvent.Wheel < SelectSlides[htSelectSlides(SelectSlideVolume)].NumValues)
                    SelectSlides[htSelectSlides(SelectSlideVolume)].Selection = SelectSlides[htSelectSlides(SelectSlideVolume)].Selection - MouseEvent.Wheel;
                else if (SelectSlides[htSelectSlides(SelectSlideVolume)].Selection - MouseEvent.Wheel >= SelectSlides[htSelectSlides(SelectSlideVolume)].NumValues)
                    SelectSlides[htSelectSlides(SelectSlideVolume)].Selection = SelectSlides[htSelectSlides(SelectSlideVolume)].NumValues - 1;
                SaveConfig();
                return true;
            }
            else if (MouseEvent.RB)
            {
                return false;
            }
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
                    if (CSongs.Category != -1)
                        CConfig.PreviewMusicVolume = SelectSlides[htSelectSlides(SelectSlideVolume)].Selection * 5;
                    else
                    {
                        CConfig.BackgroundMusicVolume = SelectSlides[htSelectSlides(SelectSlideVolume)].Selection * 5;
                        CBackgroundMusic.ApplyVolume();
                    }
                    break;

                case EScreens.ScreenSing:
                    CConfig.GameMusicVolume = SelectSlides[htSelectSlides(SelectSlideVolume)].Selection * 5;
                    break;

                default:
                    CConfig.BackgroundMusicVolume = SelectSlides[htSelectSlides(SelectSlideVolume)].Selection * 5;
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
                    if(CSongs.Category != -1)
                        SelectSlides[htSelectSlides(SelectSlideVolume)].Selection = (int)(CConfig.PreviewMusicVolume / 5);
                    else
                        SelectSlides[htSelectSlides(SelectSlideVolume)].Selection = ((int)CConfig.BackgroundMusicVolume / 5);
                    break;

                case EScreens.ScreenSing:
                    SelectSlides[htSelectSlides(SelectSlideVolume)].Selection = ((int)CConfig.GameMusicVolume / 5);
                    break;

                default:
                    SelectSlides[htSelectSlides(SelectSlideVolume)].Selection = ((int)CConfig.BackgroundMusicVolume / 5);
                    break;
            }
        }
    }
}
