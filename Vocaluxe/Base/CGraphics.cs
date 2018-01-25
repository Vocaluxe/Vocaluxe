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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Vocaluxe.Base.Fonts;
using Vocaluxe.Base.ThemeSystem;
using Vocaluxe.Screens;
using VocaluxeLib;
using VocaluxeLib.Log;
using VocaluxeLib.Menu;

namespace Vocaluxe.Base
{
    static class CGraphics
    {
        private static CFading _Fading;
        private static readonly CCursor _Cursor = new CCursor();

        private static EPopupScreens _CurrentPopupScreen;

        private static readonly List<IMenu> _Screens = new List<IMenu>();
        private static readonly List<IMenu> _PopupScreens = new List<IMenu>();

        private static Stopwatch _VolumePopupTimer;
        private static bool _CursorOverVolumeControl;

        public static float GlobalAlpha { get; private set; }

        public static float ZOffset { get; private set; }

        public static IMenu CurrentScreen { get; private set; }

        public static IMenu NextScreen { get; private set; }
        public static EScreen NextScreenType
        {
            get
            {
                int id = _Screens.IndexOf(NextScreen);
                return (EScreen)id;
            }
        }

        #region public methods
        public static void Init()
        {
            // Add Screens, must be the same order as in EScreens!
            using (CBenchmark.Time("Build Screen List"))
            {
                _Screens.Add(new CScreenTest());
                _Screens.Add(new CScreenLoad());
                _Screens.Add(new CScreenMain());
                _Screens.Add(new CScreenSong());
                _Screens.Add(new CScreenOptions());
                _Screens.Add(new CScreenSing());
                _Screens.Add(new CScreenProfiles());
                _Screens.Add(new CScreenScore());
                _Screens.Add(new CScreenHighscore());
                _Screens.Add(new CScreenOptionsGame());
                _Screens.Add(new CScreenOptionsSound());
                _Screens.Add(new CScreenOptionsRecord());
                _Screens.Add(new CScreenOptionsVideo());
                _Screens.Add(new CScreenOptionsVideoAdjustments());
                _Screens.Add(new CScreenOptionsLyrics());
                _Screens.Add(new CScreenOptionsTheme());
                _Screens.Add(new CScreenNames());
                _Screens.Add(new CScreenCredits());
                _Screens.Add(new CScreenParty());

                Debug.Assert(_Screens.Count == (int)EScreen.CountEntry, "Screen list and screens enum do not match");

                _PopupScreens.Add(new CPopupScreenPlayerControl());
                _PopupScreens.Add(new CPopupScreenVolumeControl());
                _PopupScreens.Add(new CPopupScreenServerQR());
            }
            CurrentScreen = _Screens[(int)EScreen.Load];
            NextScreen = null;
            _CurrentPopupScreen = EPopupScreens.NoPopup;
            _VolumePopupTimer = new Stopwatch();

            GlobalAlpha = 1f;
            ZOffset = 0f;

            using (CBenchmark.Time("Load Theme"))
            {
                LoadTheme();
            }
        }

        public static void Close()
        {
            if (_CurrentPopupScreen != EPopupScreens.NoPopup && _PopupScreens.Count > 0)
                _PopupScreens[(int)_CurrentPopupScreen].OnClose();
            if (CurrentScreen != null)
                CurrentScreen.OnClose();
        }

        public static void LoadTheme()
        {
            _Cursor.LoadSkin();

            for (int i = 0; i < _Screens.Count; i++)
            {
                using (CBenchmark.Time("Load Theme " + Enum.GetNames(typeof(EScreen))[i]))
                {
                    _Screens[i].Init();
                    _Screens[i].LoadTheme(CThemes.GetThemeScreensPath(_Screens[i].PartyModeID));
                }
            }

            foreach (IMenu popup in _PopupScreens)
            {
                popup.Init();
                popup.LoadTheme(CThemes.GetThemeScreensPath(popup.PartyModeID));
            }
        }

