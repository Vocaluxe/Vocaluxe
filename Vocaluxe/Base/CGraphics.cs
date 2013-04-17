﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Vocaluxe.Screens;
using VocaluxeLib.Menu;
using VocaluxeLib.PartyModes;

namespace Vocaluxe.Base
{
    enum EPopupScreens
    {
        PopupPlayerControl = 0,
        PopupVolumeControl = 1,

        NoPopup = -1
    }

    class CCursor
    {
        private readonly Stopwatch _CursorFadingTimer;
        private float _CursorTargetAlpha;
        private float _CursorStartAlpha;
        private float _CursorFadingTime;
        private STexture _Cursor;
        private readonly string _CursorName = String.Empty;

        private readonly Stopwatch _Movetimer;

        public bool ShowCursor;
        public bool Visible = true;
        public bool CursorVisible = true;

        public int X
        {
            get { return (int)_Cursor.Rect.X; }
            set { UpdatePosition(value, (int)_Cursor.Rect.Y); }
        }

        public int Y
        {
            get { return (int)_Cursor.Rect.Y; }
            set { UpdatePosition((int)_Cursor.Rect.X, value); }
        }

        public bool IsActive
        {
            get { return _Movetimer.IsRunning; }
        }

        public CCursor(string textureName, SColorF color, float w, float h, float z)
        {
            _CursorFadingTimer = new Stopwatch();
            ShowCursor = true;
            _CursorTargetAlpha = 1f;
            _CursorStartAlpha = 0f;
            _CursorFadingTime = 0.5f;

            _CursorName = textureName;
            _Cursor = CDraw.AddTexture(CTheme.GetSkinFilePath(_CursorName, -1));

            _Cursor.Color = color;
            _Cursor.Rect.W = w;
            _Cursor.Rect.H = h;
            _Cursor.Rect.Z = z;

            _Movetimer = new Stopwatch();
        }

        public void Draw()
        {
            if (_Movetimer.IsRunning && _Movetimer.ElapsedMilliseconds / 1000f > CSettings.MouseMoveOffTime)
            {
                _Movetimer.Stop();
                _Fade(0f, 0.5f);
            }


            if (_CursorFadingTimer.IsRunning)
            {
                float t = _CursorFadingTimer.ElapsedMilliseconds / 1000f;
                if (t < _CursorFadingTime)
                {
                    if (_CursorTargetAlpha >= _CursorStartAlpha)
                        _Cursor.Color.A = _CursorStartAlpha + (_CursorTargetAlpha - _CursorStartAlpha) * t / _CursorFadingTime;
                    else
                        _Cursor.Color.A = (_CursorStartAlpha - _CursorTargetAlpha) * (1f - t / _CursorFadingTime);
                }
                else
                {
                    _CursorFadingTimer.Stop();
                    _Cursor.Color.A = _CursorTargetAlpha;
                }
            }

            if (CursorVisible && (CSettings.GameState == EGameState.EditTheme || ShowCursor))
                CDraw.DrawTexture(_Cursor);
        }

        public void UpdatePosition(int x, int y)
        {
            if (Math.Abs(_Cursor.Rect.X - x) > CSettings.MouseMoveDiffMin ||
                Math.Abs(_Cursor.Rect.Y - y) > CSettings.MouseMoveDiffMin)
            {
                if (_CursorTargetAlpha == 0f)
                    _Fade(1f, 0.2f);

                _Movetimer.Reset();
                _Movetimer.Start();
                CSettings.MouseActive();
            }

            _Cursor.Rect.X = x;
            _Cursor.Rect.Y = y;
        }

        public void UnloadTextures()
        {
            CDraw.RemoveTexture(ref _Cursor);
        }

        public void ReloadTextures()
        {
            UnloadTextures();

            _Cursor = CDraw.AddTexture(CTheme.GetSkinFilePath(_CursorName, -1));
        }

        public void FadeOut()
        {
            _Movetimer.Stop();
            _Fade(0f, 0.5f);
        }

        public void FadeIn()
        {
            _Movetimer.Reset();
            _Movetimer.Start();
            _Fade(1f, 0.2f);
        }

