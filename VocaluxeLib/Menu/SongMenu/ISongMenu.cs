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

        SRectF GetRect();
    }
}