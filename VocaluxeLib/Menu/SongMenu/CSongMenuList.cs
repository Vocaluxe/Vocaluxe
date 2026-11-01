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
using System.Diagnostics;
using System.Linq;
using VocaluxeLib.Draw;
using VocaluxeLib.PartyModes;
using VocaluxeLib.Songs;

namespace VocaluxeLib.Menu.SongMenu
{
    class CSongMenuList : CSongMenuFramework
    {
        private SRectF _ScrollRect;
        private List<CStatic> _Tiles;
        private List<CText> _Texts;
        private List<CText> _SubTexts;
        private List<CStatic> _RowBackgrounds;
        private readonly CStatic _CoverBig;
        private readonly CStatic _TextBG;
        private readonly CStatic _DuetIcon;
        private readonly CStatic _RapIcon;
        private readonly CStatic _VideoIcon;
        private readonly CStatic _MedleyCalcIcon;
        private readonly CStatic _MedleyTagIcon;
        private readonly CStatic _InstrumentalIcon;
        private readonly CStatic _VocalsIcon;

        private CTextureRef _CoverBigBGTexture;
        private CTextureRef _CoverBGTexture;

        private readonly CText _Artist;
        private readonly CText _Title;
        private readonly CText _SongLength;
        private readonly CText _SongYear;

        private readonly CStatic _ScrollBar;
        private readonly CStatic _ScrollBarThumb;
        private CText _ScrollText;
        private CText _ScrollSubText;

        private float _SpaceW;
        private float _SpaceH;

        private float _TileH;

        private const float _SelectedRowScale = 1.3f;
        private const float _ExpandDuration = 120f;
        private readonly Stopwatch _ExpandTimer = new Stopwatch();
        private int _ExpandedNr = -1;
        private int _PrevExpandedNr = -1;

        private int _ListLength;

        // Offset is the song or categoryNr of the tile in the left upper corner
        private int _Offset;

        private float _Length = -1f;
        private int _LastKnownElements;
        private int _LastKnownCategory;

        private bool _MouseWasInRect;

        private readonly List<IMenuElement> _SubElements = new List<IMenuElement>();

        public override float SelectedTileZoomFactor
        {
            get { return 1f; }
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
                _UpdateExpandedRow();
            }
        }

        public CSongMenuList(SThemeSongMenu theme, int partyModeId) : base(theme, partyModeId)
        {
            _ListLength = _Theme.SongMenuList.ListLength;
            _SpaceW = _Theme.SongMenuList.SpaceW;
            _SpaceH = _Theme.SongMenuList.SpaceH;
            _Artist = new CText(_Theme.SongMenuList.TextArtist, _PartyModeId);
            _Title = new CText(_Theme.SongMenuList.TextTitle, _PartyModeId);
            _SongLength = new CText(_Theme.SongMenuList.TextSongLength, _PartyModeId);
            _SongYear = new CText(_Theme.SongMenuList.TextSongYear, _PartyModeId);
            _CoverBig = new CStatic(_Theme.SongMenuList.StaticCoverBig, _PartyModeId);
            _TextBG = new CStatic(_Theme.SongMenuList.StaticTextBG, _PartyModeId);
            _DuetIcon = new CStatic(_Theme.SongMenuList.StaticDuetIcon, _PartyModeId);
            _RapIcon = new CStatic(_Theme.SongMenuList.StaticRapIcon, _PartyModeId);
            _VideoIcon = new CStatic(_Theme.SongMenuList.StaticVideoIcon, _PartyModeId);
            _MedleyCalcIcon = new CStatic(_Theme.SongMenuList.StaticMedleyCalcIcon, _PartyModeId);
            _MedleyTagIcon = new CStatic(_Theme.SongMenuList.StaticMedleyTagIcon, _PartyModeId);
            _InstrumentalIcon = new CStatic(_Theme.SongMenuList.StaticInstrumentalIcon, _PartyModeId);
            _VocalsIcon = new CStatic(_Theme.SongMenuList.StaticVocalsIcon, _PartyModeId);
            _SubElements.AddRange(new IMenuElement[]
                { _Artist, _Title, _SongLength, _SongYear, _DuetIcon, _RapIcon, _VideoIcon, _MedleyCalcIcon, _MedleyTagIcon, _InstrumentalIcon, _VocalsIcon });

            if (_Theme.SongMenuList.StaticScrollBar.HasValue)
            {
                _ScrollBar = new CStatic(_Theme.SongMenuList.StaticScrollBar.Value, _PartyModeId);
            }

            if (_Theme.SongMenuList.StaticScrollBarThumb.HasValue)
            {
                _ScrollBarThumb = new CStatic(_Theme.SongMenuList.StaticScrollBarThumb.Value, _PartyModeId);
            }
        }

