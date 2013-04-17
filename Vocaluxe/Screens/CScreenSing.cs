using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib.Menu;
using VocaluxeLib.Menu.SingNotes;
using VocaluxeLib.Menu.SongMenu;

namespace Vocaluxe.Screens
{
    class CScreenSing : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 6; }
        }

        private struct STimeRect
        {
            public CStatic Rect;
            public float StartBeat;
            public float EndBeat;
        }

        private const string _TextSongName = "TextSongName";
        private const string _TextTime = "TextTime";
        private const string _TextPause = "TextPause";
        private const string _TextDuetName1 = "TextDuetName1";
        private const string _TextDuetName2 = "TextDuetName2";
        private string[,] _TextScores;
        private string[,] _TextNames;

        private const string _StaticSongText = "StaticSongText";
        private const string _StaticLyrics = "StaticLyrics";
        private const string _StaticLyricsDuet = "StaticLyricsDuet";
        private const string _StaticLyricsTop = "StaticLyricsTop";
        private const string _StaticTimeBar = "StaticTimeBar";
        private const string _StaticTimeLine = "StaticTimeLine";
        private const string _StaticTimeLineExpandedNormal = "StaticTimeLineExpandedNormal";
        private const string _StaticTimeLineExpandedHighlighted = "StaticTimeLineExpandedHighlighted";
        private const string _StaticTimePointer = "StaticTimePointer";
        private const string _StaticLyricHelper = "StaticLyricHelper";
        private const string _StaticLyricHelperDuet = "StaticLyricHelperDuet";
        private const string _StaticLyricHelperTop = "StaticLyricHelperTop";
        private const string _StaticPauseBG = "StaticPauseBG";

        private string[,] _StaticScores;
        private string[,] _StaticAvatars;

        private const string _ButtonCancel = "ButtonCancel";
        private const string _ButtonContinue = "ButtonContinue";
        private const string _ButtonSkip = "ButtonSkip";

        private const string _LyricMain = "LyricMain";
        private const string _LyricSub = "LyricSub";
        private const string _LyricMainDuet = "LyricMainDuet";
        private const string _LyricSubDuet = "LyricSubDuet";
        private const string _LyricMainTop = "LyricMainTop";
        private const string _LyricSubTop = "LyricSubTop";

        private const string _SingBars = "SingBars";

        private SRectF _TimeLineRect;
        private List<STimeRect> _TimeRects;
        private bool _FadeOut;

        private int _CurrentBeat;
        private int _CurrentStream = -1;
        //private int _NextStream = -1;
        private float _Volume = 100f;
        private int _CurrentVideo = -1;
        private EAspect _VideoAspect = EAspect.Crop;
        private STexture _CurrentVideoTexture = new STexture(-1);
        private STexture _CurrentWebcamFrameTexture = new STexture(-1);
        private STexture _Background = new STexture(-1);

        private float _CurrentTime;
        private float _FinishTime;

        private float _TimeToFirstNote;
        private float _RemainingTimeToFirstNote;
        private float _TimeToFirstNoteDuet;
        private float _RemainingTimeToFirstNoteDuet;

        private readonly int[] _NoteLines = new int[CSettings.MaxNumPlayer];

        private Stopwatch _TimerSongText;
        private Stopwatch _TimerDuetText1;
        private Stopwatch _TimerDuetText2;

        private bool _Pause;
        private bool _Webcam;

        public override void Init()
        {
            base.Init();

            List<string> texts = new List<string>();
            texts.Add(_TextSongName);
            texts.Add(_TextTime);
            texts.Add(_TextPause);
            texts.Add(_TextDuetName1);
            texts.Add(_TextDuetName2);
            _BuildTextStrings(ref texts);
            _ThemeTexts = texts.ToArray();

            List<string> statics = new List<string>();
            statics.Add(_StaticSongText);
            statics.Add(_StaticLyrics);
            statics.Add(_StaticLyricsDuet);
            statics.Add(_StaticLyricsTop);
            statics.Add(_StaticTimeBar);
            statics.Add(_StaticTimeLine);
            statics.Add(_StaticTimeLineExpandedNormal);
            statics.Add(_StaticTimeLineExpandedHighlighted);
            statics.Add(_StaticTimePointer);
            statics.Add(_StaticLyricHelper);
            statics.Add(_StaticLyricHelperDuet);
            statics.Add(_StaticLyricHelperTop);
            statics.Add(_StaticPauseBG);
            _BuildStaticStrings(ref statics);
            _ThemeStatics = statics.ToArray();

            List<string> buttons = new List<string>();
            buttons.Add(_ButtonCancel);
            buttons.Add(_ButtonContinue);
            buttons.Add(_ButtonSkip);
            _ThemeButtons = buttons.ToArray();

            _ThemeLyrics = new string[] {_LyricMain, _LyricSub, _LyricMainDuet, _LyricSubDuet, _LyricMainTop, _LyricSubTop};
            _ThemeSingNotes = new string[] {_SingBars};

            _TimeRects = new List<STimeRect>();
            _TimerSongText = new Stopwatch();
            _TimerDuetText1 = new Stopwatch();
            _TimerDuetText2 = new Stopwatch();
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            Statics[_StaticTimeLine].Visible = false;
            Statics[_StaticTimeLineExpandedNormal].Visible = false;
            Statics[_StaticTimeLineExpandedHighlighted].Visible = false;
            Statics[_StaticPauseBG].Visible = false;
            Texts[_TextPause].Visible = false;

            Statics[_StaticPauseBG].Visible = false;
            Texts[_TextPause].Visible = false;

            Buttons[_ButtonCancel].Visible = false;
            Buttons[_ButtonContinue].Visible = false;
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed)
            {
                //
            }
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                        _TogglePause();
                        if (_Pause)
                            SetInteractionToButton(Buttons[_ButtonCancel]);
                        break;

                    case Keys.P:
                        _TogglePause();
                        if (_Pause)
                            SetInteractionToButton(Buttons[_ButtonContinue]);
                        break;

                    case Keys.T:
                        int mode = (int)CConfig.TimerMode;

                        mode++;
                        if (mode > Enum.GetNames(typeof(ETimerMode)).Length - 1)
                            mode = 0;
                        CConfig.TimerMode = (ETimerMode)mode;
                        break;

                    case Keys.I:
                        mode = (int)CConfig.PlayerInfo;

                        mode++;
                        if (mode > Enum.GetNames(typeof(EPlayerInfo)).Length - 1)
                            mode = 0;
                        CConfig.PlayerInfo = (EPlayerInfo)mode;
                        CConfig.SaveConfig();
                        _SetVisibility();
                        if (CGame.GetSong() != null)
                            _SetDuetLyricsVisibility(CGame.GetSong().IsDuet); //make sure duet lyrics remain visible
                        break;

                    case Keys.S:
                        if (CGame.NumRounds > CGame.RoundNr)
                        {
                            if (keyEvent.ModCtrl)
                                _LoadNextSong();
                        }
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
                        if (Buttons[_ButtonContinue].Selected && _Pause)
                            _TogglePause();
                        if (Buttons[_ButtonCancel].Selected && _Pause)
                            _Stop();
                        if (Buttons[_ButtonSkip].Selected && _Pause)
                        {
                            _LoadNextSong();
                            _TogglePause();
                        }
                        break;

                    case Keys.Add:
                    case Keys.PageUp:
                        if (keyEvent.ModShift)
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
                        if (keyEvent.ModShift)
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

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.RB)
            {
                _TogglePause();
                if (_Pause)
                    SetInteractionToButton(Buttons[_ButtonContinue]);
            }

            if (mouseEvent.LB && IsMouseOver(mouseEvent) && _Pause)
            {
                if (Buttons[_ButtonContinue].Selected && _Pause)
                    _TogglePause();

                if (Buttons[_ButtonCancel].Selected && _Pause)
                    _Stop();

                if (Buttons[_ButtonSkip].Selected && _Pause)
                {
                    _LoadNextSong();
                    _TogglePause();
                }
            }


            return true;
        }

        public override bool UpdateGame()
        {
            bool finish = false;
            if (CSound.IsPlaying(_CurrentStream) || CSound.IsPaused(_CurrentStream))
            {
                _CurrentTime = CSound.GetPosition(_CurrentStream);

                if (_FinishTime != 0 && _CurrentTime >= _FinishTime)
                    finish = true;
            }
            else
                finish = true;

            if (finish)
                _LoadNextSong();

            _UpdateSongText();
            _UpdateDuetText();
            if (_FadeOut)
                return true;

            _UpdateTimeLine();

            CGame.UpdatePoints(_CurrentTime);
            _UpdateLyrics();

            float[] alpha = _CalcFadingAlpha();
            if (alpha != null)
            {
                Lyrics[_LyricMain].Alpha = alpha[0];
                Lyrics[_LyricSub].Alpha = alpha[1];

                Lyrics[_LyricMainTop].Alpha = alpha[0];
                Lyrics[_LyricSubTop].Alpha = alpha[1];

                Statics[_StaticLyrics].Alpha = alpha[0];
                Statics[_StaticLyricsTop].Alpha = alpha[0];

                Statics[_StaticLyricHelper].Alpha = alpha[0];
                Statics[_StaticLyricHelperTop].Alpha = alpha[0];

                for (int p = 0; p < CGame.NumPlayer; p++)
                {
                    SingNotes[_SingBars].SetAlpha(_NoteLines[p], alpha[CGame.Player[p].LineNr * 2]);
                    if (CConfig.FadePlayerInfo == EFadePlayerInfo.TR_CONFIG_FADEPLAYERINFO_INFO || CConfig.FadePlayerInfo == EFadePlayerInfo.TR_CONFIG_FADEPLAYERINFO_ALL)
                    {
                        Statics[_StaticAvatars[p, CGame.NumPlayer - 1]].Alpha = alpha[CGame.Player[p].LineNr * 2];
                        Texts[_TextNames[p, CGame.NumPlayer - 1]].Alpha = alpha[CGame.Player[p].LineNr * 2];
                    }
                    if (CConfig.FadePlayerInfo == EFadePlayerInfo.TR_CONFIG_FADEPLAYERINFO_ALL)
                    {
                        Statics[_StaticScores[p, CGame.NumPlayer - 1]].Alpha = alpha[CGame.Player[p].LineNr * 2];
                        Statics[_StaticAvatars[p, CGame.NumPlayer - 1]].Alpha = alpha[CGame.Player[p].LineNr * 2];
                        Texts[_TextNames[p, CGame.NumPlayer - 1]].Alpha = alpha[CGame.Player[p].LineNr * 2];
                        Texts[_TextScores[p, CGame.NumPlayer - 1]].Alpha = alpha[CGame.Player[p].LineNr * 2];
                    }
                }

                if (alpha.Length > 2)
                {
                    Lyrics[_LyricMainDuet].Alpha = alpha[0];
                    Lyrics[_LyricSubDuet].Alpha = alpha[1];

                    Statics[_StaticLyricsDuet].Alpha = alpha[0];
                    Statics[_StaticLyricHelperDuet].Alpha = alpha[0];

                    Lyrics[_LyricMain].Alpha = alpha[2];
                    Lyrics[_LyricSub].Alpha = alpha[3];

                    Statics[_StaticLyrics].Alpha = alpha[2];
                    Statics[_StaticLyricHelper].Alpha = alpha[2];
                }
            }


            for (int p = 0; p < CGame.NumPlayer; p++)
            {
                if (CGame.Player[p].Points < 10000)
                    Texts[_TextScores[p, CGame.NumPlayer - 1]].Text = CGame.Player[p].Points.ToString("0000");
                else
                    Texts[_TextScores[p, CGame.NumPlayer - 1]].Text = CGame.Player[p].Points.ToString("00000");
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

            SingNotes[_SingBars].Reset();
            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
                _NoteLines[i] = -1;

            foreach (CLyric lyric in Lyrics)
                lyric.LyricStyle = CConfig.LyricStyle;

            for (int p = 0; p < CGame.NumPlayer; p++)
                Statics[_StaticAvatars[p, CGame.NumPlayer - 1]].Aspect = EAspect.Crop;
            _SetVisibility();

            _UpdateAvatars();
            _UpdateNames();

            CBackgroundMusic.Disabled = true;
            _CloseSong();
        }

        public override void OnShowFinish()
        {
            base.OnShowFinish();

            CGame.Start();
            _LoadNextSong();
            CBackgroundMusic.Disabled = true;
        }

        public override bool Draw()
        {
            if (_Active)
            {
                if (_CurrentVideo != -1 && CConfig.VideosInSongs == EOffOn.TR_CONFIG_ON && !_Webcam)
                {
                    RectangleF bounds = new RectangleF(0, 0, CSettings.RenderW, CSettings.RenderH);
                    RectangleF rect = new RectangleF(0f, 0f, _CurrentVideoTexture.Width, _CurrentVideoTexture.Height);
                    CHelper.SetRect(bounds, ref rect, rect.Width / rect.Height, _VideoAspect);

                    CDraw.DrawTexture(_CurrentVideoTexture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, 0f),
                                      _CurrentVideoTexture.Color, new SRectF(bounds.X, bounds.Y, bounds.Width, bounds.Height, 0f), false);
                }
                else if (_Webcam)
                {
                    RectangleF bounds = new RectangleF(0, 0, CSettings.RenderW, CSettings.RenderH);
                    RectangleF rect = new RectangleF(0f, 0f, _CurrentWebcamFrameTexture.Width, _CurrentWebcamFrameTexture.Height);
                    CHelper.SetRect(bounds, ref rect, rect.Width / rect.Height, _VideoAspect);

                    CDraw.DrawTexture(_CurrentWebcamFrameTexture, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, 0f),
                                      _CurrentVideoTexture.Color, new SRectF(bounds.X, bounds.Y, bounds.Width, bounds.Height, 0f), false);
                }
                else
                {
                    // Draw Background
                    RectangleF bounds = new RectangleF(0, 0, CSettings.RenderW, CSettings.RenderH);
                    RectangleF rect = new RectangleF(0f, 0f, _Background.Width, _Background.Height);
                    CHelper.SetRect(bounds, ref rect, rect.Width / rect.Height, EAspect.Crop);

                    CDraw.DrawTexture(_Background, new SRectF(rect.X, rect.Y, rect.Width, rect.Height, 0f),
                                      _Background.Color, new SRectF(bounds.X, bounds.Y, bounds.Width, bounds.Height, 0f), false);
                }
            }

            DrawBG();

            foreach (CStatic stat in Statics)
                stat.Draw();

            foreach (CText text in Texts)
                text.Draw();

            switch (CConfig.TimerLook)
            {
                case ETimerLook.TR_CONFIG_TIMERLOOK_NORMAL:
                    CDraw.DrawTexture(Statics[_StaticTimeLine].Texture, Statics[_StaticTimeLine].Rect, new SColorF(1, 1, 1, 1), _TimeLineRect);
                    break;
                case ETimerLook.TR_CONFIG_TIMERLOOK_EXPANDED:
                    for (int i = 0; i < _TimeRects.Count; i++)
                        CDraw.DrawTexture(_TimeRects[i].Rect.Texture, Statics[_StaticTimeLine].Rect, _TimeRects[i].Rect.Color, _TimeRects[i].Rect.Rect);
                    break;
            }

            Lyrics[_LyricSub].Draw(-100);
            Lyrics[_LyricMain].Draw(CGame.Beat);

            Lyrics[_LyricSubDuet].Draw(-100);
            Lyrics[_LyricMainDuet].Draw(CGame.Beat);

            Lyrics[_LyricSubTop].Draw(-100);
            Lyrics[_LyricMainTop].Draw(CGame.Beat);


            for (int i = 0; i < CGame.NumPlayer; i++)
                SingNotes[_SingBars].Draw(_NoteLines[i], CGame.Player[i].SingLine, i);

            _DrawLyricHelper();

            if (_Pause)
            {
                Statics[_StaticPauseBG].ForceDraw();
                Texts[_TextPause].ForceDraw();

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
            _CloseSong();
        }

        public override void ApplyVolume()
        {
            CSound.SetStreamVolumeMax(_CurrentStream, CConfig.GameMusicVolume);
        }

        private void _CloseSong()
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

            Lyrics[_LyricMain].Clear();
            Lyrics[_LyricSub].Clear();
            Lyrics[_LyricMainDuet].Clear();
            Lyrics[_LyricSubDuet].Clear();
            Lyrics[_LyricMainTop].Clear();
            Lyrics[_LyricSubTop].Clear();
            Texts[_TextSongName].Text = String.Empty;
            Texts[_TextDuetName1].Text = String.Empty;
            Texts[_TextDuetName2].Text = String.Empty;
            GC.Collect();
        }

        private void _LoadNextSong()
        {
            _CloseSong();

            CGame.NextRound();

            if (CGame.IsFinished())
            {
                _FinishedSinging();
                return;
            }

            CSong song = CGame.GetSong();

            if (song == null)
            {
                CLog.LogError("Critical Error! ScreenSing.LoadNextSong() song is null!");
                return;
            }

            string songname = song.Artist + " - " + song.Title;
            int rounds = CGame.GetNumSongs();
            if (rounds > 1)
                songname += " (" + CGame.RoundNr + "/" + rounds.ToString() + ")";
            Texts[_TextSongName].Text = songname;

            _CurrentStream = CSound.Load(song.GetMP3(), true);
            CSound.SetStreamVolumeMax(_CurrentStream, CConfig.GameMusicVolume);
            CSound.SetStreamVolume(_CurrentStream, _Volume);
            CSound.SetPosition(_CurrentStream, song.Start);
            _CurrentTime = song.Start;
            _FinishTime = song.Finish;
            _TimeToFirstNote = 0f;
            _TimeToFirstNoteDuet = 0f;
            int[] duetPlayer = new int[CGame.NumPlayer];
            if (song.IsDuet)
            {
                //Save duet-assignment before resetting
                for (int i = 0; i < duetPlayer.Length; i++)
                    duetPlayer[i] = CGame.Player[i].LineNr;
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

            SingNotes[_SingBars].Reset();


            if (song.IsDuet)
            {
                Texts[_TextDuetName1].Text = song.DuetPart1;
                Texts[_TextDuetName2].Text = song.DuetPart2;
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
                        CGame.Player[i].LineNr = duetPlayer[i];
                }
            }
            _SetDuetLyricsVisibility(song.IsDuet);

            for (int p = 0; p < CGame.NumPlayer; p++)
            {
                _NoteLines[p] = SingNotes[_SingBars].AddPlayer(
                    SingNotes[_SingBars].BarPos[p, CGame.NumPlayer - 1],
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
                _TimerSongText.Start();

            _StartSong();
        }

        private void _SetDuetLyricsVisibility(bool isDuet)
        {
            Statics[_StaticLyricsDuet].Visible = isDuet;
            Lyrics[_LyricMainDuet].Visible = isDuet;
            Lyrics[_LyricSubDuet].Visible = isDuet;

            if (isDuet)
            {
                Lyrics[_LyricMainTop].Visible = false;
                Lyrics[_LyricSubTop].Visible = false;
                Statics[_StaticLyricsTop].Visible = false;
            }
            else
            {
                bool lyricsOnTop = (CGame.NumPlayer != 1) && CConfig.LyricsOnTop == EOffOn.TR_CONFIG_ON;
                Lyrics[_LyricMainTop].Visible = lyricsOnTop;
                Lyrics[_LyricSubTop].Visible = lyricsOnTop;
                Statics[_StaticLyricsTop].Visible = lyricsOnTop;
            }
        }

        private void _StartSong()
        {
            _PrepareTimeLine();
            CSound.Play(_CurrentStream);
            CSound.RecordStart();
            if (_Webcam)
                CWebcam.Start();
        }

        private void _Stop()
        {
            //Need this to set other songs to points-var
            while (!CGame.IsFinished())
                CGame.NextRound();

            _FinishedSinging();
        }

        private void _FinishedSinging()
        {
            _FadeOut = true;
            CParty.FinishedSinging();

            if (_Webcam)
                CWebcam.Close();
        }

        private int _FindCurrentLine(CLines lines, CLine[] line, CSong song)
        {
            float currentTime = _CurrentTime - song.Gap;
            //We are only interested in the last matching line, so either do not check further after line[j].StartBeat > _CurrentBeat or go backwards!
            int j = lines.FindPreviousLine(_CurrentBeat);
            for (; j >= 0; j--)
            {
                float firstNoteTime = CGame.GetTimeFromBeats(line[j].FirstNoteBeat, song.BPM);
                //Earlist possible line break is 10s before first note
                if (firstNoteTime <= currentTime + 10f)
                {
                    //First line has no predecessor or line has to be shown
                    if (j == 0 || firstNoteTime - CConfig.MinLineBreakTime <= currentTime)
                        return j;
                    float lastNoteTime = CGame.GetTimeFromBeats(line[j - 1].LastNoteBeat, song.BPM);
                    //No line break if last line is not fully evaluated (with 50% tolerance -> tested so notes are drawn)
                    if (lastNoteTime + CConfig.MicDelay / 1000f * 1.5f >= currentTime)
                        return j - 1;
                    return j;
                }
            }
            return -1;
        }

        private void _UpdateLyrics()
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
                int nr = _FindCurrentLine(lines, line, song);

                if (nr != -1)
                {
                    for (int j = 0; j < CGame.NumPlayer; j++)
                    {
                        if (CGame.Player[j].LineNr == i)
                            SingNotes[_SingBars].AddLine(_NoteLines[j], line, nr, j);
                    }

                    if (i == 0 && !song.IsDuet || i == 1 && song.IsDuet)
                    {
                        Lyrics[_LyricMain].SetLine(line[nr]);
                        Lyrics[_LyricMainTop].SetLine(line[nr]);
                        _TimeToFirstNote = CGame.GetTimeFromBeats(line[nr].FirstNoteBeat - line[nr].StartBeat, song.BPM);
                        _RemainingTimeToFirstNote = CGame.GetTimeFromBeats(line[nr].FirstNoteBeat - CGame.GetBeatFromTime(_CurrentTime, song.BPM, song.Gap), song.BPM);

                        if (line.Length >= nr + 2)
                        {
                            Lyrics[_LyricSub].SetLine(line[nr + 1]);
                            Lyrics[_LyricSubTop].SetLine(line[nr + 1]);
                        }
                        else
                        {
                            Lyrics[_LyricSub].Clear();
                            Lyrics[_LyricSubTop].Clear();
                        }
                    }
                    if (i == 0 && song.IsDuet)
                    {
                        Lyrics[_LyricMainDuet].SetLine(line[nr]);
                        _TimeToFirstNoteDuet = CGame.GetTimeFromBeats(line[nr].FirstNoteBeat - line[nr].StartBeat, song.BPM);
                        _RemainingTimeToFirstNoteDuet = CGame.GetTimeFromBeats(line[nr].FirstNoteBeat - CGame.GetBeatFromTime(_CurrentTime, song.BPM, song.Gap), song.BPM);

                        if (line.Length >= nr + 2)
                            Lyrics[_LyricSubDuet].SetLine(line[nr + 1]);
                        else
                            Lyrics[_LyricSubDuet].Clear();
                    }
                }
                else
                {
                    if (i == 0 && !song.IsDuet || i == 1 && song.IsDuet)
                    {
                        Lyrics[_LyricMain].Clear();
                        Lyrics[_LyricSub].Clear();
                        Lyrics[_LyricMainTop].Clear();
                        Lyrics[_LyricSubTop].Clear();
                        _TimeToFirstNote = 0f;
                    }

                    if (i == 0 && song.IsDuet)
                    {
                        Lyrics[_LyricMainDuet].Clear();
                        Lyrics[_LyricSubDuet].Clear();
                        _TimeToFirstNoteDuet = 0f;
                    }
                }
            }
        }

        private void _TogglePause()
        {
            _Pause = !_Pause;
            if (_Pause)
            {
                Buttons[_ButtonCancel].Visible = true;
                Buttons[_ButtonContinue].Visible = true;
                if (CGame.NumRounds > CGame.RoundNr && CGame.NumRounds > 1)
                    Buttons[_ButtonSkip].Visible = true;
                else
                    Buttons[_ButtonSkip].Visible = false;
                CSound.Pause(_CurrentStream);
            }
            else
            {
                Buttons[_ButtonCancel].Visible = false;
                Buttons[_ButtonContinue].Visible = false;
                Buttons[_ButtonSkip].Visible = false;
                CSound.Play(_CurrentStream);
                CWebcam.Start();
            }
        }

        private void _BuildTextStrings(ref List<string> texts)
        {
            _TextScores = new string[CSettings.MaxNumPlayer,CSettings.MaxNumPlayer];
            _TextNames = new string[CSettings.MaxNumPlayer,CSettings.MaxNumPlayer];

            for (int numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (int player = 0; player < CSettings.MaxNumPlayer; player++)
                {
                    if (player <= numplayer)
                    {
                        string target = "P" + (player + 1).ToString() + "N" + (numplayer + 1).ToString();
                        _TextScores[player, numplayer] = "TextScore" + target;
                        _TextNames[player, numplayer] = "TextName" + target;

                        texts.Add(_TextScores[player, numplayer]);
                        texts.Add(_TextNames[player, numplayer]);
                    }
                }
            }
        }

        private void _BuildStaticStrings(ref List<string> statics)
        {
            _StaticScores = new string[CSettings.MaxNumPlayer,CSettings.MaxNumPlayer];
            _StaticAvatars = new string[CSettings.MaxNumPlayer,CSettings.MaxNumPlayer];

            for (int numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (int player = 0; player < CSettings.MaxNumPlayer; player++)
                {
                    if (player <= numplayer)
                    {
                        string target = "P" + (player + 1).ToString() + "N" + (numplayer + 1).ToString();
                        _StaticScores[player, numplayer] = "StaticScore" + target;
                        _StaticAvatars[player, numplayer] = "StaticAvatar" + target;

                        statics.Add(_StaticScores[player, numplayer]);
                        statics.Add(_StaticAvatars[player, numplayer]);
                    }
                }
            }
        }

        private void _SetVisibility()
        {
            Statics[_StaticLyricsDuet].Visible = false;
            Statics[_StaticLyricHelper].Visible = false;
            Statics[_StaticLyricHelperDuet].Visible = false;
            Statics[_StaticLyricHelperTop].Visible = false;
            Lyrics[_LyricMainDuet].Visible = false;
            Lyrics[_LyricSubDuet].Visible = false;

            Statics[_StaticSongText].Visible = false;
            Texts[_TextSongName].Visible = false;
            Texts[_TextDuetName1].Visible = false;
            Texts[_TextDuetName2].Visible = false;

            for (int numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (int player = 0; player < CSettings.MaxNumPlayer; player++)
                {
                    if (player <= numplayer)
                    {
                        Texts[_TextScores[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayer;
                        Texts[_TextNames[player, numplayer]].Visible = (numplayer + 1 == CGame.NumPlayer)
                                                                      &&
                                                                      (CConfig.PlayerInfo == EPlayerInfo.TR_CONFIG_PLAYERINFO_BOTH ||
                                                                       CConfig.PlayerInfo == EPlayerInfo.TR_CONFIG_PLAYERINFO_NAME);
                        Statics[_StaticScores[player, numplayer]].Visible = numplayer + 1 == CGame.NumPlayer;
                        Statics[_StaticAvatars[player, numplayer]].Visible = (numplayer + 1 == CGame.NumPlayer)
                                                                            &&
                                                                            (CConfig.PlayerInfo == EPlayerInfo.TR_CONFIG_PLAYERINFO_BOTH ||
                                                                             CConfig.PlayerInfo == EPlayerInfo.TR_CONFIG_PLAYERINFO_AVATAR);
                    }
                }
            }

            Lyrics[_LyricMain].Alpha = 0f;
            Lyrics[_LyricSub].Alpha = 0f;

            Lyrics[_LyricMainTop].Alpha = 0f;
            Lyrics[_LyricSubTop].Alpha = 0f;

            Statics[_StaticLyrics].Alpha = 0f;
            Statics[_StaticLyricsTop].Alpha = 0f;

            Statics[_StaticLyricHelper].Alpha = 0f;
            Statics[_StaticLyricHelperTop].Alpha = 0f;

            Lyrics[_LyricMainDuet].Alpha = 0f;
            Lyrics[_LyricSubDuet].Alpha = 0f;

            Statics[_StaticLyricsDuet].Alpha = 0f;
            Statics[_StaticLyricHelperDuet].Alpha = 0f;
        }

        private void _DrawLyricHelper()
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

                SRectF rect = Statics[_StaticLyricHelper].Rect;
                SColorF color = new SColorF(
                    Statics[_StaticLyricHelper].Color.R,
                    Statics[_StaticLyricHelper].Color.G,
                    Statics[_StaticLyricHelper].Color.B,
                    Statics[_StaticLyricHelper].Color.A * Statics[_StaticLyricHelper].Alpha * alpha);

                float distance = Lyrics[_LyricMain].GetCurrentLyricPosX() - rect.X - rect.W;
                CDraw.DrawTexture(Statics[_StaticLyricHelper].Texture,
                                  new SRectF(rect.X + distance * (1f - time / totaltime), rect.Y, rect.W, rect.H, rect.Z), color);

                if (Statics[_StaticLyricsTop].Visible)
                {
                    rect = Statics[_StaticLyricHelperTop].Rect;
                    color = new SColorF(
                        Statics[_StaticLyricHelperTop].Color.R,
                        Statics[_StaticLyricHelperTop].Color.G,
                        Statics[_StaticLyricHelperTop].Color.B,
                        Statics[_StaticLyricHelperTop].Color.A * Statics[_StaticLyricHelper].Alpha * alpha);

                    distance = Lyrics[_LyricMainTop].GetCurrentLyricPosX() - rect.X - rect.W;
                    CDraw.DrawTexture(Statics[_StaticLyricHelperTop].Texture,
                                      new SRectF(rect.X + distance * (1f - time / totaltime), rect.Y, rect.W, rect.H, rect.Z), color);
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

                    SRectF rect = Statics[_StaticLyricHelperDuet].Rect;

                    SColorF color = new SColorF(
                        Statics[_StaticLyricHelperDuet].Color.R,
                        Statics[_StaticLyricHelperDuet].Color.G,
                        Statics[_StaticLyricHelperDuet].Color.B,
                        Statics[_StaticLyricHelperDuet].Color.A * Statics[_StaticLyricHelperDuet].Alpha * alpha);

                    float distance = Lyrics[_LyricMainDuet].GetCurrentLyricPosX() - rect.X - rect.W;
                    CDraw.DrawTexture(Statics[_StaticLyricHelperDuet].Texture,
                                      new SRectF(rect.X + distance * (1f - time / totaltime), rect.Y, rect.W, rect.H, rect.Z),
                                      color);
                }
            }
        }

        private float[] _CalcFadingAlpha()
        {
            float dt = 4f;
            float rt = dt * 0.8f;

            CSong song = CGame.GetSong();
            if (song == null)
                return null;
            else if (!song.NotesLoaded)
                return null;

            float[] alpha = new float[song.Notes.Lines.Length * 2];
            float currentTime = _CurrentTime - song.Gap;

            for (int i = 0; i < song.Notes.LinesCount; i++)
            {
                CLines lines = song.Notes.GetLines(i);
                CLine[] line = lines.Line;

                // find current line for lyric sub fading
                int currentLineSub = _FindCurrentLine(lines, line, song);

                // find current line for lyric main fading
                int currentLine = 0;
                for (int j = 0; j < line.Length; j++)
                {
                    if (line[j].FirstNoteBeat <= _CurrentBeat)
                        currentLine = j;
                }

                // default values
                alpha[i * 2] = 1f;
                alpha[i * 2 + 1] = 1f;

                // main line alpha
                if (currentLine == 0 && currentTime < CGame.GetTimeFromBeats(line[currentLine].FirstNoteBeat, song.BPM))
                {
                    // first main line and fist note is not reached
                    // => fade in
                    float diff = CGame.GetTimeFromBeats(line[currentLine].FirstNoteBeat, song.BPM) - currentTime;
                    if (diff > dt)
                        alpha[i * 2] = 1f - (diff - dt) / rt;
                }
                else if (currentLine < line.Length - 1 && CGame.GetTimeFromBeats(line[currentLine].LastNoteBeat, song.BPM) < currentTime &&
                         CGame.GetTimeFromBeats(line[currentLine + 1].FirstNoteBeat, song.BPM) > currentTime)
                {
                    // current position is between two lines

                    // time between the to lines
                    float diff = CGame.GetTimeFromBeats(line[currentLine + 1].FirstNoteBeat, song.BPM) -
                                 CGame.GetTimeFromBeats(line[currentLine].LastNoteBeat, song.BPM);

                    // fade only if there is enough time for fading
                    if (diff > 3.3f * dt)
                    {
                        // time elapsed since last line
                        float last = currentTime - CGame.GetTimeFromBeats(line[currentLine].LastNoteBeat, song.BPM);

                        // time to next line
                        float next = CGame.GetTimeFromBeats(line[currentLine + 1].FirstNoteBeat, song.BPM) - currentTime;

                        if (last < next)
                        {
                            // fade out
                            alpha[i * 2] = 1f - last / rt;
                        }
                        else
                        {
                            // fade in if it is time for
                            if (next > dt)
                                alpha[i * 2] = 1f - (next - dt) / rt;
                        }
                    }
                }
                else if (currentLine == line.Length - 1 && CGame.GetTimeFromBeats(line[currentLine].LastNoteBeat, song.BPM) < currentTime)
                {
                    // last main line and last note was reached
                    // => fade out
                    float diff = currentTime - CGame.GetTimeFromBeats(line[currentLine].LastNoteBeat, song.BPM);
                    alpha[i * 2] = 1f - diff / rt;
                }

                // sub
                if (currentLineSub < line.Length - 2)
                {
                    float diff = 0f;
                    diff = CGame.GetTimeFromBeats(line[currentLineSub + 1].FirstNoteBeat, song.BPM) - currentTime;

                    if (diff > dt)
                        alpha[i * 2 + 1] = 1f - (diff - dt) / rt;
                }

                if (alpha[i * 2] < 0f)
                    alpha[i * 2] = 0f;

                if (alpha[i * 2 + 1] < 0f)
                    alpha[i * 2 + 1] = 0f;
            }

            return alpha;
        }

        private void _UpdateSongText()
        {
            if (_TimerSongText.IsRunning && !Lyrics[_LyricMainDuet].Visible && !Lyrics[_LyricMainTop].Visible)
            {
                float t = _TimerSongText.ElapsedMilliseconds / 1000f;
                if (t < 10f)
                {
                    Statics[_StaticSongText].Visible = true;
                    Texts[_TextSongName].Visible = true;

                    if (t < 7f)
                    {
                        Statics[_StaticSongText].Color.A = 1f;
                        Texts[_TextSongName].Color.A = 1f;
                    }
                    else
                    {
                        Statics[_StaticSongText].Color.A = (3f - (t - 7f)) / 3f;
                        Texts[_TextSongName].Color.A = (3f - (t - 7f)) / 3f;
                    }
                }
                else
                {
                    Statics[_StaticSongText].Visible = false;
                    Texts[_TextSongName].Visible = false;
                    _TimerSongText.Stop();
                }
            }
            else
            {
                Statics[_StaticSongText].Visible = false;
                Texts[_TextSongName].Visible = false;
            }
        }

        private void _UpdateDuetText()
        {
            if (CGame.GetSong() != null)
            {
                //Timer for first duet-part
                if (_TimerDuetText1.IsRunning)
                {
                    float t = _TimerDuetText1.ElapsedMilliseconds / 1000f;
                    if (t < 10f)
                    {
                        Texts[_TextDuetName1].Visible = true;

                        if (t < 3f)
                            Texts[_TextDuetName1].Color.A = (3f - (3f - t)) / 3f;
                        else if (t < 7f)
                            Texts[_TextDuetName1].Color.A = 1f;
                        else
                            Texts[_TextDuetName1].Color.A = (3f - (t - 7f)) / 3f;
                    }
                    else
                    {
                        Texts[_TextDuetName1].Visible = false;
                        _TimerDuetText1.Stop();
                    }
                }
                else if (!_TimerDuetText1.IsRunning && _TimerDuetText1.ElapsedMilliseconds == 0 && Lyrics[_LyricMainDuet].Alpha > 0 && CGame.GetSong().IsDuet)
                    _TimerDuetText1.Start();
                else
                    Texts[_TextDuetName1].Visible = false;
                //Timer for second duet-part
                if (_TimerDuetText2.IsRunning)
                {
                    float t = _TimerDuetText2.ElapsedMilliseconds / 1000f;
                    if (t < 10f)
                    {
                        Texts[_TextDuetName2].Visible = true;

                        if (t < 3f)
                            Texts[_TextDuetName2].Color.A = (3f - (3f - t)) / 3f;
                        else if (t < 7f)
                            Texts[_TextDuetName2].Color.A = 1f;
                        else
                            Texts[_TextDuetName2].Color.A = (3f - (t - 7f)) / 3f;
                    }
                    else
                    {
                        Texts[_TextDuetName2].Visible = false;
                        _TimerDuetText2.Stop();
                    }
                }
                else if (!_TimerDuetText2.IsRunning && _TimerDuetText2.ElapsedMilliseconds == 0 && Lyrics[_LyricMain].Alpha > 0 && CGame.GetSong().IsDuet)
                    _TimerDuetText2.Start();
                else
                    Texts[_TextDuetName2].Visible = false;
            }
        }

        private void _UpdateTimeLine()
        {
            CSong song = CGame.GetSong();

            if (song == null)
                return;

            float totalTime = CSound.GetLength(_CurrentStream);
            if (song.Finish != 0)
                totalTime = song.Finish;

            float remainingTime = totalTime - _CurrentTime;
            totalTime -= song.Start;
            float currentTime = _CurrentTime - song.Start;

            if (totalTime <= 0f)
                return;

            switch (CConfig.TimerMode)
            {
                case ETimerMode.TR_CONFIG_TIMERMODE_CURRENT:
                    int min = (int)Math.Floor(currentTime / 60f);
                    int sec = (int)(currentTime - min * 60f);
                    Texts[_TextTime].Text = min.ToString("00") + ":" + sec.ToString("00");
                    break;

                case ETimerMode.TR_CONFIG_TIMERMODE_REMAINING:
                    min = (int)Math.Floor(remainingTime / 60f);
                    sec = (int)(remainingTime - min * 60f);
                    Texts[_TextTime].Text = "-" + min.ToString("00") + ":" + sec.ToString("00");
                    break;

                case ETimerMode.TR_CONFIG_TIMERMODE_TOTAL:
                    min = (int)Math.Floor(totalTime / 60f);
                    sec = (int)(totalTime - min * 60f);
                    Texts[_TextTime].Text = "#" + min.ToString("00") + ":" + sec.ToString("00");
                    break;
            }


            switch (CConfig.TimerLook)
            {
                case ETimerLook.TR_CONFIG_TIMERLOOK_NORMAL:
                    _TimeLineRect.W = Statics[_StaticTimeLine].Rect.W * (currentTime / totalTime);
                    break;

                case ETimerLook.TR_CONFIG_TIMERLOOK_EXPANDED:
                    CStatic stat = Statics[_StaticTimeLine];
                    int currentBeat = CGame.CurrentBeat;
                    for (int i = 0; i < _TimeRects.Count; i++)
                    {
                        if (currentBeat >= _TimeRects[i].StartBeat && currentBeat <= _TimeRects[i].EndBeat)
                        {
                            _TimeRects[i].Rect.Texture = Statics[_StaticTimeLineExpandedHighlighted].Texture;
                            _TimeRects[i].Rect.Color = Statics[_StaticTimeLineExpandedHighlighted].Color;
                        }
                        else
                        {
                            _TimeRects[i].Rect.Texture = Statics[_StaticTimeLineExpandedNormal].Texture;
                            _TimeRects[i].Rect.Color = Statics[_StaticTimeLineExpandedNormal].Color;
                        }
                    }
                    Statics[_StaticTimePointer].Rect.X = stat.Rect.X + stat.Rect.W * (currentTime / totalTime);
                    break;
            }
        }

        private void _PrepareTimeLine()
        {
            CStatic stat = Statics[_StaticTimeLine];
            switch (CConfig.TimerLook)
            {
                case ETimerLook.TR_CONFIG_TIMERLOOK_NORMAL:
                    _TimeLineRect = new SRectF(stat.Rect.X, stat.Rect.Y, 0f, stat.Rect.H, stat.Rect.Z);
                    Statics[_StaticTimePointer].Visible = false;
                    break;

                case ETimerLook.TR_CONFIG_TIMERLOOK_EXPANDED:
                    _TimeRects.Clear();
                    Statics[_StaticTimePointer].Visible = true;

                    CSong song = CGame.GetSong();

                    if (song == null)
                        return;

                    float totalTime = CSound.GetLength(_CurrentStream);
                    if (song.Finish != 0)
                        totalTime = song.Finish;

                    totalTime -= song.Start;

                    if (totalTime <= 0f)
                        return;

                    CLines[] lines = new CLines[song.Notes.Lines.Length];
                    lines = song.Notes.Lines;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        CLine[] line = lines[i].Line;
                        for (int j = 0; j < line.Length; j++)
                        {
                            if (line[j].VisibleInTimeLine)
                            {
                                STimeRect trect = new STimeRect();
                                trect.StartBeat = line[j].FirstNoteBeat;
                                trect.EndBeat = line[j].EndBeat;
                                trect.Rect = GetNewStatic(new STexture(-1),
                                                          new SColorF(1f, 1f, 1f, 1f),
                                                          new SRectF(
                                                              stat.Rect.X + stat.Rect.W * ((CGame.GetTimeFromBeats(trect.StartBeat, song.BPM) + song.Gap - song.Start) / totalTime),
                                                              stat.Rect.Y,
                                                              stat.Rect.W * (CGame.GetTimeFromBeats(trect.EndBeat - trect.StartBeat, song.BPM) / totalTime),
                                                              stat.Rect.H,
                                                              stat.Rect.Z));

                                _TimeRects.Add(trect);
                            }
                        }
                    }
                    break;
            }
        }

        private void _UpdateAvatars()
        {
            for (int i = 0; i < CGame.NumPlayer; i++)
            {
                if (CGame.Player[i].ProfileID > -1 && CProfiles.NumProfiles > CGame.Player[i].ProfileID)
                    Statics[_StaticAvatars[i, CGame.NumPlayer - 1]].Texture = CProfiles.Profiles[CGame.Player[i].ProfileID].Avatar.Texture;
                else
                    Statics[_StaticAvatars[i, CGame.NumPlayer - 1]].Visible = false;
            }
        }

        private void _UpdateNames()
        {
            for (int i = 0; i < CGame.NumPlayer; i++)
            {
                if (CGame.Player[i].ProfileID > -1 && CProfiles.NumProfiles > CGame.Player[i].ProfileID)
                    Texts[_TextNames[i, CGame.NumPlayer - 1]].Text = CProfiles.Profiles[CGame.Player[i].ProfileID].PlayerName;
                else
                    Texts[_TextNames[i, CGame.NumPlayer - 1]].Visible = false;
            }
        }
    }
}