        public static void ReloadTheme()
        {
            _Cursor.ReloadSkin();
            foreach (IMenu screen in _Screens)
                screen.ReloadTheme(CThemes.GetThemeScreensPath(screen.PartyModeID));

            foreach (IMenu popup in _PopupScreens)
                popup.ReloadTheme(CThemes.GetThemeScreensPath(popup.PartyModeID));

            CParty.ReloadTheme();
        }

        public static void ReloadSkin()
        {
            _Cursor.ReloadSkin();
            foreach (IMenu menu in _Screens)
                menu.ReloadSkin();

            foreach (IMenu menu in _PopupScreens)
                menu.ReloadSkin();

            CParty.ReloadSkin();
        }

        public static void SaveTheme()
        {
            foreach (CMenu screen in _Screens)
            {
                if (screen.ThemePath == null || screen.ThemeName == "ScreenTest")
                    continue;

                screen.SaveTheme();
            }

            foreach (IMenu popup in _PopupScreens)
                popup.SaveTheme();

            CParty.SaveThemes();
        }

        public static void InitFirstScreen()
        {
            CurrentScreen.OnShow();
            CurrentScreen.OnShowFinish();
        }

        public static bool UpdateGameLogic(CKeys keys, CMouse mouse)
        {
            bool run = true;
            _Cursor.Visible = mouse.Visible;

            mouse.CopyEvents();
            keys.CopyEvents();

            CVideo.Update();
            CSound.Update();
            CBackgroundMusic.Update();
            CController.Update();
            CProfiles.Update();

            if (CSettings.ProgramState != EProgramState.EditTheme)
            {
                run &= _HandleInputs(keys, mouse);
                run &= _Update();
                CParty.UpdateGame();
            }
            else
            {
                run &= _HandleInputThemeEditor(keys, mouse);
                run &= _Update();
            }

            return run;
        }

        public static bool Draw()
        {
            if (NextScreen != null && _Fading == null)
            {
                _Fading = new CFading(0f, 1f, CConfig.Config.Graphics.FadeTime);

                if (NextScreen.PartyModeID != -1)
                {
                    CFonts.PartyModeID = NextScreen.PartyModeID;
                    NextScreen.OnShow();
                    CFonts.PartyModeID = -1;
                }
                else
                    NextScreen.OnShow();
                if (_Cursor.IsActive)
                    NextScreen.ProcessMouseMove(_Cursor.X, _Cursor.Y);

                HidePopup(EPopupScreens.PopupPlayerControl);
                if (NextScreen.CurrentMusicType != EMusicType.Background && NextScreen.CurrentMusicType != EMusicType.Preview && NextScreen.CurrentMusicType != EMusicType.BackgroundPreview)
                    CBackgroundMusic.Disabled = true;
            }

            if (_Fading != null)
            {
                Debug.Assert(NextScreen != null);
                bool finished;
                float newAlpha = _Fading.GetValue(out finished);

                if (!finished)
                {
                    ZOffset = CSettings.ZFar / 2;
                    _DrawScreen(CurrentScreen);

                    GlobalAlpha = newAlpha;
                    ZOffset = 0f;
                    _DrawScreen(NextScreen);

                    GlobalAlpha = 1f;
                    int oldVol = CConfig.GetVolumeByType(CurrentScreen.CurrentMusicType);
                    int newVol = CConfig.GetVolumeByType(NextScreen.CurrentMusicType);
                    CSound.SetGlobalVolume((int)((newVol - oldVol) * newAlpha + oldVol));
                }
                else
                {
                    _FinishScreenFading();
                    if (_Cursor.IsActive)
                        CurrentScreen.ProcessMouseMove(_Cursor.X, _Cursor.Y);

                    _DrawScreen(CurrentScreen);
                }
            }
            else
                _DrawScreen(CurrentScreen);

            foreach (IMenu popup in _PopupScreens)
                popup.Draw();

            _Cursor.Draw();
            _DrawDebugInfos();

            return true;
        }

