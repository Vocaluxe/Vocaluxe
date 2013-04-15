using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib.Menu;
using VocaluxeLib.Menu.SongMenu;

namespace Vocaluxe.Screens
{
    class CScreenHighscore : CMenu
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

        private List<SScores>[] _Scores;
        private List<int> _NewEntryIDs;
        private int _Round;
        private int _Pos;
        private bool _IsDuet;

        public override void Init()
        {
            base.Init();

            List<string> texts = new List<string>();
            texts.Add(_TextSongName);
            texts.Add(_TextSongMode);

            _TextNumber = new string[_NumEntrys];
            for (int i = 0; i < _NumEntrys; i++)
            {
                _TextNumber[i] = "TextNumber" + (i + 1).ToString();
                texts.Add(_TextNumber[i]);
            }

            _TextName = new string[_NumEntrys];
            for (int i = 0; i < _NumEntrys; i++)
            {
                _TextName[i] = "TextName" + (i + 1).ToString();
                texts.Add(_TextName[i]);
            }

            _TextScore = new string[_NumEntrys];
            for (int i = 0; i < _NumEntrys; i++)
            {
                _TextScore[i] = "TextScore" + (i + 1).ToString();
                texts.Add(_TextScore[i]);
            }

            _TextDate = new string[_NumEntrys];
            for (int i = 0; i < _NumEntrys; i++)
            {
                _TextDate[i] = "TextDate" + (i + 1).ToString();
                texts.Add(_TextDate[i]);
            }

            _ParticleEffectNew = new string[_NumEntrys];
            for (int i = 0; i < _NumEntrys; i++)
                _ParticleEffectNew[i] = "ParticleEffectNew" + (i + 1).ToString();

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
                        _ChangePos(1);
                        break;

                    case Keys.Up:
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
            if (mouseEvent.LB && IsMouseOver(mouseEvent)) {}

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
                    Texts[_TextNumber[p]].Visible = true;
                    Texts[_TextName[p]].Visible = true;
                    Texts[_TextScore[p]].Visible = true;
                    Texts[_TextDate[p]].Visible = true;

                    Texts[_TextNumber[p]].Text = (_Pos + p + 1).ToString();

                    string name = _Scores[_Round][_Pos + p].Name;
                    name += " [" + CLanguage.Translate(Enum.GetName(typeof(EGameDifficulty), _Scores[_Round][_Pos + p].Difficulty)) + "]";
                    if (_IsDuet)
                        name += " (P" + (_Scores[_Round][_Pos + p].LineNr + 1).ToString() + ")";
                    Texts[_TextName[p]].Text = name;

                    Texts[_TextScore[p]].Text = _Scores[_Round][_Pos + p].Score.ToString("00000");
                    Texts[_TextDate[p]].Text = _Scores[_Round][_Pos + p].Date;

                    if (_IsNewEntry(_Scores[_Round][_Pos + p].ID))
                        ParticleEffects[_ParticleEffectNew[p]].Visible = true;
                    else
                        ParticleEffects[_ParticleEffectNew[p]].Visible = false;
                }
                else
                {
                    Texts[_TextNumber[p]].Visible = false;
                    Texts[_TextName[p]].Visible = false;
                    Texts[_TextScore[p]].Visible = false;
                    Texts[_TextDate[p]].Visible = false;
                    ParticleEffects[_ParticleEffectNew[p]].Visible = false;
                }
            }
            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
            _Round = 0;
            _Pos = 0;
            CPoints points = CGame.GetPoints();
            _Scores = new List<SScores>[points.NumRounds];
            for (int i = 0; i < _Scores.Length; i++)
                _Scores[i] = new List<SScores>();
            _NewEntryIDs.Clear();
            _AddScoresToDB();
            _LoadScores();
            _UpdateRound();

            UpdateGame();
        }

        private bool _IsNewEntry(int id)
        {
            for (int i = 0; i < _NewEntryIDs.Count; i++)
            {
                if (_NewEntryIDs[i] == id)
                    return true;
            }

            return false;
        }

        private void _AddScoresToDB()
        {
            CPoints points = CGame.GetPoints();
            if (points == null)
                return;

            for (int round = 0; round < points.NumRounds; round++)
            {
                SPlayer[] player = points.GetPlayer(round, CGame.NumPlayer);

                for (int p = 0; p < player.Length; p++)
                {
                    if (player[p].Points > CSettings.MinScoreForDB && player[p].SongFinished && !CProfiles.IsGuestProfile(player[p].ProfileID))
                        _NewEntryIDs.Add(CDataBase.AddScore(player[p]));
                }
            }
        }

        private void _LoadScores()
        {
            CPoints points = CGame.GetPoints();
            if (points == null)
                return;

            _Pos = 0;
            for (int round = 0; round < points.NumRounds; round++)
            {
                SPlayer player = points.GetPlayer(round, CGame.NumPlayer)[0];
                CDataBase.LoadScore(ref _Scores[round], player);
            }
        }

        private void _UpdateRound()
        {
            _IsDuet = false;
            CPoints points = CGame.GetPoints();
            SPlayer player = points.GetPlayer(_Round, CGame.NumPlayer)[0];
            CSong song = CGame.GetSong(_Round + 1);
            if (song == null)
                return;

            Texts[_TextSongName].Text = song.Artist + " - " + song.Title;
            if (points.NumRounds > 1)
                Texts[_TextSongName].Text += " (" + (_Round + 1) + "/" + points.NumRounds + ")";

            switch (CGame.GetGameMode(_Round))
            {
                case EGameMode.TR_GAMEMODE_NORMAL:
                    Texts[_TextSongMode].Text = "TR_GAMEMODE_NORMAL";
                    break;

                case EGameMode.TR_GAMEMODE_MEDLEY:
                    Texts[_TextSongMode].Text = "TR_GAMEMODE_MEDLEY";
                    break;

                case EGameMode.TR_GAMEMODE_DUET:
                    Texts[_TextSongMode].Text = "TR_GAMEMODE_DUET";
                    _IsDuet = true;
                    break;

                case EGameMode.TR_GAMEMODE_SHORTSONG:
                    Texts[_TextSongMode].Text = "TR_GAMEMODE_SHORTSONG";
                    break;

                default:
                    Texts[_TextSongMode].Text = "TR_GAMEMODE_NORMAL";
                    break;
            }

            _Pos = 0;
        }

        private void _ChangePos(int num)
        {
            if (num > 0)
            {
                if (_Pos + num + _NumEntrys < _Scores[_Round].Count)
                    _Pos += num;
            }
            if (num < 0)
            {
                _Pos += num;
                if (_Pos < 0)
                    _Pos = 0;
            }
        }

        private void _ChangeRound(int num)
        {
            CPoints points = CGame.GetPoints();
            if (_Round + num < points.NumRounds && _Round + num > -1)
                _Round += num;
            else if (_Round + num >= points.NumRounds)
                _Round = points.NumRounds - 1;
            else if (_Round + num < 0)
                _Round = 0;
            _UpdateRound();
        }

        private void _LeaveScreen()
        {
            CParty.LeavingHighscore();
        }
    }
}