        private void _Fade(float targetAlpha, float time)
        {
            _CursorFadingTimer.Stop();
            _CursorFadingTimer.Reset();

            if (targetAlpha >= 0f && targetAlpha <= 1f)
                _CursorTargetAlpha = targetAlpha;
            else
                _CursorTargetAlpha = 1f;

            if (time >= 0f)
                _CursorFadingTime = time;

            _CursorStartAlpha = _Cursor.Color.A;
            _CursorFadingTimer.Start();
        }
    }

    static class CGraphics
    {
        private static bool _Fading;
        private static Stopwatch _FadingTimer;
        private static CCursor _Cursor;
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
            _Cursor = new CCursor(
                CTheme.Cursor.SkinName,
                new SColorF(CTheme.Cursor.R, CTheme.Cursor.G, CTheme.Cursor.B, CTheme.Cursor.A),
                CTheme.Cursor.W,
                CTheme.Cursor.H,
                CSettings.ZNear);

            for (int i = 0; i < _Screens.Count; i++)
            {
                CLog.StartBenchmark(1, "Load Theme " + Enum.GetNames(typeof(EScreens))[i]);
                _Screens[i].Init();
                _Screens[i].LoadTheme(CTheme.GetThemeScreensPath(-1));
                CLog.StopBenchmark(1, "Load Theme " + Enum.GetNames(typeof(EScreens))[i]);
            }

            for (int i = 0; i < _PopupScreens.Count; i++)
            {
                _PopupScreens[i].Init();
                _PopupScreens[i].LoadTheme(CTheme.GetThemeScreensPath(-1));
            }
        }

        public static void ReloadTheme()
        {
            _ReloadCursor();
            for (int i = 0; i < _Screens.Count; i++)
                _Screens[i].ReloadTheme(CTheme.GetThemeScreensPath(-1));

            for (int i = 0; i < _PopupScreens.Count; i++)
                _PopupScreens[i].ReloadTheme(CTheme.GetThemeScreensPath(-1));
        }

        public static void ReloadSkin()
        {
            _ReloadCursor();
            for (int i = 0; i < _Screens.Count; i++)
                _Screens[i].ReloadTextures();

            for (int i = 0; i < _PopupScreens.Count; i++)
                _PopupScreens[i].ReloadTextures();
        }

        public static void SaveTheme()
        {
            CTheme.SaveTheme();
            for (int i = 0; i < _Screens.Count; i++)
                _Screens[i].SaveTheme();

            for (int i = 0; i < _PopupScreens.Count; i++)
                _PopupScreens[i].SaveTheme();
        }

        public static void InitFirstScreen()
        {
            _Screens[(int)_CurrentScreen].OnShow();
            _Screens[(int)_CurrentScreen].OnShowFinish();
        }

