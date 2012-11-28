using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.XPath;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;

namespace Vocaluxe.Menu.SongMenu
{
    class CSongMenu : ISongMenu
    {
        private ISongMenu _SongMenu;
        private ESongMenu _Type;

        public CSongMenu()
        {
            CreateSongMenu();
        }

        public void UpdateSongMenuType()
        {
            if (_Type != CConfig.SongMenu)
                CreateSongMenu();
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
            get { return _SongMenu.GetRect(); }
        }

        #region ISongMenu
        public void Update()
        {
            _SongMenu.Update();
        }

        public void OnShow()
        {
            _SongMenu.OnShow();
        }

        public void OnHide()
        {
            _SongMenu.OnHide();
        }

        public void HandleInput(ref KeyEvent KeyEvent)
        {
            _SongMenu.HandleInput(ref KeyEvent);
        }

        public void HandleMouse(ref MouseEvent MouseEvent)
        {
            _SongMenu.HandleMouse(ref MouseEvent);
        }

        public void Draw()
        {
            _SongMenu.Draw();
        }

        public bool IsActive()
        {
            return _SongMenu.IsActive();
        }

        public void SetActive(bool Active)
        {
            _SongMenu.SetActive(Active);
        }

        public int GetSelectedSong()
        {
            return _SongMenu.GetSelectedSong();
        }

        public void SetSelectedSong(int VisibleSongNr)
        {
            _SongMenu.SetSelectedSong(VisibleSongNr);
        }

        public int GetSelectedCategory()
        {
            return _SongMenu.GetSelectedCategory();
        }

        public int GetActualSelection()
        {
            return _SongMenu.GetActualSelection();
        }

        public bool IsSelected()
        {
            return _SongMenu.IsSelected();
        }

        public void SetSelected(bool Selected)
        {
            _SongMenu.SetSelected(Selected);
        }

        public bool IsVisible()
        {
            return _SongMenu.IsVisible();
        }

        public void SetVisible(bool Visible)
        {
            _SongMenu.SetVisible(Visible);
        }

        public SRectF GetRect()
        {
            return _SongMenu.GetRect();
        }

        public bool IsSmallView()
        {
            return _SongMenu.IsSmallView();
        }

        public void SetSmallView(bool SmallView)
        {
            _SongMenu.SetSmallView(SmallView);
        }

        #endregion ISongMenu

        #region IMenuElement
        public string GetThemeName()
        {
            return _SongMenu.GetThemeName();
        }

        public bool LoadTheme(string XmlPath, string ElementName, XPathNavigator navigator, int SkinIndex)
        {
            return _SongMenu.LoadTheme(XmlPath, ElementName, navigator, SkinIndex);
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

        private void CreateSongMenu()
        {
            if (_SongMenu != null)
            {
                _SongMenu.OnHide();
            }

            switch (CConfig.SongMenu)
            {
                //case ESongMenu.TR_CONFIG_LIST:
                //    _SongMenu = new CSongMenuList();
                //    break;

                //case ESongMenu.TR_CONFIG_DREIDEL:
                //    _SongMenu = new CSongMenuDreidel();
                //    break;

                case ESongMenu.TR_CONFIG_TILE_BOARD:
                    _SongMenu = new CSongMenuTileBoard();
                    break;

                //case ESongMenu.TR_CONFIG_BOOK:
                //    _SongMenu = new CSongMenuBook();
                //    break;
            }

            _Type = CConfig.SongMenu;
        }
    }
}
