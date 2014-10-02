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
using System.IO;
using System.Xml.Serialization;
using Vocaluxe.Base.Fonts;
using Vocaluxe.Screens;
using VocaluxeLib;
using VocaluxeLib.Menu;
using VocaluxeLib.PartyModes;

namespace Vocaluxe.Base
{
    static class CGraphics
    {
        private static CFading _Fading;
        private static readonly CCursor _Cursor = new CCursor();
        private static float _GlobalAlpha;
        private static float _ZOffset;

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

        public static EScreens NextScreen
        {
            get { return _NextScreen; }
        }

        #region public methods
        public static void Init()
        {
            // Add Screens, must be the same order as in EScreens!
            CLog.StartBenchmark("Build Screen List");

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
            _Screens.Add(new CScreenPartyDummy());

            _PopupScreens.Add(new CPopupScreenPlayerControl());
            _PopupScreens.Add(new CPopupScreenVolumeControl());
            _PopupScreens.Add(new CPopupScreenServerQR());

            CLog.StopBenchmark("Build Screen List");

            _CurrentScreen = EScreens.ScreenLoad;
            _NextScreen = EScreens.ScreenNull;
            _CurrentPopupScreen = EPopupScreens.NoPopup;
            _VolumePopupTimer = new Stopwatch();

            _GlobalAlpha = 1f;
            _ZOffset = 0f;

            CLog.StartBenchmark("Load Theme");
            LoadTheme();
            CLog.StopBenchmark("Load Theme");
        }

        public static void Close()
        {
            if (_CurrentPopupScreen != EPopupScreens.NoPopup)
                _PopupScreens[(int)_CurrentPopupScreen].OnClose();
            _Screens[(int)_CurrentScreen].OnClose();
        }

        public static void LoadTheme()
        {
            _Cursor.LoadTextures();

            for (int i = 0; i < _Screens.Count; i++)
            {
                CLog.StartBenchmark("Load Theme " + Enum.GetNames(typeof(EScreens))[i]);
                _Screens[i].Init();
                _Screens[i].LoadTheme(CTheme.GetThemeScreensPath(-1));
                CLog.StopBenchmark("Load Theme " + Enum.GetNames(typeof(EScreens))[i]);
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
            if (_NextScreen != EScreens.ScreenNull && _Fading == null)
            {
                _Fading = new CFading(0f, 1f, CConfig.FadeTime);

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

            if (_Fading != null)
            {
                bool finished;
                float newAlpha = _Fading.GetValue(out finished);

                if (!finished)
                {
                    _GlobalAlpha = 1f; // -newAlpha / 100f;
                    _ZOffset = CSettings.ZFar / 2;

                    _DrawScreen(_CurrentScreen);


                    _GlobalAlpha = newAlpha;
                    _ZOffset = 0f;

                    _DrawScreen(_NextScreen);

                    _GlobalAlpha = 1f;
                }
                else
                {
                    _Screens[(int)_CurrentScreen].OnClose();
                    _CurrentScreen = _NextScreen;
                    _NextScreen = EScreens.ScreenNull;
                    _Screens[(int)_CurrentScreen].OnShowFinish();
                    _Screens[(int)_CurrentScreen].ProcessMouseMove(_Cursor.X, _Cursor.Y);

                    _DrawScreen(_CurrentScreen);

                    _Fading = null;
                }
            }
            else
                _DrawScreen(_CurrentScreen);

            foreach (IMenu popup in _PopupScreens)
                popup.Draw();

            _Cursor.Draw();
            _DrawDebugInfos();

            return true;
        }

        private static void _DrawScreen(EScreens screen)
        {
            if (screen == EScreens.ScreenPartyDummy)
            {
                CFonts.PartyModeID = CParty.CurrentPartyModeID;
                _Screens[(int)screen].Draw();
                CFonts.PartyModeID = -1;
            }
            else
                _Screens[(int)screen].Draw();
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
                && _CurrentScreen != EScreens.ScreenSing && CConfig.BackgroundMusic == EBackgroundMusicOffOn.TR_CONFIG_OFF)
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
                                    CConfig.BorderLeft++;
                                else
                                    CConfig.BorderRight--;
                                break;
                            case Keys.Left:
                                if (keyEvent.ModShift)
                                    CConfig.BorderLeft--;
                                else
                                    CConfig.BorderRight++;
                                break;
                            case Keys.Down:
                                if (keyEvent.ModShift)
                                    CConfig.BorderTop++;
                                else
                                    CConfig.BorderBottom--;
                                break;
                            case Keys.Up:
                                if (keyEvent.ModShift)
                                    CConfig.BorderTop--;
                                else
                                    CConfig.BorderBottom++;
                                break;
                            case Keys.D0:
                            case Keys.NumPad0:
                                CConfig.BorderLeft = CConfig.BorderRight = CConfig.BorderTop = CConfig.BorderBottom = 0;
                                break;
                            case Keys.Add:
                                switch (CConfig.ScreenAlignment)
                                {
                                    case EGeneralAlignment.Middle:
                                        CConfig.ScreenAlignment = EGeneralAlignment.End;
                                        break;
                                    case EGeneralAlignment.End:
                                        CConfig.ScreenAlignment = EGeneralAlignment.Start;
                                        break;
                                    default:
                                        CConfig.ScreenAlignment = EGeneralAlignment.Middle;
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

                if (popupPlayerControlAllowed && keyEvent.Key == Keys.Tab)
                {
                    if (_CurrentPopupScreen == EPopupScreens.NoPopup && CConfig.BackgroundMusic == EBackgroundMusicOffOn.TR_CONFIG_ON)
                        ShowPopup(EPopupScreens.PopupPlayerControl);
                    else
                        HidePopup(EPopupScreens.PopupPlayerControl);
                }

                if (popupPlayerControlAllowed && CConfig.BackgroundMusic != EBackgroundMusicOffOn.TR_CONFIG_OFF)
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
                    //TODO: Handle this in the volume popup
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
                                    CSound.SetGlobalVolume(CConfig.PreviewMusicVolume);
                                }
                                else
                                {
                                    CConfig.BackgroundMusicVolume += diff;
                                    CSound.SetGlobalVolume(CConfig.BackgroundMusicVolume);
                                }
                                break;

                            case EScreens.ScreenSing:
                                CConfig.GameMusicVolume += diff;
                                CSound.SetGlobalVolume(CConfig.GameMusicVolume);
                                break;

                            default:
                                CConfig.BackgroundMusicVolume += diff;
                                CSound.SetGlobalVolume(CConfig.BackgroundMusicVolume);
                                break;
                        }
                        CConfig.SaveConfig();
                    }

