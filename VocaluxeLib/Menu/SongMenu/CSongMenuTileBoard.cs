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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using VocaluxeLib.Draw;
using VocaluxeLib.PartyModes;
using VocaluxeLib.Songs;

namespace VocaluxeLib.Menu.SongMenu
{
    class CSongMenuTileBoard : CSongMenuFramework
    {
        private SRectF _ScrollRect;
        private List<CStatic> _Tiles;
        private CStatic _CoverBig;
        private CStatic _TextBG;
        private CStatic _DuetIcon;
        private CStatic _RapIcon;
        private CStatic _VideoIcon;
        private CStatic _MedleyCalcIcon;
        private CStatic _MedleyTagIcon;
        private CStatic _InstrumentalIcon;
        private CStatic _VocalsIcon;

        private CTextureRef _CoverBigBGTexture;
        private CTextureRef _CoverBGTexture;

        private CText _Artist;
        private CText _Title;
        private CText _SongLength;
        private CText _SongYear;

        private int _NumW;
        private int _NumH;

        private int _TileW;
        private int _TileH;

        // Offset is the song or categoryNr of the tile in the left upper corner
        private int _Offset;

        private float _Length = -1f;
        private int _LastKnownElements;
        private int _LastKnownCategory;

        private bool _MouseWasInRect;

        private readonly List<IMenuElement> _SubElements = new List<IMenuElement>();

        public override float SelectedTileZoomFactor
        {
            get { return 1.2f; }
        }

        protected override int _SelectionNr
        {
            set
            {
                var max = CBase.Songs.IsInCategory() ? CBase.Songs.GetNumSongsVisible() : CBase.Songs.GetNumCategories();
                base._SelectionNr = value.Clamp(-1, max - 1, true);
                //Update list in case we scrolled 
                _UpdateList();

                _UpdateTileSelection();
            }
        }

        public override bool SmallView
        {
            set
            {
                if (SmallView == value)
                {
                    return;
                }

                base.SmallView = value;
                _InitTiles();
                _UpdateList(true);
                _UpdateTileSelection();
            }
        }

        protected override int _PreviewNr
        {
            set
            {
                if (value == base._PreviewNr)
                {
                    if (!CBase.BackgroundMusic.IsPlaying() && value != -1)
                    {
                        CBase.BackgroundMusic.Play();
                    }

                    return;
                }

                base._PreviewNr = value;
                _UpdatePreview();
            }
        }

        public CSongMenuTileBoard(int partyModeId) : base(partyModeId) { }

        public CSongMenuTileBoard(SThemeSongMenu theme, int partyModeId) : base(theme, partyModeId)
        {
            _Artist = new CText(_Theme.SongMenuTileBoard.TextArtist, _PartyModeId);
            _Title = new CText(_Theme.SongMenuTileBoard.TextTitle, _PartyModeId);
            _SongLength = new CText(_Theme.SongMenuTileBoard.TextSongLength, _PartyModeId);
            _SongYear = new CText(_Theme.SongMenuTileBoard.TextSongYear, _PartyModeId);
            _CoverBig = new CStatic(_Theme.SongMenuTileBoard.StaticCoverBig, _PartyModeId);
            _TextBG = new CStatic(_Theme.SongMenuTileBoard.StaticTextBG, _PartyModeId);
            _DuetIcon = new CStatic(_Theme.SongMenuTileBoard.StaticDuetIcon, _PartyModeId);
            _RapIcon = new CStatic(_Theme.SongMenuTileBoard.StaticRapIcon, _PartyModeId);
            _VideoIcon = new CStatic(_Theme.SongMenuTileBoard.StaticVideoIcon, _PartyModeId);
            _MedleyCalcIcon = new CStatic(_Theme.SongMenuTileBoard.StaticMedleyCalcIcon, _PartyModeId);
            _MedleyTagIcon = new CStatic(_Theme.SongMenuTileBoard.StaticMedleyTagIcon, _PartyModeId);
            _InstrumentalIcon = new CStatic(_Theme.SongMenuTileBoard.StaticInstrumentalIcon, _PartyModeId);
            _VocalsIcon = new CStatic(_Theme.SongMenuTileBoard.StaticVocalsIcon, _PartyModeId);
            _SubElements.AddRange(new IMenuElement[]
                { _Artist, _Title, _SongLength, _SongYear, _DuetIcon, _RapIcon, _VideoIcon, _MedleyCalcIcon, _MedleyTagIcon, _InstrumentalIcon, _VocalsIcon });
        }

