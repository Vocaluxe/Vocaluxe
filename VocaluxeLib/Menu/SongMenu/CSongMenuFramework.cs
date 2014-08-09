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
using VocaluxeLib.Draw;
using VocaluxeLib.PartyModes;
using VocaluxeLib.Songs;

namespace VocaluxeLib.Menu.SongMenu
{
    struct SThemeSongMenu
    {
        public string Name;

        public string CoverBackgroundName;
        public string CoverBigBackgroundName;
        public string DuetIconName;
        public string VideoIconName;

        public string MedleyCalcIcon;
        public string MedleyTagIcon;

        public string ColorName;

        //public SThemeSongMenuBook songMenuBook;
        //public SThemeSongMenuDreidel songMenuDreidel;
        //public SThemeSongMenuList songMenuList;
        public SThemeSongMenuTileBoard SongMenuTileBoard;
    }

    struct SThemeSongMenuTileBoard
    {
        /// <summary>
        ///     Number of tiles horizontal
        /// </summary>
        public int NumW;

        /// <summary>
        ///     Number of tiles vertical
        /// </summary>
        public int NumH;

        /// <summary>
        ///     Number of tiles horizontal in small-modus
        /// </summary>
        public int NumWsmall;

        /// <summary>
        ///     Number of tiles vertical in small-modus
        /// </summary>
        public int NumHsmall;

        /// <summary>
        ///     Space between tiles horizontal
        /// </summary>
        public float SpaceW;

        /// <summary>
        ///     Space between tiles vertical
        /// </summary>
        public float SpaceH;

        public SRectF TileRect;
        public SRectF TileRectSmall;

        public CText TextArtist;
        public CText TextTitle;
        public CText TextSongLength;

        public CStatic StaticCoverBig;
        public CStatic StaticTextBG;
        public CStatic StaticDuetIcon;
        public CStatic StaticVideoIcon;
        public CStatic StaticMedleyCalcIcon;
        public CStatic StaticMedleyTagIcon;
    }

    abstract class CSongMenuFramework : ISongMenu
    {
        protected readonly int _PartyModeID;
        protected SThemeSongMenu _Theme;
        private bool _ThemeLoaded;

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
                    value = -1;

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
            _Theme = new SThemeSongMenu
                {
                    SongMenuTileBoard =
                        {
                            TextArtist = new CText(_PartyModeID),
                            TextTitle = new CText(_PartyModeID),
                            TextSongLength = new CText(_PartyModeID),
                            StaticCoverBig = new CStatic(_PartyModeID),
                            StaticTextBG = new CStatic(_PartyModeID),
                            StaticDuetIcon = new CStatic(_PartyModeID),
                            StaticVideoIcon = new CStatic(_PartyModeID),
                            StaticMedleyCalcIcon = new CStatic(_PartyModeID),
                            StaticMedleyTagIcon = new CStatic(_PartyModeID)
                        }
                };
        }

