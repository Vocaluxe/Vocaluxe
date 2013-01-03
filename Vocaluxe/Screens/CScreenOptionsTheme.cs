using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Menu;


namespace Vocaluxe.Screens
{
    class CScreenOptionsTheme : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 3;

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

        protected override void Init()
        {
            base.Init();

            _ThemeName = "ScreenOptionsTheme";
            _ScreenVersion = ScreenVersion;
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

            SelectSlides[htSelectSlides(SelectSlideTheme)].AddValues(CTheme.ThemeNames);
            SelectSlides[htSelectSlides(SelectSlideTheme)].Selection = CTheme.GetThemeIndex(-1);

            SelectSlides[htSelectSlides(SelectSlideSkin)].AddValues(CTheme.SkinNames);
            SelectSlides[htSelectSlides(SelectSlideSkin)].Selection = CTheme.GetSkinIndex(-1);

            SelectSlides[htSelectSlides(SelectSlideCover)].AddValues(CCover.CoverThemes);
            SelectSlides[htSelectSlides(SelectSlideCover)].Selection = CCover.GetCoverThemeIndex();
            SelectSlides[htSelectSlides(SelectSlideNoteLines)].SetValues<EOffOn>((int)CConfig.DrawNoteLines);
            SelectSlides[htSelectSlides(SelectSlideToneHelper)].SetValues<EOffOn>((int)CConfig.DrawToneHelper);
            SelectSlides[htSelectSlides(SelectSlideTimerLook)].SetValues<ETimerLook>((int)CConfig.TimerLook);
            SelectSlides[htSelectSlides(SelectSlideFadeInfo)].SetValues<EFadePlayerInfo>((int)CConfig.FadePlayerInfo);
            SelectSlides[htSelectSlides(SelectSlideCoverLoading)].SetValues<ECoverLoading>((int)CConfig.CoverLoading);
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
                        if (Buttons[htButtons(ButtonExit)].Selected)
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
                if (Buttons[htButtons(ButtonExit)].Selected)
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
            CConfig.Theme = CTheme.ThemeNames[SelectSlides[htSelectSlides(SelectSlideTheme)].Selection];
            CConfig.Skin = CTheme.SkinNames[SelectSlides[htSelectSlides(SelectSlideSkin)].Selection];
            CConfig.CoverTheme = CCover.CoverThemes[SelectSlides[htSelectSlides(SelectSlideCover)].Selection];
            CConfig.DrawNoteLines = (EOffOn)SelectSlides[htSelectSlides(SelectSlideNoteLines)].Selection;
            CConfig.DrawToneHelper = (EOffOn)SelectSlides[htSelectSlides(SelectSlideToneHelper)].Selection;
            CConfig.TimerLook = (ETimerLook)SelectSlides[htSelectSlides(SelectSlideTimerLook)].Selection;
            CConfig.FadePlayerInfo = (EFadePlayerInfo)SelectSlides[htSelectSlides(SelectSlideFadeInfo)].Selection;
            CConfig.CoverLoading = (ECoverLoading)SelectSlides[htSelectSlides(SelectSlideCoverLoading)].Selection;

            CConfig.SaveConfig();

            if (_OldCoverTheme != SelectSlides[htSelectSlides(SelectSlideCover)].Selection)
            {
                CCover.ReloadCover();
                CSongs.Sort(CConfig.SongSorting, CConfig.Tabs, CConfig.IgnoreArticles, String.Empty);
            }

            if (_OldTheme != SelectSlides[htSelectSlides(SelectSlideTheme)].Selection)
            {
                CConfig.Theme = CTheme.ThemeNames[SelectSlides[htSelectSlides(SelectSlideTheme)].Selection];
                _OldTheme = SelectSlides[htSelectSlides(SelectSlideTheme)].Selection;

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
            if (_OldTheme != SelectSlides[htSelectSlides(SelectSlideTheme)].Selection)
            {
                CConfig.Theme = CTheme.ThemeNames[SelectSlides[htSelectSlides(SelectSlideTheme)].Selection];
                _OldTheme = SelectSlides[htSelectSlides(SelectSlideTheme)].Selection;

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

            if (_TempSkin != SelectSlides[htSelectSlides(SelectSlideSkin)].Selection)
            {
                _TempSkin = SelectSlides[htSelectSlides(SelectSlideSkin)].Selection;

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
