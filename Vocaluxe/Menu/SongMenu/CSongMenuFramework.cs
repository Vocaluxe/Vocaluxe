﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;

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

        public CSongMenuFramework()
        {
            _Theme = new SThemeSongMenu();

            _Theme.songMenuTileBoard.TextArtist = new CText();
            _Theme.songMenuTileBoard.TextTitle = new CText();
            _Theme.songMenuTileBoard.TextSongLength = new CText();

            _Theme.songMenuTileBoard.StaticCoverBig = new CStatic();
            _Theme.songMenuTileBoard.StaticTextBG = new CStatic();
            _Theme.songMenuTileBoard.StaticDuetIcon = new CStatic();
            _Theme.songMenuTileBoard.StaticVideoIcon = new CStatic();
            _Theme.songMenuTileBoard.StaticMedleyCalcIcon = new CStatic();
            _Theme.songMenuTileBoard.StaticMedleyTagIcon = new CStatic();

            _ThemeLoaded = false;
        }

        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public bool LoadTheme(string XmlPath, string ElementName, XPathNavigator navigator, int SkinIndex)
        {
            string item = XmlPath + "/" + ElementName;
            _ThemeLoaded = true;

            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/CoverBackground", navigator, ref _Theme.CoverBackgroundName, String.Empty);
            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/CoverBigBackground", navigator, ref _Theme.CoverBigBackgroundName, String.Empty);
            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/DuetIcon", navigator, ref _Theme.DuetIconName, String.Empty);
            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/VideoIcon", navigator, ref _Theme.VideoIconName, String.Empty);
            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/MedleyCalcIcon", navigator, ref _Theme.MedleyCalcIcon, String.Empty);
            _ThemeLoaded &= CHelper.GetValueFromXML(item + "/MedleyTagIcon", navigator, ref _Theme.MedleyTagIcon, String.Empty);

            if (CHelper.GetValueFromXML(item + "/Color", navigator, ref _Theme.ColorName, String.Empty))
            {
                _ThemeLoaded &= CTheme.GetColor(_Theme.ColorName, SkinIndex, ref _Color);
            }
            else
            {
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/R", navigator, ref _Color.R);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/G", navigator, ref _Color.G);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/B", navigator, ref _Color.B);
                _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/A", navigator, ref _Color.A);
            }

            #region SongMenuTileBoard
            _ThemeLoaded &= CHelper.TryGetIntValueFromXML(item + "/SongMenuTileBoard/NumW", navigator, ref _Theme.songMenuTileBoard.numW);
            _ThemeLoaded &= CHelper.TryGetIntValueFromXML(item + "/SongMenuTileBoard/NumH", navigator, ref _Theme.songMenuTileBoard.numH);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SongMenuTileBoard/SpaceW", navigator, ref _Theme.songMenuTileBoard.spaceW);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SongMenuTileBoard/SpaceH", navigator, ref _Theme.songMenuTileBoard.spaceH);

            _ThemeLoaded &= CHelper.TryGetIntValueFromXML(item + "/SongMenuTileBoard/NumWsmall", navigator, ref _Theme.songMenuTileBoard.numWsmall);
            _ThemeLoaded &= CHelper.TryGetIntValueFromXML(item + "/SongMenuTileBoard/NumHsmall", navigator, ref _Theme.songMenuTileBoard.numHsmall);

            _ThemeLoaded &= _Theme.songMenuTileBoard.TextArtist.LoadTheme(item + "/SongMenuTileBoard", "TextArtist", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.songMenuTileBoard.TextTitle.LoadTheme(item + "/SongMenuTileBoard", "TextTitle", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.songMenuTileBoard.TextSongLength.LoadTheme(item + "/SongMenuTileBoard", "TextSongLength", navigator, SkinIndex);

            _ThemeLoaded &= _Theme.songMenuTileBoard.StaticCoverBig.LoadTheme(item + "/SongMenuTileBoard", "StaticCoverBig", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.songMenuTileBoard.StaticTextBG.LoadTheme(item + "/SongMenuTileBoard", "StaticTextBG", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.songMenuTileBoard.StaticDuetIcon.LoadTheme(item + "/SongMenuTileBoard", "StaticDuetIcon", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.songMenuTileBoard.StaticVideoIcon.LoadTheme(item + "/SongMenuTileBoard", "StaticVideoIcon", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.songMenuTileBoard.StaticMedleyCalcIcon.LoadTheme(item + "/SongMenuTileBoard", "StaticMedleyCalcIcon", navigator, SkinIndex);
            _ThemeLoaded &= _Theme.songMenuTileBoard.StaticMedleyTagIcon.LoadTheme(item + "/SongMenuTileBoard", "StaticMedleyTagIcon", navigator, SkinIndex);

            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SongMenuTileBoard/TileRectX", navigator, ref _Theme.songMenuTileBoard.TileRect.X);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SongMenuTileBoard/TileRectY", navigator, ref _Theme.songMenuTileBoard.TileRect.Y);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SongMenuTileBoard/TileRectZ", navigator, ref _Theme.songMenuTileBoard.TileRect.Z);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SongMenuTileBoard/TileRectW", navigator, ref _Theme.songMenuTileBoard.TileRect.W);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SongMenuTileBoard/TileRectH", navigator, ref _Theme.songMenuTileBoard.TileRect.H);

            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SongMenuTileBoard/TileRectSmallX", navigator, ref _Theme.songMenuTileBoard.TileRectSmall.X);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SongMenuTileBoard/TileRectSmallY", navigator, ref _Theme.songMenuTileBoard.TileRectSmall.Y);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SongMenuTileBoard/TileRectSmallZ", navigator, ref _Theme.songMenuTileBoard.TileRectSmall.Z);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SongMenuTileBoard/TileRectSmallW", navigator, ref _Theme.songMenuTileBoard.TileRectSmall.W);
            _ThemeLoaded &= CHelper.TryGetFloatValueFromXML(item + "/SongMenuTileBoard/TileRectSmallH", navigator, ref _Theme.songMenuTileBoard.TileRectSmall.H);

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

        public virtual void Update()
        {
            if (!_Initialized)
                return;

            if (_actsong != _PreviewSelected)
                SelectSong(_PreviewSelected);

            if (_streams.Count > 0 && _video != -1)
            {
                if (CVideo.VdFinished(_video) || CSound.IsFinished(_actsongstream))
                {
                    CVideo.VdClose(_video);
                    _video = -1;
                    return;
                }

                float time = CSound.GetPosition(_actsongstream);
                
                float vtime = 0f;
                CVideo.VdGetFrame(_video, ref _vidtex, time, ref vtime);
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
                CSound.FadeAndStop(stream, 0f, 0.75f);
            }
            _streams.Clear();

            CVideo.VdClose(_video);
            _video = -1;

            CDraw.RemoveTexture(ref _vidtex);            

            _timer.Stop();
            _timer.Reset();
            _SelectedInternal = _SelectedPending;
        }

        public virtual void HandleInput(ref KeyEvent KeyEvent)
        {
            if (!_Initialized)
                return;
        }

        public virtual void HandleMouse(ref MouseEvent MouseEvent)
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
                CDraw.DrawTexture(_vidtex, new SRectF(0, 0, 1280, 720, 0));
            }
            
        }

        public virtual void ApplyVolume(float VolumeMax)
        {
            _MaxVolume = VolumeMax;

            foreach (int stream in _streams)
            {
                CSound.SetStreamVolumeMax(stream, _MaxVolume);
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
            return new CStatic();
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
                _Color = CTheme.GetColor(_Theme.ColorName);
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

            if (Category >= CSongs.NumCategories)
                return;

            CSongs.Category = Category;
        }

        protected virtual void ShowCategories()
        {
            if (!_Initialized)
                return;

            if (CSongs.Category != -1)
                Reset();

            CSongs.Category = -1;
        }

        public void ApplyVolume()
        {
            CSound.SetStreamVolume(_actsongstream, CConfig.PreviewMusicVolume);
        }

        protected void SelectSong(int nr)
        {
            if (CSongs.Category >= 0 && (CSongs.NumVisibleSongs > 0) && (nr >= 0) && ((_actsong != nr) || (_streams.Count == 0)))
            {
                foreach (int stream in _streams)
                {
                    CSound.FadeAndStop(stream, 0f, 1f);
                }
                _streams.Clear();

                CVideo.VdClose(_video);
                _video = -1;

                CDraw.RemoveTexture(ref _vidtex);

                _actsong = nr;
                if (_actsong >= CSongs.NumVisibleSongs)
                    _actsong = 0;


                int _stream = CSound.Load(Path.Combine(CSongs.VisibleSongs[_actsong].Folder, CSongs.VisibleSongs[_actsong].MP3FileName), true);
                CSound.SetStreamVolumeMax(_stream, _MaxVolume);
                CSound.SetStreamVolume(_stream, 0f);

                float startposition = CSongs.VisibleSongs[_actsong].PreviewStart;

                if (startposition == 0f)
                    startposition = CSound.GetLength(_stream) / 4f;

                CSound.SetPosition(_stream, startposition);
                CSound.Play(_stream);
                CSound.Fade(_stream, 100f, 3f);
                _streams.Add(_stream);
                _actsongstream = _stream;

                if (CSongs.VisibleSongs[_actsong].VideoFileName != String.Empty && CConfig.VideoPreview == EOffOn.TR_CONFIG_ON)
                {
                    _video = CVideo.VdLoad(Path.Combine(CSongs.VisibleSongs[_actsong].Folder, CSongs.VisibleSongs[_actsong].VideoFileName));
                    if (_video == -1)
                        return;
                    CVideo.VdSkip(_video, startposition, CSongs.VisibleSongs[_actsong].VideoGap);
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
                CSound.FadeAndStop(stream, 0f, 0.75f);
            }
            _streams.Clear();

            CVideo.VdClose(_video);
            _video = -1;

            CDraw.RemoveTexture(ref _vidtex);

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
