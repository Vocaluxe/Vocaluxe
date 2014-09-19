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
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using VocaluxeLib.Draw;
using VocaluxeLib.PartyModes;
using VocaluxeLib.Songs;

namespace VocaluxeLib.Menu.SongMenu
{
    [XmlType("SongMenu")]
    public struct SThemeSongMenu
    {
        [XmlAttributeAttribute(AttributeName = "Name")]
        public string Name;

        [XmlElement("CoverBackground")]
        public string CoverBackgroundName;
        [XmlElement("CoverBigBackground")]
        public string CoverBigBackgroundName;
        [XmlElement("DuetIcon")]
        public string DuetIconName;
        [XmlElement("VideoIcon")]
        public string VideoIconName;
        [XmlElement("MedleyCalcIcon")]
        public string MedleyCalcIcon;
        [XmlElement("MedleyTagIcon")]
        public string MedleyTagIcon;
        [XmlElement("Color")]
        public SThemeColor Color;

        //public SThemeSongMenuBook songMenuBook;
        //public SThemeSongMenuDreidel songMenuDreidel;
        //public SThemeSongMenuList songMenuList;
        [XmlElement("SongMenuTileBoard")]
        public SThemeSongMenuTileBoard SongMenuTileBoard;

        public SThemeSongMenu(SThemeSongMenu theme)
        {
            Name = theme.Name;
            CoverBackgroundName = theme.CoverBackgroundName;
            CoverBigBackgroundName = theme.CoverBigBackgroundName;
            DuetIconName = theme.DuetIconName;
            VideoIconName = theme.VideoIconName;
            MedleyCalcIcon = theme.MedleyCalcIcon;
            MedleyTagIcon = theme.MedleyTagIcon;
            Color = new SThemeColor(theme.Color);
            SongMenuTileBoard = theme.SongMenuTileBoard;
        }
    }

    public struct SThemeSongMenuTileBoard
    {
        [XmlElement("NumW")]
        /// <summary>
        ///     Number of tiles horizontal
        /// </summary>
        public int NumW;

        [XmlElement("NumH")]
        /// <summary>
        ///     Number of tiles vertical
        /// </summary>
        public int NumH;

        [XmlElement("NumWsmall")]
        /// <summary>
        ///     Number of tiles horizontal in small-modus
        /// </summary>
        public int NumWsmall;
        
        [XmlElement("NumHsmall")]
        /// <summary>
        ///     Number of tiles vertical in small-modus
        /// </summary>
        public int NumHsmall;

        [XmlElement("SpaceW")]
        /// <summary>
        ///     Space between tiles horizontal
        /// </summary>
        public float SpaceW;

        [XmlElement("SpaceH")]
        /// <summary>
        ///     Space between tiles vertical
        /// </summary>
        public float SpaceH;

        [XmlElement("TileRect")]
        public SRectF TileRect;
        [XmlElement("TileRectSmall")]
        public SRectF TileRectSmall;

        [XmlElement("TextArtist")]
        public SThemeText TextArtist;
        [XmlElement("TextTitle")]
        public SThemeText TextTitle;
        [XmlElement("TextSongLength")]
        public SThemeText TextSongLength;

        [XmlElement("StaticCoverBig")]
        public SThemeStatic StaticCoverBig;
        [XmlElement("StaticTextBG")]
        public SThemeStatic StaticTextBG;
        [XmlElement("StaticDuetIcon")]
        public SThemeStatic StaticDuetIcon;
        [XmlElement("StaticVideoIcon")]
        public SThemeStatic StaticVideoIcon;
        [XmlElement("StaticMedleyCalcIcon")]
        public SThemeStatic StaticMedleyCalcIcon;
        [XmlElement("StaticMedleyTagIcon")]
        public SThemeStatic StaticMedleyTagIcon;
    }

    abstract class CSongMenuFramework : ISongMenu
    {
        protected readonly int _PartyModeID;
        protected SThemeSongMenu _Theme;
        private bool _ThemeLoaded;

        public bool ThemeLoaded
        {
            get { return _ThemeLoaded; }
        }

        protected bool _Initialized;

        private int _PreviewNrInternal = -1;
        protected virtual int _PreviewNr
        {
            get { return _PreviewNrInternal; }
            set
            {
                if (_PreviewNrInternal == value)
                    return;
                if (CBase.Songs.IsInCategory())
                {
                    if (value >= CBase.Songs.GetNumSongsVisible())
                        value = -1;
                    _PlaySong(value);
                }
                else if (value >= CBase.Songs.GetNumCategories())
                {
                    value = -1;
                    CBase.BackgroundMusic.Stop();
                }

                _PreviewNrInternal = value;
            }
        }

