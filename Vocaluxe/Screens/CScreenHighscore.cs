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
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;

namespace Vocaluxe.Screens
{
    public class CScreenHighscore : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 4; }
        }

        private const int _NumEntrys = 10;
        private const int _NumPodium = 3;
        private const int _NumRows = _NumEntrys - _NumPodium;
        private const string _TextSongName = "TextSongName";
        private const string _TextSongMode = "TextSongMode";
        private const string _StaticCrown = "StaticCrown";
        private string[] _TextNumber;
        private string[] _TextName;
        private string[] _TextScore;
        private string[] _TextDate;
        private string[] _ParticleEffectNew;
        private string[] _StaticPodium;
        private string[] _StaticAvatar;
        private string[] _ProgressBarScore;

        private CTextureRef[] _PodiumAvatars;
        private List<SDBScoreEntry>[] _Scores;
        private List<int> _NewEntryIds;
        private int _Round;
        private bool _IsDuet;
        private bool _FromScreenSong = false;

        public override EMusicType CurrentMusicType
        {
            get { return EMusicType.BackgroundPreview; }
        }

        private int _HighscoreStream = -1;
        private bool _HasPlayedHighscoreSound = false;

        private struct STextStyle
        {
            public float Height;
            public float Y;
            public SColorF Color;
        }

        private readonly Dictionary<string, STextStyle> _TextStyles = new Dictionary<string, STextStyle>();
        private readonly Stopwatch _PulseTimer = new Stopwatch();
        private SColorF _NewEntryColor;

        private static int PlaySound(ESounds sound, int volume)
        {
            var streamId = CSound.PlaySound(sound, false);
            CSound.SetStreamVolume(streamId, volume);

            return streamId;
        }

        public override void Init()
        {
            base.Init();

            var texts = new List<string> { _TextSongName, _TextSongMode };

            _TextNumber = new string[_NumEntrys];
            for (var i = 0; i < _NumEntrys; i++)
            {
                _TextNumber[i] = "TextNumber" + (i + 1);
                texts.Add(_TextNumber[i]);
            }

            _TextName = new string[_NumEntrys];
            for (var i = 0; i < _NumEntrys; i++)
            {
                _TextName[i] = "TextName" + (i + 1);
                texts.Add(_TextName[i]);
            }

            _TextScore = new string[_NumEntrys];
            for (var i = 0; i < _NumEntrys; i++)
            {
                _TextScore[i] = "TextScore" + (i + 1);
                texts.Add(_TextScore[i]);
            }

            _TextDate = new string[_NumEntrys];
            for (var i = 0; i < _NumEntrys; i++)
            {
                _TextDate[i] = "TextDate" + (i + 1);
                texts.Add(_TextDate[i]);
            }

            _ParticleEffectNew = new string[_NumEntrys];
            for (var i = 0; i < _NumEntrys; i++)
            {
                _ParticleEffectNew[i] = "ParticleEffectNew" + (i + 1);
            }

            var statics = new List<string> { _StaticCrown };

            _StaticPodium = new string[_NumPodium];
            _StaticAvatar = new string[_NumPodium];
            for (var i = 0; i < _NumPodium; i++)
            {
                _StaticPodium[i] = "StaticPodium" + (i + 1);
                _StaticAvatar[i] = "StaticAvatar" + (i + 1);
                statics.Add(_StaticPodium[i]);
                statics.Add(_StaticAvatar[i]);
            }

            _ProgressBarScore = new string[_NumRows];
            for (var i = 0; i < _NumRows; i++)
            {
                _ProgressBarScore[i] = "ProgressBarScore" + (i + _NumPodium + 1);
            }

            _ThemeStatics = statics.ToArray();
            _ThemeTexts = texts.ToArray();
            _ThemeParticleEffects = _ParticleEffectNew;
            _ThemeProgressBars = _ProgressBarScore;

            _PodiumAvatars = new CTextureRef[_NumPodium];
            _NewEntryIds = new List<int>();
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);

            foreach (var avatar in _StaticAvatar)
            {
                _Statics[avatar].Aspect = EAspect.Crop;
            }

            _TextStyles.Clear();
            foreach (var name in _TextNumber.Concat(_TextName).Concat(_TextScore).Concat(_TextDate))
            {
                _TextStyles[name] = new STextStyle { Height = _Texts[name].Font.Height, Y = _Texts[name].Y, Color = _Texts[name].Color };
            }

            if (!CBase.Themes.GetColor("Top1Color", PartyModeId, out _NewEntryColor))
            {
                _NewEntryColor = new SColorF(1f, 0.8f, 0f, 1f);
            }

            _NewEntryColor.A = 1f;
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (keyEvent.KeyPressed && !Char.IsControl(keyEvent.Unicode)) { }
            else
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
            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent)) { }

            if (mouseEvent.LB)
            {
                _LeaveScreen();
            }

            if (mouseEvent.RB)
            {
                _LeaveScreen();
            }

            if (mouseEvent.MB)
            {
                var lastRound = _Round;
                _ChangeRound(1);
                if (lastRound == _Round)
                {
                    _Round = 0;
                    _UpdateRound();
                }
            }

            return true;
        }

        public override bool UpdateGame()
        {
            var numScores = _Scores[_Round].Count;
            _Statics[_StaticCrown].Visible = numScores > 0;
            foreach (var podium in _StaticPodium)
            {
                _Statics[podium].Visible = numScores > 0;
            }

            for (var p = 0; p < _NumEntrys; p++)
            {
                var isEntry = p < numScores;
                var isNew = isEntry && _IsNewEntry(_Scores[_Round][p].Id);

                _Texts[_TextNumber[p]].Visible = isEntry || (p < _NumPodium && numScores > 0);
                _Texts[_TextName[p]].Visible = isEntry;
                _Texts[_TextScore[p]].Visible = isEntry;
                _Texts[_TextDate[p]].Visible = isEntry;
                _ParticleEffects[_ParticleEffectNew[p]].Visible = isNew;
                _SetNewEntryStyle(p, isNew);

                _Texts[_TextNumber[p]].Text = (p + 1).ToString();

                if (p < _NumPodium)
                {
                    _Statics[_StaticAvatar[p]].Texture = _PodiumAvatars[p];
                    _Statics[_StaticAvatar[p]].Visible = isEntry && _Statics[_StaticAvatar[p]].Texture != null;
                }
                else
                {
                    var row = p - _NumPodium;
                    _ProgressBars[_ProgressBarScore[row]].Visible = isEntry;

                    var progress = isEntry ? (float)_Scores[_Round][p].Score / CSettings.MaxScore : 0f;
                    if (_ProgressBars[_ProgressBarScore[row]].Progress != progress)
                    {
                        _ProgressBars[_ProgressBarScore[row]].Progress = progress;
                    }
                }

                if (!isEntry)
                {
                    continue;
                }

                var name = _Scores[_Round][p].Name;
                name += " [" + CLanguage.Translate(Enum.GetName(typeof(EGameDifficulty), _Scores[_Round][p].Difficulty)) + "]";
                if (_IsDuet)
                {
                    var voiceNr = _Scores[_Round][p].VoiceNr;
                    string voiceName = null;

                    var song = _GetCurrentSong();
                    if (song != null && song.Notes != null && song.Notes.VoiceNames != null)
                    {
                        try
                        {
                            voiceName = song.Notes.VoiceNames[voiceNr];
                        }
                        catch
                        {
                            voiceName = null; // Ungültiger Index
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(voiceName))
                    {
                        name += " (" + voiceName + ")";
                    }
                    else
                    {
                        name += " (P" + (voiceNr + 1) + ")";
                    }
                }

                _Texts[_TextName[p]].Text = name;

                _Texts[_TextScore[p]].Text = _Scores[_Round][p].Score.ToString("D");
                _Texts[_TextDate[p]].Text = _Scores[_Round][p].Date;

                if (isNew && !_HasPlayedHighscoreSound)
                {
                    _HighscoreStream = CScreenHighscore.PlaySound(ESounds.Highscore, CConfig.SoundEffectVolume);
                    _HasPlayedHighscoreSound = true;
                }
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
            _HasPlayedHighscoreSound = false;
            _PulseTimer.Restart();
            _Round = 0;
            _NewEntryIds.Clear();
            _AddScoresToDB();
            _LoadScores();
            _UpdateRound();

            UpdateGame();
        }

        private void _SetNewEntryStyle(int entry, bool isNew)
        {
            var scale = isNew ? 1f + 0.03f * (float)Math.Sin(_PulseTimer.ElapsedMilliseconds / 1000.0 * Math.PI * 1.4) : 1f;
            foreach (var name in new[] { _TextNumber[entry], _TextName[entry], _TextScore[entry], _TextDate[entry] })
            {
                var style = _TextStyles[name];
                var text = _Texts[name];
                text.Color = isNew ? _NewEntryColor : style.Color;
                text.Font.Height = style.Height * scale;
                text.Y = style.Y - style.Height * (scale - 1f) / 2f;
            }
        }

        private bool _IsNewEntry(int id)
        {
            return _NewEntryIds.Any(t => t == id);
        }

        private void _AddScoresToDB()
        {
            var points = CGame.GetPoints();
            if (points == null)
            {
                return;
            }

            // No points in Karaoke Mode
            if (CScreenSong.GetAudioMode() == EAudioMode.TR_AUDIOMODE_KARAOKE)
            {
                return;
            }

            for (var round = 0; round < points.NumRounds; round++)
            {
                var players = points.GetPlayer(round, CGame.NumPlayers);

                for (var p = 0; p < players.Length; p++)
                {
                    if (players[p].Points > CSettings.MinScoreForDB && players[p].SongFinished && !CProfiles.IsGuestProfile(players[p].ProfileId))
                    {
                        _NewEntryIds.Add(CDataBase.AddScore(players[p]));
                    }
                }
            }
        }

        private void _LoadScores()
        {
            var rounds = CGame.NumRounds;

            if (rounds == 0)
            {
                _FromScreenSong = true;
                _Round = (int)EGameMode.TR_GAMEMODE_NORMAL;
                _Scores = new List<SDBScoreEntry>[4];
                var songId = CScreenSong.getSelectedSongId();
                var style = CBase.Config.GetHighscoreStyle();
                var foundHighscoreEntries = false;

                for (var gameModeNum = 0; gameModeNum < 4; gameModeNum++)
                {
                    _Scores[gameModeNum] = CDataBase.LoadScore(songId, (EGameMode)gameModeNum, style);
                    if (!foundHighscoreEntries && _Scores[gameModeNum].Count > 0)
                    {
                        _Round = gameModeNum;
                        foundHighscoreEntries = true;
                    }
                }
            }
            else
            {
                _FromScreenSong = false;
                _Scores = new List<SDBScoreEntry>[rounds];
                for (var round = 0; round < rounds; round++)
                {
                    var songId = CGame.GetSong(round).Id;
                    var gameMode = CGame.GetGameMode(round);
                    var style = CBase.Config.GetHighscoreStyle();
                    _Scores[round] = CDataBase.LoadScore(songId, gameMode, style);
                }
            }
        }

        private void _UpdateRound()
        {
            _IsDuet = false;
            var points = CGame.GetPoints();

            for (var p = 0; p < _NumPodium; p++)
            {
                _PodiumAvatars[p] = p < _Scores[_Round].Count ? _GetAvatarTexture(_Scores[_Round][p].Name) : null;
            }

            var song = _GetCurrentSong();
            if (song == null)
            {
                return;
            }

            _Texts[_TextSongName].Text = song.Artist + " - " + song.Title;
            if (points != null && !_FromScreenSong && points.NumRounds > 1)
            {
                _Texts[_TextSongName].Text += " (" + (_Round + 1) + "/" + points.NumRounds + ")";
            }

            switch (_FromScreenSong ? (EGameMode)_Round : CGame.GetGameMode(_Round))
            {
                case EGameMode.TR_GAMEMODE_NORMAL:
                    _Texts[_TextSongMode].Text = "TR_GAMEMODE_NORMAL";
                    break;

                case EGameMode.TR_GAMEMODE_MEDLEY:
                    _Texts[_TextSongMode].Text = "TR_GAMEMODE_MEDLEY";
                    break;

                case EGameMode.TR_GAMEMODE_DUET:
                    _Texts[_TextSongMode].Text = "TR_GAMEMODE_DUET";
                    _IsDuet = true;
                    break;

                case EGameMode.TR_GAMEMODE_SHORTSONG:
                    _Texts[_TextSongMode].Text = "TR_GAMEMODE_SHORTSONG";
                    break;

                default:
                    _Texts[_TextSongMode].Text = "TR_GAMEMODE_NORMAL";
                    break;
            }
        }

        private CSong _GetCurrentSong()
        {
            if (_FromScreenSong)
            {
                return CSongs.GetSong(CScreenSong.getSelectedSongId());
            }

            return CGame.GetSong(_Round);
        }

        private static CTextureRef _GetAvatarTexture(string playerName)
        {
            var profile = CProfiles.GetProfiles().FirstOrDefault(pr => pr.PlayerName == playerName && pr.Avatar != null);
            return profile == null ? null : profile.Avatar.Texture;
        }

        private void _ChangeRound(int num)
        {
            if (_FromScreenSong)
            {
                if (_Round == (int)EGameMode.TR_GAMEMODE_SHORTSONG)
                {
                    _Round = (int)EGameMode.TR_GAMEMODE_NORMAL;
                }
                else
                {
                    ++_Round;
                }
            }
            else
            {
                var points = CGame.GetPoints();
                _Round += num;
                _Round = _Round.Clamp(0, points.NumRounds - 1);
            }

            _UpdateRound();
        }

        private void _LeaveScreen()
        {
            if (_HighscoreStream != -1)
            {
                CSound.Close(_HighscoreStream);
                _HighscoreStream = -1;
            }

            CParty.LeavingHighscore();
        }
    }
}