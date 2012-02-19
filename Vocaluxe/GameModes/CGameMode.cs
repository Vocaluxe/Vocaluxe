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
                    _Rounds[round, player].Points = 0;
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
        protected List<int> _SongIDs;
        protected int _ActualSong;
        protected CPoints _Points;

        #region Implementation
        public virtual void Init()
        {
            _SongIDs = new List<int>();
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
                //int ID = CSongs.VisibleSongs[VisibleIndex].ID;
                //if (!_SongIDs.Exists(delegate(int i) { return (i == ID); }))
                //{
                    _SongIDs.Add(CSongs.VisibleSongs[VisibleIndex].ID);
                    return true;
                //}
            }
            return false;
        }

        public virtual bool AddSong(int AbsoluteIndex)
        {
            if (CSongs.AllSongs.Length > AbsoluteIndex)
            {
                //int ID = CSongs.AllSongs[AbsoluteIndex].ID;
                //if (!_SongIDs.Exists(delegate(int i) { return (i == ID); }))
                //{
                _SongIDs.Add(CSongs.AllSongs[AbsoluteIndex].ID);
                return true;
                //}
            }
            return false;
        }

        public virtual bool RemoveVisibleSong(int VisibleIndex)
        {
            if (CSongs.VisibleSongs.Length > VisibleIndex)
            {
                return _SongIDs.Remove(CSongs.VisibleSongs[VisibleIndex].ID);
            }
            return false;
        }

        public virtual bool RemoveSong(int AbsoluteIndex)
        {
            if (CSongs.AllSongs.Length > AbsoluteIndex)
            {
                return _SongIDs.Remove(CSongs.AllSongs[AbsoluteIndex].ID);
            }
            return false;
        }

        public virtual void ClearSongs()
        {
            _SongIDs.Clear();
        }

        public virtual void Reset()
        {
            _ActualSong = -1;
        }

        public virtual void Start(SPlayer[] Player)
        {
            _Points = new CPoints(_SongIDs.Count, Player);
        }

        public virtual void NextRound(SPlayer[] Player)
        {
            if (_ActualSong < _SongIDs.Count && _SongIDs.Count > 0)
            {
                if (_ActualSong > -1)
                {
                    _Points.SetPoints(_ActualSong, _SongIDs[_ActualSong], Player, _GameMode == EGameMode.Medley, _GameMode == EGameMode.Duet);
                }
                _ActualSong++;
            }
        }

        public virtual bool IsFinished()
        {
            return (_ActualSong == _SongIDs.Count || _SongIDs.Count == 0);
        }

        public virtual int GetActualRoundNr()
        {
            return _ActualSong + 1;
        }

        public virtual CSong GetSong()
        {
            if (_ActualSong >= 0 && _ActualSong < _SongIDs.Count)
            {
                return CSongs.GetSong(_SongIDs[_ActualSong]);
            }
            return null;
        }

        public virtual int GetNumSongs()
        {
            return _SongIDs.Count;
        }

        public virtual CSong GetSong(int Num)
        {
            if (Num - 1 < _SongIDs.Count) 
                return CSongs.GetSong(_SongIDs[Num - 1]);

            return null;
        }

        public virtual CPoints GetPoints()
        {
            return _Points;
        }
        #endregion Implementation
    }
}
