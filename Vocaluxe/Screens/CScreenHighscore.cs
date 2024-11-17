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
using System.Linq;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Game;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;
using Vocaluxe.Lib.Sound;

namespace Vocaluxe.Screens
{
    public class CScreenHighscore : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 2; }
        }

        private const int _NumEntrys = 10;
        private const string _TextSongName = "TextSongName";
        private const string _TextSongMode = "TextSongMode";
        private string[] _TextNumber;
        private string[] _TextName;
        private string[] _TextScore;
        private string[] _TextDate;
        private string[] _ParticleEffectNew;

        private List<SDBScoreEntry>[] _Scores;
        private List<int> _NewEntryIDs;
        private int _Round;
        private int _Pos;
        private bool _IsDuet;
        private bool _FromScreenSong = false;

        public override EMusicType CurrentMusicType
        {
            get { return EMusicType.BackgroundPreview; }
        }

        private int _HighscoreStream = -1;
        private bool _HasPlayedSound = false;
        
        private static int PlaySound(ESounds sound, int volume)
        {
            int streamId = CSound.PlaySound(sound, false);
            CSound.SetStreamVolume(streamId, volume);

            return streamId;
        }

        public override void Init()
        {
            base.Init();

            var texts = new List<string> {_TextSongName, _TextSongMode};

            _TextNumber = new string[_NumEntrys];
            for (int i = 0; i < _NumEntrys; i++)
            {
                _TextNumber[i] = "TextNumber" + (i + 1);
                texts.Add(_TextNumber[i]);
            }

            _TextName = new string[_NumEntrys];
            for (int i = 0; i < _NumEntrys; i++)
            {
                _TextName[i] = "TextName" + (i + 1);
                texts.Add(_TextName[i]);
            }

            _TextScore = new string[_NumEntrys];
            for (int i = 0; i < _NumEntrys; i++)
            {
                _TextScore[i] = "TextScore" + (i + 1);
                texts.Add(_TextScore[i]);
            }

            _TextDate = new string[_NumEntrys];
            for (int i = 0; i < _NumEntrys; i++)
            {
                _TextDate[i] = "TextDate" + (i + 1);
                texts.Add(_TextDate[i]);
            }

            _ParticleEffectNew = new string[_NumEntrys];
            for (int i = 0; i < _NumEntrys; i++)
                _ParticleEffectNew[i] = "ParticleEffectNew" + (i + 1);

            _ThemeTexts = texts.ToArray();
            _ThemeParticleEffects = _ParticleEffectNew;

            _NewEntryIDs = new List<int>();
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (keyEvent.KeyPressed && !Char.IsControl(keyEvent.Unicode)) {}
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                    case Keys.Enter:
                        _LeaveScreen();
                        break;

                    case Keys.Down:
                        if (!_FromScreenSong)
                            _ChangePos(1);
                        break;

                    case Keys.Up:
                        if (!_FromScreenSong)
                            _ChangePos(-1);
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
            if (mouseEvent.LB && _IsMouseOverCurSelection(mouseEvent)) {}

            if (mouseEvent.LB)
                _LeaveScreen();

            if (mouseEvent.RB)
                _LeaveScreen();

            if (mouseEvent.MB)
            {
                int lastRound = _Round;
                _ChangeRound(1);
                if (lastRound == _Round)
                {
                    _Round = 0;
                    _UpdateRound();
                }
            }

            _ChangePos(mouseEvent.Wheel);
            return true;
        }

        public override bool UpdateGame()
        {
            for (int p = 0; p < _NumEntrys; p++)
            {
                if (_Pos + p < _Scores[_Round].Count)
                {
                    _Texts[_TextNumber[p]].Visible = true;
                    _Texts[_TextName[p]].Visible = true;
                    _Texts[_TextScore[p]].Visible = true;
                    _Texts[_TextDate[p]].Visible = true;

                    _Texts[_TextNumber[p]].Text = (_Pos + p + 1).ToString();

                    string name = _Scores[_Round][_Pos + p].Name;
                    name += " [" + CLanguage.Translate(Enum.GetName(typeof(EGameDifficulty), _Scores[_Round][_Pos + p].Difficulty)) + "]";
                    if (_IsDuet)
                        name += " (P" + (_Scores[_Round][_Pos + p].VoiceNr + 1) + ")";
                    _Texts[_TextName[p]].Text = name;

                    _Texts[_TextScore[p]].Text = _Scores[_Round][_Pos + p].Score.ToString("D");
                    _Texts[_TextDate[p]].Text = _Scores[_Round][_Pos + p].Date;

                    _ParticleEffects[_ParticleEffectNew[p]].Visible = _IsNewEntry(_Scores[_Round][_Pos + p].ID);

                    if (_ParticleEffects[_ParticleEffectNew[p]].Visible && !_HasPlayedSound)
                    {
                         _HighscoreStream = CScreenHighscore.PlaySound(ESounds.Highscore, CConfig.GameMusicVolume);
                         _HasPlayedSound = true;
                    }
                  }
                else
                {
                    _Texts[_TextNumber[p]].Visible = false;
                    _Texts[_TextName[p]].Visible = false;
                    _Texts[_TextScore[p]].Visible = false;
                    _Texts[_TextDate[p]].Visible = false;
                    _ParticleEffects[_ParticleEffectNew[p]].Visible = false;
                }
            }
            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
            _HasPlayedSound = false;
            _Round = 0;
            _Pos = 0;
            _NewEntryIDs.Clear();
            _AddScoresToDB();
            _LoadScores();
            _UpdateRound();

            UpdateGame();
        }

        private bool _IsNewEntry(int id)
        {
            return _NewEntryIDs.Any(t => t == id);
        }

        private void _AddScoresToDB()
        {
            CPoints points = CGame.GetPoints();
            if (points == null)
                return;

            for (int round = 0; round < points.NumRounds; round++)
            {
                SPlayer[] players = points.GetPlayer(round, CGame.NumPlayers);

                for (int p = 0; p < players.Length; p++)
                {
                    if (players[p].Points > CSettings.MinScoreForDB && players[p].SongFinished && !CProfiles.IsGuestProfile(players[p].ProfileID))
                        _NewEntryIDs.Add(CDataBase.AddScore(players[p]));
                }
            }
        }

        private void _LoadScores()
        {
            _Pos = 0;
            int rounds = CGame.NumRounds;

            if (rounds == 0)
            {
                _FromScreenSong = true;
                _Round = (int)EGameMode.TR_GAMEMODE_NORMAL;
                _Scores = new List<SDBScoreEntry>[4];
                int songID = CScreenSong.getSelectedSongID();
                EHighscoreStyle style = CBase.Config.GetHighscoreStyle();
                bool foundHighscoreEntries = false;

                for (int gameModeNum = 0; gameModeNum < 4; gameModeNum++)
                {
                    _Scores[gameModeNum] = CDataBase.LoadScore(songID, (EGameMode)gameModeNum, style);
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
                for (int round = 0; round < rounds; round++)
                {
                    int songID = CGame.GetSong(round).ID;
                    EGameMode gameMode = CGame.GetGameMode(round);
                    EHighscoreStyle style = CBase.Config.GetHighscoreStyle();
                    _Scores[round] = CDataBase.LoadScore(songID, gameMode, style);
                }
            }
        }

        private void _UpdateRound()
        {
            _IsDuet = false;
            CPoints points = CGame.GetPoints();
            
            CSong song;
            if (_FromScreenSong)
                song = CSongs.GetSong(CScreenSong.getSelectedSongID());
            else
                song = CGame.GetSong(_Round);

            if (song == null)
                return;

            _Texts[_TextSongName].Text = song.Artist + " - " + song.Title;
            if (points != null && !_FromScreenSong && points.NumRounds > 1)
                _Texts[_TextSongName].Text += " (" + (_Round + 1) + "/" + points.NumRounds + ")";

            switch ((_FromScreenSong ? (EGameMode)_Round : CGame.GetGameMode(_Round)))
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

            _Pos = 0;
        }

        private void _ChangePos(int num)
        {
            if (_Scores[_Round].Count == 0 || _Scores[_Round].Count <= _NumEntrys)
                _Pos = 0;
            else
            {
                _Pos += num;
                _Pos = _Pos.Clamp(0, _Scores[_Round].Count - 1, true);
            }
        }

        private void _ChangeRound(int num)
        {
            if (_FromScreenSong)
            {
                if (_Round == (int)EGameMode.TR_GAMEMODE_SHORTSONG)
                    _Round = (int)EGameMode.TR_GAMEMODE_NORMAL;
                else
                    ++_Round;
            }
            else
            {
                CPoints points = CGame.GetPoints();
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
