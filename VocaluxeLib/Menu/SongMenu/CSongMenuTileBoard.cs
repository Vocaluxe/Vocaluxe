﻿#region license
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
using System.Drawing;
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
        private CStatic _VideoIcon;
        private CStatic _MedleyCalcIcon;
        private CStatic _MedleyTagIcon;

        private CTexture _CoverBigTexture;
        private CTexture _CoverTexture;

        private CText _Artist;
        private CText _Title;
        private CText _SongLength;

        private int _NumW;
        private int _NumH;

        private float _SpaceW;
        private float _SpaceH;

        private int _TileW;
        private int _TileH;

        private int _Offset = -1;
        private int _ActualSelection = -1;

        private bool _SmallView;

        public CSongMenuTileBoard(int partyModeID)
            : base(partyModeID) {}

        public override int GetActualSelection()
        {
            return _ActualSelection;
        }

        public override void Init()
        {
            base.Init();

            _Rect = _Theme.SongMenuTileBoard.TileRect;

            _NumW = _Theme.SongMenuTileBoard.NumW;
            _NumH = _Theme.SongMenuTileBoard.NumH;
            _SpaceW = _Theme.SongMenuTileBoard.SpaceW;
            _SpaceH = _Theme.SongMenuTileBoard.SpaceH;

            _PendingTime = 100L;

            _TileW = (int)((_Theme.SongMenuTileBoard.TileRect.W - _SpaceW * (_NumW - 1)) / _NumW);
            _TileH = (int)((_Theme.SongMenuTileBoard.TileRect.H - _SpaceH * (_NumH - 1)) / _NumH);

            _CoverTexture = CBase.Theme.GetSkinTexture(_Theme.CoverBackgroundName, _PartyModeID);
            _CoverBigTexture = CBase.Theme.GetSkinTexture(_Theme.CoverBigBackgroundName, _PartyModeID);

            _Tiles = new List<CStatic>();
            for (int i = 0; i < _NumH; i++)
            {
                for (int j = 0; j < _NumW; j++)
                {
                    SRectF rect = new SRectF(_Theme.SongMenuTileBoard.TileRect.X + j * (_TileW + _SpaceW),
                                             _Theme.SongMenuTileBoard.TileRect.Y + i * (_TileH + _SpaceH), _TileW, _TileH, _Rect.Z);
                    CStatic tile = new CStatic(_PartyModeID, _CoverTexture, Color, rect);
                    _Tiles.Add(tile);
                }
            }

            _ScrollRect = new SRectF(0, 0, CBase.Settings.GetRenderW(), CBase.Settings.GetRenderH(), _Theme.SongMenuTileBoard.TileRect.Z);

            _PreviewSelected = -1;
            _Offset = 0;

            _CoverBig = _Theme.SongMenuTileBoard.StaticCoverBig;
            _TextBG = _Theme.SongMenuTileBoard.StaticTextBG;
            _DuetIcon = _Theme.SongMenuTileBoard.StaticDuetIcon;
            _VideoIcon = _Theme.SongMenuTileBoard.StaticVideoIcon;
            _MedleyCalcIcon = _Theme.SongMenuTileBoard.StaticMedleyCalcIcon;
            _MedleyTagIcon = _Theme.SongMenuTileBoard.StaticMedleyTagIcon;

            _Artist = _Theme.SongMenuTileBoard.TextArtist;
            _Title = _Theme.SongMenuTileBoard.TextTitle;
            _SongLength = _Theme.SongMenuTileBoard.TextSongLength;
        }

        public override void Update(SScreenSongOptions songOptions)
        {
            base.Update(songOptions);

            if (songOptions.Selection.RandomOnly)
            {
                _Locked = _PreviewSelected;
                _ActualSelection = _PreviewSelected;
                for (int i = 0; i < _Tiles.Count; i++)
                    _Tiles[i].Selected = _Locked == i + _Offset;
            }
        }

        public override void OnShow()
        {
            if (CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_OFF && CBase.Songs.GetNumCategories() > 0 && !CBase.Songs.IsInCategory())
                _EnterCategory(0);
            _ActualSelection = -1;
            _Locked = -1;
            _PreviewSelected = -1;
            _UpdateList(0, true);
            //AfterCategoryChange();
            SetSelectedSong(_ActSong);
            _AfterCategoryChange();

            int actcat = _PreviewSelected;
            if ((CBase.Songs.GetNumCategories() > 0) && (actcat < 0))
            {
                _CoverBig.Texture = CBase.Songs.GetCategory(0).CoverTextureBig;
                _Artist.Text = CBase.Songs.GetCategory(0).Name;
                _Title.Text = String.Empty;
                _SongLength.Text = String.Empty;
                _PreviewSelected = 0;
                _Locked = 0;
                _DuetIcon.Visible = false;
                _VideoIcon.Visible = false;
                _MedleyCalcIcon.Visible = false;
                _MedleyTagIcon.Visible = false;
            }

            if (CBase.Songs.GetNumSongsVisible() == 0 && CBase.Songs.GetSearchFilter() != "")
            {
                _CoverBig.Texture = _CoverBigTexture;
                _Artist.Text = String.Empty;
                _Title.Text = String.Empty;
                _SongLength.Text = String.Empty;
                _PreviewSelected = -1;
                _Locked = -1;
                _DuetIcon.Visible = false;
                _VideoIcon.Visible = false;
                _MedleyCalcIcon.Visible = false;
                _MedleyTagIcon.Visible = false;
            }
        }

        public override void HandleInput(ref SKeyEvent keyEvent, SScreenSongOptions songOptions)
        {
            base.HandleInput(ref keyEvent, songOptions);

            if (!keyEvent.KeyPressed)
            {
                if (!(keyEvent.Key == Keys.Left || keyEvent.Key == Keys.Right || keyEvent.Key == Keys.Up || keyEvent.Key == Keys.Down ||
                      keyEvent.Key == Keys.Escape || keyEvent.Key == Keys.Back || keyEvent.Key == Keys.Enter ||
                      keyEvent.Key == Keys.PageDown || keyEvent.Key == Keys.PageUp))
                    return;

                bool sel = _Tiles.Any(tile => tile.Selected);

                if ((_Locked == -1 || !sel) &&
                    (keyEvent.Key != Keys.Escape && keyEvent.Key != Keys.Back && keyEvent.Key != Keys.PageUp && keyEvent.Key != Keys.PageDown))
                {
                    if (_PreviewSelected > -1)
                        _Locked = _PreviewSelected;
                    else
                    {
                        _Locked = 0;
                        _ActualSelection = 0;
                        _PreviewSelected = 0;
                        _SetSelectedNow();
                        _UpdateList(0, true);
                    }
                }
                else
                {
                    switch (keyEvent.Key)
                    {
                        case Keys.Enter:
                            if (!CBase.Songs.IsInCategory())
                            {
                                _EnterCategory(_PreviewSelected);
                                keyEvent.Handled = true;
                            }
                            else if (_ActualSelection > -1 && _PreviewSelected >= 0)
                                _Locked = _ActualSelection;
                            break;

                        case Keys.Escape:
                        case Keys.Back:
                            if (CBase.Songs.IsInCategory() && CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_ON && songOptions.Selection.CategoryChangeAllowed)
                            {
                                _ShowCategories();
                                keyEvent.Handled = true;
                            }
                            //else if (CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_OFF)
                            //{
                            //    CGraphics.FadeTo(EScreens.ScreenMain);
                            //}
                            break;

                        case Keys.PageUp:
                            if (CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_ON && songOptions.Selection.CategoryChangeAllowed)
                                _PrevCategory();
                            break;

                        case Keys.PageDown:
                            if (CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_ON && songOptions.Selection.CategoryChangeAllowed)
                                _NextCategory();
                            break;

                        case Keys.Left:
                            if (_Locked > 0 && (!songOptions.Selection.RandomOnly || songOptions.Selection.CategoryChangeAllowed && !CBase.Songs.IsInCategory()))
                            {
                                _Locked--;
                                _UpdateList();
                            }
                            break;

                        case Keys.Right:
                            if (!CBase.Songs.IsInCategory() && songOptions.Selection.CategoryChangeAllowed)
                            {
                                if (_Locked < CBase.Songs.GetNumCategories() - 1)
                                {
                                    _Locked++;
                                    _UpdateList();
                                }
                            }
                            else
                            {
                                if (CBase.Songs.IsInCategory() && _Locked < CBase.Songs.GetNumSongsVisible() - 1 && !songOptions.Selection.RandomOnly)
                                {
                                    _Locked++;
                                    _UpdateList();
                                }
                            }
                            break;

                        case Keys.Up:
                            if (keyEvent.ModShift && CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_ON && songOptions.Selection.CategoryChangeAllowed)
                            {
                                _PrevCategory();
                                break;
                            }

                            if (_Locked > _NumW - 1 &&
                                (!songOptions.Selection.RandomOnly || songOptions.Selection.CategoryChangeAllowed && !CBase.Songs.IsInCategory()))
                            {
                                _Locked -= _NumW;
                                _UpdateList();
                            }
                            break;

                        case Keys.Down:
                            if (keyEvent.ModShift && CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_ON && songOptions.Selection.CategoryChangeAllowed)
                            {
                                _NextCategory();
                                break;
                            }

                            if (!CBase.Songs.IsInCategory() && songOptions.Selection.CategoryChangeAllowed)
                            {
                                if (_Locked < CBase.Songs.GetNumCategories() - _NumW)
                                {
                                    _Locked += _NumW;
                                    _UpdateList();
                                }
                            }
                            else if (_Locked < CBase.Songs.GetNumSongsVisible() - _NumW && !songOptions.Selection.RandomOnly)
                            {
                                _Locked += _NumW;
                                _UpdateList();
                            }
                            break;
                    }
                }

                if (!keyEvent.Handled)
                {
                    _PreviewSelected = _Locked;
                    _ActualSelection = _Locked;

                    for (int i = 0; i < _Tiles.Count; i++)
                        _Tiles[i].Selected = _Locked == i + _Offset;
                }
            }
        }

        public override void HandleMouse(ref SMouseEvent mouseEvent, SScreenSongOptions songOptions)
        {
            base.HandleMouse(ref mouseEvent, songOptions);

            int i = 0;
            bool sel = false;
            int lastselection = _ActualSelection;

            if (!songOptions.Selection.RandomOnly || !CBase.Songs.IsInCategory() && songOptions.Selection.CategoryChangeAllowed)
            {
                foreach (CStatic tile in _Tiles)
                {
                    if (tile.Texture != _CoverTexture && CHelper.IsInBounds(tile.Rect, mouseEvent) && !sel)
                    {
                        if (mouseEvent.LB || !CBase.Songs.IsInCategory())
                        {
                            if (_PreviewSelected == i + _Offset)
                                _Locked = _PreviewSelected;
                            else
                            {
                                _PreviewSelected = i + _Offset;
                                _Locked = -1;
                            }
                        }
                        tile.Selected = true;
                        _ActualSelection = i + _Offset;
                        sel = true;
                    }
                    else
                        tile.Selected = false;
                    i++;
                }
            }
            else
                sel = true;

            if (mouseEvent.Sender == ESender.WiiMote && _ActualSelection != lastselection && _ActualSelection != -1)
                CBase.Controller.SetRumble(0.050f);

            if (!sel)
                _ActualSelection = -1;

            if (mouseEvent.RB && (CBase.Songs.GetNumCategories() > 0) && CBase.Songs.IsInCategory() && CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_ON &&
                songOptions.Selection.CategoryChangeAllowed)
            {
                _ShowCategories();
                mouseEvent.Handled = true;
                return;
            }
            if (mouseEvent.RB && CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_OFF && !songOptions.Selection.PartyMode)
                CBase.Graphics.FadeTo(EScreens.ScreenMain);
            else if (_PreviewSelected != -1 && mouseEvent.LB && CBase.Songs.GetCurrentCategoryIndex() != -1 && !songOptions.Selection.PartyMode)
            {
                if (CHelper.IsInBounds(_CoverBig.Rect, mouseEvent) || CHelper.IsInBounds(_TextBG.Rect, mouseEvent))
                    _Locked = _PreviewSelected;
            }
            else if (mouseEvent.LB && (!CBase.Songs.IsInCategory()))
            {
                foreach (CStatic tile in _Tiles)
                {
                    if (tile.Texture != _CoverTexture && CHelper.IsInBounds(tile.Rect, mouseEvent))
                    {
                        _EnterCategory(_PreviewSelected);
                        mouseEvent.Handled = true;
                        return;
                    }
                }
            }

            if (mouseEvent.Wheel != 0 && CHelper.IsInBounds(_ScrollRect, mouseEvent) &&
                (!songOptions.Selection.RandomOnly || !CBase.Songs.IsInCategory() && songOptions.Selection.CategoryChangeAllowed))
                _UpdateList(_Offset + _NumW * mouseEvent.Wheel);
        }

        public override void Draw()
        {
            foreach (CStatic tile in _Tiles)
            {
                if (tile.Selected && _Active)
                    tile.Draw(1.2f, tile.Rect.Z - 0.1f, EAspect.Crop);
                else
                {
                    // ReSharper disable ConvertIfStatementToConditionalTernaryExpression
                    if (tile.Texture != _CoverTexture)
                        // ReSharper restore ConvertIfStatementToConditionalTernaryExpression
                        tile.Draw(1f, tile.Rect.Z, EAspect.Crop);
                    else
                        tile.Draw(1f, tile.Rect.Z, EAspect.Stretch);
                }
            }

            if (CBase.Songs.IsInCategory())
            {
                int actsong = _PreviewSelected;
                if ((CBase.Songs.GetNumSongsVisible() > actsong) && (actsong >= 0))
                {
                    CSong song = CBase.Songs.GetVisibleSong(actsong);

                    _CoverBig.Texture = song.CoverTextureBig;
                    _Artist.Text = song.Artist;
                    _Title.Text = song.Title;
                    _DuetIcon.Visible = song.IsDuet;
                    _VideoIcon.Visible = song.VideoFileName != "";
                    _MedleyCalcIcon.Visible = song.Medley.Source == EMedleySource.Calculated;
                    _MedleyTagIcon.Visible = song.Medley.Source == EMedleySource.Tag;

                    float time = CBase.Sound.GetLength(_SongStream);
                    if (Math.Abs(song.Finish) > 0.001)
                        time = song.Finish;

                    time -= song.Start;
                    int min = (int)Math.Floor(time / 60f);
                    int sec = (int)(time - min * 60f);
                    _SongLength.Text = min.ToString("00") + ":" + sec.ToString("00");
                }
            }
            else
            {
                int actcat = _PreviewSelected;
                if ((CBase.Songs.GetNumCategories() > actcat) && (actcat >= 0))
                {
                    _CoverBig.Texture = CBase.Songs.GetCategory(actcat).CoverTextureBig;
                    _Artist.Text = CBase.Songs.GetCategory(actcat).Name;

                    int num = CBase.Songs.NumSongsInCategory(actcat);
                    // ReSharper disable ConvertIfStatementToConditionalTernaryExpression
                    if (num != 1)
                        // ReSharper restore ConvertIfStatementToConditionalTernaryExpression
                        _Title.Text = CBase.Language.Translate("TR_SCREENSONG_NUMSONGS").Replace("%v", num.ToString());
                    else
                        _Title.Text = CBase.Language.Translate("TR_SCREENSONG_NUMSONG").Replace("%v", num.ToString());

                    _SongLength.Text = String.Empty;
                    _DuetIcon.Visible = false;
                    _VideoIcon.Visible = false;
                    _MedleyCalcIcon.Visible = false;
                    _MedleyTagIcon.Visible = false;
                }
            }

            _TextBG.Draw();

            if (_Vidtex != null && _Video != -1)
            {
                if (_Vidtex.Color.A < 1)
                    _CoverBig.Draw(1f, EAspect.Crop);
                RectangleF bounds = new RectangleF(_CoverBig.Rect.X, _CoverBig.Rect.Y, _CoverBig.Rect.W, _CoverBig.Rect.H);
                RectangleF rect;
                CHelper.SetRect(bounds, out rect, _Vidtex.OrigAspect, EAspect.Crop);
                SRectF vidRect = new SRectF(rect.X, rect.Y, rect.Width, rect.Height, _CoverBig.Rect.Z);
                SRectF vidRectBounds = new SRectF(bounds.X, bounds.Y, bounds.Width, bounds.Height, 0f);

                CBase.Drawing.DrawTexture(_Vidtex, vidRect, _Vidtex.Color, vidRectBounds);
                CBase.Drawing.DrawTextureReflection(_Vidtex, vidRect, _Vidtex.Color, vidRectBounds, _CoverBig.ReflectionSpace, _CoverBig.ReflectionHeight);
            }
            else
                _CoverBig.Draw(1f, EAspect.Crop);


            _Artist.Draw();
            _Title.Draw();
            _SongLength.Draw();
            _DuetIcon.Draw();
            _VideoIcon.Draw();
            _MedleyCalcIcon.Draw();
            _MedleyTagIcon.Draw();
        }

        public override int GetSelectedSong()
        {
            return _Locked;
        }

        public override CStatic GetSelectedSongCover()
        {
            foreach (CStatic tile in _Tiles)
            {
                if (tile.Selected)
                    return new CStatic(tile);
            }
            return new CStatic(_PartyModeID);
        }

        private void _SetSelectedTile(int itemNr)
        {
            bool sel = _Tiles.Any(tile => tile.Selected);

            if (_Locked == -1 || !sel)
            {
                if (_PreviewSelected > -1)
                    _Locked = _PreviewSelected;
                else
                {
                    _Locked = 0;
                    _ActualSelection = 0;
                    _PreviewSelected = 0;
                    _SetSelectedNow();
                    _UpdateList(0, true);
                }
            }

            foreach (CStatic tile in _Tiles)
                tile.Selected = false;

            _PreviewSelected = itemNr;
            _SetSelectedNow();
            _Locked = itemNr;

            _UpdateList(true);

            _PreviewSelected = _Locked;
            _ActualSelection = _Locked;

            if (_Locked - _Offset >= 0)
                _Tiles[_Locked - _Offset].Selected = true;
        }

        public override void SetSelectedSong(int visibleSongNr)
        {
            base.SetSelectedSong(visibleSongNr);

            if (visibleSongNr >= 0 && visibleSongNr < CBase.Songs.GetNumSongsVisible())
                _SetSelectedTile(visibleSongNr);
        }

        public override void SetSelectedCategory(int categoryNr)
        {
            base.SetSelectedCategory(categoryNr);

            if (categoryNr >= 0 && categoryNr < CBase.Songs.GetNumCategories())
                _SetSelectedTile(categoryNr);
        }

        protected override void _EnterCategory(int cat)
        {
            base._EnterCategory(cat);

            _PreviewSelected = 0;
            _Locked = -1;
            _ActualSelection = 0;
            _AfterCategoryChange();
            _Locked = 0;
        }

        protected override void _ShowCategories()
        {
            base._ShowCategories();

            _PreviewSelected = 0;
            _Locked = -1;
            _ActualSelection = 0;
            _AfterCategoryChange();
            _Locked = 0;
        }

        private void _NextCategory()
        {
            if (CBase.Songs.GetCurrentCategoryIndex() > -1)
            {
                CBase.Songs.NextCategory();
                _EnterCategory(CBase.Songs.GetCurrentCategoryIndex());
            }
        }

        private void _PrevCategory()
        {
            if (CBase.Songs.GetCurrentCategoryIndex() > -1)
            {
                CBase.Songs.PrevCategory();
                _EnterCategory(CBase.Songs.GetCurrentCategoryIndex());
            }
        }

        private void _AfterCategoryChange()
        {
            _SetSelectedNow();
            _SelectSong(_PreviewSelected);

            foreach (CStatic tile in _Tiles)
                tile.Selected = false;

            if (_ActualSelection >= 0 && _ActualSelection < _Tiles.Count)
                _Tiles[_ActualSelection].Selected = true;

            if ((_LastKnownNumSongs == CBase.Songs.GetNumSongsVisible()) && (_LastKnownCategory == CBase.Songs.GetCurrentCategoryIndex()))
                return;

            _LastKnownCategory = CBase.Songs.GetCurrentCategoryIndex();
            _LastKnownNumSongs = CBase.Songs.GetNumSongsVisible();
            _UpdateList(0, true);
            CBase.Songs.UpdateRandomSongList();
        }

        private void _UpdateList(bool force = false)
        {
            _UpdateList((_Locked / _NumW) * _NumW - (_NumW * (_NumH - 2)), force);
        }

        private void _UpdateList(int offset, bool force = false)
        {
            bool isInCategory = CBase.Songs.IsInCategory();
            int itemCount = isInCategory ? CBase.Songs.GetNumSongsVisible() : CBase.Songs.GetNumCategories();

            if (offset >= (itemCount / _NumW) * _NumW - (_NumW * (_NumH - 1)))
                offset = (itemCount / _NumW) * _NumW - (_NumW * (_NumH - 1));

            if (offset < 0)
                offset = 0;

            if (offset == _Offset && !force)
                return;

            for (int i = 0; i < _Tiles.Count; i++)
            {
                if (itemCount > i + offset)
                {
                    _Tiles[i].Texture = isInCategory ? CBase.Songs.GetVisibleSong(i + offset).CoverTextureSmall : CBase.Songs.GetCategory(i + offset).CoverTextureSmall;
                    _Tiles[i].Color = new SColorF(1f, 1f, 1f, 1f);
                }
                else
                {
                    _Tiles[i].Texture = _CoverTexture;
                    _Tiles[i].Color = _Color;
                }
            }
            _Offset = offset;
        }

        public override void SetSmallView(bool smallView)
        {
            base.SetSmallView(smallView);

            _SmallView = smallView;

            if (_SmallView)
            {
                _NumH = _Theme.SongMenuTileBoard.NumHsmall;
                _NumW = _Theme.SongMenuTileBoard.NumWsmall;
                _Rect = _Theme.SongMenuTileBoard.TileRectSmall;
            }
            else
            {
                _NumH = _Theme.SongMenuTileBoard.NumH;
                _NumW = _Theme.SongMenuTileBoard.NumW;
                _Rect = _Theme.SongMenuTileBoard.TileRect;
            }

            _TileW = (int)((_Rect.W - _SpaceW * (_NumW - 1)) / _NumW);
            _TileH = (int)((_Rect.H - _SpaceH * (_NumH - 1)) / _NumH);

            _CoverTexture = CBase.Theme.GetSkinTexture(_Theme.CoverBackgroundName, _PartyModeID);
            _CoverBigTexture = CBase.Theme.GetSkinTexture(_Theme.CoverBigBackgroundName, _PartyModeID);

            _Tiles = new List<CStatic>();
            for (int i = 0; i < _NumH; i++)
            {
                for (int j = 0; j < _NumW; j++)
                {
                    SRectF rect = new SRectF(_Rect.X + j * (_TileW + _SpaceW), _Rect.Y + i * (_TileH + _SpaceH), _TileW, _TileH, _Rect.Z);
                    CStatic tile = new CStatic(_PartyModeID, _CoverTexture, Color, rect);
                    _Tiles.Add(tile);
                }
            }

            _ScrollRect = new SRectF(0, 0, CBase.Settings.GetRenderW(), CBase.Settings.GetRenderH(), _Rect.Z);
            _UpdateList(true);
        }

        public override bool IsSmallView()
        {
            return _SmallView;
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