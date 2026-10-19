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
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;
using VocaluxeLib.Utils.Player;

namespace Vocaluxe.Screens
{
    public class CScreenJukeboxPlayer : CMenu
    {
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private class CTrack
        {
            public CJukeboxEntry Entry;
            public CSongPlayer Player;
            public EAudioMode AudioMode;
            public float Start;
            public float End;
            public CTextureRef Background;
            public bool FadeInBackground = true;
            public readonly Stopwatch FadeTimer = new Stopwatch();
            public readonly Stopwatch ShowTimer = new Stopwatch();
        }

        private class CQueueRow
        {
            public CStatic Highlight;
            public CStatic Playing;
            public CStatic PlayingIcon;
            public CStatic Cover;
            public CText Title;
            public CText Artist;
        }

        private const string _StaticInfoBG = "StaticInfoBG";
        private const string _StaticInfoCover = "StaticInfoCover";
        private const string _StaticLyrics = "StaticLyrics";
        private const string _StaticBarBG = "StaticBarBG";
        private const string _StaticTimeBarBG = "StaticTimeBarBG";
        private const string _StaticTimeBarFill = "StaticTimeBarFill";
        private const string _StaticQueueBG = "StaticQueueBG";
        private const string _StaticQueueSearch = "StaticQueueSearch";
        private const string _StaticQueueRow = "StaticQueueRow";
        private const string _StaticQueueRowPlaying = "StaticQueueRowPlaying";
        private const string _StaticQueuePlaying = "StaticQueuePlaying";
        private const string _StaticQueueCover = "StaticQueueCover";
        private const string _StaticAudioModeNormal = "StaticAudioModeNormal";
        private const string _StaticAudioModeInstrumental = "StaticAudioModeInstrumental";
        private const string _StaticAudioModeIcon = "StaticAudioModeIcon";

        private const string _TextInfoTitle = "TextInfoTitle";
        private const string _TextInfoArtist = "TextInfoArtist";
        private const string _TextInfoPosition = "TextInfoPosition";
        private const string _TextTimeElapsed = "TextTimeElapsed";
        private const string _TextTimeTotal = "TextTimeTotal";
        private const string _TextQueueTitle = "TextQueueTitle";
        private const string _TextQueueCount = "TextQueueCount";
        private const string _TextQueueSearch = "TextQueueSearch";
        private const string _TextQueueHelp = "TextQueueHelp";
        private const string _TextQueueHelp2 = "TextQueueHelp2";
        private const string _TextQueueEntryTitle = "TextQueueEntryTitle";
        private const string _TextQueueEntryArtist = "TextQueueEntryArtist";

        private const string _LyricMain = "LyricMain";
        private const string _LyricSub = "LyricSub";
        private const string _LyricMainBar = "LyricMainBar";

        private const string _ButtonPrevious = "ButtonPrevious";
        private const string _ButtonPlay = "ButtonPlay";
        private const string _ButtonPause = "ButtonPause";
        private const string _ButtonNext = "ButtonNext";
        private const string _ButtonSkipIntro = "ButtonSkipIntro";
        private const string _ButtonShuffle = "ButtonShuffle";
        private const string _ButtonRepeat = "ButtonRepeat";
        private const string _ButtonLyrics = "ButtonLyrics";
        private const string _ButtonAudioMode = "ButtonAudioMode";
        private const string _ButtonQueue = "ButtonQueue";
        private const string _ButtonSing = "ButtonSing";

        private const int _MaxQueueRows = 12;
        private const float _QueueRowSpacing = 4f;
        private const float _QueueHelpSpacing = 10f;
        private const float _HideDelay = 5f;
        private const float _InfoDuration = 6f;
        private const float _UiFadeSpeed = 4f;
        private const float _PreloadTime = 10f;
        private const float _SkipIntroLeadTime = 3f;
        private const float _RestartThreshold = 3f;
        private const float _DimmedAlpha = 0.35f;
        private const float _BackgroundFadeTime = 3f;

        private readonly Stopwatch _InputTimer = new Stopwatch();
        private readonly Stopwatch _InfoTimer = new Stopwatch();
        private readonly Stopwatch _FrameTimer = new Stopwatch();
        private readonly List<CTrack> _FadingTracks = new List<CTrack>();
        private readonly List<CQueueRow> _QueueRows = new List<CQueueRow>();
        private readonly List<int> _Filtered = new List<int>();
        private readonly List<IMenuElement> _InfoElements = new List<IMenuElement>();
        private readonly List<IMenuElement> _BarElements = new List<IMenuElement>();
        private readonly List<IMenuElement> _QueueElements = new List<IMenuElement>();
        private readonly List<IMenuElement> _LyricElements = new List<IMenuElement>();
        private readonly Dictionary<string, SColorF> _ButtonColors = new Dictionary<string, SColorF>();

        private CTrack _Current;
        private CTrack _Preloaded;
        private bool _PreloadTried;
        private bool _Paused;
        private bool _Leaving;
        private bool _QueueOpen;
        private int _QueueSelected;
        private int _QueueScroll;
        private string _Search = string.Empty;
        private int _CurrentLine = -1;
        private float _UiAlpha = 1f;
        private float _InfoAlpha = 1f;
        private float _LyricBarOffset;
        private float _TimeBarWidth;
        private int _VisibleRows = 1;
        private float _QueueRowTop;
        private float _QueueRowStep;
        private float _QueuePanelMargin;
        private float _QueueHelpOffset;
        private float _QueueHelp2Offset;

