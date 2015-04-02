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
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Vocaluxe.Base;
using Vocaluxe.Base.Fonts;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Menu;
using VocaluxeLib.Menu.SingNotes;
using VocaluxeLib.Songs;

namespace Vocaluxe.Screens
{
    public class CScreenSing : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 7; }
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
        private const string _TextMedleyCountdown = "TextMedleyCountdown";
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

        private bool _DynamicLyricsTop;
        private bool _DynamicLyricsBottom;

        private SRectF _TimeLineRect;
        private List<STimeRect> _TimeRects;
        private bool _FadeOut;

        private int _CurrentBeat;
        private int _CurrentStream = -1;
        private float _Length = -1f;

        private CVideoStream _CurrentVideo;
        private EAspect _VideoAspect = EAspect.Crop;
        private CTextureRef _CurrentWebcamFrameTexture;
        private CTextureRef _Background;

        private float _CurrentTime;
        private float _FinishTime;

        private float _TimeToFirstNote;
        private float _RemainingTimeToFirstNote;
        private float _TimeToFirstNoteDuet;
        private float _RemainingTimeToFirstNoteDuet;

        private Stopwatch _TimerSongText;
        private Stopwatch _TimerDuetText1;
        private Stopwatch _TimerDuetText2;

        private bool _Pause;
        private bool _Webcam;

        private CBackground _SlideShow;

        public override EMusicType CurrentMusicType
        {
            get { return EMusicType.Game; }
        }

        public override void Init()
        {
            base.Init();

            var texts = new List<string> {_TextSongName, _TextTime, _TextPause, _TextDuetName1, _TextDuetName2, _TextMedleyCountdown};
            _BuildTextStrings(texts);
            _ThemeTexts = texts.ToArray();

            var statics = new List<string>
                {
                    _StaticSongText,
                    _StaticLyrics,
                    _StaticLyricsDuet,
                    _StaticLyricsTop,
                    _StaticTimeBar,
                    _StaticTimeLine,
                    _StaticTimeLineExpandedNormal,
                    _StaticTimeLineExpandedHighlighted,
                    _StaticTimePointer,
                    _StaticLyricHelper,
                    _StaticLyricHelperDuet,
                    _StaticLyricHelperTop,
                    _StaticPauseBG
                };
            _BuildStaticStrings(ref statics);
            _ThemeStatics = statics.ToArray();

            _ThemeButtons = new string[] {_ButtonCancel, _ButtonContinue, _ButtonSkip};
            _ThemeLyrics = new string[] {_LyricMain, _LyricSub, _LyricMainDuet, _LyricSubDuet, _LyricMainTop, _LyricSubTop};
            _ThemeSingNotes = new string[] {_SingBars};

            _TimeRects = new List<STimeRect>();
            _TimerSongText = new Stopwatch();
            _TimerDuetText1 = new Stopwatch();
            _TimerDuetText2 = new Stopwatch();
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (!keyEvent.KeyPressed)
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                        _TogglePause();
                        if (_Pause)
                            _SelectElement(_Buttons[_ButtonCancel]);
                        break;

                    case Keys.P:
                        _TogglePause();
                        if (_Pause)
                            _SelectElement(_Buttons[_ButtonContinue]);
                        break;

                    case Keys.T:
                        var mode = (int)CConfig.Config.Game.TimerMode;

                        mode++;
                        if (mode > Enum.GetNames(typeof(ETimerMode)).Length - 1)
                            mode = 0;
                        CConfig.Config.Game.TimerMode = (ETimerMode)mode;
                        CConfig.SaveConfig();
                        break;

                    case Keys.I:
                        mode = (int)CConfig.Config.Theme.PlayerInfo;

                        mode++;
                        if (mode > Enum.GetNames(typeof(EPlayerInfo)).Length - 1)
                            mode = 0;
                        CConfig.Config.Theme.PlayerInfo = (EPlayerInfo)mode;
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
                        if (CWebcam.IsDeviceAvailable())
                        {
                            _Webcam = !CWebcam.IsCapturing();
                            if (_Webcam)
                                CWebcam.Start();
                            else
                                CWebcam.Stop();
                        }
                        break;
                    case Keys.Enter:
                        if (_Pause)
                        {
                            if (_Buttons[_ButtonContinue].Selected)
                                _SetPause(false);
                            if (_Buttons[_ButtonCancel].Selected)
                                _Stop();
                            if (_Buttons[_ButtonSkip].Selected)
                            {
                                _LoadNextSong();
                                _SetPause(false);
                            }
                        }
                        break;

                    case Keys.V:
                        if (_VideoAspect == EAspect.Zoom2)
                            _VideoAspect = EAspect.Crop;
                        else
                            _VideoAspect++;

                        CSong song = CGame.GetSong();
                        song.VideoAspect = _VideoAspect;

                        if (CConfig.Config.Debug.SaveModifiedSongs == EOffOn.TR_CONFIG_ON)
                            song.Save();
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
                    _SelectElement(_Buttons[_ButtonContinue]);
            }

            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent) && _Pause)
            {
                if (_Buttons[_ButtonContinue].Selected)
                    _SetPause(false);

                if (_Buttons[_ButtonCancel].Selected)
                    _Stop();

                if (_Buttons[_ButtonSkip].Selected)
                {
                    _LoadNextSong();
                    _SetPause(false);
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

                if (_FinishTime > 0.001 && _CurrentTime >= _FinishTime)
                    finish = true;
            }
            else
                finish = true;

            if (finish && !_FadeOut)
                _LoadNextSong();

            _UpdateSongText();
            _UpdateDuetNames();
            if (_FadeOut)
                return true;

            _UpdateTimeLine();

            CGame.UpdatePoints(_CurrentTime);
            _UpdateLyrics();
            if (CGame.GameMode == EGameMode.TR_GAMEMODE_MEDLEY)
                _UpdateMedleyCountdown();

            float[] alpha = _CalcFadingAlpha();
            if (alpha != null)
            {
                _Lyrics[_LyricMain].Alpha = alpha[0];
                _Lyrics[_LyricSub].Alpha = alpha[1];

                _Lyrics[_LyricMainTop].Alpha = alpha[0];
                _Lyrics[_LyricSubTop].Alpha = alpha[1];

                _Statics[_StaticLyrics].Alpha = alpha[0];
                _Statics[_StaticLyricsTop].Alpha = alpha[0];

                _Statics[_StaticLyricHelper].Alpha = alpha[0];
                _Statics[_StaticLyricHelperTop].Alpha = alpha[0];

                for (int p = 0; p < CGame.NumPlayers; p++)
                {
                    _SingNotes[_SingBars].PlayerNotes[p].Alpha = alpha[CGame.Players[p].VoiceNr * 2];
                    if (CConfig.Config.Theme.FadePlayerInfo == EFadePlayerInfo.TR_CONFIG_FADEPLAYERINFO_INFO ||
                        CConfig.Config.Theme.FadePlayerInfo == EFadePlayerInfo.TR_CONFIG_FADEPLAYERINFO_ALL)
                    {
                        _Statics[_StaticAvatars[p, CGame.NumPlayers - 1]].Alpha = alpha[CGame.Players[p].VoiceNr * 2];
                        _Texts[_TextNames[p, CGame.NumPlayers - 1]].Alpha = alpha[CGame.Players[p].VoiceNr * 2];
                    }
                    if (CConfig.Config.Theme.FadePlayerInfo == EFadePlayerInfo.TR_CONFIG_FADEPLAYERINFO_ALL)
                    {
                        _Statics[_StaticScores[p, CGame.NumPlayers - 1]].Alpha = alpha[CGame.Players[p].VoiceNr * 2];
                        _Statics[_StaticAvatars[p, CGame.NumPlayers - 1]].Alpha = alpha[CGame.Players[p].VoiceNr * 2];
                        _Texts[_TextNames[p, CGame.NumPlayers - 1]].Alpha = alpha[CGame.Players[p].VoiceNr * 2];
                        _Texts[_TextScores[p, CGame.NumPlayers - 1]].Alpha = alpha[CGame.Players[p].VoiceNr * 2];
                    }
                }

                if (alpha.Length > 2)
                {
                    _Lyrics[_LyricMainDuet].Alpha = alpha[0];
                    _Lyrics[_LyricSubDuet].Alpha = alpha[1];

                    _Statics[_StaticLyricsDuet].Alpha = alpha[0];
                    _Statics[_StaticLyricHelperDuet].Alpha = alpha[0];

                    _Lyrics[_LyricMain].Alpha = alpha[2];
                    _Lyrics[_LyricSub].Alpha = alpha[3];

                    _Statics[_StaticLyrics].Alpha = alpha[2];
                    _Statics[_StaticLyricHelper].Alpha = alpha[2];
                }
            }


            for (int p = 0; p < CGame.NumPlayers; p++)
            {
                string fmtString = (CGame.Players[p].Points < 10000) ? "0000" : "00000";
                _Texts[_TextScores[p, CGame.NumPlayers - 1]].Text = CGame.Players[p].Points.ToString(fmtString);
            }

            if (_CurrentVideo != null && !_FadeOut && CConfig.Config.Video.VideosInSongs == EOffOn.TR_CONFIG_ON)
                CVideo.GetFrame(_CurrentVideo, _CurrentTime);

            if (_Webcam)
                CWebcam.GetFrame(ref _CurrentWebcamFrameTexture);

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            _FadeOut = false;

            _CurrentVideo = null;
            _CurrentWebcamFrameTexture = null;
            _CurrentBeat = -100;
            _CurrentTime = 0f;
            _FinishTime = 0f;
            _Length = -1f;
            _TimeToFirstNote = 0f;
            _TimeToFirstNoteDuet = 0f;
            _SetPause(false);

            _TimeRects.Clear();

            _SingNotes[_SingBars].Init(0);

            foreach (CLyric lyric in _Lyrics)
                lyric.LyricStyle = CConfig.Config.Theme.LyricStyle;

            for (int p = 0; p < CGame.NumPlayers; p++)
                _Statics[_StaticAvatars[p, CGame.NumPlayers - 1]].Aspect = EAspect.Crop;
            _SetVisibility();

            _UpdateAvatars();
            _UpdateNames();

            _CloseSong();
        }

        public override void OnShowFinish()
        {
            base.OnShowFinish();

            CGame.Start();
            _LoadNextSong();
        }

        public override void Draw()
        {
            if (_Active)
            {
                CTextureRef background;
                var aspect = EAspect.Crop;
                if (_CurrentVideo != null && CConfig.Config.Video.VideosInSongs == EOffOn.TR_CONFIG_ON && !_Webcam)
                {
                    background = _CurrentVideo.Texture;
                    aspect = _VideoAspect;
                }
                else if (_Webcam)
                {
                    background = _CurrentWebcamFrameTexture;
                    aspect = _VideoAspect;
                }
                else
                    background = _Background;
                if (background != null)
                {
                    SRectF bounds = CSettings.RenderRect;
                    SRectF rect = CHelper.FitInBounds(bounds, background.OrigAspect, aspect);
                    CDraw.DrawTexture(background, rect, background.Color, bounds);
                }
                else if (_SlideShow != null)
                    _SlideShow.Draw();
            }

            base.Draw();

            switch (CConfig.Config.Theme.TimerLook)
            {
                case ETimerLook.TR_CONFIG_TIMERLOOK_NORMAL:
                    CDraw.DrawTexture(_Statics[_StaticTimeLine].Texture, _Statics[_StaticTimeLine].Rect, new SColorF(1, 1, 1, 1), _TimeLineRect);
                    break;
                case ETimerLook.TR_CONFIG_TIMERLOOK_EXPANDED:
                    for (int i = 0; i < _TimeRects.Count; i++)
                        CDraw.DrawTexture(_TimeRects[i].Rect.Texture, _Statics[_StaticTimeLine].Rect, _TimeRects[i].Rect.Color, _TimeRects[i].Rect.Rect);
                    break;
            }

            _DrawLyricHelper();
        }

        public override void OnClose()
        {
            base.OnClose();
            _CloseSong();
            if (_Webcam)
                CWebcam.Stop();
            CBackgroundMusic.Disabled = false;
        }

        private void _CloseSong()
        {
            if (_CurrentStream > -1)
            {
                CSound.FadeAndClose(_CurrentStream, 0, 0.5f);
                _CurrentStream = -1;
            }
            CRecord.Stop();
            if (_CurrentVideo != null)
                CVideo.Close(ref _CurrentVideo);
            CDraw.RemoveTexture(ref _Background);

            if (_SlideShow != null)
                _SlideShow.RemoveSlideShowTextures();

            _Lyrics[_LyricMain].Clear();
            _Lyrics[_LyricSub].Clear();
            _Lyrics[_LyricMainDuet].Clear();
            _Lyrics[_LyricSubDuet].Clear();
            _Lyrics[_LyricMainTop].Clear();
            _Lyrics[_LyricSubTop].Clear();
            _Texts[_TextSongName].Text = String.Empty;
            _Texts[_TextDuetName1].Text = String.Empty;
            _Texts[_TextDuetName2].Text = String.Empty;
            _Texts[_TextMedleyCountdown].Visible = false;
            _Texts[_TextMedleyCountdown].Text = String.Empty;
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
                songname += " (" + CGame.RoundNr + "/" + rounds + ")";
            _Texts[_TextSongName].Text = songname;

            _CurrentStream = CSound.Load(song.GetMP3(), false, true);
            CSound.SetStreamVolume(_CurrentStream, 100);
            CSound.SetPosition(_CurrentStream, song.Start);
            _CurrentTime = song.Start;
            _FinishTime = song.Finish;
            _TimeToFirstNote = 0f;
            _TimeToFirstNoteDuet = 0f;
            _Length = -1f;
            var voiceAssignments = new int[CGame.NumPlayers];
            if (song.IsDuet)
            {
                //Save duet-assignment before resetting
                for (int i = 0; i < voiceAssignments.Length; i++)
                    voiceAssignments[i] = CGame.Players[i].VoiceNr;
            }
            CGame.ResetPlayer();

            if (!String.IsNullOrEmpty(song.VideoFileName))
            {
                _CurrentVideo = CVideo.Load(Path.Combine(song.Folder, song.VideoFileName));
                CVideo.Skip(_CurrentVideo, song.Start, song.VideoGap);
                _VideoAspect = song.VideoAspect;
            }

            CDraw.RemoveTexture(ref _Background);
            if (song.BackgroundFileNames.Count > 1)
            {
                _SlideShow = GetNewBackground();
                foreach (string bgFile in song.BackgroundFileNames)
                    _SlideShow.AddSlideShowTexture(Path.Combine(song.Folder, bgFile));
            }
            else if (song.BackgroundFileNames.Count == 1)
            {
                if (!String.IsNullOrEmpty(song.BackgroundFileNames[0]))
                    _Background = CDraw.AddTexture(Path.Combine(song.Folder, song.BackgroundFileNames[0]));
            }

            if (song.IsDuet)
            {
                //TODO: Show more than 2 voicenames
                _Texts[_TextDuetName1].Text = song.Notes.VoiceNames[0];
                _Texts[_TextDuetName2].Text = song.Notes.VoiceNames[1];
                //More than one song: Player is not assigned to line by user
                //Otherwise, this is done by CScreenNames
                if (CGame.GetNumSongs() > 1)
                {
                    for (int i = 0; i < CGame.NumPlayers; i++)
                        CGame.Players[i].VoiceNr = (i + 1) % 2;
                }
                else
                {
                    for (int i = 0; i < CGame.NumPlayers; i++) 
                        CGame.Players [i].VoiceNr = voiceAssignments [i];
                }
            }

            //Attention: This needs to be done after player-assignment!
            _SingNotes[_SingBars].Init(CGame.NumPlayers);

            _DynamicLyricsTop = false;
            _DynamicLyricsBottom = false;

            foreach (CNoteBars notes in _SingNotes[_SingBars].PlayerNotes)
            {
                if (notes.Rect.Bottom >= CSettings.RenderH / 2)
                    _DynamicLyricsBottom = true;
                else
                    _DynamicLyricsTop = true;
            }

            _SetDuetLyricsVisibility(song.IsDuet);
            _SetNormalLyricsVisibility();

            _TimerSongText.Reset();
            _TimerDuetText1.Reset();
            _TimerDuetText2.Reset();

            if (!song.IsDuet)
                _TimerSongText.Start();

            _StartSong();
        }

        private void _SetDuetLyricsVisibility(bool isDuet)
        {
            _Statics[_StaticLyricsDuet].Visible = isDuet;
            _Lyrics[_LyricMainDuet].Visible = isDuet;
            _Lyrics[_LyricSubDuet].Visible = isDuet;

            if (isDuet)
            {
                _Lyrics[_LyricMainTop].Visible = false;
                _Lyrics[_LyricSubTop].Visible = false;
                _Statics[_StaticLyricsTop].Visible = false;
            }
            else
            {
                bool lyricsOnTop = CConfig.Config.Game.LyricsPosition == ELyricsPosition.TR_CONFIG_LYRICSPOSITION_TOP
                                   || CConfig.Config.Game.LyricsPosition == ELyricsPosition.TR_CONFIG_LYRICSPOSITION_BOTH
                                   || (CConfig.Config.Game.LyricsPosition == ELyricsPosition.TR_CONFIG_LYRICSPOSITION_DYNAMIC && _DynamicLyricsTop);
                _Lyrics[_LyricMainTop].Visible = lyricsOnTop;
                _Lyrics[_LyricSubTop].Visible = lyricsOnTop;
                _Statics[_StaticLyricsTop].Visible = lyricsOnTop;
            }
        }

        private void _SetNormalLyricsVisibility()
        {
            bool visible = CConfig.Config.Game.LyricsPosition == ELyricsPosition.TR_CONFIG_LYRICSPOSITION_BOTTOM
                           || CConfig.Config.Game.LyricsPosition == ELyricsPosition.TR_CONFIG_LYRICSPOSITION_BOTH
                           || (CConfig.Config.Game.LyricsPosition == ELyricsPosition.TR_CONFIG_LYRICSPOSITION_DYNAMIC && _DynamicLyricsBottom);
            _Lyrics[_LyricMain].Visible = visible;
            _Lyrics[_LyricSub].Visible = visible;
            _Statics[_StaticLyrics].Visible = visible;
        }

        private void _StartSong()
        {
            _PrepareTimeLine();
            CSound.Play(_CurrentStream);
            CRecord.Start();
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
                CWebcam.Stop();
        }

        private int _FindCurrentLine(CVoice voice, CSongLine[] lines, CSong song)
        {
            float currentTime = _CurrentTime - song.Gap;
            //We are only interested in the last matching line, so either do not check further after line[j].StartBeat > _CurrentBeat or go backwards!
            int j = voice.FindPreviousLine(_CurrentBeat);
            for (; j >= 0; j--)
            {
                float firstNoteTime = CGame.GetTimeFromBeats(lines[j].FirstNoteBeat, song.BPM);
                //Earlist possible line break is 10s before first note
                if (firstNoteTime <= currentTime + 10f)
                {
                    //First line has no predecessor or line has to be shown
                    if (j == 0 || firstNoteTime - CConfig.Config.Game.MinLineBreakTime <= currentTime)
                        return j;
                    float lastNoteTime = CGame.GetTimeFromBeats(lines[j - 1].LastNoteBeat, song.BPM);
                    //No line break if last line is not fully evaluated (with 50% tolerance -> tested so notes are drawn)
                    if (lastNoteTime + CConfig.Config.Record.MicDelay / 1000f * 1.5f >= currentTime)
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
            _Lyrics[_LyricSub].Update(-100);
            _Lyrics[_LyricMain].Update(CGame.CurrentBeatF);

            _Lyrics[_LyricSubDuet].Update(-100);
            _Lyrics[_LyricMainDuet].Update(CGame.CurrentBeatF);

            _Lyrics[_LyricSubTop].Update(-100);
            _Lyrics[_LyricMainTop].Update(CGame.CurrentBeatF);

            for (int i = 0; i < song.Notes.VoiceCount; i++)
            {
                if (i > 1)
                    break; // for later

                CVoice voice = song.Notes.GetVoice(i);
                CSongLine[] lines = voice.Lines;

                // find current line
                int nr = _FindCurrentLine(voice, lines, song);

                if (nr != -1)
                {
                    for (int j = 0; j < CGame.NumPlayers; j++)
                    {
                        if (CGame.Players[j].VoiceNr == i)
                            _SingNotes[_SingBars].PlayerNotes[j].SetLine(nr);
                    }

                    if (i == 0 && !song.IsDuet || i == 1 && song.IsDuet)
                    {
                        _Lyrics[_LyricMain].SetLine(lines[nr]);
                        _Lyrics[_LyricMainTop].SetLine(lines[nr]);
                        _TimeToFirstNote = CGame.GetTimeFromBeats(lines[nr].FirstNoteBeat - lines[nr].StartBeat, song.BPM);
                        _RemainingTimeToFirstNote = CGame.GetTimeFromBeats(lines[nr].FirstNoteBeat - CGame.GetBeatFromTime(_CurrentTime, song.BPM, song.Gap), song.BPM);

                        if (lines.Length >= nr + 2)
                        {
                            _Lyrics[_LyricSub].SetLine(lines[nr + 1]);
                            _Lyrics[_LyricSubTop].SetLine(lines[nr + 1]);
                        }
                        else
                        {
                            _Lyrics[_LyricSub].Clear();
                            _Lyrics[_LyricSubTop].Clear();
                        }
                    }
                    if (i == 0 && song.IsDuet)
                    {
                        _Lyrics[_LyricMainDuet].SetLine(lines[nr]);
                        _TimeToFirstNoteDuet = CGame.GetTimeFromBeats(lines[nr].FirstNoteBeat - lines[nr].StartBeat, song.BPM);
                        _RemainingTimeToFirstNoteDuet = CGame.GetTimeFromBeats(lines[nr].FirstNoteBeat - CGame.GetBeatFromTime(_CurrentTime, song.BPM, song.Gap), song.BPM);

                        if (lines.Length >= nr + 2)
                            _Lyrics[_LyricSubDuet].SetLine(lines[nr + 1]);
                        else
                            _Lyrics[_LyricSubDuet].Clear();
                    }
                }
                else
                {
                    if (i == 0 && !song.IsDuet || i == 1 && song.IsDuet)
                    {
                        _Lyrics[_LyricMain].Clear();
                        _Lyrics[_LyricSub].Clear();
                        _Lyrics[_LyricMainTop].Clear();
                        _Lyrics[_LyricSubTop].Clear();
                        _TimeToFirstNote = 0f;
                    }

                    if (i == 0 && song.IsDuet)
                    {
                        _Lyrics[_LyricMainDuet].Clear();
                        _Lyrics[_LyricSubDuet].Clear();
                        _TimeToFirstNoteDuet = 0f;
                    }
                }
            }
        }

        private void _TogglePause()
        {
            _SetPause(!_Pause);
        }

        private void _SetPause(bool paused)
        {
            _Pause = paused;
            _Statics[_StaticPauseBG].Visible = _Pause;
            _Texts[_TextPause].Visible = _Pause;
            _Buttons[_ButtonCancel].Visible = _Pause;
            _Buttons[_ButtonContinue].Visible = _Pause;
            _Buttons[_ButtonSkip].Visible = _Pause && CGame.NumRounds > CGame.RoundNr && CGame.NumRounds > 1;
            if (_Pause)
                CSound.Pause(_CurrentStream);
            else
                CSound.Play(_CurrentStream);
        }

        private void _BuildTextStrings(List<string> texts)
        {
            _TextScores = new string[CSettings.MaxNumPlayer,CSettings.MaxNumPlayer];
            _TextNames = new string[CSettings.MaxNumPlayer,CSettings.MaxNumPlayer];

            for (int numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (int player = 0; player < CSettings.MaxNumPlayer; player++)
                {
                    if (player <= numplayer)
                    {
                        string target = "P" + (player + 1) + "N" + (numplayer + 1);
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
                        string target = "P" + (player + 1) + "N" + (numplayer + 1);
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
            _Statics[_StaticLyricsDuet].Visible = false;
            _Statics[_StaticLyricHelper].Visible = false;
            _Statics[_StaticLyricHelperDuet].Visible = false;
            _Statics[_StaticLyricHelperTop].Visible = false;
            _Lyrics[_LyricMainDuet].Visible = false;
            _Lyrics[_LyricSubDuet].Visible = false;

            _Statics[_StaticSongText].Visible = false;
            _Texts[_TextSongName].Visible = false;
            _Texts[_TextDuetName1].Visible = false;
            _Texts[_TextDuetName2].Visible = false;

            List<string> staticsExtra = new List<string> ();
            List<string> textsExtra = new List<string> ();

            //Everything Static or Text with Extra should be only visible
            //If Player-Number matches CGame.NumPlayers
            foreach(CStatic st in _Statics)
                if(st.GetThemeName().StartsWith("StaticExtra"))
                    staticsExtra.Add(st.GetThemeName());

            foreach(CText tx in _Texts)
                if(tx.GetThemeName().StartsWith("TextExtra"))
                    textsExtra.Add(tx.GetThemeName());

            string curN = "N"+(CGame.NumPlayers);
            foreach (string st in staticsExtra) 
            {
                string n = st.Substring(st.Length - 2);
                _Statics[st].Visible = n == curN;        
            }

            foreach (string tx in textsExtra) 
            {
                string n = tx.Substring(tx.Length - 2);
                _Texts[tx].Visible = n == curN;        
            }

            for (int numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (int player = 0; player < CSettings.MaxNumPlayer && player <= numplayer; player++)
                {
                    bool isIvisible = numplayer + 1 == CGame.NumPlayers && player <= CGame.NumPlayers;
                    _Texts[_TextScores[player, numplayer]].Visible = isIvisible;
                    _Texts[_TextNames[player, numplayer]].Visible = isIvisible &&
                                                                    (CConfig.Config.Theme.PlayerInfo == EPlayerInfo.TR_CONFIG_PLAYERINFO_BOTH ||
                                                                     CConfig.Config.Theme.PlayerInfo == EPlayerInfo.TR_CONFIG_PLAYERINFO_NAME);
                    _Statics[_StaticScores[player, numplayer]].Visible = isIvisible;
                    _Statics[_StaticAvatars[player, numplayer]].Visible = isIvisible &&
                                                                          (CConfig.Config.Theme.PlayerInfo == EPlayerInfo.TR_CONFIG_PLAYERINFO_BOTH ||
                                                                           CConfig.Config.Theme.PlayerInfo == EPlayerInfo.TR_CONFIG_PLAYERINFO_AVATAR);
                }
            }

            _Lyrics[_LyricMain].Alpha = 0f;
            _Lyrics[_LyricSub].Alpha = 0f;

            _Lyrics[_LyricMainTop].Alpha = 0f;
            _Lyrics[_LyricSubTop].Alpha = 0f;

            _Statics[_StaticLyrics].Alpha = 0f;
            _Statics[_StaticLyricsTop].Alpha = 0f;

            _Statics[_StaticLyricHelper].Alpha = 0f;
            _Statics[_StaticLyricHelperTop].Alpha = 0f;

            _Lyrics[_LyricMainDuet].Alpha = 0f;
            _Lyrics[_LyricSubDuet].Alpha = 0f;

            _Statics[_StaticLyricsDuet].Alpha = 0f;
            _Statics[_StaticLyricHelperDuet].Alpha = 0f;

            _Statics[_StaticTimeLine].Visible = CConfig.Config.Theme.TimerLook == ETimerLook.TR_CONFIG_TIMERLOOK_NORMAL;
            _Statics[_StaticTimeLineExpandedHighlighted].Visible = false;
            _Statics[_StaticTimeLineExpandedNormal].Visible = false;
        }

        private void _UpdateMedleyCountdown()
        {
            CSong song = CGame.GetSong();
            if (song == null)
                return;
            float timeToFirstMedleyNote = CGame.GetTimeFromBeats(song.Medley.StartBeat, song.BPM) + song.Gap;
            if (_CurrentTime < timeToFirstMedleyNote)
            {
                float timeDiff = timeToFirstMedleyNote - _CurrentTime + 1;
                var fullSeconds = (float)Math.Truncate(timeDiff);
                float partSeconds = timeDiff - fullSeconds;
                _Texts[_TextMedleyCountdown].Visible = true;
                _Texts[_TextMedleyCountdown].Text = fullSeconds.ToString();
                _Texts[_TextMedleyCountdown].Font.Height = partSeconds * CSettings.RenderH;

                RectangleF textBounds = CFonts.GetTextBounds(_Texts[_TextMedleyCountdown]);
                float x = CSettings.RenderW / 2 - textBounds.Width / 2;
                float y = CSettings.RenderH / 2 - textBounds.Height / 2;
                _Texts[_TextMedleyCountdown].X = x;
                _Texts[_TextMedleyCountdown].Y = y;
            }
            else
                _Texts[_TextMedleyCountdown].Visible = false;
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

                if (_Statics[_StaticLyrics].Visible)
                {
                    SRectF rect = _Statics[_StaticLyricHelper].Rect;
                    var color = new SColorF(
                        _Statics[_StaticLyricHelper].Color.R,
                        _Statics[_StaticLyricHelper].Color.G,
                        _Statics[_StaticLyricHelper].Color.B,
                        _Statics[_StaticLyricHelper].Color.A * _Statics[_StaticLyricHelper].Alpha * alpha);

                    float distance = _Lyrics[_LyricMain].GetCurrentLyricPosX() - rect.X - rect.W;
                    CDraw.DrawTexture(_Statics[_StaticLyricHelper].Texture,
                                      new SRectF(rect.X + distance * (1f - time / totaltime), rect.Y, rect.W, rect.H, rect.Z), color);
                }

                if (_Statics[_StaticLyricsTop].Visible)
                {
                    SRectF rect = _Statics[_StaticLyricHelperTop].Rect;
                    var color = new SColorF(
                        _Statics[_StaticLyricHelperTop].Color.R,
                        _Statics[_StaticLyricHelperTop].Color.G,
                        _Statics[_StaticLyricHelperTop].Color.B,
                        _Statics[_StaticLyricHelperTop].Color.A * _Statics[_StaticLyricHelper].Alpha * alpha);

                    float distance = _Lyrics[_LyricMainTop].GetCurrentLyricPosX() - rect.X - rect.W;
                    CDraw.DrawTexture(_Statics[_StaticLyricHelperTop].Texture,
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

                    SRectF rect = _Statics[_StaticLyricHelperDuet].Rect;

                    var color = new SColorF(
                        _Statics[_StaticLyricHelperDuet].Color.R,
                        _Statics[_StaticLyricHelperDuet].Color.G,
                        _Statics[_StaticLyricHelperDuet].Color.B,
                        _Statics[_StaticLyricHelperDuet].Color.A * _Statics[_StaticLyricHelperDuet].Alpha * alpha);

                    float distance = _Lyrics[_LyricMainDuet].GetCurrentLyricPosX() - rect.X - rect.W;
                    CDraw.DrawTexture(_Statics[_StaticLyricHelperDuet].Texture,
                                      new SRectF(rect.X + distance * (1f - time / totaltime), rect.Y, rect.W, rect.H, rect.Z),
                                      color);
                }
            }
        }

        private float[] _CalcFadingAlpha()
        {
            const float dt = 4f;
            const float rt = dt * 0.8f;

            CSong song = CGame.GetSong();
            if (song == null || !song.NotesLoaded)
                return null;

            var alpha = new float[song.Notes.VoiceCount * 2];
            float currentTime = _CurrentTime - song.Gap;

            for (int i = 0; i < song.Notes.VoiceCount; i++)
            {
                CVoice voice = song.Notes.GetVoice(i);
                CSongLine[] lines = voice.Lines;

                // find current line for lyric sub fading
                int currentLineSub = _FindCurrentLine(voice, lines, song);

                // find current line for lyric main fading
                int currentLine = 0;
                for (int j = 0; j < lines.Length; j++)
                {
                    if (lines[j].FirstNoteBeat <= _CurrentBeat)
                        currentLine = j;
                }

                // default values
                alpha[i * 2] = 1f;
                alpha[i * 2 + 1] = 1f;

                // main line alpha
                if (currentLine == 0 && currentTime < CGame.GetTimeFromBeats(lines[currentLine].FirstNoteBeat, song.BPM))
                {
                    // first main line and fist note is not reached
                    // => fade in
                    float diff = CGame.GetTimeFromBeats(lines[currentLine].FirstNoteBeat, song.BPM) - currentTime;
                    if (diff > dt)
                        alpha[i * 2] = 1f - (diff - dt) / rt;
                }
                else if (currentLine < lines.Length - 1 && CGame.GetTimeFromBeats(lines[currentLine].LastNoteBeat, song.BPM) < currentTime &&
                         CGame.GetTimeFromBeats(lines[currentLine + 1].FirstNoteBeat, song.BPM) > currentTime)
                {
                    // current position is between two lines

                    // time between the to lines
                    float diff = CGame.GetTimeFromBeats(lines[currentLine + 1].FirstNoteBeat, song.BPM) -
                                 CGame.GetTimeFromBeats(lines[currentLine].LastNoteBeat, song.BPM);

                    // fade only if there is enough time for fading
                    if (diff > 3.3f * dt)
                    {
                        // time elapsed since last line
                        float last = currentTime - CGame.GetTimeFromBeats(lines[currentLine].LastNoteBeat, song.BPM);

                        // time to next line
                        float next = CGame.GetTimeFromBeats(lines[currentLine + 1].FirstNoteBeat, song.BPM) - currentTime;

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
                else if (currentLine == lines.Length - 1 && CGame.GetTimeFromBeats(lines[currentLine].LastNoteBeat, song.BPM) < currentTime)
                {
                    // last main line and last note was reached
                    // => fade out
                    float diff = currentTime - CGame.GetTimeFromBeats(lines[currentLine].LastNoteBeat, song.BPM);
                    alpha[i * 2] = 1f - diff / rt;
                }

                // sub
                if (currentLineSub < lines.Length - 2)
                {
                    float diff = CGame.GetTimeFromBeats(lines[currentLineSub + 1].FirstNoteBeat, song.BPM) - currentTime;

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
            if (_TimerSongText.IsRunning && !_Lyrics[_LyricMainDuet].Visible && !_Lyrics[_LyricMainTop].Visible)
            {
                float t = _TimerSongText.ElapsedMilliseconds / 1000f;
                if (t < 10f)
                {
                    _Statics[_StaticSongText].Visible = true;
                    _Texts[_TextSongName].Visible = true;

                    if (t < 7f)
                    {
                        _Statics[_StaticSongText].Color.A = 1f;
                        _Texts[_TextSongName].Color.A = 1f;
                    }
                    else
                    {
                        _Statics[_StaticSongText].Color.A = (10f - t) / 3f;
                        _Texts[_TextSongName].Color.A = (10f - t) / 3f;
                    }
                }
                else
                {
                    _Statics[_StaticSongText].Visible = false;
                    _Texts[_TextSongName].Visible = false;
                    _TimerSongText.Stop();
                }
            }
            else
            {
                _Statics[_StaticSongText].Visible = false;
                _Texts[_TextSongName].Visible = false;
            }
        }

        private void _UpdateDuetNames(CText textName, Stopwatch timer)
        {
            if (timer.IsRunning)
            {
                float t = timer.ElapsedMilliseconds / 1000f;
                if (t < 10f)
                {
                    textName.Visible = true;

                    if (t < 3f)
                        textName.Color.A = t / 3f;
                    else if (t < 7f)
                        textName.Color.A = 1f;
                    else
                        textName.Color.A = (10f - t) / 3f;
                }
                else
                {
                    textName.Visible = false;
                    timer.Stop();
                }
            }
            else if (!timer.IsRunning && timer.ElapsedMilliseconds == 0 && _Lyrics[_LyricMainDuet].Alpha > 0 && CGame.GetSong().IsDuet)
                timer.Start();
            else
                textName.Visible = false;
        }

        private void _UpdateDuetNames()
        {
            if (CGame.GetSong() == null)
                return;
            //Timer for first duet-part
            _UpdateDuetNames(_Texts[_TextDuetName1], _TimerDuetText1);
            //Timer for second duet-part
            if (_TimerDuetText2.IsRunning)
            {
                float t = _TimerDuetText2.ElapsedMilliseconds / 1000f;
                if (t < 10f)
                {
                    _Texts[_TextDuetName2].Visible = true;

                    if (t < 3f)
                        _Texts[_TextDuetName2].Color.A = (3f - (3f - t)) / 3f;
                    else if (t < 7f)
                        _Texts[_TextDuetName2].Color.A = 1f;
                    else
                        _Texts[_TextDuetName2].Color.A = (3f - (t - 7f)) / 3f;
                }
                else
                {
                    _Texts[_TextDuetName2].Visible = false;
                    _TimerDuetText2.Stop();
                }
            }
            else if (!_TimerDuetText2.IsRunning && _TimerDuetText2.ElapsedMilliseconds == 0 && _Lyrics[_LyricMain].Alpha > 0 && CGame.GetSong().IsDuet)
                _TimerDuetText2.Start();
            else
                _Texts[_TextDuetName2].Visible = false;
        }

        private void _UpdateTimeLine()
        {
            CSong song = CGame.GetSong();

            if (song == null)
                return;

            float totalTime = CSound.GetLength(_CurrentStream);
            if (Math.Abs(song.Finish) > 0.001)
                totalTime = song.Finish;

            float remainingTime = totalTime - _CurrentTime;
            totalTime -= song.Start;
            float currentTime = _CurrentTime - song.Start;

            if (_Length < 0 && totalTime > 0)
                _PrepareTimeLine();

            if (totalTime <= 0f)
                return;

            switch (CConfig.Config.Game.TimerMode)
            {
                case ETimerMode.TR_CONFIG_TIMERMODE_CURRENT:
                    var min = (int)Math.Floor(currentTime / 60f);
                    var sec = (int)(currentTime - min * 60f);
                    _Texts[_TextTime].Text = min.ToString("00") + ":" + sec.ToString("00");
                    break;

                case ETimerMode.TR_CONFIG_TIMERMODE_REMAINING:
                    min = (int)Math.Floor(remainingTime / 60f);
                    sec = (int)(remainingTime - min * 60f);
                    _Texts[_TextTime].Text = "-" + min.ToString("00") + ":" + sec.ToString("00");
                    break;

                case ETimerMode.TR_CONFIG_TIMERMODE_TOTAL:
                    min = (int)Math.Floor(totalTime / 60f);
                    sec = (int)(totalTime - min * 60f);
                    _Texts[_TextTime].Text = "#" + min.ToString("00") + ":" + sec.ToString("00");
                    break;
            }


            switch (CConfig.Config.Theme.TimerLook)
            {
                case ETimerLook.TR_CONFIG_TIMERLOOK_NORMAL:
                    _TimeLineRect.W = _Statics[_StaticTimeLine].Rect.W * (currentTime / totalTime);
                    break;

                case ETimerLook.TR_CONFIG_TIMERLOOK_EXPANDED:
                    CStatic stat = _Statics[_StaticTimeLine];
                    int currentBeat = CGame.CurrentBeat;
                    for (int i = 0; i < _TimeRects.Count; i++)
                    {
                        if (currentBeat >= _TimeRects[i].StartBeat && currentBeat <= _TimeRects[i].EndBeat)
                        {
                            _TimeRects[i].Rect.Texture = _Statics[_StaticTimeLineExpandedHighlighted].Texture;
                            _TimeRects[i].Rect.Color = _Statics[_StaticTimeLineExpandedHighlighted].Color;
                        }
                        else
                        {
                            _TimeRects[i].Rect.Texture = _Statics[_StaticTimeLineExpandedNormal].Texture;
                            _TimeRects[i].Rect.Color = _Statics[_StaticTimeLineExpandedNormal].Color;
                        }
                    }
                    _Statics[_StaticTimePointer].X = stat.X + stat.W * (currentTime / totalTime);
                    break;
            }
        }

        private void _PrepareTimeLine()
        {
            CStatic stat = _Statics[_StaticTimeLine];
            switch (CConfig.Config.Theme.TimerLook)
            {
                case ETimerLook.TR_CONFIG_TIMERLOOK_NORMAL:
                    _TimeLineRect = stat.Rect;
                    _Statics[_StaticTimePointer].Visible = false;
                    break;

                case ETimerLook.TR_CONFIG_TIMERLOOK_EXPANDED:
                    _TimeRects.Clear();
                    _Statics[_StaticTimePointer].Visible = true;

                    CSong song = CGame.GetSong();

                    if (song == null)
                        return;

                    float totalTime = CSound.GetLength(_CurrentStream);
                    if (Math.Abs(song.Finish) > 0.001)
                        totalTime = song.Finish;

                    totalTime -= song.Start;

                    if (totalTime <= 0f)
                        return;

                    _Length = totalTime;

                    foreach (CVoice voice in song.Notes.Voices)
                    {
                        foreach (CSongLine line in voice.Lines.Where(line => line.VisibleInTimeLine))
                        {
                            var trect = new STimeRect {StartBeat = line.FirstNoteBeat, EndBeat = line.EndBeat};
                            trect.Rect = GetNewStatic(null,
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
                    break;
            }
        }

        private void _UpdateAvatars()
        {
            for (int i = 0; i < CGame.NumPlayers; i++)
            {
                if (CProfiles.IsProfileIDValid(CGame.Players[i].ProfileID))
                    _Statics[_StaticAvatars[i, CGame.NumPlayers - 1]].Texture = CProfiles.GetAvatarTextureFromProfile(CGame.Players[i].ProfileID);
                else
                    _Statics[_StaticAvatars[i, CGame.NumPlayers - 1]].Visible = false;
            }
        }

        private void _UpdateNames()
        {
            for (int i = 0; i < CGame.NumPlayers; i++)
            {
                if (CProfiles.IsProfileIDValid(CGame.Players[i].ProfileID))
                    _Texts[_TextNames[i, CGame.NumPlayers - 1]].Text = CProfiles.GetPlayerName(CGame.Players[i].ProfileID);
                else
                    _Texts[_TextNames[i, CGame.NumPlayers - 1]].Visible = false;
            }
        }
    }
}