        private static void _FinishScreenFading()
        {
            if (_Fading == null)
                return;
            Debug.Assert(NextScreen != null);
            CurrentScreen.OnClose();
            CurrentScreen = NextScreen;
            NextScreen = null;
            CurrentScreen.OnShowFinish();
            CSound.SetGlobalVolume(CConfig.GetVolumeByType(CurrentScreen.CurrentMusicType));
            if (CurrentScreen.CurrentMusicType == EMusicType.Background || CurrentScreen.CurrentMusicType == EMusicType.Preview || CurrentScreen.CurrentMusicType == EMusicType.BackgroundPreview)
            {
                CBackgroundMusic.Disabled = false;
                CBackgroundMusic.IsPlayingPreview = CurrentScreen.CurrentMusicType == EMusicType.Preview || CurrentScreen.CurrentMusicType == EMusicType.BackgroundPreview;
            }
            _Fading = null;
        }

        private static void _DrawScreen(IMenu screen)
        {
            if (screen.PartyModeID != -1)
            {
                CFonts.PartyModeID = screen.PartyModeID;
                screen.Draw();
                CFonts.PartyModeID = -1;
            }
            else
                screen.Draw();
        }

        public static IMenu GetScreen(EScreen screen)
        {
            if (screen == EScreen.Unknown || screen == EScreen.CountEntry)
                throw new ArgumentException("Invalid screen: " + screen);
            return _Screens[(int)screen];
        }

        public static void FadeTo(EScreen screen)
        {
            FadeTo(GetScreen(screen));
        }

        public static void FadeTo(IMenu screen)
        {
            if (screen == null)
                throw new ArgumentNullException("screen");
            Debug.Assert(NextScreen == null || NextScreen != screen, "Don't fade to currently fading screen!");
            if (screen == NextScreen)
                return;
            // Make sure the last screen change is done
            _FinishScreenFading();
            NextScreen = screen;
        }

        public static void ShowPopup(EPopupScreens popupScreen)
        {
            _PopupScreens[(int)popupScreen].OnShow();
            _PopupScreens[(int)popupScreen].OnShowFinish();
            _CurrentPopupScreen = popupScreen;
        }

        public static void HidePopup(EPopupScreens popupScreen)
        {
            if (_CurrentPopupScreen != popupScreen)
                return;

            _PopupScreens[(int)popupScreen].OnClose();
            _CurrentPopupScreen = EPopupScreens.NoPopup;
        }

        public static void HideCursor()
        {
            _Cursor.ShowCursor = false;
        }

        public static void ShowCursor()
        {
            _Cursor.ShowCursor = true;
        }
        #endregion public methods

