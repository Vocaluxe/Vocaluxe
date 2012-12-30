using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Base;
using Vocaluxe.Menu;
using Vocaluxe.Menu.SongMenu;

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
                        _SongQueque[_CurrentSong].GameMode == EGameMode.TR_GAMEMODE_DUET,
                        _SongQueque[_CurrentSong].GameMode == EGameMode.TR_GAMEMODE_SHORTSONG);
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
                song = new CSong(song);

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
            if (Num - 1 < _SongQueque.Count && Num - 1 > -1)
                return CSongs.GetSong(_SongQueque[Num - 1].SongID);

            return null;
        }

        public virtual EGameMode GetGameMode(int Num)
        {
            if(Num - 1 < _SongQueque.Count && Num > -1)
                return _SongQueque[Num].GameMode;

            return EGameMode.TR_GAMEMODE_NORMAL;
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
