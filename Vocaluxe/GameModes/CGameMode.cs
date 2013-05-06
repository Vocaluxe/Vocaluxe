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

using Vocaluxe.Base;
using VocaluxeLib.Songs;

namespace Vocaluxe.GameModes
{
    abstract class CGameMode : IGameMode
    {
        private CSong _LastSong;
        private int _LastSongID = -1;

        public CSong GetSong(int songID)
        {
            if (songID != _LastSongID)
            {
                CSong song = CSongs.GetSong(songID);
                _LastSong = _PrepareSong(song);
                _LastSongID = songID;
            }
            return _LastSong;
        }

        protected abstract CSong _PrepareSong(CSong song);
    }
}