        private void _ReadSubTheme()
        {
            _Theme.SongMenuList.TextArtist = (SThemeText)_Artist.GetTheme();
            _Theme.SongMenuList.TextSongLength = (SThemeText)_SongLength.GetTheme();
            _Theme.SongMenuList.TextSongYear = (SThemeText)_SongYear.GetTheme();
            _Theme.SongMenuList.TextTitle = (SThemeText)_Title.GetTheme();
            _Theme.SongMenuList.StaticCoverBig = (SThemeStatic)_CoverBig.GetTheme();
            _Theme.SongMenuList.StaticDuetIcon = (SThemeStatic)_DuetIcon.GetTheme();
            _Theme.SongMenuList.StaticRapIcon = (SThemeStatic)_RapIcon.GetTheme();
            _Theme.SongMenuList.StaticMedleyCalcIcon = (SThemeStatic)_MedleyCalcIcon.GetTheme();
            _Theme.SongMenuList.StaticMedleyTagIcon = (SThemeStatic)_MedleyTagIcon.GetTheme();
            _Theme.SongMenuList.StaticInstrumentalIcon = (SThemeStatic)_InstrumentalIcon.GetTheme();
            _Theme.SongMenuList.StaticVocalsIcon = (SThemeStatic)_VocalsIcon.GetTheme();
            _Theme.SongMenuList.StaticTextBG = (SThemeStatic)_TextBG.GetTheme();
            _Theme.SongMenuList.StaticVideoIcon = (SThemeStatic)_VideoIcon.GetTheme();

            if (_ScrollBar != null)
            {
                _Theme.SongMenuList.StaticScrollBar = (SThemeStatic)_ScrollBar.GetTheme();
            }

            if (_ScrollBarThumb != null)
            {
                _Theme.SongMenuList.StaticScrollBarThumb = (SThemeStatic)_ScrollBarThumb.GetTheme();
            }
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
            MaxRect = SmallView ? _Theme.SongMenuList.TileRectSmall : _Theme.SongMenuList.TileRect;

            _TileH = (MaxRect.H - _SpaceH * (_ListLength - 1)) / (_ListLength - 1 + _SelectedRowScale);

            _CoverBGTexture = CBase.Themes.GetSkinTexture(_Theme.CoverBackground, _PartyModeId);
            _CoverBigBGTexture = CBase.Themes.GetSkinTexture(_Theme.CoverBigBackground, _PartyModeId);

            //Create cover tiles
            _RowBackgrounds = new List<CStatic>();
            _Tiles = new List<CStatic>();
            _Texts = new List<CText>();
            _SubTexts = new List<CText>();

            for (var i = 0; i < _ListLength; i++)
            {
                //Create Cover
                var rowBackground = new CStatic(_PartyModeId, _CoverBGTexture, _Color, Rect);
                rowBackground.Color = new SColorF(1f, 1f, 1f, 0f);
                _RowBackgrounds.Add(rowBackground);

                var rect = new SRectF(Rect.X, Rect.Y, _TileH, _TileH, Rect.Z);
                var tile = new CStatic(_PartyModeId, _CoverBGTexture, _Color, rect);
                _Tiles.Add(tile);

                //Create text
                _Texts.Add(_CreateRowText(1f));
                _SubTexts.Add(_CreateRowText(0.7f));
            }

            _ScrollText = _CreateRowText(1f);
            _ScrollSubText = _CreateRowText(0.7f);
            _ScrollRect = CBase.Settings.GetRenderRect();

            _ExpandTimer.Reset();
            _PrevExpandedNr = -1;
        }

        private CText _CreateRowText(float alpha)
        {
            var color = new SColorF(_Artist.Color.R, _Artist.Color.G, _Artist.Color.B, _Artist.Color.A * alpha);
            var text = new CText(Rect.X, Rect.Y, Rect.Z, _TileH, Rect.W, EAlignment.Left, EStyle.Normal, "Normal", color, "");
            text.ResizeAlign = EHAlignment.Center;
            return text;
        }

