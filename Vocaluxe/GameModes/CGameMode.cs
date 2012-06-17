using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Base;
using Vocaluxe.Lib.Song;

namespace Vocaluxe.GameModes
{
    struct SongQueque
    {
        public int SongID;
        public EGameMode GameMode;

        public SongQueque(int songID, EGameMode gameMode)
        {
            SongID = songID;
            GameMode = gameMode;
        }
    }

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
        protected List<SongQueque> _SongQueque;
        protected int _CurrentSong;
        protected CPoints _Points;

        #region Implementation
        public virtual void Init()
        {
            _SongQueque = new List<SongQueque>();
            Reset();
        }

        public virtual EGameMode GetCurrentGameMode()
        {
            if (_CurrentSong >= 0 && _CurrentSong < _SongQueque.Count)
            {
                return _SongQueque[_CurrentSong].GameMode;
            }
            return EGameMode.TR_GAMEMODE_NORMAL;
        }

        public virtual bool AddVisibleSong(int VisibleIndex, EGameMode GameMode)
        {
            if (CSongs.VisibleSongs.Length > VisibleIndex)
            {
                int SongID = CSongs.VisibleSongs[VisibleIndex].ID;
                if (GameMode == EGameMode.TR_GAMEMODE_DUET && !CSongs.GetSong(SongID).IsDuet)
                    return false;

                _SongQueque.Add(new SongQueque(SongID, GameMode));
                return true;
            }
            return false;
        }

        public virtual bool AddSong(int AbsoluteIndex, EGameMode GameMode)
        {
            if (CSongs.AllSongs.Length > AbsoluteIndex)
            {
                int SongID = CSongs.AllSongs[AbsoluteIndex].ID;
                if (GameMode == EGameMode.TR_GAMEMODE_DUET && !CSongs.GetSong(SongID).IsDuet)
                    return false;

                _SongQueque.Add(new SongQueque(SongID, GameMode));
                return true;
            }
            return false;
        }

        public virtual bool RemoveVisibleSong(int VisibleIndex)
        {
            if (CSongs.VisibleSongs.Length > VisibleIndex)
            {
                int index = -1;
                int SongID = CSongs.VisibleSongs[VisibleIndex].ID;
                for (int i = 0; i < _SongQueque.Count; i++)
                {
                    if (_SongQueque[i].SongID == SongID)
                    {
                        index = i;
                        break;
                    }
                }

                if (index == -1)
                    return false;

                _SongQueque.RemoveAt(index);
                return true;
            }
            return false;
        }

        public virtual bool RemoveSong(int AbsoluteIndex)
        {
            if (CSongs.AllSongs.Length > AbsoluteIndex)
            {
                int index = -1;
                int SongID = CSongs.AllSongs[AbsoluteIndex].ID;
                for (int i = 0; i < _SongQueque.Count; i++)
                {
                    if (_SongQueque[i].SongID == SongID)
                    {
                        index = i;
                        break;
                    }
                }
                if (index == -1)
                    return false;

                _SongQueque.RemoveAt(index);
                return true;
            }
            return false;
        }

        public virtual void ClearSongs()
        {
            _SongQueque.Clear();
        }

        public virtual void Reset()
        {
            _CurrentSong = -1;
        }

        public virtual void Start(SPlayer[] Player)
        {
            _Points = new CPoints(_SongQueque.Count, Player);
        }

        public virtual void NextRound(SPlayer[] Player)
        {
            if (_CurrentSong < _SongQueque.Count && _SongQueque.Count > 0)
            {
                if (_CurrentSong > -1)
                {
                    _Points.SetPoints(
                        _CurrentSong,
                        _SongQueque[_CurrentSong].SongID,
                        Player,
                        _SongQueque[_CurrentSong].GameMode == EGameMode.TR_GAMEMODE_MEDLEY,
                        _SongQueque[_CurrentSong].GameMode == EGameMode.TR_GAMEMODE_DUET);
                }
                _CurrentSong++;
            }
        }

        public virtual bool IsFinished()
        {
            return (_CurrentSong == _SongQueque.Count || _SongQueque.Count == 0);
        }

        public virtual int GetCurrentRoundNr()
        {
            return _CurrentSong + 1;
        }

        public virtual CSong GetSong()
        {
            if (_CurrentSong >= 0 && _CurrentSong < _SongQueque.Count)
            {
                CSong song = CSongs.GetSong(_SongQueque[_CurrentSong].SongID);

                switch (GetCurrentGameMode())
                {
                    case EGameMode.TR_GAMEMODE_MEDLEY:
                        // set medley mode timings
                        song.Start = CGame.GetTimeFromBeats(song.Medley.StartBeat, song.BPM) - song.Medley.FadeInTime + song.Gap;
                        if (song.Start < 0f)
                            song.Start = 0f;

                        song.Finish = CGame.GetTimeFromBeats(song.Medley.EndBeat, song.BPM) + song.Medley.FadeOutTime + song.Gap;

                        // set lines to medley mode
                        song.Notes.SetMedley(song.Medley.StartBeat, song.Medley.EndBeat);
                        break;

                    case EGameMode.TR_GAMEMODE_SHORTSONG:
                        song.Finish = CGame.GetTimeFromBeats(song.ShortEnd, song.BPM) + CSettings.DefaultMedleyFadeOutTime + song.Gap;

                        // set lines to medley mode
                        song.Notes.SetMedley(song.Notes.GetLines(0).Line[0].FirstNoteBeat, song.ShortEnd);
                        break;
                }

                return song;
            }
            return null;
        }

        public virtual int GetNumSongs()
        {
            return _SongQueque.Count;
        }

        public virtual CSong GetSong(int Num)
        {
            if (Num - 1 < _SongQueque.Count)
                return CSongs.GetSong(_SongQueque[Num - 1].SongID);

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
