#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using System.Collections.Generic;
using Vocaluxe.Base;
using Vocaluxe.GameModes;
using VocaluxeLib.Menu;
using VocaluxeLib.Menu.SongMenu;

namespace Vocaluxe.SongQueue
{
    struct SSongQueueEntry
    {
        public readonly int SongID;
        public readonly EGameMode GameMode;

        public SSongQueueEntry(int songID, EGameMode gameMode)
        {
            SongID = songID;
            GameMode = gameMode;
        }
    }

    class CSongQueue : ISongQueue
    {
        private List<SSongQueueEntry> _SongQueque;
        private int _CurrentSong;
        private CPoints _Points;

        #region Implementation
        public void Init()
        {
            _SongQueque = new List<SSongQueueEntry>();
            Reset();
            CGameModes.Init();
        }

        public EGameMode GetCurrentGameMode()
        {
            if (_CurrentSong >= 0 && _CurrentSong < _SongQueque.Count)
                return _SongQueque[_CurrentSong].GameMode;
            return EGameMode.TR_GAMEMODE_NORMAL;
        }

        public bool AddVisibleSong(int visibleIndex, EGameMode gameMode)
        {
            return CSongs.VisibleSongs.Length > visibleIndex && _AddSong(CSongs.VisibleSongs[visibleIndex].ID, gameMode);
        }

        public bool AddSong(int absoluteIndex, EGameMode gameMode)
        {
            return CSongs.AllSongs.Length > absoluteIndex && _AddSong(CSongs.AllSongs[absoluteIndex].ID, gameMode);
        }

        private bool _AddSong(int songID, EGameMode gameMode)
        {
            if (gameMode == EGameMode.TR_GAMEMODE_DUET && !CSongs.GetSong(songID).IsDuet)
                return false;

            _SongQueque.Add(new SSongQueueEntry(songID, gameMode));
            return true;
        }

        public bool RemoveVisibleSong(int visibleIndex)
        {
            return CSongs.VisibleSongs.Length > visibleIndex && _RemoveSong(CSongs.VisibleSongs[visibleIndex].ID);
        }

        public bool RemoveSong(int absoluteIndex)
        {
            return CSongs.AllSongs.Length > absoluteIndex && _RemoveSong(CSongs.AllSongs[absoluteIndex].ID);
        }

        private bool _RemoveSong(int songID)
        {
            for (int i = 0; i < _SongQueque.Count; i++)
            {
                if (_SongQueque[i].SongID != songID)
                    continue;
                _SongQueque.RemoveAt(i);
                return true;
            }
            return false;
        }

        public void ClearSongs()
        {
            _SongQueque.Clear();
        }

        public void Reset()
        {
            _CurrentSong = -1;
        }

        public void Start(SPlayer[] players)
        {
            _Points = new CPoints(_SongQueque.Count, players);
        }

        public void NextRound(SPlayer[] players)
        {
            if (_CurrentSong < _SongQueque.Count && _SongQueque.Count > 0)
            {
                if (_CurrentSong > -1)
                {
                    _Points.SetPoints(
                        _CurrentSong,
                        _SongQueque[_CurrentSong].SongID,
                        players,
                        _SongQueque[_CurrentSong].GameMode == EGameMode.TR_GAMEMODE_MEDLEY,
                        _SongQueque[_CurrentSong].GameMode == EGameMode.TR_GAMEMODE_DUET,
                        _SongQueque[_CurrentSong].GameMode == EGameMode.TR_GAMEMODE_SHORTSONG);
                }
                _CurrentSong++;
            }
        }

        public bool IsFinished()
        {
            return _CurrentSong == _SongQueque.Count || _SongQueque.Count == 0;
        }

        public int GetCurrentRoundNr()
        {
            return _CurrentSong + 1;
        }

        public CSong GetSong()
        {
            if (_CurrentSong >= 0 && _CurrentSong < _SongQueque.Count)
                CGameModes.Get(GetCurrentGameMode()).GetSong(_SongQueque[_CurrentSong].SongID);
            return null;
        }

        public int GetNumSongs()
        {
            return _SongQueque.Count;
        }

        public CSong GetSong(int num)
        {
            if (num - 1 < _SongQueque.Count && num > 0)
                return CSongs.GetSong(_SongQueque[num - 1].SongID);

            return null;
        }

        public EGameMode GetGameMode(int num)
        {
            if (num - 1 < _SongQueque.Count && num > 0)
                return _SongQueque[num - 1].GameMode;

            return EGameMode.TR_GAMEMODE_NORMAL;
        }

        public CPoints GetPoints()
        {
            return _Points;
        }
        #endregion Implementation
    }
}