        private void _UpdateTileSelection()
        {
            foreach (var tile in _Tiles)
            {
                tile.Selected = false;
            }

            var tileNr = _SelectionNr - _Offset;
            if (tileNr >= 0 && tileNr < _Tiles.Count)
            {
                _Tiles[tileNr].Selected = true;
            }
        }

        private void _ReadSubTheme()
        {
            _Theme.SongMenuTileBoard.TextArtist = (SThemeText)_Artist.GetTheme();
            _Theme.SongMenuTileBoard.TextSongLength = (SThemeText)_SongLength.GetTheme();
            _Theme.SongMenuTileBoard.TextSongYear = (SThemeText)_SongYear.GetTheme();
            _Theme.SongMenuTileBoard.TextTitle = (SThemeText)_Title.GetTheme();
            _Theme.SongMenuTileBoard.StaticCoverBig = (SThemeStatic)_CoverBig.GetTheme();
            _Theme.SongMenuTileBoard.StaticDuetIcon = (SThemeStatic)_DuetIcon.GetTheme();
            _Theme.SongMenuTileBoard.StaticRapIcon = (SThemeStatic)_RapIcon.GetTheme();
            _Theme.SongMenuTileBoard.StaticMedleyCalcIcon = (SThemeStatic)_MedleyCalcIcon.GetTheme();
            _Theme.SongMenuTileBoard.StaticMedleyTagIcon = (SThemeStatic)_MedleyTagIcon.GetTheme();
            _Theme.SongMenuTileBoard.StaticTextBG = (SThemeStatic)_TextBG.GetTheme();
            _Theme.SongMenuTileBoard.StaticVideoIcon = (SThemeStatic)_VideoIcon.GetTheme();
            _Theme.SongMenuTileBoard.StaticInstrumentalIcon = (SThemeStatic)_InstrumentalIcon.GetTheme();
            _Theme.SongMenuTileBoard.StaticVocalsIcon = (SThemeStatic)_VocalsIcon.GetTheme();
        }

        public override object GetTheme()
        {
            _ReadSubTheme();
            return base.GetTheme();
        }

        public override void Init()
        {
            base.Init();

            _PreviewNr = -1;
            _InitTiles();
        }

        private void _InitTiles()
        {
            if (SmallView)
            {
                _NumH = _Theme.SongMenuTileBoard.NumHsmall;
                _NumW = _Theme.SongMenuTileBoard.NumWsmall;
                MaxRect = _Theme.SongMenuTileBoard.TileRectSmall;
            }
            else
            {
                _NumH = _Theme.SongMenuTileBoard.NumH;
                _NumW = _Theme.SongMenuTileBoard.NumW;
                MaxRect = _Theme.SongMenuTileBoard.TileRect;
            }

            _TileW = (int)((Rect.W - _Theme.SongMenuTileBoard.SpaceW * (_NumW - 1)) / _NumW);
            _TileH = (int)((Rect.H - _Theme.SongMenuTileBoard.SpaceH * (_NumH - 1)) / _NumH);

            _CoverBGTexture = CBase.Themes.GetSkinTexture(_Theme.CoverBackground, _PartyModeId);
            _CoverBigBGTexture = CBase.Themes.GetSkinTexture(_Theme.CoverBigBackground, _PartyModeId);

            _Tiles = new List<CStatic>();
            for (var i = 0; i < _NumH; i++)
            {
                for (var j = 0; j < _NumW; j++)
                {
                    var rect = new SRectF(Rect.X + j * (_TileW + _Theme.SongMenuTileBoard.SpaceW), Rect.Y + i * (_TileH + _Theme.SongMenuTileBoard.SpaceH), _TileW, _TileH, Rect.Z);
                    var tile = new CStatic(_PartyModeId, _CoverBGTexture, _Color, rect);
                    _Tiles.Add(tile);
                }
            }

            _ScrollRect = CBase.Settings.GetRenderRect();
        }

