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
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;
using VocaluxeLib.PartyModes;
using VocaluxeLib.Songs;

namespace Vocaluxe.Screens
{
    class CScreenSong : CMenu
    {
        private enum ESongOptionsView
        {
            None,
            Song,
            General,
            Medley
        }

        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 5; }
        }

        private const string _TextCategory = "TextCategory";
        private const string _TextSelection = "TextSelection";
        private const string _TextSearchBarTitle = "TextSearchBarTitle";
        private const string _TextSearchBar = "TextSearchBar";
        private const string _TextOptionsTitle = "TextOptionsTitle";

        private const string _ButtonOpenOptions = "ButtonOpenOptions";
        private const string _ButtonOptionsSing = "ButtonOptionsSing";
        private const string _ButtonOptionsPlaylist = "ButtonOptionsPlaylist";
        private const string _ButtonOptionsClose = "ButtonOptionsClose";
        private const string _ButtonOptionsRandom = "ButtonOptionsRandom";
        private const string _ButtonOptionsRandomCategory = "ButtonOptionsRandomCategory";
        private const string _ButtonOptionsSingAll = "ButtonOptionsSingAll";
        private const string _ButtonOptionsSingAllVisible = "ButtonOptionsSingAllVisible";
        private const string _ButtonOptionsOpenSelectedItem = "ButtonOptionsOpenSelectedItem";
        private const string _ButtonOptionsRandomMedley = "ButtonOptionsRandomMedley";
        private const string _ButtonOptionsStartMedley = "ButtonOptionsStartMedley";
        private const string _ButtonStart = "ButtonStart";

        private const string _SelectSlideOptionsMode = "SelectSlideOptionsMode";
        private const string _SelectSlideOptionsPlaylistAdd = "SelectSlideOptionsPlaylistAdd";
        private const string _SelectSlideOptionsPlaylistOpen = "SelectSlideOptionsPlaylistOpen";
        private const string _SelectSlideOptionsNumMedleySongs = "SelectSlideOptionsNumMedleySongs";

        private const string _StaticSearchBar = "StaticSearchBar";
        private const string _StaticOptionsBG = "StaticOptionsBG";
        private const string _SongMenu = "SongMenu";
        private const string _Playlist = "Playlist";

        private string _SearchText = String.Empty;
        private bool _SearchActive;

        private readonly List<string> _ButtonsJoker = new List<string>();
        private readonly List<string> _TextsPlayer = new List<string>();
        private bool _SongOptionsActive;
        private bool _PlaylistActive;
        private readonly List<EGameMode> _AvailableGameModes;
        private SScreenSongOptions _Sso;

        private CStatic _DragAndDropCover;
        private bool _DragAndDropActive;
        private int _OldMousePosX;
        private int _OldMousePosY;

        private int _SelectedSongID;
        private int _SelectedCategoryIndex;

        public CScreenSong()
        {
            _AvailableGameModes = new List<EGameMode>();
        }

        public override void Init()
        {
            base.Init();

            _ButtonsJoker.Clear();
            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
                _ButtonsJoker.Add("ButtonJoker" + (i + 1));
            var blist = new List<string>();
            blist.AddRange(_ButtonsJoker);
            blist.Add(_ButtonOptionsClose);
            blist.Add(_ButtonOptionsPlaylist);
            blist.Add(_ButtonOptionsSing);
            blist.Add(_ButtonOptionsRandom);
            blist.Add(_ButtonOptionsRandomCategory);
            blist.Add(_ButtonOptionsSingAll);
            blist.Add(_ButtonOptionsSingAllVisible);
            blist.Add(_ButtonOptionsOpenSelectedItem);
            blist.Add(_ButtonOpenOptions);
            blist.Add(_ButtonStart);
            blist.Add(_ButtonOptionsRandomMedley);
            blist.Add(_ButtonOptionsStartMedley);

            _TextsPlayer.Clear();
            for (int i = 0; i < CSettings.MaxNumPlayer; i++)
                _TextsPlayer.Add("TextPlayer" + (i + 1));
            var tlist = new List<string>();
            tlist.AddRange(_TextsPlayer);
            tlist.Add(_TextCategory);
            tlist.Add(_TextSelection);
            tlist.Add(_TextSearchBarTitle);
            tlist.Add(_TextSearchBar);
            tlist.Add(_TextOptionsTitle);

            _ThemeStatics = new string[] {_StaticSearchBar, _StaticOptionsBG};
            _ThemeTexts = tlist.ToArray();
            _ThemeButtons = blist.ToArray();
            _ThemeSelectSlides = new string[] {_SelectSlideOptionsMode, _SelectSlideOptionsPlaylistAdd, _SelectSlideOptionsPlaylistOpen, _SelectSlideOptionsNumMedleySongs};
            _ThemeSongMenus = new string[] {_SongMenu};
            _ThemePlaylists = new string[] {_Playlist};
        }

        public override void LoadTheme(string xmlPath)
        {
            base.LoadTheme(xmlPath);
            _ToggleSongOptions(ESongOptionsView.None);
            _Playlists[_Playlist].Visible = false;

            _DragAndDropCover = GetNewStatic();

            _Playlists[_Playlist].Init();

            _AvailableGameModes.Clear();

            ApplyVolume();
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);
            if (keyEvent.Handled)
                return true;

            if (_PlaylistActive)
            {
                _Playlists[_Playlist].HandleInput(keyEvent);
                return true;
            }

            if (!_SongOptionsActive)
            {
                if (keyEvent.KeyPressed && !Char.IsControl(keyEvent.Unicode) && keyEvent.Mod != EModifier.Ctrl)
                {
                    if (_SearchActive)
                        _ApplyNewSearchFilter(_SearchText + keyEvent.Unicode);
                    else if (!_Sso.Selection.PartyMode)
                    {
                        _JumpTo(keyEvent.Unicode);
                        return true;
                    }
                }
                else
                {
                    _SongMenus[_SongMenu].HandleInput(ref keyEvent, _Sso);
                    _UpdatePartyModeOptions();

                    if (keyEvent.Handled)
                        return true;

                    switch (keyEvent.Key)
                    {
                        case Keys.Escape:
                            if ((CSongs.Category < 0 || _Sso.Sorting.Tabs == EOffOn.TR_CONFIG_OFF) && !_Sso.Selection.PartyMode && !_SearchActive)
                                CGraphics.FadeTo(EScreens.ScreenMain);
                            if (_SearchActive)
                            {
                                _SearchActive = false;
                                _SearchText = String.Empty;
                                _ApplyNewSearchFilter(_SearchText);
                            }
                            break;

                        case Keys.Enter:
                            if (_Sso.Selection.RandomOnly && _Sso.Selection.NumJokers != null)
                            {
                                for (int i = 0; i < _ButtonsJoker.Count; i++)
                                {
                                    if (i < _Sso.Selection.NumJokers.Length)
                                    {
                                        if (_Buttons[_ButtonsJoker[i]].Selected)
                                        {
                                            _SelectNextRandom(i);
                                            return true;
                                        }
                                    }
                                }
                                if (_Buttons[_ButtonStart].Selected)
                                    _HandlePartySongSelection(_SongMenus[_SongMenu].GetSelectedSong());
                            }
                            if (CSongs.NumSongsVisible > 0 && !_Sso.Selection.PartyMode)
                            {
                                if (_SongMenus[_SongMenu].GetSelectedSong() != -1 && !_SongOptionsActive)
                                {
                                    if (!_Sso.Selection.PartyMode)
                                        _ToggleSongOptions(ESongOptionsView.Song);
                                }
                            }
                            break;

                        case Keys.Tab:
                            if (_Playlists[_Playlist].Visible)
                            {
                                _PlaylistActive = !_PlaylistActive;
                                _Playlists[_Playlist].Selected = _PlaylistActive;
                                _SongMenus[_SongMenu].SetActive(!_PlaylistActive);
                            }
                            break;

                        case Keys.Back:
                            if (_SearchActive && _SearchText != "")
                                _ApplyNewSearchFilter(_SearchText.Remove(_SearchText.Length - 1));

                            if ((CSongs.Category < 0 || _Sso.Sorting.Tabs == EOffOn.TR_CONFIG_OFF) && !_Sso.Selection.PartyMode && !_SearchActive)
                                CGraphics.FadeTo(EScreens.ScreenMain);

                            break;

                        case Keys.F3:
                            if (_SearchActive)
                            {
                                _SearchActive = false;
                                _SearchText = String.Empty;
                                _ApplyNewSearchFilter(_SearchText);
                            }
                            else if (!_Sso.Selection.PartyMode)
                                _SearchActive = true;
                            break;
                    }
                    if (!_SearchActive)
                    {
                        switch (keyEvent.Key)
                        {
                            case Keys.Space:
                                if (!_Sso.Selection.PartyMode)
                                    _ToggleSongOptions(ESongOptionsView.General);
                                break;

                            case Keys.A:
                                if (keyEvent.Mod == EModifier.Ctrl && !_Sso.Selection.PartyMode)
                                    _StartRandomAllSongs();
                                break;
                            case Keys.V:
                                if (keyEvent.Mod == EModifier.Ctrl && !_Sso.Selection.PartyMode)
                                    _StartRandomVisibleSongs();
                                break;

                            case Keys.R:
                                if (keyEvent.Mod == EModifier.Ctrl && !_Sso.Selection.RandomOnly)
                                    _SelectNextRandom(-1);
                                break;

                            case Keys.S:
                                if (keyEvent.Mod == EModifier.Ctrl && CSongs.NumSongsVisible > 0 && !_Sso.Selection.PartyMode)
                                    _StartMedleySong(_SongMenus[_SongMenu].GetSelectedSong());
                                break;

                            case Keys.D1:
                            case Keys.NumPad1:
                                _SelectNextRandom(0);
                                break;

                            case Keys.D2:
                            case Keys.NumPad2:
                                _SelectNextRandom(1);
                                break;

                            case Keys.D3:
                            case Keys.NumPad3:
                                _SelectNextRandom(2);
                                break;

                            case Keys.D4:
                            case Keys.NumPad4:
                                _SelectNextRandom(3);
                                break;

                            case Keys.D5:
                            case Keys.NumPad5:
                                _SelectNextRandom(4);
                                break;

                            case Keys.D6:
                            case Keys.NumPad6:
                                _SelectNextRandom(5);
                                break;
                        }
                    }
                }
            }
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Enter:
                        if (_Buttons[_ButtonOptionsClose].Selected)
                            _ToggleSongOptions(ESongOptionsView.None);
                        else if (_Buttons[_ButtonOptionsSing].Selected)
                        {
                            _ToggleSongOptions(ESongOptionsView.None);
                            _StartSong(_SongMenus[_SongMenu].GetSelectedSong());
                        }
                        else if (_Buttons[_ButtonOptionsPlaylist].Selected)
                        {
                            _ToggleSongOptions(ESongOptionsView.None);
                            _OpenAndAddPlaylistAction();
                        }
                        else if (_Buttons[_ButtonOptionsRandom].Selected)
                        {
                            if (CSongs.IsInCategory)
                                _SongMenus[_SongMenu].SetSelectedSong(CSongs.GetRandomSong());
                            else
                                _SongMenus[_SongMenu].SetSelectedCategory(CSongs.GetRandomCategory());
                        }
                        else if (_Buttons[_ButtonOptionsSingAll].Selected)
                            _StartRandomAllSongs();
                        else if (_Buttons[_ButtonOptionsSingAllVisible].Selected)
                            _StartRandomVisibleSongs();
                        else if (_Buttons[_ButtonOptionsOpenSelectedItem].Selected)
                            _HandleSelectButton();
                        else if (_SelectSlides[_SelectSlideOptionsPlaylistOpen].Selected)
                            _OpenPlaylist(_SelectSlides[_SelectSlideOptionsPlaylistOpen].Selection);
                        else if (_SelectSlides[_SelectSlideOptionsPlaylistAdd].Selected)
                            _OpenPlaylist(_SelectSlides[_SelectSlideOptionsPlaylistAdd].Selection - 1);
                        else if (_Buttons[_ButtonOptionsRandomMedley].Selected)
                            _ToggleSongOptions(ESongOptionsView.Medley);
                        else if (_Buttons[_ButtonOptionsStartMedley].Selected)
                        {
                            _ToggleSongOptions(ESongOptionsView.None);
                            _StartRandomMedley(_SelectSlides[_SelectSlideOptionsNumMedleySongs].Selection + 1, !CSongs.IsInCategory);
                        }
                        break;

                    case Keys.Escape:
                    case Keys.Back:
                    case Keys.Space:
                        _ToggleSongOptions(ESongOptionsView.None);
                        break;
                }
            }

            if (keyEvent.ModShift && (keyEvent.Key == Keys.Add || keyEvent.Key == Keys.PageUp))
            {
                CConfig.PreviewMusicVolume = CConfig.PreviewMusicVolume + 5;
                if (CConfig.PreviewMusicVolume > 100)
                    CConfig.PreviewMusicVolume = 100;
                CConfig.SaveConfig();
                ApplyVolume();
            }
            else if (keyEvent.ModShift && (keyEvent.Key == Keys.Subtract || keyEvent.Key == Keys.PageDown))
            {
                CConfig.PreviewMusicVolume = CConfig.PreviewMusicVolume - 5;
                if (CConfig.PreviewMusicVolume < 0)
                    CConfig.PreviewMusicVolume = 0;
                CConfig.SaveConfig();
                ApplyVolume();
            }

            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (_DragAndDropActive)
            {
                _DragAndDropCover.Rect.X += mouseEvent.X - _OldMousePosX;
                _DragAndDropCover.Rect.Y += mouseEvent.Y - _OldMousePosY;
            }
            _OldMousePosX = mouseEvent.X;
            _OldMousePosY = mouseEvent.Y;

            if (_Playlists[_Playlist].Visible && _Playlists[_Playlist].IsMouseOver(mouseEvent))
            {
                _PlaylistActive = true;
                _Playlists[_Playlist].Selected = _PlaylistActive;
                _SongMenus[_SongMenu].SetActive(!_PlaylistActive);
                _ToggleSongOptions(ESongOptionsView.None);
            }
            else if (CHelper.IsInBounds(_SongMenus[_SongMenu].Rect, mouseEvent.X, mouseEvent.Y))
            {
                _PlaylistActive = false;
                _Playlists[_Playlist].Selected = _PlaylistActive;
                _SongMenus[_SongMenu].SetActive(!_PlaylistActive);
            }


            if (_Playlists[_Playlist].Visible && _PlaylistActive)
            {
                if (_Playlists[_Playlist].HandleMouse(mouseEvent))
                    return true;
            }


            if (mouseEvent.RB)
            {
                if (_SongOptionsActive)
                {
                    _ToggleSongOptions(ESongOptionsView.None);
                    return true;
                }

                if (_SearchActive)
                {
                    _SearchActive = false;
                    _SearchText = String.Empty;
                    _ApplyNewSearchFilter(_SearchText);
                    return true;
                }

                if (CSongs.Category < 0 && !_Sso.Selection.PartyMode && !_SearchActive)
                {
                    CGraphics.FadeTo(EScreens.ScreenMain);
                    return true;
                }
            }

            if (mouseEvent.MB && !_Sso.Selection.PartyMode)
                return _SelectNextRandom(-1);

            if (mouseEvent.LD && !_Sso.Selection.PartyMode)
            {
                if (CSongs.NumSongsVisible > 0 && _SongMenus[_SongMenu].GetActualSelection() != -1 && _SongMenus[_SongMenu].IsMouseOverActualSelection(mouseEvent))
                {
                    _ToggleSongOptions(ESongOptionsView.None);
                    _StartVisibleSong(_SongMenus[_SongMenu].GetActualSelection());
                    return true;
                }
            }

            _SongMenus[_SongMenu].HandleMouse(ref mouseEvent, _Sso);
            _UpdatePartyModeOptions();

            if (mouseEvent.Handled)
                return true;

            if (mouseEvent.LB)
            {
                if (_IsMouseOver(mouseEvent))
                {
                    if (_Buttons[_ButtonOpenOptions].Selected)
                    {
                        _ToggleSongOptions(ESongOptionsView.General);
                        return true;
                    }
                    if (_Buttons[_ButtonOptionsClose].Selected)
                    {
                        _ToggleSongOptions(ESongOptionsView.None);
                        return true;
                    }
                    if (_Buttons[_ButtonOptionsSing].Selected)
                    {
                        _ToggleSongOptions(ESongOptionsView.None);
                        _StartSong(_SongMenus[_SongMenu].GetSelectedSong());
                        return true;
                    }
                    if (_Buttons[_ButtonOptionsPlaylist].Selected)
                    {
                        _ToggleSongOptions(ESongOptionsView.None);
                        _OpenAndAddPlaylistAction();
                        return true;
                    }
                    if (_Buttons[_ButtonOptionsRandom].Selected)
                    {
                        if (CSongs.IsInCategory)
                        {
                            _SongMenus[_SongMenu].SetSelectedSong(CSongs.GetRandomSong());
                            return true;
                        }
                    }
                    else if (_Buttons[_ButtonOptionsRandomCategory].Selected)
                    {
                        if (!CSongs.IsInCategory)
                        {
                            _SongMenus[_SongMenu].SetSelectedCategory(CSongs.GetRandomCategory());
                            return true;
                        }
                    }
                    else if (_Buttons[_ButtonOptionsSingAll].Selected)
                    {
                        _ToggleSongOptions(ESongOptionsView.None);
                        _StartRandomAllSongs();
                        return true;
                    }
                    else if (_Buttons[_ButtonOptionsSingAllVisible].Selected)
                    {
                        _ToggleSongOptions(ESongOptionsView.None);
                        _StartRandomVisibleSongs();
                        return true;
                    }
                    else if (_Buttons[_ButtonOptionsOpenSelectedItem].Selected)
                    {
                        _HandleSelectButton();
                        return true;
                    }
                    else if (_SelectSlides[_SelectSlideOptionsPlaylistOpen].ValueSelected)
                    {
                        _OpenPlaylist(_SelectSlides[_SelectSlideOptionsPlaylistOpen].Selection);
                        return true;
                    }
                    else if (_SelectSlides[_SelectSlideOptionsPlaylistAdd].ValueSelected)
                    {
                        _OpenPlaylist(_SelectSlides[_SelectSlideOptionsPlaylistAdd].Selection - 1);
                        return true;
                    }
                    else if (_Buttons[_ButtonOptionsRandomMedley].Selected)
                    {
                        _ToggleSongOptions(ESongOptionsView.Medley);
                        return true;
                    }
                    else if (_Buttons[_ButtonOptionsStartMedley].Selected)
                    {
                        _ToggleSongOptions(ESongOptionsView.None);
                        _StartRandomMedley(_SelectSlides[_SelectSlideOptionsNumMedleySongs].Selection + 1, !CSongs.IsInCategory);
                        return true;
                    }
                    else if (_Sso.Selection.RandomOnly && _Sso.Selection.NumJokers != null)
                    {
                        if (_Buttons[_ButtonStart].Selected)
                        {
                            _HandlePartySongSelection(_SongMenus[_SongMenu].GetSelectedSong());
                            return true;
                        }

                        for (int i = 0; i < _ButtonsJoker.Count; i++)
                        {
                            if (i < _Sso.Selection.NumJokers.Length)
                            {
                                if (_Buttons[_ButtonsJoker[i]].Selected)
                                {
                                    _SelectNextRandom(i);
                                    return true;
                                }
                            }
                        }
                    }
                }

                if (CSongs.NumSongsVisible > 0 && _SongMenus[_SongMenu].GetActualSelection() != -1 && !_Sso.Selection.PartyMode)
                {
                    if (_SongMenus[_SongMenu].GetSelectedSong() != -1 && !_SongOptionsActive)
                    {
                        _ToggleSongOptions(ESongOptionsView.Song);
                        return true;
                    }
                    _ToggleSongOptions(ESongOptionsView.None);
                    return true;
                }
            }

            if (mouseEvent.LBH)
            {
                if (!_DragAndDropActive && _Playlists[_Playlist].Visible && CSongs.NumSongsVisible > 0 && _SongMenus[_SongMenu].GetActualSelection() != -1)
                {
                    _DragAndDropCover = _SongMenus[_SongMenu].GetSelectedSongCover();
                    _DragAndDropCover.Rect.Z = CSettings.ZNear;
                    _Playlists[_Playlist].DragAndDropSongID = CSongs.VisibleSongs[_SongMenus[_SongMenu].GetActualSelection()].ID;
                    _DragAndDropActive = true;
                    return true;
                }
            }


            if (!mouseEvent.LBH && _DragAndDropActive)
            {
                _DragAndDropActive = false;
                _Playlists[_Playlist].DragAndDropSongID = -1;
                return true;
            }

            return true;
        }

        private void _HandleSelectButton()
        {
            if (CSongs.IsInCategory)
            {
                if (!_Sso.Selection.PartyMode)
                    _ToggleSongOptions(ESongOptionsView.Song);
            }
            else if (_Sso.Selection.CategoryChangeAllowed)
            {
                _ToggleSongOptions(ESongOptionsView.None);
                _SongMenus[_SongMenu].EnterCurrentCategory();
            }
        }

        public override void OnShow()
        {
            base.OnShow();

            _SelectedSongID = -1;
            _SelectedCategoryIndex = -2;

            _Sso = CParty.GetSongSelectionOptions();
            CSongs.Sort(_Sso.Sorting.SongSorting, _Sso.Sorting.Tabs, _Sso.Sorting.IgnoreArticles, _Sso.Sorting.SearchString, _Sso.Sorting.DuetOptions);
            _SearchActive = _Sso.Sorting.SearchActive;
            _SearchText = _Sso.Sorting.SearchString;

            CGame.Reset();
            _SongMenus[_SongMenu].OnShow();

            if (_Sso.Selection.PartyMode)
                _PlaylistActive = false;

            if (_Sso.Selection.PartyMode)
                _ToggleSongOptions(ESongOptionsView.None);

            _SongMenus[_SongMenu].SetActive(!_PlaylistActive);
            _SongMenus[_SongMenu].SetSmallView(_Playlists[_Playlist].Visible);

            if (_Playlists[_Playlist].ActivePlaylistID != -1)
                _Playlists[_Playlist].LoadPlaylist(_Playlists[_Playlist].ActivePlaylistID);

            _DragAndDropActive = false;
            _Playlists[_Playlist].DragAndDropSongID = -1;

            UpdateGame();
        }

        public override bool UpdateGame()
        {
            if (_SongMenus[_SongMenu].IsSmallView())
                CheckPlaylist();

            _Texts[_TextCategory].Text = CSongs.GetCurrentCategoryName();

            if (CSongs.Category > -1 || CConfig.Tabs == EOffOn.TR_CONFIG_OFF)
                CBackgroundMusic.Disabled = true;
            else
                CBackgroundMusic.Disabled = false;

            int song = _SongMenus[_SongMenu].GetActualSelection();
            if ((CSongs.IsInCategory || CConfig.Tabs == EOffOn.TR_CONFIG_OFF) && song >= 0 && song < CSongs.VisibleSongs.Count)
                _Texts[_TextSelection].Text = CSongs.VisibleSongs[song].Artist + " - " + CSongs.VisibleSongs[song].Title;
            else if (!CSongs.IsInCategory && song >= 0 && song < CSongs.Categories.Count)
                _Texts[_TextSelection].Text = CSongs.Categories[song].Name;
            else
                _Texts[_TextSelection].Text = String.Empty;

            _Texts[_TextSearchBar].Text = _SearchText;
            if (_SearchActive)
            {
                _Texts[_TextSearchBar].Text += '|';

                _Texts[_TextSearchBar].Visible = true;
                _Texts[_TextSearchBarTitle].Visible = true;
                _Statics[_StaticSearchBar].Visible = true;
            }
            else
            {
                _Texts[_TextSearchBar].Visible = false;
                _Texts[_TextSearchBarTitle].Visible = false;
                _Statics[_StaticSearchBar].Visible = false;
            }

            _UpdatePartyModeOptions();

            return true;
        }

        public override bool Draw()
        {
            base.Draw();

            if (_DragAndDropActive)
                _DragAndDropCover.Draw();

            return true;
        }

        public override void OnClose()
        {
            base.OnClose();
            CBackgroundMusic.Disabled = false;
            _SongMenus[_SongMenu].OnHide();
        }

        public override void ApplyVolume()
        {
            _SongMenus[_SongMenu].ApplyVolume(CConfig.PreviewMusicVolume);
        }

        private void _HandlePartySongSelection(int songNr)
        {
            if ((CSongs.Category >= 0) && (songNr >= 0))
            {
                CSong song = CSongs.VisibleSongs[songNr];
                if (song != null)
                    CParty.SongSelected(song.ID);
            }
        }

        private void _UpdatePartyModeOptions()
        {
            if (!CSongs.IsInCategory)
                _SelectedSongID = -1;

            if (_SelectedCategoryIndex != CSongs.Category)
            {
                _SelectedCategoryIndex = CSongs.Category;
                CParty.OnCategoryChange(_SelectedCategoryIndex, ref _Sso);

                if (_Sso.Selection.SelectNextRandomSong)
                    _SelectNextRandomSong();

                if (_Sso.Selection.SongIndex != -1)
                    _SongMenus[_SongMenu].SetSelectedSong(_Sso.Selection.SongIndex);
            }

            if (_SelectedSongID != _SongMenus[_SongMenu].GetSelectedSong() && CSongs.Category > -1)
            {
                _SelectedSongID = _SongMenus[_SongMenu].GetSelectedSong();
                CParty.OnSongChange(_SelectedSongID, ref _Sso);
            }

            _Sso = CParty.GetSongSelectionOptions();


            if (_Sso.Selection.PartyMode)
            {
                CSongs.Sort(_Sso.Sorting.SongSorting, _Sso.Sorting.Tabs, _Sso.Sorting.IgnoreArticles, _Sso.Sorting.SearchString, _Sso.Sorting.DuetOptions);
                _SearchActive = _Sso.Sorting.SearchActive;
                _SearchText = _Sso.Sorting.SearchString;

                _ClosePlaylist();
                _ToggleSongOptions(ESongOptionsView.None);
            }

            _SongMenus[_SongMenu].Update(_Sso);

            if (_Sso.Selection.RandomOnly && _Sso.Selection.NumJokers != null)
            {
                _Buttons[_ButtonStart].Visible = true;

                if (!_SongMenus[_SongMenu].IsSmallView())
                    _SongMenus[_SongMenu].SetSmallView(true);

                for (int i = 0; i < CSettings.MaxNumPlayer; i++)
                {
                    if (i < _Sso.Selection.NumJokers.Length)
                    {
                        _Buttons[_ButtonsJoker[i]].Visible = true;
                        _Buttons[_ButtonsJoker[i]].Text.Text = _Sso.Selection.NumJokers[i].ToString();
                        _Texts[_TextsPlayer[i]].Visible = true;

                        bool nameExists = false;
                        if (_Sso.Selection.TeamNames != null)
                        {
                            if (_Sso.Selection.TeamNames.Length > i)
                            {
                                _Texts[_TextsPlayer[i]].Text = _Sso.Selection.TeamNames[i];
                                nameExists = true;
                            }
                        }

                        if (!nameExists)
                            _Texts[_TextsPlayer[i]].Text = i.ToString();
                    }
                    else
                    {
                        _Buttons[_ButtonsJoker[i]].Visible = false;
                        _Texts[_TextsPlayer[i]].Visible = false;
                    }
                }
            }
            else
            {
                if (_Sso.Selection.PartyMode && _SongMenus[_SongMenu].IsSmallView())
                    _SongMenus[_SongMenu].SetSmallView(false);

                for (int i = 0; i < CSettings.MaxNumPlayer; i++)
                {
                    _Buttons[_ButtonsJoker[i]].Visible = false;
                    _Texts[_TextsPlayer[i]].Visible = false;
                }

                _Buttons[_ButtonStart].Visible = false;
            }
        }

        private void _StartSong(int songNr)
        {
            if ((CSongs.Category >= 0) && (songNr >= 0))
            {
                EGameMode gm;
                if (_AvailableGameModes.Count >= _SelectSlides[_SelectSlideOptionsMode].Selection)
                    gm = _AvailableGameModes[_SelectSlides[_SelectSlideOptionsMode].Selection];
                else
                    gm = CSongs.VisibleSongs[songNr].IsDuet ? EGameMode.TR_GAMEMODE_DUET : EGameMode.TR_GAMEMODE_NORMAL;

                CGame.Reset();
                CGame.ClearSongs();

                CGame.AddVisibleSong(songNr, gm);

                CGraphics.FadeTo(EScreens.ScreenNames);
            }
        }

        private void _StartVisibleSong(int songNr)
        {
            if (CSongs.Category >= 0 && songNr >= 0 && CSongs.NumSongsVisible > songNr)
            {
                EGameMode gm = CSongs.VisibleSongs[songNr].IsDuet ? EGameMode.TR_GAMEMODE_DUET : EGameMode.TR_GAMEMODE_NORMAL;

                CGame.Reset();
                CGame.ClearSongs();

                CGame.AddVisibleSong(songNr, gm);

                CGraphics.FadeTo(EScreens.ScreenNames);
            }
        }

        private void _StartMedleySong(int songNr)
        {
            if ((CSongs.Category >= 0) && (songNr >= 0))
            {
                if (CSongs.VisibleSongs[songNr].Medley.Source == EMedleySource.None)
                    return;

                CGame.Reset();
                CGame.ClearSongs();
                CGame.AddVisibleSong(songNr, EGameMode.TR_GAMEMODE_MEDLEY);

                CGraphics.FadeTo(EScreens.ScreenNames);
            }
        }

        private void _StartRandomAllSongs()
        {
            CGame.Reset();
            CGame.ClearSongs();

            var ids = new List<int>();
            for (int i = 0; i < CSongs.AllSongs.Count; i++)
                ids.Add(i);

            while (ids.Count > 0)
            {
                int songNr = ids[CGame.Rand.Next(ids.Count)];

                var gm = EGameMode.TR_GAMEMODE_NORMAL;
                if (CSongs.AllSongs[songNr].IsDuet)
                    gm = EGameMode.TR_GAMEMODE_DUET;

                CGame.AddSong(songNr, gm);

                ids.Remove(songNr);
            }

            if (CGame.GetNumSongs() > 0)
                CGraphics.FadeTo(EScreens.ScreenNames);
        }

        private void _StartRandomVisibleSongs()
        {
            CGame.Reset();
            CGame.ClearSongs();

            List<int> ids = CSongs.VisibleSongs.Select(t => t.ID).ToList();

            while (ids.Count > 0)
            {
                int songNr = ids[CGame.Rand.Next(ids.Count)];

                var gm = EGameMode.TR_GAMEMODE_NORMAL;
                if (CSongs.AllSongs[songNr].IsDuet)
                    gm = EGameMode.TR_GAMEMODE_DUET;

                CGame.AddSong(songNr, gm);

                ids.Remove(songNr);
            }

            if (CGame.GetNumSongs() > 0)
                CGraphics.FadeTo(EScreens.ScreenNames);
        }

        private void _StartRandomMedley(int numSongs, bool allSongs)
        {
            CGame.Reset();
            CGame.ClearSongs();

            List<int> ids;
            if (allSongs)
            {
                ids = new List<int>();
                for (int i = 0; i < CSongs.AllSongs.Count; i++)
                    ids.Add(i);
            }
            else
                ids = CSongs.VisibleSongs.Select(t => t.ID).ToList();
            int s = 0;
            while (s < numSongs && ids.Count > 0)
            {
                int songNr = ids[CGame.Rand.Next(ids.Count)];

                foreach (EGameMode gm in CSongs.AllSongs[songNr].AvailableGameModes)
                {
                    if (gm == EGameMode.TR_GAMEMODE_MEDLEY)
                    {
                        CGame.AddSong(songNr, gm);
                        s++;
                        break;
                    }
                }

                ids.Remove(songNr);
            }

            if (CGame.GetNumSongs() > 0)
                CGraphics.FadeTo(EScreens.ScreenNames);
        }

        private bool _SelectNextRandom(int teamNr)
        {
            if (teamNr != -1 && _Sso.Selection.RandomOnly && _Sso.Selection.NumJokers != null)
            {
                bool result = false;
                if (_Sso.Selection.NumJokers.Length > teamNr)
                {
                    if (_Sso.Selection.NumJokers[teamNr] > 0)
                    {
                        result = _SelectNextRandomSong() || _SelectNextRandomCategory();
                        CParty.JokerUsed(teamNr);
                        _Sso = CParty.GetSongSelectionOptions();
                    }
                }
                return result;
            }

            if (teamNr == -1)
                return _SelectNextRandomSong() || _SelectNextRandomCategory();

            return false;
        }

        private bool _SelectNextRandomSong()
        {
            if (CSongs.IsInCategory)
            {
                _ToggleSongOptions(ESongOptionsView.None);
                _SongMenus[_SongMenu].SetSelectedSong(CSongs.GetRandomSong());
                return true;
            }
            return false;
        }

        private bool _SelectNextRandomCategory()
        {
            if (!CSongs.IsInCategory)
            {
                _ToggleSongOptions(ESongOptionsView.None);
                _SongMenus[_SongMenu].SetSelectedCategory(CSongs.GetRandomCategory());
                return true;
            }
            return false;
        }

        private int _FindIndex<T>(IList<T> list, int start, Predicate<T> match)
        {
            for (int i = start; i < list.Count; i++)
            {
                if (match(list[i]))
                    return i;
            }
            for (int i = 0; i < start; i++)
            {
                if (match(list[i]))
                    return i;
            }
            return -1;
        }

        private void _JumpTo(char letter)
        {
            int start = 0;
            int curSelected = _SongMenus[_SongMenu].GetActualSelection();
            bool firstLevel = CConfig.Tabs == EOffOn.TR_CONFIG_OFF && CSongs.IsInCategory;
            bool secondSort = CConfig.Tabs == EOffOn.TR_CONFIG_ON &&
                              (CConfig.SongSorting == ESongSorting.TR_CONFIG_ARTIST ||
                               CConfig.SongSorting == ESongSorting.TR_CONFIG_ARTIST_LETTER ||
                               CConfig.SongSorting == ESongSorting.TR_CONFIG_FOLDER ||
                               CConfig.SongSorting == ESongSorting.TR_CONFIG_TITLE_LETTER);
            if (firstLevel && !secondSort)
            {
                //TODO: What's to do with multiple tags?
                //Flamefire: What? We only sorted by one tag, sorting by multiple tags (e.g. Album) will be by e.g. the first entry. That can be used here too as otherwhise it will confuse users because it jumps randomly
                ReadOnlyCollection<CSong> songs = CSongs.VisibleSongs;
                int ct = songs.Count;
                int visibleID = -1;
                switch (CConfig.SongSorting)
                {
                    case ESongSorting.TR_CONFIG_ARTIST:
                    case ESongSorting.TR_CONFIG_ARTIST_LETTER:
                        if (curSelected >= 0 && curSelected < ct - 1 && songs[curSelected].Artist.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase))
                            start = curSelected + 1;
                        visibleID = _FindIndex(songs, start, element => element.Artist.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase));
                        break;

                    case ESongSorting.TR_CONFIG_YEAR:
                    case ESongSorting.TR_CONFIG_DECADE:
                        if (curSelected >= 0 && curSelected < ct - 1 && songs[curSelected].Year.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase))
                            start = curSelected + 1;
                        visibleID = _FindIndex(songs, start, element => element.Year.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase));
                        break;

                    case ESongSorting.TR_CONFIG_TITLE_LETTER:
                        if (curSelected >= 0 && curSelected < ct - 1 && songs[curSelected].Title.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase))
                            start = curSelected + 1;
                        visibleID = _FindIndex(songs, start, element => element.Title.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase));
                        break;

                    case ESongSorting.TR_CONFIG_FOLDER:
                        if (curSelected >= 0 && curSelected < ct - 1 && songs[curSelected].Folder.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase))
                            start = curSelected + 1;
                        visibleID = _FindIndex(songs, start, element => element.Folder.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase));
                        break;
                }
                if (visibleID > -1)
                    _SongMenus[_SongMenu].SetSelectedSong(visibleID);
            }
            else if (secondSort && CSongs.IsInCategory)
            {
                ReadOnlyCollection<CSong> songs = CSongs.VisibleSongs;
                int ct = songs.Count;
                int visibleID = -1;
                switch (CConfig.SongSorting)
                {
                    case ESongSorting.TR_CONFIG_FOLDER:
                    case ESongSorting.TR_CONFIG_TITLE_LETTER:
                        if (curSelected >= 0 && curSelected < ct - 1 && songs[curSelected].Artist.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase))
                            start = curSelected + 1;
                        visibleID = _FindIndex(songs, start, element => element.Artist.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase));
                        break;

                    case ESongSorting.TR_CONFIG_ARTIST:
                    case ESongSorting.TR_CONFIG_ARTIST_LETTER:
                        if (curSelected >= 0 && curSelected < ct - 1 && songs[curSelected].Title.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase))
                            start = curSelected + 1;
                        visibleID = _FindIndex(songs, start, element => element.Title.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase));
                        break;
                }
                if (visibleID > -1)
                    _SongMenus[_SongMenu].SetSelectedSong(visibleID);
            }
            else if (!CSongs.IsInCategory)
            {
                ReadOnlyCollection<CCategory> categories = CSongs.Categories;
                int ct = categories.Count;
                if (curSelected >= 0 && curSelected < ct - 1 && categories[curSelected].Name.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase))
                    start = curSelected + 1;
                int visibleID = _FindIndex(categories, start, element => element.Name.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase));
                if (visibleID > -1)
                    _SongMenus[_SongMenu].SetSelectedCategory(visibleID);
            }
        }

        private void _ApplyNewSearchFilter(string newFilterString)
        {
            CParty.SetSearchString(newFilterString, _SearchActive);
            _Sso = CParty.GetSongSelectionOptions();

            bool refresh = false;
            _SearchText = newFilterString;

            int songIndex = _SongMenus[_SongMenu].GetSelectedSong();
            int songID = -1;
            if (songIndex != -1 && CSongs.NumSongsVisible > 0 && CSongs.NumSongsVisible > songIndex)
                songID = CSongs.VisibleSongs[songIndex].ID;

            if (newFilterString == "" && _Sso.Sorting.Tabs == EOffOn.TR_CONFIG_ON)
            {
                CSongs.Category = -1;
                refresh = true;
            }

            if (newFilterString != "" && CSongs.Category != 0)
            {
                CSongs.Category = 0;
                refresh = true;
            }

            CSongs.Sort(_Sso.Sorting.SongSorting, _Sso.Sorting.Tabs, _Sso.Sorting.IgnoreArticles, newFilterString, _Sso.Sorting.DuetOptions);

            if (songID == -1 || CSongs.NumSongsVisible == 0 || CSongs.NumSongsVisible <= songIndex || CSongs.GetVisibleSongByIndex(songIndex).ID != songID)
                refresh = true;

            if (refresh)
                _SongMenus[_SongMenu].OnHide();

            _SongMenus[_SongMenu].OnShow();
        }

        private void _ToggleSongOptions(ESongOptionsView view)
        {
            _SelectSlides[_SelectSlideOptionsMode].Visible = false;
            _SelectSlides[_SelectSlideOptionsPlaylistAdd].Visible = false;
            _SelectSlides[_SelectSlideOptionsPlaylistOpen].Visible = false;
            _SelectSlides[_SelectSlideOptionsNumMedleySongs].Visible = false;
            _Buttons[_ButtonOptionsClose].Visible = false;
            _Buttons[_ButtonOptionsSing].Visible = false;
            _Buttons[_ButtonOptionsPlaylist].Visible = false;
            _Buttons[_ButtonOptionsRandom].Visible = false;
            _Buttons[_ButtonOptionsRandomCategory].Visible = false;
            _Buttons[_ButtonOptionsSingAll].Visible = false;
            _Buttons[_ButtonOptionsSingAllVisible].Visible = false;
            _Buttons[_ButtonOptionsOpenSelectedItem].Visible = false;
            _Buttons[_ButtonOptionsRandomMedley].Visible = false;
            _Buttons[_ButtonOptionsStartMedley].Visible = false;
            _Texts[_TextOptionsTitle].Visible = false;
            _Statics[_StaticOptionsBG].Visible = false;
            _Buttons[_ButtonOpenOptions].Visible = true;

            if (view == ESongOptionsView.None)
                _SongOptionsActive = false;
            else if (CSongs.IsInCategory)
                _SongOptionsActive = CSongs.VisibleSongs.Count > 0;
            else
                _SongOptionsActive = CSongs.Categories.Count > 0;

            if (!_SongOptionsActive)
                return;

            //Has to be done here otherwhise changed playlist names will not appear until OnShow is called!
            _UpdatePlaylistNames();

            _Texts[_TextOptionsTitle].Visible = true;
            _Buttons[_ButtonOptionsClose].Visible = true;
            _Statics[_StaticOptionsBG].Visible = true;
            _Buttons[_ButtonOpenOptions].Visible = false;
            if (view == ESongOptionsView.Song)
                _ShowSongOptionsSong();
            else if (view == ESongOptionsView.General)
                _ShowSongOptionsGeneral();
            else if (view == ESongOptionsView.Medley)
                _ShowSongOptionsMedley();
        }

        private void _ShowSongOptionsSong()
        {
            var lastMode = EGameMode.TR_GAMEMODE_NORMAL;
            if (_AvailableGameModes.Count > 0)
                lastMode = _AvailableGameModes[_SelectSlides[_SelectSlideOptionsMode].Selection];
            _AvailableGameModes.Clear();
            _SelectSlides[_SelectSlideOptionsMode].Clear();
            if (CSongs.VisibleSongs[_SongMenus[_SongMenu].GetSelectedSong()].IsDuet)
            {
                _SelectSlides[_SelectSlideOptionsMode].AddValue(Enum.GetName(typeof(EGameMode), EGameMode.TR_GAMEMODE_DUET));
                _AvailableGameModes.Add(EGameMode.TR_GAMEMODE_DUET);
            }
            else
            {
                _SelectSlides[_SelectSlideOptionsMode].AddValue(Enum.GetName(typeof(EGameMode), EGameMode.TR_GAMEMODE_NORMAL));
                _AvailableGameModes.Add(EGameMode.TR_GAMEMODE_NORMAL);
                _SelectSlides[_SelectSlideOptionsMode].AddValue(Enum.GetName(typeof(EGameMode), EGameMode.TR_GAMEMODE_SHORTSONG));
                _AvailableGameModes.Add(EGameMode.TR_GAMEMODE_SHORTSONG);
            }
            if (CSongs.VisibleSongs[_SongMenus[_SongMenu].GetSelectedSong()].Medley.Source != EMedleySource.None)
            {
                _SelectSlides[_SelectSlideOptionsMode].AddValue(Enum.GetName(typeof(EGameMode), EGameMode.TR_GAMEMODE_MEDLEY));
                _AvailableGameModes.Add(EGameMode.TR_GAMEMODE_MEDLEY);
            }
            //Set SelectSlide-Selection to last selected game-mode if possible
            for (int i = 0; i < _AvailableGameModes.Count; i++)
            {
                if (_AvailableGameModes[i] == lastMode)
                    _SelectSlides[_SelectSlideOptionsMode].SetSelectionByValueIndex(i);
            }
            _SelectSlides[_SelectSlideOptionsMode].Visible = true;
            _SelectSlides[_SelectSlideOptionsPlaylistAdd].Visible = true;
            _Buttons[_ButtonOptionsSing].Visible = true;
            _Buttons[_ButtonOptionsPlaylist].Visible = true;
            _SetInteractionToButton(_Buttons[_ButtonOptionsSing]);
            _SetSelectSlidePlaylistToCurrentPlaylist();
        }

        private void _ShowSongOptionsGeneral()
        {
            if (CSongs.IsInCategory)
            {
                _Buttons[_ButtonOptionsRandom].Visible = true;
                _Buttons[_ButtonOptionsSingAllVisible].Visible = true;
            }
            else
                _Buttons[_ButtonOptionsRandomCategory].Visible = true;
            _Buttons[_ButtonOptionsSingAll].Visible = true;
            _Buttons[_ButtonOptionsRandomMedley].Visible = true;
            _Buttons[_ButtonOptionsOpenSelectedItem].Visible = true;

            if (_SelectSlides[_SelectSlideOptionsPlaylistOpen].NumValues > 0)
                _SelectSlides[_SelectSlideOptionsPlaylistOpen].Visible = true;

            // ReSharper disable ConvertIfStatementToConditionalTernaryExpression
            if (_Buttons[_ButtonOptionsRandom].Visible)
                // ReSharper restore ConvertIfStatementToConditionalTernaryExpression
                _SetInteractionToButton(_Buttons[_ButtonOptionsRandom]);
            else
                _SetInteractionToButton(_Buttons[_ButtonOptionsRandomCategory]);
        }

        private void _ShowSongOptionsMedley()
        {
            _Buttons[_ButtonOptionsStartMedley].Visible = true;
            _SelectSlides[_SelectSlideOptionsNumMedleySongs].Visible = true;
            _SelectSlides[_SelectSlideOptionsNumMedleySongs].Clear();
            if (CSongs.IsInCategory)
            {
                for (int i = 1; i <= CSongs.VisibleSongs.Count; i++)
                    _SelectSlides[_SelectSlideOptionsNumMedleySongs].AddValue(i.ToString());
            }
            else
            {
                for (int i = 1; i <= CSongs.AllSongs.Count; i++)
                    _SelectSlides[_SelectSlideOptionsNumMedleySongs].AddValue(i.ToString());
            }
            if (_SelectSlides[_SelectSlideOptionsNumMedleySongs].NumValues >= 5)
                _SelectSlides[_SelectSlideOptionsNumMedleySongs].SetSelectionByValueIndex(4);
            else
                _SelectSlides[_SelectSlideOptionsNumMedleySongs].SetSelectionByValueIndex(_SelectSlides[_SelectSlideOptionsNumMedleySongs].NumValues - 1);
            _SetInteractionToButton(_Buttons[_ButtonOptionsStartMedley]);
        }

        #region Playlist Actions
        public void CheckPlaylist()
        {
            if (_Playlists[_Playlist].ActivePlaylistID == -1 && _PlaylistActive)
                _ClosePlaylist();
        }

        private void _OpenPlaylist(int playlistID)
        {
            if (CPlaylists.Playlists.Length > playlistID && playlistID >= 0)
            {
                _Playlists[_Playlist].LoadPlaylist(playlistID);
                _SongMenus[_SongMenu].SetSmallView(true);
                _Playlists[_Playlist].Visible = true;
            }
        }

        private void _ClosePlaylist()
        {
            if (_Playlists[_Playlist].Visible || _PlaylistActive)
            {
                _SongMenus[_SongMenu].SetSmallView(false);
                _PlaylistActive = false;
                _Playlists[_Playlist].Selected = _PlaylistActive;
                _SongMenus[_SongMenu].SetActive(!_PlaylistActive);
                _Playlists[_Playlist].ClosePlaylist();
            }
        }

        private void _UpdatePlaylistNames()
        {
            _SelectSlides[_SelectSlideOptionsPlaylistAdd].Clear();
            _SelectSlides[_SelectSlideOptionsPlaylistAdd].AddValue("TR_SCREENSONG_NEWPLAYLIST");
            _SelectSlides[_SelectSlideOptionsPlaylistAdd].AddValues(CPlaylists.PlaylistNames);
            _SelectSlides[_SelectSlideOptionsPlaylistOpen].Clear();
            _SelectSlides[_SelectSlideOptionsPlaylistOpen].AddValues(CPlaylists.PlaylistNames);
        }

        private void _OpenAndAddPlaylistAction()
        {
            //Open an existing playlist and add song
            if (_Playlists[_Playlist].ActivePlaylistID != (_SelectSlides[_SelectSlideOptionsPlaylistAdd].Selection - 1) &&
                (_SelectSlides[_SelectSlideOptionsPlaylistAdd].Selection - 1) != -1)
            {
                //Check selected game-mode
                EGameMode gm;
                if (_AvailableGameModes.Count >= _SelectSlides[_SelectSlideOptionsMode].Selection)
                    gm = _AvailableGameModes[_SelectSlides[_SelectSlideOptionsMode].Selection];
                else if (CSongs.VisibleSongs[_SongMenus[_SongMenu].GetSelectedSong()].IsDuet)
                    gm = EGameMode.TR_GAMEMODE_DUET;
                else
                    gm = EGameMode.TR_GAMEMODE_NORMAL;

                //Check if Playlist really exists
                if (_SelectSlides[_SelectSlideOptionsPlaylistAdd].Selection - 1 >= 0)
                {
                    _Playlists[_Playlist].ActivePlaylistID = _SelectSlides[_SelectSlideOptionsPlaylistAdd].Selection - 1;

                    //Add song to playlist
                    CPlaylists.Playlists[_Playlists[_Playlist].ActivePlaylistID].AddSong(CSongs.VisibleSongs[_SongMenus[_SongMenu].GetSelectedSong()].ID, gm);

                    //Open playlist
                    _OpenPlaylist(_Playlists[_Playlist].ActivePlaylistID);

                    _Playlists[_Playlist].ScrollToBottom();
                }
            }
                //Create a new playlist and add song
            else if ((_SelectSlides[_SelectSlideOptionsPlaylistAdd].Selection - 1) == -1)
            {
                //Check selected game-mode
                EGameMode gm;
                if (_AvailableGameModes.Count >= _SelectSlides[_SelectSlideOptionsMode].Selection)
                    gm = _AvailableGameModes[_SelectSlides[_SelectSlideOptionsMode].Selection];
                else if (CSongs.VisibleSongs[_SongMenus[_SongMenu].GetSelectedSong()].IsDuet)
                    gm = EGameMode.TR_GAMEMODE_DUET;
                else
                    gm = EGameMode.TR_GAMEMODE_NORMAL;

                //Create new playlist
                _Playlists[_Playlist].ActivePlaylistID = CPlaylists.NewPlaylist();

                //Add song to playlist
                CPlaylists.Playlists[_Playlists[_Playlist].ActivePlaylistID].AddSong(CSongs.VisibleSongs[_SongMenus[_SongMenu].GetSelectedSong()].ID, gm);

                //Open playlist
                _OpenPlaylist(_Playlists[_Playlist].ActivePlaylistID);

                //Add new playlist to select-slide
                _SelectSlides[_SelectSlideOptionsPlaylistAdd].AddValue(CPlaylists.Playlists[_Playlists[_Playlist].ActivePlaylistID].PlaylistName);
                _SelectSlides[_SelectSlideOptionsPlaylistOpen].AddValue(CPlaylists.Playlists[_Playlists[_Playlist].ActivePlaylistID].PlaylistName);
            }
                //Add song to loaded playlist
            else
            {
                //Check selected game-mode
                EGameMode gm;
                if (_AvailableGameModes.Count >= _SelectSlides[_SelectSlideOptionsMode].Selection)
                    gm = _AvailableGameModes[_SelectSlides[_SelectSlideOptionsMode].Selection];
                else if (CSongs.VisibleSongs[_SongMenus[_SongMenu].GetSelectedSong()].IsDuet)
                    gm = EGameMode.TR_GAMEMODE_DUET;
                else
                    gm = EGameMode.TR_GAMEMODE_NORMAL;
                CPlaylists.Playlists[_Playlists[_Playlist].ActivePlaylistID].AddSong(CSongs.VisibleSongs[_SongMenus[_SongMenu].GetSelectedSong()].ID, gm);
                _Playlists[_Playlist].UpdatePlaylist();
                _Playlists[_Playlist].ScrollToBottom();
            }
        }

        private void _SetSelectSlidePlaylistToCurrentPlaylist()
        {
            if (_Playlists[_Playlist].ActivePlaylistID > -1)
                _SelectSlides[_SelectSlideOptionsPlaylistAdd].Selection = _Playlists[_Playlist].ActivePlaylistID + 1;
            else
                _SelectSlides[_SelectSlideOptionsPlaylistAdd].Selection = 0;
        }
        #endregion Playlist Actions
    }
}