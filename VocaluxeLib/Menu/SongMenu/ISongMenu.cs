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

using VocaluxeLib.PartyModes;

namespace VocaluxeLib.Menu.SongMenu
{
    public interface ISongMenu : IMenuElement, IThemeable
    {
        void Update(SScreenSongOptions songOptions);
        void OnShow();
        void OnHide();

        bool HandleInput(ref SKeyEvent keyEvent, SScreenSongOptions options);
        bool HandleMouse(ref SMouseEvent mouseEvent, SScreenSongOptions songOptions);

        // The selected song is the song, where the mouse is hovering over
        // The number is refering to the index in the visible songs array
        int GetPreviewSongNr();
        int GetSelectedSongNr();
        CStatic GetSelectedSongCover();
        void SetSelectedSong(int visibleSongNr);
        bool IsMouseOverSelectedSong(SMouseEvent mEvent);

        // Same for categories
        int GetSelectedCategory();
        void SetSelectedCategory(int categoryNr);
        bool EnterSelectedCategory();
        void LeaveSelectedCategory();

        // The song that is currently playing (set e.g. by clicking on a song)
        int GetPreviewSong();

        bool SmallView { get; set; }
    }
}