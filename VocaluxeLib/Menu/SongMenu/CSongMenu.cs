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

        #region ISongMenu
        public bool Selected
        {
            get { return _SongMenu.Selected; }
            set { _SongMenu.Selected = value; }
        }

        public bool Visible
        {
            get { return _SongMenu.Visible; }
            set { _SongMenu.Visible = value; }
        }

        public bool Active
        {
            get { return _SongMenu.Active; }
            set { _SongMenu.Active = value; }
        }

        public bool SmallView
        {
            get { return _SongMenu.SmallView; }
            set { _SongMenu.SmallView = value; }
        }

        public SRectF Rect
        {
            get { return _SongMenu.Rect; }
        }

        public bool ThemeLoaded
        {
            get { return _SongMenu.ThemeLoaded; }
        }

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

        public bool HandleInput(ref SKeyEvent keyEvent, SScreenSongOptions options)
        {
            return _SongMenu.HandleInput(ref keyEvent, options);
        }

        public bool HandleMouse(ref SMouseEvent mouseEvent, SScreenSongOptions songOptions)
        {
            return _SongMenu.HandleMouse(ref mouseEvent, songOptions);
        }

        public void Draw()
        {
            _SongMenu.Draw();
        }

        public bool IsMouseOverSelectedSong(SMouseEvent mEvent)
        {
            return _SongMenu.IsMouseOverSelectedSong(mEvent);
        }

        public int GetPreviewSongNr()
        {
            return _SongMenu.GetPreviewSongNr();
        }

        public int GetSelectedSongNr()
        {
            return _SongMenu.GetSelectedSongNr();
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

        public bool EnterSelectedCategory()
        {
            return _SongMenu.EnterSelectedCategory();
        }

        public int GetPreviewSong()
        {
            return _SongMenu.GetPreviewSong();
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

        public SThemeSongMenu GetTheme()
        {
            return _SongMenu.GetTheme();
        }
        #endregion IMenuElement

        private void _CreateSongMenu()
        {
            if (_SongMenu != null)
                _SongMenu.OnHide();

            _Type = CBase.Config.GetSongMenuType();
            switch (_Type)
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
        }
    }
}