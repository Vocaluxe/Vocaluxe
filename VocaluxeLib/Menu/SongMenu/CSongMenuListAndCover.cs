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
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VocaluxeLib.Draw;
using VocaluxeLib.PartyModes;
using VocaluxeLib.Songs;

namespace VocaluxeLib.Menu.SongMenu
{
    class CSongMenuListAndCover : CSongMenuFramework
    {
        private SRectF _ScrollRect;
        private List<CStatic> _Tiles;
        private List<CText> _Texts;
        private CStatic _CoverBig;
        private CStatic _TextBG;
        private CStatic _DuetIcon;
        private CStatic _VideoIcon;
        private CStatic _MedleyCalcIcon;
        private CStatic _MedleyTagIcon;

        private CTexture _CoverBigBGTexture;
        private CTexture _CoverBGTexture;

        private CText _Artist;
        private CText _Title;
        private CText _SongLength;


        private float _SpaceW;
        private float _SpaceH;

        private int _TileW;
        private int _TileH;
        
        //TODO: remove all 1* from the code (artifacts from translation from tile board)
        //TODO: make variable list length
        private int _ListLength = 15;
        private float _CoverTileWidth;
        private float _ListTextWidth;
        

        // Offset is the song or categoryNr of the tile in the left upper corner
        private int _Offset;

        private float _Length = -1f;
        private int _LastKnownElements;
        private int _LastKnownCategory;

        private bool _MouseWasInRect;

        public override float SelectedTileZoomFactor
        {
            get { return 1.2f; }
        }

        protected override int _SelectionNr
        {
            set
            {
                int max = CBase.Songs.IsInCategory() ? CBase.Songs.GetNumSongsVisible() : CBase.Songs.GetNumCategories();
                base._SelectionNr = value.Clamp(-1, max - 1, true);
                //Update list in case we scrolled 
                _UpdateList();

                _UpdateTileSelection();
            }
        }

        private void _UpdateTileSelection()
        {
            foreach (CStatic tile in _Tiles)
                tile.Selected = false;

            int tileNr = _SelectionNr - _Offset;
            if (tileNr >= 0 && tileNr < _Tiles.Count)
                _Tiles[tileNr].Selected = true;
        }

        public override bool SmallView
        {
            set
            {
                if (SmallView == value)
                    return;
                base.SmallView = value;
                _InitTiles();
                _UpdateList(true);
                _UpdateTileSelection();
            }
        }

        public CSongMenuListAndCover(int partyModeID)
            : base(partyModeID) {}

        protected override int _PreviewNr
        {
            set
            {
                if (value == base._PreviewNr)
                {
                    if (!CBase.BackgroundMusic.IsPlaying() && value != -1)
                        CBase.BackgroundMusic.PlayPreview();
                    return;
                }
                base._PreviewNr = value;
                _UpdatePreview();
            }
        }

        public override void Init()
        {
            base.Init();

            _SpaceW = _Theme.SongMenuTileBoard.SpaceW;
            _SpaceH = _Theme.SongMenuTileBoard.SpaceH;

            _CoverBig = _Theme.SongMenuTileBoard.StaticCoverBig;
            _TextBG = _Theme.SongMenuTileBoard.StaticTextBG;
            _DuetIcon = _Theme.SongMenuTileBoard.StaticDuetIcon;
            _VideoIcon = _Theme.SongMenuTileBoard.StaticVideoIcon;
            _MedleyCalcIcon = _Theme.SongMenuTileBoard.StaticMedleyCalcIcon;
            _MedleyTagIcon = _Theme.SongMenuTileBoard.StaticMedleyTagIcon;

            _Artist = _Theme.SongMenuTileBoard.TextArtist;
            _Title = _Theme.SongMenuTileBoard.TextTitle;
            _SongLength = _Theme.SongMenuTileBoard.TextSongLength;

            _PreviewNr = -1;
            _InitTiles();
        }

