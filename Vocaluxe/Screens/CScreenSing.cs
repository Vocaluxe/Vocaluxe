using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;
using Vocaluxe.Menu.SingNotes;
using Vocaluxe.Menu.SongMenu;

namespace Vocaluxe.Screens
{
    class CScreenSing : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 6;

        struct TimeRect
        {
            public CStatic rect;
            public float startBeat;
            public float endBeat;
        }
        
        private const string TextSongName = "TextSongName";
        private const string TextTime = "TextTime";
        private const string TextPause = "TextPause";
        private const string TextDuetName1 = "TextDuetName1";
        private const string TextDuetName2 = "TextDuetName2";
        private string[,] TextScores;
        private string[,] TextNames;

        private const string StaticSongText = "StaticSongText";
        private const string StaticLyrics = "StaticLyrics";
        private const string StaticLyricsDuet = "StaticLyricsDuet";
        private const string StaticLyricsTop = "StaticLyricsTop";
        private const string StaticTimeBar = "StaticTimeBar";
        private const string StaticTimeLine = "StaticTimeLine";
        private const string StaticTimeLineExpandedNormal = "StaticTimeLineExpandedNormal";
        private const string StaticTimeLineExpandedHighlighted = "StaticTimeLineExpandedHighlighted";
        private const string StaticTimePointer = "StaticTimePointer";
        private const string StaticLyricHelper = "StaticLyricHelper";
        private const string StaticLyricHelperDuet = "StaticLyricHelperDuet";
        private const string StaticLyricHelperTop = "StaticLyricHelperTop";
        private const string StaticPauseBG = "StaticPauseBG";

        private string[,] StaticScores;
        private string[,] StaticAvatars;

        private const string ButtonCancel = "ButtonCancel";
        private const string ButtonContinue = "ButtonContinue";
        private const string ButtonSkip = "ButtonSkip";

        private const string LyricMain = "LyricMain";
        private const string LyricSub = "LyricSub";
        private const string LyricMainDuet = "LyricMainDuet";
        private const string LyricSubDuet = "LyricSubDuet";
        private const string LyricMainTop = "LyricMainTop";
        private const string LyricSubTop = "LyricSubTop";

        private const string SingBars = "SingBars";

        private SRectF _TimeLineRect;
        private List<TimeRect> _TimeRects;
        private bool _FadeOut = false;

        private int _CurrentBeat;
        private int _CurrentStream = -1;
        //private int _NextStream = -1;
        private float _Volume = 100f;
        private int _CurrentVideo = -1;
        private EAspect _VideoAspect = EAspect.Crop;
        private STexture _CurrentVideoTexture = new STexture(-1);
        private STexture _CurrentWebcamFrameTexture = new STexture(-1);
        private STexture _Background = new STexture(-1);

        private float _CurrentTime = 0f;
        private float _FinishTime = 0f;

        private float _TimeToFirstNote = 0f;
        private float _RemainingTimeToFirstNote = 0f;
        private float _TimeToFirstNoteDuet = 0f;
        private float _RemainingTimeToFirstNoteDuet = 0f;

        private int[] NoteLines = new int[CSettings.MaxNumPlayer];

        private Stopwatch _TimerSongText;
        private Stopwatch _TimerDuetText1;
        private Stopwatch _TimerDuetText2;

        private bool _Pause;
        private bool _Webcam;

        public CScreenSing()
        {
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "ScreenSing";
            _ScreenVersion = ScreenVersion;

            List<string> texts = new List<string>();
            texts.Add(TextSongName);
            texts.Add(TextTime);
            texts.Add(TextPause);
            texts.Add(TextDuetName1);
            texts.Add(TextDuetName2);
            BuildTextStrings(ref texts);
            _ThemeTexts = texts.ToArray();

            List<string> statics = new List<string>();
            statics.Add(StaticSongText);
            statics.Add(StaticLyrics);
            statics.Add(StaticLyricsDuet);
            statics.Add(StaticLyricsTop);
            statics.Add(StaticTimeBar);
            statics.Add(StaticTimeLine);
            statics.Add(StaticTimeLineExpandedNormal);
            statics.Add(StaticTimeLineExpandedHighlighted);
            statics.Add(StaticTimePointer);
            statics.Add(StaticLyricHelper);
            statics.Add(StaticLyricHelperDuet);
            statics.Add(StaticLyricHelperTop);
            statics.Add(StaticPauseBG);
            BuildStaticStrings(ref statics);
            _ThemeStatics = statics.ToArray();

            List<string> buttons = new List<string>();
            buttons.Add(ButtonCancel);
            buttons.Add(ButtonContinue);
            buttons.Add(ButtonSkip);
            _ThemeButtons = buttons.ToArray();

            _ThemeLyrics = new string[] { LyricMain, LyricSub, LyricMainDuet, LyricSubDuet, LyricMainTop, LyricSubTop };
            _ThemeSingNotes = new string[] { SingBars };

            _TimeRects = new List<TimeRect>();
            _TimerSongText = new Stopwatch();
            _TimerDuetText1 = new Stopwatch();
            _TimerDuetText2 = new Stopwatch();
        }

        public override void LoadTheme(string XmlPath)
        {
            base.LoadTheme(XmlPath);

            Statics[htStatics(StaticTimeLine)].Visible = false;
            Statics[htStatics(StaticTimeLineExpandedNormal)].Visible = false;
            Statics[htStatics(StaticTimeLineExpandedHighlighted)].Visible = false;
            Statics[htStatics(StaticPauseBG)].Visible = false;
            Texts[htTexts(TextPause)].Visible = false;

            Statics[htStatics(StaticPauseBG)].Visible = false;
            Texts[htTexts(TextPause)].Visible = false;

            Buttons[htButtons(ButtonCancel)].Visible = false;
            Buttons[htButtons(ButtonContinue)].Visible = false;
        }

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            base.HandleInput(KeyEvent);

