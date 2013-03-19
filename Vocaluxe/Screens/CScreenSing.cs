using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Menu;
using Vocaluxe.Menu.SingNotes;
using Vocaluxe.Menu.SongMenu;

namespace Vocaluxe.Screens
{
    class CScreenSing : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion { get { return 6; } }

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

        public override void Init()
        {
            base.Init();

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

            Statics[StaticTimeLine].Visible = false;
            Statics[StaticTimeLineExpandedNormal].Visible = false;
            Statics[StaticTimeLineExpandedHighlighted].Visible = false;
            Statics[StaticPauseBG].Visible = false;
            Texts[TextPause].Visible = false;

            Statics[StaticPauseBG].Visible = false;
            Texts[TextPause].Visible = false;

            Buttons[ButtonCancel].Visible = false;
            Buttons[ButtonContinue].Visible = false;
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
                            SetInteractionToButton(Buttons[ButtonCancel]);
                        break;

                    case Keys.P:
                        TogglePause();
                        if (_Pause)
                            SetInteractionToButton(Buttons[ButtonContinue]);
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
                        if (CGame.GetSong() != null)
                            SetDuetLyricsVisibility(CGame.GetSong().IsDuet); //make sure duet lyrics remain visible
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
                        if (Buttons[ButtonContinue].Selected && _Pause)
                            TogglePause();
                        if (Buttons[ButtonCancel].Selected && _Pause)
                            Stop();
                        if (Buttons[ButtonSkip].Selected && _Pause)
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
                    SetInteractionToButton(Buttons[ButtonContinue]);
            }

            if (MouseEvent.LB && IsMouseOver(MouseEvent) && _Pause)
            {
                if (Buttons[ButtonContinue].Selected && _Pause)
                    TogglePause();

                if (Buttons[ButtonCancel].Selected && _Pause)
                    Stop();

                if (Buttons[ButtonSkip].Selected && _Pause)
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
                Lyrics[LyricMain].Alpha = Alpha[0];
                Lyrics[LyricSub].Alpha = Alpha[1];

                Lyrics[LyricMainTop].Alpha = Alpha[0];
                Lyrics[LyricSubTop].Alpha = Alpha[1];

                Statics[StaticLyrics].Alpha = Alpha[0];
                Statics[StaticLyricsTop].Alpha = Alpha[0];

                Statics[StaticLyricHelper].Alpha = Alpha[0];
                Statics[StaticLyricHelperTop].Alpha = Alpha[0];

                for (int p = 0; p < CGame.NumPlayer; p++)
                {
                    SingNotes[SingBars].SetAlpha(NoteLines[p], Alpha[CGame.Player[p].LineNr * 2]);
                    if (CConfig.FadePlayerInfo == EFadePlayerInfo.TR_CONFIG_FADEPLAYERINFO_INFO || CConfig.FadePlayerInfo == EFadePlayerInfo.TR_CONFIG_FADEPLAYERINFO_ALL)
                    {
                        Statics[StaticAvatars[p, CGame.NumPlayer - 1]].Alpha = Alpha[CGame.Player[p].LineNr * 2];
                        Texts[TextNames[p, CGame.NumPlayer - 1]].Alpha = Alpha[CGame.Player[p].LineNr * 2];
                    }
                    if (CConfig.FadePlayerInfo == EFadePlayerInfo.TR_CONFIG_FADEPLAYERINFO_ALL)
                    {
                        Statics[StaticScores[p, CGame.NumPlayer - 1]].Alpha = Alpha[CGame.Player[p].LineNr * 2];
                        Statics[StaticAvatars[p, CGame.NumPlayer - 1]].Alpha = Alpha[CGame.Player[p].LineNr * 2];
                        Texts[TextNames[p, CGame.NumPlayer - 1]].Alpha = Alpha[CGame.Player[p].LineNr * 2];
                        Texts[TextScores[p, CGame.NumPlayer - 1]].Alpha = Alpha[CGame.Player[p].LineNr * 2];
                    }
                }

                if (Alpha.Length > 2)
                {
                    Lyrics[LyricMainDuet].Alpha = Alpha[0];
                    Lyrics[LyricSubDuet].Alpha = Alpha[1];

                    Statics[StaticLyricsDuet].Alpha = Alpha[0];
                    Statics[StaticLyricHelperDuet].Alpha = Alpha[0];

                    Lyrics[LyricMain].Alpha = Alpha[2];
                    Lyrics[LyricSub].Alpha = Alpha[3];
                     
                    Statics[StaticLyrics].Alpha = Alpha[2];
                    Statics[StaticLyricHelper].Alpha = Alpha[2];
                }
            }


