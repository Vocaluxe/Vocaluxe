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

using System;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    class CScreenOptionsTheme : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 3; }
        }

        private const string _SelectSlideTheme = "SelectSlideTheme";
        private const string _SelectSlideSkin = "SelectSlideSkin";
        private const string _SelectSlideCover = "SelectSlideCover";
        private const string _SelectSlideNoteLines = "SelectSlideNoteLines";
        private const string _SelectSlideToneHelper = "SelectSlideToneHelper";
        private const string _SelectSlideTimerLook = "SelectSlideTimerLook";
        private const string _SelectSlideFadeInfo = "SelectSlideFadeInfo";
        private const string _SelectSlideCoverLoading = "SelectSlideCoverLoading";

        private const string _ButtonExit = "ButtonExit";

        private int _OldCoverTheme;
        private int _OldTheme;
        private int _OldSkin;

        private int _TempSkin;

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[] {_ButtonExit};
            _ThemeSelectSlides = new string[]
                {
                    _SelectSlideTheme,
                    _SelectSlideSkin,
                    _SelectSlideCover,
                    _SelectSlideNoteLines,
                    _SelectSlideToneHelper,
                    _SelectSlideTimerLook,
                    _SelectSlideFadeInfo,
                    _SelectSlideCoverLoading
                };
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _SelectSlides[_SelectSlideTheme].AddValues(CTheme.ThemeNames);
            _SelectSlides[_SelectSlideTheme].Selection = CTheme.GetThemeIndex(-1);

            _SelectSlides[_SelectSlideSkin].AddValues(CTheme.SkinNames);
            _SelectSlides[_SelectSlideSkin].Selection = CTheme.GetSkinIndex(-1);

            _SelectSlides[_SelectSlideCover].AddValues(CCover.CoverThemes);
            _SelectSlides[_SelectSlideCover].Selection = CCover.GetCoverThemeIndex();
            _SelectSlides[_SelectSlideNoteLines].SetValues<EOffOn>((int)CConfig.DrawNoteLines);
            _SelectSlides[_SelectSlideToneHelper].SetValues<EOffOn>((int)CConfig.DrawToneHelper);
            _SelectSlides[_SelectSlideTimerLook].SetValues<ETimerLook>((int)CConfig.TimerLook);
            _SelectSlides[_SelectSlideFadeInfo].SetValues<EFadePlayerInfo>((int)CConfig.FadePlayerInfo);
            _SelectSlides[_SelectSlideCoverLoading].SetValues<ECoverLoading>((int)CConfig.CoverLoading);
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
                        _Close();
                        break;

                    case Keys.S:
                        CParty.SetNormalGameMode();
                        CGraphics.FadeTo(EScreens.ScreenSong);
                        break;

                    case Keys.Enter:
                        if (_Buttons[_ButtonExit].Selected)
                            _Close();
                        break;

                    case Keys.Left:
                        _OnChange();
                        break;

                    case Keys.Right:
                        _OnChange();
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.RB)
                _Close();

            if (mouseEvent.LB && _IsMouseOver(mouseEvent))
            {
                if (_Buttons[_ButtonExit].Selected)
                    _Close();
                else
                    _OnChange();
            }
            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            _OldCoverTheme = CCover.GetCoverThemeIndex();
            _OldTheme = CTheme.GetThemeIndex(-1);
            _OldSkin = CTheme.GetSkinIndex(-1);
            _TempSkin = _OldSkin;
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

        private void _Close()
        {
            _SaveConfig();
            CGraphics.FadeTo(EScreens.ScreenOptions);
        }

        private void _SaveConfig()
        {
            CConfig.Theme = CTheme.ThemeNames[_SelectSlides[_SelectSlideTheme].Selection];
            CConfig.Skin = CTheme.SkinNames[_SelectSlides[_SelectSlideSkin].Selection];
            CConfig.CoverTheme = CCover.CoverThemes[_SelectSlides[_SelectSlideCover].Selection];
            CConfig.DrawNoteLines = (EOffOn)_SelectSlides[_SelectSlideNoteLines].Selection;
            CConfig.DrawToneHelper = (EOffOn)_SelectSlides[_SelectSlideToneHelper].Selection;
            CConfig.TimerLook = (ETimerLook)_SelectSlides[_SelectSlideTimerLook].Selection;
            CConfig.FadePlayerInfo = (EFadePlayerInfo)_SelectSlides[_SelectSlideFadeInfo].Selection;
            CConfig.CoverLoading = (ECoverLoading)_SelectSlides[_SelectSlideCoverLoading].Selection;

            CConfig.SaveConfig();

            if (_OldCoverTheme != _SelectSlides[_SelectSlideCover].Selection)
            {
                CCover.ReloadCover();
                CSongs.Filter.SearchString = String.Empty;
                CSongs.Sorter.SetOptions(CConfig.SongSorting, CConfig.IgnoreArticles);
                CSongs.Categorizer.Tabs = CConfig.Tabs;
            }

            if (_OldTheme != _SelectSlides[_SelectSlideTheme].Selection)
            {
                CConfig.Theme = CTheme.ThemeNames[_SelectSlides[_SelectSlideTheme].Selection];
                _OldTheme = _SelectSlides[_SelectSlideTheme].Selection;

                CTheme.UnloadSkins();
                CFonts.UnloadThemeFonts(CConfig.Theme);
                CTheme.ListSkins();
                CConfig.Skin = CTheme.SkinNames[0];
                _OldSkin = 0;
                _TempSkin = _OldSkin;

                CConfig.SaveConfig();

                CTheme.LoadSkins();
                CTheme.LoadTheme();
                CGraphics.ReloadTheme();
            }
        }

        private void _OnChange()
        {
            if (_OldTheme != _SelectSlides[_SelectSlideTheme].Selection)
            {
                CConfig.Theme = CTheme.ThemeNames[_SelectSlides[_SelectSlideTheme].Selection];
                _OldTheme = _SelectSlides[_SelectSlideTheme].Selection;

                CTheme.UnloadSkins();
                CFonts.UnloadThemeFonts(CConfig.Theme);
                CTheme.ListSkins();
                CConfig.Skin = CTheme.SkinNames[0];
                _OldSkin = 0;
                _TempSkin = _OldSkin;

                CConfig.SaveConfig();

                CTheme.LoadSkins();
                CTheme.LoadTheme();
                CGraphics.ReloadTheme();

                OnShow();
                OnShowFinish();
                return;
            }

            if (_TempSkin != _SelectSlides[_SelectSlideSkin].Selection)
            {
                _TempSkin = _SelectSlides[_SelectSlideSkin].Selection;

                _PauseBG();
                CConfig.Skin = CTheme.SkinNames[_TempSkin];
                CGraphics.ReloadSkin();
                _ResumeBG();
            }
        }
    }
}