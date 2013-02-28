using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Menu;
using Vocaluxe.PartyModes;
using Vocaluxe.Screens;

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
        private Stopwatch _CursorFadingTimer;
        private float _CursorTargetAlpha;
        private float _CursorStartAlpha;
        private float _CursorFadingTime;
        private STexture _Cursor;
        private string _CursorName = String.Empty;

        private Stopwatch _Movetimer;

        public bool ShowCursor;
        public bool Visible = true;
        public bool CursorVisible = true;
        
        public int X
        {
            get { return (int)_Cursor.rect.X; }
            set { UpdatePosition(value, (int)_Cursor.rect.Y); }
        }

        public int Y
        {
            get { return (int)_Cursor.rect.Y; }
            set { UpdatePosition((int)_Cursor.rect.X, value); }
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

            _Cursor.color = color;
            _Cursor.rect.W = w;
            _Cursor.rect.H = h;
            _Cursor.rect.Z = z;

            _Movetimer = new Stopwatch();
        }

        public void Draw()
        {
            if (_Movetimer.IsRunning && _Movetimer.ElapsedMilliseconds / 1000f > CSettings.MouseMoveOffTime)
            {
                _Movetimer.Stop();
                Fade(0f, 0.5f);
            }


            if (_CursorFadingTimer.IsRunning)
            {
                float t = _CursorFadingTimer.ElapsedMilliseconds / 1000f;
                if (t < _CursorFadingTime)
                {
                    if (_CursorTargetAlpha >= _CursorStartAlpha)
                        _Cursor.color.A = _CursorStartAlpha + (_CursorTargetAlpha - _CursorStartAlpha) * t / _CursorFadingTime;
                    else
                        _Cursor.color.A = (_CursorStartAlpha - _CursorTargetAlpha) * (1f - t / _CursorFadingTime);
                }
                else
                {
                    _CursorFadingTimer.Stop();
                    _Cursor.color.A = _CursorTargetAlpha;
                }
            }

            if (CursorVisible && (CSettings.GameState == EGameState.EditTheme || ShowCursor))
                CDraw.DrawTexture(_Cursor);
        }

        public void UpdatePosition(int x, int y)
        {
            if (Math.Abs(_Cursor.rect.X - x) > CSettings.MouseMoveDiffMin ||
                Math.Abs(_Cursor.rect.Y - y) > CSettings.MouseMoveDiffMin)
            {
                if (_CursorTargetAlpha == 0f)
                    Fade(1f, 0.2f);

                _Movetimer.Reset();
                _Movetimer.Start();
                CSettings.MouseActive();
            }

            _Cursor.rect.X = x;
            _Cursor.rect.Y = y;
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
            Fade(0f, 0.5f);
        }

        public void FadeIn()
        {
            _Movetimer.Reset();
            _Movetimer.Start();
            Fade(1f, 0.2f);
        }

        private void Fade(float targetAlpha, float time)
        {
            _CursorFadingTimer.Stop();
            _CursorFadingTimer.Reset();

            if (targetAlpha >= 0f && targetAlpha <= 1f)
                _CursorTargetAlpha = targetAlpha;
            else
                _CursorTargetAlpha = 1f;

            if (time >= 0f)
                _CursorFadingTime = time;

            _CursorStartAlpha = _Cursor.color.A;
            _CursorFadingTimer.Start();
        }
    }

    static class CGraphics
    {
        private static bool _Fading = false;
        private static Stopwatch _FadingTimer;
        private static CCursor _Cursor;
        private static float _GlobalAlpha;
        private static float _ZOffset;

        private static IMenu _OldScreen;
        private static EScreens _CurrentScreen;
        private static EScreens _NextScreen;
        private static EPopupScreens _CurrentPopupScreen;

        private static List<IMenu> _Screens = new List<IMenu>();
        private static List<IMenu> _PopupScreens = new List<IMenu>();

        private static Stopwatch _VolumePopupTimer;
        private static bool CursorOverVolumeControl = false;

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
                new SColorF(CTheme.Cursor.r, CTheme.Cursor.g, CTheme.Cursor.b, CTheme.Cursor.a),
                CTheme.Cursor.w,
                CTheme.Cursor.h,
                CSettings.zNear);

            for (int i = 0; i < _Screens.Count; i++)
            {
                CLog.StartBenchmark(1, "Load Theme " + Enum.GetNames(typeof(EScreens))[i]);
                _Screens[i].Initialize();
                _Screens[i].LoadTheme(CTheme.GetThemeScreensPath(-1));
                CLog.StopBenchmark(1, "Load Theme " + Enum.GetNames(typeof(EScreens))[i]);
            }

            for (int i = 0; i < _PopupScreens.Count; i++)
			{
                _PopupScreens[i].Initialize();
                _PopupScreens[i].LoadTheme(CTheme.GetThemeScreensPath(-1));
			}
        }

        public static void ReloadTheme()
        {
            ReloadCursor();
            for (int i = 0; i < _Screens.Count; i++)
            {
                _Screens[i].ReloadTheme(CTheme.GetThemeScreensPath(-1));
            }

            for (int i = 0; i < _PopupScreens.Count; i++)
			{
                _PopupScreens[i].ReloadTheme(CTheme.GetThemeScreensPath(-1));
			}
        }

        public static void ReloadSkin()
        {
            ReloadCursor();
            for (int i = 0; i < _Screens.Count; i++)
            {
                _Screens[i].ReloadTextures();
            }

            for (int i = 0; i < _PopupScreens.Count; i++)
			{
			    _PopupScreens[i].ReloadTextures();
			}
        }

        public static void SaveTheme()
        {
            CTheme.SaveTheme();
            for (int i = 0; i < _Screens.Count; i++)
            {
                _Screens[i].SaveTheme();
            }

            for (int i = 0; i < _PopupScreens.Count; i++)
			{
                _PopupScreens[i].SaveTheme();
			}
        }

        public static void InitFirstScreen()
        {
            _Screens[(int)_CurrentScreen].OnShow();
            _Screens[(int)_CurrentScreen].OnShowFinish();
        }

        public static bool UpdateGameLogic(CKeys Keys, CMouse Mouse)
        {
            bool _Run = true;
            _Cursor.CursorVisible = Mouse.Visible;

            Mouse.CopyEvents();
            Keys.CopyEvents();

            CVideo.Update();
            CSound.Update();
            CBackgroundMusic.Update();
            CInput.Update();

            if (CConfig.CoverLoading == ECoverLoading.TR_CONFIG_COVERLOADING_DYNAMIC && _CurrentScreen != EScreens.ScreenSing)
                CSongs.LoadCover(30, 1);

            if (CSettings.GameState != EGameState.EditTheme)
            {
                _Run &= HandleInputs(Keys, Mouse);
                _Run &= Update();
                CParty.UpdateGame();
            }
            else
            {
                _Run &= HandleInputThemeEditor(Keys, Mouse);
                _Run &= Update();
            }

            return _Run;
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
                long FadeTime = (long)(CConfig.FadeTime * 1000);

                if ((_FadingTimer.ElapsedMilliseconds < FadeTime) && (CConfig.FadeTime > 0))
                {
                    long ms = 1;
                    if (_FadingTimer.ElapsedMilliseconds > 0)
                        ms = _FadingTimer.ElapsedMilliseconds;

                    float factor = (float)ms / FadeTime;

                    _GlobalAlpha = 1f;// -factor / 100f;
                    _ZOffset = CSettings.zFar/2;

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
                    if(CBackgroundMusic.Playing)
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
            {
                _PopupScreens[i].Draw();
            }

            _Cursor.Draw();
            DrawDebugInfos();

            return true;
        }

        public static void FadeTo(EScreens Screen)
        {
            if (Screen == EScreens.ScreenPartyDummy)
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
                
            _NextScreen = Screen;
        }
        
        public static void ShowPopup(EPopupScreens PopupScreen)
        {
            _PopupScreens[(int)PopupScreen].OnShow();
            _PopupScreens[(int)PopupScreen].OnShowFinish();
            _CurrentPopupScreen = PopupScreen;
        }

        public static void HidePopup(EPopupScreens PopupScreen)
        {
            if (_CurrentPopupScreen != PopupScreen)
                return;

            _PopupScreens[(int)PopupScreen].OnClose();
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
        private static bool HandleInputs(CKeys keys, CMouse Mouse)
        {
            KeyEvent KeyEvent = new KeyEvent();
            MouseEvent MouseEvent = new MouseEvent();
            KeyEvent InputKeyEvent = new KeyEvent();
            MouseEvent InputMouseEvent = new MouseEvent();

            bool PopupPlayerControlAllowed = _CurrentScreen != EScreens.ScreenOptionsRecord && _CurrentScreen != EScreens.ScreenSing &&
                (_CurrentScreen != EScreens.ScreenSong || (_CurrentScreen == EScreens.ScreenSong && !CSongs.IsInCategory && CConfig.Tabs ==EOffOn.TR_CONFIG_ON)) 
                && _CurrentScreen != EScreens.ScreenCredits && !CBackgroundMusic.Disabled;

            bool PopupVolumeControlAllowed = _CurrentScreen != EScreens.ScreenCredits && _CurrentScreen != EScreens.ScreenOptionsRecord;
            //Hide volume control for bg-music if bg-music is disabled
            if (PopupVolumeControlAllowed && (_CurrentScreen != EScreens.ScreenSong || (_CurrentScreen == EScreens.ScreenSong && CSongs.Category == -1)) 
                && _CurrentScreen != EScreens.ScreenSing && CConfig.BackgroundMusic == EOffOn.TR_CONFIG_OFF)
                PopupVolumeControlAllowed = false;


            bool Resume = true;
            bool EventsAvailable = false;
            bool InputEventsAvailable = CInput.PollKeyEvent(ref InputKeyEvent);

            while ((EventsAvailable = keys.PollEvent(ref KeyEvent)) || InputEventsAvailable)
            {
                if (!EventsAvailable)
                    KeyEvent = InputKeyEvent;

                if (KeyEvent.Key == Keys.Left || KeyEvent.Key == Keys.Right || KeyEvent.Key == Keys.Up || KeyEvent.Key == Keys.Down)
                {
                    CSettings.MouseInactive();
                    _Cursor.FadeOut();
                }
                
                if (PopupPlayerControlAllowed && KeyEvent.Key == Keys.Tab)
                {
                    if (_CurrentPopupScreen == EPopupScreens.NoPopup && CConfig.BackgroundMusic == EOffOn.TR_CONFIG_ON)
                        ShowPopup(EPopupScreens.PopupPlayerControl);
                    else
                        HidePopup(EPopupScreens.PopupPlayerControl);
                }

                if (PopupPlayerControlAllowed && CConfig.BackgroundMusic == EOffOn.TR_CONFIG_ON)
                {
                    if (KeyEvent.Key == Keys.MediaNextTrack)
                        CMain.BackgroundMusic.Next();
                    else if (KeyEvent.Key == Keys.MediaPreviousTrack)
                        CMain.BackgroundMusic.Previous();
                    else if (KeyEvent.Key == Keys.MediaPlayPause)
                    {
                        if (CMain.BackgroundMusic.IsPlaying())
                            CMain.BackgroundMusic.Pause();
                        else
                            CMain.BackgroundMusic.Play();
                    }
                }

                if (PopupVolumeControlAllowed)
                {
                    int Diff = 0;
                    if ((KeyEvent.ModSHIFT && (KeyEvent.Key == Keys.Add || KeyEvent.Key == Keys.PageUp)) || (KeyEvent.Sender == ESender.WiiMote && KeyEvent.Key == Keys.Add))
                        Diff = 5;
                    else if ((KeyEvent.ModSHIFT && (KeyEvent.Key == Keys.Subtract || KeyEvent.Key == Keys.PageDown)) || (KeyEvent.Sender == ESender.WiiMote && KeyEvent.Key == Keys.Subtract))
                        Diff = -5;

                    if (Diff != 0)
                    {
                        switch (CGraphics.CurrentScreen)
                        {
                            case EScreens.ScreenSong:
                                if (CSongs.IsInCategory)
                                {
                                    CConfig.PreviewMusicVolume += Diff;
                                    _Screens[(int)_CurrentScreen].ApplyVolume();
                                }
                                else
                                {
                                    CConfig.BackgroundMusicVolume += Diff;
                                    CBackgroundMusic.ApplyVolume();
                                }
                                break;

                            case EScreens.ScreenSing:
                                CConfig.GameMusicVolume += Diff;
                                _Screens[(int)_CurrentScreen].ApplyVolume();
                                break;

                            default:
                                CConfig.BackgroundMusicVolume += Diff;
                                CBackgroundMusic.ApplyVolume();
                                break;
                        }
                        CConfig.SaveConfig();
                    }

                    if (_CurrentPopupScreen == EPopupScreens.NoPopup && Diff != 0)
                    {
                        ShowPopup(EPopupScreens.PopupVolumeControl);
                        _VolumePopupTimer.Reset();
                        _VolumePopupTimer.Start();
                    }

                    CMain.BackgroundMusic.ApplyVolume();
                }

                if (KeyEvent.ModSHIFT && (KeyEvent.Key == Keys.F1))
                {
                    CSettings.GameState = EGameState.EditTheme;
                }
                else if (KeyEvent.ModALT && (KeyEvent.Key == Keys.Enter ))
                {
                    CSettings.bFullScreen = !CSettings.bFullScreen;
                }
                else if (KeyEvent.ModALT && (KeyEvent.Key == Keys.P))
                {
                    CDraw.MakeScreenShot();
                }
                else
                {
                    if (!_Fading)
                    {
                        bool handled = false;
                        if (_CurrentPopupScreen != EPopupScreens.NoPopup)
                            handled = _PopupScreens[(int)_CurrentPopupScreen].HandleInput(KeyEvent);
                        
                        if (!handled)
                            Resume &= _Screens[(int)_CurrentScreen].HandleInput(KeyEvent);
                    }
                }

                if (!EventsAvailable)
                    InputEventsAvailable = CInput.PollKeyEvent(ref InputKeyEvent);
            }

            InputEventsAvailable = CInput.PollMouseEvent(ref InputMouseEvent);

            while ((EventsAvailable = Mouse.PollEvent(ref MouseEvent)) || InputEventsAvailable)
            {
                if (!EventsAvailable)
                    MouseEvent = InputMouseEvent;

                if (MouseEvent.Wheel != 0)
                {
                    CSettings.MouseActive();
                    _Cursor.FadeIn();
                }

                UpdateMousePosition(MouseEvent.X, MouseEvent.Y);

                bool isOverPopupPlayerControl = CHelper.IsInBounds(_PopupScreens[(int)EPopupScreens.PopupPlayerControl].GetScreenArea(), MouseEvent);
                if (PopupPlayerControlAllowed && isOverPopupPlayerControl)
                {
                    if (_CurrentPopupScreen == EPopupScreens.NoPopup && CConfig.BackgroundMusic == EOffOn.TR_CONFIG_ON)
                        ShowPopup(EPopupScreens.PopupPlayerControl);
                }
                
                if (!isOverPopupPlayerControl && _CurrentPopupScreen == EPopupScreens.PopupPlayerControl)
                    HidePopup(EPopupScreens.PopupPlayerControl);

                bool isOverPopupVolumeControl = CHelper.IsInBounds(_PopupScreens[(int)EPopupScreens.PopupVolumeControl].GetScreenArea(), MouseEvent);
                if (PopupVolumeControlAllowed && isOverPopupVolumeControl)
                {
                    if (_CurrentPopupScreen == EPopupScreens.NoPopup)
                    {
                        ShowPopup(EPopupScreens.PopupVolumeControl);
                        _VolumePopupTimer.Reset();
                        _VolumePopupTimer.Start();
                    }
                }

                if (CursorOverVolumeControl && !isOverPopupVolumeControl)
                {
                    if (_CurrentPopupScreen == EPopupScreens.PopupVolumeControl)
                    {
                        HidePopup(EPopupScreens.PopupVolumeControl);
                        _VolumePopupTimer.Reset();
                    }
                }
                CursorOverVolumeControl = isOverPopupVolumeControl;


                bool handled = false;
                if (_CurrentPopupScreen != EPopupScreens.NoPopup)
                    handled = _PopupScreens[(int)_CurrentPopupScreen].HandleMouse(MouseEvent);

                if (handled && _CurrentPopupScreen == EPopupScreens.PopupVolumeControl)
                    _Screens[(int)_CurrentScreen].ApplyVolume();

                if (!handled && !_Fading && (_Cursor.IsActive || MouseEvent.LB || MouseEvent.RB || MouseEvent.MB))
                    Resume &= _Screens[(int)_CurrentScreen].HandleMouse(MouseEvent); 
              
                if (!EventsAvailable)
                    InputEventsAvailable = CInput.PollMouseEvent(ref InputMouseEvent);
            }
            return Resume;
        }

        private static bool HandleInputThemeEditor(CKeys keys, CMouse Mouse)
        {
            KeyEvent KeyEvent = new KeyEvent();
            MouseEvent MouseEvent = new MouseEvent();

            while (keys.PollEvent(ref KeyEvent))
            {
                if (KeyEvent.ModSHIFT && (KeyEvent.Key == Keys.F1))
                {
                    CSettings.GameState = EGameState.Normal;
                    _Screens[(int)_CurrentScreen].NextInteraction();
                }
                else if (KeyEvent.ModALT && (KeyEvent.Key == Keys.Enter))
                {
                    CSettings.bFullScreen = !CSettings.bFullScreen;
                }
                else if (KeyEvent.ModALT && (KeyEvent.Key == Keys.P))
                {
                    CDraw.MakeScreenShot();
                }
                else
                {
                    if (!_Fading)
                        _Screens[(int)_CurrentScreen].HandleInputThemeEditor(KeyEvent);
                }
            }

            while (Mouse.PollEvent(ref MouseEvent))
            {
                if (!_Fading)
                    _Screens[(int)_CurrentScreen].HandleMouseThemeEditor(MouseEvent);

                UpdateMousePosition(MouseEvent.X, MouseEvent.Y); 
            }
            return true;
        }

        private static void UpdateMousePosition(int x, int y)
        {
            _Cursor.UpdatePosition(x, y);
        }

        private static bool Update()
        {
            if(_VolumePopupTimer.IsRunning && _VolumePopupTimer.ElapsedMilliseconds >= 1500 && _CurrentPopupScreen == EPopupScreens.PopupVolumeControl)
            {
                _VolumePopupTimer.Reset();
                HidePopup(EPopupScreens.PopupVolumeControl);
            }

            if (_VolumePopupTimer.IsRunning)
            {
                _Cursor.FadeIn();
            }

            if (_CurrentPopupScreen != EPopupScreens.NoPopup)
                _PopupScreens[(int)_CurrentPopupScreen].UpdateGame();
            return _Screens[(int)_CurrentScreen].UpdateGame();
        }

        private static void DrawDebugInfos()
        {

            string txt = String.Empty;
            CFonts.Style = EStyle.Normal;
            CFonts.SetFont("Normal");
            SColorF Gray = new SColorF(1f, 1f, 1f, 0.5f);

            float dy = 0;
            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_ONLY_FPS)
            {
                txt = CTime.GetFPS().ToString("FPS: 000");
                CFonts.Height = 30f;
                RectangleF rect = new RectangleF(CSettings.iRenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(Gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.zNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.zNear);
                dy += rect.Height;
            }

            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL1)
            {
                txt = CSound.GetStreamCount().ToString(CLanguage.Translate("TR_DEBUG_AUDIO_STREAMS") + ": 00");

                RectangleF rect = new RectangleF(CSettings.iRenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(Gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.zNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.zNear);
                dy += rect.Height;
            }

            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL1)
            {
                txt = CVideo.GetNumStreams().ToString(CLanguage.Translate("TR_DEBUG_VIDEO_STREAMS") + ": 00");

                RectangleF rect = new RectangleF(CSettings.iRenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(Gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.zNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.zNear);
                dy += rect.Height;
            }

            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL1)
            {
                txt = CDraw.TextureCount().ToString(CLanguage.Translate("TR_DEBUG_TEXTURES") + ": 00000");

                RectangleF rect = new RectangleF(CSettings.iRenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(Gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.zNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.zNear);
                dy += rect.Height;
            }

            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL1)
            {
                long memory = GC.GetTotalMemory(false);
                txt = (memory / 1000000L).ToString(CLanguage.Translate("TR_DEBUG_MEMORY") + ": 00000 MB");

                RectangleF rect = new RectangleF(CSettings.iRenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(Gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.zNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.zNear);
                dy += rect.Height;
            }

            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL2)
            {
                txt = CSound.RecordGetToneAbs(0).ToString(CLanguage.Translate("TR_DEBUG_TONE_ABS") + " P1: 00");

                RectangleF rect = new RectangleF(CSettings.iRenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(Gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.zNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.zNear);
                dy += rect.Height;


                txt = CSound.RecordGetMaxVolume(0).ToString(CLanguage.Translate("TR_DEBUG_MAX_VOLUME") + " P1: 0.000");

                rect = new RectangleF(CSettings.iRenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(Gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.zNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.zNear);
                dy += rect.Height;

                txt = CSound.RecordGetToneAbs(1).ToString(CLanguage.Translate("TR_DEBUG_TONE_ABS") + " P2: 00");

                rect = new RectangleF(CSettings.iRenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(Gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.zNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.zNear);
                dy += rect.Height;


                txt = CSound.RecordGetMaxVolume(1).ToString(CLanguage.Translate("TR_DEBUG_MAX_VOLUME") + " P2: 0.000");

                rect = new RectangleF(CSettings.iRenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(Gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.zNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.zNear);
                dy += rect.Height;
            }

            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL3)
            {
                txt = CSongs.NumSongsWithCoverLoaded.ToString(CLanguage.Translate("TR_DEBUG_SONGS") + ": 00000");

                RectangleF rect = new RectangleF(CSettings.iRenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(Gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.zNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.zNear);
                dy += rect.Height;
            }

            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL_MAX)
            {
                txt = _Cursor.X.ToString(CLanguage.Translate("TR_DEBUG_MOUSE") + " : (0000/") + _Cursor.Y.ToString("0000)");

                RectangleF rect = new RectangleF(CSettings.iRenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(Gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.zNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.zNear);
                dy += rect.Height;
            }
        }

        private static void ReloadCursor()
        {
            _Cursor.UnloadTextures();

            if (CTheme.Cursor.color.Length > 0)
            {
                SColorF color;
                color = CTheme.GetColor(CTheme.Cursor.color, -1);
                CTheme.Cursor.r = color.R;
                CTheme.Cursor.g = color.G;
                CTheme.Cursor.b = color.B;
                CTheme.Cursor.a = color.A;
            }

            _Cursor = new CCursor(
                CTheme.Cursor.SkinName,
                new SColorF(CTheme.Cursor.r, CTheme.Cursor.g, CTheme.Cursor.b, CTheme.Cursor.a),
                CTheme.Cursor.w,
                CTheme.Cursor.h,
                CSettings.zNear);
        }
        #endregion private stuff
    }
}
