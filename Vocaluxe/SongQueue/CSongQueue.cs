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
using VocaluxeLib;
using VocaluxeLib.Game;
using VocaluxeLib.Songs;

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
        private List<SSongQueueEntry> _SongQueue;
        private int _CurrentSong;
        private CPoints _Points;

        #region Implementation
        public void Init()
        {
            _SongQueue = new List<SSongQueueEntry>();
            Reset();
            CGameModes.Init();
        }

        public EGameMode GetCurrentGameMode()
        {
            if (_CurrentSong >= 0 && _CurrentSong < _SongQueue.Count)
                return _SongQueue[_CurrentSong].GameMode;
            return EGameMode.TR_GAMEMODE_NORMAL;
        }

        public bool AddVisibleSong(int visibleIndex, EGameMode gameMode)
        {
            return CSongs.VisibleSongs.Count > visibleIndex && _AddSong(CSongs.VisibleSongs[visibleIndex].ID, gameMode);
        }

        public bool AddSong(int absoluteIndex, EGameMode gameMode)
        {
            return CSongs.AllSongs.Count > absoluteIndex && _AddSong(CSongs.AllSongs[absoluteIndex].ID, gameMode);
        }

        private bool _AddSong(int songID, EGameMode gameMode)
        {
            if (gameMode == EGameMode.TR_GAMEMODE_DUET && !CSongs.GetSong(songID).IsDuet)
                return false;

            _SongQueue.Add(new SSongQueueEntry(songID, gameMode));
            return true;
        }

        public bool RemoveVisibleSong(int visibleIndex)
        {
            return CSongs.VisibleSongs.Count > visibleIndex && _RemoveSong(CSongs.VisibleSongs[visibleIndex].ID);
        }

        public bool RemoveSong(int absoluteIndex)
        {
            return CSongs.AllSongs.Count > absoluteIndex && _RemoveSong(CSongs.AllSongs[absoluteIndex].ID);
        }

        private bool _RemoveSong(int songID)
        {
            for (int i = 0; i < _SongQueue.Count; i++)
            {
                if (_SongQueue[i].SongID != songID)
                    continue;
                _SongQueue.RemoveAt(i);
                return true;
            }
            return false;
        }

        public void ClearSongs()
        {
            _SongQueue.Clear();
        }

        public void Reset()
        {
            _CurrentSong = -1;
        }

        public void Start(SPlayer[] players)
        {
            _Points = new CPoints(_SongQueue.Count, players);
        }

        public void NextRound(SPlayer[] players)
        {
            if (_CurrentSong < _SongQueue.Count && _SongQueue.Count > 0)
            {
                if (_CurrentSong > -1)
                {
                    _Points.SetPoints(
                        _CurrentSong,
                        _SongQueue[_CurrentSong].SongID,
                        players,
                        _SongQueue[_CurrentSong].GameMode == EGameMode.TR_GAMEMODE_MEDLEY,
                        _SongQueue[_CurrentSong].GameMode == EGameMode.TR_GAMEMODE_DUET,
                        _SongQueue[_CurrentSong].GameMode == EGameMode.TR_GAMEMODE_SHORTSONG);
                }
                _CurrentSong++;
            }
        }

        public bool IsFinished()
        {
            return _CurrentSong == _SongQueue.Count || _SongQueue.Count == 0;
        }

        public int GetCurrentRoundNr()
        {
            return _CurrentSong + 1;
        }

        public CSong GetSong()
        {
            if (_CurrentSong >= 0 && _CurrentSong < _SongQueue.Count)
                return CGameModes.Get(_SongQueue[_CurrentSong].GameMode).GetSong(_SongQueue[_CurrentSong].SongID);
            return null;
        }

        public int GetNumSongs()
        {
            return _SongQueue.Count;
        }

        public CSong GetSong(int num)
        {
            if (num - 1 < _SongQueue.Count && num > 0)
                return CSongs.GetSong(_SongQueue[num - 1].SongID);

            return null;
        }

        public EGameMode GetGameMode(int num)
        {
            if (num - 1 < _SongQueue.Count && num > 0)
                return _SongQueue[num - 1].GameMode;

            return EGameMode.TR_GAMEMODE_NORMAL;
        }

        public CPoints GetPoints()
        {
            return _Points;
        }
        #endregion Implementation
    }
}