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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Vocaluxe.Base.Fonts;
using Vocaluxe.Screens;
using VocaluxeLib.Menu;
using VocaluxeLib.PartyModes;

namespace Vocaluxe.Base
{
    static class CGraphics
    {
        private static bool _Fading;
        private static Stopwatch _FadingTimer;
        private static readonly CCursor _Cursor = new CCursor();
        private static float _GlobalAlpha;
        private static float _ZOffset;

        private static IMenu _OldScreen;
        private static EScreens _CurrentScreen;
        private static EScreens _NextScreen;
        private static EPopupScreens _CurrentPopupScreen;

        private static readonly List<IMenu> _Screens = new List<IMenu>();
        private static readonly List<IMenu> _PopupScreens = new List<IMenu>();

        private static Stopwatch _VolumePopupTimer;
        private static bool _CursorOverVolumeControl;

        public static float GlobalAlpha
        {
            get { return _GlobalAlpha; }
        }

        public static float ZOffset
        {
            get { return _ZOffset; }
        }

        public static EScreens CurrentScreen
        {
            get { return _CurrentScreen; }
        }

        #region public methods
        public static void InitGraphics()
        {
            // Add Screens, must be the same order as in EScreens!
            CLog.StartBenchmark(1, "Build Screen List");

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
            _Screens.Add(new CScreenOptionsLyrics());
            _Screens.Add(new CScreenOptionsTheme());
            _Screens.Add(new CScreenNames());
            _Screens.Add(new CScreenCredits());
            _Screens.Add(new CScreenParty());
            _Screens.Add(new CScreenPartyDummy());

            _PopupScreens.Add(new CPopupScreenPlayerControl());
            _PopupScreens.Add(new CPopupScreenVolumeControl());

            CLog.StopBenchmark(1, "Build Screen List");

            _CurrentScreen = EScreens.ScreenLoad;
            _NextScreen = EScreens.ScreenNull;
            _CurrentPopupScreen = EPopupScreens.NoPopup;
            _FadingTimer = new Stopwatch();
            _VolumePopupTimer = new Stopwatch();

            _GlobalAlpha = 1f;
            _ZOffset = 0f;

            CLog.StartBenchmark(0, "Load Theme");
            LoadTheme();
            CLog.StopBenchmark(0, "Load Theme");
        }

        public static void LoadTheme()
        {
            _Cursor.LoadTextures();

            for (int i = 0; i < _Screens.Count; i++)
            {
                CLog.StartBenchmark(1, "Load Theme " + Enum.GetNames(typeof(EScreens))[i]);
                _Screens[i].Init();
                _Screens[i].LoadTheme(CTheme.GetThemeScreensPath(-1));
                CLog.StopBenchmark(1, "Load Theme " + Enum.GetNames(typeof(EScreens))[i]);
            }

            foreach (IMenu popup in _PopupScreens)
            {
                popup.Init();
                popup.LoadTheme(CTheme.GetThemeScreensPath(-1));
            }
        }

        public static void ReloadTheme()
        {
            _Cursor.ReloadTextures();
            foreach (IMenu screen in _Screens)
                screen.ReloadTheme(CTheme.GetThemeScreensPath(-1));

            foreach (IMenu popup in _PopupScreens)
                popup.ReloadTheme(CTheme.GetThemeScreensPath(-1));
        }

        public static void ReloadSkin()
        {
            _Cursor.ReloadTextures();
            foreach (IMenu menu in _Screens)
                menu.ReloadTextures();

            foreach (IMenu menu in _PopupScreens)
                menu.ReloadTextures();
        }

        public static void SaveTheme()
        {
            CTheme.SaveTheme();
            foreach (IMenu screen in _Screens)
                screen.SaveTheme();

            foreach (IMenu popup in _PopupScreens)
                popup.SaveTheme();
        }

