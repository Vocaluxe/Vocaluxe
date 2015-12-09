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

using System;

namespace VocaluxeLib.Menu.SongMenu
{
    public static class CSongMenuFactory
    {
        public static ISongMenu CreateSongMenu(SThemeSongMenu theme, int partyModeID)
        {
            switch (CBase.Config.GetSongMenuType())
            {
                case ESongMenu.TR_CONFIG_LIST:
                    return new CSongMenuList(theme, partyModeID);

                    //case ESongMenu.TR_CONFIG_DREIDEL:
                    //    _SongMenu = new CSongMenuDreidel();
                    //    break;
                case ESongMenu.TR_CONFIG_TILE_BOARD:
                    return new CSongMenuTileBoard(theme, partyModeID);

                    //case ESongMenu.TR_CONFIG_BOOK:
                    //    _SongMenu = new CSongMenuBook();
                    //    break;
            }
            throw new ArgumentException("Invalid songmenu type: " + CBase.Config.GetSongMenuType());
        }

        /// <summary>
        ///     Deprecated! Only used for old theme loading.
        /// </summary>
        /// <param name="partyModeID"></param>
        /// <returns></returns>
        public static ISongMenu CreateSongMenu(int partyModeID)
        {
            return new CSongMenuTileBoard(partyModeID);
        }
    }
}