        public static bool UpdateGameLogic(CKeys keys, CMouse mouse)
        {
            bool run = true;
            _Cursor.CursorVisible = mouse.Visible;

            mouse.CopyEvents();
            keys.CopyEvents();

            CVideo.Update();
            CSound.Update();
            CBackgroundMusic.Update();
            CInput.Update();

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
                    if (CBackgroundMusic.Playing)
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

            for (int i = 0; i < _PopupScreens.Count; i++)
                _PopupScreens[i].Draw();

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
            if (popupVolumeControlAllowed && (_CurrentScreen != EScreens.ScreenSong || (_CurrentScreen == EScreens.ScreenSong && CSongs.Category == -1))
                && _CurrentScreen != EScreens.ScreenSing && CConfig.BackgroundMusic == EOffOn.TR_CONFIG_OFF)
                popupVolumeControlAllowed = false;


            bool resume = true;
            bool eventsAvailable = false;
            bool inputEventsAvailable = CInput.PollKeyEvent(ref inputKeyEvent);

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
                        CMain.BackgroundMusic.Next();
                    else if (keyEvent.Key == Keys.MediaPreviousTrack)
                        CMain.BackgroundMusic.Previous();
                    else if (keyEvent.Key == Keys.MediaPlayPause)
                    {
                        if (CMain.BackgroundMusic.IsPlaying())
                            CMain.BackgroundMusic.Pause();
                        else
                            CMain.BackgroundMusic.Play();
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

                    CMain.BackgroundMusic.ApplyVolume();
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
                    inputEventsAvailable = CInput.PollKeyEvent(ref inputKeyEvent);
            }

            inputEventsAvailable = CInput.PollMouseEvent(ref inputMouseEvent);

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
                    inputEventsAvailable = CInput.PollMouseEvent(ref inputMouseEvent);
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
            string txt = String.Empty;
            CFonts.Style = EStyle.Normal;
            CFonts.SetFont("Normal");
            SColorF gray = new SColorF(1f, 1f, 1f, 0.5f);

            float dy = 0;
            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_ONLY_FPS)
            {
                txt = CTime.GetFPS().ToString("FPS: 000");
                CFonts.Height = 30f;
                RectangleF rect = new RectangleF(CSettings.RenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.ZNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.ZNear);
                dy += rect.Height;
            }

            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL1)
            {
                txt = CSound.GetStreamCount().ToString(CLanguage.Translate("TR_DEBUG_AUDIO_STREAMS") + ": 00");

                RectangleF rect = new RectangleF(CSettings.RenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.ZNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.ZNear);
                dy += rect.Height;
            }

            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL1)
            {
                txt = CVideo.GetNumStreams().ToString(CLanguage.Translate("TR_DEBUG_VIDEO_STREAMS") + ": 00");

                RectangleF rect = new RectangleF(CSettings.RenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.ZNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.ZNear);
                dy += rect.Height;
            }

            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL1)
            {
                txt = CDraw.TextureCount().ToString(CLanguage.Translate("TR_DEBUG_TEXTURES") + ": 00000");

                RectangleF rect = new RectangleF(CSettings.RenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.ZNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.ZNear);
                dy += rect.Height;
            }

            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL1)
            {
                long memory = GC.GetTotalMemory(false);
                txt = (memory / 1000000L).ToString(CLanguage.Translate("TR_DEBUG_MEMORY") + ": 00000 MB");

                RectangleF rect = new RectangleF(CSettings.RenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.ZNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.ZNear);
                dy += rect.Height;
            }

            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL2)
            {
                txt = CSound.RecordGetToneAbs(0).ToString(CLanguage.Translate("TR_DEBUG_TONE_ABS") + " P1: 00");

                RectangleF rect = new RectangleF(CSettings.RenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.ZNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.ZNear);
                dy += rect.Height;


                txt = CSound.RecordGetMaxVolume(0).ToString(CLanguage.Translate("TR_DEBUG_MAX_VOLUME") + " P1: 0.000");

                rect = new RectangleF(CSettings.RenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.ZNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.ZNear);
                dy += rect.Height;

                txt = CSound.RecordGetToneAbs(1).ToString(CLanguage.Translate("TR_DEBUG_TONE_ABS") + " P2: 00");

                rect = new RectangleF(CSettings.RenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.ZNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.ZNear);
                dy += rect.Height;


                txt = CSound.RecordGetMaxVolume(1).ToString(CLanguage.Translate("TR_DEBUG_MAX_VOLUME") + " P2: 0.000");

                rect = new RectangleF(CSettings.RenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.ZNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.ZNear);
                dy += rect.Height;
            }

            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL3)
            {
                txt = CSongs.NumSongsWithCoverLoaded.ToString(CLanguage.Translate("TR_DEBUG_SONGS") + ": 00000");

                RectangleF rect = new RectangleF(CSettings.RenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.ZNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.ZNear);
                dy += rect.Height;
            }

            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL_MAX)
            {
                txt = _Cursor.X.ToString(CLanguage.Translate("TR_DEBUG_MOUSE") + " : (0000/") + _Cursor.Y.ToString("0000)");

                RectangleF rect = new RectangleF(CSettings.RenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.ZNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.ZNear);
                dy += rect.Height;
            }
        }

        private static void _ReloadCursor()
        {
            _Cursor.UnloadTextures();

            if (CTheme.Cursor.Color.Length > 0)
            {
                SColorF color;
                color = CTheme.GetColor(CTheme.Cursor.Color, -1);
                CTheme.Cursor.R = color.R;
                CTheme.Cursor.G = color.G;
                CTheme.Cursor.B = color.B;
                CTheme.Cursor.A = color.A;
            }

            _Cursor = new CCursor(
                CTheme.Cursor.SkinName,
                new SColorF(CTheme.Cursor.R, CTheme.Cursor.G, CTheme.Cursor.B, CTheme.Cursor.A),
                CTheme.Cursor.W,
                CTheme.Cursor.H,
                CSettings.ZNear);
        }
        #endregion private stuff
    }
}