        public override void Update(SScreenSongOptions songOptions)
        {
            if (songOptions.Selection.RandomOnly)
            {
                _PreviewSelectedSong();
            }

            if (_Length < 0 && CBase.Songs.IsInCategory() && CBase.BackgroundMusic.GetLength() > 0)
            {
                _UpdateLength(CBase.Songs.GetVisibleSong(_PreviewNr));
            }
        }

        private void _UpdatePreview()
        {
            //First hide everything so we just have to set what we actually want
            _CoverBig.Texture = _CoverBigBGTexture;
            _Artist.Text = string.Empty;
            _Title.Text = string.Empty;
            _SongLength.Text = string.Empty;
            _SongYear.Text = string.Empty;
            _DuetIcon.Visible = false;
            _RapIcon.Visible = false;
            _VideoIcon.Visible = false;
            _MedleyCalcIcon.Visible = false;
            _MedleyTagIcon.Visible = false;
            _InstrumentalIcon.Visible = false;
            _VocalsIcon.Visible = false;
            _Length = -1f;

            //Check if nothing is selected (for preview)
            if (_PreviewNr < 0)
            {
                return;
            }

            if (CBase.Songs.IsInCategory())
            {
                var song = CBase.Songs.GetVisibleSong(_PreviewNr);
                //Check if we have a valid song (song still visible, index >=0 etc is checked by framework)
                if (song == null)
                {
                    //Display at least the category
                    var category = CBase.Songs.GetCategory(CBase.Songs.GetCurrentCategoryIndex());
                    //Check if we have a valid category
                    if (category == null)
                    {
                        return;
                    }

                    _CoverBig.Texture = category.CoverTextureBig;
                    _Artist.Text = category.Name;
                    return;
                }

                _CoverBig.Texture = song.CoverTextureBig;
                _Artist.Text = song.Artist;
                _Title.Text = song.Title;
                _SongYear.Text = song.Year;
                _DuetIcon.Visible = song.IsDuet;
                _RapIcon.Visible = song.IsRap;
                _VideoIcon.Visible = song.Video != "";
                _MedleyCalcIcon.Visible = song.Medley.Source == EDataSource.Calculated;
                _MedleyTagIcon.Visible = song.Medley.Source == EDataSource.Tag;
                _InstrumentalIcon.Visible = song.HasInstrumental();
                _VocalsIcon.Visible = song.HasVocals();

                _UpdateLength(song);
            }
            else
            {
                var category = CBase.Songs.GetCategory(_PreviewNr);
                //Check if we have a valid category
                if (category == null)
                {
                    return;
                }

                _CoverBig.Texture = category.CoverTextureBig;
                _Artist.Text = category.Name;

                var num = category.GetNumSongsNotSung();
                var songOrSongs = num == 1 ? "TR_SCREENSONG_NUMSONG" : "TR_SCREENSONG_NUMSONGS";
                _Title.Text = CBase.Language.Translate(songOrSongs).Replace("%v", num.ToString());
            }
        }

        private void _UpdateLength(CSong song)
        {
            if (song == null)
            {
                return;
            }

            var time = CBase.BackgroundMusic.GetLength();
            if (Math.Abs(song.End) > 0.001)
            {
                time = song.End;
            }

            // The audiobackend is ready to return the length
            if (time > 0)
            {
                time -= song.Start;
                var min = (int)Math.Floor(time / 60f);
                var sec = (int)(time - min * 60f);
                _SongLength.Text = min.ToString("00") + ":" + sec.ToString("00");
                _Length = time;
            }
            else
            {
                _SongLength.Text = "...";
            }
        }

        public override void OnShow()
        {
            _LastKnownElements = -1; //Force refresh of list
            if (!CBase.Songs.IsInCategory())
            {
                if ((CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_OFF && CBase.Songs.GetNumCategories() > 0) || CBase.Songs.GetNumCategories() == 1)
                {
                    _EnterCategory(0);
                }
            }

            if (CBase.Songs.IsInCategory())
            {
                SetSelectedSong(_SelectionNr < 0 ? 0 : _SelectionNr);
            }
            else
            {
                SetSelectedCategory(_SelectionNr < 0 ? 0 : _SelectionNr);
            }

            _PreviewSelectedSong();
            _UpdateListIfRequired();
        }

