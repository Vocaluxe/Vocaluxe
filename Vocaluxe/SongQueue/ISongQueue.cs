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

using VocaluxeLib.Menu;
using VocaluxeLib.Menu.SongMenu;

namespace Vocaluxe.SongQueue
{
    interface ISongQueue
    {
        void Init();

        EGameMode GetCurrentGameMode();

        bool AddVisibleSong(int visibleIndex, EGameMode gameMode);
        bool AddSong(int absoluteIndex, EGameMode gameMode);
        bool RemoveVisibleSong(int visibleIndex);
        bool RemoveSong(int absoluteIndex);
        void ClearSongs();

        void Reset();
        void Start(SPlayer[] players);
        void NextRound(SPlayer[] players);
        bool IsFinished();
        int GetCurrentRoundNr();

        CPoints GetPoints();

        int GetNumSongs();
        CSong GetSong();
        CSong GetSong(int num);
        EGameMode GetGameMode(int num);
    }
}