        private SColorF _ColorInternal;
        protected SColorF _Color
        {
            get { return _ColorInternal; }
        }

        public int GetPreviewSong()
        {
            return _PreviewNr;
        }

        public SRectF Rect { get; protected set; }
        public bool Active { get; set; }
        private bool _Selected;
        public bool Selected
        {
            get { return _Selected; }
            set
            {
                _Selected = value;
                Active = value;
            }
        }
        public bool Visible { get; set; }
        public virtual bool SmallView { get; set; }
        public abstract float SelectedTileZoomFactor { get; }
        // This is the nr of the current selection (song or category)
        protected virtual int _SelectionNr { get; set; }

        protected CSongMenuFramework(int partyModeID)
        {
            Visible = true;
            _PartyModeID = partyModeID;
        }

        protected CSongMenuFramework(SThemeSongMenu theme, int partyModeID)
        {
            Visible = true;
            _PartyModeID = partyModeID;
            _Theme = new SThemeSongMenu(theme);

            LoadTextures();
        }

        #region Theme
        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public SThemeSongMenu GetTheme()
        {
            return new SThemeSongMenu(_Theme);
        }

        public virtual bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader, int skinIndex)
        {
            string item = xmlPath + "/" + elementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.GetValue(item + "/CoverBackground", out _Theme.CoverBackgroundName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/CoverBigBackground", out _Theme.CoverBigBackgroundName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/DuetIcon", out _Theme.DuetIconName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/VideoIcon", out _Theme.VideoIconName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/MedleyCalcIcon", out _Theme.MedleyCalcIcon, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/MedleyTagIcon", out _Theme.MedleyTagIcon, String.Empty);

            if (xmlReader.GetValue(item + "/Color", out _Theme.Color.Name, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.Color.Name, skinIndex, out _ColorInternal);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref _ColorInternal.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref _ColorInternal.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref _ColorInternal.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref _ColorInternal.A);
            }
            _Theme.Color.Color = new SColorF(_ColorInternal);

            #region SongMenuTileBoard
            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/SongMenuTileBoard/NumW", ref _Theme.SongMenuTileBoard.NumW);
            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/SongMenuTileBoard/NumH", ref _Theme.SongMenuTileBoard.NumH);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/SpaceW", ref _Theme.SongMenuTileBoard.SpaceW);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/SpaceH", ref _Theme.SongMenuTileBoard.SpaceH);

            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/SongMenuTileBoard/NumWsmall", ref _Theme.SongMenuTileBoard.NumWsmall);
            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/SongMenuTileBoard/NumHsmall", ref _Theme.SongMenuTileBoard.NumHsmall);

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/TileRectX", ref _Theme.SongMenuTileBoard.TileRect.X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/TileRectY", ref _Theme.SongMenuTileBoard.TileRect.Y);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/TileRectZ", ref _Theme.SongMenuTileBoard.TileRect.Z);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/TileRectW", ref _Theme.SongMenuTileBoard.TileRect.W);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/TileRectH", ref _Theme.SongMenuTileBoard.TileRect.H);

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/TileRectSmallX", ref _Theme.SongMenuTileBoard.TileRectSmall.X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/TileRectSmallY", ref _Theme.SongMenuTileBoard.TileRectSmall.Y);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/TileRectSmallZ", ref _Theme.SongMenuTileBoard.TileRectSmall.Z);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/TileRectSmallW", ref _Theme.SongMenuTileBoard.TileRectSmall.W);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/TileRectSmallH", ref _Theme.SongMenuTileBoard.TileRectSmall.H);
            #endregion SongMenuTileBoard

            if (_ThemeLoaded)
            {
                _Theme.Name = elementName;

                LoadTextures();
                Init();
            }

            return _ThemeLoaded;
        }

        #endregion Theme

        public virtual void Init()
        {
            _ResetPreview(false);
            _Initialized = true;
        }

        public virtual void Update(SScreenSongOptions songOptions)
        {
            if (!_Initialized)
                return;
        }

        public virtual void OnShow() {}
        
        public virtual void OnHide()
        {
            if(CBase.Graphics.GetNextScreen() != EScreens.ScreenNames)
                _ResetPreview();
        }

        public abstract bool HandleInput(ref SKeyEvent keyEvent, SScreenSongOptions options);

        public abstract bool HandleMouse(ref SMouseEvent mouseEvent, SScreenSongOptions songOptions);

        public virtual void Draw()
        {
            if (!_Initialized || !Visible)
                return;

            if (CBase.BackgroundMusic.IsPlaying())
                CBase.Drawing.DrawTexture(CBase.BackgroundMusic.GetVideoTexture(), new SRectF(0, 0, 1280, 720, 0));
        }

        public bool IsMouseOverSelectedSong(SMouseEvent mEvent)
        {
            CStatic selCov = GetSelectedSongCover();
            return selCov != null && CHelper.IsInBounds(selCov.Rect.Scale(SelectedTileZoomFactor), mEvent);
        }

        public int GetPreviewSongNr()
        {
            if (CBase.Songs.IsInCategory())
                return _PreviewNr;
            return -1;
        }

        public int GetSelectedSongNr()
        {
            if (CBase.Songs.IsInCategory())
                return _SelectionNr;
            return -1;
        }

        public abstract CStatic GetSelectedSongCover();

        public int GetSelectedCategory()
        {
            if (!CBase.Songs.IsInCategory())
                return _SelectionNr;
            return -1;
        }

        public void SetSelectedSong(int visibleSongNr)
        {
            Debug.Assert(CBase.Songs.IsInCategory());
            if (visibleSongNr >= 0 && visibleSongNr < CBase.Songs.GetNumSongsVisible())
                _SelectionNr = visibleSongNr;
            else
                _SelectionNr = -1;
            _PreviewNr = _SelectionNr;
        }

        public void SetSelectedCategory(int categoryNr)
        {
            Debug.Assert(!CBase.Songs.IsInCategory());
            if (categoryNr >= 0 && categoryNr < CBase.Songs.GetNumCategories())
                _SelectionNr = categoryNr;
            else
                _SelectionNr = -1;
            _PreviewNr = _SelectionNr;
        }

        public virtual void UnloadTextures() {}

        public virtual void LoadTextures()
        {
            Init();

            if (!String.IsNullOrEmpty(_Theme.Color.Name))
                _ColorInternal = CBase.Theme.GetColor(_Theme.Color.Name, _PartyModeID);
            else
                _ColorInternal = _Theme.Color.Color;

        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();
        }

        public bool EnterSelectedCategory()
        {
            if (!_Initialized)
                return false;
            if (CBase.Songs.IsInCategory() || GetSelectedCategory() < 0 || GetSelectedCategory() >= CBase.Songs.GetNumCategories())
                return false;
            _EnterCategory(GetSelectedCategory());
            return true;
        }

        protected virtual void _EnterCategory(int categoryNr)
        {
            if (!_Initialized)
                return;

            if (categoryNr >= CBase.Songs.GetNumCategories())
                return;

            _ResetPreview(false);
            CBase.Songs.SetCategory(categoryNr);
        }

        protected virtual void _LeaveCategory()
        {
            if (!_Initialized)
                return;

            if (!CBase.Songs.IsInCategory())
                return;

            _ResetPreview();
            CBase.Songs.SetCategory(-1);
        }

        private void _PlaySong(int nr)
        {
            _PreviewNrInternal = -1;

            CSong song = CBase.Songs.GetVisibleSong(nr);
            if (song == null)
                return;

            CBase.BackgroundMusic.LoadPreview(song);
        }

        protected void _ResetPreview(bool playBGagain = true)
        {
            if (_PreviewNrInternal == -1)
                return;

            CBase.BackgroundMusic.StopPreview();

            if (playBGagain)
                CBase.BackgroundMusic.Play();

            //Make sure we don't have a preview here otherwise a change won't be recognized
            //(e.g. leave a category with one song and set preview to 0 --> previewOld=previewNew=0 --> No change --> Old data shown
            //Use internal nr because this function gets called from withing setPreviewNr
            _PreviewNrInternal = -1;
        }

        #region ThemeEdit
        private void _UpdateRect(SRectF rect)
        {
            Rect = rect;
            Init();
        }

        public void MoveElement(int stepX, int stepY)
        {
            SRectF rect = Rect;
            rect.X += stepX;
            rect.Y += stepY;
            _UpdateRect(rect);
        }

        public void ResizeElement(int stepW, int stepH)
        {
            SRectF rect = Rect;

            rect.W += stepW;
            if (rect.W < 100)
                rect.W = 100;

            rect.H += stepH;
            if (rect.H < 100)
                rect.H = 100;

            _UpdateRect(rect);
        }
        #endregion ThemeEdit
    }
}