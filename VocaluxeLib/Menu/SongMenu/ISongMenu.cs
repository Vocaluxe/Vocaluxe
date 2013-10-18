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

using VocaluxeLib.PartyModes;

namespace VocaluxeLib.Menu.SongMenu
{
    interface ISongMenu : IMenuElement
    {
        void Update(SScreenSongOptions songOptions);
        void OnShow();
        void OnHide();

        void HandleInput(ref SKeyEvent keyEvent, SScreenSongOptions songOptions);
        void HandleMouse(ref SMouseEvent mouseEvent, SScreenSongOptions songOptions);
        void Draw();

        void ApplyVolume(float volumeMax);

        int GetSelectedSong();
        CStatic GetSelectedSongCover();
        void SetSelectedSong(int visibleSongNr);

        bool IsActive();
        void SetActive(bool active);

        bool IsMouseOverActualSelection(SMouseEvent mEvent);

        int GetSelectedCategory();
        void SetSelectedCategory(int categoryNr);
        int GetActualSelection();

        bool EnterCurrentCategory();

        bool IsSelected();
        void SetSelected(bool selected);

        bool IsVisible();
        void SetVisible(bool visible);

        bool IsSmallView();
        void SetSmallView(bool smallView);

        SRectF Rect { get; }
    }
}