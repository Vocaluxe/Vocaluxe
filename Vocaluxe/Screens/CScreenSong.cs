using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib.Menu;
using VocaluxeLib.Menu.SongMenu;
using VocaluxeLib.PartyModes;

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
            for (int i = 0; i < CMain.Settings.GetMaxNumPlayer(); i++)
                _ButtonsJoker.Add("ButtonJoker" + (i + 1));
            List<string> blist = new List<string>();
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
            for (int i = 0; i < CMain.Settings.GetMaxNumPlayer(); i++)
                _TextsPlayer.Add("TextPlayer" + (i + 1));
            List<string> tlist = new List<string>();
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
            Playlists[_Playlist].Visible = false;

            _DragAndDropCover = GetNewStatic();

            Playlists[_Playlist].Init();

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
                Playlists[_Playlist].HandleInput(keyEvent);
                return true;
            }

            if (!_SongOptionsActive)
            {
                if (keyEvent.KeyPressed && !Char.IsControl(keyEvent.Unicode) && keyEvent.Mod != EModifier.Ctrl)
                {
                    if (_SearchActive)
                        _ApplyNewSearchFilter(_SearchText + keyEvent.Unicode);
                    else
                    {
                        _JumpTo(keyEvent.Unicode);
                        return true;
                    }
                }
                else
                {
                    SongMenus[_SongMenu].HandleInput(ref keyEvent, _Sso);
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
                                        if (Buttons[_ButtonsJoker[i]].Selected)
                                        {
                                            _SelectNextRandom(i);
                                            return true;
                                        }
                                    }
                                }
                                if (Buttons[_ButtonStart].Selected)
                                    _HandlePartySongSelection(SongMenus[_SongMenu].GetSelectedSong());
                            }
                            if (CSongs.NumVisibleSongs > 0 && !_Sso.Selection.PartyMode)
                            {
                                if (SongMenus[_SongMenu].GetSelectedSong() != -1 && !_SongOptionsActive)
                                {
                                    if (!_Sso.Selection.PartyMode)
                                        _ToggleSongOptions(ESongOptionsView.Song);
                                }
                            }
                            break;

                        case Keys.Tab:
                            if (Playlists[_Playlist].Visible)
                            {
                                _PlaylistActive = !_PlaylistActive;
                                Playlists[_Playlist].Selected = _PlaylistActive;
                                SongMenus[_SongMenu].SetActive(!_PlaylistActive);
                            }
                            break;

                        case Keys.Back:
                            if (_SearchActive && _SearchText.Length > 0)
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
                                if (keyEvent.Mod == EModifier.Ctrl && CSongs.NumVisibleSongs > 0 && !_Sso.Selection.PartyMode)
                                    _StartMedleySong(SongMenus[_SongMenu].GetSelectedSong());
                                break;

                            case Keys.D1:
                                _SelectNextRandom(0);
                                break;

                            case Keys.D2:
                                _SelectNextRandom(1);
                                break;

                            case Keys.D3:
                                _SelectNextRandom(2);
                                break;

                            case Keys.D4:
                                _SelectNextRandom(3);
                                break;

                            case Keys.D5:
                                _SelectNextRandom(4);
                                break;

                            case Keys.D6:
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
                        if (Buttons[_ButtonOptionsClose].Selected)
                            _ToggleSongOptions(ESongOptionsView.None);
                        else if (Buttons[_ButtonOptionsSing].Selected)
                        {
                            _ToggleSongOptions(ESongOptionsView.None);
                            _StartSong(SongMenus[_SongMenu].GetSelectedSong());
                        }
                        else if (Buttons[_ButtonOptionsPlaylist].Selected)
                        {
                            _ToggleSongOptions(ESongOptionsView.None);
                            _OpenAndAddPlaylistAction();
                        }
                        else if (Buttons[_ButtonOptionsRandom].Selected)
                        {
                            if (CSongs.IsInCategory)
                                SongMenus[_SongMenu].SetSelectedSong(CSongs.GetRandomSong());
                            else
                                SongMenus[_SongMenu].SetSelectedCategory(CSongs.GetRandomCategory());
                        }
                        else if (Buttons[_ButtonOptionsSingAll].Selected)
                            _StartRandomAllSongs();
                        else if (Buttons[_ButtonOptionsSingAllVisible].Selected)
                            _StartRandomVisibleSongs();
                        else if (Buttons[_ButtonOptionsOpenSelectedItem].Selected)
                            _HandleSelectButton();
                        else if (SelectSlides[_SelectSlideOptionsPlaylistOpen].Selected)
                            _OpenPlaylistAction();
                        else if (Buttons[_ButtonOptionsRandomMedley].Selected)
                            _ToggleSongOptions(ESongOptionsView.Medley);
                        else if (Buttons[_ButtonOptionsStartMedley].Selected)
                        {
                            _ToggleSongOptions(ESongOptionsView.None);
                            _StartRandomMedley(SelectSlides[_SelectSlideOptionsNumMedleySongs].Selection + 1, !CSongs.IsInCategory);
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

            if (Playlists[_Playlist].Visible && Playlists[_Playlist].IsMouseOver(mouseEvent))
            {
                _PlaylistActive = true;
                Playlists[_Playlist].Selected = _PlaylistActive;
                SongMenus[_SongMenu].SetActive(!_PlaylistActive);
                _ToggleSongOptions(ESongOptionsView.None);
            }
            else if (CHelper.IsInBounds(SongMenus[_SongMenu].Rect, mouseEvent.X, mouseEvent.Y))
            {
                _PlaylistActive = false;
                Playlists[_Playlist].Selected = _PlaylistActive;
                SongMenus[_SongMenu].SetActive(!_PlaylistActive);
            }


            if (Playlists[_Playlist].Visible && _PlaylistActive)
            {
                if (Playlists[_Playlist].HandleMouse(mouseEvent))
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
                //TODO: Causes Bug if you select a song (e.g. with Select random song) and double click a normal button.
                //E.g. clicking to fast on Select random song starts the next random song. is this OK?
                if (CSongs.NumVisibleSongs > 0 && SongMenus[_SongMenu].GetActualSelection() != -1)
                {
                    _ToggleSongOptions(ESongOptionsView.None);
                    _StartVisibleSong(SongMenus[_SongMenu].GetActualSelection());
                    return true;
                }
            }

            SongMenus[_SongMenu].HandleMouse(ref mouseEvent, _Sso);
            _UpdatePartyModeOptions();

            if (mouseEvent.Handled)
                return true;

            if (mouseEvent.LB)
            {
                if (IsMouseOver(mouseEvent))
                {
                    if (Buttons[_ButtonOpenOptions].Selected)
                    {
                        _ToggleSongOptions(ESongOptionsView.General);
                        return true;
                    }
                    else if (Buttons[_ButtonOptionsClose].Selected)
                    {
                        _ToggleSongOptions(ESongOptionsView.None);
                        return true;
                    }
                    else if (Buttons[_ButtonOptionsSing].Selected)
                    {
                        _ToggleSongOptions(ESongOptionsView.None);
                        _StartSong(SongMenus[_SongMenu].GetSelectedSong());
                        return true;
                    }
                    else if (Buttons[_ButtonOptionsPlaylist].Selected)
                    {
                        _ToggleSongOptions(ESongOptionsView.None);
                        _OpenAndAddPlaylistAction();
                        return true;
                    }
                    else if (Buttons[_ButtonOptionsRandom].Selected)
                    {
                        if (CSongs.IsInCategory)
                        {
                            SongMenus[_SongMenu].SetSelectedSong(CSongs.GetRandomSong());
                            return true;
                        }
                    }
                    else if (Buttons[_ButtonOptionsRandomCategory].Selected)
                    {
                        if (!CSongs.IsInCategory)
                        {
                            SongMenus[_SongMenu].SetSelectedCategory(CSongs.GetRandomCategory());
                            return true;
                        }
                    }
                    else if (Buttons[_ButtonOptionsSingAll].Selected)
                    {
                        _ToggleSongOptions(ESongOptionsView.None);
                        _StartRandomAllSongs();
                        return true;
                    }
                    else if (Buttons[_ButtonOptionsSingAllVisible].Selected)
                    {
                        _ToggleSongOptions(ESongOptionsView.None);
                        _StartRandomVisibleSongs();
                        return true;
                    }
                    else if (Buttons[_ButtonOptionsOpenSelectedItem].Selected)
                    {
                        _HandleSelectButton();
                        return true;
                    }
                    else if (SelectSlides[_SelectSlideOptionsPlaylistOpen].ValueSelected)
                    {
                        _OpenPlaylistAction();
                        return true;
                    }
                    else if (Buttons[_ButtonOptionsRandomMedley].Selected)
                    {
                        _ToggleSongOptions(ESongOptionsView.Medley);
                        return true;
                    }
                    else if (Buttons[_ButtonOptionsStartMedley].Selected)
                    {
                        _ToggleSongOptions(ESongOptionsView.None);
                        _StartRandomMedley(SelectSlides[_SelectSlideOptionsNumMedleySongs].Selection + 1, !CSongs.IsInCategory);
                        return true;
                    }
                    else if (_Sso.Selection.RandomOnly && _Sso.Selection.NumJokers != null)
                    {
                        if (Buttons[_ButtonStart].Selected)
                        {
                            _HandlePartySongSelection(SongMenus[_SongMenu].GetSelectedSong());
                            return true;
                        }

                        for (int i = 0; i < _ButtonsJoker.Count; i++)
                        {
                            if (i < _Sso.Selection.NumJokers.Length)
                            {
                                if (Buttons[_ButtonsJoker[i]].Selected)
                                {
                                    _SelectNextRandom(i);
                                    return true;
                                }
                            }
                        }
                    }
                }

                if (CSongs.NumVisibleSongs > 0 && SongMenus[_SongMenu].GetActualSelection() != -1 && !_Sso.Selection.PartyMode)
                {
                    if (SongMenus[_SongMenu].GetSelectedSong() != -1 && !_SongOptionsActive)
                    {
                        _ToggleSongOptions(ESongOptionsView.Song);
                        return true;
                    }
                    else
                    {
                        _ToggleSongOptions(ESongOptionsView.None);
                        return true;
                    }
                }
            }

            if (mouseEvent.LBH)
            {
                if (!_DragAndDropActive && Playlists[_Playlist].Visible && CSongs.NumVisibleSongs > 0 && SongMenus[_SongMenu].GetActualSelection() != -1)
                {
                    _DragAndDropCover = SongMenus[_SongMenu].GetSelectedSongCover();
                    _DragAndDropCover.Rect.Z = CSettings.ZNear;
                    Playlists[_Playlist].DragAndDropSongID = CSongs.VisibleSongs[SongMenus[_SongMenu].GetActualSelection()].ID;
                    _DragAndDropActive = true;
                    return true;
                }
            }


            if (!mouseEvent.LBH && _DragAndDropActive)
            {
                _DragAndDropActive = false;
                Playlists[_Playlist].DragAndDropSongID = -1;
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
                SongMenus[_SongMenu].EnterCurrentCategory();
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

            CGame.EnterNormalGame();
            SongMenus[_SongMenu].OnShow();

            if (_Sso.Selection.PartyMode)
                _PlaylistActive = false;

            if (_Sso.Selection.PartyMode)
                _ToggleSongOptions(ESongOptionsView.None);

            SongMenus[_SongMenu].SetActive(!_PlaylistActive);
            SongMenus[_SongMenu].SetSmallView(Playlists[_Playlist].Visible);

            if (Playlists[_Playlist].ActivePlaylistID != -1)
                Playlists[_Playlist].LoadPlaylist(Playlists[_Playlist].ActivePlaylistID);

            _DragAndDropActive = false;
            Playlists[_Playlist].DragAndDropSongID = -1;

            UpdateGame();
        }

        public override bool UpdateGame()
        {
            if (SongMenus[_SongMenu].IsSmallView())
                CheckPlaylist();

            Texts[_TextCategory].Text = CSongs.GetCurrentCategoryName();

            if (CSongs.Category > -1 || CConfig.Tabs == EOffOn.TR_CONFIG_OFF)
                CBackgroundMusic.Disabled = true;
            else
                CBackgroundMusic.Disabled = false;

            int song = SongMenus[_SongMenu].GetActualSelection();
            if ((CSongs.Category >= 0 || CConfig.Tabs == EOffOn.TR_CONFIG_OFF) && song >= 0 && song < CSongs.VisibleSongs.Length)
                Texts[_TextSelection].Text = CSongs.VisibleSongs[song].Artist + " - " + CSongs.VisibleSongs[song].Title;
            else if (!CSongs.IsInCategory && song >= 0 && song < CSongs.Categories.Length)
                Texts[_TextSelection].Text = CSongs.Categories[song].Name;
            else
                Texts[_TextSelection].Text = String.Empty;

            Texts[_TextSearchBar].Text = _SearchText;
            if (_SearchActive)
            {
                Texts[_TextSearchBar].Text += '|';

                Texts[_TextSearchBar].Visible = true;
                Texts[_TextSearchBarTitle].Visible = true;
                Statics[_StaticSearchBar].Visible = true;
            }
            else
            {
                Texts[_TextSearchBar].Visible = false;
                Texts[_TextSearchBarTitle].Visible = false;
                Statics[_StaticSearchBar].Visible = false;
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
            SongMenus[_SongMenu].OnHide();
        }

        public override void ApplyVolume()
        {
            SongMenus[_SongMenu].ApplyVolume(CConfig.PreviewMusicVolume);
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
                    SongMenus[_SongMenu].SetSelectedSong(_Sso.Selection.SongIndex);
            }

            if (_SelectedSongID != SongMenus[_SongMenu].GetSelectedSong() && CSongs.Category > -1)
            {
                _SelectedSongID = SongMenus[_SongMenu].GetSelectedSong();
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

            SongMenus[_SongMenu].Update(_Sso);

            if (_Sso.Selection.RandomOnly && _Sso.Selection.NumJokers != null)
            {
                Buttons[_ButtonStart].Visible = true;

                if (!SongMenus[_SongMenu].IsSmallView())
                    SongMenus[_SongMenu].SetSmallView(true);

                for (int i = 0; i < CMain.Settings.GetMaxNumPlayer(); i++)
                {
                    if (i < _Sso.Selection.NumJokers.Length)
                    {
                        Buttons[_ButtonsJoker[i]].Visible = true;
                        Buttons[_ButtonsJoker[i]].Text.Text = _Sso.Selection.NumJokers[i].ToString();
                        Texts[_TextsPlayer[i]].Visible = true;

                        bool nameExists = false;
                        if (_Sso.Selection.TeamNames != null)
                        {
                            if (_Sso.Selection.TeamNames.Length > i)
                            {
                                Texts[_TextsPlayer[i]].Text = _Sso.Selection.TeamNames[i];
                                nameExists = true;
                            }
                        }

                        if (!nameExists)
                            Texts[_TextsPlayer[i]].Text = i.ToString();
                    }
                    else
                    {
                        Buttons[_ButtonsJoker[i]].Visible = false;
                        Texts[_TextsPlayer[i]].Visible = false;
                    }
                }
            }
            else
            {
                if (_Sso.Selection.PartyMode && SongMenus[_SongMenu].IsSmallView())
                    SongMenus[_SongMenu].SetSmallView(false);

                for (int i = 0; i < CMain.Settings.GetMaxNumPlayer(); i++)
                {
                    Buttons[_ButtonsJoker[i]].Visible = false;
                    Texts[_TextsPlayer[i]].Visible = false;
                }

                Buttons[_ButtonStart].Visible = false;
            }
        }

        private void _StartSong(int songNr)
        {
            if ((CSongs.Category >= 0) && (songNr >= 0))
            {
                EGameMode gm;
                if (_AvailableGameModes.Count >= SelectSlides[_SelectSlideOptionsMode].Selection)
                    gm = _AvailableGameModes[SelectSlides[_SelectSlideOptionsMode].Selection];
                else
                {
                    if (CSongs.VisibleSongs[songNr].IsDuet)
                        gm = EGameMode.TR_GAMEMODE_DUET;
                    else
                        gm = EGameMode.TR_GAMEMODE_NORMAL;
                }

                CGame.Reset();
                CGame.ClearSongs();

                CGame.AddVisibleSong(songNr, gm);

                CGraphics.FadeTo(EScreens.ScreenNames);
            }
        }

        private void _StartVisibleSong(int songNr)
        {
            if (CSongs.Category >= 0 && songNr >= 0 && CSongs.NumVisibleSongs > songNr)
            {
                EGameMode gm;
                if (CSongs.VisibleSongs[songNr].IsDuet)
                    gm = EGameMode.TR_GAMEMODE_DUET;
                else
                    gm = EGameMode.TR_GAMEMODE_NORMAL;

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
                EGameMode gm;
                if (CSongs.VisibleSongs[songNr].Medley.Source != EMedleySource.None)
                    gm = EGameMode.TR_GAMEMODE_MEDLEY;
                else
                    return;

                CGame.Reset();
                CGame.ClearSongs();
                CGame.AddVisibleSong(songNr, gm);

                CGraphics.FadeTo(EScreens.ScreenNames);
            }
        }

        private void _StartRandomAllSongs()
        {
            CGame.Reset();
            CGame.ClearSongs();

            List<int> iDs = new List<int>();
            for (int i = 0; i < CSongs.AllSongs.Length; i++)
                iDs.Add(i);

            while (iDs.Count > 0)
            {
                int songNr = iDs[CGame.Rand.Next(iDs.Count)];

                EGameMode gm = EGameMode.TR_GAMEMODE_NORMAL;
                if (CSongs.AllSongs[songNr].IsDuet)
                    gm = EGameMode.TR_GAMEMODE_DUET;

                CGame.AddSong(songNr, gm);

                iDs.Remove(songNr);
            }

            if (CGame.GetNumSongs() > 0)
                CGraphics.FadeTo(EScreens.ScreenNames);
        }

        private void _StartRandomVisibleSongs()
        {
            CGame.Reset();
            CGame.ClearSongs();

            List<int> iDs = new List<int>();
            for (int i = 0; i < CSongs.VisibleSongs.Length; i++)
                iDs.Add(CSongs.VisibleSongs[i].ID);

            while (iDs.Count > 0)
            {
                int songNr = iDs[CGame.Rand.Next(iDs.Count)];

                EGameMode gm = EGameMode.TR_GAMEMODE_NORMAL;
                if (CSongs.AllSongs[songNr].IsDuet)
                    gm = EGameMode.TR_GAMEMODE_DUET;

                CGame.AddSong(songNr, gm);

                iDs.Remove(songNr);
            }

            if (CGame.GetNumSongs() > 0)
                CGraphics.FadeTo(EScreens.ScreenNames);
        }

        private void _StartRandomMedley(int numSongs, bool allSongs)
        {
            CGame.Reset();
            CGame.ClearSongs();

            List<int> iDs = new List<int>();
            if (allSongs)
            {
                for (int i = 0; i < CSongs.AllSongs.Length; i++)
                    iDs.Add(i);
            }
            else
            {
                for (int i = 0; i < CSongs.VisibleSongs.Length; i++)
                    iDs.Add(CSongs.VisibleSongs[i].ID);
            }
            int s = 0;
            while (s < numSongs && iDs.Count > 0)
            {
                int songNr = iDs[CGame.Rand.Next(iDs.Count)];

                foreach (EGameMode gm in CSongs.AllSongs[songNr].AvailableGameModes)
                {
                    if (gm == EGameMode.TR_GAMEMODE_MEDLEY)
                    {
                        CGame.AddSong(songNr, gm);
                        s++;
                        break;
                    }
                }

                iDs.Remove(songNr);
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
                SongMenus[_SongMenu].SetSelectedSong(CSongs.GetRandomSong());
                return true;
            }
            return false;
        }

        private bool _SelectNextRandomCategory()
        {
            if (!CSongs.IsInCategory)
            {
                _ToggleSongOptions(ESongOptionsView.None);
                SongMenus[_SongMenu].SetSelectedCategory(CSongs.GetRandomCategory());
                return true;
            }
            return false;
        }

        private void _JumpTo(char letter)
        {
            int start = 0;
            int curSelected = SongMenus[_SongMenu].GetActualSelection();
            if (CSongs.IsInCategory)
            {
                //TODO: Check and use sorting method
                CSong[] songs = CSongs.VisibleSongs;
                int ct = songs.Length;
                if (curSelected >= 0 && curSelected < ct - 1)
                {
                    CSong currentSong = CSongs.GetVisibleSongByIndex(curSelected);
                    if (currentSong != null && currentSong.Artist.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase))
                        start = curSelected + 1;
                }
                int visibleID = Array.FindIndex(songs, start, ct - start, element => element.Artist.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase));
                if (visibleID < 0 && start > 1)
                    visibleID = Array.FindIndex(songs, 0, start - 1, element => element.Artist.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase));
                if (visibleID > -1)
                    SongMenus[_SongMenu].SetSelectedSong(visibleID);
            }
            else
            {
                CCategory[] categories = CSongs.Categories;
                int ct = categories.Length;
                if (curSelected >= 0 && curSelected < ct - 1 && categories[curSelected].Name.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase))
                    start = curSelected + 1;
                int visibleID = Array.FindIndex(categories, start, ct - start, element => element.Name.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase));
                if (visibleID < 0 && start > 1)
                    visibleID = Array.FindIndex(categories, 0, start - 1, element => element.Name.StartsWith(letter.ToString(), StringComparison.OrdinalIgnoreCase));
                if (visibleID > -1)
                    SongMenus[_SongMenu].SetSelectedCategory(visibleID);
            }
        }

        private void _ApplyNewSearchFilter(string newFilterString)
        {
            CParty.SetSearchString(newFilterString, _SearchActive);
            _Sso = CParty.GetSongSelectionOptions();

            bool refresh = false;
            _SearchText = newFilterString;

            int songIndex = SongMenus[_SongMenu].GetSelectedSong();
            int songID = -1;
            if (songIndex != -1 && CSongs.NumVisibleSongs > 0 && CSongs.NumVisibleSongs > songIndex)
                songID = CSongs.VisibleSongs[songIndex].ID;

            if (newFilterString.Length == 0 && _Sso.Sorting.Tabs == EOffOn.TR_CONFIG_ON)
            {
                CSongs.Category = -1;
                refresh = true;
            }

            if (newFilterString.Length > 0 && CSongs.Category != 0)
            {
                CSongs.Category = 0;
                refresh = true;
            }

            CSongs.Sort(_Sso.Sorting.SongSorting, _Sso.Sorting.Tabs, _Sso.Sorting.IgnoreArticles, newFilterString, _Sso.Sorting.DuetOptions);

            if (songID == -1 || CSongs.NumVisibleSongs == 0 || CSongs.NumVisibleSongs <= songIndex || CSongs.GetVisibleSongByIndex(songIndex).ID != songID)
                refresh = true;

            if (refresh)
                SongMenus[_SongMenu].OnHide();

            SongMenus[_SongMenu].OnShow();
        }

        private void _ToggleSongOptions(ESongOptionsView view)
        {
            SelectSlides[_SelectSlideOptionsMode].Visible = false;
            SelectSlides[_SelectSlideOptionsPlaylistAdd].Visible = false;
            SelectSlides[_SelectSlideOptionsPlaylistOpen].Visible = false;
            SelectSlides[_SelectSlideOptionsNumMedleySongs].Visible = false;
            Buttons[_ButtonOptionsClose].Visible = false;
            Buttons[_ButtonOptionsSing].Visible = false;
            Buttons[_ButtonOptionsPlaylist].Visible = false;
            Buttons[_ButtonOptionsRandom].Visible = false;
            Buttons[_ButtonOptionsRandomCategory].Visible = false;
            Buttons[_ButtonOptionsSingAll].Visible = false;
            Buttons[_ButtonOptionsSingAllVisible].Visible = false;
            Buttons[_ButtonOptionsOpenSelectedItem].Visible = false;
            Buttons[_ButtonOptionsRandomMedley].Visible = false;
            Buttons[_ButtonOptionsStartMedley].Visible = false;
            Texts[_TextOptionsTitle].Visible = false;
            Statics[_StaticOptionsBG].Visible = false;
            Buttons[_ButtonOpenOptions].Visible = true;

            _SongOptionsActive = view != ESongOptionsView.None;

            if (_SongOptionsActive)
            {
                //Has to be done here otherwhise changed playlist names will not appear until OnShow is called!
                _UpdatePlaylistNames();

                Texts[_TextOptionsTitle].Visible = true;
                Buttons[_ButtonOptionsClose].Visible = true;
                Statics[_StaticOptionsBG].Visible = true;
                Buttons[_ButtonOpenOptions].Visible = false;
                if (view == ESongOptionsView.Song)
                    _ShowSongOptionsSong();
                else if (view == ESongOptionsView.General)
                    _ShowSongOptionsGeneral();
                else if (view == ESongOptionsView.Medley)
                    _ShowSongOptionsMedley();
            }
        }

        private void _ShowSongOptionsSong()
        {
            EGameMode lastMode = EGameMode.TR_GAMEMODE_NORMAL;
            if (_AvailableGameModes.Count > 0)
                lastMode = _AvailableGameModes[SelectSlides[_SelectSlideOptionsMode].Selection];
            SetInteractionToButton(Buttons[_ButtonOptionsSing]);
            _AvailableGameModes.Clear();
            SelectSlides[_SelectSlideOptionsMode].Clear();
            if (CSongs.VisibleSongs[SongMenus[_SongMenu].GetSelectedSong()].IsDuet)
            {
                SelectSlides[_SelectSlideOptionsMode].AddValue(Enum.GetName(typeof(EGameMode), EGameMode.TR_GAMEMODE_DUET));
                _AvailableGameModes.Add(EGameMode.TR_GAMEMODE_DUET);
            }
            else
            {
                SelectSlides[_SelectSlideOptionsMode].AddValue(Enum.GetName(typeof(EGameMode), EGameMode.TR_GAMEMODE_NORMAL));
                _AvailableGameModes.Add(EGameMode.TR_GAMEMODE_NORMAL);
                SelectSlides[_SelectSlideOptionsMode].AddValue(Enum.GetName(typeof(EGameMode), EGameMode.TR_GAMEMODE_SHORTSONG));
                _AvailableGameModes.Add(EGameMode.TR_GAMEMODE_SHORTSONG);
            }
            if (CSongs.VisibleSongs[SongMenus[_SongMenu].GetSelectedSong()].Medley.Source != EMedleySource.None)
            {
                SelectSlides[_SelectSlideOptionsMode].AddValue(Enum.GetName(typeof(EGameMode), EGameMode.TR_GAMEMODE_MEDLEY));
                _AvailableGameModes.Add(EGameMode.TR_GAMEMODE_MEDLEY);
            }
            //Set SelectSlide-Selection to last selected game-mode if possible
            for (int i = 0; i < _AvailableGameModes.Count; i++)
            {
                if (_AvailableGameModes[i] == lastMode)
                    SelectSlides[_SelectSlideOptionsMode].SetSelectionByValueIndex(i);
            }
            SelectSlides[_SelectSlideOptionsMode].Visible = true;
            SelectSlides[_SelectSlideOptionsPlaylistAdd].Visible = true;
            Buttons[_ButtonOptionsSing].Visible = true;
            Buttons[_ButtonOptionsPlaylist].Visible = true;
            SetInteractionToButton(Buttons[_ButtonOptionsSing]);
        }

        private void _ShowSongOptionsGeneral()
        {
            if (CSongs.IsInCategory)
            {
                Buttons[_ButtonOptionsRandom].Visible = true;
                Buttons[_ButtonOptionsSingAllVisible].Visible = true;
            }
            else
                Buttons[_ButtonOptionsRandomCategory].Visible = true;
            Buttons[_ButtonOptionsSingAll].Visible = true;
            Buttons[_ButtonOptionsRandomMedley].Visible = true;
            Buttons[_ButtonOptionsOpenSelectedItem].Visible = true;

            if (SelectSlides[_SelectSlideOptionsPlaylistOpen].NumValues > 0)
                SelectSlides[_SelectSlideOptionsPlaylistOpen].Visible = true;

            if (Buttons[_ButtonOptionsRandom].Visible)
                SetInteractionToButton(Buttons[_ButtonOptionsRandom]);
            else
                SetInteractionToButton(Buttons[_ButtonOptionsRandomCategory]);
        }

        private void _ShowSongOptionsMedley()
        {
            Buttons[_ButtonOptionsStartMedley].Visible = true;
            SelectSlides[_SelectSlideOptionsNumMedleySongs].Visible = true;
            SelectSlides[_SelectSlideOptionsNumMedleySongs].Clear();
            if (CSongs.IsInCategory)
            {
                for (int i = 1; i <= CSongs.VisibleSongs.Length; i++)
                    SelectSlides[_SelectSlideOptionsNumMedleySongs].AddValue(i.ToString());
            }
            else
            {
                for (int i = 1; i <= CSongs.AllSongs.Length; i++)
                    SelectSlides[_SelectSlideOptionsNumMedleySongs].AddValue(i.ToString());
            }
            if (SelectSlides[_SelectSlideOptionsNumMedleySongs].NumValues >= 5)
                SelectSlides[_SelectSlideOptionsNumMedleySongs].SetSelectionByValueIndex(4);
            else
                SelectSlides[_SelectSlideOptionsNumMedleySongs].SetSelectionByValueIndex(SelectSlides[_SelectSlideOptionsNumMedleySongs].NumValues - 1);
            SetInteractionToButton(Buttons[_ButtonOptionsStartMedley]);
        }

        #region Playlist Actions
        public void CheckPlaylist()
        {
            if (Playlists[_Playlist].ActivePlaylistID == -1 && _PlaylistActive)
                _ClosePlaylist();
        }

        private void _OpenPlaylist(int playlistID)
        {
            if (CPlaylists.Playlists.Length > playlistID && playlistID > -1)
            {
                Playlists[_Playlist].LoadPlaylist(playlistID);
                SongMenus[_SongMenu].SetSmallView(true);
                Playlists[_Playlist].Visible = true;
            }
        }

        private void _ClosePlaylist()
        {
            if (Playlists[_Playlist].Visible || _PlaylistActive)
            {
                SongMenus[_SongMenu].SetSmallView(false);
                _PlaylistActive = false;
                Playlists[_Playlist].Selected = _PlaylistActive;
                SongMenus[_SongMenu].SetActive(!_PlaylistActive);
                Playlists[_Playlist].ClosePlaylist();
            }
        }

        private void _UpdatePlaylistNames()
        {
            SelectSlides[_SelectSlideOptionsPlaylistAdd].Clear();
            SelectSlides[_SelectSlideOptionsPlaylistAdd].AddValue("TR_SCREENSONG_NEWPLAYLIST");
            SelectSlides[_SelectSlideOptionsPlaylistAdd].AddValues(CPlaylists.PlaylistNames);
            SelectSlides[_SelectSlideOptionsPlaylistOpen].Clear();
            SelectSlides[_SelectSlideOptionsPlaylistOpen].AddValues(CPlaylists.PlaylistNames);
        }

        private void _OpenPlaylistAction()
        {
            //Open a playlist
            if (Playlists[_Playlist].ActivePlaylistID != SelectSlides[_SelectSlideOptionsPlaylistOpen].Selection)
            {
                Playlists[_Playlist].ActivePlaylistID = SelectSlides[_SelectSlideOptionsPlaylistOpen].Selection;
                _SetSelectSlidePlaylistToCurrentPlaylist();

                //Open playlist
                _OpenPlaylist(Playlists[_Playlist].ActivePlaylistID);
            }
        }

        private void _OpenAndAddPlaylistAction()
        {
            //Open an existing playlist and add song
            if (Playlists[_Playlist].ActivePlaylistID != (SelectSlides[_SelectSlideOptionsPlaylistAdd].Selection - 1) &&
                (SelectSlides[_SelectSlideOptionsPlaylistAdd].Selection - 1) != -1)
            {
                //Check selected game-mode
                EGameMode gm;
                if (_AvailableGameModes.Count >= SelectSlides[_SelectSlideOptionsMode].Selection)
                    gm = _AvailableGameModes[SelectSlides[_SelectSlideOptionsMode].Selection];
                else if (CSongs.VisibleSongs[SongMenus[_SongMenu].GetSelectedSong()].IsDuet)
                    gm = EGameMode.TR_GAMEMODE_DUET;
                else
                    gm = EGameMode.TR_GAMEMODE_NORMAL;

                //Check if Playlist really exists
                if (SelectSlides[_SelectSlideOptionsPlaylistAdd].Selection - 1 >= 0)
                {
                    Playlists[_Playlist].ActivePlaylistID = SelectSlides[_SelectSlideOptionsPlaylistAdd].Selection - 1;

                    //Add song to playlist
                    CPlaylists.Playlists[Playlists[_Playlist].ActivePlaylistID].AddSong(CSongs.VisibleSongs[SongMenus[_SongMenu].GetSelectedSong()].ID, gm);

                    //Open playlist
                    _OpenPlaylist(Playlists[_Playlist].ActivePlaylistID);

                    _SetSelectSlidePlaylistToCurrentPlaylist();
                    Playlists[_Playlist].ScrollToBottom();
                }
            }
                //Create a new playlist and add song
            else if ((SelectSlides[_SelectSlideOptionsPlaylistAdd].Selection - 1) == -1)
            {
                //Check selected game-mode
                EGameMode gm;
                if (_AvailableGameModes.Count >= SelectSlides[_SelectSlideOptionsMode].Selection)
                    gm = _AvailableGameModes[SelectSlides[_SelectSlideOptionsMode].Selection];
                else if (CSongs.VisibleSongs[SongMenus[_SongMenu].GetSelectedSong()].IsDuet)
                    gm = EGameMode.TR_GAMEMODE_DUET;
                else
                    gm = EGameMode.TR_GAMEMODE_NORMAL;

                //Create new playlist
                Playlists[_Playlist].ActivePlaylistID = CPlaylists.NewPlaylist();

                //Add song to playlist
                CPlaylists.Playlists[Playlists[_Playlist].ActivePlaylistID].AddSong(CSongs.VisibleSongs[SongMenus[_SongMenu].GetSelectedSong()].ID, gm);

                //Open playlist
                _OpenPlaylist(Playlists[_Playlist].ActivePlaylistID);

                //Add new playlist to select-slide
                SelectSlides[_SelectSlideOptionsPlaylistAdd].AddValue(CPlaylists.Playlists[Playlists[_Playlist].ActivePlaylistID].PlaylistName);
                SelectSlides[_SelectSlideOptionsPlaylistOpen].AddValue(CPlaylists.Playlists[Playlists[_Playlist].ActivePlaylistID].PlaylistName);

                _SetSelectSlidePlaylistToCurrentPlaylist();
            }
                //Add song to loaded playlist
            else
            {
                //Check selected game-mode
                EGameMode gm;
                if (_AvailableGameModes.Count >= SelectSlides[_SelectSlideOptionsMode].Selection)
                    gm = _AvailableGameModes[SelectSlides[_SelectSlideOptionsMode].Selection];
                else if (CSongs.VisibleSongs[SongMenus[_SongMenu].GetSelectedSong()].IsDuet)
                    gm = EGameMode.TR_GAMEMODE_DUET;
                else
                    gm = EGameMode.TR_GAMEMODE_NORMAL;
                CPlaylists.Playlists[Playlists[_Playlist].ActivePlaylistID].AddSong(CSongs.VisibleSongs[SongMenus[_SongMenu].GetSelectedSong()].ID, gm);
                Playlists[_Playlist].UpdatePlaylist();
                Playlists[_Playlist].ScrollToBottom();
            }
        }

        private void _SetSelectSlidePlaylistToCurrentPlaylist()
        {
            if (Playlists[_Playlist].ActivePlaylistID > -1)
                SelectSlides[_SelectSlideOptionsPlaylistAdd].Selection = Playlists[_Playlist].ActivePlaylistID + 1;
            else
                SelectSlides[_SelectSlideOptionsPlaylistAdd].Selection = 0;
        }
        #endregion Playlist Actions
    }
}