        public static void InitFirstScreen()
        {
            _Screens[(int)_CurrentScreen].OnShow();
            _Screens[(int)_CurrentScreen].OnShowFinish();
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

            if (CConfig.CoverLoading == ECoverLoading.TR_CONFIG_COVERLOADING_DYNAMIC && _CurrentScreen != EScreens.ScreenSing)
                CSongs.LoadCover();

            if (CSettings.GameState != EGameState.EditTheme)
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
            if ((_NextScreen != EScreens.ScreenNull) && !_Fading)
            {
                _Fading = true;
                _FadingTimer.Reset();
                _FadingTimer.Start();

                if (_NextScreen == EScreens.ScreenPartyDummy)
                {
                    CFonts.PartyModeID = CParty.CurrentPartyModeID;
                    _Screens[(int)_NextScreen].OnShow();
                    CFonts.PartyModeID = -1;
                }
                else
                    _Screens[(int)_NextScreen].OnShow();

                HidePopup(EPopupScreens.PopupPlayerControl);
            }

            if (_Fading)
            {
                long fadeTime = (long)(CConfig.FadeTime * 1000);

                if ((_FadingTimer.ElapsedMilliseconds < fadeTime) && (CConfig.FadeTime > 0))
                {
                    long ms = 1;
                    if (_FadingTimer.ElapsedMilliseconds > 0)
                        ms = _FadingTimer.ElapsedMilliseconds;

                    float factor = (float)ms / fadeTime;

                    _GlobalAlpha = 1f; // -factor / 100f;
                    _ZOffset = CSettings.ZFar / 2;

                    if (_CurrentScreen == EScreens.ScreenPartyDummy)
                    {
                        CFonts.PartyModeID = CParty.CurrentPartyModeID;
                        _OldScreen.Draw();
                        CFonts.PartyModeID = -1;
                    }
                    else
                        _OldScreen.Draw();


                    _GlobalAlpha = factor;
                    _ZOffset = 0f;

                    if (_NextScreen == EScreens.ScreenPartyDummy)
                    {
                        CFonts.PartyModeID = CParty.CurrentPartyModeID;
                        _Screens[(int)_NextScreen].Draw();
                        CFonts.PartyModeID = -1;
                    }
                    else
                        _Screens[(int)_NextScreen].Draw();

                    _GlobalAlpha = 1f;
                }
                else
                {
                    _Screens[(int)_CurrentScreen].OnClose();
                    GC.Collect();
                    _CurrentScreen = _NextScreen;
                    _NextScreen = EScreens.ScreenNull;
                    if (CBackgroundMusic.IsPlaying)
                        CBackgroundMusic.Play();
                    _Screens[(int)_CurrentScreen].OnShowFinish();
                    _Screens[(int)_CurrentScreen].ProcessMouseMove(_Cursor.X, _Cursor.Y);

                    if (_CurrentScreen == EScreens.ScreenPartyDummy)
                    {
                        CFonts.PartyModeID = CParty.CurrentPartyModeID;
                        _Screens[(int)_CurrentScreen].Draw();
                        CFonts.PartyModeID = -1;
                    }
                    else
                        _Screens[(int)_CurrentScreen].Draw();

                    _Fading = false;
                    _FadingTimer.Stop();
                }
            }
            else
            {
                if (_CurrentScreen == EScreens.ScreenPartyDummy)
                {
                    CFonts.PartyModeID = CParty.CurrentPartyModeID;
                    _Screens[(int)_CurrentScreen].Draw();
                    CFonts.PartyModeID = -1;
                }
                else
                    _Screens[(int)_CurrentScreen].Draw();

                _OldScreen = _Screens[(int)_CurrentScreen];
            }

            foreach (IMenu popup in _PopupScreens)
                popup.Draw();

            _Cursor.Draw();
            _DrawDebugInfos();

            return true;
        }