        public override bool HandleInput(ref SKeyEvent keyEvent, SScreenSongOptions options)
        {
            if (keyEvent.KeyPressed)
            {
                return false;
            }

            var moveAllowed = !options.Selection.RandomOnly || (options.Selection.CategoryChangeAllowed && !CBase.Songs.IsInCategory());
            var catChangePossible = CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_ON && options.Selection.CategoryChangeAllowed;

            //If nothing selected set a reasonable default value
            if (keyEvent.IsArrowKey() && moveAllowed && _SelectionNr < 0)
            {
                _SelectionNr = _PreviewNr < 0 ? _Offset : _PreviewNr;
            }

            switch (keyEvent.Key)
            {
                case Keys.Enter:
                    if (CBase.Songs.IsInCategory())
                    {
                        if (_SelectionNr >= 0 && _PreviewNr != _SelectionNr)
                        {
                            _PreviewSelectedSong();
                            keyEvent.Handled = true;
                        }
                    }
                    else
                    {
                        _EnterCategory(_PreviewNr);
                        keyEvent.Handled = true;
                    }

                    break;

                case Keys.Escape:
                case Keys.Back:
                    if (CBase.Songs.IsInCategory() && catChangePossible)
                    {
                        LeaveSelectedCategory();
                        keyEvent.Handled = true;
                    }

                    break;

                case Keys.PageUp:
                    if (catChangePossible && CBase.Songs.IsInCategory() && (keyEvent.Mod == EModifier.Ctrl || keyEvent.Mod == EModifier.Alt))
                    {
                        _PrevCategory();
                        keyEvent.Handled = true;
                    }
                    else if (moveAllowed)
                    {
                        var numSongsPerPage = _NumH * _NumW;
                        if (_SelectionNr - numSongsPerPage <= 0)
                        {
                            _SelectionNr = 0;
                        }
                        else
                        {
                            _SelectionNr = _SelectionNr - numSongsPerPage;
                        }

                        keyEvent.Handled = true;
                    }

                    break;

                case Keys.PageDown:
                    if (catChangePossible && CBase.Songs.IsInCategory() && (keyEvent.Mod == EModifier.Ctrl || keyEvent.Mod == EModifier.Alt))
                    {
                        _NextCategory();
                        keyEvent.Handled = true;
                    }
                    else if (moveAllowed)
                    {
                        var numSongsPerPage = _NumH * _NumW;
                        var maxCount_PageDown = CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_ON && !CBase.Songs.IsInCategory() ? CBase.Songs.GetNumCategories()
                            : CBase.Songs.GetNumSongsVisible();
                        if (_SelectionNr + numSongsPerPage >= maxCount_PageDown)
                        {
                            _SelectionNr = maxCount_PageDown - 1;
                        }
                        else
                        {
                            _SelectionNr = _SelectionNr + numSongsPerPage;
                        }

                        keyEvent.Handled = true;
                    }

                    break;

                case Keys.Home:
                    if (moveAllowed)
                    {
                        _SelectionNr = 0;
                        keyEvent.Handled = true;
                    }

                    break;

                case Keys.End:
                    var maxCount_End = CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_ON && !CBase.Songs.IsInCategory() ? CBase.Songs.GetNumCategories()
                        : CBase.Songs.GetNumSongsVisible();
                    if (moveAllowed)
                    {
                        _SelectionNr = maxCount_End - 1;
                        keyEvent.Handled = true;
                    }

                    break;

                case Keys.Left:
                    //Check for >0 so we do not allow selection of nothing (-1)
                    if (_SelectionNr > 0 && moveAllowed)
                    {
                        _SelectionNr--;
                        _AutoplayPreviewIfEnabled();
                        keyEvent.Handled = true;
                    }

                    break;

                case Keys.Right:
                    if (moveAllowed)
                    {
                        _SelectionNr++;
                        _AutoplayPreviewIfEnabled();
                        keyEvent.Handled = true;
                    }

                    break;

                case Keys.Up:
                    if (keyEvent.ModShift)
                    {
                        if (catChangePossible)
                        {
                            _PrevCategory();
                            keyEvent.Handled = true;
                        }
                    }
                    else if (_SelectionNr >= _NumW && moveAllowed)
                    {
                        _SelectionNr -= _NumW;
                        _AutoplayPreviewIfEnabled();
                        keyEvent.Handled = true;
                    }

                    break;

                case Keys.Down:
                    if (keyEvent.ModShift)
                    {
                        if (catChangePossible)
                        {
                            _NextCategory();
                            keyEvent.Handled = true;
                        }
                    }
                    else if (moveAllowed)
                    {
                        _SelectionNr += _NumW;
                        _AutoplayPreviewIfEnabled();
                        keyEvent.Handled = true;
                    }

                    break;
            }

            if (!CBase.Songs.IsInCategory())
            {
                _PreviewSelectedSong();
            }

            return keyEvent.Handled;
        }

