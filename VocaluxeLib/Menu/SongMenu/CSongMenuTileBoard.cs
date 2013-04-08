using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using VocaluxeLib.PartyModes;

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

        private STexture _CoverBigTexture;
        private STexture _CoverTexture;

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
        private int _actualSelection = -1;

        private bool _SmallView;

        public CSongMenuTileBoard(int PartyModeID)
            : base(PartyModeID) {}

        public override int GetActualSelection()
        {
            return _actualSelection;
        }

        public override void Init()
        {
            base.Init();

            _Rect = _Theme.songMenuTileBoard.TileRect;

            _NumW = _Theme.songMenuTileBoard.numW;
            _NumH = _Theme.songMenuTileBoard.numH;
            _SpaceW = _Theme.songMenuTileBoard.spaceW;
            _SpaceH = _Theme.songMenuTileBoard.spaceH;

            _PendingTime = 100L;

            _TileW = (int)((_Theme.songMenuTileBoard.TileRect.W - _SpaceW * (_NumW - 1)) / _NumW);
            _TileH = (int)((_Theme.songMenuTileBoard.TileRect.H - _SpaceH * (_NumH - 1)) / _NumH);

            _CoverTexture = CBase.Theme.GetSkinTexture(_Theme.CoverBackgroundName, _PartyModeID);
            _CoverBigTexture = CBase.Theme.GetSkinTexture(_Theme.CoverBigBackgroundName, _PartyModeID);

            _Tiles = new List<CStatic>();
            for (int i = 0; i < _NumH; i++)
            {
                for (int j = 0; j < _NumW; j++)
                {
                    SRectF rect = new SRectF(_Theme.songMenuTileBoard.TileRect.X + j * (_TileW + _SpaceW),
                                             _Theme.songMenuTileBoard.TileRect.Y + i * (_TileH + _SpaceH), _TileW, _TileH, _Rect.Z);
                    CStatic tile = new CStatic(_PartyModeID, _CoverTexture, Color, rect);
                    _Tiles.Add(tile);
                }
            }

            _ScrollRect = new SRectF(0, 0, CBase.Settings.GetRenderW(), CBase.Settings.GetRenderH(), _Theme.songMenuTileBoard.TileRect.Z);

            _PreviewSelected = -1;
            _Offset = 0;

            _CoverBig = _Theme.songMenuTileBoard.StaticCoverBig;
            _TextBG = _Theme.songMenuTileBoard.StaticTextBG;
            _DuetIcon = _Theme.songMenuTileBoard.StaticDuetIcon;
            _VideoIcon = _Theme.songMenuTileBoard.StaticVideoIcon;
            _MedleyCalcIcon = _Theme.songMenuTileBoard.StaticMedleyCalcIcon;
            _MedleyTagIcon = _Theme.songMenuTileBoard.StaticMedleyTagIcon;

            _Artist = _Theme.songMenuTileBoard.TextArtist;
            _Title = _Theme.songMenuTileBoard.TextTitle;
            _SongLength = _Theme.songMenuTileBoard.TextSongLength;
        }

        public override void Update(ScreenSongOptions SongOptions)
        {
            base.Update(SongOptions);

            if (SongOptions.Selection.RandomOnly)
            {
                _Locked = _PreviewSelected;
                _actualSelection = _PreviewSelected;
                for (int i = 0; i < _Tiles.Count; i++)
                    _Tiles[i].Selected = _Locked == i + _Offset;
            }
        }

        public override void OnShow()
        {
            if (CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_OFF && CBase.Songs.GetNumCategories() > 0 && CBase.Songs.GetCurrentCategoryIndex() == -1)
                EnterCategory(0);
            _actualSelection = -1;
            _Locked = -1;
            _PreviewSelected = -1;
            UpdateList(0, true);
            //AfterCategoryChange();
            SetSelectedSong(_ActSong);
            AfterCategoryChange();
            CBase.Songs.UpdateRandomSongList();

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

            if (CBase.Songs.GetNumVisibleSongs() == 0 && CBase.Songs.GetSearchFilter().Length > 0)
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

        public override void HandleInput(ref KeyEvent KeyEvent, ScreenSongOptions SongOptions)
        {
            base.HandleInput(ref KeyEvent, SongOptions);

            if (!KeyEvent.KeyPressed)
            {
                if (!(KeyEvent.Key == Keys.Left || KeyEvent.Key == Keys.Right || KeyEvent.Key == Keys.Up || KeyEvent.Key == Keys.Down ||
                      KeyEvent.Key == Keys.Escape || KeyEvent.Key == Keys.Back || KeyEvent.Key == Keys.Enter ||
                      KeyEvent.Key == Keys.PageDown || KeyEvent.Key == Keys.PageUp))
                    return;

                bool sel = false;
                foreach (CStatic tile in _Tiles)
                {
                    if (tile.Selected)
                    {
                        sel = true;
                        break;
                    }
                }

                if ((_Locked == -1 || !sel) &&
                    (KeyEvent.Key != Keys.Escape && KeyEvent.Key != Keys.Back && KeyEvent.Key != Keys.PageUp && KeyEvent.Key != Keys.PageDown))
                {
                    if (_PreviewSelected > -1)
                        _Locked = _PreviewSelected;
                    else
                    {
                        _Locked = 0;
                        _actualSelection = 0;
                        _PreviewSelected = 0;
                        SetSelectedNow();
                        UpdateList(0, true);
                    }
                }
                else
                {
                    switch (KeyEvent.Key)
                    {
                        case Keys.Enter:
                            if (CBase.Songs.GetCurrentCategoryIndex() < 0)
                            {
                                EnterCategory(_PreviewSelected);
                                KeyEvent.Handled = true;
                            }
                            else if (_actualSelection > -1 && _PreviewSelected >= 0)
                                _Locked = _actualSelection;
                            break;

                        case Keys.Escape:
                        case Keys.Back:
                            if (CBase.Songs.GetCurrentCategoryIndex() > -1 && CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_ON && SongOptions.Selection.CategoryChangeAllowed)
                            {
                                ShowCategories();
                                KeyEvent.Handled = true;
                            }
                            //else if (CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_OFF)
                            //{
                            //    CGraphics.FadeTo(EScreens.ScreenMain);
                            //}
                            break;

                        case Keys.PageUp:
                            if (CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_ON && SongOptions.Selection.CategoryChangeAllowed)
                                PrevCategory();
                            break;

                        case Keys.PageDown:
                            if (CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_ON && SongOptions.Selection.CategoryChangeAllowed)
                                NextCategory();
                            break;

                        case Keys.Left:
                            if (_Locked > 0 && (!SongOptions.Selection.RandomOnly || SongOptions.Selection.CategoryChangeAllowed && CBase.Songs.GetCurrentCategoryIndex() < 0))
                            {
                                _Locked--;
                                UpdateList();
                            }
                            break;

                        case Keys.Right:
                            if (CBase.Songs.GetCurrentCategoryIndex() < 0 && SongOptions.Selection.CategoryChangeAllowed)
                            {
                                if (_Locked < CBase.Songs.GetNumCategories() - 1)
                                {
                                    _Locked++;
                                    UpdateList();
                                }
                            }
                            else
                            {
                                if (CBase.Songs.GetCurrentCategoryIndex() != -1 && _Locked < CBase.Songs.GetNumVisibleSongs() - 1 && !SongOptions.Selection.RandomOnly)
                                {
                                    _Locked++;
                                    UpdateList();
                                }
                            }
                            break;

                        case Keys.Up:
                            if (KeyEvent.ModSHIFT && CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_ON && SongOptions.Selection.CategoryChangeAllowed)
                            {
                                PrevCategory();
                                break;
                            }

                            if (_Locked > _NumW - 1 &&
                                (!SongOptions.Selection.RandomOnly || SongOptions.Selection.CategoryChangeAllowed && CBase.Songs.GetCurrentCategoryIndex() < 0))
                            {
                                _Locked -= _NumW;
                                UpdateList();
                            }
                            break;

                        case Keys.Down:
                            if (KeyEvent.ModSHIFT && CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_ON && SongOptions.Selection.CategoryChangeAllowed)
                            {
                                NextCategory();
                                break;
                            }

                            if (CBase.Songs.GetCurrentCategoryIndex() < 0 && SongOptions.Selection.CategoryChangeAllowed)
                            {
                                if (_Locked < CBase.Songs.GetNumCategories() - _NumW)
                                {
                                    _Locked += _NumW;
                                    UpdateList();
                                }
                            }
                            else if (_Locked < CBase.Songs.GetNumVisibleSongs() - _NumW && !SongOptions.Selection.RandomOnly)
                            {
                                _Locked += _NumW;
                                UpdateList();
                            }
                            break;
                    }
                }

                if (!KeyEvent.Handled)
                {
                    _PreviewSelected = _Locked;
                    _actualSelection = _Locked;

                    for (int i = 0; i < _Tiles.Count; i++)
                        _Tiles[i].Selected = _Locked == i + _Offset;
                }
            }
        }

        public override void HandleMouse(ref MouseEvent MouseEvent, ScreenSongOptions SongOptions)
        {
            base.HandleMouse(ref MouseEvent, SongOptions);

            int i = 0;
            bool sel = false;
            int lastselection = _actualSelection;

            if (!SongOptions.Selection.RandomOnly || CBase.Songs.GetCurrentCategoryIndex() < 0 && SongOptions.Selection.CategoryChangeAllowed)
            {
                foreach (CStatic tile in _Tiles)
                {
                    if ((tile.Texture.index != _CoverTexture.index) && CHelper.IsInBounds(tile.Rect, MouseEvent) && !sel)
                    {
                        if (MouseEvent.LB || CBase.Songs.GetCurrentCategoryIndex() == -1)
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
                        _actualSelection = i + _Offset;
                        sel = true;
                    }
                    else
                        tile.Selected = false;
                    i++;
                }
            }
            else
                sel = true;

            if (MouseEvent.Sender == ESender.WiiMote && _actualSelection != lastselection && _actualSelection != -1)
                CBase.Input.SetRumble(0.050f);

            if (!sel)
                _actualSelection = -1;

            if (MouseEvent.RB && (CBase.Songs.GetNumCategories() > 0) && CBase.Songs.GetCurrentCategoryIndex() >= 0 && CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_ON &&
                SongOptions.Selection.CategoryChangeAllowed)
            {
                ShowCategories();
                MouseEvent.Handled = true;
                return;
            }
            else if (MouseEvent.RB && CBase.Songs.GetTabs() == EOffOn.TR_CONFIG_OFF && !SongOptions.Selection.PartyMode)
                CBase.Graphics.FadeTo(EScreens.ScreenMain);
            else if (_PreviewSelected != -1 && MouseEvent.LB && CBase.Songs.GetCurrentCategoryIndex() != -1 && !SongOptions.Selection.PartyMode)
            {
                if (CHelper.IsInBounds(_CoverBig.Rect, MouseEvent) || CHelper.IsInBounds(_TextBG.Rect, MouseEvent))
                    _Locked = _PreviewSelected;
            }
            else if (MouseEvent.LB && (CBase.Songs.GetCurrentCategoryIndex() == -1))
            {
                foreach (CStatic tile in _Tiles)
                {
                    if ((tile.Texture.index != _CoverTexture.index) && CHelper.IsInBounds(tile.Rect, MouseEvent))
                    {
                        EnterCategory(_PreviewSelected);
                        MouseEvent.Handled = true;
                        return;
                    }
                }
            }

            if (MouseEvent.Wheel != 0 && CHelper.IsInBounds(_ScrollRect, MouseEvent) &&
                (!SongOptions.Selection.RandomOnly || CBase.Songs.GetCurrentCategoryIndex() < 0 && SongOptions.Selection.CategoryChangeAllowed))
                UpdateList(_Offset + _NumW * MouseEvent.Wheel);
        }

        public override void Draw()
        {
            foreach (CStatic tile in _Tiles)
            {
                if (tile.Selected && _Active)
                    tile.Draw(1.2f, tile.Rect.Z - 0.1f, EAspect.Crop, false);
                else
                {
                    if (tile.Texture.index != _CoverTexture.index)
                        tile.Draw(1f, tile.Rect.Z, EAspect.Crop, false);
                    else
                        tile.Draw(1f, tile.Rect.Z, EAspect.Stretch, false);
                }
            }

            if (CBase.Songs.GetCurrentCategoryIndex() >= 0)
            {
                int actsong = _PreviewSelected;
                if ((CBase.Songs.GetNumVisibleSongs() > actsong) && (actsong >= 0))
                {
                    CSong song = CBase.Songs.GetVisibleSong(actsong);

                    _CoverBig.Texture = song.CoverTextureBig;
                    _Artist.Text = song.Artist;
                    _Title.Text = song.Title;
                    _DuetIcon.Visible = song.IsDuet;
                    _VideoIcon.Visible = song.VideoFileName.Length > 0;
                    _MedleyCalcIcon.Visible = song.Medley.Source == EMedleySource.Calculated;
                    _MedleyTagIcon.Visible = song.Medley.Source == EMedleySource.Tag;

                    float Time = CBase.Sound.GetLength(_SongStream);
                    if (song.Finish != 0)
                        Time = song.Finish;

                    Time -= song.Start;
                    int min = (int)Math.Floor(Time / 60f);
                    int sec = (int)(Time - min * 60f);
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
                    if (num != 1)
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


            if (_vidtex.index != -1 && _Video != -1)
            {
                if (_vidtex.color.A < 1)
                    _CoverBig.Draw(1f, EAspect.Crop);
                RectangleF bounds = new RectangleF(_CoverBig.Rect.X, _CoverBig.Rect.Y, _CoverBig.Rect.W, _CoverBig.Rect.H);
                RectangleF rect = new RectangleF(0f, 0f, _vidtex.width, _vidtex.height);
                CHelper.SetRect(bounds, ref rect, rect.Width / rect.Height, EAspect.Crop);
                SRectF vidRect = new SRectF(rect.X, rect.Y, rect.Width, rect.Height, _CoverBig.Rect.Z);
                SRectF vidRectBounds = new SRectF(bounds.X, bounds.Y, bounds.Width, bounds.Height, 0f);

                CBase.Drawing.DrawTexture(_vidtex, vidRect, _vidtex.color, vidRectBounds, false);
                CBase.Drawing.DrawTextureReflection(_vidtex, vidRect, _vidtex.color, vidRectBounds, _CoverBig.ReflectionSpace, _CoverBig.ReflectionHeight);
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

        protected void SetSelectedTile(int itemNr)
        {
            bool sel = false;
            foreach (CStatic tile in _Tiles)
            {
                if (tile.Selected)
                {
                    sel = true;
                    break;
                }
            }

            if (_Locked == -1 || !sel)
            {
                if (_PreviewSelected > -1)
                    _Locked = _PreviewSelected;
                else
                {
                    _Locked = 0;
                    _actualSelection = 0;
                    _PreviewSelected = 0;
                    SetSelectedNow();
                    UpdateList(0, true);
                }
            }

            foreach (CStatic tile in _Tiles)
                tile.Selected = false;

            _PreviewSelected = itemNr;
            SetSelectedNow();
            _Locked = itemNr;

            UpdateList(true);

            _PreviewSelected = _Locked;
            _actualSelection = _Locked;

            if (_Locked - _Offset >= 0)
                _Tiles[_Locked - _Offset].Selected = true;
        }

        public override void SetSelectedSong(int VisibleSongNr)
        {
            base.SetSelectedSong(VisibleSongNr);

            if (VisibleSongNr >= 0 && VisibleSongNr < CBase.Songs.GetNumVisibleSongs())
                SetSelectedTile(VisibleSongNr);
        }

        public override void SetSelectedCategory(int CategoryNr)
        {
            base.SetSelectedCategory(CategoryNr);

            if (CategoryNr >= 0 && CategoryNr < CBase.Songs.GetNumCategories())
                SetSelectedTile(CategoryNr);
        }

        protected override void EnterCategory(int cat)
        {
            base.EnterCategory(cat);

            _PreviewSelected = 0;
            _Locked = -1;
            _actualSelection = 0;
            AfterCategoryChange();
            _Locked = 0;
        }

        protected override void ShowCategories()
        {
            base.ShowCategories();

            _PreviewSelected = 0;
            _Locked = -1;
            _actualSelection = 0;
            AfterCategoryChange();
            _Locked = 0;
        }

        private void NextCategory()
        {
            if (CBase.Songs.GetCurrentCategoryIndex() > -1)
            {
                CBase.Songs.NextCategory();
                EnterCategory(CBase.Songs.GetCurrentCategoryIndex());
            }
        }

        private void PrevCategory()
        {
            if (CBase.Songs.GetCurrentCategoryIndex() > -1)
            {
                CBase.Songs.PrevCategory();
                EnterCategory(CBase.Songs.GetCurrentCategoryIndex());
            }
        }

        private void AfterCategoryChange()
        {
            SetSelectedNow();
            SelectSong(_PreviewSelected);

            foreach (CStatic tile in _Tiles)
                tile.Selected = false;

            if (_actualSelection >= 0 && _actualSelection < _Tiles.Count)
                _Tiles[_actualSelection].Selected = true;

            if ((_LastKnownNumSongs == CBase.Songs.GetNumVisibleSongs()) && (_LastKnownCategory == CBase.Songs.GetCurrentCategoryIndex()))
                return;

            _LastKnownCategory = CBase.Songs.GetCurrentCategoryIndex();
            _LastKnownNumSongs = CBase.Songs.GetNumVisibleSongs();
            UpdateList(0, true);
            CBase.Songs.UpdateRandomSongList();
        }

        private void UpdateList(bool force = false)
        {
            UpdateList((_Locked / _NumW) * _NumW - (_NumW * (_NumH - 2)), force);
        }

        private void UpdateList(int offset, bool force = false)
        {
            bool isInCategory = CBase.Songs.GetCurrentCategoryIndex() >= 0;
            int itemCount = isInCategory ? CBase.Songs.GetNumVisibleSongs() : CBase.Songs.GetNumCategories();

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
                    if (isInCategory)
                        _Tiles[i].Texture = CBase.Songs.GetVisibleSong(i + offset).CoverTextureSmall;
                    else
                        _Tiles[i].Texture = CBase.Songs.GetCategory(i + offset).CoverTextureSmall;
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

        public override void SetSmallView(bool SmallView)
        {
            base.SetSmallView(SmallView);

            _SmallView = SmallView;

            if (_SmallView)
            {
                _NumH = _Theme.songMenuTileBoard.numHsmall;
                _NumW = _Theme.songMenuTileBoard.numWsmall;
                _Rect = _Theme.songMenuTileBoard.TileRectSmall;
            }
            else
            {
                _NumH = _Theme.songMenuTileBoard.numH;
                _NumW = _Theme.songMenuTileBoard.numW;
                _Rect = _Theme.songMenuTileBoard.TileRect;
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
            UpdateList(true);
        }

        public override bool IsSmallView()
        {
            return _SmallView;
        }

        public override void LoadTextures()
        {
            base.LoadTextures();

            _Theme.songMenuTileBoard.StaticCoverBig.ReloadTextures();
            _Theme.songMenuTileBoard.StaticTextBG.ReloadTextures();
            _Theme.songMenuTileBoard.StaticDuetIcon.ReloadTextures();
            _Theme.songMenuTileBoard.StaticVideoIcon.ReloadTextures();
            _Theme.songMenuTileBoard.StaticMedleyCalcIcon.ReloadTextures();
            _Theme.songMenuTileBoard.StaticMedleyTagIcon.ReloadTextures();

            _Theme.songMenuTileBoard.TextArtist.ReloadTextures();
            _Theme.songMenuTileBoard.TextTitle.ReloadTextures();
            _Theme.songMenuTileBoard.TextSongLength.ReloadTextures();
        }
    }
}