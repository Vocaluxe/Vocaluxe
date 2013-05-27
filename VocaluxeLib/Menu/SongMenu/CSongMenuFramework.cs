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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using VocaluxeLib.Draw;
using VocaluxeLib.PartyModes;

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

        private readonly Stopwatch _Timer = new Stopwatch();
        private readonly Stopwatch _VideoFadeTimer = new Stopwatch();
        private readonly List<int> _Streams = new List<int>();
        private int _Actsong = -1;
        private int _Actsongstream = -1;
        protected CTexture _Vidtex;

        protected bool _Initialized;
        protected int _LastKnownNumSongs;
        protected int _LastKnownCategory = -1;

        protected SRectF _Rect;
        protected SColorF _Color;

        private int _SelectedInternal = -1;
        private int _SelectedPending = -1;
        protected long _PendingTime = 500L;

        private int _LockedInternal = -1;
        protected bool _Active;

        protected float _MaxVolume = 100f;

        protected int _PreviewSelected
        {
            //for preview only
            get
            {
                if ((_SelectedInternal != _SelectedPending) && (_Timer.ElapsedMilliseconds >= _PendingTime))
                {
                    _Timer.Stop();
                    _Timer.Reset();
                    _SelectedInternal = _SelectedPending;
                }
                return _SelectedInternal;
            }
            set
            {
                if (value == -1)
                {
                    _Timer.Stop();
                    _Timer.Reset();

                    _SelectedInternal = -1;
                    _SelectedPending = -1;
                    return;
                }

                if ((value != _SelectedInternal) && (value != _SelectedPending))
                {
                    _Timer.Reset();
                    _Timer.Start();

                    _SelectedPending = value;
                }

                if ((value == _SelectedPending) && ((_Timer.ElapsedMilliseconds >= _PendingTime) || (_SelectedInternal == -1)))
                {
                    _Timer.Stop();
                    _Timer.Reset();
                    _SelectedInternal = _SelectedPending;
                }
            }
        }

        protected void _SetSelectedNow()
        {
            _Timer.Stop();
            _Timer.Reset();
            _SelectedInternal = _SelectedPending;
        }

        protected int _Locked
        {
            //the real selected song for singing
            get { return _LockedInternal; }
            set { _LockedInternal = value; }
        }

        protected int _SongStream
        {
            get { return _Actsongstream; }
        }

        protected int _Video { get; private set; }

        protected int _ActSong
        {
            get { return _Actsong; }
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
            return _Actsong;
        }

        private bool _Selected;
        private bool _Visible = true;

        public bool IsSelected()
        {
            return _Selected;
        }

        public void SetSelected(bool selected)
        {
            _Selected = selected;
            SetActive(selected);
        }

        public bool IsVisible()
        {
            return _Visible;
        }

        public void SetVisible(bool visible)
        {
            _Visible = visible;
        }

        protected CSongMenuFramework(int partyModeID)
        {
            _PartyModeID = partyModeID;
            _Video = -1;
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
                _ThemeLoaded &= CBase.Theme.GetColor(_Theme.ColorName, skinIndex, out _Color);
            else
            {
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/R", ref _Color.R);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/G", ref _Color.G);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/B", ref _Color.B);
                _ThemeLoaded &= xmlReader.TryGetFloatValue(item + "/A", ref _Color.A);
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

        public virtual void Init()
        {
            _Reset();
            _Initialized = true;
        }

        public void UpdateRect(SRectF rect)
        {
            _Rect = rect;
            Init();
        }

        public virtual void Update(SScreenSongOptions songOptions)
        {
            if (!_Initialized)
                return;

            if (_Actsong != _PreviewSelected)
                _SelectSong(_PreviewSelected);

            if (_Streams.Count <= 0 || _Video == -1)
                return;

            if (CBase.Video.IsFinished(_Video) || CBase.Sound.IsFinished(_Actsongstream))
            {
                CBase.Video.Close(_Video);
                _Video = -1;
                return;
            }

            float time = CBase.Sound.GetPosition(_Actsongstream);

            float vtime;
            if (CBase.Video.GetFrame(_Video, ref _Vidtex, time, out vtime))
            {
                if (_VideoFadeTimer.ElapsedMilliseconds <= 3000L)
                    _Vidtex.Color.A = _VideoFadeTimer.ElapsedMilliseconds / 3000f;
                else
                {
                    _Vidtex.Color.A = 1f;
                    _VideoFadeTimer.Stop();
                }
            }
        }

        public virtual void OnShow()
        {
            _Actsongstream = -1;
            _Vidtex = null;
            ApplyVolume(CBase.Config.GetPreviewMusicVolume());
        }

        public virtual void OnHide()
        {
            foreach (int stream in _Streams)
                CBase.Sound.FadeAndStop(stream, 0f, 0.75f);
            _Streams.Clear();

            CBase.Video.Close(_Video);
            _Video = -1;

            CBase.Drawing.RemoveTexture(ref _Vidtex);

            _Timer.Stop();
            _Timer.Reset();
            _SelectedInternal = _SelectedPending;
        }

        public virtual void HandleInput(ref SKeyEvent keyEvent, SScreenSongOptions songOptions) {}

        public virtual void HandleMouse(ref SMouseEvent mouseEvent, SScreenSongOptions songOptions) {}

        public virtual void Draw()
        {
            if (!_Initialized || !_Visible)
                return;

            if (_Video != -1)
                CBase.Drawing.DrawTexture(_Vidtex, new SRectF(0, 0, 1280, 720, 0));
        }

        public virtual void ApplyVolume(float volumeMax)
        {
            _MaxVolume = volumeMax;

            foreach (int stream in _Streams)
                CBase.Sound.SetStreamVolumeMax(stream, _MaxVolume);
        }

        public virtual bool IsActive()
        {
            return _Active;
        }

        public virtual void SetActive(bool active)
        {
            _Active = active;
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

        public virtual void SetSelectedSong(int visibleSongNr) {}

        public virtual void SetSelectedCategory(int categoryNr) {}

        public virtual void SetSmallView(bool smallView) {}

        public virtual bool IsSmallView()
        {
            return false;
        }

        public virtual void UnloadTextures() {}

        public virtual void LoadTextures()
        {
            Init();

            if (!String.IsNullOrEmpty(_Theme.ColorName))
                _Color = CBase.Theme.GetColor(_Theme.ColorName, _PartyModeID);
        }

        public void ReloadTextures()
        {
            UnloadTextures();
            LoadTextures();
        }

        public bool EnterCurrentCategory()
        {
            if (!_Initialized)
                return false;
            if (CBase.Songs.IsInCategory() || _PreviewSelected < 0 || _PreviewSelected >= CBase.Songs.GetNumCategories())
                return false;
            _EnterCategory(_PreviewSelected);
            return true;
        }

        protected virtual void _EnterCategory(int category)
        {
            if (!_Initialized)
                return;

            if (category >= CBase.Songs.GetNumCategories())
                return;

            _Reset();
            CBase.Songs.SetCategory(category);
        }

        protected virtual void _ShowCategories()
        {
            if (!_Initialized)
                return;

            if (!CBase.Songs.IsInCategory())
                return;

            _Reset();
            CBase.Songs.SetCategory(-1);
        }

        public void ApplyVolume()
        {
            CBase.Sound.SetStreamVolume(_Actsongstream, CBase.Config.GetPreviewMusicVolume());
        }

        protected void _SelectSong(int nr)
        {
            if (CBase.Songs.IsInCategory() && (CBase.Songs.GetNumSongsVisible() > 0) && (nr >= 0) && ((_Actsong != nr) || (_Streams.Count == 0)))
            {
                _Streams.ForEach(soundStream => CBase.Sound.FadeAndStop(soundStream, 0f, 1f));
                _Streams.Clear();

                CBase.Video.Close(_Video);
                _Video = -1;

                CBase.Drawing.RemoveTexture(ref _Vidtex);

                _Actsong = nr;
                if (_Actsong >= CBase.Songs.GetNumSongsVisible())
                    _Actsong = 0;


                int stream = CBase.Sound.Load(Path.Combine(CBase.Songs.GetVisibleSong(_Actsong).Folder, CBase.Songs.GetVisibleSong(_Actsong).MP3FileName), true);
                CBase.Sound.SetStreamVolumeMax(stream, _MaxVolume);
                CBase.Sound.SetStreamVolume(stream, 0f);

                float startposition = CBase.Songs.GetVisibleSong(_Actsong).PreviewStart;

                if (Math.Abs(startposition) < 0.001)
                    startposition = CBase.Sound.GetLength(stream) / 4f;

                CBase.Sound.SetPosition(stream, startposition);
                CBase.Sound.Play(stream);
                CBase.Sound.Fade(stream, 100f, 3f);
                _Streams.Add(stream);
                _Actsongstream = stream;

                if (CBase.Songs.GetVisibleSong(_Actsong).VideoFileName != "" && CBase.Config.GetVideoPreview() == EOffOn.TR_CONFIG_ON)
                {
                    _Video = CBase.Video.Load(Path.Combine(CBase.Songs.GetVisibleSong(_Actsong).Folder, CBase.Songs.GetVisibleSong(_Actsong).VideoFileName));
                    if (_Video == -1)
                        return;
                    CBase.Video.Skip(_Video, startposition, CBase.Songs.GetVisibleSong(_Actsong).VideoGap);
                    _VideoFadeTimer.Stop();
                    _VideoFadeTimer.Reset();
                    _VideoFadeTimer.Start();
                }
            }
        }

        protected void _Reset()
        {
            foreach (int stream in _Streams)
                CBase.Sound.FadeAndStop(stream, 0f, 0.75f);
            _Streams.Clear();

            CBase.Video.Close(_Video);
            _Video = -1;

            CBase.Drawing.RemoveTexture(ref _Vidtex);

            _Timer.Stop();
            _Timer.Reset();
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