        #region Theme
        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool LoadTheme(string xmlPath, string elementName, CXMLReader xmlReader, int skinIndex)
        {
            string item = xmlPath + "/" + elementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.GetValue(item + "/CoverBackground", out _Theme.CoverBackgroundName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/CoverBigBackground", out _Theme.CoverBigBackgroundName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/DuetIcon", out _Theme.DuetIconName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/VideoIcon", out _Theme.VideoIconName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/MedleyCalcIcon", out _Theme.MedleyCalcIcon, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/MedleyTagIcon", out _Theme.MedleyTagIcon, String.Empty);

            if (xmlReader.GetValue(item + "/Color", out _Theme.ColorName, String.Empty))
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.ColorName, skinIndex, out _ColorInternal);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref _ColorInternal.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref _ColorInternal.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref _ColorInternal.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref _ColorInternal.A);
            }

            #region SongMenuTileBoard
            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/SongMenuTileBoard/NumW", ref _Theme.SongMenuTileBoard.NumW);
            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/SongMenuTileBoard/NumH", ref _Theme.SongMenuTileBoard.NumH);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/SpaceW", ref _Theme.SongMenuTileBoard.SpaceW);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/SpaceH", ref _Theme.SongMenuTileBoard.SpaceH);

            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/SongMenuTileBoard/NumWsmall", ref _Theme.SongMenuTileBoard.NumWsmall);
            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/SongMenuTileBoard/NumHsmall", ref _Theme.SongMenuTileBoard.NumHsmall);

            _ThemeLoaded &= _Theme.SongMenuTileBoard.TextArtist.LoadTheme(item + "/SongMenuTileBoard", "TextArtist", xmlReader, skinIndex);
            _ThemeLoaded &= _Theme.SongMenuTileBoard.TextTitle.LoadTheme(item + "/SongMenuTileBoard", "TextTitle", xmlReader, skinIndex);
            _ThemeLoaded &= _Theme.SongMenuTileBoard.TextSongLength.LoadTheme(item + "/SongMenuTileBoard", "TextSongLength", xmlReader, skinIndex);

            _ThemeLoaded &= _Theme.SongMenuTileBoard.StaticCoverBig.LoadTheme(item + "/SongMenuTileBoard", "StaticCoverBig", xmlReader, skinIndex);
            _ThemeLoaded &= _Theme.SongMenuTileBoard.StaticTextBG.LoadTheme(item + "/SongMenuTileBoard", "StaticTextBG", xmlReader, skinIndex);
            _ThemeLoaded &= _Theme.SongMenuTileBoard.StaticDuetIcon.LoadTheme(item + "/SongMenuTileBoard", "StaticDuetIcon", xmlReader, skinIndex);
            _ThemeLoaded &= _Theme.SongMenuTileBoard.StaticVideoIcon.LoadTheme(item + "/SongMenuTileBoard", "StaticVideoIcon", xmlReader, skinIndex);
            _ThemeLoaded &= _Theme.SongMenuTileBoard.StaticMedleyCalcIcon.LoadTheme(item + "/SongMenuTileBoard", "StaticMedleyCalcIcon", xmlReader, skinIndex);
            _ThemeLoaded &= _Theme.SongMenuTileBoard.StaticMedleyTagIcon.LoadTheme(item + "/SongMenuTileBoard", "StaticMedleyTagIcon", xmlReader, skinIndex);

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

        public bool SaveTheme(XmlWriter writer)
        {
            if (_ThemeLoaded)
            {
                writer.WriteStartElement(_Theme.Name);

                writer.WriteComment("<CoverBackground>: Texture name of cover background/tiles");
                writer.WriteElementString("CoverBackground", _Theme.CoverBackgroundName);

                writer.WriteComment("<CoverBigBackground>: Texture name of big cover background and info texts");
                writer.WriteElementString("CoverBigBackground", _Theme.CoverBigBackgroundName);

                writer.WriteComment("<DuetIcon>: Texture name of duet icon");
                writer.WriteElementString("DuetIcon", _Theme.DuetIconName);

                writer.WriteComment("<VideoIcon>: Texture name of video icon");
                writer.WriteElementString("VideoIcon", _Theme.VideoIconName);

                writer.WriteComment("<MedleyCalcIcon>: Texture name of medley calc (calculated) icon");
                writer.WriteElementString("MedleyCalcIcon", _Theme.MedleyCalcIcon);

                writer.WriteComment("<MedleyTagIcon>: Texture name of medley tag (manuelly set) icon");
                writer.WriteElementString("MedleyTagIcon", _Theme.MedleyTagIcon);

                writer.WriteComment("<Color>: Tile color from ColorScheme (high priority)");
                writer.WriteComment("or <R>, <G>, <B>, <A> (lower priority)");
                if (!String.IsNullOrEmpty(_Theme.ColorName))
                    writer.WriteElementString("Color", _Theme.ColorName);
                else
                {
                    writer.WriteElementString("R", _Color.R.ToString("#0.00"));
                    writer.WriteElementString("G", _Color.G.ToString("#0.00"));
                    writer.WriteElementString("B", _Color.B.ToString("#0.00"));
                    writer.WriteElementString("A", _Color.A.ToString("#0.00"));
                }

                #region SongMenuTileBoard
                writer.WriteComment("<SongMenuTileBoard>: Config for TileBoard view");
                writer.WriteStartElement("SongMenuTileBoard");

                writer.WriteComment("<NumW>: Number of tiles horizontal");
                writer.WriteElementString("NumW", _Theme.SongMenuTileBoard.NumW.ToString());

                writer.WriteComment("<NumH>: Number of tiles vertical");
                writer.WriteElementString("NumH", _Theme.SongMenuTileBoard.NumH.ToString());

                writer.WriteComment("<SpaceW>: Space between tiles horizontal");
                writer.WriteElementString("SpaceW", _Theme.SongMenuTileBoard.SpaceW.ToString("#0.00"));

                writer.WriteComment("<SpaceH>: Space between tiles vertical");
                writer.WriteElementString("SpaceH", _Theme.SongMenuTileBoard.SpaceH.ToString("#0.00"));

                writer.WriteComment("<NumWsmall>: Number of tiles horizontal in small-mode");
                writer.WriteElementString("NumWsmall", _Theme.SongMenuTileBoard.NumWsmall.ToString());

                writer.WriteComment("<NumHsmall>: Number of tiles vertical in small-mode");
                writer.WriteElementString("NumHsmall", _Theme.SongMenuTileBoard.NumHsmall.ToString());

                writer.WriteComment("<TileRectX>, <TileRectY>, <TileRectZ>, <TileRectW>, <TileRectH>: SongMenu position, width and height");
                writer.WriteElementString("TileRectX", _Theme.SongMenuTileBoard.TileRect.X.ToString("#0"));
                writer.WriteElementString("TileRectY", _Theme.SongMenuTileBoard.TileRect.Y.ToString("#0"));
                writer.WriteElementString("TileRectZ", _Theme.SongMenuTileBoard.TileRect.Z.ToString("#0.00"));
                writer.WriteElementString("TileRectW", _Theme.SongMenuTileBoard.TileRect.W.ToString("#0"));
                writer.WriteElementString("TileRectH", _Theme.SongMenuTileBoard.TileRect.H.ToString("#0"));

                writer.WriteComment("<TileRectSmallX>, <TileRectSmallY>, <TileRectSmallZ>, <TileRectSmallW>, <TileRectSmallH>: SongMenu position, width and height in small-mode");
                writer.WriteElementString("TileRectSmallX", _Theme.SongMenuTileBoard.TileRectSmall.X.ToString("#0"));
                writer.WriteElementString("TileRectSmallY", _Theme.SongMenuTileBoard.TileRectSmall.Y.ToString("#0"));
                writer.WriteElementString("TileRectSmallZ", _Theme.SongMenuTileBoard.TileRectSmall.Z.ToString("#0.00"));
                writer.WriteElementString("TileRectSmallW", _Theme.SongMenuTileBoard.TileRectSmall.W.ToString("#0"));
                writer.WriteElementString("TileRectSmallH", _Theme.SongMenuTileBoard.TileRectSmall.H.ToString("#0"));

                _Theme.SongMenuTileBoard.TextArtist.SaveTheme(writer);
                _Theme.SongMenuTileBoard.TextTitle.SaveTheme(writer);
                _Theme.SongMenuTileBoard.TextSongLength.SaveTheme(writer);

                _Theme.SongMenuTileBoard.StaticCoverBig.SaveTheme(writer);
                _Theme.SongMenuTileBoard.StaticTextBG.SaveTheme(writer);
                _Theme.SongMenuTileBoard.StaticDuetIcon.SaveTheme(writer);
                _Theme.SongMenuTileBoard.StaticVideoIcon.SaveTheme(writer);
                _Theme.SongMenuTileBoard.StaticMedleyCalcIcon.SaveTheme(writer);
                _Theme.SongMenuTileBoard.StaticMedleyTagIcon.SaveTheme(writer);

                writer.WriteEndElement();
                #endregion SongMenuTileBoard

                writer.WriteEndElement();

                return true;
            }
            return false;
        }
        #endregion Theme

        public virtual void Init()
        {
            _ResetPreview();
            _Initialized = true;
        }

        public virtual void Update(SScreenSongOptions songOptions)
        {
            if (!_Initialized)
                return;
            /*
            if (_PreviewSongStream == -1 || _PreviewVideoStream == -1)
                return;

            if (CBase.Video.IsFinished(_PreviewVideoStream) || CBase.Sound.IsFinished(_PreviewSongStream))
            {
                CBase.Video.Close(_PreviewVideoStream);
                _PreviewVideoStream = -1;
                return;
            }

            float time = CBase.Sound.GetPosition(_PreviewSongStream);

            float vtime;
            if (CBase.Video.GetFrame(_PreviewVideoStream, ref _Vidtex, time, out vtime))
            {
                if (_Vidtex != null)
                {
                    if (_VideoFading != null)
                    {
                        bool finished;
                        _Vidtex.Color.A = _VideoFading.GetValue(out finished);
                        if (finished)
                            _VideoFading = null;
                    }
                }
            }*/
        }

        public virtual void OnShow() {}
        
        public virtual void OnHide()
        {
            if(CBase.Graphics.GetNextScreen() != EScreens.ScreenNames || CBase.Config.GetBackgroundMusicStatus() == EOffOn.TR_CONFIG_OFF)
                _ResetPreview();
        }

        public abstract bool HandleInput(ref SKeyEvent keyEvent, SScreenSongOptions options);

        public abstract bool HandleMouse(ref SMouseEvent mouseEvent, SScreenSongOptions songOptions);

        public virtual void Draw()
        {
            if (!_Initialized || !Visible)
                return;

            if (CBase.PreviewPlayer.IsPlaying())
                CBase.Drawing.DrawTexture(CBase.PreviewPlayer.GetVideoTexture(), new SRectF(0, 0, 1280, 720, 0));
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

            if (!String.IsNullOrEmpty(_Theme.ColorName))
                _ColorInternal = CBase.Theme.GetColor(_Theme.ColorName, _PartyModeID);
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

            _ResetPreview();
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
            _ResetPreview();

            CSong song = CBase.Songs.GetVisibleSong(nr);
            if (song == null)
                return;

            CBase.PreviewPlayer.Load(song);

            float startposition = song.Preview.StartTime;

            float length = CBase.PreviewPlayer.GetLength();

            if (song.Preview.Source == EDataSource.None)
                startposition = length / 4f;
            else if (startposition > length - 5f)
                startposition = Math.Max(0f, Math.Min(length / 4f, length - 5f));

            CBase.PreviewPlayer.Play(startposition);
        }

        protected void _ResetPreview()
        {
            CBase.PreviewPlayer.Stop();

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