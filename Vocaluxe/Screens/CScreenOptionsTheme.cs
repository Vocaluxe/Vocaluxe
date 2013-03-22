using System;
using System.Windows.Forms;

using Vocaluxe.Base;
using VocaluxeLib.Menu;

namespace Vocaluxe.Screens
{
    class CScreenOptionsTheme : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion { get { return 3; } }

        private const string SelectSlideTheme = "SelectSlideTheme";
        private const string SelectSlideSkin = "SelectSlideSkin";
        private const string SelectSlideCover = "SelectSlideCover";
        private const string SelectSlideNoteLines = "SelectSlideNoteLines";
        private const string SelectSlideToneHelper = "SelectSlideToneHelper";
        private const string SelectSlideTimerLook = "SelectSlideTimerLook";
        private const string SelectSlideFadeInfo = "SelectSlideFadeInfo";
        private const string SelectSlideCoverLoading = "SelectSlideCoverLoading";

        private const string ButtonExit = "ButtonExit";

        private int _OldCoverTheme;
        private int _OldTheme;
        private int _OldSkin;

        private int _TempSkin;

        public CScreenOptionsTheme()
        {
        }

        public override void Init()
        {
            base.Init();

            _ThemeButtons = new string[] { ButtonExit };
            _ThemeSelectSlides = new string[] {
                SelectSlideTheme,
                SelectSlideSkin,
                SelectSlideCover,
                SelectSlideNoteLines,
                SelectSlideToneHelper,
                SelectSlideTimerLook,
                SelectSlideFadeInfo,
                SelectSlideCoverLoading
            };
        }

        public override void LoadTheme(string XmlPath)
        {
            base.LoadTheme(XmlPath);

            SelectSlides[SelectSlideTheme].AddValues(CTheme.ThemeNames);
            SelectSlides[SelectSlideTheme].Selection = CTheme.GetThemeIndex(-1);

            SelectSlides[SelectSlideSkin].AddValues(CTheme.SkinNames);
            SelectSlides[SelectSlideSkin].Selection = CTheme.GetSkinIndex(-1);

            SelectSlides[SelectSlideCover].AddValues(CCover.CoverThemes);
            SelectSlides[SelectSlideCover].Selection = CCover.GetCoverThemeIndex();
            SelectSlides[SelectSlideNoteLines].SetValues<EOffOn>((int)CConfig.DrawNoteLines);
            SelectSlides[SelectSlideToneHelper].SetValues<EOffOn>((int)CConfig.DrawToneHelper);
            SelectSlides[SelectSlideTimerLook].SetValues<ETimerLook>((int)CConfig.TimerLook);
            SelectSlides[SelectSlideFadeInfo].SetValues<EFadePlayerInfo>((int)CConfig.FadePlayerInfo);
            SelectSlides[SelectSlideCoverLoading].SetValues<ECoverLoading>((int)CConfig.CoverLoading);
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
                        Close();
                        break;

                    case Keys.S:
                        CParty.SetNormalGameMode();
                        CGraphics.FadeTo(EScreens.ScreenSong);
                        break;

                    case Keys.Enter:
                        if (Buttons[ButtonExit].Selected)
                        {
                            Close();
                        }
                        break;

                    case Keys.Left:
                        OnChange();
                        break;

                    case Keys.Right:
                        OnChange();
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
                Close();
            }

            if (MouseEvent.LB && IsMouseOver(MouseEvent))
            {
                if (Buttons[ButtonExit].Selected)
                {
                    Close();
                }
                else
                    OnChange();
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

        private void Close()
        {
            SaveConfig();
            CGraphics.FadeTo(EScreens.ScreenOptions);
        }

        private void SaveConfig()
        {
            CConfig.Theme = CTheme.ThemeNames[SelectSlides[SelectSlideTheme].Selection];
            CConfig.Skin = CTheme.SkinNames[SelectSlides[SelectSlideSkin].Selection];
            CConfig.CoverTheme = CCover.CoverThemes[SelectSlides[SelectSlideCover].Selection];
            CConfig.DrawNoteLines = (EOffOn)SelectSlides[SelectSlideNoteLines].Selection;
            CConfig.DrawToneHelper = (EOffOn)SelectSlides[SelectSlideToneHelper].Selection;
            CConfig.TimerLook = (ETimerLook)SelectSlides[SelectSlideTimerLook].Selection;
            CConfig.FadePlayerInfo = (EFadePlayerInfo)SelectSlides[SelectSlideFadeInfo].Selection;
            CConfig.CoverLoading = (ECoverLoading)SelectSlides[SelectSlideCoverLoading].Selection;

            CConfig.SaveConfig();

            if (_OldCoverTheme != SelectSlides[SelectSlideCover].Selection)
            {
                CCover.ReloadCover();
                CSongs.Filter.SearchString = String.Empty;
                CSongs.Sorter.SetOptions(CConfig.SongSorting, CConfig.IgnoreArticles);
                CSongs.Categorizer.Tabs = CConfig.Tabs;
            }

            if (_OldTheme != SelectSlides[SelectSlideTheme].Selection)
            {
                CConfig.Theme = CTheme.ThemeNames[SelectSlides[SelectSlideTheme].Selection];
                _OldTheme = SelectSlides[SelectSlideTheme].Selection;

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
                return;
            }
        }

        private void OnChange()
        {
            if (_OldTheme != SelectSlides[SelectSlideTheme].Selection)
            {
                CConfig.Theme = CTheme.ThemeNames[SelectSlides[SelectSlideTheme].Selection];
                _OldTheme = SelectSlides[SelectSlideTheme].Selection;

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

            if (_TempSkin != SelectSlides[SelectSlideSkin].Selection)
            {
                _TempSkin = SelectSlides[SelectSlideSkin].Selection;

                PauseBG();
                CConfig.Skin = CTheme.SkinNames[_TempSkin];
                CGraphics.ReloadSkin();
                ResumeBG();
            }
        }

        private void AfterSkinReload()
        {
            _Active = true;
        }
    }
}
