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
using System.Linq;
using Vocaluxe.Base;
using Vocaluxe.Base.Web;
using VocaluxeLib;
using VocaluxeLib.Game;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;

namespace Vocaluxe.Screens
{
    public class CScreenScore : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 6; }
        }

        private const string _TextSong = "TextSong";

        private const string _StaticCover = "StaticCover";
        private const string _StaticRoundLeft = "StaticRoundLeft";
        private const string _StaticRoundRight = "StaticRoundRight";
        private const string _StaticLegendGolden = "StaticLegendGolden";
        private const string _StaticLegendLineBonus = "StaticLegendLineBonus";
        private const string _TextLegendGolden = "TextLegendGolden";
        private const string _TextLegendLineBonus = "TextLegendLineBonus";

        private const float _RevealDuration = 300f;

        private const string _ScreenSettingShortScore = "ScreenSettingShortScore";
        private const string _ScreenSettingShortRating = "ScreenSettingShortRating";
        private const string _ScreenSettingShortDifficulty = "ScreenSettingShortDifficulty";

        private CBackground _SlideShowBG;

        private string[,] _TextNames;
        private string[,] _TextScores;
        private string[,] _TextRatings;
        private string[,] _TextDifficulty;
        private string[,] _ProgressBarPoints;
        private string[,] _ProgressBarGolden;
        private string[,] _ProgressBarNotes;
        private string[,] _StaticAvatar;
        private string[,] _StaticRow;
        private int _Round;
        private CPoints _Points;

        private struct STextStyle
        {
            public float Height;
            public float Y;
            public SColorF Color;
        }

        private readonly Dictionary<string, STextStyle> _TextStyles = new Dictionary<string, STextStyle>();
        private readonly Stopwatch _RevealTimer = new Stopwatch();
        private readonly Stopwatch _PulseTimer = new Stopwatch();
        private readonly bool[] _IsWinner = new bool[CSettings.MaxNumPlayer];
        private SColorF _WinnerColor;

        public override EMusicType CurrentMusicType
        {
            get { return EMusicType.BackgroundPreview; }
        }

        public override void Init()
        {
            base.Init();

            var texts = new List<string> { _TextSong, _TextLegendGolden, _TextLegendLineBonus };

            _BuildTextStrings(ref texts);

            _ThemeTexts = texts.ToArray();

            var statics = new List<string> { _StaticCover, _StaticRoundLeft, _StaticRoundRight, _StaticLegendGolden, _StaticLegendLineBonus };
            _BuildStaticStrings(ref statics);

            _ThemeStatics = statics.ToArray();

            var progressBars = new List<string>();
            _BuildProgressBarString(ref progressBars);

            _ThemeProgressBars = progressBars.ToArray();

            _ThemeScreenSettings = new string[] { _ScreenSettingShortScore, _ScreenSettingShortRating, _ScreenSettingShortDifficulty };

            _SlideShowBG = GetNewBackground();
            _AddBackground(_SlideShowBG);
            _SlideShowBG.Z--;
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            foreach (var avatar in _StaticAvatar.Cast<string>().Where(name => name != null))
            {
                _Statics[avatar].Aspect = EAspect.Crop;
            }

            _TextStyles.Clear();
            foreach (var name in _TextNames.Cast<string>().Concat(_TextScores.Cast<string>()).Concat(_TextRatings.Cast<string>()).Where(name => name != null))
            {
                _TextStyles[name] = new STextStyle { Height = _Texts[name].Font.Height, Y = _Texts[name].Y, Color = _Texts[name].Color };
            }

            if (!CBase.Themes.GetColor("Top1Color", PartyModeId, out _WinnerColor))
            {
                _WinnerColor = new SColorF(1f, 0.8f, 0f, 1f);
            }

            _WinnerColor.A = 1f;
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (!keyEvent.KeyPressed)
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                    case Keys.Enter:
                        _LeaveScreen();
                        break;

                    case Keys.Left:
                        _ChangeRound(-1);
                        break;

                    case Keys.Right:
                        _ChangeRound(1);
                        break;
                }
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.Wheel != 0)
            {
                _ChangeRound(mouseEvent.Wheel);
            }

            if (mouseEvent.LB)
            {
                if (_IsMouseOverStatic(mouseEvent, _StaticRoundLeft))
                {
                    _ChangeRound(-1);
                }
                else if (_IsMouseOverStatic(mouseEvent, _StaticRoundRight))
                {
                    _ChangeRound(1);
                }
                else
                {
                    _LeaveScreen();
                }
            }

            if (mouseEvent.RB)
            {
                _LeaveScreen();
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
            //-1 --> Show average
            _Round = CGame.NumRounds > 1 ? -1 : 0;
            _Points = CGame.GetPoints();

            _SavePlayedSongs();

            _SetVisibility();
            _UpdateRatings();
            _SlideShowBG.Visible = _UpdateBackground();

            var lastSong = CGame.GetSong(CGame.NumRounds - 1);
            if (lastSong != null)
            {
                CBackgroundMusic.LoadPreview(lastSong);
            }
        }

        public override void OnClose()
        {
            base.OnClose();
            _StopScoreSounds();
        }

        public override bool UpdateGame()
        {
            _UpdateProgressBarSound();
            _UpdateRevealAnimation();

            return true;
        }

        private void _UpdateRevealAnimation()
        {
            if (!_RevealTimer.IsRunning)
            {
                if (_AreProgressBarsAnimating())
                {
                    return;
                }

                _RevealTimer.Restart();
                _PulseTimer.Restart();
            }

            var progress = Math.Min(_RevealTimer.ElapsedMilliseconds / _RevealDuration, 1f);
            var revealScale = 1.3f - 0.3f * (1f - (1f - progress) * (1f - progress));
            var pulseScale = 1f + 0.03f * (float)Math.Sin(_PulseTimer.ElapsedMilliseconds / 1000.0 * Math.PI * 1.4);
            var numPlayer = CGame.NumPlayers - 1;

            for (var p = 0; p < CGame.NumPlayers; p++)
            {
                _ApplyTextStyle(_TextRatings[p, numPlayer], revealScale, progress, false);
                if (_IsWinner[p])
                {
                    _ApplyTextStyle(_TextNames[p, numPlayer], pulseScale, 1f, false);
                    _ApplyTextStyle(_TextScores[p, numPlayer], pulseScale, 1f, true);
                }
            }
        }

        private void _ResetRevealAnimation()
        {
            _RevealTimer.Reset();
            _PulseTimer.Reset();
            var numPlayer = CGame.NumPlayers - 1;

            for (var p = 0; p < CGame.NumPlayers; p++)
            {
                _ApplyTextStyle(_TextRatings[p, numPlayer], 1f, 0f, false);
                _ApplyTextStyle(_TextNames[p, numPlayer], 1f, 1f, false);
                _ApplyTextStyle(_TextScores[p, numPlayer], 1f, 1f, false);
            }
        }

        private void _ApplyTextStyle(string name, float scale, float alpha, bool winnerColor)
        {
            var style = _TextStyles[name];
            var text = _Texts[name];
            text.Color = winnerColor ? _WinnerColor : style.Color;
            text.Font.Height = style.Height * scale;
            text.Y = style.Y - style.Height * (scale - 1f) / 2f;
            text.Alpha = alpha;
        }

        private bool _AreProgressBarsAnimating()
        {
            return Enumerable.Range(0, CGame.NumPlayers)
                             .Select(p => _ProgressBars[_ProgressBarPoints[p, CGame.NumPlayers - 1]])
                             .Any(progressBar => progressBar.Visible && progressBar.IsAnimating);
        }

        private bool _IsMouseOverStatic(SMouseEvent mouseEvent, string staticName)
        {
            var element = _Statics[staticName];
            return element.Visible && CHelper.IsInBounds(element.Rect, mouseEvent);
        }

        private bool _IsKaraokeMode(int round)
        {
            if (round >= 0)
            {
                return CGame.GetAudioMode(round) == EAudioMode.TR_AUDIOMODE_KARAOKE;
            }

            for (var r = 0; r < CGame.NumRounds; r++)
            {
                if (CGame.GetAudioMode(r) != EAudioMode.TR_AUDIOMODE_KARAOKE)
                {
                    return false;
                }
            }

            return true;
        }

        private string _GetRating(double points)
        {
            string rating;

            if (_IsKaraokeMode(_Round))
            {
                rating = "TR_RATING_KARAOKE";
            }
            else if (points >= 9800)
            {
                rating = "TR_RATING_VOCAL_HERO";
            }
            else if (points >= 8400)
            {
                rating = "TR_RATING_SUPERSTAR";
            }
            else if (points >= 7000)
            {
                rating = "TR_RATING_LEAD_SINGER";
            }
            else if (points >= 5600)
            {
                rating = "TR_RATING_RISING_STAR";
            }
            else if (points >= 4200)
            {
                rating = "TR_RATING_HOPEFUL";
            }
            else if (points >= 2800)
            {
                rating = "TR_RATING_WANNABE";
            }
            else if (points >= 1400)
            {
                rating = "TR_RATING_AMATEUR";
            }
            else
            {
                rating = "TR_RATING_TONE_DEAF";
            }

            return rating;
        }

        private void _BuildTextStrings(ref List<string> texts)
        {
            _TextNames = new string[CSettings.MaxNumPlayer, CSettings.MaxNumPlayer];
            _TextScores = new string[CSettings.MaxNumPlayer, CSettings.MaxNumPlayer];
            _TextRatings = new string[CSettings.MaxNumPlayer, CSettings.MaxNumPlayer];
            _TextDifficulty = new string[CSettings.MaxNumPlayer, CSettings.MaxNumPlayer];

            for (var numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (var player = 0; player < CSettings.MaxNumPlayer; player++)
                {
                    if (player <= numplayer)
                    {
                        var target = "P" + (player + 1) + "N" + (numplayer + 1);
                        _TextNames[player, numplayer] = "TextName" + target;
                        _TextScores[player, numplayer] = "TextScore" + target;
                        _TextRatings[player, numplayer] = "TextRating" + target;
                        _TextDifficulty[player, numplayer] = "TextDifficulty" + target;

                        texts.Add(_TextNames[player, numplayer]);
                        texts.Add(_TextScores[player, numplayer]);
                        texts.Add(_TextRatings[player, numplayer]);
                        texts.Add(_TextDifficulty[player, numplayer]);
                    }
                }
            }
        }

        private void _BuildStaticStrings(ref List<string> statics)
        {
            _StaticAvatar = new string[CSettings.MaxNumPlayer, CSettings.MaxNumPlayer];
            _StaticRow = new string[CSettings.MaxNumPlayer, CSettings.MaxNumPlayer];

            for (var numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (var player = 0; player < CSettings.MaxNumPlayer; player++)
                {
                    if (player > numplayer)
                    {
                        continue;
                    }

                    var target = "P" + (player + 1) + "N" + (numplayer + 1);
                    _StaticAvatar[player, numplayer] = "StaticAvatar" + target;
                    _StaticRow[player, numplayer] = "StaticRow" + target;

                    statics.Add(_StaticAvatar[player, numplayer]);
                    statics.Add(_StaticRow[player, numplayer]);
                }
            }
        }

        private void _BuildProgressBarString(ref List<string> progressBars)
        {
            _ProgressBarPoints = new string[CSettings.MaxNumPlayer, CSettings.MaxNumPlayer];
            _ProgressBarGolden = new string[CSettings.MaxNumPlayer, CSettings.MaxNumPlayer];
            _ProgressBarNotes = new string[CSettings.MaxNumPlayer, CSettings.MaxNumPlayer];

            for (var numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (var player = 0; player < CSettings.MaxNumPlayer; player++)
                {
                    if (player > numplayer)
                    {
                        continue;
                    }

                    var target = "P" + (player + 1) + "N" + (numplayer + 1);
                    _ProgressBarPoints[player, numplayer] = "ProgressBarPoints" + target;
                    _ProgressBarGolden[player, numplayer] = "ProgressBarGolden" + target;
                    _ProgressBarNotes[player, numplayer] = "ProgressBarNotes" + target;

                    progressBars.Add(_ProgressBarPoints[player, numplayer]);
                    progressBars.Add(_ProgressBarGolden[player, numplayer]);
                    progressBars.Add(_ProgressBarNotes[player, numplayer]);
                }
            }
        }

        private int _ProgressBarSoundStream = -1;
        private readonly Stopwatch _ProgressBarSoundTimer = new Stopwatch();
        private double _ProgressBarSoundDuration;
        private int _ProgressBarSoundMaxPoints;

        private void _PlayProgressBarSound(int maxPoints)
        {
            // Stop any previous ProgressBar sound
            _StopScoreSounds();

            // Do not play in Karaoke Mode
            if (_IsKaraokeMode(_Round) || maxPoints <= 0)
            {
                return;
            }

            // Calculate duration: 10,000 points = 5 seconds
            _ProgressBarSoundDuration = Math.Min(maxPoints / 10000.0, 1.0) * 5;
            _ProgressBarSoundMaxPoints = maxPoints;

            // Play the sound
            _ProgressBarSoundStream = PlaySound(ESounds.ProgressBar, CConfig.SoundEffectVolume);
            _ProgressBarSoundTimer.Restart();
        }

        private void _UpdateProgressBarSound()
        {
            if (!_ProgressBarSoundTimer.IsRunning || !_IsProgressBarAnimationFinished())
            {
                return;
            }

            _ProgressBarSoundTimer.Reset();
            _CloseStream(ref _ProgressBarSoundStream);
            _PlayApplauseSound(_ProgressBarSoundMaxPoints);
        }

        private bool _IsProgressBarAnimationFinished()
        {
            var animatedProgressBars = Enumerable.Range(0, CGame.NumPlayers)
                                                 .Select(p => _ProgressBars[_ProgressBarPoints[p, CGame.NumPlayers - 1]])
                                                 .Where(progressBar => progressBar.Visible && progressBar.AnimatesMovement)
                                                 .ToList();

            if (animatedProgressBars.Count == 0)
            {
                return _ProgressBarSoundTimer.Elapsed.TotalSeconds >= _ProgressBarSoundDuration;
            }

            return !animatedProgressBars.Any(progressBar => progressBar.IsAnimating);
        }

        private void _StopScoreSounds()
        {
            _ProgressBarSoundTimer.Reset();
            _CloseStream(ref _ProgressBarSoundStream);
            _CloseStream(ref _ApplauseStream);
        }

        private static void _CloseStream(ref int stream)
        {
            if (stream == -1)
            {
                return;
            }

            CSound.Close(stream);
            stream = -1;
        }

        private int _ApplauseStream = -1;

        private void _PlayApplauseSound(int maxPoints)
        {
            // Play no applause sound based on Karaoke Mode
            if (_IsKaraokeMode(_Round))
            {
                return;
            }

            // Play the appropriate applause sound based on maxPoints
            if (maxPoints >= 8000)
            {
                _ApplauseStream = PlaySound(ESounds.ApplauseHigh, CConfig.SoundEffectVolume);
            }
            else if (maxPoints >= 5000)
            {
                _ApplauseStream = PlaySound(ESounds.ApplauseMid, CConfig.SoundEffectVolume);
            }
            else if (maxPoints >= 2000)
            {
                _ApplauseStream = PlaySound(ESounds.ApplauseLow, CConfig.SoundEffectVolume);
            }
        }

        private static int PlaySound(ESounds sound, int volume)
        {
            var streamId = CSound.PlaySound(sound, false);
            CSound.SetStreamVolume(streamId, volume);

            return streamId;
        }

        private void _UpdateRatings()
        {
            CSong song = null;
            if (_Round >= 0)
            {
                song = CGame.GetSong(_Round);
                if (song == null)
                {
                    return;
                }

                _Texts[_TextSong].Text = song.Artist + " - " + song.Title;
                if (_Points.NumRounds > 1)
                {
                    _Texts[_TextSong].Text += " (" + (_Round + 1) + "/" + _Points.NumRounds + ")";
                }
            }
            else
            {
                _Texts[_TextSong].Text = "TR_SCREENSCORE_OVERALLSCORE";
            }

            var players = _GetPlayers(_Round);
            var maxPoints = (int)Math.Round(players.Max(player => player.Points));
            _PlayProgressBarSound(maxPoints);

            _UpdateCover(song);
            _UpdateRoundArrows();

            var showWinner = CGame.NumPlayers > 1 && !_IsKaraokeMode(_Round);

            for (var p = 0; p < players.Length; p++)
            {
                var playerPoints = (int)Math.Round(players[p].Points);
                _IsWinner[p] = showWinner && playerPoints > 0 && players.All(other => (int)Math.Round(other.Points) <= playerPoints);

                var name = CProfiles.GetPlayerName(players[p].ProfileId, p);
                if (song != null && song.IsDuet)
                {
                    if (song.Notes.VoiceNames.IsSet(players[p].VoiceNr))
                    {
                        name += " (" + song.Notes.VoiceNames[players[p].VoiceNr] + ")";
                    }
                }

                _Texts[_TextNames[p, CGame.NumPlayers - 1]].Text = name;

                if (CGame.NumPlayers < (int)_ScreenSettings[_ScreenSettingShortScore].GetValue())
                {
                    _Texts[_TextScores[p, CGame.NumPlayers - 1]].Text = ((int)Math.Round(players[p].Points)).ToString("0000") + " " + CLanguage.Translate("TR_SCREENSCORE_POINTS");
                }
                else
                {
                    _Texts[_TextScores[p, CGame.NumPlayers - 1]].Text = ((int)Math.Round(players[p].Points)).ToString("0000");
                }

                if (CGame.NumPlayers < (int)_ScreenSettings[_ScreenSettingShortDifficulty].GetValue())
                {
                    _Texts[_TextDifficulty[p, CGame.NumPlayers - 1]].Text = CLanguage.Translate("TR_SCREENSCORE_GAMEDIFFICULTY") + ": " +
                                                                            CLanguage.Translate(CProfiles.GetDifficulty(players[p].ProfileId).ToString());
                }
                else
                {
                    _Texts[_TextDifficulty[p, CGame.NumPlayers - 1]].Text = CLanguage.Translate(CProfiles.GetDifficulty(players[p].ProfileId).ToString());
                }

                if (CGame.NumPlayers < (int)_ScreenSettings[_ScreenSettingShortRating].GetValue())
                {
                    _Texts[_TextRatings[p, CGame.NumPlayers - 1]].Text = CLanguage.Translate("TR_SCREENSCORE_RATING") + ": " +
                                                                         CLanguage.Translate(_GetRating((int)Math.Round(players[p].Points)));
                }
                else
                {
                    _Texts[_TextRatings[p, CGame.NumPlayers - 1]].Text = CLanguage.Translate(_GetRating((int)Math.Round(players[p].Points)));
                }

                _ProgressBars[_ProgressBarPoints[p, CGame.NumPlayers - 1]].Progress = (float)players[p].Points / CSettings.MaxScore;
                _ProgressBars[_ProgressBarGolden[p, CGame.NumPlayers - 1]].Progress = (float)Math.Max(players[p].Points - players[p].PointsLineBonus, 0) / CSettings.MaxScore;
                _ProgressBars[_ProgressBarNotes[p, CGame.NumPlayers - 1]].Progress = (float)Math.Max(players[p].Points - players[p].PointsLineBonus - players[p].PointsGoldenNotes, 0) / CSettings.MaxScore;

                if (CProfiles.IsProfileIdValid(players[p].ProfileId))
                {
                    _Statics[_StaticAvatar[p, CGame.NumPlayers - 1]].Texture = CProfiles.GetAvatarTextureFromProfile(players[p].ProfileId);
                }
            }

            _ResetRevealAnimation();
        }

        private SPlayer[] _GetPlayers(int round)
        {
            if (round >= 0)
            {
                return _Points.GetPlayer(round, CGame.NumPlayers);
            }

            var players = new SPlayer[CGame.NumPlayers];
            for (var i = 0; i < CGame.NumRounds; i++)
            {
                var points = _Points.GetPlayer(i, CGame.NumPlayers);
                for (var p = 0; p < players.Length; p++)
                {
                    if (i < 1)
                    {
                        players[p].ProfileId = points[p].ProfileId;
                    }

                    players[p].Points += points[p].Points;
                    players[p].PointsGoldenNotes += points[p].PointsGoldenNotes;
                    players[p].PointsLineBonus += points[p].PointsLineBonus;
                }
            }

            for (var p = 0; p < players.Length; p++)
            {
                players[p].Points = (int)Math.Round(players[p].Points / CGame.NumRounds);
                players[p].PointsGoldenNotes /= CGame.NumRounds;
                players[p].PointsLineBonus /= CGame.NumRounds;
            }

            return players;
        }

        private void _UpdateCover(CSong song)
        {
            var cover = _Statics[_StaticCover];
            cover.Visible = song != null;
            if (song == null)
            {
                return;
            }

            cover.Texture = song.CoverTextureSmall;
            cover.Aspect = EAspect.Crop;
            cover.X = _Texts[_TextSong].Rect.X - cover.W - 15f;
        }

        private void _UpdateRoundArrows()
        {
            var hasRounds = _Points.NumRounds > 1;
            _Statics[_StaticRoundLeft].Visible = hasRounds;
            _Statics[_StaticRoundRight].Visible = hasRounds;
            _Statics[_StaticRoundLeft].Alpha = _Round > -1 ? 1f : 0.3f;
            _Statics[_StaticRoundRight].Alpha = _Round < _Points.NumRounds - 1 ? 1f : 0.3f;
        }

        private void _SetVisibility()
        {
            var isKaraokeMode = _IsKaraokeMode(_Round);

            _Statics[_StaticLegendGolden].Visible = !isKaraokeMode;
            _Statics[_StaticLegendLineBonus].Visible = !isKaraokeMode;
            _Texts[_TextLegendGolden].Visible = !isKaraokeMode;
            _Texts[_TextLegendLineBonus].Visible = !isKaraokeMode;

            for (var numplayer = 0; numplayer < CSettings.MaxNumPlayer; numplayer++)
            {
                for (var player = 0; player < CSettings.MaxNumPlayer; player++)
                {
                    if (player <= numplayer)
                    {
                        var isVisible = numplayer + 1 == CGame.NumPlayers;

                        _Texts[_TextNames[player, numplayer]].Visible = isVisible;
                        _Texts[_TextScores[player, numplayer]].Visible = isVisible && !isKaraokeMode;
                        _Texts[_TextRatings[player, numplayer]].Visible = isVisible;
                        _Texts[_TextDifficulty[player, numplayer]].Visible = isVisible;
                        _ProgressBars[_ProgressBarPoints[player, numplayer]].Visible = isVisible && !isKaraokeMode;
                        _ProgressBars[_ProgressBarPoints[player, numplayer]].Reset(true);
                        _ProgressBars[_ProgressBarGolden[player, numplayer]].Visible = isVisible && !isKaraokeMode;
                        _ProgressBars[_ProgressBarGolden[player, numplayer]].Reset(true);
                        _ProgressBars[_ProgressBarNotes[player, numplayer]].Visible = isVisible && !isKaraokeMode;
                        _ProgressBars[_ProgressBarNotes[player, numplayer]].Reset(true);
                        _Statics[_StaticAvatar[player, numplayer]].Visible = isVisible;
                        _Statics[_StaticRow[player, numplayer]].Visible = isVisible;

                        _Statics[_StaticAvatar[player, numplayer]].Texture = null;
                    }
                }
            }
        }

        private void _ChangeRound(int num)
        {
            var round = (_Round + num).Clamp(-1, _Points.NumRounds - 1);
            if (round == _Round)
            {
                return;
            }

            _Round = round;
            _SetVisibility();
            _UpdateRatings();
        }

        private void _SavePlayedSongs()
        {
            for (var round = 0; round < _Points.NumRounds; round++)
            {
                var players = _Points.GetPlayer(round, CGame.NumPlayers);
                var isKaraokeMode = CGame.GetAudioMode(round) == EAudioMode.TR_AUDIOMODE_KARAOKE;

                for (var p = 0; p < players.Length; p++)
                {
                    if ((isKaraokeMode || players[p].Points > CSettings.MinScoreForDB) && players[p].SongFinished)
                    {
                        var song = CSongs.GetSong(players[p].SongId);
                        CDataBase.IncreaseSongCounter(song.DataBaseSongId);
                        song.NumPlayed++;
                        song.NumPlayedSession++;
                        break;
                    }
                }
            }
        }

        private bool _UpdateBackground()
        {
            var photos = CWebServer.GetPhotosOfThisRound();
            _SlideShowBG.RemoveSlideShowTextures();
            foreach (var photo in photos)
            {
                _SlideShowBG.AddSlideShowTexture(photo);
            }

            return photos.Length > 0;
        }

        private void _LeaveScreen()
        {
            _StopScoreSounds();

            if (_IsKaraokeMode(-1))
            {
                if (!CJukebox.ReturnAfterSinging() && !CWebQueue.ReturnAfterSinging())
                {
                    CGraphics.FadeTo(EScreen.Song);
                }
            }
            else
            {
                CParty.LeavingScore();
            }
        }
    }
}