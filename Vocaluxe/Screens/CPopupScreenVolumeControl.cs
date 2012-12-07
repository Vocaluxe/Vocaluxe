﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.GameModes;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;
using Vocaluxe.Lib.Song;

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
            Init();
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "PopupScreenVolumeControl";
            _ScreenVersion = ScreenVersion;

            _ThemeStatics = new string[] { StaticBG };
            _ThemeSelectSlides = new string[] { SelectSlideVolume };

        }

        public override void LoadTheme()
        {
            base.LoadTheme();

            _ScreenArea = Statics[htStatics(StaticBG)].Rect;
            SelectSlides[htSelectSlides(SelectSlideVolume)].AddValues(new string[] { "0", "5", "10", "15", "20", "25", "30", "35", "40", "45", "50", "55", "60", "65", "70", "75", "80", "85", "90", "95", "100" });
        }

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            base.HandleInput(KeyEvent);
            if (KeyEvent.KeyPressed && !Char.IsControl(KeyEvent.Unicode))
            {

            }
            else
            {
                switch (KeyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                        CGraphics.HidePopup(EPopupScreens.PopupVolumeControl);
                        return false;
                }
            }

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
                    SelectSlides[htSelectSlides(SelectSlideVolume)].SetSelectionByValueIndex(SelectSlides[htSelectSlides(SelectSlideVolume)].Selection - MouseEvent.Wheel);
                else if (SelectSlides[htSelectSlides(SelectSlideVolume)].Selection - MouseEvent.Wheel < 0)
                    SelectSlides[htSelectSlides(SelectSlideVolume)].SetSelectionByValueIndex(0);
                SaveConfig();
                return true;
            }
            else if (MouseEvent.Wheel < 0 && CHelper.IsInBounds(_ScreenArea, MouseEvent))
            {
                if (SelectSlides[htSelectSlides(SelectSlideVolume)].Selection - MouseEvent.Wheel < SelectSlides[htSelectSlides(SelectSlideVolume)].NumValues)
                    SelectSlides[htSelectSlides(SelectSlideVolume)].SetSelectionByValueIndex(SelectSlides[htSelectSlides(SelectSlideVolume)].Selection - MouseEvent.Wheel);
                else if (SelectSlides[htSelectSlides(SelectSlideVolume)].Selection - MouseEvent.Wheel >= SelectSlides[htSelectSlides(SelectSlideVolume)].NumValues)
                    SelectSlides[htSelectSlides(SelectSlideVolume)].SetSelectionByValueIndex(SelectSlides[htSelectSlides(SelectSlideVolume)].NumValues - 1);
                SaveConfig();
                return true;
            }
            else if (MouseEvent.RB)
            {
                //CGraphics.HidePopup(EPopupScreens.PopupPlayerControl);
                return false;
            }
            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            switch (CGraphics.CurrentScreen)
            {
                case EScreens.ScreenSong:
                    SelectSlides[htSelectSlides(SelectSlideVolume)].SetSelectionByValueIndex((int)CConfig.PreviewMusicVolume / 5);
                    break;

                case EScreens.ScreenSing:
                    SelectSlides[htSelectSlides(SelectSlideVolume)].SetSelectionByValueIndex((int)CConfig.GameMusicVolume / 5);
                    break;

                default:
                    SelectSlides[htSelectSlides(SelectSlideVolume)].SetSelectionByValueIndex((int)CConfig.BackgroundMusicVolume / 5);
                    break;
            }
        }

        public override bool UpdateGame()
        {
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
                    CConfig.PreviewMusicVolume = SelectSlides[htSelectSlides(SelectSlideVolume)].Selection * 5;               
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
    }
}
