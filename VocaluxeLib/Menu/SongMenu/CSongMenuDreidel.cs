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

namespace VocaluxeLib.Menu.SongMenu
{
    class CSongMenuDreidel : CSongMenuFramework
    {
        /*
        private SRectF _ScrollRect;
        private CStatic _CoverBig;
        private CStatic _TextBG;

        private CStatic _DuetIcon;
        private CStatic _VideoIcon;

        private CText _Artist;
        private CText _Title;
        private CText _SongLength;

        private int _actualSelection = -1;

        public override int GetActualSelection()
        {
            return _actualSelection;
        }
        */

        public CSongMenuDreidel(int partyModeID)
            : base(partyModeID) {}

        // ReSharper disable RedundantOverridenMember
        public override void Init()
        {
            base.Init();
            /**
            _CoverBig = _Theme.songMenuDreidel.StaticCoverBig;
            _TextBG = _Theme.songMenuDreidel.StaticTextBG;
            _DuetIcon = _Theme.songMenuDreidel.StaticDuetIcon;
            _VideoIcon = _Theme.songMenuDreidel.StaticVideoIcon;

            _Artist = _Theme.songMenuDreidel.TextArtist;
            _Title = _Theme.songMenuDreidel.TextTitle;
            _SongLength = _Theme.songMenuDreidel.TextSongLength;
             **/
        }

        // ReSharper restore RedundantOverridenMember
    }
}