        private float _GetExpandProgress()
        {
            if (!_ExpandTimer.IsRunning)
            {
                return 1f;
            }

            var progress = _ExpandTimer.ElapsedMilliseconds / _ExpandDuration;
            if (progress >= 1f)
            {
                _ExpandTimer.Reset();
                _PrevExpandedNr = -1;
                return 1f;
            }

            return 1f - (1f - progress) * (1f - progress);
        }

        private float _GetRowHeight(int index, float progress)
        {
            var extra = _TileH * (_SelectedRowScale - 1f);
            if (index == _ExpandedNr)
            {
                return _TileH + extra * progress;
            }

            if (index == _PrevExpandedNr)
            {
                return _TileH + extra * (1f - progress);
            }

            return _TileH;
        }

        private SRectF _GetRowRect(int index, float position, float progress)
        {
            var y = Rect.Y + (index - position) * (_TileH + _SpaceH);
            for (var i = (int)Math.Floor(position); i < index; i++)
            {
                y += _GetRowHeight(i, progress) - _TileH;
            }

            return new SRectF(Rect.X, y, Rect.W, _GetRowHeight(index, progress), Rect.Z);
        }

        private void _LayoutRowTexts(CText title, CText artist, SRectF row, int index, bool isInCategory)
        {
            var x = row.X + row.H + _SpaceW;
            var w = Rect.Right - x - 5f;
            var itemCount = isInCategory ? CBase.Songs.GetNumSongsVisible() : CBase.Songs.GetNumCategories();

            if (index >= itemCount)
            {
                title.Text = "";
                artist.Text = "";
                return;
            }

            title.Font.Style = index == _ExpandedNr ? EStyle.Bold : EStyle.Normal;
            title.Font.Height = row.H * 0.56f;
            title.MaxRect = new SRectF(x, row.Y, w, title.Font.Height, Rect.Z);
            title.Text = _GetItemTitle(index, isInCategory);

            artist.Font.Height = row.H * 0.42f;
            artist.MaxRect = new SRectF(x, row.Y + row.H * 0.56f, w, artist.Font.Height, Rect.Z);
            artist.Text = _GetItemSubTitle(index, isInCategory);
        }

        private void _UpdateRowLayout()
        {
            var progress = _GetExpandProgress();
            var isInCategory = CBase.Songs.IsInCategory();
            for (var i = 0; i < _Tiles.Count; i++)
            {
                var index = i + _Offset;
                var row = _GetRowRect(index, _Offset, progress);

                _Tiles[i].MaxRect = new SRectF(row.X, row.Y, row.H, row.H, row.Z);
                _RowBackgrounds[i].MaxRect = new SRectF(row.X, row.Y - 4f, row.W, row.H + 8f, row.Z + 0.05f);
                _RowBackgrounds[i].Color = new SColorF(1f, 1f, 1f, index == _ExpandedNr ? 0.25f : 0f);
                _LayoutRowTexts(_Texts[i], _SubTexts[i], row, index, isInCategory);
            }
        }

        private void _UpdateExpandedRow()
        {
            var expandedNr = _SelectionNr >= 0 ? _SelectionNr : _PreviewNr;
            if (expandedNr == _ExpandedNr)
            {
                return;
            }

            _PrevExpandedNr = _ExpandedNr;
            _ExpandedNr = expandedNr;
            _ExpandTimer.Restart();
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

            _UpdateExpandedRow();
        }

        public override void Update(SScreenSongOptions songOptions)
        {
            base.Update(songOptions);
            
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
                    _Title.Text = category.Name;
                    return;
                }

                _CoverBig.Texture = song.CoverTextureBig;
                _Artist.Text = song.Artist;
                _Title.Text = song.Title;
                _SongYear.Text = string.IsNullOrEmpty(song.Year) ? "-" : song.Year;
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
                _Title.Text = category.Name;