        public override EMusicType CurrentMusicType
        {
            get { return EMusicType.Game; }
        }

        public override void Init()
        {
            base.Init();

            _ThemeStatics = new string[]
            {
                _StaticInfoBG, _StaticInfoCover, _StaticLyrics, _StaticBarBG, _StaticTimeBarBG, _StaticTimeBarFill, _StaticQueueBG, _StaticQueueSearch, _StaticQueueRow,
                _StaticQueueRowPlaying, _StaticQueuePlaying, _StaticQueueCover, _StaticAudioModeNormal, _StaticAudioModeInstrumental,
                _StaticAudioModeIcon
            };
            _ThemeTexts = new string[]
            {
                _TextInfoTitle, _TextInfoArtist, _TextInfoPosition, _TextTimeElapsed, _TextTimeTotal, _TextQueueTitle, _TextQueueCount, _TextQueueSearch,
                _TextQueueHelp, _TextQueueHelp2, _TextQueueEntryTitle, _TextQueueEntryArtist
            };
            _ThemeLyrics = new string[] { _LyricMain, _LyricSub, _LyricMainBar };
            _ThemeButtons = new string[]
            {
                _ButtonPrevious, _ButtonPlay, _ButtonPause, _ButtonNext, _ButtonSkipIntro, _ButtonShuffle, _ButtonRepeat, _ButtonLyrics, _ButtonAudioMode, _ButtonQueue,
                _ButtonSing
            };
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            _LyricBarOffset = _Lyrics[_LyricMainBar].Y - _Lyrics[_LyricMain].Y;
            _Lyrics[_LyricMainBar].Visible = false;
            _TimeBarWidth = _Statics[_StaticTimeBarBG].W;

            _Statics[_StaticAudioModeNormal].Visible = false;
            _Statics[_StaticAudioModeInstrumental].Visible = false;

            _ButtonColors.Clear();
            foreach (var name in new[] { _ButtonSkipIntro, _ButtonShuffle, _ButtonRepeat, _ButtonLyrics, _ButtonAudioMode })
            {
                _ButtonColors[name] = _Buttons[name].Color;
            }

            _CreateQueueRows();

            var panel = _Statics[_StaticQueueBG].Rect;
            var panelBottom = panel.Y + panel.H;
            _QueuePanelMargin = _Statics[_StaticLyrics].Y + _LyricBarOffset - panelBottom;
            _QueueHelpOffset = panelBottom - _Texts[_TextQueueHelp].Y;
            _QueueHelp2Offset = panelBottom - _Texts[_TextQueueHelp2].Y;

            _LyricElements.Clear();
            _LyricElements.AddRange(new IMenuElement[] { _Statics[_StaticLyrics], _Lyrics[_LyricMain], _Lyrics[_LyricSub] });

            _InfoElements.Clear();
            _InfoElements.AddRange(new IMenuElement[]
                { _Statics[_StaticInfoBG], _Statics[_StaticInfoCover], _Texts[_TextInfoTitle], _Texts[_TextInfoArtist], _Texts[_TextInfoPosition] });

            _BarElements.Clear();
            _BarElements.AddRange(new IMenuElement[]
            {
                _Statics[_StaticBarBG], _Statics[_StaticTimeBarBG], _Statics[_StaticTimeBarFill], _Texts[_TextTimeElapsed], _Texts[_TextTimeTotal], _Statics[_StaticAudioModeIcon]
            });
            foreach (var button in _ThemeButtons)
            {
                _BarElements.Add(_Buttons[button]);
            }

            _QueueElements.Clear();
            _QueueElements.AddRange(new IMenuElement[]
            {
                _Statics[_StaticQueueBG], _Statics[_StaticQueueSearch], _Texts[_TextQueueTitle], _Texts[_TextQueueCount], _Texts[_TextQueueSearch], _Texts[_TextQueueHelp],
                _Texts[_TextQueueHelp2]
            });
            foreach (var row in _QueueRows)
            {
                _QueueElements.AddRange(new IMenuElement[] { row.Highlight, row.Playing, row.PlayingIcon, row.Cover, row.Title, row.Artist });
            }

            _Texts[_TextQueueTitle].Text = "TR_SCREENJUKEBOXPLAYER_QUEUE";
            _Texts[_TextQueueHelp].Text = "TR_SCREENJUKEBOXPLAYER_QUEUEHELP";
            _Texts[_TextQueueHelp2].Text = "TR_SCREENJUKEBOXPLAYER_QUEUEHELP2";
            _UpdateQueueLayout();
        }