            if (KeyEvent.KeyPressed)
            {
                //
            }
            else
            {
                switch (KeyEvent.Key)
                {
                    case Keys.Escape:
                        TogglePause();
                        if (_Pause)
                            SetInteractionToButton(Buttons[htButtons(ButtonCancel)]);
                        break;

                    case Keys.P:
                        TogglePause();
                        if (_Pause)
                            SetInteractionToButton(Buttons[htButtons(ButtonContinue)]);
                        break;

                    case Keys.T:
                        int mode = (int)CConfig.TimerMode;

                        mode++;
                        if (mode > Enum.GetNames(typeof(ETimerMode)).Length - 1)
                        {
                            mode = 0;
                        }
                        CConfig.TimerMode = (ETimerMode)mode;
                        break;

                    case Keys.I:
                        mode = (int)CConfig.PlayerInfo;

                        mode++;
                        if (mode > Enum.GetNames(typeof(EPlayerInfo)).Length - 1)
                        {
                            mode = 0;
                        }
                        CConfig.PlayerInfo = (EPlayerInfo)mode;
                        CConfig.SaveConfig();
                        SetVisibility();
                        break;

                    case Keys.S:
                        if(CGame.NumRounds > CGame.RoundNr)
                            if(KeyEvent.ModCTRL)
                                LoadNextSong();
                        break;
                    case Keys.W:
                        if (CWebcam.GetDevices().Length > 0)
                        {
                            _Webcam = !_Webcam;
                            if (_Webcam)
                                CWebcam.Start();
                            else
                                CWebcam.Stop();
                        }
                        break;
                    case Keys.Enter:
                        if (Buttons[htButtons(ButtonContinue)].Selected && _Pause)
                            TogglePause();
                        if (Buttons[htButtons(ButtonCancel)].Selected && _Pause)
                            Stop();
                        if (Buttons[htButtons(ButtonSkip)].Selected && _Pause)
                        {
                            LoadNextSong();
                            TogglePause();
                        }
                        break;

                    case Keys.Add:
                    case Keys.PageUp:
                        if (KeyEvent.ModSHIFT)
                        {
                            CConfig.GameMusicVolume = CConfig.GameMusicVolume + 5;
                            if (CConfig.GameMusicVolume > 100)
                                CConfig.GameMusicVolume = 100;
                            CConfig.SaveConfig();
                            ApplyVolume();
                        }
                        break;

                    case Keys.Subtract:
                    case Keys.PageDown:
                        if (KeyEvent.ModSHIFT)
                        {
                            CConfig.GameMusicVolume = CConfig.GameMusicVolume - 5;
                            if (CConfig.GameMusicVolume < 0)
                                CConfig.GameMusicVolume = 0;
                            CConfig.SaveConfig();
                            ApplyVolume();
                        }
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
                TogglePause();
                if (_Pause)
                    SetInteractionToButton(Buttons[htButtons(ButtonContinue)]);
            }

            if (MouseEvent.LB && IsMouseOver(MouseEvent) && _Pause)
            {
                if (Buttons[htButtons(ButtonContinue)].Selected && _Pause)
                    TogglePause();

                if (Buttons[htButtons(ButtonCancel)].Selected && _Pause)
                    Stop();

                if (Buttons[htButtons(ButtonSkip)].Selected && _Pause)
                {
                    LoadNextSong();
                    TogglePause();
                }
            }


            return true;
        }

        public override bool UpdateGame()
        {
            bool Finish = false;
            if (CSound.IsPlaying(_CurrentStream) || CSound.IsPaused(_CurrentStream))
            {
                _CurrentTime = CSound.GetPosition(_CurrentStream);

                if (_FinishTime != 0 && _CurrentTime >= _FinishTime)
                    Finish = true;
            }
            else
                Finish = true;

            if (Finish)
            {
                LoadNextSong();
            }

            UpdateSongText();
            UpdateDuetText();
            if (_FadeOut)
                return true;

            UpdateTimeLine();

            CGame.UpdatePoints(_CurrentTime);
            UpdateLyrics();

            float[] Alpha = CalcFadingAlpha();
            if (Alpha != null)
            {
                Lyrics[htLyrics(LyricMain)].Alpha = Alpha[0];
                Lyrics[htLyrics(LyricSub)].Alpha = Alpha[1];

                Lyrics[htLyrics(LyricMainTop)].Alpha = Alpha[0];
                Lyrics[htLyrics(LyricSubTop)].Alpha = Alpha[1];

                Statics[htStatics(StaticLyrics)].Alpha = Alpha[0];
                Statics[htStatics(StaticLyricsTop)].Alpha = Alpha[0];

                Statics[htStatics(StaticLyricHelper)].Alpha = Alpha[0];
                Statics[htStatics(StaticLyricHelperTop)].Alpha = Alpha[0];

                for (int p = 0; p < CGame.NumPlayer; p++)
                {
                    SingNotes[htSingNotes(SingBars)].SetAlpha(NoteLines[p], Alpha[CGame.Player[p].LineNr * 2]);
                    if (CConfig.FadePlayerInfo == EFadePlayerInfo.TR_CONFIG_FADEPLAYERINFO_INFO || CConfig.FadePlayerInfo == EFadePlayerInfo.TR_CONFIG_FADEPLAYERINFO_ALL)
                    {
                        Statics[htStatics(StaticAvatars[p, CGame.NumPlayer - 1])].Alpha = Alpha[CGame.Player[p].LineNr * 2];
                        Texts[htTexts(TextNames[p, CGame.NumPlayer - 1])].Alpha = Alpha[CGame.Player[p].LineNr * 2];
                    }
                    if (CConfig.FadePlayerInfo == EFadePlayerInfo.TR_CONFIG_FADEPLAYERINFO_ALL)
                    {
                        Statics[htStatics(StaticScores[p, CGame.NumPlayer - 1])].Alpha = Alpha[CGame.Player[p].LineNr * 2];
                        Statics[htStatics(StaticAvatars[p, CGame.NumPlayer - 1])].Alpha = Alpha[CGame.Player[p].LineNr * 2];
                        Texts[htTexts(TextNames[p, CGame.NumPlayer - 1])].Alpha = Alpha[CGame.Player[p].LineNr * 2];
                        Texts[htTexts(TextScores[p, CGame.NumPlayer - 1])].Alpha = Alpha[CGame.Player[p].LineNr * 2];
                    }
                }

                if (Alpha.Length > 2)
                {
                    Lyrics[htLyrics(LyricMainDuet)].Alpha = Alpha[0];
                    Lyrics[htLyrics(LyricSubDuet)].Alpha = Alpha[1];

                    Statics[htStatics(StaticLyricsDuet)].Alpha = Alpha[0];
                    Statics[htStatics(StaticLyricHelperDuet)].Alpha = Alpha[0];

                    Lyrics[htLyrics(LyricMain)].Alpha = Alpha[2];
                    Lyrics[htLyrics(LyricSub)].Alpha = Alpha[3];

                    Statics[htStatics(StaticLyrics)].Alpha = Alpha[2];
                    Statics[htStatics(StaticLyricHelper)].Alpha = Alpha[2];
                }
            }


            for (int p = 0; p < CGame.NumPlayer; p++)
            {
                if (CGame.Player[p].Points < 10000)
                {
                    Texts[htTexts(TextScores[p, CGame.NumPlayer - 1])].Text = CGame.Player[p].Points.ToString("0000");
                }
                else
                {
                    Texts[htTexts(TextScores[p, CGame.NumPlayer - 1])].Text = CGame.Player[p].Points.ToString("00000");
                }
            }

            if (_CurrentVideo != -1 && !_FadeOut && CConfig.VideosInSongs == EOffOn.TR_CONFIG_ON)
            {
                float vtime = 0f;
                CVideo.VdGetFrame(_CurrentVideo, ref _CurrentVideoTexture, _CurrentTime, ref vtime);
            }

            if (_Webcam)
                CWebcam.GetFrame(ref _CurrentWebcamFrameTexture);

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            _FadeOut = false;

            _CurrentVideo = -1;
            _CurrentVideoTexture = new STexture(-1);
            _CurrentWebcamFrameTexture = new STexture(-1);
            _CurrentBeat = -100;
            _CurrentTime = 0f;
            _FinishTime = 0f;
            _TimeToFirstNote = 0f;
            _TimeToFirstNoteDuet = 0f;
            _Pause = false;

            _TimeRects.Clear();

            SingNotes[htSingNotes(SingBars)].Reset();
            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
            {
                NoteLines[i] = -1;
            }

            for (int i = 0; i < Lyrics.Length; i++)
            {
                Lyrics[i].LyricStyle = CConfig.LyricStyle;
            }

            for (int p = 0; p < CGame.NumPlayer; p++)
            {
                Statics[htStatics(StaticAvatars[p, CGame.NumPlayer - 1])].Aspect = EAspect.Crop;
            }
            SetVisibility();

            UpdateAvatars();
            UpdateNames();

            CBackgroundMusic.Disabled = true;
            CloseSong();
        }

