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
using VocaluxeLib.Menu;

namespace VocaluxeLib.PartyModes.TicTacToe
{
    class CPartyScreenTicTacToeSongs : CMenuPartySongSelection
    {
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private new CPartyModeTicTacToe _PartyMode;

        public override void Init()
        {
            base.Init();
            _PartyMode = (CPartyModeTicTacToe)base._PartyMode;
        }

        public override void OnShow()
        {
            Source = _PartyMode.GameData.SongSource;
            Sorting = _PartyMode.GameData.Sorting;
            Category = _PartyMode.GameData.CategoryIndex;
            Playlist = _PartyMode.GameData.PlaylistID;
            SongMode = _PartyMode.GameData.GameMode;
            NumMedleySongs = _PartyMode.GameData.NumMedleySongs;

            base.OnShow();
        }

        public override void Back()
        {
            _SaveConfig();
            _PartyMode.Back();
        }

        public override void Next()
        {
            _SaveConfig();
            _PartyMode.Next();
        }

        public override bool UpdateGame()
        {
            return true;
        }

        protected override void _SetAllowedOptions()
        {
            base._SetAllowedOptions();

            AllowedSongModes = new EGameMode[] { EGameMode.TR_GAMEMODE_NORMAL, EGameMode.TR_GAMEMODE_SHORTSONG, EGameMode.TR_GAMEMODE_MEDLEY };
        }

        private void _SaveConfig()
        {
            _PartyMode.GameData.SongSource = Source;
            _PartyMode.GameData.Sorting = Sorting;
            _PartyMode.GameData.CategoryIndex = Category;
            _PartyMode.GameData.PlaylistID = Playlist;
            _PartyMode.GameData.GameMode = SongMode;
            _PartyMode.GameData.NumMedleySongs = NumMedleySongs;
        }
    }
}
