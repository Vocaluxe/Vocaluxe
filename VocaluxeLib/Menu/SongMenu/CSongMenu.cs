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
using VocaluxeLib.PartyModes;
using VocaluxeLib.Xml;

namespace VocaluxeLib.Menu.SongMenu
{
    public class CSongMenu : ISongMenu
    {
        private readonly int _PartyModeID;
        private ISongMenu _SongMenu;

        /// <summary>
        ///     Deprecated! Only used for old theme loading.
        /// </summary>
        /// <param name="partyModeID"></param>
        public CSongMenu(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _SongMenu = new CSongMenuTileBoard(partyModeID);
        }

        public CSongMenu(SThemeSongMenu theme, int partyModeID)
        {
            _PartyModeID = partyModeID;
            _CreateSongMenu(theme);
            CBase.Config.AddSongMenuListener(_OnSongMenuChanged);
        }

        #region ISongMenu
        public bool SmallView
        {
            get { return _SongMenu.SmallView; }
            set { _SongMenu.SmallView = value; }
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
        public bool Highlighted
        {
            get { return _SongMenu.Highlighted; }
            set { _SongMenu.Highlighted = value; }
        }
        public bool Selected
        {
            get { return _SongMenu.Selected; }
            set { _SongMenu.Selected = value; }
        }

        public bool Selectable
        {
            get { return _SongMenu.Selectable; }
        }
        public bool Visible
        {
            get { return _SongMenu.Visible; }
            set { _SongMenu.Visible = value; }
        }

        public SRectF Rect
        {
            get { return _SongMenu.Rect; }
        }
        public SRectF MaxRect
        {
            get { return _SongMenu.MaxRect; }
            set { _SongMenu.MaxRect = value; }
        }
        #endregion IMenuElement

        #region IThemeable
        public string GetThemeName()
        {
            return _SongMenu.GetThemeName();
        }

        public object GetTheme()
        {
            return _SongMenu.GetTheme();
        }

        public bool ThemeLoaded
        {
            get { return _SongMenu.ThemeLoaded; }
        }

        public bool LoadTheme(string xmlPath, string elementName, CXmlReader xmlReader)
        {
            return _SongMenu.LoadTheme(xmlPath, elementName, xmlReader);
        }

        public void UnloadSkin()
        {
            _SongMenu.UnloadSkin();
        }

        public void LoadSkin()
        {
            _SongMenu.LoadSkin();
        }

        public void ReloadSkin()
        {
            _SongMenu.ReloadSkin();
        }

        public void MoveElement(int stepX, int stepY)
        {
            _SongMenu.MoveElement(stepX, stepY);
        }

        public void ResizeElement(int stepW, int stepH)
        {
            _SongMenu.ResizeElement(stepW, stepH);
        }
        #endregion IThemeable

        private void _OnSongMenuChanged()
        {
            _CreateSongMenu((SThemeSongMenu)_SongMenu.GetTheme());
            _SongMenu.LoadSkin();
        }

        private void _CreateSongMenu(SThemeSongMenu theme)
        {
            switch (CBase.Config.GetSongMenuType())
            {
                case ESongMenu.TR_CONFIG_LIST:
                    _SongMenu = new CSongMenuList(theme, _PartyModeID);
                    break;

                    //case ESongMenu.TR_CONFIG_DREIDEL:
                    //    _SongMenu = new CSongMenuDreidel();
                    //    break;
                case ESongMenu.TR_CONFIG_TILE_BOARD:
                    _SongMenu = new CSongMenuTileBoard(theme, _PartyModeID);
                    break;

                    //case ESongMenu.TR_CONFIG_BOOK:
                    //    _SongMenu = new CSongMenuBook();
                    //    break;
                default:
                    throw new ArgumentException("Invalid songmenu type: " + CBase.Config.GetSongMenuType());
            }
        }
    }
}