        #region private stuff
        private static bool _HandleInputs(CKeys keys, CMouse mouse)
        {
            SKeyEvent keyEvent = new SKeyEvent();
            SMouseEvent mouseEvent = new SMouseEvent();
            SKeyEvent inputKeyEvent = new SKeyEvent();
            SMouseEvent inputMouseEvent = new SMouseEvent();

            bool popupPlayerControlAllowed = CurrentScreen.CurrentMusicType == EMusicType.Background;
            bool popupVolumeControlAllowed = CurrentScreen.CurrentMusicType != EMusicType.None;
            //Hide volume control for bg-music if bg-music is disabled
            if (popupVolumeControlAllowed && (CurrentScreen.CurrentMusicType == EMusicType.Background || CurrentScreen.CurrentMusicType == EMusicType.BackgroundPreview) && CConfig.Config.Sound.BackgroundMusic == EBackgroundMusicOffOn.TR_CONFIG_OFF)
                popupVolumeControlAllowed = false;

            bool resume = true;
            bool eventsAvailable;
            bool inputEventsAvailable = CController.PollKeyEvent(ref inputKeyEvent);

            while ((eventsAvailable = keys.PollEvent(ref keyEvent)) || inputEventsAvailable)
            {
                if (!eventsAvailable)
                    keyEvent = inputKeyEvent;

                if (keyEvent.IsArrowKey() || keyEvent.Key == Keys.NumPad0 || keyEvent.Key == Keys.D0 || keyEvent.Key == Keys.Add)
                {
                    _Cursor.Deactivate();

                    if (keyEvent.ModAlt && keyEvent.ModCtrl)
                    {
                        switch (keyEvent.Key)
                        {
                            case Keys.Right:
                                if (keyEvent.ModShift)
                                    CConfig.Config.Graphics.BorderLeft++;
                                else
                                    CConfig.Config.Graphics.BorderRight--;
                                break;
                            case Keys.Left:
                                if (keyEvent.ModShift)
                                    CConfig.Config.Graphics.BorderLeft--;
                                else
                                    CConfig.Config.Graphics.BorderRight++;
                                break;
                            case Keys.Down:
                                if (keyEvent.ModShift)
                                    CConfig.Config.Graphics.BorderTop++;
                                else
                                    CConfig.Config.Graphics.BorderBottom--;
                                break;
                            case Keys.Up:
                                if (keyEvent.ModShift)
                                    CConfig.Config.Graphics.BorderTop--;
                                else
                                    CConfig.Config.Graphics.BorderBottom++;
                                break;
                            case Keys.D0:
                            case Keys.NumPad0:
                                CConfig.Config.Graphics.BorderLeft =
                                    CConfig.Config.Graphics.BorderRight = CConfig.Config.Graphics.BorderTop = CConfig.Config.Graphics.BorderBottom = 0;
                                break;
                            case Keys.Add:
                                switch (CConfig.Config.Graphics.ScreenAlignment)
                                {
                                    case EGeneralAlignment.Middle:
                                        CConfig.Config.Graphics.ScreenAlignment = EGeneralAlignment.End;
                                        break;
                                    case EGeneralAlignment.End:
                                        CConfig.Config.Graphics.ScreenAlignment = EGeneralAlignment.Start;
                                        break;
                                    default:
                                        CConfig.Config.Graphics.ScreenAlignment = EGeneralAlignment.Middle;
                                        break;
                                }
                                break;
                        }
                        CConfig.SaveConfig();
                        break;
                    }
                }

                if (keyEvent.Key == Keys.F11)
                {
                    if (_CurrentPopupScreen == EPopupScreens.NoPopup)
                        ShowPopup(EPopupScreens.PopupServerQR);
                    else
                        HidePopup(EPopupScreens.PopupServerQR);
                }

                if (keyEvent.Key == Keys.F8)
                {
                    CLog.ShowLogAssistant("", null);
                }

                if (popupPlayerControlAllowed && keyEvent.Key == Keys.Tab)
                {
                    if (_CurrentPopupScreen == EPopupScreens.NoPopup && CConfig.Config.Sound.BackgroundMusic == EBackgroundMusicOffOn.TR_CONFIG_ON)
                        ShowPopup(EPopupScreens.PopupPlayerControl);
                    else
                        HidePopup(EPopupScreens.PopupPlayerControl);
                }

                if (popupPlayerControlAllowed && CConfig.Config.Sound.BackgroundMusic != EBackgroundMusicOffOn.TR_CONFIG_OFF)
                {
                    if (keyEvent.Key == Keys.MediaNextTrack)
                        CBackgroundMusic.Next();
                    else if (keyEvent.Key == Keys.MediaPreviousTrack)
                        CBackgroundMusic.Previous();
                    else if (keyEvent.Key == Keys.MediaPlayPause)
                    {
                        if (CBackgroundMusic.IsPlaying)
                            CBackgroundMusic.Pause();
                        else
                            CBackgroundMusic.Play();
                    }
                }

                if (keyEvent.ModShift && (keyEvent.Key == Keys.F1))
                    CSettings.ProgramState = EProgramState.EditTheme;
                else if (keyEvent.ModAlt && (keyEvent.Key == Keys.Enter))
                    CConfig.Config.Graphics.FullScreen = (CConfig.Config.Graphics.FullScreen == EOffOn.TR_CONFIG_ON) ? EOffOn.TR_CONFIG_OFF : EOffOn.TR_CONFIG_ON;
                else if (keyEvent.ModAlt && (keyEvent.Key == Keys.P))
                    CDraw.MakeScreenShot();
                else
                {
                    if (_Fading == null)
                    {
                        bool handled = false;
                        if (_CurrentPopupScreen != EPopupScreens.NoPopup)
                        {
                            handled = _PopupScreens[(int)_CurrentPopupScreen].HandleInput(keyEvent);
                            if (popupVolumeControlAllowed && _CurrentPopupScreen == EPopupScreens.PopupVolumeControl && handled)
                                _VolumePopupTimer.Restart();
                        }
                        else if (popupVolumeControlAllowed && _PopupScreens[(int)EPopupScreens.PopupVolumeControl].HandleInput(keyEvent))
                        {
                            ShowPopup(EPopupScreens.PopupVolumeControl);
                            _VolumePopupTimer.Restart();
                        }

                        if (!handled)
                            resume &= CurrentScreen.HandleInput(keyEvent);
                    }
                }

                if (!eventsAvailable)
                    inputEventsAvailable = CController.PollKeyEvent(ref inputKeyEvent);
            }

            inputEventsAvailable = CController.PollMouseEvent(ref inputMouseEvent);

            while ((eventsAvailable = mouse.PollEvent(ref mouseEvent)) || inputEventsAvailable)
            {
                if (!eventsAvailable)
                    mouseEvent = inputMouseEvent;

                if (mouseEvent.Wheel != 0)
                    _Cursor.Activate();

                _UpdateMousePosition(mouseEvent.X, mouseEvent.Y);

                bool isOverPopupPlayerControl = CHelper.IsInBounds(_PopupScreens[(int)EPopupScreens.PopupPlayerControl].ScreenArea, mouseEvent);
                if (popupPlayerControlAllowed && isOverPopupPlayerControl)
                {
                    if (_CurrentPopupScreen == EPopupScreens.NoPopup && CConfig.Config.Sound.BackgroundMusic == EBackgroundMusicOffOn.TR_CONFIG_ON)
                        ShowPopup(EPopupScreens.PopupPlayerControl);
                }

                if (!isOverPopupPlayerControl && _CurrentPopupScreen == EPopupScreens.PopupPlayerControl)
                    HidePopup(EPopupScreens.PopupPlayerControl);

                bool isOverPopupVolumeControl = CHelper.IsInBounds(_PopupScreens[(int)EPopupScreens.PopupVolumeControl].ScreenArea, mouseEvent);
                if (popupVolumeControlAllowed && isOverPopupVolumeControl)
                {
                    if (_CurrentPopupScreen == EPopupScreens.NoPopup)
                    {
                        ShowPopup(EPopupScreens.PopupVolumeControl);
                        _VolumePopupTimer.Reset();
                        _VolumePopupTimer.Start();
                    }
                }

                if (_CursorOverVolumeControl && !isOverPopupVolumeControl)
                {
                    if (_CurrentPopupScreen == EPopupScreens.PopupVolumeControl)
                    {
                        HidePopup(EPopupScreens.PopupVolumeControl);
                        _VolumePopupTimer.Reset();
                    }
                }
                _CursorOverVolumeControl = isOverPopupVolumeControl;


                bool handled = false;
                if (_CurrentPopupScreen != EPopupScreens.NoPopup)
                    handled = _PopupScreens[(int)_CurrentPopupScreen].HandleMouse(mouseEvent);

                if (!handled && _Fading == null && (_Cursor.IsActive || mouseEvent.LB || mouseEvent.RB || mouseEvent.MB))
                    resume &= CurrentScreen.HandleMouse(mouseEvent);

                if (!eventsAvailable)
                    inputEventsAvailable = CController.PollMouseEvent(ref inputMouseEvent);
            }
            return resume;
        }