        private void _InitTiles()
        {
            Rect = _Theme.SongMenuTileBoard.TileRect;

            //keep tile ratio 3/2 so the covers still look nice. 
            _CoverTileWidth = _ListLength * 3 / 2;
            _ListTextWidth = Rect.W - _ListTextWidth;

            _TileW = (int)((Rect.W - _SpaceW * (_CoverTileWidth - 1)) / _CoverTileWidth);
            _TileH = (int)((Rect.H - _SpaceH * (_ListLength - 1)) / _ListLength);

            _CoverBGTexture = CBase.Theme.GetSkinTexture(_Theme.CoverBackgroundName, _PartyModeID);
            _CoverBigBGTexture = CBase.Theme.GetSkinTexture(_Theme.CoverBigBackgroundName, _PartyModeID);

            //Create cover tiles
            _Tiles = new List<CStatic>();
            _Texts = new List<CText>();

          

            for (int i = 0; i < _ListLength; i++)
            {
                //Create Cover
                var rect = new SRectF(Rect.X + (_TileW + _SpaceW), Rect.Y + i * (_TileH + _SpaceH), _TileW, _TileH, Rect.Z);
                var tile = new CStatic(_PartyModeID, _CoverBGTexture, _Color, rect);
                _Tiles.Add(tile);

                //Create text
                var textRect = new SRectF(Rect.X + 2*(_TileW + _SpaceW), Rect.Y + i * (_TileH + _SpaceH), _ListTextWidth, _TileH, Rect.Z);
                //string textString = _Artist.Text + " - " + _Title.Text;
                string textString = "";                
                CText text = new CText(textRect.X,textRect.Y,textRect.Z,
                    textRect.H,textRect.W,EAlignment.Left,EStyle.Normal,
                    "Normal",_Artist.Color,textString);

                _Texts.Add(text);
            }

            _ScrollRect = new SRectF(0, 0, CBase.Settings.GetRenderW(), CBase.Settings.GetRenderH(), Rect.Z);
        }

        public override void Update(SScreenSongOptions songOptions)
        {
            base.Update(songOptions);

            if (songOptions.Selection.RandomOnly)
                _PreviewNr = _SelectionNr;

            if (_Length < 0 && CBase.Songs.IsInCategory() && CBase.BackgroundMusic.GetLength() > 0)
                _UpdateLength(CBase.Songs.GetVisibleSong(_PreviewNr));
        }

        private void _UpdatePreview()
        {
            //First hide everything so we just have to set what we actually want
            _CoverBig.Texture = _CoverBigBGTexture;
            _Artist.Text = String.Empty;
            _Title.Text = String.Empty;
            _SongLength.Text = String.Empty;
            _DuetIcon.Visible = false;
            _VideoIcon.Visible = false;
            _MedleyCalcIcon.Visible = false;
            _MedleyTagIcon.Visible = false;
            _Length = -1f;

            //Check if nothing is selected (for preview)
            if (_PreviewNr < 0)
                return;

            if (CBase.Songs.IsInCategory())
            {
                CSong song = CBase.Songs.GetVisibleSong(_PreviewNr);
                //Check if we have a valid song (song still visible, index >=0 etc is checked by framework)
                if (song == null)
                {
                    //Display at least the category
                    CCategory category = CBase.Songs.GetCategory(CBase.Songs.GetCurrentCategoryIndex());
                    //Check if we have a valid category
                    if (category == null)
                        return;
                    _CoverBig.Texture = category.CoverTextureBig;
                    _Artist.Text = category.Name;
                    return;
                }
                _CoverBig.Texture = song.CoverTextureBig;
                _Artist.Text = song.Artist;
                _Title.Text = song.Title;
                _DuetIcon.Visible = song.IsDuet;
                _VideoIcon.Visible = song.VideoFileName != "";
                _MedleyCalcIcon.Visible = song.Medley.Source == EDataSource.Calculated;
                _MedleyTagIcon.Visible = song.Medley.Source == EDataSource.Tag;

                _UpdateLength(song);
            }
            else
            {
                CCategory category = CBase.Songs.GetCategory(_PreviewNr);
                //Check if we have a valid category
                if (category == null)
                    return;
                _CoverBig.Texture = category.CoverTextureBig;
                _Artist.Text = category.Name;

                int num = category.GetNumSongsNotSung();
                String songOrSongs = (num == 1) ? "TR_SCREENSONG_NUMSONG" : "TR_SCREENSONG_NUMSONGS";
                _Title.Text = CBase.Language.Translate(songOrSongs).Replace("%v", num.ToString());
            }
        }

        private void _UpdateLength(CSong song)
        {
            if (song == null)
                return;
            float time = CBase.BackgroundMusic.GetLength();
            if (Math.Abs(song.Finish) > 0.001)
                time = song.Finish;

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
                _SongLength.Text = "...";
        }

        public override void OnShow()
        {
            _LastKnownElements = -1; //Force refresh of list
            if (!CBase.Songs.IsInCategory())
            {
                if ((CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_OFF && CBase.Songs.GetNumCategories() > 0) || CBase.Songs.GetNumCategories() == 1)
                    _EnterCategory(0);
            }
            if (CBase.Songs.IsInCategory())
                SetSelectedSong(_SelectionNr < 0 ? 0 : _SelectionNr);
            else
                SetSelectedCategory(_SelectionNr < 0 ? 0 : _SelectionNr);
            _PreviewNr = _SelectionNr;
            _UpdateListIfRequired();
        }

