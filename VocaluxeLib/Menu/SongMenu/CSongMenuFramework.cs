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

using System.Diagnostics;
using System.Xml.Serialization;
using VocaluxeLib.PartyModes;
using VocaluxeLib.Songs;

namespace VocaluxeLib.Menu.SongMenu
{
    [XmlType("SongMenu")]
    public struct SThemeSongMenu
    {
        [XmlAttribute(AttributeName = "Name")] public string Name;

        public string CoverBackground;
        public string CoverBigBackground;
        public string DuetIcon;
        public string VideoIcon;
        public string MedleyCalcIcon;
        public string MedleyTagIcon;
        public SThemeColor Color;

        //public SThemeSongMenuBook songMenuBook;
        //public SThemeSongMenuDreidel songMenuDreidel;
        public SThemeSongMenuList SongMenuList;
        public SThemeSongMenuTileBoard SongMenuTileBoard;
    }
    public struct SThemeSongMenuList
    {
        /// <summary>
        ///     Number of visible songs in list
        /// </summary>
        public int ListLength;

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

        public SThemeText TextArtist;
        public SThemeText TextTitle;
        public SThemeText TextSongLength;

        public SThemeStatic StaticCoverBig;
        public SThemeStatic StaticTextBG;
        public SThemeStatic StaticDuetIcon;
        public SThemeStatic StaticVideoIcon;
        public SThemeStatic StaticMedleyCalcIcon;
        public SThemeStatic StaticMedleyTagIcon;
    }


    public struct SThemeSongMenuTileBoard
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

        public SThemeText TextArtist;
        public SThemeText TextTitle;
        public SThemeText TextSongLength;

        public SThemeStatic StaticCoverBig;
        public SThemeStatic StaticTextBG;
        public SThemeStatic StaticDuetIcon;
        public SThemeStatic StaticVideoIcon;
        public SThemeStatic StaticMedleyCalcIcon;
        public SThemeStatic StaticMedleyTagIcon;
    }

    public abstract class CSongMenuFramework : CMenuElementBase, ISongMenu
    {
        protected readonly int _PartyModeID;
        protected SThemeSongMenu _Theme;

        public bool ThemeLoaded { get; private set; }

        public bool Selectable
        {
            get { return Visible; }
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
                    CBase.BackgroundMusic.SetPlayingPreview(false);
                }

                _PreviewNrInternal = value;
            }
        }

        protected int _AutoplayDelayinMs = CBase.Config.GetAutoplayPreviewDelay();
        protected System.Timers.Timer _AutoplayTimer = new System.Timers.Timer();

        private SColorF _ColorInternal;
        protected SColorF _Color
        {
            get { return _ColorInternal; }
        }

        public int GetPreviewSong()
        {
            return _PreviewNr;
        }

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
            _Theme = theme;

            ThemeLoaded = true;
        }

        #region Theme
        public string GetThemeName()
        {
            return _Theme.Name;
        }

        public virtual object GetTheme()
        {
            return _Theme;
        }
        #endregion Theme

        public virtual void Init()
        {
            _ResetPreview(false);
            _InitializeAutoplayTimer();
            _Initialized = true;
        }

        public abstract void Update(SScreenSongOptions songOptions);

        public abstract void OnShow();

        public virtual void OnHide()
        {
            EScreen check = CBase.Graphics.GetNextScreenType();
            if (CBase.Graphics.GetNextScreenType() == EScreen.Sing)
                _ResetPreview(false);
            else if (CBase.Graphics.GetNextScreenType() != EScreen.Names || CBase.Config.GetBackgroundMusicStatus() == EBackgroundMusicOffOn.TR_CONFIG_OFF)
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

        public virtual bool IsMouseOverSelectedSong(SMouseEvent mEvent)
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

        public virtual void UnloadSkin() {}

        public virtual void LoadSkin()
        {
            Init();

            _Theme.Color.Get(_PartyModeID, out _ColorInternal);
        }

        public void ReloadSkin()
        {
            UnloadSkin();
            LoadSkin();
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

            CBase.BackgroundMusic.LoadPreview(song, song.Preview.StartTime);
        }

        protected void _ResetPreview(bool playBGagain = true)
        {
            if (_PreviewNrInternal == -1)
                return;

            CBase.BackgroundMusic.StopPreview();
            CBase.Sound.SetGlobalVolume(CBase.Config.GetMusicVolume(EMusicType.Background));
            if (playBGagain)
                CBase.BackgroundMusic.Play();

            //Make sure we don't have a preview here otherwise a change won't be recognized
            //(e.g. leave a category with one song and set preview to 0 --> previewOld=previewNew=0 --> No change --> Old data shown
            //Use internal nr because this function gets called from withing setPreviewNr
            _PreviewNrInternal = -1;
        }

        protected void _PreviewSelectedSong()
        {
            _PreviewNr = _SelectionNr;
        }

        protected void _InitializeAutoplayTimer()
        {
            _AutoplayTimer.Interval = _AutoplayDelayinMs;
            _AutoplayTimer.AutoReset = false;
            _AutoplayTimer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
            {
                _PreviewSelectedSong();
            };
        }

        protected void _AutoplayPreviewIfEnabled()
        {
            if (CBase.Config.GetAutoplayPreviews() == EOffOn.TR_CONFIG_ON)
            {
                _PlayPreviewAfterDelay();
            }
        }        

        protected void _PlayPreviewAfterDelay()
        {
            if (!_AutoplayTimer.Enabled)
            {
                _AutoplayTimer.Start();
            }
            else
            {
                _AutoplayTimer.Stop();
                _AutoplayTimer.Start();
            }
        }

        #region ThemeEdit
        public void MoveElement(int stepX, int stepY)
        {
            X += stepX;
            Y += stepY;
            Init();
        }

        public void ResizeElement(int stepW, int stepH)
        {
            W += stepW;
            if (W < 100)
                W = 100;

            H += stepH;
            if (H < 100)
                H = 100;

            Init();
        }
        #endregion ThemeEdit
    }
}