                    if (_CurrentPopupScreen == EPopupScreens.NoPopup && diff != 0)
                    {
                        ShowPopup(EPopupScreens.PopupVolumeControl);
                        _VolumePopupTimer.Restart();
                    }
                }

                if (keyEvent.ModShift && (keyEvent.Key == Keys.F1))
                    CSettings.ProgramState = EProgramState.EditTheme;
                else if (keyEvent.ModAlt && (keyEvent.Key == Keys.Enter))
                    CConfig.FullScreen = (CConfig.FullScreen == EOffOn.TR_CONFIG_ON) ? EOffOn.TR_CONFIG_OFF : EOffOn.TR_CONFIG_ON;
                else if (keyEvent.ModAlt && (keyEvent.Key == Keys.P))
                    CDraw.MakeScreenShot();
                else
                {
                    if (_Fading == null)
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
                    _Cursor.Activate();

                _UpdateMousePosition(mouseEvent.X, mouseEvent.Y);

                bool isOverPopupPlayerControl = CHelper.IsInBounds(_PopupScreens[(int)EPopupScreens.PopupPlayerControl].ScreenArea, mouseEvent);
                if (popupPlayerControlAllowed && isOverPopupPlayerControl)
                {
                    if (_CurrentPopupScreen == EPopupScreens.NoPopup && CConfig.BackgroundMusic == EBackgroundMusicOffOn.TR_CONFIG_ON)
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
                    CSettings.ProgramState = EProgramState.Normal;
                    _Screens[(int)_CurrentScreen].NextInteraction();
                }
                else if (keyEvent.ModAlt && (keyEvent.Key == Keys.Enter))
                    CConfig.FullScreen = (CConfig.FullScreen == EOffOn.TR_CONFIG_ON) ? EOffOn.TR_CONFIG_OFF : EOffOn.TR_CONFIG_ON;
                else if (keyEvent.ModAlt && (keyEvent.Key == Keys.P))
                    CDraw.MakeScreenShot();
                else
                {
                    if (_Fading == null)
                        _Screens[(int)_CurrentScreen].HandleInputThemeEditor(keyEvent);
                }
            }

            while (mouse.PollEvent(ref mouseEvent))
            {
                if (_Fading == null)
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
                _Cursor.Activate();

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
                    debugOutput.Add(CRecord.GetToneAbs(0).ToString(CLanguage.Translate("TR_DEBUG_TONE_ABS") + " P1: 00"));
                    debugOutput.Add(CRecord.GetMaxVolume(0).ToString(CLanguage.Translate("TR_DEBUG_MAX_VOLUME") + " P1: 0.000"));
                    debugOutput.Add(CRecord.GetToneAbs(1).ToString(CLanguage.Translate("TR_DEBUG_TONE_ABS") + " P2: 00"));
                    debugOutput.Add(CRecord.GetMaxVolume(1).ToString(CLanguage.Translate("TR_DEBUG_MAX_VOLUME") + " P2: 0.000"));

                    if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL3)
                    {
                        debugOutput.Add(CSongs.NumSongsWithCoverLoaded.ToString(CLanguage.Translate("TR_DEBUG_SONGS") + ": 00000"));

                        if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL_MAX)
                            debugOutput.Add(_Cursor.X.ToString(CLanguage.Translate("TR_DEBUG_MOUSE") + " : (0000/") + _Cursor.Y.ToString("0000)"));
                    }
                }
            }
            CFont font=new CFont("Normal",EStyle.Normal, 25);
            SColorF gray = new SColorF(1f, 1f, 1f, 0.5f);
            float y = 0;
            foreach (string txt in debugOutput)
            {
                float textWidth = CFonts.GetTextWidth(txt, font);
                RectangleF rect = new RectangleF(CSettings.RenderW - textWidth, y, textWidth, CFonts.GetTextHeight(txt,font));
                CDraw.DrawRect(gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.ZNear));
                CFonts.DrawText(txt, font, rect.X, rect.Y, CSettings.ZNear);
                y += rect.Height;
            }
        }
        #endregion private stuff
    }
}