        public override bool HandleInput(ref SKeyEvent keyEvent, SScreenSongOptions options)
        {
            if (keyEvent.KeyPressed)
                return false;

            bool moveAllowed = !options.Selection.RandomOnly || (options.Selection.CategoryChangeAllowed && !CBase.Songs.IsInCategory());
            bool catChangePossible = CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_ON && options.Selection.CategoryChangeAllowed;

            //If nothing selected set a reasonable default value
            if (keyEvent.IsArrowKey() && moveAllowed && _SelectionNr < 0)
                _SelectionNr = (_PreviewNr < 0) ? _Offset : _PreviewNr;

            switch (keyEvent.Key)
            {
                case Keys.Enter:
                    if (CBase.Songs.IsInCategory())
                    {
                        if (_SelectionNr >= 0 && _PreviewNr != _SelectionNr)
                        {
                            _PreviewNr = _SelectionNr;
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
                        _LeaveCategory();
                        keyEvent.Handled = true;
                    }
                    break;

                case Keys.PageUp:
                    if (catChangePossible)
                    {
                        _PrevCategory();
                        keyEvent.Handled = true;
                    }
                    break;

                case Keys.PageDown:
                    if (catChangePossible)
                    {
                        _NextCategory();
                        keyEvent.Handled = true;
                    }
                    break;

                case Keys.Left:
                    //Check for >0 so we do not allow selection of nothing (-1)
                    if (_SelectionNr > 0 && moveAllowed)
                    {
                        _SelectionNr--;
                        keyEvent.Handled = true;
                    }
                    break;

                case Keys.Right:
                    if (moveAllowed)
                    {
                        _SelectionNr++;
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
                        keyEvent.Handled = true;
                    }
                    break;
            }
            if (!CBase.Songs.IsInCategory())
                _PreviewNr = _SelectionNr;
            return keyEvent.Handled;
        }

        public override bool HandleMouse(ref SMouseEvent mouseEvent, SScreenSongOptions songOptions)
        {
            if (!songOptions.Selection.RandomOnly || (!CBase.Songs.IsInCategory() && songOptions.Selection.CategoryChangeAllowed))
            {
                if (mouseEvent.Wheel != 0 && CHelper.IsInBounds(_ScrollRect, mouseEvent))
                    _UpdateList(_Offset + 1 * mouseEvent.Wheel);

                int lastSelection = _SelectionNr;
                int i = 0;
                bool somethingSelected = false;

                //needed for hover over list selection
                SRectF songRect;


                foreach (CStatic tile in _Tiles)
                {
                    //create a rect including the cover and text of the song. 
                    //(this way mouse over text should make a selection as well)
                    songRect = new SRectF(tile.Rect.X, tile.Rect.Y, Rect.W, tile.Rect.H, tile.Rect.Z);
                    if (tile.Texture != _CoverBGTexture && CHelper.IsInBounds(songRect, mouseEvent))
                    {
                        somethingSelected = true;
                        _SelectionNr = i + _Offset;
                        if (!CBase.Songs.IsInCategory())
                            _PreviewNr = i + _Offset;
                        break;
                    }
                    i++;
                }
                //Reset selection only if we moved out of the rect to avoid loosing it when selecting random songs
                if (_MouseWasInRect && !somethingSelected)
                    _SelectionNr = -1;
                if (mouseEvent.Sender == ESender.WiiMote && _SelectionNr != lastSelection && _SelectionNr != -1)
                    CBase.Controller.SetRumble(0.050f);
            }
            _MouseWasInRect = CHelper.IsInBounds(Rect, mouseEvent);

            if (mouseEvent.RB)
            {
                if (CBase.Songs.IsInCategory() && CBase.Songs.GetNumCategories() > 0 && CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_ON &&
                    songOptions.Selection.CategoryChangeAllowed)
                {
                    _LeaveCategory();
                    return true;
                }
                if (CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_OFF && !songOptions.Selection.PartyMode)
                {
                    CBase.Graphics.FadeTo(EScreens.ScreenMain);
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
                            return false;
                        _PreviewNr = _SelectionNr;
                    }
                    else
                        EnterSelectedCategory();
                    return true;
                }
            }
            return false;
        }