                var num = category.GetNumSongsNotSung();
                var songOrSongs = num == 1 ? "TR_SCREENSONG_NUMSONG" : "TR_SCREENSONG_NUMSONGS";
                _Artist.Text = CBase.Language.Translate(songOrSongs).Replace("%v", num.ToString());
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
                        if (_SelectionNr - _ListLength <= 0)
                        {
                            _SelectionNr = 0;
                        }
                        else
                        {
                            _SelectionNr = _SelectionNr - _ListLength;
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
                        var maxCount_PageDown = CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_ON && !CBase.Songs.IsInCategory() ? CBase.Songs.GetNumCategories()
                            : CBase.Songs.GetNumSongsVisible();
                        if (_SelectionNr + _ListLength >= maxCount_PageDown)
                        {
                            _SelectionNr = maxCount_PageDown - 1;
                        }
                        else
                        {
                            _SelectionNr = _SelectionNr + _ListLength;
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
                    else if (_SelectionNr >= 1 && moveAllowed)
                    {
                        _SelectionNr -= 1;
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
                        _SelectionNr += 1;
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
                    _UpdateList(_Offset + mouseEvent.Wheel);
                }

                var lastSelection = _SelectionNr;
                var i = 0;
                var somethingSelected = false;

                foreach (var tile in _Tiles)
                {
                    //create a rect including the cover and text of the song. 
                    //(this way mouse over text should make a selection as well)
                    var songRect = new SRectF(tile.Rect.X, tile.Rect.Y, Rect.W, tile.Rect.H, tile.Rect.Z);
                    if (tile.Texture != _CoverBGTexture && CHelper.IsInBounds(songRect, mouseEvent) && tile.Color.A != 0)
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
            if (_IsScrolling())
            {
                _DrawScrollingList();
            }
            else
            {
                _UpdateRowLayout();

                foreach (var rowBackground in _RowBackgrounds)
                {
                    rowBackground.Draw(EAspect.Stretch);
                }

                foreach (var tile in _Tiles)
                {
                    var aspect = tile.Texture != _CoverBGTexture ? EAspect.Crop : EAspect.Stretch;
                    tile.Draw(aspect);
                }

                //highlight the text of the selected song
                for (var i = 0; i < _Texts.Count; i++)
                {
                    _Texts[i].Draw();
                    _SubTexts[i].Draw();
                }
            }

            var itemCount = CBase.Songs.IsInCategory() ? CBase.Songs.GetNumSongsVisible() : CBase.Songs.GetNumCategories();
            _DrawScrollBar(_ScrollBar, _ScrollBarThumb, _Theme.SongMenuList.TileRect, _GetScrollPosition(), _ListLength, itemCount);

            if (!PreviewVisible)
            {
                return;
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

        private void _DrawScrollingList()
        {
            var position = _GetScrollPosition();
            var progress = _GetExpandProgress();
            var isInCategory = CBase.Songs.IsInCategory();
            var itemCount = isInCategory ? CBase.Songs.GetNumSongsVisible() : CBase.Songs.GetNumCategories();

            for (var index = Math.Max((int)Math.Floor(position), 0); index < itemCount; index++)
            {
                var row = _GetRowRect(index, position, progress);
                if (row.Y >= Rect.Bottom)
                {
                    break;
                }

                if (index == _ExpandedNr)
                {
                    _DrawClipped(_CoverBGTexture, new SRectF(row.X, row.Y - 4f, row.W, row.H + 8f, row.Z + 0.05f), new SColorF(1f, 1f, 1f, 0.25f), EAspect.Stretch);
                }

                var rect = new SRectF(row.X, row.Y, row.H, row.H, row.Z);
                _DrawClipped(_GetCoverTexture(index, isInCategory), rect, new SColorF(1f, 1f, 1f, 1f), EAspect.Crop);

                _LayoutRowTexts(_ScrollText, _ScrollSubText, row, index, isInCategory);
                var visibleFraction = _GetVisibleFraction(rect);

                _ScrollText.Alpha = visibleFraction;
                _ScrollText.Draw();

                _ScrollSubText.Alpha = visibleFraction;
                _ScrollSubText.Draw();
            }
        }

        private static CTextureRef _GetCoverTexture(int index, bool isInCategory)
        {
            return isInCategory ? CBase.Songs.GetVisibleSong(index).CoverTextureSmall : CBase.Songs.GetCategory(index).CoverTextureSmall;
        }

        private static string _GetItemText(int index, bool isInCategory)
        {
            if (isInCategory)
            {
                var song = CBase.Songs.GetVisibleSong(index);
                if (CBase.Config.GetSongSorting() == ESongSorting.TR_CONFIG_TITLE_LETTER)
                {
                    return song.Title + " - " + song.Artist;
                }

                return song.Artist + " - " + song.Title;
            }

            var category = CBase.Songs.GetCategory(index);
            var num = category.GetNumSongsNotSung();
            var songOrSongs = num == 1 ? "TR_SCREENSONG_NUMSONG" : "TR_SCREENSONG_NUMSONGS";
            return category.Name + " " + CBase.Language.Translate(songOrSongs).Replace("%v", num.ToString());
        }

        private static bool _IsSortedByArtistLetter()
        {
            return CBase.Config.GetSongSorting() == ESongSorting.TR_CONFIG_ARTIST_LETTER;
        }

        private static string _GetItemTitle(int index, bool isInCategory)
        {
            if (!isInCategory)
            {
                return CBase.Songs.GetCategory(index).Name;
            }

            var song = CBase.Songs.GetVisibleSong(index);
            return _IsSortedByArtistLetter() ? song.Artist : song.Title;
        }

        private static string _GetItemSubTitle(int index, bool isInCategory)
        {
            if (isInCategory)
            {
                var song = CBase.Songs.GetVisibleSong(index);
                return _IsSortedByArtistLetter() ? song.Title : song.Artist;
            }

            var num = CBase.Songs.GetCategory(index).GetNumSongsNotSung();
            var songOrSongs = num == 1 ? "TR_SCREENSONG_NUMSONG" : "TR_SCREENSONG_NUMSONGS";
            return CBase.Language.Translate(songOrSongs).Replace("%v", num.ToString());
        }

        public override CStatic GetSelectedSongCover()
        {
            return _Tiles.FirstOrDefault(tile => tile.Selected);
        }

        public override bool IsMouseOverSelectedSong(SMouseEvent mEvent)
        {
            for (var i = 0; i < _Tiles.Count; i++)
            {
                if (!_Tiles[i].Selected)
                {
                    continue;
                }

                var tileRect = _Tiles[i].Rect;
                return CHelper.IsInBounds(new SRectF(tileRect.X, tileRect.Y, Rect.W, tileRect.H, tileRect.Z), mEvent);
            }

            return false;
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

        private void _UpdateList(bool force = false)
        {
            var isInCategory = CBase.Songs.IsInCategory();
            var itemCount = isInCategory ? CBase.Songs.GetNumSongsVisible() : CBase.Songs.GetNumCategories();
            var halfListLength = _ListLength / 2;
            int offset;

            if (_SelectionNr < _Offset && _SelectionNr >= 0)
            {
                if (_SelectionNr - halfListLength >= 0)
                {
                    offset = _SelectionNr - halfListLength;
                }
                else
                {
                    offset = 0;
                }
            }
            else if (_SelectionNr >= _Offset + _ListLength)
            {
                if (_SelectionNr + halfListLength > itemCount)
                {
                    offset = itemCount - _ListLength;
                }
                else
                {
                    offset = _SelectionNr - halfListLength;
                }
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
            var totalSongNumber = CBase.Songs.GetNumSongs();

            offset = offset.Clamp(0, itemCount - _ListLength, true);

            if (offset == _Offset && !force)
            {
                return;
            }

            if (force)
            {
                _ExpandTimer.Reset();
                _ExpandedNr = -1;
                _PrevExpandedNr = -1;
            }

            for (var i = 0; i < _Tiles.Count; i++)
            {
                if (i + offset < itemCount)
                {
                    _Tiles[i].Color = new SColorF(1f, 1f, 1f, 1f);
                    _Tiles[i].Texture = _GetCoverTexture(i + offset, isInCategory);
                    _Texts[i].Text = _GetItemText(i + offset, isInCategory);
                }
                else
                {
                    _Tiles[i].Texture = _CoverBGTexture;
                    _Tiles[i].Color.A = 0;
                    _Texts[i].Text = "";
                    _SubTexts[i].Text = "";
                }
            }

            _SetScrollTarget(offset, !force && offset != _Offset, _ListLength);
            _Offset = offset;
            _UpdateTileSelection();
        }

        public override void LoadSkin()
        {
            foreach (var themeable in _SubElements.OfType<IThemeable>())
            {
                themeable.LoadSkin();
            }

            // Those are drawn seperately so they are not in the above list
            _CoverBig.LoadSkin();
            _TextBG.LoadSkin();
            _ScrollBar?.LoadSkin();
            _ScrollBarThumb?.LoadSkin();

            base.LoadSkin();
        }
    }
}