        private static bool _HandleInputThemeEditor(CKeys keys, CMouse mouse)
        {
            SKeyEvent keyEvent = new SKeyEvent();
            SMouseEvent mouseEvent = new SMouseEvent();

            while (keys.PollEvent(ref keyEvent))
            {
                if (keyEvent.ModShift && (keyEvent.Key == Keys.F1))
                {
                    CSettings.ProgramState = EProgramState.Normal;
                    CurrentScreen.NextElement();
                }
                else if (keyEvent.ModAlt && (keyEvent.Key == Keys.Enter))
                    CConfig.Config.Graphics.FullScreen = (CConfig.Config.Graphics.FullScreen == EOffOn.TR_CONFIG_ON) ? EOffOn.TR_CONFIG_OFF : EOffOn.TR_CONFIG_ON;
                else if (keyEvent.ModAlt && (keyEvent.Key == Keys.P))
                    CDraw.MakeScreenShot();
                else
                {
                    if (_Fading == null)
                        CurrentScreen.HandleInputThemeEditor(keyEvent);
                }
            }

            while (mouse.PollEvent(ref mouseEvent))
            {
                if (_Fading == null)
                    CurrentScreen.HandleMouseThemeEditor(mouseEvent);

                _UpdateMousePosition(mouseEvent.X, mouseEvent.Y);
            }
            return true;
        }

