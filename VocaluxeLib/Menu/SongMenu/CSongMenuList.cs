﻿#region license
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
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.PartyModes;
using VocaluxeLib.Songs;
using VocaluxeLib.Xml;

namespace VocaluxeLib.Menu.SongMenu
{
    class CSongMenuList : CSongMenuFramework
    {
        private SRectF _ScrollRect;
        private List<CStatic> _Tiles;
        private List<CText> _Texts;
        private List<CText> _Records;
        private readonly CStatic _CoverBig;
        private readonly CStatic _TextBG;
        private readonly CStatic _DuetIcon;
        private readonly CStatic _VideoIcon;
        private readonly CStatic _MedleyCalcIcon;
        private readonly CStatic _MedleyTagIcon;

        private CTextureRef _CoverBigBGTexture;
        private CTextureRef _CoverBGTexture;

        private readonly CText _Artist;
        private readonly CText _Title;
        private readonly CText _SongLength;

        private float _SpaceW;
        private float _SpaceH;

        private int _TileW;
        private int _TileH;

        private int _ListLength;
        private float _ListTextWidth;

        private int _NumRecords;
        private ERecordStyle _RecordStyle;
        private EAspect _VideoAspect;


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
                int max = CBase.Songs.IsInCategory() ? CBase.Songs.GetNumSongsVisible() : CBase.Songs.GetNumCategories();
                base._SelectionNr = value.Clamp(-1, max - 1, true);
                //Update list in case we scrolled 
                _UpdateList();