            for (int p = 0; p < CGame.NumPlayer; p++)
            {
                if (CGame.Player[p].Points < 10000)
                {
                    Texts[TextScores[p, CGame.NumPlayer - 1]].Text = CGame.Player[p].Points.ToString("0000");
                }
                else
                {
                    Texts[TextScores[p, CGame.NumPlayer - 1]].Text = CGame.Player[p].Points.ToString("00000");
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

            SingNotes[SingBars].Reset();
            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
            {
                NoteLines[i] = -1;
            }

            foreach(CLyric lyric in Lyrics)
            {
                lyric.LyricStyle = CConfig.LyricStyle;
            }

            for (int p = 0; p < CGame.NumPlayer; p++)
            {
                Statics[StaticAvatars[p, CGame.NumPlayer - 1]].Aspect = EAspect.Crop;
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
                    CDraw.DrawTexture(Statics[StaticTimeLine].Texture, Statics[StaticTimeLine].Rect, new SColorF(1, 1, 1, 1), _TimeLineRect);
                    break;
                case ETimerLook.TR_CONFIG_TIMERLOOK_EXPANDED:
                    for (int i = 0; i < _TimeRects.Count; i++)
                    {
                        CDraw.DrawTexture(_TimeRects[i].rect.Texture, Statics[StaticTimeLine].Rect, _TimeRects[i].rect.Color, _TimeRects[i].rect.Rect);
                    }
                    break;
            }

            Lyrics[LyricSub].Draw(-100);
            Lyrics[LyricMain].Draw(CGame.Beat);

            Lyrics[LyricSubDuet].Draw(-100);
            Lyrics[LyricMainDuet].Draw(CGame.Beat);

            Lyrics[LyricSubTop].Draw(-100);
            Lyrics[LyricMainTop].Draw(CGame.Beat);


            for (int i = 0; i < CGame.NumPlayer; i++)
            {
                SingNotes[SingBars].Draw(NoteLines[i], CGame.Player[i].SingLine, i);
            }

            DrawLyricHelper();

            if (_Pause)
            {
                Statics[StaticPauseBG].ForceDraw();
                Texts[TextPause].ForceDraw();

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

            Lyrics[LyricMain].Clear();
            Lyrics[LyricSub].Clear();
            Lyrics[LyricMainDuet].Clear();
            Lyrics[LyricSubDuet].Clear();
            Lyrics[LyricMainTop].Clear();
            Lyrics[LyricSubTop].Clear();
            Texts[TextSongName].Text = String.Empty;
            Texts[TextDuetName1].Text = String.Empty;
            Texts[TextDuetName2].Text = String.Empty;
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
            Texts[TextSongName].Text = songname;

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

            if (song.VideoFileName.Length > 0)
            {
                _CurrentVideo = CVideo.VdLoad(Path.Combine(song.Folder, song.VideoFileName));
                CVideo.VdSkip(_CurrentVideo, song.Start, song.VideoGap);
                _VideoAspect = song.VideoAspect;
            }

            CDraw.RemoveTexture(ref _Background);
            if (song.BackgroundFileName.Length > 0)
                _Background = CDraw.AddTexture(Path.Combine(song.Folder, song.BackgroundFileName));

            SingNotes[SingBars].Reset();

            
            if (song.IsDuet)
            {
                Texts[TextDuetName1].Text = song.DuetPart1;
                Texts[TextDuetName2].Text = song.DuetPart2;
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
                
            }
            SetDuetLyricsVisibility(song.IsDuet);

            for (int p = 0; p < CGame.NumPlayer; p++)
            {
                NoteLines[p] = SingNotes[SingBars].AddPlayer(
                    SingNotes[SingBars].BarPos[p, CGame.NumPlayer - 1],
                    CTheme.GetPlayerColor(p + 1),
                    p);
            }

            /*
                case 4:
                    NoteLines[0] = SingNotes[SingBars].AddPlayer(new SRectF(35f, 100f, 590f, 200f, -0.5f), CTheme.ThemeColors.Player[0]);
                    NoteLines[1] = SingNotes[SingBars].AddPlayer(new SRectF(35f, 350f, 590f, 200f, -0.5f), CTheme.ThemeColors.Player[1]);
                    NoteLines[2] = SingNotes[SingBars].AddPlayer(new SRectF(640f, 100f, 590f, 200f, -0.5f), CTheme.ThemeColors.Player[2]);
                    NoteLines[3] = SingNotes[SingBars].AddPlayer(new SRectF(640f, 350f, 590f, 200f, -0.5f), CTheme.ThemeColors.Player[3]);
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

        private void SetDuetLyricsVisibility(bool isDuet)
        {
            Statics[StaticLyricsDuet].Visible = isDuet;
            Lyrics[LyricMainDuet].Visible = isDuet;
            Lyrics[LyricSubDuet].Visible = isDuet;

            if (isDuet)
            {
                Lyrics[LyricMainTop].Visible = false;
                Lyrics[LyricSubTop].Visible = false;
                Statics[StaticLyricsTop].Visible = false;
            }
            else
            {
                bool LyricsOnTop = (CGame.NumPlayer != 1) && CConfig.LyricsOnTop == EOffOn.TR_CONFIG_ON;
                Lyrics[LyricMainTop].Visible = LyricsOnTop;
                Lyrics[LyricSubTop].Visible = LyricsOnTop;
                Statics[StaticLyricsTop].Visible = LyricsOnTop;
            }
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
                            SingNotes[SingBars].AddLine(NoteLines[j], line, nr, j);
                    }

                    if (i == 0 && !song.IsDuet || i == 1 && song.IsDuet)
                    {
                        Lyrics[LyricMain].SetLine(line[nr]);
                        Lyrics[LyricMainTop].SetLine(line[nr]);
                        _TimeToFirstNote = CGame.GetTimeFromBeats(line[nr].FirstNoteBeat - line[nr].StartBeat, song.BPM);
                        _RemainingTimeToFirstNote = CGame.GetTimeFromBeats(line[nr].FirstNoteBeat - CGame.GetBeatFromTime(_CurrentTime, song.BPM, song.Gap), song.BPM);

                        if (line.Length >= nr + 2)
                        {
                            Lyrics[LyricSub].SetLine(line[nr + 1]);
                            Lyrics[LyricSubTop].SetLine(line[nr + 1]);
                        }
                        else
                        {
                            Lyrics[LyricSub].Clear();
                            Lyrics[LyricSubTop].Clear();
                        }
                    }
                    if (i == 0 && song.IsDuet)
                    {
                        Lyrics[LyricMainDuet].SetLine(line[nr]);
                        _TimeToFirstNoteDuet = CGame.GetTimeFromBeats(line[nr].FirstNoteBeat - line[nr].StartBeat, song.BPM);
                        _RemainingTimeToFirstNoteDuet = CGame.GetTimeFromBeats(line[nr].FirstNoteBeat - CGame.GetBeatFromTime(_CurrentTime, song.BPM, song.Gap), song.BPM);

                        if (line.Length >= nr + 2)
                            Lyrics[LyricSubDuet].SetLine(line[nr + 1]);
                        else
                            Lyrics[LyricSubDuet].Clear();
                    }
                }
                else
                {
                    if (i == 0 && !song.IsDuet || i == 1 && song.IsDuet)
                    {
                        Lyrics[LyricMain].Clear();
                        Lyrics[LyricSub].Clear();
                        Lyrics[LyricMainTop].Clear();
                        Lyrics[LyricSubTop].Clear();
                        _TimeToFirstNote = 0f;
                    }

                    if (i == 0 && song.IsDuet)
                    {
                        Lyrics[LyricMainDuet].Clear();
                        Lyrics[LyricSubDuet].Clear();
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
                Buttons[ButtonCancel].Visible = true;
                Buttons[ButtonContinue].Visible = true;
                if (CGame.NumRounds > CGame.RoundNr && CGame.NumRounds > 1)
                    Buttons[ButtonSkip].Visible = true;
                else
                    Buttons[ButtonSkip].Visible = false;
                CSound.Pause(_CurrentStream);               
            }else
            {
                Buttons[ButtonCancel].Visible = false;
                Buttons[ButtonContinue].Visible = false;
                Buttons[ButtonSkip].Visible = false;
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
            Statics[StaticLyricsDuet].Visible = false;
            Statics[StaticLyricHelper].Visible = false;
            Statics[StaticLyricHelperDuet].Visible = false;
            Statics[StaticLyricHelperTop].Visible = false;
            Lyrics[LyricMainDuet].Visible = false;
            Lyrics[LyricSubDuet].Visible = false;

            Statics[StaticSongText].Visible = false;
            Texts[TextSongName].Visible = false;
            Texts[TextDuetName1].Visible = false;
            Texts[TextDuetName2].Visible = false;

            for (int numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (int player = 0; player < CSettings.MaxNumPlayer; player++)
                {
                    if (player <= numplayer)
                    {
                        Texts[TextScores[player, numplayer]].Visible = (numplayer + 1 == CGame.NumPlayer);
                        Texts[TextNames[player, numplayer]].Visible = ((numplayer + 1 == CGame.NumPlayer)
                             && (CConfig.PlayerInfo == EPlayerInfo.TR_CONFIG_PLAYERINFO_BOTH || CConfig.PlayerInfo == EPlayerInfo.TR_CONFIG_PLAYERINFO_NAME));
                        Statics[StaticScores[player, numplayer]].Visible = (numplayer + 1 == CGame.NumPlayer);
                        Statics[StaticAvatars[player, numplayer]].Visible = ((numplayer + 1 == CGame.NumPlayer)
                            && (CConfig.PlayerInfo == EPlayerInfo.TR_CONFIG_PLAYERINFO_BOTH || CConfig.PlayerInfo == EPlayerInfo.TR_CONFIG_PLAYERINFO_AVATAR));
                    }
                }
            }

            Lyrics[LyricMain].Alpha = 0f;
            Lyrics[LyricSub].Alpha = 0f;

            Lyrics[LyricMainTop].Alpha = 0f;
            Lyrics[LyricSubTop].Alpha = 0f;

            Statics[StaticLyrics].Alpha = 0f;
            Statics[StaticLyricsTop].Alpha = 0f;

            Statics[StaticLyricHelper].Alpha = 0f;
            Statics[StaticLyricHelperTop].Alpha = 0f;

            Lyrics[LyricMainDuet].Alpha = 0f;
            Lyrics[LyricSubDuet].Alpha = 0f;

            Statics[StaticLyricsDuet].Alpha = 0f;
            Statics[StaticLyricHelperDuet].Alpha = 0f;

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

                SRectF Rect = Statics[StaticLyricHelper].Rect;
                SColorF Color = new SColorF(
                    Statics[StaticLyricHelper].Color.R,
                    Statics[StaticLyricHelper].Color.G,
                    Statics[StaticLyricHelper].Color.B,
                    Statics[StaticLyricHelper].Color.A * Statics[StaticLyricHelper].Alpha * alpha);

                float distance = Lyrics[LyricMain].GetCurrentLyricPosX() - Rect.X - Rect.W;
                CDraw.DrawTexture(Statics[StaticLyricHelper].Texture,
                    new SRectF(Rect.X + distance * (1f - time / totaltime), Rect.Y, Rect.W, Rect.H, Rect.Z), Color);

                if (Statics[StaticLyricsTop].Visible)
                {
                    Rect = Statics[StaticLyricHelperTop].Rect;
                    Color = new SColorF(
                        Statics[StaticLyricHelperTop].Color.R,
                        Statics[StaticLyricHelperTop].Color.G,
                        Statics[StaticLyricHelperTop].Color.B,
                        Statics[StaticLyricHelperTop].Color.A * Statics[StaticLyricHelper].Alpha * alpha);

                    distance = Lyrics[LyricMainTop].GetCurrentLyricPosX() - Rect.X - Rect.W;
                    CDraw.DrawTexture(Statics[StaticLyricHelperTop].Texture,
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

                    SRectF Rect = Statics[StaticLyricHelperDuet].Rect;

                    SColorF Color = new SColorF(
                        Statics[StaticLyricHelperDuet].Color.R,
                        Statics[StaticLyricHelperDuet].Color.G,
                        Statics[StaticLyricHelperDuet].Color.B,
                        Statics[StaticLyricHelperDuet].Color.A * Statics[StaticLyricHelperDuet].Alpha * alpha);

                    float distance = Lyrics[LyricMainDuet].GetCurrentLyricPosX() - Rect.X - Rect.W;
                    CDraw.DrawTexture(Statics[StaticLyricHelperDuet].Texture,
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
            if (_TimerSongText.IsRunning && !Lyrics[LyricMainDuet].Visible && !Lyrics[LyricMainTop].Visible)
            {
                float t = _TimerSongText.ElapsedMilliseconds / 1000f;
                if (t < 10f)
                {
                    Statics[StaticSongText].Visible = true;
                    Texts[TextSongName].Visible = true;

                    if (t < 7f)
                    {
                        Statics[StaticSongText].Color.A = 1f;
                        Texts[TextSongName].Color.A = 1f;
                    }
                    else
                    {
                        Statics[StaticSongText].Color.A = (3f - (t - 7f)) / 3f;
                        Texts[TextSongName].Color.A = (3f - (t - 7f)) / 3f;
                    }
                }
                else
                {
                    Statics[StaticSongText].Visible = false;
                    Texts[TextSongName].Visible = false;
                    _TimerSongText.Stop();
                }
            }
            else
            {
                Statics[StaticSongText].Visible = false;
                Texts[TextSongName].Visible = false;
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
                        Texts[TextDuetName1].Visible = true;

                        if (t < 3f)
                        {
                            Texts[TextDuetName1].Color.A = (3f - (3f - t)) / 3f;
                        }
                        else if (t < 7f)
                        {
                            Texts[TextDuetName1].Color.A = 1f;
                        }
                        else
                        {
                            Texts[TextDuetName1].Color.A = (3f - (t - 7f)) / 3f;
                        }
                    }
                    else
                    {
                        Texts[TextDuetName1].Visible = false;
                        _TimerDuetText1.Stop();
                    }
                }
                else if (!_TimerDuetText1.IsRunning && _TimerDuetText1.ElapsedMilliseconds == 0 && Lyrics[LyricMainDuet].Alpha > 0 && CGame.GetSong().IsDuet)
                    _TimerDuetText1.Start();
                else
                {
                    Texts[TextDuetName1].Visible = false;
                }
                //Timer for second duet-part
                if (_TimerDuetText2.IsRunning)
                {
                    float t = _TimerDuetText2.ElapsedMilliseconds / 1000f;
                    if (t < 10f)
                    {
                        Texts[TextDuetName2].Visible = true;

                        if (t < 3f)
                        {
                            Texts[TextDuetName2].Color.A = (3f - (3f - t)) / 3f;
                        }
                        else if (t < 7f)
                        {
                            Texts[TextDuetName2].Color.A = 1f;
                        }
                        else
                        {
                            Texts[TextDuetName2].Color.A = (3f - (t - 7f)) / 3f;
                        }
                    }
                    else
                    {
                        Texts[TextDuetName2].Visible = false;
                        _TimerDuetText2.Stop();
                    }
                }
                else if (!_TimerDuetText2.IsRunning && _TimerDuetText2.ElapsedMilliseconds == 0 && Lyrics[LyricMain].Alpha > 0 && CGame.GetSong().IsDuet)
                    _TimerDuetText2.Start();
                else
                {
                    Texts[TextDuetName2].Visible = false;
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
                    Texts[TextTime].Text = min.ToString("00") + ":" + sec.ToString("00");
                    break;

                case ETimerMode.TR_CONFIG_TIMERMODE_REMAINING:
                    min = (int)Math.Floor(RemainingTime / 60f);
                    sec = (int)(RemainingTime - min * 60f);
                    Texts[TextTime].Text = "-" + min.ToString("00") + ":" + sec.ToString("00");
                    break;

                case ETimerMode.TR_CONFIG_TIMERMODE_TOTAL:
                    min = (int)Math.Floor(TotalTime / 60f);
                    sec = (int)(TotalTime - min * 60f);
                    Texts[TextTime].Text = "#" + min.ToString("00") + ":" + sec.ToString("00");
                    break;
            }


            switch (CConfig.TimerLook)
            {
                case ETimerLook.TR_CONFIG_TIMERLOOK_NORMAL:
                    _TimeLineRect.W = Statics[StaticTimeLine].Rect.W * (CurrentTime / TotalTime);
                    break;

                case ETimerLook.TR_CONFIG_TIMERLOOK_EXPANDED:
                    CStatic stat = Statics[StaticTimeLine];
                    int CurrentBeat = CGame.CurrentBeat;
                    for (int i = 0; i < _TimeRects.Count; i++)
                    {
                        if (CurrentBeat >= _TimeRects[i].startBeat && CurrentBeat <= _TimeRects[i].endBeat)
                        {
                            _TimeRects[i].rect.Texture = Statics[StaticTimeLineExpandedHighlighted].Texture;
                            _TimeRects[i].rect.Color = Statics[StaticTimeLineExpandedHighlighted].Color;
                        }
                        else
                        {
                            _TimeRects[i].rect.Texture = Statics[StaticTimeLineExpandedNormal].Texture;
                            _TimeRects[i].rect.Color = Statics[StaticTimeLineExpandedNormal].Color;
                        }

                    }
                    Statics[StaticTimePointer].Rect.X = stat.Rect.X + stat.Rect.W * (CurrentTime / TotalTime);
                    break;
            }
        }

        private void PrepareTimeLine()
        {
            CStatic stat = Statics[StaticTimeLine];
            switch (CConfig.TimerLook)
            {
                case ETimerLook.TR_CONFIG_TIMERLOOK_NORMAL:
                    _TimeLineRect = new SRectF(stat.Rect.X, stat.Rect.Y, 0f, stat.Rect.H, stat.Rect.Z);
                    Statics[StaticTimePointer].Visible = false;
                    break;

                case ETimerLook.TR_CONFIG_TIMERLOOK_EXPANDED:
                    _TimeRects.Clear();
                    Statics[StaticTimePointer].Visible = true;

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
                    Statics[StaticAvatars[i, CGame.NumPlayer - 1]].Texture = CProfiles.Profiles[CGame.Player[i].ProfileID].Avatar.Texture;
                }
                else
                {
                    Statics[StaticAvatars[i, CGame.NumPlayer - 1]].Visible = false;
                }
            }
        }

        private void UpdateNames()
        {
            for (int i = 0; i < CGame.NumPlayer; i++)
            {
                if (CGame.Player[i].ProfileID > -1)
                {
                    Texts[TextNames[i, CGame.NumPlayer - 1]].Text = CProfiles.Profiles[CGame.Player[i].ProfileID].PlayerName;
                }
                else
                {
                    Texts[TextNames[i, CGame.NumPlayer - 1]].Visible = false;
                }
            }
        }
    }
}
