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
        public readonly int SongId;
        public readonly EGameMode GameMode;

        public SSongQueueEntry(int songId, EGameMode gameMode)
        {
            SongId = songId;
            GameMode = gameMode;
        }
    }

    class CSongQueue : ISongQueue
    {
        private List<SSongQueueEntry> _SongQueue;
        private int _CurrentRound;
        private CPoints _Points;
        private CSong _CurrentSong;

        #region Implementation
        public void Init()
        {
            _SongQueue = new List<SSongQueueEntry>();
            Reset();
            CGameModes.Init();
        }

        public EGameMode GetCurrentGameMode()
        {
            return GetGameMode(_CurrentRound);
        }

        public bool AddVisibleSong(int visibleIndex, EGameMode gameMode)
        {
            return CSongs.VisibleSongs.Count > visibleIndex && _AddSong(CSongs.VisibleSongs[visibleIndex].Id, gameMode);
        }

        public bool AddSong(int absoluteIndex, EGameMode gameMode)
        {
            return CSongs.AllSongs.Count > absoluteIndex && _AddSong(CSongs.AllSongs[absoluteIndex].Id, gameMode);
        }

        private bool _AddSong(int songId, EGameMode gameMode)
        {
            if (!CSongs.GetSong(songId).IsGameModeAvailable(gameMode))
            {
                return false;
            }

            _SongQueue.Add(new SSongQueueEntry(songId, gameMode));
            return true;
        }

        public bool RemoveVisibleSong(int visibleIndex)
        {
            return CSongs.VisibleSongs.Count > visibleIndex && _RemoveSong(CSongs.VisibleSongs[visibleIndex].Id);
        }

        public bool RemoveSong(int absoluteIndex)
        {
            return CSongs.AllSongs.Count > absoluteIndex && _RemoveSong(CSongs.AllSongs[absoluteIndex].Id);
        }

        private bool _RemoveSong(int songId)
        {
            for (var i = 0; i < _SongQueue.Count; i++)
            {
                if (_SongQueue[i].SongId != songId)
                {
                    continue;
                }

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
            _CurrentRound = -1;
        }

        public void Start(SPlayer[] players)
        {
            _Points = new CPoints(_SongQueue.Count, players);
        }

        public void StartNextRound(SPlayer[] players)
        {
            if (IsFinished())
            {
                return;
            }

            if (_CurrentRound > -1)
            {
                _Points.SetPoints(
                    _CurrentRound,
                    _SongQueue[_CurrentRound].SongId,
                    players,
                    _SongQueue[_CurrentRound].GameMode);
            }

            _CurrentRound++;
            _CurrentSong = IsFinished() ? null : CGameModes.Get(GetCurrentGameMode()).GetSong(_SongQueue[_CurrentRound].SongId);
        }

        public bool IsFinished()
        {
            return _CurrentRound >= _SongQueue.Count || _SongQueue.Count == 0;
        }

        /// <summary>
        ///     Get current round nr (1 ~ n)
        /// </summary>
        /// <returns>current round nr (1 ~ n)</returns>
        public int GetCurrentRoundNr()
        {
            return _CurrentRound + 1;
        }

        /// <summary>
        ///     Returns current round
        /// </summary>
        /// <returns>Current round (0 based)</returns>
        public int GetCurrentRound()
        {
            return _CurrentRound;
        }

        /// <summary>
        ///     Get current song
        /// </summary>
        /// <returns>Song of current round or null if there is none/game finished</returns>
        public CSong GetSong()
        {
            return _CurrentSong;
        }

        public int GetNumSongs()
        {
            return _SongQueue.Count;
        }

        /// <summary>
        ///     Get song of specified round
        /// </summary>
        /// <param name="round">Round (0 based)</param>
        /// <returns>Current song or null if out of bounds</returns>
        public CSong GetSong(int round)
        {
            if (round == _CurrentRound)
            {
                return _CurrentSong;
            }

            if (round < _SongQueue.Count && round >= 0)
            {
                return CSongs.GetSong(_SongQueue[round].SongId);
            }

            return null;
        }

        /// <summary>
        ///     Get gameMode of specified round
        /// </summary>
        /// <param name="round">Round (0 based)</param>
        /// <returns>Current song or null if out of bounds</returns>
        public EGameMode GetGameMode(int round)
        {
            if (round < _SongQueue.Count && round >= 0)
            {
                return _SongQueue[round].GameMode;
            }

            return EGameMode.TR_GAMEMODE_NORMAL;
        }

        public CPoints GetPoints()
        {
            return _Points;
        }
        #endregion Implementation
    }
}