        public override bool HandleMouse(ref SMouseEvent mouseEvent, SScreenSongOptions songOptions)
        {
            if (!songOptions.Selection.RandomOnly || (!CBase.Songs.IsInCategory() && songOptions.Selection.CategoryChangeAllowed))
            {
                if (mouseEvent.Wheel != 0 && CHelper.IsInBounds(_ScrollRect, mouseEvent))
                {
                    _UpdateList(_Offset + _NumW * mouseEvent.Wheel);
                }

                var lastSelection = _SelectionNr;
                var i = 0;
                var somethingSelected = false;
                foreach (var tile in _Tiles)
                {
                    if (tile.Texture != _CoverBGTexture && CHelper.IsInBounds(tile.Rect, mouseEvent))
                    {
                        somethingSelected = true;
                        _SelectionNr = i + _Offset;
                        if (!CBase.Songs.IsInCategory())
                        {
                            _PreviewNr = i + _Offset;
                        }

                        break;
                    }

                    i++;
                }

                //Reset selection only if we moved out of the rect to avoid loosing it when selecting random songs
                if (_MouseWasInRect && !somethingSelected)
                {
                    _SelectionNr = -1;
                }

                if (mouseEvent.Sender == ESender.WiiMote && _SelectionNr != lastSelection && _SelectionNr != -1)
                {
                    CBase.Controller.SetRumble(0.050f);
                }
            }

            _MouseWasInRect = CHelper.IsInBounds(Rect, mouseEvent);

            if (mouseEvent.RB)
            {
                if (CBase.Songs.IsInCategory() && CBase.Songs.GetNumCategories() > 0 && CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_ON &&
                    songOptions.Selection.CategoryChangeAllowed)
                {
                    LeaveSelectedCategory();
                    return true;
                }

                if (CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_OFF && !songOptions.Selection.PartyMode)
                {
                    CBase.Graphics.FadeTo(EScreen.Main);
                    return true;
                }
            }
            else if (mouseEvent.LB)
            {
                if (_SelectionNr >= 0 && _MouseWasInRect)
                {
                    if (CBase.Songs.IsInCategory())
                    {
                        if (_PreviewNr == _SelectionNr)
                        {
                            return false;
                        }

                        _PreviewSelectedSong();
                    }
                    else
                    {
                        EnterSelectedCategory();
                    }

                    return true;
                }
            }

            return false;
        }

        public override void Draw()
        {
            foreach (var tile in _Tiles)
            {
                if (tile.Selected)
                {
                    tile.Draw(EAspect.Crop, SelectedTileZoomFactor, -0.1f);
                }
                else
                {
                    var aspect = tile.Texture != _CoverBGTexture ? EAspect.Crop : EAspect.Stretch;
                    tile.Draw(aspect);
                }
            }

            _TextBG.Draw();

            CTextureRef vidtex = null;
            if (CBase.Config.GetVideoPreview() == EOffOn.TR_CONFIG_ON)
            {
                vidtex = CBase.BackgroundMusic.IsPlayingPreview() ? CBase.BackgroundMusic.GetVideoTexture() : null;
            }

            if (vidtex != null)
            {
                if (vidtex.Color.A < 1)
                {
                    _CoverBig.Draw(EAspect.Crop);
                }

                var rect = CHelper.FitInBounds(_CoverBig.Rect, vidtex.OrigAspect, EAspect.Crop);
                CBase.Drawing.DrawTexture(vidtex, rect, vidtex.Color, _CoverBig.Rect);
                CBase.Drawing.DrawTextureReflection(vidtex, rect, vidtex.Color, _CoverBig.Rect, _CoverBig.ReflectionSpace, _CoverBig.ReflectionHeight);
            }
            else
            {
                _CoverBig.Draw(EAspect.Crop);
            }

            foreach (var element in _SubElements)
            {
                element.Draw();
            }
        }

