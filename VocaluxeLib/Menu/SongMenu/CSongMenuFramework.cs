using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

using Vocaluxe.PartyModes;

namespace Vocaluxe.Menu.SongMenu
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
        public SThemeSongMenuTileBoard songMenuTileBoard;
    }

    struct SThemeSongMenuBook
    {
    }

    struct SThemeSongMenuDreidel
    {
    }

    struct SThemeSongMenuList
    {
    }

    struct SThemeSongMenuTileBoard
    {
        /// <summary>
        /// Number of tiles horizontal
        /// </summary>
        public int numW;

        /// <summary>
        /// Number of tiles vertical
        /// </summary>
        public int numH;

        /// <summary>
        /// Number of tiles horizontal in small-modus
        /// </summary>
        public int numWsmall;

        /// <summary>
        /// Number of tiles vertical in small-modus
        /// </summary>
        public int numHsmall;

        /// <summary>
        /// Space between tiles horizontal
        /// </summary>
        public float spaceW;

        /// <summary>
        /// Space between tiles vertical
        /// </summary>
        public float spaceH;

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
        protected int _PartyModeID;
        protected SThemeSongMenu _Theme;
        private bool _ThemeLoaded;

        private Stopwatch _timer = new Stopwatch();
        private Stopwatch _VideoFadeTimer = new Stopwatch();
        private List<int> _streams = new List<int>();
        private int _video = -1;
        private int _actsong = -1;
        private int _actsongstream = -1;
        protected STexture _vidtex = new STexture(-1);

        protected bool _Initialized = false;
        protected int _LastKnownNumSongs = 0;
        protected int _LastKnownCategory = -1;

        protected SRectF _Rect = new SRectF();
        protected SColorF _Color = new SColorF();

        private int _SelectedInternal = -1;
        private int _SelectedPending = -1;
        protected long _PendingTime = 500L;

        private int _LockedInternal = -1;
        protected bool _Active = false;

        protected float _MaxVolume = 100f;

        protected int _PreviewSelected //for preview only
        {
            get
            {
                if ((_SelectedInternal != _SelectedPending) && (_timer.ElapsedMilliseconds >= _PendingTime))
                {
                    _timer.Stop();
                    _timer.Reset();
                    _SelectedInternal = _SelectedPending;
                }
                return _SelectedInternal;
            }
            set
            {
                if (value == -1)
                {
                    _timer.Stop();
                    _timer.Reset();

                    _SelectedInternal = -1;
                    _SelectedPending = -1;
                    return;
                }

                if ((value != _SelectedInternal) && (value != _SelectedPending))
                {
                    _timer.Reset();
                    _timer.Start();

                    _SelectedPending = value;
                }

                if ((value == _SelectedPending) && ((_timer.ElapsedMilliseconds >= _PendingTime) || (_SelectedInternal == -1)))
                {
                    _timer.Stop();
                    _timer.Reset();
                    _SelectedInternal = _SelectedPending;
                }
            }

        }

        protected virtual void SetSelectedNow()
        {
            _timer.Stop();
            _timer.Reset();
            _SelectedInternal = _SelectedPending;
        }

        protected int _Locked   //the real selected song for singing
        {
            get { return _LockedInternal; }
            set { _LockedInternal = value; }
        }

        protected int _SongStream
        {
            get { return _actsongstream; }
        }

        protected int _Video
        {
            get { return _video; }
        }

        protected int _ActSong
        {
            get { return _actsong; }
        }

        public SRectF Rect
        {
            get { return _Rect; }
        }

        public SRectF GetRect()
        {
            return _Rect;
        }

        public SColorF Color
        {
            get { return _Color; }
        }

        public virtual int GetActualSelection()
        {
            return _actsong;
        }

        private bool _Selected = false;
        private bool _Visible = true;

        public bool IsSelected()
        {
            return _Selected;
        }

        public void SetSelected(bool Selected)
        {
            _Selected = Selected;
            SetActive(Selected);
        }

        public bool IsVisible()
        {
            return _Visible;
        }

        public void SetVisible(bool Visible)
        {
            _Visible = Visible;
        }

        public CSongMenuFramework(int PartyModeID)
        {
            _PartyModeID = PartyModeID;
            _Theme = new SThemeSongMenu();

            _Theme.songMenuTileBoard.TextArtist = new CText(_PartyModeID);
            _Theme.songMenuTileBoard.TextTitle = new CText(_PartyModeID);
            _Theme.songMenuTileBoard.TextSongLength = new CText(_PartyModeID);

            _Theme.songMenuTileBoard.StaticCoverBig = new CStatic(_PartyModeID);
            _Theme.songMenuTileBoard.StaticTextBG = new CStatic(_PartyModeID);
            _Theme.songMenuTileBoard.StaticDuetIcon = new CStatic(_PartyModeID);
            _Theme.songMenuTileBoard.StaticVideoIcon = new CStatic(_PartyModeID);
            _Theme.songMenuTileBoard.StaticMedleyCalcIcon = new CStatic(_PartyModeID);
            _Theme.songMenuTileBoard.StaticMedleyTagIcon = new CStatic(_PartyModeID);

            _ThemeLoaded = false;
        }

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool LoadTheme(string XmlPath, string ElementName, CXMLReader xmlReader, int SkinIndex)
        {
            string item = XmlPath + "/" + ElementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= xmlReader.GetValue(item + "/CoverBackground", ref _Theme.CoverBackgroundName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/CoverBigBackground", ref _Theme.CoverBigBackgroundName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/DuetIcon", ref _Theme.DuetIconName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/VideoIcon", ref _Theme.VideoIconName, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/MedleyCalcIcon", ref _Theme.MedleyCalcIcon, String.Empty);
            _ThemeLoaded &= xmlReader.GetValue(item + "/MedleyTagIcon", ref _Theme.MedleyTagIcon, String.Empty);

            if (xmlReader.GetValue(item + "/Color", ref _Theme.ColorName, String.Empty))
            {
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.ColorName, SkinIndex, ref _Color);
            }
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref _Color.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref _Color.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref _Color.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref _Color.A);
            }

            #region SongMenuTileBoard
            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/SongMenuTileBoard/NumW", ref _Theme.songMenuTileBoard.numW);
            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/SongMenuTileBoard/NumH", ref _Theme.songMenuTileBoard.numH);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/SpaceW", ref _Theme.songMenuTileBoard.spaceW);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/SpaceH", ref _Theme.songMenuTileBoard.spaceH);

            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/SongMenuTileBoard/NumWsmall", ref _Theme.songMenuTileBoard.numWsmall);
            _ThemeLoaded &= xmlReader.TryGetIntValue(item + "/SongMenuTileBoard/NumHsmall", ref _Theme.songMenuTileBoard.numHsmall);

            _ThemeLoaded &= _Theme.songMenuTileBoard.TextArtist.LoadTheme(item + "/SongMenuTileBoard", "TextArtist", xmlReader, SkinIndex);
            _ThemeLoaded &= _Theme.songMenuTileBoard.TextTitle.LoadTheme(item + "/SongMenuTileBoard", "TextTitle", xmlReader, SkinIndex);
            _ThemeLoaded &= _Theme.songMenuTileBoard.TextSongLength.LoadTheme(item + "/SongMenuTileBoard", "TextSongLength", xmlReader, SkinIndex);

            _ThemeLoaded &= _Theme.songMenuTileBoard.StaticCoverBig.LoadTheme(item + "/SongMenuTileBoard", "StaticCoverBig", xmlReader, SkinIndex);
            _ThemeLoaded &= _Theme.songMenuTileBoard.StaticTextBG.LoadTheme(item + "/SongMenuTileBoard", "StaticTextBG", xmlReader, SkinIndex);
            _ThemeLoaded &= _Theme.songMenuTileBoard.StaticDuetIcon.LoadTheme(item + "/SongMenuTileBoard", "StaticDuetIcon", xmlReader, SkinIndex);
            _ThemeLoaded &= _Theme.songMenuTileBoard.StaticVideoIcon.LoadTheme(item + "/SongMenuTileBoard", "StaticVideoIcon", xmlReader, SkinIndex);
            _ThemeLoaded &= _Theme.songMenuTileBoard.StaticMedleyCalcIcon.LoadTheme(item + "/SongMenuTileBoard", "StaticMedleyCalcIcon", xmlReader, SkinIndex);
            _ThemeLoaded &= _Theme.songMenuTileBoard.StaticMedleyTagIcon.LoadTheme(item + "/SongMenuTileBoard", "StaticMedleyTagIcon", xmlReader, SkinIndex);

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/TileRectX", ref _Theme.songMenuTileBoard.TileRect.X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/TileRectY", ref _Theme.songMenuTileBoard.TileRect.Y);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/TileRectZ", ref _Theme.songMenuTileBoard.TileRect.Z);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/TileRectW", ref _Theme.songMenuTileBoard.TileRect.W);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/TileRectH", ref _Theme.songMenuTileBoard.TileRect.H);

            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/TileRectSmallX", ref _Theme.songMenuTileBoard.TileRectSmall.X);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/TileRectSmallY", ref _Theme.songMenuTileBoard.TileRectSmall.Y);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/TileRectSmallZ", ref _Theme.songMenuTileBoard.TileRectSmall.Z);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/TileRectSmallW", ref _Theme.songMenuTileBoard.TileRectSmall.W);
            _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/SongMenuTileBoard/TileRectSmallH", ref _Theme.songMenuTileBoard.TileRectSmall.H);

            #endregion SongMenuTileBoard

            if (_ThemeLoaded)
            {
                _Theme.Name = ElementName;
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
                if (_Theme.ColorName != String.Empty)
                {
                    writer.WriteElementString("Color", _Theme.ColorName);
                }
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
                writer.WriteElementString("NumW", _Theme.songMenuTileBoard.numW.ToString());

                writer.WriteComment("<NumH>: Number of tiles vertical");
                writer.WriteElementString("NumH", _Theme.songMenuTileBoard.numH.ToString());

                writer.WriteComment("<SpaceW>: Space between tiles horizontal");
                writer.WriteElementString("SpaceW", _Theme.songMenuTileBoard.spaceW.ToString("#0.00"));

                writer.WriteComment("<SpaceH>: Space between tiles vertical");
                writer.WriteElementString("SpaceH", _Theme.songMenuTileBoard.spaceH.ToString("#0.00"));

                writer.WriteComment("<NumWsmall>: Number of tiles horizontal in small-mode");
                writer.WriteElementString("NumWsmall", _Theme.songMenuTileBoard.numWsmall.ToString());

                writer.WriteComment("<NumHsmall>: Number of tiles vertical in small-mode");
                writer.WriteElementString("NumHsmall", _Theme.songMenuTileBoard.numHsmall.ToString());

                writer.WriteComment("<TileRectX>, <TileRectY>, <TileRectZ>, <TileRectW>, <TileRectH>: SongMenu position, width and height");
                writer.WriteElementString("TileRectX", _Theme.songMenuTileBoard.TileRect.X.ToString("#0"));
                writer.WriteElementString("TileRectY", _Theme.songMenuTileBoard.TileRect.Y.ToString("#0"));
                writer.WriteElementString("TileRectZ", _Theme.songMenuTileBoard.TileRect.Z.ToString("#0.00"));
                writer.WriteElementString("TileRectW", _Theme.songMenuTileBoard.TileRect.W.ToString("#0"));
                writer.WriteElementString("TileRectH", _Theme.songMenuTileBoard.TileRect.H.ToString("#0"));

                writer.WriteComment("<TileRectSmallX>, <TileRectSmallY>, <TileRectSmallZ>, <TileRectSmallW>, <TileRectSmallH>: SongMenu position, width and height in small-mode");
                writer.WriteElementString("TileRectSmallX", _Theme.songMenuTileBoard.TileRectSmall.X.ToString("#0"));
                writer.WriteElementString("TileRectSmallY", _Theme.songMenuTileBoard.TileRectSmall.Y.ToString("#0"));
                writer.WriteElementString("TileRectSmallZ", _Theme.songMenuTileBoard.TileRectSmall.Z.ToString("#0.00"));
                writer.WriteElementString("TileRectSmallW", _Theme.songMenuTileBoard.TileRectSmall.W.ToString("#0"));
                writer.WriteElementString("TileRectSmallH", _Theme.songMenuTileBoard.TileRectSmall.H.ToString("#0"));

                _Theme.songMenuTileBoard.TextArtist.SaveTheme(writer);
                _Theme.songMenuTileBoard.TextTitle.SaveTheme(writer);
                _Theme.songMenuTileBoard.TextSongLength.SaveTheme(writer);

                _Theme.songMenuTileBoard.StaticCoverBig.SaveTheme(writer);
                _Theme.songMenuTileBoard.StaticTextBG.SaveTheme(writer);
                _Theme.songMenuTileBoard.StaticDuetIcon.SaveTheme(writer);
                _Theme.songMenuTileBoard.StaticVideoIcon.SaveTheme(writer);
                _Theme.songMenuTileBoard.StaticMedleyCalcIcon.SaveTheme(writer);
                _Theme.songMenuTileBoard.StaticMedleyTagIcon.SaveTheme(writer);
                                
                writer.WriteEndElement();
                #endregion SongMenuTileBoard

                writer.WriteEndElement();

                return true;
            }
            return false;
        }

        public virtual void Init()
        {
            Reset();
            _Initialized = true;
        }

        public virtual void UpdateRect(SRectF rect)
        {
            _Rect = rect;
            Init();
        }

        public virtual void Update(ScreenSongOptions SongOptions)
        {
            if (!_Initialized)
                return;

            if (_actsong != _PreviewSelected)
                SelectSong(_PreviewSelected);

            if (_streams.Count > 0 && _video != -1)
            {
                if (CBase.Video.IsFinished(_video) || CBase.Sound.IsFinished(_actsongstream))
                {
                    CBase.Video.Close(_video);
                    _video = -1;
                    return;
                }

                float time = CBase.Sound.GetPosition(_actsongstream);
                
                float vtime = 0f;
                CBase.Video.GetFrame(_video, ref _vidtex, time, ref vtime);
                if (_VideoFadeTimer.ElapsedMilliseconds <= 3000L)
                {
                    _vidtex.color.A = (_VideoFadeTimer.ElapsedMilliseconds / 3000f);
                }
                else
                {
                    _vidtex.color.A = 1f;
                    _VideoFadeTimer.Stop();
                }
            }
        }

        public virtual void OnShow()
        {
            _actsongstream = -1;
            _vidtex = new STexture(-1);
        }

        public virtual void OnHide()
        {
            foreach (int stream in _streams)
            {
                CBase.Sound.FadeAndStop(stream, 0f, 0.75f);
            }
            _streams.Clear();

            CBase.Video.Close(_video);
            _video = -1;

            CBase.Drawing.RemoveTexture(ref _vidtex);            

            _timer.Stop();
            _timer.Reset();
            _SelectedInternal = _SelectedPending;
        }

        public virtual void HandleInput(ref KeyEvent KeyEvent, ScreenSongOptions SongOptions)
        {
            if (!_Initialized)
                return;
        }

        public virtual void HandleMouse(ref MouseEvent MouseEvent, ScreenSongOptions SongOptions)
        {
            if (!_Initialized)
                return;
        }

        public virtual void Draw()
        {
            if (!_Initialized || !_Visible)
                return;

            if (_video != -1)
            {
                CBase.Drawing.DrawTexture(_vidtex, new SRectF(0, 0, 1280, 720, 0));
            }
            
        }

        public virtual void ApplyVolume(float VolumeMax)
        {
            _MaxVolume = VolumeMax;

            foreach (int stream in _streams)
            {
                CBase.Sound.SetStreamVolumeMax(stream, _MaxVolume);
            }
        }

        public virtual bool IsActive()
        {
            return _Active;
        }

        public virtual void SetActive(bool Active)
        {
            _Active = Active;
        }

        public virtual int GetSelectedSong()
        {
            return -1;
        }

        public virtual CStatic GetSelectedSongCover()
        {
            return new CStatic(_PartyModeID);
        }

        public virtual int GetSelectedCategory()
        {
            return -1;
        }

        public virtual void SetSelectedSong(int VisibleSongNr)
        {
            if (!_Initialized)
                return;
        }

        public virtual void SetSelectedCategory(int CategoryNr)
        {
            if (!_Initialized)
                return;
        }

        public virtual void SetSmallView(bool SmallView)
        {
            if (!_Initialized)
                return;
        }

        public virtual bool IsSmallView()
        {
            return false;
        }

        public virtual void UnloadTextures()
        {
        }

        public virtual void LoadTextures()
        {
            Init();

            if (_Theme.ColorName != String.Empty)
                _Color = CBase.Theme.GetColor(_Theme.ColorName, _PartyModeID);
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();
        }

        protected virtual void EnterCategory(int Category)
        {
            if (!_Initialized)
                return;

            if (Category >= CBase.Songs.GetNumCategories())
                return;

            Reset();
            CBase.Songs.SetCategory(Category);
        }

        protected virtual void ShowCategories()
        {
            if (!_Initialized)
                return;

            if (CBase.Songs.GetCurrentCategoryIndex() != -1)
                Reset();

            Reset();
            CBase.Songs.SetCategory(-1);
        }

        public void ApplyVolume()
        {
            CBase.Sound.SetStreamVolume(_actsongstream, CBase.Config.GetPreviewMusicVolume());
        }

        protected void SelectSong(int nr)
        {
            if (CBase.Songs.GetCurrentCategoryIndex() >= 0 && (CBase.Songs.GetNumVisibleSongs() > 0) && (nr >= 0) && ((_actsong != nr) || (_streams.Count == 0)))
            {
                foreach (int stream in _streams)
                {
                    CBase.Sound.FadeAndStop(stream, 0f, 1f);
                }
                _streams.Clear();

                CBase.Video.Close(_video);
                _video = -1;

                CBase.Drawing.RemoveTexture(ref _vidtex);

                _actsong = nr;
                if (_actsong >= CBase.Songs.GetNumVisibleSongs())
                    _actsong = 0;


                int _stream = CBase.Sound.Load(Path.Combine(CBase.Songs.GetVisibleSong(_actsong).Folder, CBase.Songs.GetVisibleSong(_actsong).MP3FileName), true);
                CBase.Sound.SetStreamVolumeMax(_stream, _MaxVolume);
                CBase.Sound.SetStreamVolume(_stream, 0f);

                float startposition = CBase.Songs.GetVisibleSong(_actsong).PreviewStart;

                if (startposition == 0f)
                    startposition = CBase.Sound.GetLength(_stream) / 4f;

                CBase.Sound.SetPosition(_stream, startposition);
                CBase.Sound.Play(_stream);
                CBase.Sound.Fade(_stream, 100f, 3f);
                _streams.Add(_stream);
                _actsongstream = _stream;
                
                if (CBase.Songs.GetVisibleSong(_actsong).VideoFileName != String.Empty && CBase.Config.GetVideoPreview() == EOffOn.TR_CONFIG_ON)
                {
                    _video = CBase.Video.Load(Path.Combine(CBase.Songs.GetVisibleSong(_actsong).Folder, CBase.Songs.GetVisibleSong(_actsong).VideoFileName));
                    if (_video == -1)
                        return;
                    CBase.Video.Skip(_video, startposition, CBase.Songs.GetVisibleSong(_actsong).VideoGap);
                    _VideoFadeTimer.Stop();
                    _VideoFadeTimer.Reset();
                    _VideoFadeTimer.Start();
                }
            }
        }

        protected void Reset()
        {
            foreach (int stream in _streams)
            {
                CBase.Sound.FadeAndStop(stream, 0f, 0.75f);
            }
            _streams.Clear();

            CBase.Video.Close(_video);
            _video = -1;

            CBase.Drawing.RemoveTexture(ref _vidtex);

            _timer.Stop();
            _timer.Reset();
            _SelectedInternal = _SelectedPending;
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
            SRectF rect = Rect;
            rect.X += stepX;
            rect.Y += stepY;
            UpdateRect(rect);
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

            UpdateRect(rect);
        }
        #endregion ThemeEdit
    }
}
