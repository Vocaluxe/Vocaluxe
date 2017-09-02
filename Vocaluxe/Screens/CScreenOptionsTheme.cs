#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Windows.Forms;
using Vocaluxe.Base;
using Vocaluxe.Base.ThemeSystem;
using VocaluxeLib;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    public class CScreenOptionsTheme : CMenu
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

        private string _OldCoverTheme;

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

            _SelectSlides[_SelectSlideCover].AddValues(CCover.CoverThemes);
            _SelectSlides[_SelectSlideCover].Selection = CCover.GetCoverThemeIndex();
            _SelectSlides[_SelectSlideNoteLines].SetValues<EOffOn>((int)CConfig.Config.Theme.DrawNoteLines);
            _SelectSlides[_SelectSlideToneHelper].SetValues<EOffOn>((int)CConfig.Config.Theme.DrawToneHelper);
            _SelectSlides[_SelectSlideTimerLook].SetValues<ETimerLook>((int)CConfig.Config.Theme.TimerLook);
            _SelectSlides[_SelectSlideFadeInfo].SetValues<EFadePlayerInfo>((int)CConfig.Config.Theme.FadePlayerInfo);
            _SelectSlides[_SelectSlideCoverLoading].SetValues<ECoverLoading>((int)CConfig.Config.Theme.CoverLoading);
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
                        _SaveConfig();
                        CGraphics.FadeTo(EScreen.Song);
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

            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
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

            _ResetSlides();

            _OldCoverTheme = CConfig.Config.Theme.CoverTheme;
        }

        private void _ResetSlides()
        {
            _SelectSlides[_SelectSlideTheme].Clear();
            _SelectSlides[_SelectSlideTheme].AddValues(CThemes.ThemeNames);
            _SelectSlides[_SelectSlideTheme].Selection = Array.IndexOf(CThemes.ThemeNames, CConfig.Config.Theme.Theme);

            _SelectSlides[_SelectSlideSkin].Clear();
            _SelectSlides[_SelectSlideSkin].AddValues(CThemes.SkinNames);
            _SelectSlides[_SelectSlideSkin].Selection = Array.IndexOf(CThemes.SkinNames, CConfig.Config.Theme.Skin);
        }

        public override bool UpdateGame()
        {
            return true;
        }

        private void _Close()
        {
            _SaveConfig();
            CGraphics.FadeTo(EScreen.Options);
        }

        private void _SaveConfig()
        {
            CConfig.Config.Theme.CoverTheme = CCover.CoverThemes[_SelectSlides[_SelectSlideCover].Selection];
            CConfig.Config.Theme.DrawNoteLines = (EOffOn)_SelectSlides[_SelectSlideNoteLines].Selection;
            CConfig.Config.Theme.DrawToneHelper = (EOffOn)_SelectSlides[_SelectSlideToneHelper].Selection;
            CConfig.Config.Theme.TimerLook = (ETimerLook)_SelectSlides[_SelectSlideTimerLook].Selection;
            CConfig.Config.Theme.FadePlayerInfo = (EFadePlayerInfo)_SelectSlides[_SelectSlideFadeInfo].Selection;
            CConfig.Config.Theme.CoverLoading = (ECoverLoading)_SelectSlides[_SelectSlideCoverLoading].Selection;

            CConfig.SaveConfig();

            if (_OldCoverTheme != _SelectSlides[_SelectSlideCover].SelectedValue)
                CCover.ReloadCovers();
        }

        private void _OnChange()
        {
            if (CConfig.Config.Theme.Theme != _SelectSlides[_SelectSlideTheme].SelectedValue)
            {
                CConfig.Config.Theme.Theme = _SelectSlides[_SelectSlideTheme].SelectedValue;

                CThemes.Reload();
                CGraphics.ReloadTheme();
                _ResetSlides();
                _ResumeBG();
                return;
            }

            if (CConfig.Config.Theme.Skin != _SelectSlides[_SelectSlideSkin].SelectedValue)
            {
                CConfig.Config.Theme.Skin = _SelectSlides[_SelectSlideSkin].SelectedValue;

                CThemes.ReloadSkin();
                CGraphics.ReloadSkin();
                _ResetSlides();
                _ResumeBG();
            }
        }
    }
}