        public override CStatic GetSelectedSongCover()
        {
            return _Tiles.FirstOrDefault(tile => tile.Selected);
        }

        protected override void _EnterCategory(int categoryNr)
        {
            base._EnterCategory(categoryNr);

            SetSelectedSong(0);
            _UpdateListIfRequired();
        }

        protected override void _LeaveCategory()
        {
            base._LeaveCategory();
            _UpdateListIfRequired();
        }

        private void _NextCategory()
        {
            if (CBase.Songs.IsInCategory())
            {
                CBase.Songs.NextCategory();
                _EnterCategory(CBase.Songs.GetCurrentCategoryIndex());
            }
        }

        private void _PrevCategory()
        {
            if (CBase.Songs.IsInCategory())
            {
                CBase.Songs.PrevCategory();
                _EnterCategory(CBase.Songs.GetCurrentCategoryIndex());
            }
        }

        private void _UpdateListIfRequired()
        {
            var curElements = CBase.Songs.IsInCategory() ? CBase.Songs.GetNumSongsVisible() : CBase.Songs.GetNumCategories();
            if (_LastKnownElements == curElements && _LastKnownCategory == CBase.Songs.GetCurrentCategoryIndex())
            {
                return;
            }

            _LastKnownCategory = CBase.Songs.GetCurrentCategoryIndex();
            _LastKnownElements = curElements;
            CBase.Songs.UpdateRandomSongList();
            _UpdateList(true);
        }

        private int _GetOffsetForPosition(int index, int rowNum = 0)
        {
            return index / _NumW * _NumW - _NumW * rowNum.Clamp(0, _NumH - 1, true);
        }

        private void _UpdateList(bool force = false)
        {
            int offset;
            if (_SelectionNr < _Offset && _SelectionNr >= 0)
            {
                offset = _GetOffsetForPosition(_SelectionNr);
            }
            else if (_SelectionNr >= _Offset + _NumW * _NumH)
            {
                offset = _GetOffsetForPosition(_SelectionNr, _NumH);
            }
            else
            {
                offset = _Offset;
            }

            _UpdateList(offset, force);
        }

        private void _UpdateList(int offset, bool force = false)
        {
            var isInCategory = CBase.Songs.IsInCategory();
            var itemCount = isInCategory ? CBase.Songs.GetNumSongsVisible() : CBase.Songs.GetNumCategories();

            offset = offset.Clamp(0, _GetOffsetForPosition(itemCount, _NumH - 1), true);

            if (offset == _Offset && !force)
            {
                return;
            }

            for (var i = 0; i < _Tiles.Count; i++)
            {
                if (itemCount > i + offset)
                {
                    _Tiles[i].Texture = isInCategory ? CBase.Songs.GetVisibleSong(i + offset).CoverTextureSmall : CBase.Songs.GetCategory(i + offset).CoverTextureSmall;
                    _Tiles[i].Color = new SColorF(1f, 1f, 1f, 1f);
                }
                else
                {
                    _Tiles[i].Texture = _CoverBGTexture;
                    _Tiles[i].Color = _Color;
                }
            }

            _Offset = offset;
        }

        public override void LoadSkin()
        {
            base.LoadSkin();
            foreach (var themeable in _SubElements.OfType<IThemeable>())
            {
                themeable.LoadSkin();
            }

            // Those are drawn seperately so they are not in the above list
            _CoverBig.LoadSkin();
            _TextBG.LoadSkin();

            Init();
        }
    }
}