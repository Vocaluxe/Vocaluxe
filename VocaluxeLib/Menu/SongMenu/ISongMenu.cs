using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.PartyModes;

namespace Vocaluxe.Menu.SongMenu
{
    interface ISongMenu : IMenuElement
    {
        void Update(ScreenSongOptions SongOptions);
        void OnShow();
        void OnHide();

        void HandleInput(ref KeyEvent KeyEvent, ScreenSongOptions SongOptions);
        void HandleMouse(ref MouseEvent MouseEvent, ScreenSongOptions SongOptions);
        void Draw();

        void ApplyVolume(float VolumeMax);

        int GetSelectedSong();
        CStatic GetSelectedSongCover();
        void SetSelectedSong(int VisibleSongNr);

        bool IsActive();
        void SetActive(bool Active);

        int GetSelectedCategory();
        void SetSelectedCategory(int CategoryNr);
        int GetActualSelection();

        bool IsSelected();
        void SetSelected(bool Selected);

        bool IsVisible();
        void SetVisible(bool Visible);

        bool IsSmallView();
        void SetSmallView(bool SmallView);

        SRectF GetRect();
    }
}