        public override void OnShow()
        {
            base.OnShow();

            _Leaving = false;
            _Paused = false;
            _QueueOpen = false;
            _Search = string.Empty;
            _UiAlpha = 1f;
            _InfoAlpha = 1f;
            _CurrentLine = -1;
            _InputTimer.Restart();
            _InfoTimer.Restart();
            _FrameTimer.Restart();

            _Lyrics[_LyricMain].LyricStyle = CConfig.Config.Theme.LyricStyle;
            _Lyrics[_LyricSub].LyricStyle = CConfig.Config.Theme.LyricStyle;
            _Lyrics[_LyricMain].Clear();
            _Lyrics[_LyricSub].Clear();

            if (CJukebox.TakeResume())
            {
                CBackgroundMusic.StopPreview();
                if (!CJukebox.Next())
                {
                    CJukebox.Play(0);
                }
            }

            _UpdateFilter();
            _UpdateControls();
            _SelectElement(_Buttons[_ButtonPause]);
        }

        public override void OnClose()
        {
            base.OnClose();
            _DisposeAll();
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (_Leaving)
            {
                return true;
            }

            var hidden = _UiAlpha < 0.5f && _IsUiIdle();
            _InputTimer.Restart();
            if (hidden)
            {
                return true;
            }

            if (_QueueOpen)
            {
                _HandleQueueInput(keyEvent);
                return true;
            }

            base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed)
            {
                return true;
            }

            switch (keyEvent.Key)
            {
                case Keys.Escape:
                case Keys.Back:
                    _Leave();
                    break;

                case Keys.Space:
                    _TogglePause();
                    break;

                case Keys.Enter:
                    _HandleButton();
                    break;
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            if (_Leaving)
            {
                return true;
            }

            var hidden = _UiAlpha < 0.5f && _IsUiIdle();
            _InputTimer.Restart();
            if (hidden)
            {
                return true;
            }

            if (_QueueOpen && _HandleQueueMouse(mouseEvent))
            {
                return true;
            }

            base.HandleMouse(mouseEvent);

            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent))
            {
                _HandleButton();
            }
            else if (mouseEvent.RB)
            {
                if (_QueueOpen)
                {
                    _ToggleQueue();
                }
                else
                {
                    _Leave();
                }
            }

