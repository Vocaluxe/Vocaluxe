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

using System.Xml;
using VocaluxeLib.PartyModes;

namespace VocaluxeLib.Menu.SongMenu
{
    public class CSongMenu : ISongMenu
    {
        private readonly int _PartyModeID;

        private ISongMenu _SongMenu;
        private ESongMenu _Type;

        public CSongMenu(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _CreateSongMenu();
        }

        public bool Selected
        {
            get { return _SongMenu.IsSelected(); }
            set { _SongMenu.SetSelected(value); }
        }

        public bool Visible
        {
            get { return _SongMenu.IsVisible(); }
            set { _SongMenu.SetVisible(value); }
        }

        public SRectF Rect
        {
            get { return _SongMenu.Rect; }
        }

        #region ISongMenu
        public void Update(SScreenSongOptions songOptions)
        {
            _SongMenu.Update(songOptions);
        }

        public void OnShow()
        {
            _SongMenu.OnShow();
        }

        public void OnHide()
        {
            _SongMenu.OnHide();
        }

        public void HandleInput(ref SKeyEvent keyEvent, SScreenSongOptions songOptions)
        {
            _SongMenu.HandleInput(ref keyEvent, songOptions);
        }

        public void HandleMouse(ref SMouseEvent mouseEvent, SScreenSongOptions songOptions)
        {
            _SongMenu.HandleMouse(ref mouseEvent, songOptions);
        }

        public void Draw()
        {
            _SongMenu.Draw();
        }

        public void ApplyVolume(float volumeMax)
        {
            _SongMenu.ApplyVolume(volumeMax);
        }

        public bool IsActive()
        {
            return _SongMenu.IsActive();
        }

        public void SetActive(bool active)
        {
            _SongMenu.SetActive(active);
        }

        public bool IsMouseOverActualSelection(SMouseEvent mEvent)
        {
            return _SongMenu.IsMouseOverActualSelection(mEvent);
        }

        public int GetSelectedSong()
        {
            return _SongMenu.GetSelectedSong();
        }

        public CStatic GetSelectedSongCover()
        {
            return _SongMenu.GetSelectedSongCover();
        }

        public void SetSelectedSong(int visibleSongNr)
        {
            _SongMenu.SetSelectedSong(visibleSongNr);
        }

        public int GetSelectedCategory()
        {
            return _SongMenu.GetSelectedCategory();
        }

        public void SetSelectedCategory(int categoryNr)
        {
            _SongMenu.SetSelectedCategory(categoryNr);
        }

        public bool EnterCurrentCategory()
        {
            return _SongMenu.EnterCurrentCategory();
        }

        public int GetActualSelection()
        {
            return _SongMenu.GetActualSelection();
        }

        public bool IsSelected()
        {
            return _SongMenu.IsSelected();
        }

        public void SetSelected(bool selected)
        {
            _SongMenu.SetSelected(selected);
        }

        public bool IsVisible()
        {
            return _SongMenu.IsVisible();
        }

        public void SetVisible(bool visible)
        {
            _SongMenu.SetVisible(visible);
        }

        public bool IsSmallView()
        {
            return _SongMenu.IsSmallView();
        }

        public void SetSmallView(bool smallView)
        {
            _SongMenu.SetSmallView(smallView);
        }
        #endregion ISongMenu

        #region IMenuElement
        public string GetThemeName()
        {
            return _SongMenu.GetThemeName();
        }

        public bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader, int skinIndex)
        {
            return _SongMenu.LoadTheme(xmlPath, elementName, xmlReader, skinIndex);
        }

        public bool SaveTheme(XmlWriter writer)
        {
            return _SongMenu.SaveTheme(writer);
        }

        public void UnloadTextures()
        {
            _SongMenu.UnloadTextures();
        }

        public void LoadTextures()
        {
            _SongMenu.LoadTextures();
        }

        public void ReloadTextures()
        {
            _SongMenu.ReloadTextures();
        }

        public void MoveElement(int stepX, int stepY)
        {
            _SongMenu.MoveElement(stepX, stepY);
        }

        public void ResizeElement(int stepW, int stepH)
        {
            _SongMenu.ResizeElement(stepW, stepH);
        }
        #endregion IMenuElement

        private void _CreateSongMenu()
        {
            if (_SongMenu != null)
                _SongMenu.OnHide();

            switch (CBase.Config.GetSongMenuType())
            {
                    //case ESongMenu.TR_CONFIG_LIST:
                    //    _SongMenu = new CSongMenuList();
                    //    break;

                    //case ESongMenu.TR_CONFIG_DREIDEL:
                    //    _SongMenu = new CSongMenuDreidel();
                    //    break;
                case ESongMenu.TR_CONFIG_TILE_BOARD:
                    _SongMenu = new CSongMenuTileBoard(_PartyModeID);
                    break;

                    //case ESongMenu.TR_CONFIG_BOOK:
                    //    _SongMenu = new CSongMenuBook();
                    //    break;
            }

            _Type = CBase.Config.GetSongMenuType();
        }
    }
}