                _UpdateTileSelection();
            }
        }

        public override SRectF VideoRect
        {
            get
            {
                if (SmallView)
                {
                    return _Theme.SongMenuList.VideoRectSmall;
                }
                else{
                    return _Theme.SongMenuList.VideoRect;
                }
            }
        }

        public override bool SmallView
        {
            set
            {
                if (SmallView == value)
                    return;
                base.SmallView = value;

                if (value)
                {
                    _CoverBig.MaxRect = _Theme.SongMenuList.VideoRectSmall;
                    _TextBG.MaxRect = _Theme.SongMenuList.VideoRectSmall;
                    _VideoAspect = EAspect.Crop;
                    _RecordStyle = ERecordStyle.RECORDSTYLE_NONE;
                }
                else
                {
                    _CoverBig.MaxRect = _Theme.SongMenuList.VideoRect;
                    _TextBG.MaxRect = _Theme.SongMenuList.VideoRect;
                    _VideoAspect = EAspect.LetterBox;
                    _RecordStyle = _Theme.SongMenuList.RecordStyle;
                }

                _InitTiles();
                _UpdateList(true);
                _UpdateTileSelection();
                _UpdatePreview();
            }
        }

        protected override int _PreviewNr
        {
            set
            {
                if (value == base._PreviewNr)
                {
                    if (!CBase.BackgroundMusic.IsPlaying() && value != -1)
                        CBase.BackgroundMusic.Play();
                    return;
                }
                base._PreviewNr = value;
                _UpdatePreview();
            }
        }

        public CSongMenuList(SThemeSongMenu theme, int partyModeID) : base(theme, partyModeID)
        {
            _ListLength = _Theme.SongMenuList.ListLength;
            _SpaceW = _Theme.SongMenuList.SpaceW;
            _SpaceH = _Theme.SongMenuList.SpaceH;
            _NumRecords = _Theme.SongMenuList.NumRecords;
            _RecordStyle = _Theme.SongMenuList.RecordStyle;
            _Artist = new CText(_Theme.SongMenuList.TextArtist, _PartyModeID);
            _Title = new CText(_Theme.SongMenuList.TextTitle, _PartyModeID);
            _SongLength = new CText(_Theme.SongMenuList.TextSongLength, _PartyModeID);
            _CoverBig = new CStatic(_Theme.SongMenuList.StaticCoverBig, _PartyModeID);
            _TextBG = new CStatic(_Theme.SongMenuList.StaticTextBG, _PartyModeID);
            _DuetIcon = new CStatic(_Theme.SongMenuList.StaticDuetIcon, _PartyModeID);
            _VideoIcon = new CStatic(_Theme.SongMenuList.StaticVideoIcon, _PartyModeID);
            _MedleyCalcIcon = new CStatic(_Theme.SongMenuList.StaticMedleyCalcIcon, _PartyModeID);
            _MedleyTagIcon = new CStatic(_Theme.SongMenuList.StaticMedleyTagIcon, _PartyModeID);
            _SubElements.AddRange(new IMenuElement[] {_Artist, _Title, _SongLength, _DuetIcon, _VideoIcon, _MedleyCalcIcon, _MedleyTagIcon});
        }

        private void _ReadSubTheme()
        {
            _Theme.SongMenuList.TextArtist = (SThemeText)_Artist.GetTheme();
            _Theme.SongMenuList.TextSongLength = (SThemeText)_SongLength.GetTheme();
            _Theme.SongMenuList.TextTitle = (SThemeText)_Title.GetTheme();
            _Theme.SongMenuList.StaticCoverBig = (SThemeStatic)_CoverBig.GetTheme();
            _Theme.SongMenuList.StaticDuetIcon = (SThemeStatic)_DuetIcon.GetTheme();
            _Theme.SongMenuList.StaticMedleyCalcIcon = (SThemeStatic)_MedleyCalcIcon.GetTheme();
            _Theme.SongMenuList.StaticMedleyTagIcon = (SThemeStatic)_MedleyTagIcon.GetTheme();
            _Theme.SongMenuList.StaticTextBG = (SThemeStatic)_TextBG.GetTheme();
            _Theme.SongMenuList.StaticVideoIcon = (SThemeStatic)_VideoIcon.GetTheme();
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
            _InitRecords();
        }

        private void _InitTiles()
        {
            MaxRect = SmallView ? _Theme.SongMenuList.TileRectSmall : _Theme.SongMenuList.TileRect;

            _ListTextWidth = MaxRect.W - _ListTextWidth;

            _TileW = (int)((MaxRect.H - _SpaceH * (_ListLength - 1)) / _ListLength);
            _TileH = _TileW;

            _CoverBGTexture = CBase.Themes.GetSkinTexture(_Theme.CoverBackground, _PartyModeID);
            _CoverBigBGTexture = CBase.Themes.GetSkinTexture(_Theme.CoverBigBackground, _PartyModeID);

            //Create cover tiles
            _Tiles = new List<CStatic>();
            _Texts = new List<CText>();

            for (int i = 0; i < _ListLength; i++)
            {
                //Create Cover
                var rect = new SRectF(Rect.X + (_TileW + _SpaceW), Rect.Y + i * (_TileH + _SpaceH), _TileW, _TileH, Rect.Z);
                var tile = new CStatic(_PartyModeID, _CoverBGTexture, _Color, rect);
                _Tiles.Add(tile);

                //Create text for Artist
                var textRect = new SRectF(MaxRect.X + 2 * (_TileW + _SpaceW), Rect.Y + i * (_TileH + _SpaceH), _ListTextWidth, _TileH, Rect.Z);
                CText text = new CText(textRect.X, textRect.Y, textRect.Z,
                                       textRect.H / 2, textRect.W, EAlignment.Left, EStyle.Normal,
                                       "Normal", _Artist.Color, "");
                text.MaxRect = new SRectF(text.MaxRect.X, text.MaxRect.Y, MaxRect.W + MaxRect.X - text.Rect.X - 5f, text.MaxRect.H / 2, text.MaxRect.Z);
                text.ResizeAlign = EHAlignment.Center;

                _Texts.Add(text);

                //Create text for Title
                textRect = new SRectF(MaxRect.X + 2 * (_TileW + _SpaceW), Rect.Y + i * (_TileH + _SpaceH), _ListTextWidth, _TileH, Rect.Z);
                text = new CText(textRect.X, textRect.Y + (textRect.H / 2) - _SpaceH/2, textRect.Z,
                                       textRect.H / 2, textRect.W, EAlignment.Left, EStyle.Normal,
                                       "Normal", _Artist.Color, "");
                text.MaxRect = new SRectF(text.MaxRect.X, text.MaxRect.Y, MaxRect.W + MaxRect.X - text.Rect.X - 5f, text.MaxRect.H, text.MaxRect.Z);
                text.ResizeAlign = EHAlignment.Center;

                _Texts.Add(text);
            }

            _ScrollRect = CBase.Settings.GetRenderRect();
        }

        private void _InitRecords()
        {
            SRectF rect = _Theme.SongMenuList.TileRectRecord;
            _Records = new List<CText>();

            for (int i = 0; i < _NumRecords; i++)
            {
                
                //Create text for Record
                var textRect = new SRectF(rect.X, rect.Y + i * (rect.H / _NumRecords), rect.W, rect.H / _NumRecords, rect.Z);
                CText text = new CText(textRect.X, textRect.Y, textRect.Z,
                                       textRect.H, textRect.W, EAlignment.Left, EStyle.Normal,
                                       "Normal", _Artist.Color, "");
                text.MaxRect = new SRectF(text.MaxRect.X, text.MaxRect.Y, text.MaxRect.W + text.MaxRect.X - text.Rect.X - 5f, text.MaxRect.H, text.MaxRect.Z);
                text.ResizeAlign = EHAlignment.Center;
                text.Font.Style = EStyle.Bold;

                _Records.Add(text);
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

        public override void Update(SScreenSongOptions songOptions)
        {
            if (songOptions.Selection.RandomOnly)
                _PreviewSelectedSong();

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
                _UpdateRecords(song);
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

        private void _UpdateRecords(CSong song)
        {
            if (!CBase.Songs.IsInCategory() || _RecordStyle == ERecordStyle.RECORDSTYLE_NONE)
            {
                _ResetRecords();
                return;
            }

            if (song == null)
                return;

            List<SDBScoreEntry> _Scores = CBase.DataBase.LoadScore(song.ID,EGameMode.TR_GAMEMODE_NORMAL, EHighscoreStyle.TR_CONFIG_HIGHSCORE_LIST_ALL);
            List<String> players = new List<string>();

            if (_RecordStyle == ERecordStyle.RECORDSTYLE_ALL)
            {
                for(int i = 0; i < _Records.Count; i++)
                {
                    if (i < _Scores.Count)
                    {
                        SDBScoreEntry score = _Scores[i];
                        _Records[i].Text = (i + 1) + ". " + score.Name + " " + score.Score + " (" + CBase.Language.Translate(score.Difficulty.ToString()) + ")";
                    }else
                    {
                        _Records[i].Text = "";
                    }
                }
            }
            else
            {
                int k = 0;
                for (int i = 0; i < _Records.Count; i++)
                {
                    if (k < _Scores.Count)
                    {
                        SDBScoreEntry score = _Scores[k];
    
                        String recordName = "";

                        if (_RecordStyle == ERecordStyle.RECORDSTYLE_DIFFICULTY)
                        {
                            recordName = score.Name + score.Difficulty;
                        }
                        else
                        {
                            recordName = score.Name;
                        }

                        while (players.Contains(recordName))
                        {
                            k++;
                            if (k < _Scores.Count)
                            {
                                score = _Scores[k];

                                if (_RecordStyle == ERecordStyle.RECORDSTYLE_DIFFICULTY)
                                {
                                    recordName = score.Name + score.Difficulty;
                                }
                                else
                                {
                                    recordName = score.Name;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (!players.Contains(recordName))
                        {
                            _Records[i].Text = (i + 1) + ". " + score.Name + " " + score.Score + " (" + CBase.Language.Translate(score.Difficulty.ToString()) + ")"; //+ score.Date;
                            players.Add(recordName);
                        }
                        else
                        {
                            _Records[i].Text = "";
                        }
                    }
                    else
                    {
                        _Records[i].Text = "";
                    }
                }
            }
        }

        private void _ResetRecords()
        {
            for (int i = 0; i < _Records.Count; i++)
            {
                _Records[i].Text = "";
            }
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
            _PreviewSelectedSong();
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
                _PreviewSelectedSong();
            return keyEvent.Handled;
        }

        public override bool HandleMouse(ref SMouseEvent mouseEvent, SScreenSongOptions songOptions)
        {
            if (!songOptions.Selection.RandomOnly || (!CBase.Songs.IsInCategory() && songOptions.Selection.CategoryChangeAllowed))
            {
                if (mouseEvent.Wheel != 0 && CHelper.IsInBounds(_ScrollRect, mouseEvent))
                    _UpdateList(_Offset +  mouseEvent.Wheel);

                int lastSelection = _SelectionNr;
                int i = 0;
                bool somethingSelected = false;

                foreach (CStatic tile in _Tiles)
                {
                    //create a rect including the cover and text of the song. 
                    //(this way mouse over text should make a selection as well)
                    SRectF songRect = new SRectF(tile.Rect.X, tile.Rect.Y, Rect.W, tile.Rect.H, tile.Rect.Z);
                    if (tile.Texture != _CoverBGTexture && CHelper.IsInBounds(songRect, mouseEvent) && tile.Color.A != 0)
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
                            return false;
                        _PreviewSelectedSong();
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
                if (tile.Selected)
                    tile.Draw(EAspect.Crop, SelectedTileZoomFactor, -0.1f);
                else
                {
                    EAspect aspect = (tile.Texture != _CoverBGTexture) ? EAspect.Crop : EAspect.Stretch;
                    tile.Draw(aspect);
                }
            }

            for (int k = 0; k < _Tiles.Count; k++)
            {
                CText artist = _Texts[k * 2];
                CText title = _Texts[k * 2 + 1];

                if (_Tiles[k].Selected)
                {
                    artist.Font.Style = EStyle.BoldItalic;
                    title.Font.Style = EStyle.BoldItalic;
                }
                else
                {
                    artist.Font.Style = EStyle.Normal;
                    title.Font.Style = EStyle.Normal;
                }
                
                artist.Draw();
                title.Draw();
            }

            foreach (CText record in _Records)
            {
                record.Draw();
            }

            _TextBG.Draw();

            CTextureRef vidtex = CBase.BackgroundMusic.IsPlayingPreview() ? CBase.BackgroundMusic.GetVideoTexture() : null;

            if (vidtex != null)
            {
                if (vidtex.Color.A < 1)
                    _CoverBig.Draw(_VideoAspect);
                
                SRectF rect = CHelper.FitInBounds(_CoverBig.Rect, vidtex.OrigAspect, _VideoAspect);
                CBase.Drawing.DrawTexture(vidtex, rect, vidtex.Color, _CoverBig.Rect);
                CBase.Drawing.DrawTextureReflection(vidtex, rect, vidtex.Color, _CoverBig.Rect, _CoverBig.ReflectionSpace, _CoverBig.ReflectionHeight);
            }
            else
                _CoverBig.Draw(_VideoAspect);

            foreach (IMenuElement element in _SubElements)
                element.Draw();
        }

        public override CStatic GetSelectedSongCover()
        {
            return _Tiles.FirstOrDefault(tile => tile.Selected);
        }

        public override bool IsMouseOverSelectedSong(SMouseEvent mEvent)
        {
            for (int i = 0; i < _Tiles.Count; i++)
            {
                if (!_Tiles[i].Selected)
                    continue;
                return CHelper.IsInBounds(_Tiles[i].Rect, mEvent) 
                    || CHelper.IsInBounds(_Texts[i * 2].Rect, mEvent)
                    || CHelper.IsInBounds(_Texts[i * 2 + 1].Rect, mEvent);
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

            SetSelectedCategory(0);
            _UpdateListIfRequired();
            _ResetRecords();
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

        private void _UpdateList(bool force = false)
        {
            int offset;
            if (_SelectionNr < _Offset && _SelectionNr >= 0)
                offset = _SelectionNr;
            else if (_SelectionNr >= _Offset + _ListLength)
                offset = _SelectionNr - _ListLength + 1;
            else
                offset = _Offset;
            _UpdateList(offset, force);
        }

        private void _UpdateList(int offset, bool force = false)
        {
            bool isInCategory = CBase.Songs.IsInCategory();
            int itemCount = isInCategory ? CBase.Songs.GetNumSongsVisible() : CBase.Songs.GetNumCategories();
            int totalSongNumber = CBase.Songs.GetNumSongs();

            offset = offset.Clamp(0, itemCount - _ListLength, true);

            if (offset == _Offset && !force)
                return;

            for (int i = 0; i < _Tiles.Count; i++)
            {
                if (i + offset < itemCount)
                {
                    _Tiles[i].Color = new SColorF(1f, 1f, 1f, 1f);
                    if (isInCategory)
                    {
                        CSong currentSong = CBase.Songs.GetVisibleSong(i + offset);
                        _Tiles[i].Texture = currentSong.CoverTextureSmall;
                        _Texts[i * 2].Text = currentSong.Artist;
                        _Texts[i * 2 + 1].Text = currentSong.Title;
                    }
                    else
                    {
                        CCategory currentCat = CBase.Songs.GetCategory(i + offset);
                        _Tiles[i].Texture = currentCat.CoverTextureSmall;
                        int num = currentCat.GetNumSongsNotSung();
                        String songOrSongs = (num == 1) ? "TR_SCREENSONG_NUMSONG" : "TR_SCREENSONG_NUMSONGS";
                        _Texts[i * 2].Text = currentCat.Name + " " + CBase.Language.Translate(songOrSongs).Replace("%v", num.ToString());
                        _Texts[i * 2 + 1].Text = "";
                    }
                }
                else
                {
                    _Tiles[i].Color.A = 0;
                    _Texts[i * 2].Text = "";
                    _Texts[i * 2 + 1].Text = "";
                }
            }
            _Offset = offset;
        }

        public override void LoadSkin()
        {
            foreach (IThemeable themeable in _SubElements.OfType<IThemeable>())
                themeable.LoadSkin();
            // Those are drawn seperately so they are not in the above list
            _CoverBig.LoadSkin();
            _TextBG.LoadSkin();

            base.LoadSkin();
        }
    }
}