        public static void FadeTo(EScreens screen)
        {
            if (screen == EScreens.ScreenPartyDummy)
            {
                EScreens alt;
                CMenu scr = CParty.GetNextPartyScreen(out alt);
                if (scr == null)
                {
                    _NextScreen = alt;
                    return;
                }
                _Screens[(int)EScreens.ScreenPartyDummy] = scr;
            }

            _NextScreen = screen;
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

            bool popupPlayerControlAllowed = _CurrentScreen != EScreens.ScreenOptionsRecord && _CurrentScreen != EScreens.ScreenSing &&
                                             (_CurrentScreen != EScreens.ScreenSong ||
                                              (_CurrentScreen == EScreens.ScreenSong && !CSongs.IsInCategory && CConfig.Tabs == EOffOn.TR_CONFIG_ON))
                                             && _CurrentScreen != EScreens.ScreenCredits && !CBackgroundMusic.Disabled;

            bool popupVolumeControlAllowed = _CurrentScreen != EScreens.ScreenCredits && _CurrentScreen != EScreens.ScreenOptionsRecord;
            //Hide volume control for bg-music if bg-music is disabled
            if (popupVolumeControlAllowed && (_CurrentScreen != EScreens.ScreenSong || CSongs.Category == -1)
                && _CurrentScreen != EScreens.ScreenSing && CConfig.BackgroundMusic == EOffOn.TR_CONFIG_OFF)
                popupVolumeControlAllowed = false;


            bool resume = true;
            bool eventsAvailable;
            bool inputEventsAvailable = CController.PollKeyEvent(ref inputKeyEvent);

            while ((eventsAvailable = keys.PollEvent(ref keyEvent)) || inputEventsAvailable)
            {
                if (!eventsAvailable)
                    keyEvent = inputKeyEvent;

                if (keyEvent.Key == Keys.Left || keyEvent.Key == Keys.Right || keyEvent.Key == Keys.Up || keyEvent.Key == Keys.Down)
                {
                    CSettings.MouseInactive();
                    _Cursor.FadeOut();
                }

                if (popupPlayerControlAllowed && keyEvent.Key == Keys.Tab)
                {
                    if (_CurrentPopupScreen == EPopupScreens.NoPopup && CConfig.BackgroundMusic == EOffOn.TR_CONFIG_ON)
                        ShowPopup(EPopupScreens.PopupPlayerControl);
                    else
                        HidePopup(EPopupScreens.PopupPlayerControl);
                }

                if (popupPlayerControlAllowed && CConfig.BackgroundMusic == EOffOn.TR_CONFIG_ON)
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

                if (popupVolumeControlAllowed)
                {
                    int diff = 0;
                    if ((keyEvent.ModShift && (keyEvent.Key == Keys.Add || keyEvent.Key == Keys.PageUp)) || (keyEvent.Sender == ESender.WiiMote && keyEvent.Key == Keys.Add))
                        diff = 5;
                    else if ((keyEvent.ModShift && (keyEvent.Key == Keys.Subtract || keyEvent.Key == Keys.PageDown)) ||
                             (keyEvent.Sender == ESender.WiiMote && keyEvent.Key == Keys.Subtract))
                        diff = -5;

                    if (diff != 0)
                    {
                        switch (CurrentScreen)
                        {
                            case EScreens.ScreenSong:
                                if (CSongs.IsInCategory)
                                {
                                    CConfig.PreviewMusicVolume += diff;
                                    _Screens[(int)_CurrentScreen].ApplyVolume();
                                }
                                else
                                {
                                    CConfig.BackgroundMusicVolume += diff;
                                    CBackgroundMusic.ApplyVolume();
                                }
                                break;

                            case EScreens.ScreenSing:
                                CConfig.GameMusicVolume += diff;
                                _Screens[(int)_CurrentScreen].ApplyVolume();
                                break;

                            default:
                                CConfig.BackgroundMusicVolume += diff;
                                CBackgroundMusic.ApplyVolume();
                                break;
                        }
                        CConfig.SaveConfig();
                    }

                    if (_CurrentPopupScreen == EPopupScreens.NoPopup && diff != 0)
                    {
                        ShowPopup(EPopupScreens.PopupVolumeControl);
                        _VolumePopupTimer.Reset();
                        _VolumePopupTimer.Start();
                    }

                    CBackgroundMusic.ApplyVolume();
                }

                if (keyEvent.ModShift && (keyEvent.Key == Keys.F1))
                    CSettings.GameState = EGameState.EditTheme;
                else if (keyEvent.ModAlt && (keyEvent.Key == Keys.Enter))
                    CSettings.IsFullScreen = !CSettings.IsFullScreen;
                else if (keyEvent.ModAlt && (keyEvent.Key == Keys.P))
                    CDraw.MakeScreenShot();
                else
                {
                    if (!_Fading)
                    {
                        bool handled = false;
                        if (_CurrentPopupScreen != EPopupScreens.NoPopup)
                            handled = _PopupScreens[(int)_CurrentPopupScreen].HandleInput(keyEvent);

                        if (!handled)
                            resume &= _Screens[(int)_CurrentScreen].HandleInput(keyEvent);
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
                {
                    CSettings.MouseActive();
                    _Cursor.FadeIn();
                }

                _UpdateMousePosition(mouseEvent.X, mouseEvent.Y);

                bool isOverPopupPlayerControl = CHelper.IsInBounds(_PopupScreens[(int)EPopupScreens.PopupPlayerControl].ScreenArea, mouseEvent);
                if (popupPlayerControlAllowed && isOverPopupPlayerControl)
                {
                    if (_CurrentPopupScreen == EPopupScreens.NoPopup && CConfig.BackgroundMusic == EOffOn.TR_CONFIG_ON)
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

                if (handled && _CurrentPopupScreen == EPopupScreens.PopupVolumeControl)
                    _Screens[(int)_CurrentScreen].ApplyVolume();

                if (!handled && !_Fading && (_Cursor.IsActive || mouseEvent.LB || mouseEvent.RB || mouseEvent.MB))
                    resume &= _Screens[(int)_CurrentScreen].HandleMouse(mouseEvent);

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
                    CSettings.GameState = EGameState.Normal;
                    _Screens[(int)_CurrentScreen].NextInteraction();
                }
                else if (keyEvent.ModAlt && (keyEvent.Key == Keys.Enter))
                    CSettings.IsFullScreen = !CSettings.IsFullScreen;
                else if (keyEvent.ModAlt && (keyEvent.Key == Keys.P))
                    CDraw.MakeScreenShot();
                else
                {
                    if (!_Fading)
                        _Screens[(int)_CurrentScreen].HandleInputThemeEditor(keyEvent);
                }
            }

            while (mouse.PollEvent(ref mouseEvent))
            {
                if (!_Fading)
                    _Screens[(int)_CurrentScreen].HandleMouseThemeEditor(mouseEvent);

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
                _Cursor.FadeIn();

            if (_CurrentPopupScreen != EPopupScreens.NoPopup)
                _PopupScreens[(int)_CurrentPopupScreen].UpdateGame();
            return _Screens[(int)_CurrentScreen].UpdateGame();
        }

        private static void _DrawDebugInfos()
        {
            if (CConfig.DebugLevel == EDebugLevel.TR_CONFIG_OFF)
                return;

            List<String> debugOutput = new List<string> {CTime.GetFPS().ToString("FPS: 000")};

            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL1)
            {
                debugOutput.Add(CSound.GetStreamCount().ToString(CLanguage.Translate("TR_DEBUG_AUDIO_STREAMS") + ": 00"));
                debugOutput.Add(CVideo.GetNumStreams().ToString(CLanguage.Translate("TR_DEBUG_VIDEO_STREAMS") + ": 00"));
                debugOutput.Add(CDraw.TextureCount().ToString(CLanguage.Translate("TR_DEBUG_TEXTURES") + ": 00000"));
                long memory = GC.GetTotalMemory(false);
                debugOutput.Add((memory / 1000000L).ToString(CLanguage.Translate("TR_DEBUG_MEMORY") + ": 00000 MB"));

                if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL2)
                {
                    debugOutput.Add(CSound.RecordGetToneAbs(0).ToString(CLanguage.Translate("TR_DEBUG_TONE_ABS") + " P1: 00"));
                    debugOutput.Add(CSound.RecordGetMaxVolume(0).ToString(CLanguage.Translate("TR_DEBUG_MAX_VOLUME") + " P1: 0.000"));
                    debugOutput.Add(CSound.RecordGetToneAbs(1).ToString(CLanguage.Translate("TR_DEBUG_TONE_ABS") + " P2: 00"));
                    debugOutput.Add(CSound.RecordGetMaxVolume(1).ToString(CLanguage.Translate("TR_DEBUG_MAX_VOLUME") + " P2: 0.000"));

                    if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL3)
                    {
                        debugOutput.Add(CSongs.NumSongsWithCoverLoaded.ToString(CLanguage.Translate("TR_DEBUG_SONGS") + ": 00000"));

                        if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL_MAX)
                            debugOutput.Add(_Cursor.X.ToString(CLanguage.Translate("TR_DEBUG_MOUSE") + " : (0000/") + _Cursor.Y.ToString("0000)"));
                    }
                }
            }
            CFonts.Style = EStyle.Normal;
            CFonts.SetFont("Normal");
            CFonts.Height = 30;
            SColorF gray = new SColorF(1f, 1f, 1f, 0.5f);
            float y = 0;
            foreach (string txt in debugOutput)
            {
                RectangleF rect = new RectangleF(CSettings.RenderW - CFonts.GetTextWidth(txt), y, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));
                CDraw.DrawColor(gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.ZNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.ZNear);
                y += rect.Height;
            }
        }
        #endregion private stuff
    }
}