            return true;
        }

        public override bool UpdateGame()
        {
            var delta = (float)_FrameTimer.Elapsed.TotalSeconds;
            _FrameTimer.Restart();

            if (_Leaving)
            {
                return true;
            }

            if (_Current == null && !_StartTrack(CJukebox.CurrentEntry))
            {
                _PlayNextEntry(true);
                return true;
            }

            _UpdatePlayback();
            _UpdateFadingTracks();
            _UpdateLyrics();
            _UpdateQueueLayout();
            _UpdateUi(delta);
            _UpdateControls();
            return true;
        }

        public override void Draw()
        {
            var hasBackground = _DrawBackground();
            foreach (var background in _Backgrounds)
            {
                background.Visible = !hasBackground;
            }

            var groups = new[] { _LyricElements, _InfoElements, _BarElements, _QueueElements };
            var alphas = new[] { 1f, _InfoAlpha, _UiAlpha, _QueueOpen ? _UiAlpha : 0f };
            var offsets = new[] { _LyricBarOffset * _UiAlpha, 0f, 0f, 0f };
            var visible = new List<IMenuElement>[groups.Length];
            for (var i = 0; i < groups.Length; i++)
            {
                visible[i] = groups[i].Where(e => e.Visible).ToList();
                foreach (var element in visible[i])
                {
                    element.Visible = false;
                }
            }

            base.Draw();

            for (var i = 0; i < groups.Length; i++)
            {
                if (alphas[i] > 0f)
                {
                    CDraw.PushTransform(1f, 0f, 0f, offsets[i], alphas[i]);
                    foreach (var element in visible[i].OrderByDescending(e => e.MaxRect.Z))
                    {
                        element.Visible = true;
                        element.Draw();
                    }

                    CDraw.PopTransform();
                }

                foreach (var element in visible[i])
                {
                    element.Visible = true;
                }
            }
        }

        private bool _DrawBackground()
        {
            var drawn = false;
            foreach (var track in _FadingTracks.Concat(new[] { _Current }).Where(t => t != null))
            {
                CTextureRef texture = null;
                if (CJukebox.ShowVideo && track.Player.SongHasVideo)
                {
                    texture = track.Player.GetVideoTexture();
                }

                var alpha = 1f;
                if (texture == null && track.Background != null)
                {
                    texture = track.Background;
                    if (track.FadeInBackground)
                    {
                        alpha = Math.Min(1f, (float)track.ShowTimer.Elapsed.TotalSeconds / _BackgroundFadeTime);
                    }
                }

                if (texture == null)
                {
                    continue;
                }

                var color = texture.Color;
                color.A *= alpha;
                var bounds = CSettings.RenderRect;
                var aspect = texture.OrigAspect <= 1.5 || texture.OrigAspect >= 1.9 ? EAspect.LetterBox : EAspect.Crop;
                CDraw.DrawTexture(texture, CHelper.FitInBounds(bounds, texture.OrigAspect, aspect), color, bounds);
                drawn = true;
            }

            return drawn;
        }

        private void _CreateQueueRows()
        {
            foreach (var row in _QueueRows)
            {
                _RemoveQueueRow(row);
            }

            _QueueRows.Clear();
            var template = _Statics[_StaticQueueRow].Rect;
            var step = template.H + _QueueRowSpacing;
            _QueueRowTop = template.Y;
            _QueueRowStep = step;
            for (var i = 0; i < _MaxQueueRows; i++)
            {
                var row = new CQueueRow
                {
                    Highlight = GetNewStatic(_Statics[_StaticQueueRow]),
                    Playing = GetNewStatic(_Statics[_StaticQueueRowPlaying]),
                    PlayingIcon = GetNewStatic(_Statics[_StaticQueuePlaying]),
                    Cover = GetNewStatic(_Statics[_StaticQueueCover]),
                    Title = GetNewText(_Texts[_TextQueueEntryTitle]),
                    Artist = GetNewText(_Texts[_TextQueueEntryArtist])
                };
                var offset = i * step;
                row.Highlight.Y += offset;
                row.Playing.Y += offset;
                row.PlayingIcon.Y += offset;
                row.Cover.Y += offset;
                row.Title.Y += offset;
                row.Artist.Y += offset;
                _AddStatic(row.Highlight);
                _AddStatic(row.Playing);
                _AddStatic(row.PlayingIcon);
                _AddStatic(row.Cover);
                _AddText(row.Title);
                _AddText(row.Artist);
                _QueueRows.Add(row);
            }

            _Statics[_StaticQueueRow].Visible = false;
            _Statics[_StaticQueueRowPlaying].Visible = false;
            _Statics[_StaticQueuePlaying].Visible = false;
            _Statics[_StaticQueueCover].Visible = false;
            _Texts[_TextQueueEntryTitle].Visible = false;
            _Texts[_TextQueueEntryArtist].Visible = false;
        }

        private static void _RemoveQueueRow(CQueueRow row)
        {
            row.Highlight.Visible = false;
            row.Playing.Visible = false;
            row.PlayingIcon.Visible = false;
            row.Cover.Visible = false;
            row.Title.Visible = false;
            row.Artist.Visible = false;
        }

        private CTrack _LoadTrack(CJukeboxEntry entry)
        {
            var song = entry == null ? null : entry.Song;
            if (song == null)
            {
                return null;
            }

            var track = new CTrack { Entry = entry, Player = new CSongPlayer(), AudioMode = CJukebox.GetAudioMode(entry) };
            var file = track.AudioMode == EAudioMode.TR_AUDIOMODE_INSTRUMENTAL ? song.GetInstrumental() : song.GetMP3();
            track.Player.Load(song, file, CJukebox.ShowVideo);
            if (!track.Player.SoundLoaded)
            {
                track.Player.Close();
                return null;
            }

            CJukebox.GetSection(song, track.Player.Length, out track.Start, out track.End);
            if (track.Start > 0f)
            {
                track.Player.SetPosition(track.Start);
            }

            if ((!CJukebox.ShowVideo || !track.Player.SongHasVideo) && song.BackgroundFileNames.Count > 0)
            {
                track.Background = CDraw.AddTexture(Path.Combine(song.Folder, song.BackgroundFileNames[0]));
            }

            return track;
        }

        private bool _StartTrack(CJukeboxEntry entry)
        {
            var track = _Preloaded != null && _Preloaded.Entry == entry ? _Preloaded : _LoadTrack(entry);
            if (_Preloaded != null && _Preloaded != track)
            {
                _DisposeTrack(_Preloaded);
            }

            _Preloaded = null;
            _PreloadTried = false;

            if (_Current != null)
            {
                _FadeOut(_Current);
            }

            _Current = track;
            if (track == null)
            {
                return false;
            }

            _Paused = false;
            track.Player.Play();
            track.ShowTimer.Restart();
            _CurrentLine = -1;
            _Lyrics[_LyricMain].Clear();
            _Lyrics[_LyricSub].Clear();
            _InfoTimer.Restart();
            _UpdateSongInfo();
            _UpdateFilter();
            return true;
        }

        private void _PlayNextEntry(bool automatic)
        {
            var attempts = CJukebox.Queue.Count;
            while (attempts-- > 0)
            {
                if (!CJukebox.Next())
                {
                    if (automatic)
                    {
                        _Leave();
                    }

                    return;
                }

                if (_StartTrack(CJukebox.CurrentEntry))
                {
                    return;
                }
            }

            _Leave();
        }

        private void _PlayPrevious()
        {
            if (_Current == null)
            {
                return;
            }

            if (_Current.Player.Position - _Current.Start > _RestartThreshold || !CJukebox.Previous())
            {
                _Current.Player.SetPosition(_Current.Start);
                _CurrentLine = -1;
                return;
            }

            _StartTrack(CJukebox.CurrentEntry);
        }

        private void _UpdatePlayback()
        {
            if (_Current == null || _Paused)
            {
                return;
            }

            var player = _Current.Player;
            var position = player.Position;
            var end = _Current.End > 0f ? _Current.End : player.Length;
            var fadeTime = CBase.Settings.GetSoundPlayerFadeTime();

            if (_Preloaded == null && !_PreloadTried && end - position < _PreloadTime && CJukebox.Current + 1 < CJukebox.Queue.Count)
            {
                _PreloadTried = true;
                _Preloaded = _LoadTrack(CJukebox.NextEntry);
            }

            if ((end > 0f && position >= end - fadeTime) || player.IsFinished)
            {
                _PlayNextEntry(true);
            }
        }

        private void _UpdateFadingTracks()
        {
            var fadeTime = CBase.Settings.GetSoundPlayerFadeTime();
            foreach (var track in _FadingTracks.Where(t => t.FadeTimer.Elapsed.TotalSeconds > fadeTime + 0.5f).ToList())
            {
                _FadingTracks.Remove(track);
                _DisposeTrack(track);
            }
        }

        private void _FadeOut(CTrack track)
        {
            track.Player.Pause();
            track.FadeTimer.Restart();
            _FadingTracks.Add(track);
        }

        private static void _DisposeTrack(CTrack track)
        {
            track.Player.Close();
            if (track.Background != null)
            {
                CDraw.RemoveTexture(ref track.Background);
            }
        }

        private void _DisposeAll()
        {
            foreach (var track in _FadingTracks)
            {
                _DisposeTrack(track);
            }

            _FadingTracks.Clear();
            if (_Preloaded != null)
            {
                _DisposeTrack(_Preloaded);
                _Preloaded = null;
            }

            if (_Current != null)
            {
                _DisposeTrack(_Current);
                _Current = null;
            }
        }

        private void _ResetPreload()
        {
            if (_Preloaded != null)
            {
                _DisposeTrack(_Preloaded);
                _Preloaded = null;
            }

            _PreloadTried = false;
        }

        private void _Leave()
        {
            if (_Leaving)
            {
                return;
            }

            _Leaving = true;
            _DisposeAll();
            CGraphics.FadeTo(EScreen.Jukebox);
        }

        private void _TogglePause()
        {
            if (_Current == null)
            {
                return;
            }

            _Paused = !_Paused;
            if (_Paused)
            {
                _Current.Player.Pause();
                _SelectElement(_Buttons[_ButtonPlay]);
            }
            else
            {
                _Current.Player.Play();
                _SelectElement(_Buttons[_ButtonPause]);
            }

            _UpdateControls();
        }

        private void _SkipIntro()
        {
            if (!_CanSkipIntro())
            {
                return;
            }

            _Current.Player.SetPosition(CJukebox.GetFirstNoteTime(_Current.Entry.Song) - _SkipIntroLeadTime);
            _CurrentLine = -1;
        }

        private bool _CanSkipIntro()
        {
            if (_Current == null)
            {
                return false;
            }

            var target = CJukebox.GetFirstNoteTime(_Current.Entry.Song) - _SkipIntroLeadTime;
            return _Current.Player.Position < target - 1f && (_Current.End <= 0f || target < _Current.End - CBase.Settings.GetSoundPlayerFadeTime());
        }

        private void _ToggleAudioMode()
        {
            if (_Current == null || !_Current.Entry.Song.HasInstrumental())
            {
                return;
            }

            CJukebox.AudioMode = _Current.AudioMode == EAudioMode.TR_AUDIOMODE_INSTRUMENTAL ? EAudioMode.TR_AUDIOMODE_NORMAL : EAudioMode.TR_AUDIOMODE_INSTRUMENTAL;
            _ResetPreload();

            var position = _Current.Player.Position;
            var track = _LoadTrack(_Current.Entry);
            if (track == null)
            {
                return;
            }

            track.Player.SetPosition(position);
            track.FadeInBackground = false;
            track.ShowTimer.Restart();
            _FadeOut(_Current);
            _Current = track;
            if (!_Paused)
            {
                track.Player.Play();
            }
        }

        private void _SingNow()
        {
            var entry = CJukebox.CurrentEntry;
            var song = entry == null ? null : entry.Song;
            if (song == null)
            {
                return;
            }

            var position = _Current != null ? _Current.Player.Position : -1f;
            _Leaving = true;
            CJukebox.RequestSinging();
            _DisposeAll();

            CParty.SetNormalGameMode();
            CGame.Reset();
            CGame.ClearSongs();
            var gameMode = song.IsGameModeAvailable(EGameMode.TR_GAMEMODE_DUET) ? EGameMode.TR_GAMEMODE_DUET : EGameMode.TR_GAMEMODE_NORMAL;
            CGame.AddSong(entry.SongId, gameMode);
            CBackgroundMusic.LoadPreview(song, position);
            CGame.GotoNameSelection();
        }

        private void _HandleButton()
        {
            if (_Buttons[_ButtonPrevious].Selected)
            {
                _PlayPrevious();
            }
            else if (_Buttons[_ButtonPlay].Selected || _Buttons[_ButtonPause].Selected)
            {
                _TogglePause();
            }
            else if (_Buttons[_ButtonNext].Selected)
            {
                _PlayNextEntry(false);
            }
            else if (_Buttons[_ButtonSkipIntro].Selected)
            {
                _SkipIntro();
            }
            else if (_Buttons[_ButtonShuffle].Selected)
            {
                CJukebox.SetShuffle(!CJukebox.Shuffle);
                _ResetPreload();
                _UpdateFilter();
            }
            else if (_Buttons[_ButtonRepeat].Selected)
            {
                CJukebox.Repeat = !CJukebox.Repeat;
                _ResetPreload();
            }
            else if (_Buttons[_ButtonLyrics].Selected)
            {
                CJukebox.ShowLyrics = !CJukebox.ShowLyrics;
                _CurrentLine = -1;
            }
            else if (_Buttons[_ButtonAudioMode].Selected)
            {
                _ToggleAudioMode();
            }
            else if (_Buttons[_ButtonQueue].Selected)
            {
                _ToggleQueue();
            }
            else if (_Buttons[_ButtonSing].Selected)
            {
                _SingNow();
            }

            _UpdateControls();
        }

        private void _ToggleQueue()
        {
            _QueueOpen = !_QueueOpen;
            _Search = string.Empty;
            _UpdateFilter();
            if (_QueueOpen)
            {
                _QueueSelected = Math.Max(0, _Filtered.IndexOf(CJukebox.Current));
                _QueueScroll = _QueueSelected;
                _ClampQueueScroll();
            }
            else
            {
                _SelectElement(_Buttons[_ButtonQueue]);
            }

            _UpdateQueueRows();
        }

        private void _HandleQueueInput(SKeyEvent keyEvent)
        {
            if (keyEvent.KeyPressed)
            {
                if (!Char.IsControl(keyEvent.Unicode) && keyEvent.Mod != EModifier.Ctrl)
                {
                    _SetSearch(_Search + keyEvent.Unicode);
                }

                return;
            }

            switch (keyEvent.Key)
            {
                case Keys.Up:
                case Keys.Down:
                    var direction = keyEvent.Key == Keys.Up ? -1 : 1;
                    var entry = _GetSelectedEntry();
                    if (keyEvent.ModShift && _Search.Length == 0 && entry != null && CJukebox.Move(_Filtered[_QueueSelected], direction))
                    {
                        _ResetPreload();
                        _UpdateFilter();
                        _SelectEntry(entry);
                    }
                    else
                    {
                        _SelectQueueRow(_QueueSelected + direction);
                    }

                    break;

                case Keys.PageUp:
                    _SelectQueueRow(_QueueSelected - _VisibleRows);
                    break;

                case Keys.PageDown:
                    _SelectQueueRow(_QueueSelected + _VisibleRows);
                    break;

                case Keys.Enter:
                    _ActivateSelected();
                    break;

                case Keys.Delete:
                    if (_QueueSelected < _Filtered.Count)
                    {
                        CJukebox.Remove(_Filtered[_QueueSelected]);
                        _ResetPreload();
                        _UpdateFilter();
                        _SelectQueueRow(_QueueSelected);
                    }

                    break;

                case Keys.Back:
                    if (_Search.Length > 0)
                    {
                        _SetSearch(_Search.Remove(_Search.Length - 1));
                    }
                    else
                    {
                        _ToggleQueue();
                    }

                    break;

                case Keys.Escape:
                    if (_Search.Length > 0)
                    {
                        _SetSearch(string.Empty);
                    }
                    else
                    {
                        _ToggleQueue();
                    }

                    break;

                case Keys.Tab:
                    _ToggleQueue();
                    break;
            }
        }

        private bool _HandleQueueMouse(SMouseEvent mouseEvent)
        {
            if (mouseEvent.Wheel != 0 && CHelper.IsInBounds(_Statics[_StaticQueueBG].Rect, mouseEvent))
            {
                _QueueScroll += mouseEvent.Wheel;
                _ClampQueueScroll();
                _UpdateQueueRows();
                return true;
            }

            if (!mouseEvent.LB && !mouseEvent.LD)
            {
                return false;
            }

            for (var i = 0; i < _VisibleRows; i++)
            {
                var index = _QueueScroll + i;
                if (index >= _Filtered.Count || !CHelper.IsInBounds(_QueueRows[i].Highlight.Rect, mouseEvent))
                {
                    continue;
                }

                if (mouseEvent.LD)
                {
                    _PlayNow(CJukebox.Queue[_Filtered[index]]);
                }
                else if (index == _QueueSelected)
                {
                    _ActivateSelected();
                }
                else
                {
                    _SelectQueueRow(index);
                }

                return true;
            }

            return CHelper.IsInBounds(_Statics[_StaticQueueBG].Rect, mouseEvent);
        }

        private CJukeboxEntry _GetSelectedEntry()
        {
            return _QueueSelected < _Filtered.Count ? CJukebox.Queue[_Filtered[_QueueSelected]] : null;
        }

        private void _SelectEntry(CJukeboxEntry entry)
        {
            var queue = CJukebox.Queue;
            var row = _Filtered.FindIndex(i => queue[i] == entry);
            if (row >= 0)
            {
                _SelectQueueRow(row);
            }
        }

        private void _ActivateSelected()
        {
            var entry = _GetSelectedEntry();
            if (entry == null || entry == CJukebox.CurrentEntry)
            {
                return;
            }

            if (entry == CJukebox.NextEntry)
            {
                _PlayNow(entry);
                return;
            }

            CJukebox.PlayNext(CJukebox.Queue.IndexOf(entry));
            _ResetPreload();
            _UpdateFilter();
            _SelectEntry(entry);
        }

        private void _PlayNow(CJukeboxEntry entry)
        {
            var index = CJukebox.Queue.IndexOf(entry);
            if (index < 0 || index == CJukebox.Current)
            {
                return;
            }

            CJukebox.Play(index);
            _ResetPreload();
            _StartTrack(CJukebox.CurrentEntry);
            _SelectEntry(entry);
        }

        private void _UpdateQueueLayout()
        {
            var limit = _Statics[_StaticLyrics].Visible ? _Statics[_StaticLyrics].Y + _LyricBarOffset : _Statics[_StaticBarBG].Y;
            var panelBottom = limit - _QueuePanelMargin;
            var panel = _Statics[_StaticQueueBG];
            panel.H = panelBottom - panel.Y;
            _Texts[_TextQueueHelp].Y = panelBottom - _QueueHelpOffset;
            _Texts[_TextQueueHelp2].Y = panelBottom - _QueueHelp2Offset;

            var rowsBottom = _Texts[_TextQueueHelp].Y - _QueueHelpSpacing;
            var rows = Math.Max(1, Math.Min(_QueueRows.Count, (int)((rowsBottom - _QueueRowTop + _QueueRowSpacing) / _QueueRowStep)));
            if (rows != _VisibleRows)
            {
                _VisibleRows = rows;
                _SelectQueueRow(_QueueSelected);
            }
        }

        private void _SetSearch(string search)
        {
            _Search = search;
            _UpdateFilter();
            _QueueSelected = 0;
            _QueueScroll = 0;
            _UpdateQueueRows();
        }

        private void _SelectQueueRow(int index)
        {
            _QueueSelected = Math.Max(0, Math.Min(_Filtered.Count - 1, index));
            if (_QueueSelected < _QueueScroll)
            {
                _QueueScroll = _QueueSelected;
            }
            else if (_QueueSelected >= _QueueScroll + _VisibleRows)
            {
                _QueueScroll = _QueueSelected - _VisibleRows + 1;
            }

            _ClampQueueScroll();
            _UpdateQueueRows();
        }

        private void _ClampQueueScroll()
        {
            _QueueScroll = Math.Max(0, Math.Min(Math.Max(0, _Filtered.Count - _VisibleRows), _QueueScroll));
        }

        private void _UpdateFilter()
        {
            _Filtered.Clear();
            var queue = CJukebox.Queue;
            for (var i = 0; i < queue.Count; i++)
            {
                var song = queue[i].Song;
                if (song == null)
                {
                    continue;
                }

                if (_Search.Length == 0 || song.Title.IndexOf(_Search, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                    song.Artist.IndexOf(_Search, StringComparison.CurrentCultureIgnoreCase) >= 0)
                {
                    _Filtered.Add(i);
                }
            }

            if (_QueueSelected >= _Filtered.Count)
            {
                _QueueSelected = Math.Max(0, _Filtered.Count - 1);
            }

            _ClampQueueScroll();
            _UpdateQueueRows();
            _UpdateSongPosition();
        }

        private void _UpdateQueueRows()
        {
            var queue = CJukebox.Queue;
            for (var i = 0; i < _QueueRows.Count; i++)
            {
                var row = _QueueRows[i];
                var index = _QueueScroll + i;
                var visible = i < _VisibleRows && index < _Filtered.Count;
                var song = visible ? queue[_Filtered[index]].Song : null;
                visible = visible && song != null;

                row.Highlight.Visible = visible && index == _QueueSelected;
                row.Playing.Visible = visible && _Filtered[index] == CJukebox.Current;
                row.PlayingIcon.Visible = row.Playing.Visible;
                row.Cover.Visible = visible;
                row.Title.Visible = visible;
                row.Artist.Visible = visible;
                if (!visible)
                {
                    continue;
                }

                row.Cover.Texture = song.CoverTextureSmall;
                row.Title.Text = song.Title;
                row.Artist.Text = song.Artist;
            }

            _Texts[_TextQueueCount].Text = CLanguage.Translate("TR_SCREENJUKEBOXPLAYER_NUMSONGS").Replace("%d", _Filtered.Count.ToString());
            _Texts[_TextQueueSearch].Text = _Search.Length > 0 ? _Search : CLanguage.Translate("TR_SCREENJUKEBOXPLAYER_SEARCH");
        }

        private void _UpdateSongInfo()
        {
            var song = _Current == null ? null : _Current.Entry.Song;
            if (song == null)
            {
                return;
            }

            _Statics[_StaticInfoCover].Texture = song.CoverTextureBig;
            _Texts[_TextInfoTitle].Text = song.Title;
            _Texts[_TextInfoArtist].Text = song.Artist;
            _UpdateSongPosition();
        }

        private void _UpdateSongPosition()
        {
            _Texts[_TextInfoPosition].Text = (CJukebox.Current + 1) + " / " + CJukebox.Queue.Count;
        }

        private void _UpdateLyrics()
        {
            var main = _Lyrics[_LyricMain];
            var sub = _Lyrics[_LyricSub];

            var song = _Current == null ? null : _Current.Entry.Song;
            var visible = CJukebox.ShowLyrics && song != null && song.Notes.VoiceCount > 0;
            main.Visible = visible;
            sub.Visible = visible;
            _Statics[_StaticLyrics].Visible = visible;
            if (!visible)
            {
                return;
            }

            var position = _Current.Player.Position;
            var beat = CGame.GetBeatFromTime(position, song.Bpm, song.Gap);
            CSongLine[] lines = null;
            var line = -1;
            var key = -1;
            var bestScore = float.MaxValue;
            for (var voiceNr = 0; voiceNr < song.Notes.VoiceCount; voiceNr++)
            {
                var voice = song.Notes.GetVoice(voiceNr);
                var voiceLines = voice.Lines;
                var nr = _FindCurrentLine(voice, voiceLines, song, position - song.Gap, (int)Math.Floor(beat));
                if (nr < 0)
                {
                    continue;
                }

                var candidate = voiceLines[nr];
                float score;
                if (beat > candidate.LastNoteBeat)
                {
                    score = 100000f + beat - candidate.LastNoteBeat;
                }
                else if (beat >= candidate.FirstNoteBeat)
                {
                    score = -1f;
                }
                else
                {
                    score = candidate.FirstNoteBeat - beat;
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    lines = voiceLines;
                    line = nr;
                    key = voiceNr * 100000 + nr;
                }
            }

            if (lines == null)
            {
                if (_CurrentLine != -1)
                {
                    main.Clear();
                    sub.Clear();
                    _CurrentLine = -1;
                }

                return;
            }

            if (key != _CurrentLine)
            {
                main.SetLine(lines[line]);
                if (line + 1 < lines.Length)
                {
                    sub.SetLine(lines[line + 1]);
                }
                else
                {
                    sub.Clear();
                }

                _CurrentLine = key;
            }

            main.Update(beat);
            sub.Update(-100);
        }

        private static int _FindCurrentLine(CVoice voice, CSongLine[] lines, CSong song, float time, int beat)
        {
            for (var j = voice.FindPreviousLine(beat); j >= 0; j--)
            {
                var firstNoteTime = CGame.GetTimeFromBeats(lines[j].FirstNoteBeat, song.Bpm);
                if (firstNoteTime > time + 10f)
                {
                    continue;
                }

                if (j == 0 || firstNoteTime - CConfig.Config.Game.MinLineBreakTime <= time)
                {
                    return j;
                }

                var lastNoteTime = CGame.GetTimeFromBeats(lines[j - 1].LastNoteBeat, song.Bpm);
                return lastNoteTime >= time ? j - 1 : j;
            }

            return -1;
        }

        private bool _IsUiIdle()
        {
            return !_Paused && _InputTimer.Elapsed.TotalSeconds > _HideDelay;
        }

        private void _UpdateUi(float delta)
        {
            var uiTarget = _IsUiIdle() ? 0f : 1f;
            _UiAlpha = _Approach(_UiAlpha, uiTarget, delta * _UiFadeSpeed);

            var infoTarget = uiTarget > 0f || _InfoTimer.Elapsed.TotalSeconds < _InfoDuration ? 1f : 0f;
            _InfoAlpha = _Approach(_InfoAlpha, infoTarget, delta * _UiFadeSpeed);
        }

        private static float _Approach(float value, float target, float step)
        {
            return value < target ? Math.Min(target, value + step) : Math.Max(target, value - step);
        }

        private void _UpdateControls()
        {
            _Buttons[_ButtonPlay].Visible = _Paused;
            _Buttons[_ButtonPause].Visible = !_Paused;
            if (_Paused && _Buttons[_ButtonPause].Selected)
            {
                _SelectElement(_Buttons[_ButtonPlay]);
            }
            else if (!_Paused && _Buttons[_ButtonPlay].Selected)
            {
                _SelectElement(_Buttons[_ButtonPause]);
            }

            var song = _Current == null ? null : _Current.Entry.Song;
            var instrumental = _Current != null && _Current.AudioMode == EAudioMode.TR_AUDIOMODE_INSTRUMENTAL;
            _SetButtonState(_ButtonSkipIntro, _CanSkipIntro());
            _SetButtonState(_ButtonShuffle, CJukebox.Shuffle);
            _SetButtonState(_ButtonRepeat, CJukebox.Repeat);
            _SetButtonState(_ButtonLyrics, CJukebox.ShowLyrics);
            _SetButtonState(_ButtonAudioMode, song != null && song.HasInstrumental());

            var audioMode = _Buttons[_ButtonAudioMode];
            var audioModeIcon = _Statics[_StaticAudioModeIcon];
            audioModeIcon.Texture = _Statics[instrumental ? _StaticAudioModeInstrumental : _StaticAudioModeNormal].Texture;
            audioModeIcon.Color = audioMode.Selected ? audioMode.Text.SelColor : audioMode.Text.Color;
            audioMode.Text.Text = instrumental ? "TR_AUDIOMODE_INSTRUMENTAL" : "TR_AUDIOMODE_NORMAL";

            if (_Current == null)
            {
                return;
            }

            var length = Math.Max(0f, _Current.End - _Current.Start);
            var elapsed = Math.Max(0f, Math.Min(length, _Current.Player.Position - _Current.Start));
            _Texts[_TextTimeElapsed].Text = _FormatTime(elapsed);
            _Texts[_TextTimeTotal].Text = _FormatTime(length);
            _Statics[_StaticTimeBarFill].W = length > 0f ? _TimeBarWidth * elapsed / length : 0f;
        }

        private void _SetButtonState(string button, bool active)
        {
            var color = _ButtonColors[button];
            _Buttons[button].Color = active ? color : new SColorF(color.R, color.G, color.B, color.A * _DimmedAlpha);
        }

        private static string _FormatTime(float seconds)
        {
            var total = (int)seconds;
            return (total / 60) + ":" + (total % 60).ToString("00");
        }
    }
}