        public override void OnShowFinish()
        {
            base.OnShowFinish();

            CGame.Start();
            LoadNextSong();
            CBackgroundMusic.Disabled = true;
        }

        public override bool Draw()
        {
            if (_Active)
            {
                if (_CurrentVideo != -1 && CConfig.VideosInSongs == EOffOn.TR_CONFIG_ON && !_Webcam)
                {
                    RectangleF bounds = new RectangleF(0, 0, CSettings.iRenderW, CSettings.iRenderH);
                    RectangleF rect = new RectangleF(0f, 0f, _CurrentVideoTexture.width, _CurrentVideoTexture.height);
                    CHelper.SetRect(bounds, ref rect, rect.Width / rect.Height, _VideoAspect);

                    CDraw.DrawTexture(_CurrentVideoTexture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, 0f),
                        _CurrentVideoTexture.color, new SRectF(bounds.X, bounds.Y, bounds.Width, bounds.Height, 0f), false);
                }
                else if (_Webcam)
                {
                    RectangleF bounds = new RectangleF(0, 0, CSettings.iRenderW, CSettings.iRenderH);
                    RectangleF rect = new RectangleF(0f, 0f, _CurrentWebcamFrameTexture.width, _CurrentWebcamFrameTexture.height);
                    CHelper.SetRect(bounds, ref rect, rect.Width / rect.Height, _VideoAspect);

                    CDraw.DrawTexture(_CurrentWebcamFrameTexture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, 0f),
                        _CurrentVideoTexture.color, new SRectF(bounds.X, bounds.Y, bounds.Width, bounds.Height, 0f), false);
                }
                else
                {   // Draw Background
                    RectangleF bounds = new RectangleF(0, 0, CSettings.iRenderW, CSettings.iRenderH);
                    RectangleF rect = new RectangleF(0f, 0f, _Background.width, _Background.height);
                    CHelper.SetRect(bounds, ref rect, rect.Width / rect.Height, EAspect.Crop);

                    CDraw.DrawTexture(_Background, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, 0f),
                        _Background.color, new SRectF(bounds.X, bounds.Y, bounds.Width, bounds.Height, 0f), false);
                }
            }

            base.DrawBG();

            foreach (CStatic stat in Statics)
                stat.Draw();

            foreach (CText text in Texts)
                text.Draw();

            switch (CConfig.TimerLook)
            {
                case ETimerLook.TR_CONFIG_TIMERLOOK_NORMAL:
                    CDraw.DrawTexture(Statics[htStatics(StaticTimeLine)].Texture, Statics[htStatics(StaticTimeLine)].Rect, new SColorF(1, 1, 1, 1), _TimeLineRect);
                    break;
                case ETimerLook.TR_CONFIG_TIMERLOOK_EXPANDED:
                    for (int i = 0; i < _TimeRects.Count; i++)
                    {
                        CDraw.DrawTexture(_TimeRects[i].rect.Texture, Statics[htStatics(StaticTimeLine)].Rect, _TimeRects[i].rect.Color, _TimeRects[i].rect.Rect);
                    }
                    break;
            }

            Lyrics[htLyrics(LyricSub)].Draw(-100);
            Lyrics[htLyrics(LyricMain)].Draw(CGame.Beat);

            Lyrics[htLyrics(LyricSubDuet)].Draw(-100);
            Lyrics[htLyrics(LyricMainDuet)].Draw(CGame.Beat);

            Lyrics[htLyrics(LyricSubTop)].Draw(-100);
            Lyrics[htLyrics(LyricMainTop)].Draw(CGame.Beat);


            for (int i = 0; i < CGame.NumPlayer; i++)
            {
                SingNotes[htSingNotes(SingBars)].Draw(NoteLines[i], CGame.Player[i].SingLine, i);
            }

            DrawLyricHelper();

            if (_Pause)
            {
                Statics[htStatics(StaticPauseBG)].ForceDraw();
                Texts[htTexts(TextPause)].ForceDraw();

                foreach (CButton button in Buttons)
                    button.Draw();

                foreach (CSelectSlide slide in SelectSlides)
                    slide.Draw();
            }

            return true;
        }

        public override void OnClose()
        {
            base.OnClose();
            CBackgroundMusic.Disabled = false;
            CloseSong();
        }

        public override void ApplyVolume()
        {
            CSound.SetStreamVolumeMax(_CurrentStream, CConfig.GameMusicVolume);
        }

        private void CloseSong()
        {
            CSound.FadeAndStop(_CurrentStream, 0f, 0.5f);
            CSound.RecordStop();
            if (_CurrentVideo != -1)
            {
                CVideo.VdClose(_CurrentVideo);
                _CurrentVideo = -1;
                CDraw.RemoveTexture(ref _CurrentVideoTexture);
            }
            CDraw.RemoveTexture(ref _Background);

            Lyrics[htLyrics(LyricMain)].Clear();
            Lyrics[htLyrics(LyricSub)].Clear();
            Lyrics[htLyrics(LyricMainDuet)].Clear();
            Lyrics[htLyrics(LyricSubDuet)].Clear();
            Lyrics[htLyrics(LyricMainTop)].Clear();
            Lyrics[htLyrics(LyricSubTop)].Clear();
            Texts[htTexts(TextSongName)].Text = String.Empty;
            Texts[htTexts(TextDuetName1)].Text = String.Empty;
            Texts[htTexts(TextDuetName2)].Text = String.Empty;
            GC.Collect();
        }

        private void LoadNextSong()
        {
            CloseSong();

            CGame.NextRound();

            if (CGame.IsFinished())
            {
                FinishedSinging();
                return;
            }

            CSong song = CGame.GetSong();

            if (song == null)
            {
                CLog.LogError("Critical Error! ScreenSing.LoadNextSong() song is null!");
                return;
            }

            if (!song.NotesLoaded)
                song.ReadNotes();

            string songname = song.Artist + " - " + song.Title;
            int rounds = CGame.GetNumSongs();
            if (rounds > 1)
                songname += " (" + CGame.RoundNr + "/" + rounds.ToString() + ")";
            Texts[htTexts(TextSongName)].Text = songname;

            _CurrentStream = CSound.Load(song.GetMP3(), true);
            CSound.SetStreamVolumeMax(_CurrentStream, CConfig.GameMusicVolume);
            CSound.SetStreamVolume(_CurrentStream, _Volume);
            CSound.SetPosition(_CurrentStream, song.Start);
            _CurrentTime = song.Start;
            _FinishTime = song.Finish;
            _TimeToFirstNote = 0f;
            _TimeToFirstNoteDuet = 0f;
            int[] duet_player = new int[CGame.NumPlayer];
            if (song.IsDuet)
            {
                //Save duet-assignment before resetting
                for (int i = 0; i < duet_player.Length; i++)
                {
                    duet_player[i] = CGame.Player[i].LineNr;
                }
            }
            CGame.ResetPlayer();

            CDraw.RemoveTexture(ref _CurrentVideoTexture);

            if (song.VideoFileName != String.Empty)
            {
                _CurrentVideo = CVideo.VdLoad(Path.Combine(song.Folder, song.VideoFileName));
                CVideo.VdSkip(_CurrentVideo, song.Start, song.VideoGap);
                _VideoAspect = song.VideoAspect;
            }

            CDraw.RemoveTexture(ref _Background);
            if (song.BackgroundFileName != String.Empty)
                _Background = CDraw.AddTexture(Path.Combine(song.Folder, song.BackgroundFileName));

            SingNotes[htSingNotes(SingBars)].Reset();

            bool LyricsOnTop = (CGame.NumPlayer != 1) && CConfig.LyricsOnTop == EOffOn.TR_CONFIG_ON;
            if (song.IsDuet)
            {
                Texts[htTexts(TextDuetName1)].Text = song.DuetPart1;
                Texts[htTexts(TextDuetName2)].Text = song.DuetPart2;
                //More than one song: Player is not assigned to line by user
                //Otherwise, this is done by CScreenNames
                if (CGame.GetNumSongs() > 1)
                {
                    for (int i = 0; i < CGame.NumPlayer; i++)
                    {
                        if ((i % 2) == 0)
                            CGame.Player[i].LineNr = 1;
                        else
                            CGame.Player[i].LineNr = 0;
                    }
                }
                else
                {
                    for (int i = 0; i < CGame.NumPlayer; i++)
                    {
                        CGame.Player[i].LineNr = duet_player[i];
                    }
                }
                Statics[htStatics(StaticLyricsDuet)].Visible = true;
                Lyrics[htLyrics(LyricMainDuet)].Visible = true;
                Lyrics[htLyrics(LyricSubDuet)].Visible = true;

                Lyrics[htLyrics(LyricMainTop)].Visible = false;
                Lyrics[htLyrics(LyricSubTop)].Visible = false;
                Statics[htStatics(StaticLyricsTop)].Visible = false;
            }
            else
            {
                Statics[htStatics(StaticLyricsDuet)].Visible = false;
                Lyrics[htLyrics(LyricMainDuet)].Visible = false;
                Lyrics[htLyrics(LyricSubDuet)].Visible = false;

                Lyrics[htLyrics(LyricMainTop)].Visible = LyricsOnTop;
                Lyrics[htLyrics(LyricSubTop)].Visible = LyricsOnTop;
                Statics[htStatics(StaticLyricsTop)].Visible = LyricsOnTop;
            }

            for (int p = 0; p < CGame.NumPlayer; p++)
            {
                NoteLines[p] = SingNotes[htSingNotes(SingBars)].AddPlayer(
                    SingNotes[htSingNotes(SingBars)].BarPos[p, CGame.NumPlayer - 1],
                    CTheme.GetPlayerColor(p + 1),
                    p);
            }

            /*
                case 4:
                    NoteLines[0] = SingNotes[htSingNotes(SingBars)].AddPlayer(new SRectF(35f, 100f, 590f, 200f, -0.5f), CTheme.ThemeColors.Player[0]);
                    NoteLines[1] = SingNotes[htSingNotes(SingBars)].AddPlayer(new SRectF(35f, 350f, 590f, 200f, -0.5f), CTheme.ThemeColors.Player[1]);
                    NoteLines[2] = SingNotes[htSingNotes(SingBars)].AddPlayer(new SRectF(640f, 100f, 590f, 200f, -0.5f), CTheme.ThemeColors.Player[2]);
                    NoteLines[3] = SingNotes[htSingNotes(SingBars)].AddPlayer(new SRectF(640f, 350f, 590f, 200f, -0.5f), CTheme.ThemeColors.Player[3]);
                    break;
            */


            _TimerSongText.Stop();
            _TimerSongText.Reset();
            _TimerDuetText1.Stop();
            _TimerDuetText1.Reset();
            _TimerDuetText2.Stop();
            _TimerDuetText2.Reset();

            if (song.Notes.Lines.Length != 2)
            {
                _TimerSongText.Start();
            }

            StartSong();
        }

        private void StartSong()
        {
            PrepareTimeLine();
            CSound.Play(_CurrentStream);
            CSound.RecordStart();
            if(_Webcam)
                CWebcam.Start();
        }

        private void Stop()
        {
            //Need this to set other songs to points-var
            while(!CGame.IsFinished())
                CGame.NextRound();

            FinishedSinging();
        }

        private void FinishedSinging()
        {
            _FadeOut = true;
            CParty.FinishedSinging();

            if (_Webcam)
                CWebcam.Close();
        }

        private int FindCurrentLine(CLines lines, CLine[] line, CSong song)
        {
            float CurrentTime = _CurrentTime - song.Gap;
            //We are only interested in the last matching line, so either do not check further after line[j].StartBeat > _CurrentBeat or go backwards!
            int j = lines.FindPreviousLine(_CurrentBeat);
            for (; j >= 0; j--)
            {
                float FirstNoteTime = CGame.GetTimeFromBeats(line[j].FirstNoteBeat, song.BPM);
                //Earlist possible line break is 10s before first note
                if (FirstNoteTime <= CurrentTime + 10f)
                {
                    //First line has no predecessor or line has to be shown
                    if(j == 0 || FirstNoteTime - CConfig.MinLineBreakTime <= CurrentTime) return j;
                    float LastNoteTime = CGame.GetTimeFromBeats(line[j-1].LastNoteBeat, song.BPM);
                    //No line break if last line is not fully evaluated (with 50% tolerance -> tested so notes are drawn)
                    if(LastNoteTime + CConfig.MicDelay/1000f * 1.5f >= CurrentTime) return j-1;
                    return j;
                }
            }
            return -1;
        }

        private void UpdateLyrics()
        {
            if (_FadeOut)
                return;

            CSong song = CGame.GetSong();

            if (song == null)
                return;


            _CurrentBeat = CGame.CurrentBeat;
            for (int i = 0; i < song.Notes.LinesCount; i++)
            {
                if (i > 1)
                    break; // for later

                CLines lines = song.Notes.GetLines(i);
                CLine[] line = lines.Line;

                // find current line
                int nr = FindCurrentLine(lines, line, song);

                if (nr != -1)
                {
                    for (int j = 0; j < CGame.NumPlayer; j++)
                    {
                        if (CGame.Player[j].LineNr == i)
                            SingNotes[htSingNotes(SingBars)].AddLine(NoteLines[j], line, nr, j);
                    }

                    if (i == 0 && !song.IsDuet || i == 1 && song.IsDuet)
                    {
                        Lyrics[htLyrics(LyricMain)].SetLine(line[nr]);
                        Lyrics[htLyrics(LyricMainTop)].SetLine(line[nr]);
                        _TimeToFirstNote = CGame.GetTimeFromBeats(line[nr].FirstNoteBeat - line[nr].StartBeat, song.BPM);
                        _RemainingTimeToFirstNote = CGame.GetTimeFromBeats(line[nr].FirstNoteBeat - CGame.GetBeatFromTime(_CurrentTime, song.BPM, song.Gap), song.BPM);

                        if (line.Length >= nr + 2)
                        {
                            Lyrics[htLyrics(LyricSub)].SetLine(line[nr + 1]);
                            Lyrics[htLyrics(LyricSubTop)].SetLine(line[nr + 1]);
                        }
                        else
                        {
                            Lyrics[htLyrics(LyricSub)].Clear();
                            Lyrics[htLyrics(LyricSubTop)].Clear();
                        }
                    }
                    if (i == 0 && song.IsDuet)
                    {
                        Lyrics[htLyrics(LyricMainDuet)].SetLine(line[nr]);
                        _TimeToFirstNoteDuet = CGame.GetTimeFromBeats(line[nr].FirstNoteBeat - line[nr].StartBeat, song.BPM);
                        _RemainingTimeToFirstNoteDuet = CGame.GetTimeFromBeats(line[nr].FirstNoteBeat - CGame.GetBeatFromTime(_CurrentTime, song.BPM, song.Gap), song.BPM);

                        if (line.Length >= nr + 2)
                            Lyrics[htLyrics(LyricSubDuet)].SetLine(line[nr + 1]);
                        else
                            Lyrics[htLyrics(LyricSubDuet)].Clear();
                    }
                }
                else
                {
                    if (i == 0 && !song.IsDuet || i == 1 && song.IsDuet)
                    {
                        Lyrics[htLyrics(LyricMain)].Clear();
                        Lyrics[htLyrics(LyricSub)].Clear();
                        Lyrics[htLyrics(LyricMainTop)].Clear();
                        Lyrics[htLyrics(LyricSubTop)].Clear();
                        _TimeToFirstNote = 0f;
                    }

                    if (i == 0 && song.IsDuet)
                    {
                        Lyrics[htLyrics(LyricMainDuet)].Clear();
                        Lyrics[htLyrics(LyricSubDuet)].Clear();
                        _TimeToFirstNoteDuet = 0f;
                    }
                }
            }
        }

        private void TogglePause()
        {
            _Pause = !_Pause;
            if (_Pause)
            {
                Buttons[htButtons(ButtonCancel)].Visible = true;
                Buttons[htButtons(ButtonContinue)].Visible = true;
                if (CGame.NumRounds > CGame.RoundNr && CGame.NumRounds > 1)
                    Buttons[htButtons(ButtonSkip)].Visible = true;
                else
                    Buttons[htButtons(ButtonSkip)].Visible = false;
                CSound.Pause(_CurrentStream);               
            }else
            {
                Buttons[htButtons(ButtonCancel)].Visible = false;
                Buttons[htButtons(ButtonContinue)].Visible = false;
                Buttons[htButtons(ButtonSkip)].Visible = false;
                CSound.Play(_CurrentStream);
                CWebcam.Start();
            }
        }

        private void BuildTextStrings(ref List<string> texts)
        {
            TextScores = new string[CSettings.MaxNumPlayer, CSettings.MaxNumPlayer];
            TextNames = new string[CSettings.MaxNumPlayer, CSettings.MaxNumPlayer];

            for (int numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (int player = 0; player < CSettings.MaxNumPlayer; player++)
                {
                    if (player <= numplayer)
                    {
                        string target = "P" + (player + 1).ToString() + "N" + (numplayer + 1).ToString();
                        TextScores[player, numplayer] = "TextScore" + target;
                        TextNames[player, numplayer] = "TextName" + target;

                        texts.Add(TextScores[player, numplayer]);
                        texts.Add(TextNames[player, numplayer]);
                    }
                }
            }
        }

        private void BuildStaticStrings(ref List<string> statics)
        {
            StaticScores = new string[CSettings.MaxNumPlayer, CSettings.MaxNumPlayer];
            StaticAvatars = new string[CSettings.MaxNumPlayer, CSettings.MaxNumPlayer];

            for (int numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (int player = 0; player < CSettings.MaxNumPlayer; player++)
                {
                    if (player <= numplayer)
                    {
                        string target = "P" + (player + 1).ToString() + "N" + (numplayer + 1).ToString();
                        StaticScores[player, numplayer] = "StaticScore" + target;
                        StaticAvatars[player, numplayer] = "StaticAvatar" + target;

                        statics.Add(StaticScores[player, numplayer]);
                        statics.Add(StaticAvatars[player, numplayer]);
                    }
                }
            }
        }

        private void SetVisibility()
        {
            Statics[htStatics(StaticLyricsDuet)].Visible = false;
            Statics[htStatics(StaticLyricHelper)].Visible = false;
            Statics[htStatics(StaticLyricHelperDuet)].Visible = false;
            Statics[htStatics(StaticLyricHelperTop)].Visible = false;
            Lyrics[htLyrics(LyricMainDuet)].Visible = false;
            Lyrics[htLyrics(LyricSubDuet)].Visible = false;

            Statics[htStatics(StaticSongText)].Visible = false;
            Texts[htTexts(TextSongName)].Visible = false;
            Texts[htTexts(TextDuetName1)].Visible = false;
            Texts[htTexts(TextDuetName2)].Visible = false;

            for (int numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (int player = 0; player < CSettings.MaxNumPlayer; player++)
                {
                    if (player <= numplayer)
                    {
                        Texts[htTexts(TextScores[player, numplayer])].Visible = (numplayer + 1 == CGame.NumPlayer);
                        Texts[htTexts(TextNames[player, numplayer])].Visible = ((numplayer + 1 == CGame.NumPlayer)
                             && (CConfig.PlayerInfo == EPlayerInfo.TR_CONFIG_PLAYERINFO_BOTH || CConfig.PlayerInfo == EPlayerInfo.TR_CONFIG_PLAYERINFO_NAME));
                        Statics[htStatics(StaticScores[player, numplayer])].Visible = (numplayer + 1 == CGame.NumPlayer);
                        Statics[htStatics(StaticAvatars[player, numplayer])].Visible = ((numplayer + 1 == CGame.NumPlayer)
                            && (CConfig.PlayerInfo == EPlayerInfo.TR_CONFIG_PLAYERINFO_BOTH || CConfig.PlayerInfo == EPlayerInfo.TR_CONFIG_PLAYERINFO_AVATAR));
                    }
                }
            }

            Lyrics[htLyrics(LyricMain)].Alpha = 0f;
            Lyrics[htLyrics(LyricSub)].Alpha = 0f;

            Lyrics[htLyrics(LyricMainTop)].Alpha = 0f;
            Lyrics[htLyrics(LyricSubTop)].Alpha = 0f;

            Statics[htStatics(StaticLyrics)].Alpha = 0f;
            Statics[htStatics(StaticLyricsTop)].Alpha = 0f;

            Statics[htStatics(StaticLyricHelper)].Alpha = 0f;
            Statics[htStatics(StaticLyricHelperTop)].Alpha = 0f;

            Lyrics[htLyrics(LyricMainDuet)].Alpha = 0f;
            Lyrics[htLyrics(LyricSubDuet)].Alpha = 0f;

            Statics[htStatics(StaticLyricsDuet)].Alpha = 0f;
            Statics[htStatics(StaticLyricHelperDuet)].Alpha = 0f;

        }

        private void DrawLyricHelper()
        {
            if (_FadeOut)
                return;

            if (!CSound.IsPlaying(_CurrentStream) && !CSound.IsPaused(_CurrentStream))
                return;

            CSong song = CGame.GetSong();

            if (song == null)
                return;

            float alpha = (float)((Math.Cos(_CurrentTime * Math.PI * 2) + 1) / 2.0) / 2f + 0.5f;

            if (_TimeToFirstNote > CSettings.LyricHelperMinTime && _RemainingTimeToFirstNote > 0f && _RemainingTimeToFirstNote < CSettings.LyricHelperEnableTime)
            {
                float time = _RemainingTimeToFirstNote;
                float totaltime = CSettings.LyricHelperMoveTime;

                if (totaltime > _TimeToFirstNote)
                    totaltime = _TimeToFirstNote;

                if (time > totaltime)
                    time = totaltime;

                SRectF Rect = Statics[htStatics(StaticLyricHelper)].Rect;
                SColorF Color = new SColorF(
                    Statics[htStatics(StaticLyricHelper)].Color.R,
                    Statics[htStatics(StaticLyricHelper)].Color.G,
                    Statics[htStatics(StaticLyricHelper)].Color.B,
                    Statics[htStatics(StaticLyricHelper)].Color.A * Statics[htStatics(StaticLyricHelper)].Alpha * alpha);

                float distance = Lyrics[htLyrics(LyricMain)].GetCurrentLyricPosX() - Rect.X - Rect.W;
                CDraw.DrawTexture(Statics[htStatics(StaticLyricHelper)].Texture,
                    new SRectF(Rect.X + distance * (1f - time / totaltime), Rect.Y, Rect.W, Rect.H, Rect.Z), Color);

                if (Statics[htStatics(StaticLyricsTop)].Visible)
                {
                    Rect = Statics[htStatics(StaticLyricHelperTop)].Rect;
                    Color = new SColorF(
                        Statics[htStatics(StaticLyricHelperTop)].Color.R,
                        Statics[htStatics(StaticLyricHelperTop)].Color.G,
                        Statics[htStatics(StaticLyricHelperTop)].Color.B,
                        Statics[htStatics(StaticLyricHelperTop)].Color.A * Statics[htStatics(StaticLyricHelper)].Alpha * alpha);

                    distance = Lyrics[htLyrics(LyricMainTop)].GetCurrentLyricPosX() - Rect.X - Rect.W;
                    CDraw.DrawTexture(Statics[htStatics(StaticLyricHelperTop)].Texture,
                        new SRectF(Rect.X + distance * (1f - time / totaltime), Rect.Y, Rect.W, Rect.H, Rect.Z), Color);
                }
            }

            if (song.IsDuet)
            {
                if (_TimeToFirstNoteDuet > CSettings.LyricHelperMinTime && _RemainingTimeToFirstNoteDuet > 0f && _RemainingTimeToFirstNoteDuet < CSettings.LyricHelperEnableTime)
                {
                    float time = _RemainingTimeToFirstNoteDuet;
                    float totaltime = CSettings.LyricHelperMoveTime;

                    if (totaltime > _TimeToFirstNoteDuet)
                        totaltime = _TimeToFirstNoteDuet;

                    if (time > totaltime)
                        time = totaltime;

                    SRectF Rect = Statics[htStatics(StaticLyricHelperDuet)].Rect;

                    SColorF Color = new SColorF(
                        Statics[htStatics(StaticLyricHelperDuet)].Color.R,
                        Statics[htStatics(StaticLyricHelperDuet)].Color.G,
                        Statics[htStatics(StaticLyricHelperDuet)].Color.B,
                        Statics[htStatics(StaticLyricHelperDuet)].Color.A * Statics[htStatics(StaticLyricHelperDuet)].Alpha * alpha);

                    float distance = Lyrics[htLyrics(LyricMainDuet)].GetCurrentLyricPosX() - Rect.X - Rect.W;
                    CDraw.DrawTexture(Statics[htStatics(StaticLyricHelperDuet)].Texture,
                        new SRectF(Rect.X + distance * (1f - time / totaltime), Rect.Y, Rect.W, Rect.H, Rect.Z),
                        Color);
                }
            }
        }

        private float[] CalcFadingAlpha()
        {
            float dt = 4f;
            float rt = dt * 0.8f;

            CSong Song = CGame.GetSong();
            if (Song == null)
                return null;
            else if (!Song.NotesLoaded)
                return null;

            float[] Alpha = new float[Song.Notes.Lines.Length * 2];
            float CurrentTime = _CurrentTime - Song.Gap;

            for (int i = 0; i < Song.Notes.LinesCount; i++)
            {
                CLines lines = Song.Notes.GetLines(i);
                CLine[] line = lines.Line;

                // find current line for lyric sub fading
                int CurrentLineSub = FindCurrentLine(lines, line, Song);

                // find current line for lyric main fading
                int CurrentLine = 0;
                for (int j = 0; j < line.Length; j++)
                {
                    if (line[j].FirstNoteBeat <= _CurrentBeat)
                    {
                        CurrentLine = j;
                    }
                }

                // default values
                Alpha[i * 2] = 1f;
                Alpha[i * 2 + 1] = 1f;

                // main line alpha
                if (CurrentLine == 0 && CurrentTime < CGame.GetTimeFromBeats(line[CurrentLine].FirstNoteBeat, Song.BPM))
                {
                    // first main line and fist note is not reached
                    // => fade in
                    float diff = CGame.GetTimeFromBeats(line[CurrentLine].FirstNoteBeat, Song.BPM) - CurrentTime;
                    if (diff > dt)
                    {
                        Alpha[i * 2] = 1f - (diff - dt) / rt;
                    }
                }
                else if (CurrentLine < line.Length - 1 && CGame.GetTimeFromBeats(line[CurrentLine].LastNoteBeat, Song.BPM) < CurrentTime &&
                    CGame.GetTimeFromBeats(line[CurrentLine + 1].FirstNoteBeat, Song.BPM) > CurrentTime)
                {
                    // current position is between two lines

                    // time between the to lines
                    float diff = CGame.GetTimeFromBeats(line[CurrentLine + 1].FirstNoteBeat, Song.BPM) -
                        CGame.GetTimeFromBeats(line[CurrentLine].LastNoteBeat, Song.BPM);

                    // fade only if there is enough time for fading
                    if (diff > 3.3f * dt)
                    {
                        // time elapsed since last line
                        float last = CurrentTime - CGame.GetTimeFromBeats(line[CurrentLine].LastNoteBeat, Song.BPM);

                        // time to next line
                        float next = CGame.GetTimeFromBeats(line[CurrentLine + 1].FirstNoteBeat, Song.BPM) - CurrentTime;

                        if (last < next)
                        {
                            // fade out
                            Alpha[i * 2] = 1f - last / rt;
                        }
                        else
                        {
                            // fade in if it is time for
                            if (next > dt)
                                Alpha[i * 2] = 1f - (next - dt) / rt;
                        }
                    }
                }
                else if (CurrentLine == line.Length - 1 && CGame.GetTimeFromBeats(line[CurrentLine].LastNoteBeat, Song.BPM) < CurrentTime)
                {
                    // last main line and last note was reached
                    // => fade out
                    float diff = CurrentTime - CGame.GetTimeFromBeats(line[CurrentLine].LastNoteBeat, Song.BPM);
                    Alpha[i * 2] = 1f - diff / rt;
                }

                // sub
                if (CurrentLineSub < line.Length - 2)
                {
                    float diff = 0f;
                    diff = CGame.GetTimeFromBeats(line[CurrentLineSub + 1].FirstNoteBeat, Song.BPM) - CurrentTime;

                    if (diff > dt)
                    {
                        Alpha[i * 2 + 1] = 1f - (diff - dt) / rt;
                    }
                }

                if (Alpha[i * 2] < 0f)
                    Alpha[i * 2] = 0f;

                if (Alpha[i * 2 + 1] < 0f)
                    Alpha[i * 2 + 1] = 0f;
            }

            return Alpha;
        }

        private void UpdateSongText()
        {
            if (_TimerSongText.IsRunning && !Lyrics[htLyrics(LyricMainDuet)].Visible && !Lyrics[htLyrics(LyricMainTop)].Visible)
            {
                float t = _TimerSongText.ElapsedMilliseconds / 1000f;
                if (t < 10f)
                {
                    Statics[htStatics(StaticSongText)].Visible = true;
                    Texts[htTexts(TextSongName)].Visible = true;

                    if (t < 7f)
                    {
                        Statics[htStatics(StaticSongText)].Color.A = 1f;
                        Texts[htTexts(TextSongName)].Color.A = 1f;
                    }
                    else
                    {
                        Statics[htStatics(StaticSongText)].Color.A = (3f - (t - 7f)) / 3f;
                        Texts[htTexts(TextSongName)].Color.A = (3f - (t - 7f)) / 3f;
                    }
                }
                else
                {
                    Statics[htStatics(StaticSongText)].Visible = false;
                    Texts[htTexts(TextSongName)].Visible = false;
                    _TimerSongText.Stop();
                }
            }
            else
            {
                Statics[htStatics(StaticSongText)].Visible = false;
                Texts[htTexts(TextSongName)].Visible = false;
            }
        }

        private void UpdateDuetText()
        {
            if (CGame.GetSong() != null)
            {
                //Timer for first duet-part
                if (_TimerDuetText1.IsRunning)
                {
                    float t = _TimerDuetText1.ElapsedMilliseconds / 1000f;
                    if (t < 10f)
                    {
                        Texts[htTexts(TextDuetName1)].Visible = true;

                        if (t < 3f)
                        {
                            Texts[htTexts(TextDuetName1)].Color.A = (3f - (3f - t)) / 3f;
                        }
                        else if (t < 7f)
                        {
                            Texts[htTexts(TextDuetName1)].Color.A = 1f;
                        }
                        else
                        {
                            Texts[htTexts(TextDuetName1)].Color.A = (3f - (t - 7f)) / 3f;
                        }
                    }
                    else
                    {
                        Texts[htTexts(TextDuetName1)].Visible = false;
                        _TimerDuetText1.Stop();
                    }
                }
                else if (!_TimerDuetText1.IsRunning && _TimerDuetText1.ElapsedMilliseconds == 0 && Lyrics[htLyrics(LyricMainDuet)].Alpha > 0 && CGame.GetSong().IsDuet)
                    _TimerDuetText1.Start();
                else
                {
                    Texts[htTexts(TextDuetName1)].Visible = false;
                }
                //Timer for second duet-part
                if (_TimerDuetText2.IsRunning)
                {
                    float t = _TimerDuetText2.ElapsedMilliseconds / 1000f;
                    if (t < 10f)
                    {
                        Texts[htTexts(TextDuetName2)].Visible = true;

                        if (t < 3f)
                        {
                            Texts[htTexts(TextDuetName2)].Color.A = (3f - (3f - t)) / 3f;
                        }
                        else if (t < 7f)
                        {
                            Texts[htTexts(TextDuetName2)].Color.A = 1f;
                        }
                        else
                        {
                            Texts[htTexts(TextDuetName2)].Color.A = (3f - (t - 7f)) / 3f;
                        }
                    }
                    else
                    {
                        Texts[htTexts(TextDuetName2)].Visible = false;
                        _TimerDuetText2.Stop();
                    }
                }
                else if (!_TimerDuetText2.IsRunning && _TimerDuetText2.ElapsedMilliseconds == 0 && Lyrics[htLyrics(LyricMain)].Alpha > 0 && CGame.GetSong().IsDuet)
                    _TimerDuetText2.Start();
                else
                {
                    Texts[htTexts(TextDuetName2)].Visible = false;
                }
            }
        }

        private void UpdateTimeLine()
        {
            CSong song = CGame.GetSong();

            if (song == null)
                return;

            float TotalTime = CSound.GetLength(_CurrentStream);
            if (song.Finish != 0)
                TotalTime = song.Finish;

            float RemainingTime = TotalTime - _CurrentTime;
            TotalTime -= song.Start;
            float CurrentTime = _CurrentTime - song.Start;

            if (TotalTime <= 0f)
                return;

            switch (CConfig.TimerMode)
            {
                case ETimerMode.TR_CONFIG_TIMERMODE_CURRENT:
                    int min = (int)Math.Floor(CurrentTime / 60f);
                    int sec = (int)(CurrentTime - min * 60f);
                    Texts[htTexts(TextTime)].Text = min.ToString("00") + ":" + sec.ToString("00");
                    break;

                case ETimerMode.TR_CONFIG_TIMERMODE_REMAINING:
                    min = (int)Math.Floor(RemainingTime / 60f);
                    sec = (int)(RemainingTime - min * 60f);
                    Texts[htTexts(TextTime)].Text = "-" + min.ToString("00") + ":" + sec.ToString("00");
                    break;

                case ETimerMode.TR_CONFIG_TIMERMODE_TOTAL:
                    min = (int)Math.Floor(TotalTime / 60f);
                    sec = (int)(TotalTime - min * 60f);
                    Texts[htTexts(TextTime)].Text = "#" + min.ToString("00") + ":" + sec.ToString("00");
                    break;
            }


            switch (CConfig.TimerLook)
            {
                case ETimerLook.TR_CONFIG_TIMERLOOK_NORMAL:
                    _TimeLineRect.W = Statics[htStatics(StaticTimeLine)].Rect.W * (CurrentTime / TotalTime);
                    break;

                case ETimerLook.TR_CONFIG_TIMERLOOK_EXPANDED:
                    CStatic stat = Statics[htStatics(StaticTimeLine)];
                    int CurrentBeat = CGame.CurrentBeat;
                    for (int i = 0; i < _TimeRects.Count; i++)
                    {
                        if (CurrentBeat >= _TimeRects[i].startBeat && CurrentBeat <= _TimeRects[i].endBeat)
                        {
                            _TimeRects[i].rect.Texture = Statics[htStatics(StaticTimeLineExpandedHighlighted)].Texture;
                            _TimeRects[i].rect.Color = Statics[htStatics(StaticTimeLineExpandedHighlighted)].Color;
                        }
                        else
                        {
                            _TimeRects[i].rect.Texture = Statics[htStatics(StaticTimeLineExpandedNormal)].Texture;
                            _TimeRects[i].rect.Color = Statics[htStatics(StaticTimeLineExpandedNormal)].Color;
                        }

                    }
                    Statics[htStatics(StaticTimePointer)].Rect.X = stat.Rect.X + stat.Rect.W * (CurrentTime / TotalTime);
                    break;
            }
        }

        private void PrepareTimeLine()
        {
            CStatic stat = Statics[htStatics(StaticTimeLine)];
            switch (CConfig.TimerLook)
            {
                case ETimerLook.TR_CONFIG_TIMERLOOK_NORMAL:
                    _TimeLineRect = new SRectF(stat.Rect.X, stat.Rect.Y, 0f, stat.Rect.H, stat.Rect.Z);
                    Statics[htStatics(StaticTimePointer)].Visible = false;
                    break;

                case ETimerLook.TR_CONFIG_TIMERLOOK_EXPANDED:
                    _TimeRects.Clear();
                    Statics[htStatics(StaticTimePointer)].Visible = true;

                    CSong song = CGame.GetSong();

                    if (song == null)
                        return;

                    float TotalTime = CSound.GetLength(_CurrentStream);
                    if (song.Finish != 0)
                        TotalTime = song.Finish;

                    TotalTime -= song.Start;

                    if (TotalTime <= 0f)
                        return;

                    CLines[] Lines = new CLines[song.Notes.Lines.Length];
                    Lines = song.Notes.Lines;
                    for (int i = 0; i < Lines.Length; i++)
                    {
                        CLine[] Line = Lines[i].Line;
                        for (int j = 0; j < Line.Length; j++)
                        {
                            if (Line[j].VisibleInTimeLine)
                            {
                                TimeRect trect = new TimeRect();
                                trect.startBeat = Line[j].FirstNoteBeat;
                                trect.endBeat = Line[j].EndBeat;
                                trect.rect = GetNewStatic(new STexture(-1),
                                    new SColorF(1f, 1f, 1f, 1f),
                                    new SRectF(stat.Rect.X + stat.Rect.W * ((CGame.GetTimeFromBeats(trect.startBeat, song.BPM) + song.Gap - song.Start) / TotalTime),
                                        stat.Rect.Y,
                                        stat.Rect.W * (CGame.GetTimeFromBeats((trect.endBeat - trect.startBeat), song.BPM) / TotalTime),
                                        stat.Rect.H,
                                        stat.Rect.Z));

                                _TimeRects.Add(trect);
                            }
                        }

                    }
                    break;
            }
        }

        private void UpdateAvatars()
        {
            for (int i = 0; i < CGame.NumPlayer; i++)
            {
                if (CGame.Player[i].ProfileID > -1)
                {
                    Statics[htStatics(StaticAvatars[i, CGame.NumPlayer - 1])].Texture = CProfiles.Profiles[CGame.Player[i].ProfileID].Avatar.Texture;
                }
                else
                {
                    Statics[htStatics(StaticAvatars[i, CGame.NumPlayer - 1])].Visible = false;
                }
            }
        }

        private void UpdateNames()
        {
            for (int i = 0; i < CGame.NumPlayer; i++)
            {
                if (CGame.Player[i].ProfileID > -1)
                {
                    Texts[htTexts(TextNames[i, CGame.NumPlayer - 1])].Text = CProfiles.Profiles[CGame.Player[i].ProfileID].PlayerName;
                }
                else
                {
                    Texts[htTexts(TextNames[i, CGame.NumPlayer - 1])].Visible = false;
                }
            }
        }
    }
}
