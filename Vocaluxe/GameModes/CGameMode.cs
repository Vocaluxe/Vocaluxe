using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Base;
using Vocaluxe.Lib.Song;

namespace Vocaluxe.GameModes
{
    class CPoints
    {
        private SPlayer[,] _Rounds;

        public CPoints(int NumRounds, SPlayer[] Player)
        {
            _Rounds = new SPlayer[NumRounds, Player.Length];

            for (int round = 0; round < NumRounds; round++)
            {
                for (int player = 0; player < Player.Length; player++)
                {
                    _Rounds[round, player].ProfileID = Player[player].ProfileID;
                    _Rounds[round, player].Name = Player[player].Name;
                    _Rounds[round, player].Difficulty = Player[player].Difficulty;
                    _Rounds[round, player].Points = 0f;
                    _Rounds[round, player].PointsGoldenNotes = 0f;
                    _Rounds[round, player].PointsLineBonus = 0f;
                    _Rounds[round, player].Medley = false;
                    _Rounds[round, player].Duet = false;
                    _Rounds[round, player].SongFinished = false;
                }
            }
        }

        public void SetPoints(int Round, int SongID, SPlayer[] Player, bool Medley, bool Duet)
        {
            long DateTicks = DateTime.Now.Ticks;
            for (int player = 0; player < Player.Length; player++)
            {
                _Rounds[Round, player].SongID = SongID;
                _Rounds[Round, player].LineNr = Player[player].LineNr;
                _Rounds[Round, player].Points = Player[player].Points;
                _Rounds[Round, player].PointsGoldenNotes = Player[player].PointsGoldenNotes;
                _Rounds[Round, player].PointsLineBonus = Player[player].PointsLineBonus;
                _Rounds[Round, player].Medley = Medley;
                _Rounds[Round, player].Duet = Duet;
                _Rounds[Round, player].DateTicks = DateTicks;
                _Rounds[Round, player].SongFinished = Player[player].SongFinished;
            }
        }

        public int NumRounds
        {
            get { return _Rounds.GetLength(0); }
        }

        public int NumPlayer
        {
            get { return _Rounds.GetLength(1); }
        }

        public SPlayer[] GetPlayer(int Round, int numPlayer)
        {
            if (NumPlayer == 0)
                return new SPlayer[1];
            if (Round >= NumRounds)
                return new SPlayer[1];

            SPlayer[] player = new SPlayer[numPlayer];

            for (int p = 0; p < player.Length; p++)
			{
			    player[p].Name = _Rounds[Round, p].Name;
                player[p].Points = _Rounds[Round, p].Points;
                player[p].PointsGoldenNotes = _Rounds[Round, p].PointsGoldenNotes;
                player[p].PointsLineBonus = _Rounds[Round, p].PointsLineBonus;
                player[p].SongID = _Rounds[Round, p].SongID;
                player[p].LineNr = _Rounds[Round, p].LineNr;
                player[p].Difficulty = _Rounds[Round, p].Difficulty;
                player[p].Medley = _Rounds[Round, p].Medley;
                player[p].Duet = _Rounds[Round, p].Duet;
                player[p].DateTicks = _Rounds[Round, p].DateTicks;
                player[p].SongFinished = _Rounds[Round, p].SongFinished;
                player[p].ProfileID = _Rounds[Round, p].ProfileID;
			}
            return player;
        }
    }

    abstract class CGameMode : IGameMode
    {
        protected bool _Initialized = false;
        protected EGameMode _GameMode;
        protected List<CSong> _Songs;
        protected int _ActualSong;
        protected CPoints _Points;

        #region Implementation
        public virtual void Init()
        {
            _Songs = new List<CSong>();
            Reset();
        }

        public virtual EGameMode GetGameMode()
        {
            return _GameMode;
        }

        public virtual bool AddVisibleSong(int VisibleIndex)
        {
            if (CSongs.VisibleSongs.Length > VisibleIndex)
            {
                _Songs.Add(new CSong(CSongs.VisibleSongs[VisibleIndex]));
                SongManipulation(_Songs.Count - 1);
                return true;
            }
            return false;
        }

        public virtual bool AddSong(int AbsoluteIndex)
        {
            if (CSongs.AllSongs.Length > AbsoluteIndex)
            {
                _Songs.Add(CSongs.AllSongs[AbsoluteIndex]);
                SongManipulation(_Songs.Count - 1);
                return true;
            }
            return false;
        }

        public virtual bool RemoveVisibleSong(int VisibleIndex)
        {
            if (CSongs.VisibleSongs.Length > VisibleIndex)
            {
                int ID = CSongs.VisibleSongs[VisibleIndex].ID;
                int index = -1;

                for (int i = 0; i < _Songs.Count; i++)
                {
                    if (_Songs[i].ID == ID)
                    {
                        index = i;
                        break;
                    }
                }

                if (index > -1)
                {
                    _Songs.RemoveAt(index);
                    return true;
                }
                return false;
            }
            return false;
        }

        public virtual bool RemoveSong(int AbsoluteIndex)
        {
            if (CSongs.AllSongs.Length > AbsoluteIndex)
            {
                int ID = CSongs.AllSongs[AbsoluteIndex].ID;
                int index = -1;

                for (int i = 0; i < _Songs.Count; i++)
                {
                    if (_Songs[i].ID == ID)
                    {
                        index = i;
                        break;
                    }
                }

                if (index > -1)
                {
                    _Songs.RemoveAt(index);
                    return true;
                }
                return false;
            }
            return false;
        }

        public virtual void ClearSongs()
        {
            _Songs.Clear();
        }

        public virtual void Reset()
        {
            _ActualSong = -1;
        }

        public virtual void Start(SPlayer[] Player)
        {
            _Points = new CPoints(_Songs.Count, Player);
        }

        public virtual void NextRound(SPlayer[] Player)
        {
            if (_ActualSong < _Songs.Count && _Songs.Count > 0)
            {
                if (_ActualSong > -1)
                {
                    _Points.SetPoints(_ActualSong, _Songs[_ActualSong].ID, Player, _GameMode == EGameMode.Medley, _GameMode == EGameMode.Duet);
                }
                _ActualSong++;
            }
        }

        public virtual bool IsFinished()
        {
            return (_ActualSong == _Songs.Count || _Songs.Count == 0);
        }

        public virtual int GetActualRoundNr()
        {
            return _ActualSong + 1;
        }

        public virtual CSong GetSong()
        {
            if (_ActualSong >= 0 && _ActualSong < _Songs.Count)
            {
                return _Songs[_ActualSong];
            }
            return null;
        }

        public virtual int GetNumSongs()
        {
            return _Songs.Count;
        }

        public virtual CSong GetSong(int Num)
        {
            if (Num - 1 < _Songs.Count) 
                return _Songs[Num - 1];

            return null;
        }

        public virtual CPoints GetPoints()
        {
            return _Points;
        }
        #endregion Implementation

        protected virtual void SongManipulation(int SongIndex)
        {
        }
    }
}