        public override void Draw()
        {
            foreach (CStatic tile in _Tiles)
            {
                if (tile.Selected && Active)
                    tile.Draw(EAspect.Crop, SelectedTileZoomFactor, -0.1f);
                else
                {
                    EAspect aspect = (tile.Texture != _CoverBGTexture) ? EAspect.Crop : EAspect.Stretch;
                    tile.Draw(aspect);
                }
            }

            //hightlight the text of the selected song
            int i = 0;
            foreach(CText text in _Texts)
            {
                if(i < _Tiles.Count && _Tiles.ElementAt(i).Selected && Active)
                {
                    text.Style = EStyle.BoldItalic;
                }
                else if (i < _Tiles.Count)
                {
                    text.Style = EStyle.Normal;
                } else
                {
                    text.Text = "";
                }
                text.Draw();
                i++;
            }

            _TextBG.Draw();

            CTexture vidtex = CBase.BackgroundMusic.IsPlayingPreview() ? CBase.BackgroundMusic.GetVideoTexture() : null;

            if (vidtex != null)
            {
                if (vidtex.Color.A < 1)
                    _CoverBig.Draw(EAspect.Crop);
                var bounds = new RectangleF(_CoverBig.Rect.X, _CoverBig.Rect.Y, _CoverBig.Rect.W, _CoverBig.Rect.H);
                RectangleF rect;
                CHelper.SetRect(bounds, out rect, vidtex.OrigAspect, EAspect.Crop);
                var vidRect = new SRectF(rect.X, rect.Y, rect.Width, rect.Height, _CoverBig.Rect.Z);
                var vidRectBounds = new SRectF(bounds.X, bounds.Y, bounds.Width, bounds.Height, 0f);

                CBase.Drawing.DrawTexture(vidtex, vidRect, vidtex.Color, vidRectBounds);
                CBase.Drawing.DrawTextureReflection(vidtex, vidRect, vidtex.Color, vidRectBounds, _CoverBig.ReflectionSpace, _CoverBig.ReflectionHeight);
            }
            else
                _CoverBig.Draw(EAspect.Crop);


            _Artist.Draw();
            _Title.Draw();
            _SongLength.Draw();
            _DuetIcon.Draw();
            _VideoIcon.Draw();
            _MedleyCalcIcon.Draw();
            _MedleyTagIcon.Draw();
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

            SetSelectedCategory(0);
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
            int curElements = CBase.Songs.IsInCategory() ? CBase.Songs.GetNumSongsVisible() : CBase.Songs.GetNumCategories();
            if ((_LastKnownElements == curElements) && (_LastKnownCategory == CBase.Songs.GetCurrentCategoryIndex()))
                return;

            _LastKnownCategory = CBase.Songs.GetCurrentCategoryIndex();
            _LastKnownElements = curElements;
            CBase.Songs.UpdateRandomSongList();
            _UpdateList(true);
        }

        private int _GetOffsetForPosition(int index, int rowNum = 0)
        {
            return (index / 1) * 1 - (1 * rowNum.Clamp(0, _ListLength - 1, true));
        }

        private void _UpdateList(bool force = false)
        {
            int offset;
            if (_SelectionNr < _Offset && _SelectionNr >= 0)
                offset = _GetOffsetForPosition(_SelectionNr);
            else if (_SelectionNr >= _Offset + 1 * _ListLength)
                offset = _GetOffsetForPosition(_SelectionNr, _ListLength);
            else
                offset = _Offset;
            _UpdateList(offset, force);
        }

        private void _UpdateList(int offset, bool force = false)
        {
            bool isInCategory = CBase.Songs.IsInCategory();
            int itemCount = isInCategory ? CBase.Songs.GetNumSongsVisible() : CBase.Songs.GetNumCategories();

            offset = offset.Clamp(0, _GetOffsetForPosition(itemCount, _ListLength - 1), true);

            if (offset == _Offset && !force)
                return;

            CSong currentSong;
            string textString;

            for (int i = 0; i < _Tiles.Count; i++)
            {
                if (itemCount > i + offset)
                {
                    currentSong = CBase.Songs.GetVisibleSong(i+offset);
                    //isInCategory does not really seem to be interesting.. :D
                    _Tiles[i].Texture = isInCategory ? currentSong.CoverTextureSmall : currentSong.CoverTextureSmall;
                    _Tiles[i].Color = new SColorF(1f, 1f, 1f, 1f);

                    textString = currentSong.Artist + " - "+ currentSong.Title;
                    _Texts.ElementAt(i).Text = textString;
                }
                else
                {
                    _Tiles[i].Texture = _CoverBGTexture;
                    _Tiles[i].Color = _Color;
                    _Texts.ElementAt(i).Text = "";
                }
            }
            _Offset = offset;
        }

        public override void LoadTextures()
        {
            base.LoadTextures();

            _Theme.SongMenuTileBoard.StaticCoverBig.ReloadTextures();
            _Theme.SongMenuTileBoard.StaticTextBG.ReloadTextures();
            _Theme.SongMenuTileBoard.StaticDuetIcon.ReloadTextures();
            _Theme.SongMenuTileBoard.StaticVideoIcon.ReloadTextures();
            _Theme.SongMenuTileBoard.StaticMedleyCalcIcon.ReloadTextures();
            _Theme.SongMenuTileBoard.StaticMedleyTagIcon.ReloadTextures();

            _Theme.SongMenuTileBoard.TextArtist.ReloadTextures();
            _Theme.SongMenuTileBoard.TextTitle.ReloadTextures();
            _Theme.SongMenuTileBoard.TextSongLength.ReloadTextures();
        }
    }
}