        private static void _UpdateMousePosition(int x, int y)
        {
            _Cursor.UpdatePosition(x, y);
        }

        private static bool _Update()
        {
            if (_VolumePopupTimer.IsRunning && _VolumePopupTimer.ElapsedMilliseconds >= 1500 && _CurrentPopupScreen == EPopupScreens.PopupVolumeControl)
            {
                _VolumePopupTimer.Reset();
                HidePopup(EPopupScreens.PopupVolumeControl);
            }

            if (_VolumePopupTimer.IsRunning)
                _Cursor.Activate();

            if (_CurrentPopupScreen != EPopupScreens.NoPopup)
                _PopupScreens[(int)_CurrentPopupScreen].UpdateGame();
            return CurrentScreen.UpdateGame();
        }

        private static void _DrawDebugInfos()
        {
            if (CConfig.Config.Debug.DebugLevel == EDebugLevel.TR_CONFIG_OFF)
                return;

            List<String> debugOutput = new List<string> {CTime.GetFPS().ToString("FPS: 000")};

            if (CConfig.Config.Debug.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL1)
            {
                debugOutput.Add(CSound.GetStreamCount().ToString(CLanguage.Translate("TR_DEBUG_AUDIO_STREAMS") + ": 00"));
                debugOutput.Add(CVideo.GetNumStreams().ToString(CLanguage.Translate("TR_DEBUG_VIDEO_STREAMS") + ": 00"));
                debugOutput.Add(CDraw.TextureCount().ToString(CLanguage.Translate("TR_DEBUG_TEXTURES") + ": 00000"));
                long memory = GC.GetTotalMemory(false);
                debugOutput.Add((memory / 1000000L).ToString(CLanguage.Translate("TR_DEBUG_MEMORY") + ": 00000 MB"));

                if (CConfig.Config.Debug.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL2)
                {
                    debugOutput.Add(CRecord.GetToneAbs(0).ToString(CLanguage.Translate("TR_DEBUG_TONE_ABS") + " P1: 00"));
                    debugOutput.Add(CRecord.GetMaxVolume(0).ToString(CLanguage.Translate("TR_DEBUG_MAX_VOLUME") + " P1: 0.000"));
                    debugOutput.Add(CRecord.GetToneAbs(1).ToString(CLanguage.Translate("TR_DEBUG_TONE_ABS") + " P2: 00"));
                    debugOutput.Add(CRecord.GetMaxVolume(1).ToString(CLanguage.Translate("TR_DEBUG_MAX_VOLUME") + " P2: 0.000"));

                    if (CConfig.Config.Debug.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL3)
                    {
                        debugOutput.Add(CSongs.NumSongsWithCoverLoaded.ToString(CLanguage.Translate("TR_DEBUG_SONGS") + ": 00000"));

                        if (CConfig.Config.Debug.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL_MAX)
                            debugOutput.Add(_Cursor.X.ToString(CLanguage.Translate("TR_DEBUG_MOUSE") + " : (0000/") + _Cursor.Y.ToString("0000)"));
                    }
                }
            }
            CFont font = new CFont("Normal", EStyle.Normal, 25);
            SColorF gray = new SColorF(1f, 1f, 1f, 0.5f);
            float y = 0;
            foreach (string txt in debugOutput)
            {
                float textWidth = CFonts.GetTextWidth(txt, font);
                RectangleF rect = new RectangleF(CSettings.RenderW - textWidth, y, textWidth, CFonts.GetTextHeight(txt, font));
                CDraw.DrawRect(gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.ZNear));
                CFonts.DrawText(txt, font, rect.X, rect.Y, CSettings.ZNear);
                y += rect.Height;
            }
        }
        #endregion private stuff
    }
}