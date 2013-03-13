using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.GameModes;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;
using Vocaluxe.Menu.SongMenu;
using Vocaluxe.PartyModes;

namespace Vocaluxe.Screens
{
    class CScreenSong : CMenu
    {
        enum ESongOptionsView
        {
            None,
            Song,
            General,
            Medley
        }

        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 5;

        private const string TextCategory = "TextCategory";
        private const string TextSelection = "TextSelection";
        private const string TextSearchBarTitle = "TextSearchBarTitle";
        private const string TextSearchBar = "TextSearchBar";
        private const string TextOptionsTitle = "TextOptionsTitle";

        private const string ButtonOpenOptions = "ButtonOpenOptions";
        private const string ButtonOptionsSing = "ButtonOptionsSing";
        private const string ButtonOptionsPlaylist = "ButtonOptionsPlaylist";
        private const string ButtonOptionsClose = "ButtonOptionsClose";
        private const string ButtonOptionsRandom = "ButtonOptionsRandom";
        private const string ButtonOptionsRandomCategory = "ButtonOptionsRandomCategory";
        private const string ButtonOptionsSingAll = "ButtonOptionsSingAll";
        private const string ButtonOptionsSingAllVisible = "ButtonOptionsSingAllVisible";
        private const string ButtonOptionsOpenSelectedItem = "ButtonOptionsOpenSelectedItem";
        private const string ButtonOptionsRandomMedley = "ButtonOptionsRandomMedley";
        private const string ButtonOptionsStartMedley = "ButtonOptionsStartMedley";
        private const string ButtonStart = "ButtonStart";

        private const string SelectSlideOptionsMode = "SelectSlideOptionsMode";
        private const string SelectSlideOptionsPlaylistAdd = "SelectSlideOptionsPlaylistAdd";
        private const string SelectSlideOptionsPlaylistOpen = "SelectSlideOptionsPlaylistOpen";
        private const string SelectSlideOptionsNumMedleySongs = "SelectSlideOptionsNumMedleySongs";

        private const string StaticSearchBar = "StaticSearchBar";
        private const string StaticOptionsBG = "StaticOptionsBG";
        private const string SongMenu = "SongMenu";
        private const string Playlist = "Playlist";

        private string _SearchText = String.Empty;
        private bool _SearchActive = false;

        private List<string> ButtonsJoker = new List<string>();
        private List<string> TextsPlayer = new List<string>();
        private bool _SongOptionsActive = false;
        private bool _PlaylistActive = false;
        private List<EGameMode> _AvailableGameModes;
        private ScreenSongOptions _sso;

        private CStatic DragAndDropCover;
        private bool DragAndDropActive;
        private int OldMousePosX;
        private int OldMousePosY;

        private int SelectedSongID;
        private int SelectedCategoryIndex;

        public CScreenSong()
        {
            _AvailableGameModes = new List<EGameMode>();
        }

        protected override void Init()
        {
            base.Init();

            ButtonsJoker.Clear();
            for (int i = 0; i < CMain.Settings.GetMaxNumPlayer(); i++)
            {
                ButtonsJoker.Add("ButtonJoker" + (i + 1));
            }
            List<string> blist = new List<string>();
            blist.AddRange(ButtonsJoker);
            blist.Add(ButtonOptionsClose);
            blist.Add(ButtonOptionsPlaylist);
            blist.Add(ButtonOptionsSing);
            blist.Add(ButtonOptionsRandom);
            blist.Add(ButtonOptionsRandomCategory);
            blist.Add(ButtonOptionsSingAll);
            blist.Add(ButtonOptionsSingAllVisible);
            blist.Add(ButtonOptionsOpenSelectedItem);
            blist.Add(ButtonOpenOptions);
            blist.Add(ButtonStart);
            blist.Add(ButtonOptionsRandomMedley);
            blist.Add(ButtonOptionsStartMedley);

            TextsPlayer.Clear();
            for (int i = 0; i < CMain.Settings.GetMaxNumPlayer(); i++)
            {
                TextsPlayer.Add("TextPlayer" + (i + 1));
            }
            List<string> tlist = new List<string>();
            tlist.AddRange(TextsPlayer);
            tlist.Add(TextCategory);
            tlist.Add(TextSelection);
            tlist.Add(TextSearchBarTitle);
            tlist.Add(TextSearchBar);
            tlist.Add(TextOptionsTitle);

            _ThemeName = "ScreenSong";
            _ScreenVersion = ScreenVersion;
            _ThemeStatics = new string[] { StaticSearchBar, StaticOptionsBG };
            _ThemeTexts = tlist.ToArray();
            _ThemeButtons = blist.ToArray();
            _ThemeSelectSlides = new string[] { SelectSlideOptionsMode, SelectSlideOptionsPlaylistAdd, SelectSlideOptionsPlaylistOpen, SelectSlideOptionsNumMedleySongs };
            _ThemeSongMenus = new string[] { SongMenu };
            _ThemePlaylists = new string[] { Playlist };
        }

        public override void LoadTheme(string XmlPath)
        {
            base.LoadTheme(XmlPath);
            ToggleSongOptions(ESongOptionsView.None);
            Playlists[Playlist].Visible = false;

            DragAndDropCover = GetNewStatic();

            Playlists[Playlist].Init();

            _AvailableGameModes.Clear();

            ApplyVolume();
        }

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            base.HandleInput(KeyEvent);
            if (KeyEvent.Handled)
                return true;

            if (_PlaylistActive)
            {
                Playlists[Playlist].HandleInput(KeyEvent);
                return true;
            }

            if (!_SongOptionsActive)
            {
                if (KeyEvent.KeyPressed && !Char.IsControl(KeyEvent.Unicode) && KeyEvent.Mod != EModifier.Ctrl)
                {
                    if (_SearchActive)
                        ApplyNewSearchFilter(_SearchText + KeyEvent.Unicode);
                    else
                    {
                        JumpTo(KeyEvent.Unicode);
                        return true;
                    }
                }
                else
                {
                    SongMenus[SongMenu].HandleInput(ref KeyEvent, _sso);
                    UpdatePartyModeOptions();

                    if (KeyEvent.Handled)
                        return true;

                    switch (KeyEvent.Key)
                    {
                        case Keys.Escape:
                            if ((CSongs.Category < 0 || _sso.Sorting.Tabs == EOffOn.TR_CONFIG_OFF) && !_sso.Selection.PartyMode && !_SearchActive)
                                CGraphics.FadeTo(EScreens.ScreenMain);
                            if (_SearchActive)
                            {
                                _SearchActive = false;
                                _SearchText = String.Empty;
                                ApplyNewSearchFilter(_SearchText);
                            }
                            break;

                        case Keys.Enter:
                            if (_sso.Selection.RandomOnly && _sso.Selection.NumJokers != null)
                            {
                                for (int i = 0; i < ButtonsJoker.Count; i++)
                                {
                                    if (i < _sso.Selection.NumJokers.Length)
                                    {
                                        if (Buttons[ButtonsJoker[i]].Selected)
                                        {
                                            SelectNextRandom(i);
                                            return true;
                                        }
                                    }
                                }
                                if (Buttons[ButtonStart].Selected)
                                {
                                    HandlePartySongSelection(SongMenus[SongMenu].GetSelectedSong());
                                }
                            }
                            if (CSongs.NumVisibleSongs > 0 && !_sso.Selection.PartyMode)
                            {
                                if (SongMenus[SongMenu].GetSelectedSong() != -1 && !_SongOptionsActive)
                                {
                                    if (!_sso.Selection.PartyMode)
                                        ToggleSongOptions(ESongOptionsView.Song);
                                }
                            }
                            break;

                        case Keys.Tab:
                            if (Playlists[Playlist].Visible)
                            {
                                _PlaylistActive = !_PlaylistActive;
                                Playlists[Playlist].Selected = _PlaylistActive;
                                SongMenus[SongMenu].SetActive(!_PlaylistActive);
                            }
                            break;

                        case Keys.Back:
                            if (_SearchActive && _SearchText.Length > 0)
                            {
                                ApplyNewSearchFilter(_SearchText.Remove(_SearchText.Length - 1));
                            }

                            if ((CSongs.Category < 0 || _sso.Sorting.Tabs == EOffOn.TR_CONFIG_OFF) && !_sso.Selection.PartyMode && !_SearchActive)
                            {
                                CGraphics.FadeTo(EScreens.ScreenMain);
                            }

                            break;

                        case Keys.F3:
                            if (_SearchActive)
                            {
                                _SearchActive = false;
                                _SearchText = String.Empty;
                                ApplyNewSearchFilter(_SearchText);
                            }
                            else if (!_sso.Selection.PartyMode)
                                _SearchActive = true;
                            break;
                    }
                    if(!_SearchActive){
                        switch (KeyEvent.Key)
                        {
                            case Keys.Space:
                                if (!_sso.Selection.PartyMode)
                                    ToggleSongOptions(ESongOptionsView.General);
                                break;

                            case Keys.A:
                                if (KeyEvent.Mod == EModifier.Ctrl && !_sso.Selection.PartyMode)
                                    StartRandomAllSongs();
                                break;
                            case Keys.V:
                                if (KeyEvent.Mod == EModifier.Ctrl && !_sso.Selection.PartyMode)
                                    StartRandomVisibleSongs();
                                break;

                            case Keys.R:
                                if (KeyEvent.Mod == EModifier.Ctrl && !_sso.Selection.RandomOnly)
                                    SelectNextRandom(-1);
                                break;

                            case Keys.S:
                                if (KeyEvent.Mod == EModifier.Ctrl && CSongs.NumVisibleSongs > 0 && !_sso.Selection.PartyMode)
                                    StartMedleySong(SongMenus[SongMenu].GetSelectedSong());
                                break;

                            case Keys.D1:
                                SelectNextRandom(0);
                                break;

                            case Keys.D2:
                                SelectNextRandom(1);
                                break;

                            case Keys.D3:
                                SelectNextRandom(2);
                                break;

                            case Keys.D4:
                                SelectNextRandom(3);
                                break;

                            case Keys.D5:
                                SelectNextRandom(4);
                                break;

                            case Keys.D6:
                                SelectNextRandom(5);
                                break;
                        }
                    }
                }
            }
            else
            {
                switch (KeyEvent.Key)
                {
                    case Keys.Enter:
                        if (Buttons[ButtonOptionsClose].Selected)
                        {
                            ToggleSongOptions(ESongOptionsView.None);
                        }
                        else if (Buttons[ButtonOptionsSing].Selected)
                        {
                            ToggleSongOptions(ESongOptionsView.None);
                            StartSong(SongMenus[SongMenu].GetSelectedSong());
                        }
                        else if (Buttons[ButtonOptionsPlaylist].Selected)
                        {
                            ToggleSongOptions(ESongOptionsView.None);
                            OpenAndAddPlaylistAction();
                        }
                        else if (Buttons[ButtonOptionsRandom].Selected)
                        {
                            if (CSongs.IsInCategory)
                                SongMenus[SongMenu].SetSelectedSong(CSongs.GetRandomSong());
                            else
                                SongMenus[SongMenu].SetSelectedCategory(CSongs.GetRandomCategory());
                        }                        
                        else if (Buttons[ButtonOptionsSingAll].Selected)
                        {
                            StartRandomAllSongs();
                        }
                        else if (Buttons[ButtonOptionsSingAllVisible].Selected)
                        {
                            StartRandomVisibleSongs();
                        }
                        else if (Buttons[ButtonOptionsOpenSelectedItem].Selected)
                        {
                            _HandleSelectButton();
                        }
                        else if (SelectSlides[SelectSlideOptionsPlaylistOpen].Selected)
                        {
                            OpenPlaylistAction();
                        }
                        else if (Buttons[ButtonOptionsRandomMedley].Selected)
                        {
                            ToggleSongOptions(ESongOptionsView.Medley);
                        }
                        else if (Buttons[ButtonOptionsStartMedley].Selected)
                        {
                            ToggleSongOptions(ESongOptionsView.None);
                            StartRandomMedley(SelectSlides[SelectSlideOptionsNumMedleySongs].Selection + 1, !CSongs.IsInCategory);
                        }
                        break;

                    case Keys.Escape:
                    case Keys.Back:
                    case Keys.Space:
                        ToggleSongOptions(ESongOptionsView.None);
                        break;
                }
            }

            if (KeyEvent.ModSHIFT && (KeyEvent.Key == Keys.Add || KeyEvent.Key == Keys.PageUp))
            {
                CConfig.PreviewMusicVolume = CConfig.PreviewMusicVolume + 5;
                if (CConfig.PreviewMusicVolume > 100)
                    CConfig.PreviewMusicVolume = 100;
                CConfig.SaveConfig();
                ApplyVolume();
            }
            else if (KeyEvent.ModSHIFT && (KeyEvent.Key == Keys.Subtract || KeyEvent.Key == Keys.PageDown))
            {
                CConfig.PreviewMusicVolume = CConfig.PreviewMusicVolume - 5;
                if (CConfig.PreviewMusicVolume < 0)
                    CConfig.PreviewMusicVolume = 0;
                CConfig.SaveConfig();
                ApplyVolume();
            }

            return true;
        }

        public override bool HandleMouse(MouseEvent MouseEvent)
        {
            base.HandleMouse(MouseEvent);

            if (DragAndDropActive)
            {
                DragAndDropCover.Rect.X += MouseEvent.X - OldMousePosX;
                DragAndDropCover.Rect.Y += MouseEvent.Y - OldMousePosY; 
            }
            OldMousePosX = MouseEvent.X;
            OldMousePosY = MouseEvent.Y;

            if (Playlists[Playlist].Visible && Playlists[Playlist].IsMouseOver(MouseEvent))
            {
                _PlaylistActive = true;
                Playlists[Playlist].Selected = _PlaylistActive;
                SongMenus[SongMenu].SetActive(!_PlaylistActive);
                ToggleSongOptions(ESongOptionsView.None);
            }
            else if (CHelper.IsInBounds(SongMenus[SongMenu].Rect, MouseEvent.X, MouseEvent.Y))
            {
                _PlaylistActive = false;
                Playlists[Playlist].Selected = _PlaylistActive;
                SongMenus[SongMenu].SetActive(!_PlaylistActive);
            }


            if (Playlists[Playlist].Visible && _PlaylistActive)
            {
                if (Playlists[Playlist].HandleMouse(MouseEvent))
                {
                    return true;
                }
            }


            if (MouseEvent.RB)
            {
                if (_SongOptionsActive)
                {
                    ToggleSongOptions(ESongOptionsView.None);
                    return true;
                }

                if (_SearchActive)
                {
                    _SearchActive = false;
                    _SearchText = String.Empty;
                    ApplyNewSearchFilter(_SearchText);
                    return true;
                }

                if (CSongs.Category < 0 && !_sso.Selection.PartyMode && !_SearchActive)
                {
                    CGraphics.FadeTo(EScreens.ScreenMain);
                    return true;
                }
            }

            if (MouseEvent.MB && !_sso.Selection.PartyMode)
            {
                return SelectNextRandom(-1);
            }

            if (MouseEvent.LD && !_sso.Selection.PartyMode)
            {
                //TODO: Causes Bug if you select a song (e.g. with Select random song) and double click a normal button.
                //E.g. clicking to fast on Select random song starts the next random song. is this OK?
                if (CSongs.NumVisibleSongs > 0 && SongMenus[SongMenu].GetActualSelection() != -1)
                {
                    ToggleSongOptions(ESongOptionsView.None);
                    StartVisibleSong(SongMenus[SongMenu].GetActualSelection());
                    return true;
                }
            }

            SongMenus[SongMenu].HandleMouse(ref MouseEvent, _sso);
            UpdatePartyModeOptions();

            if (MouseEvent.Handled)
                return true;

            if (MouseEvent.LB)
            {
                if (IsMouseOver(MouseEvent))
                {
                    if (Buttons[ButtonOpenOptions].Selected)
                    {
                        ToggleSongOptions(ESongOptionsView.General);
                        return true;
                    } else if (Buttons[ButtonOptionsClose].Selected)
                    {
                        ToggleSongOptions(ESongOptionsView.None);
                        return true;
                    } else if (Buttons[ButtonOptionsSing].Selected)
                    {
                        ToggleSongOptions(ESongOptionsView.None);
                        StartSong(SongMenus[SongMenu].GetSelectedSong());
                        return true;
                    }
                    else if (Buttons[ButtonOptionsPlaylist].Selected)
                    {
                        ToggleSongOptions(ESongOptionsView.None);
                        OpenAndAddPlaylistAction();
                        return true;
                    }
                    else if (Buttons[ButtonOptionsRandom].Selected)
                    {
                        if (CSongs.IsInCategory)
                        {
                            SongMenus[SongMenu].SetSelectedSong(CSongs.GetRandomSong());
                            return true;
                        }
                    }
                    else if (Buttons[ButtonOptionsRandomCategory].Selected)
                    {
                        if (!CSongs.IsInCategory)
                        {
                            SongMenus[SongMenu].SetSelectedCategory(CSongs.GetRandomCategory());
                            return true;
                        }
                    }
                    else if (Buttons[ButtonOptionsSingAll].Selected)
                    {
                        ToggleSongOptions(ESongOptionsView.None);
                        StartRandomAllSongs();
                        return true;
                    }
                    else if (Buttons[ButtonOptionsSingAllVisible].Selected)
                    {
                        ToggleSongOptions(ESongOptionsView.None);
                        StartRandomVisibleSongs();
                        return true;
                    }
                    else if (Buttons[ButtonOptionsOpenSelectedItem].Selected)
                    {
                        _HandleSelectButton();
                        return true;
                    }
                    else if (SelectSlides[SelectSlideOptionsPlaylistOpen].ValueSelected)
                    {
                        OpenPlaylistAction();
                        return true;
                    }
                    else if (Buttons[ButtonOptionsRandomMedley].Selected)
                    {
                        ToggleSongOptions(ESongOptionsView.Medley);
                        return true;
                    }
                    else if (Buttons[ButtonOptionsStartMedley].Selected)
                    {
                        ToggleSongOptions(ESongOptionsView.None);
                        StartRandomMedley(SelectSlides[SelectSlideOptionsNumMedleySongs].Selection + 1, !CSongs.IsInCategory);
                        return true;
                    }
                    else if (_sso.Selection.RandomOnly && _sso.Selection.NumJokers != null)
                    {
                        if (Buttons[ButtonStart].Selected)
                        {
                            HandlePartySongSelection(SongMenus[SongMenu].GetSelectedSong());
                            return true;
                        }
                            
                        for (int i = 0; i < ButtonsJoker.Count; i++)
                        {
                            if (i < _sso.Selection.NumJokers.Length)
                            {
                                if (Buttons[ButtonsJoker[i]].Selected)
                                {
                                    SelectNextRandom(i);
                                    return true;
                                }
                            }
                        }
                    }
                }

                if (CSongs.NumVisibleSongs > 0 && SongMenus[SongMenu].GetActualSelection() != -1 && !_sso.Selection.PartyMode)
                {
                    if (SongMenus[SongMenu].GetSelectedSong() != -1 && !_SongOptionsActive)
                    {
                        ToggleSongOptions(ESongOptionsView.Song);
                        return true;
                    }
                    else
                    {
                        ToggleSongOptions(ESongOptionsView.None);
                        return true;
                    }
                }
            }

            if (MouseEvent.LBH)
            {
                if (!DragAndDropActive && Playlists[Playlist].Visible && CSongs.NumVisibleSongs > 0 && SongMenus[SongMenu].GetActualSelection() != -1)
                {
                    DragAndDropCover = SongMenus[SongMenu].GetSelectedSongCover();
                    DragAndDropCover.Rect.Z = CSettings.zNear;
                    Playlists[Playlist].DragAndDropSongID = CSongs.VisibleSongs[SongMenus[SongMenu].GetActualSelection()].ID;
                    DragAndDropActive = true;
                    return true;
                }
            }


            if (!MouseEvent.LBH && DragAndDropActive)
            {
                DragAndDropActive = false;
                Playlists[Playlist].DragAndDropSongID = -1;
                return true;
            }

            return true;
        }

        private void _HandleSelectButton()
        {
            if (CSongs.IsInCategory) {
                if(!_sso.Selection.PartyMode)
                    ToggleSongOptions(ESongOptionsView.Song);
            }
            else if(_sso.Selection.CategoryChangeAllowed)
            {
                ToggleSongOptions(ESongOptionsView.None);
                SongMenus[SongMenu].EnterCurrentCategory();
            }
        }

        public override void OnShow()
        {
            base.OnShow();

            SelectedSongID = -1;
            SelectedCategoryIndex = -2;

            _sso = CParty.GetSongSelectionOptions();
            CSongs.Sort(_sso.Sorting.SongSorting, _sso.Sorting.Tabs, _sso.Sorting.IgnoreArticles, _sso.Sorting.SearchString, _sso.Sorting.DuetOptions);
            _SearchActive = _sso.Sorting.SearchActive;
            _SearchText = _sso.Sorting.SearchString;

            CGame.EnterNormalGame();
            SongMenus[SongMenu].OnShow();

            if (_sso.Selection.PartyMode)
                _PlaylistActive = false;

            if (_sso.Selection.PartyMode)
                ToggleSongOptions(ESongOptionsView.None);

            SongMenus[SongMenu].SetActive(!_PlaylistActive);
            SongMenus[SongMenu].SetSmallView(Playlists[Playlist].Visible);
                        
            if (Playlists[Playlist].ActivePlaylistID != -1)
                Playlists[Playlist].LoadPlaylist(Playlists[Playlist].ActivePlaylistID);

            DragAndDropActive = false;
            Playlists[Playlist].DragAndDropSongID = -1;

            UpdateGame();
        }

        public override bool UpdateGame()
        {
            if (SongMenus[SongMenu].IsSmallView())
                CheckPlaylist();

            Texts[TextCategory].Text = CSongs.GetCurrentCategoryName();

            if (CSongs.Category > -1 || CConfig.Tabs == EOffOn.TR_CONFIG_OFF)
                CBackgroundMusic.Disabled = true;
            else
                CBackgroundMusic.Disabled = false;

            int song = SongMenus[SongMenu].GetActualSelection();
            if ((CSongs.Category >= 0 || CConfig.Tabs == EOffOn.TR_CONFIG_OFF) && song >= 0 && song < CSongs.VisibleSongs.Length)
            {
                Texts[TextSelection].Text = CSongs.VisibleSongs[song].Artist + " - " + CSongs.VisibleSongs[song].Title;                
            }
            else if (!CSongs.IsInCategory && song >= 0 && song < CSongs.Categories.Length)
            {
                Texts[TextSelection].Text = CSongs.Categories[song].Name;                
            }
            else
                Texts[TextSelection].Text = String.Empty;

            Texts[TextSearchBar].Text = _SearchText;
            if (_SearchActive)
            {
                Texts[TextSearchBar].Text += '|';

                Texts[TextSearchBar].Visible = true;
                Texts[TextSearchBarTitle].Visible = true;
                Statics[StaticSearchBar].Visible = true;
            }
            else
            {
                Texts[TextSearchBar].Visible = false;
                Texts[TextSearchBarTitle].Visible = false;
                Statics[StaticSearchBar].Visible = false;
            }

            UpdatePartyModeOptions();

            return true;
        }

        public override bool Draw()
        {
            base.Draw();

            if (DragAndDropActive)
                DragAndDropCover.Draw();

            return true;
        }

        public override void OnClose()
        {
            base.OnClose();
            CBackgroundMusic.Disabled = false;
            SongMenus[SongMenu].OnHide();
        }

        public override void ApplyVolume()
        {
            SongMenus[SongMenu].ApplyVolume(CConfig.PreviewMusicVolume);
        }

        private void HandlePartySongSelection(int SongNr)
        {
            if ((CSongs.Category >= 0) && (SongNr >= 0))
            {
                CSong song = CSongs.VisibleSongs[SongNr];
                if (song != null)
                {
                    CParty.SongSelected(song.ID);
                }
            }
        }

        private void UpdatePartyModeOptions()
        {
            if (!CSongs.IsInCategory)
                SelectedSongID = -1;

            if (SelectedCategoryIndex != CSongs.Category)
            {
                SelectedCategoryIndex = CSongs.Category;
                CParty.OnCategoryChange(SelectedCategoryIndex, ref _sso);

                if (_sso.Selection.SelectNextRandomSong)
                    SelectNextRandomSong();

                if (_sso.Selection.SongIndex != -1)
                    SongMenus[SongMenu].SetSelectedSong(_sso.Selection.SongIndex);
            }

            if (SelectedSongID != SongMenus[SongMenu].GetSelectedSong() && CSongs.Category > -1)
            {
                SelectedSongID = SongMenus[SongMenu].GetSelectedSong();
                CParty.OnSongChange(SelectedSongID, ref _sso);
            }

            _sso = CParty.GetSongSelectionOptions();
 

            if (_sso.Selection.PartyMode)
            {
                CSongs.Sort(_sso.Sorting.SongSorting, _sso.Sorting.Tabs, _sso.Sorting.IgnoreArticles, _sso.Sorting.SearchString, _sso.Sorting.DuetOptions);
                _SearchActive = _sso.Sorting.SearchActive;
                _SearchText = _sso.Sorting.SearchString;

                ClosePlaylist();
                ToggleSongOptions(ESongOptionsView.None);
            }

            SongMenus[SongMenu].Update(_sso);

            if (_sso.Selection.RandomOnly && _sso.Selection.NumJokers != null)
            {
                Buttons[ButtonStart].Visible = true;

                if (!SongMenus[SongMenu].IsSmallView())
                    SongMenus[SongMenu].SetSmallView(true);

                for (int i = 0; i < CMain.Settings.GetMaxNumPlayer(); i++)
                {
                    if (i < _sso.Selection.NumJokers.Length)
                    {
                        Buttons[ButtonsJoker[i]].Visible = true;
                        Buttons[ButtonsJoker[i]].Text.Text = _sso.Selection.NumJokers[i].ToString();
                        Texts[TextsPlayer[i]].Visible = true;

                        bool NameExists = false;
                        if (_sso.Selection.TeamNames != null)
                        {
                            if (_sso.Selection.TeamNames.Length > i)
                            {
                                Texts[TextsPlayer[i]].Text = _sso.Selection.TeamNames[i];
                                NameExists = true;
                            }
                        }

                        if (!NameExists)
                            Texts[TextsPlayer[i]].Text = i.ToString();
                    }
                    else
                    {
                        Buttons[ButtonsJoker[i]].Visible = false;
                        Texts[TextsPlayer[i]].Visible = false;
                    }
                }
            }
            else
            {
                if (_sso.Selection.PartyMode && SongMenus[SongMenu].IsSmallView())
                    SongMenus[SongMenu].SetSmallView(false);

                for (int i = 0; i < CMain.Settings.GetMaxNumPlayer(); i++)
                {
                    Buttons[ButtonsJoker[i]].Visible = false;
                    Texts[TextsPlayer[i]].Visible = false;
                }

                Buttons[ButtonStart].Visible = false;
            }
        }

        private void StartSong(int SongNr)
        {
            if ((CSongs.Category >= 0) && (SongNr >= 0))
            {
                EGameMode gm;
                if (_AvailableGameModes.Count >= SelectSlides[SelectSlideOptionsMode].Selection)
                {
                    gm = _AvailableGameModes[SelectSlides[SelectSlideOptionsMode].Selection];
                }
                else
                {
                    if (CSongs.VisibleSongs[SongNr].IsDuet)
                       gm = EGameMode.TR_GAMEMODE_DUET;
                    else
                       gm = EGameMode.TR_GAMEMODE_NORMAL;
                }

                CGame.Reset();
                CGame.ClearSongs();

                CGame.AddVisibleSong(SongNr, gm);

                CGraphics.FadeTo(EScreens.ScreenNames);
            }
        }

        private void StartVisibleSong(int SongNr)
        {
            if (CSongs.Category >= 0 && SongNr >= 0 && CSongs.NumVisibleSongs > SongNr)
            {
                EGameMode gm;
                if (CSongs.VisibleSongs[SongNr].IsDuet)
                    gm = EGameMode.TR_GAMEMODE_DUET;
                else
                    gm = EGameMode.TR_GAMEMODE_NORMAL;

                CGame.Reset();
                CGame.ClearSongs();

                CGame.AddVisibleSong(SongNr, gm);

                CGraphics.FadeTo(EScreens.ScreenNames);
            }
        }

        private void StartMedleySong(int SongNr)
        {
            if ((CSongs.Category >= 0) && (SongNr >= 0))
            {
                EGameMode gm;
                if (CSongs.VisibleSongs[SongNr].Medley.Source != EMedleySource.None)
                    gm = EGameMode.TR_GAMEMODE_MEDLEY;
                else
                    return;

                CGame.Reset();
                CGame.ClearSongs();
                CGame.AddVisibleSong(SongNr, gm);

                CGraphics.FadeTo(EScreens.ScreenNames);
            }
        }

        private void StartRandomAllSongs()
        {
            CGame.Reset();
            CGame.ClearSongs();

            List<int> IDs = new List<int>();
            for (int i = 0; i < CSongs.AllSongs.Length; i++)
            {
                IDs.Add(i);
            }

            while (IDs.Count > 0)
            {
                int SongNr = IDs[CGame.Rand.Next(IDs.Count)];

                EGameMode gm = EGameMode.TR_GAMEMODE_NORMAL;
                if (CSongs.AllSongs[SongNr].IsDuet)
                    gm = EGameMode.TR_GAMEMODE_DUET;

                CGame.AddSong(SongNr, gm);

                IDs.Remove(SongNr);    
            }

            if (CGame.GetNumSongs() > 0)
                CGraphics.FadeTo(EScreens.ScreenNames);
        }

        private void StartRandomVisibleSongs()
        {
            CGame.Reset();
            CGame.ClearSongs();

            List<int> IDs = new List<int>();
            for (int i = 0; i < CSongs.VisibleSongs.Length; i++)
            {
                IDs.Add(CSongs.VisibleSongs[i].ID);
            }

            while (IDs.Count > 0)
            {
                int SongNr = IDs[CGame.Rand.Next(IDs.Count)];

                EGameMode gm = EGameMode.TR_GAMEMODE_NORMAL;
                if (CSongs.AllSongs[SongNr].IsDuet)
                    gm = EGameMode.TR_GAMEMODE_DUET;

                CGame.AddSong(SongNr, gm);

                IDs.Remove(SongNr);
            }

            if (CGame.GetNumSongs() > 0)
                CGraphics.FadeTo(EScreens.ScreenNames);
        }

        private void StartRandomMedley(int NumSongs, bool AllSongs)
        {
            CGame.Reset();
            CGame.ClearSongs();

            List<int> IDs = new List<int>();
            if (AllSongs)
            {
                for (int i = 0; i < CSongs.AllSongs.Length; i++)
                {
                    IDs.Add(i);
                }
            }
            else
            {
                for (int i = 0; i < CSongs.VisibleSongs.Length; i++)
                {
                    IDs.Add(CSongs.VisibleSongs[i].ID);
                }
            }
            int s = 0;
            while (s < NumSongs && IDs.Count > 0)
            {
                int SongNr = IDs[CGame.Rand.Next(IDs.Count)];

                foreach (EGameMode gm in CSongs.AllSongs[SongNr].AvailableGameModes)
                {
                    if (gm == EGameMode.TR_GAMEMODE_MEDLEY)
                    {
                        CGame.AddSong(SongNr, gm);
                        s++;
                        break;
                    }
                }

                IDs.Remove(SongNr);
            }

            if (CGame.GetNumSongs() > 0)
                CGraphics.FadeTo(EScreens.ScreenNames);
        }

        private bool SelectNextRandom(int TeamNr)
        {           
            if (TeamNr != -1 && _sso.Selection.RandomOnly && _sso.Selection.NumJokers != null)
            {
                bool result = false;
                if (_sso.Selection.NumJokers.Length > TeamNr)
                {
                    if (_sso.Selection.NumJokers[TeamNr] > 0)
                    {
                        result = SelectNextRandomSong() || SelectNextRandomCategory();
                        CParty.JokerUsed(TeamNr);
                        _sso = CParty.GetSongSelectionOptions();
                    }
                }
                return result;
            }

            if (TeamNr == -1)
                return SelectNextRandomSong() || SelectNextRandomCategory();

            return false;
        }

        private bool SelectNextRandomSong()
        {
            if (CSongs.IsInCategory)
            {
                ToggleSongOptions(ESongOptionsView.None);
                SongMenus[SongMenu].SetSelectedSong(CSongs.GetRandomSong());
                return true;
            }
            return false;
        }

        private bool SelectNextRandomCategory()
        {
            if (!CSongs.IsInCategory)
            {
                ToggleSongOptions(ESongOptionsView.None);
                SongMenus[SongMenu].SetSelectedCategory(CSongs.GetRandomCategory());
                return true;
            }
            return false;
        }

        private void JumpTo(char Letter)
        {
            int start = 0;
            int curSelected = SongMenus[SongMenu].GetActualSelection();
            if (CSongs.IsInCategory)
            {
                //TODO: Check and use sorting method
                CSong[] Songs = CSongs.VisibleSongs;
                int ct = Songs.Length;
                if (curSelected >= 0 && curSelected < ct - 1)
                {
                    CSong CurrentSong = CSongs.GetVisibleSongByIndex(curSelected);
                    if (CurrentSong != null && CurrentSong.Artist.StartsWith(Letter.ToString(), StringComparison.OrdinalIgnoreCase))
                        start = curSelected + 1;
                }
                int visibleID = Array.FindIndex<CSong>(Songs, start, ct - start, element => element.Artist.StartsWith(Letter.ToString(), StringComparison.OrdinalIgnoreCase));
                if (visibleID < 0 && start > 1)
                    visibleID = Array.FindIndex<CSong>(Songs, 0, start - 1, element => element.Artist.StartsWith(Letter.ToString(), StringComparison.OrdinalIgnoreCase));
                if (visibleID > -1)
                    SongMenus[SongMenu].SetSelectedSong(visibleID);
            }
            else
            {
                CCategory[] Categories = CSongs.Categories;
                int ct = Categories.Length;
                if (curSelected >= 0 && curSelected < ct - 1 && Categories[curSelected].Name.StartsWith(Letter.ToString(), StringComparison.OrdinalIgnoreCase))
                    start = curSelected + 1;
                int visibleID = Array.FindIndex<CCategory>(Categories, start, ct - start, element => element.Name.StartsWith(Letter.ToString(), StringComparison.OrdinalIgnoreCase));
                if (visibleID < 0 && start > 1)
                    visibleID = Array.FindIndex<CCategory>(Categories, 0, start - 1, element => element.Name.StartsWith(Letter.ToString(), StringComparison.OrdinalIgnoreCase));
                if (visibleID > -1)
                    SongMenus[SongMenu].SetSelectedCategory(visibleID);

            }
        }

        private void ApplyNewSearchFilter(string NewFilterString)
        {
            CParty.SetSearchString(NewFilterString, _SearchActive);
            _sso = CParty.GetSongSelectionOptions();
            
            bool refresh = false;
            _SearchText = NewFilterString;

            int SongIndex = SongMenus[SongMenu].GetSelectedSong();
            int SongID = -1;
            if (SongIndex != -1 && CSongs.NumVisibleSongs > 0 && CSongs.NumVisibleSongs > SongIndex)
            {
                SongID = CSongs.VisibleSongs[SongIndex].ID;
            }

            if (NewFilterString.Length == 0 && _sso.Sorting.Tabs == EOffOn.TR_CONFIG_ON)
            {
                CSongs.Category = -1;
                refresh = true;
            }

            if (NewFilterString.Length > 0 && CSongs.Category != 0)
            {
                CSongs.Category = 0;
                refresh = true;
            }

            CSongs.Sort(_sso.Sorting.SongSorting, _sso.Sorting.Tabs, _sso.Sorting.IgnoreArticles, NewFilterString, _sso.Sorting.DuetOptions);

            if (SongID == -1 || CSongs.NumVisibleSongs == 0 || CSongs.NumVisibleSongs <= SongIndex || CSongs.GetVisibleSongByIndex(SongIndex).ID != SongID)
                refresh = true;

            if (refresh)
                SongMenus[SongMenu].OnHide();

            SongMenus[SongMenu].OnShow();
        }

        private void ToggleSongOptions(ESongOptionsView view)
        {
            SelectSlides[SelectSlideOptionsMode].Visible = false;
            SelectSlides[SelectSlideOptionsPlaylistAdd].Visible = false;
            SelectSlides[SelectSlideOptionsPlaylistOpen].Visible = false;
            SelectSlides[SelectSlideOptionsNumMedleySongs].Visible = false;
            Buttons[ButtonOptionsClose].Visible = false;
            Buttons[ButtonOptionsSing].Visible = false;
            Buttons[ButtonOptionsPlaylist].Visible = false;
            Buttons[ButtonOptionsRandom].Visible = false;
            Buttons[ButtonOptionsRandomCategory].Visible = false;
            Buttons[ButtonOptionsSingAll].Visible = false;
            Buttons[ButtonOptionsSingAllVisible].Visible = false;
            Buttons[ButtonOptionsOpenSelectedItem].Visible = false;
            Buttons[ButtonOptionsRandomMedley].Visible = false;
            Buttons[ButtonOptionsStartMedley].Visible = false;
            Texts[TextOptionsTitle].Visible = false;
            Statics[StaticOptionsBG].Visible = false;
            Buttons[ButtonOpenOptions].Visible = true;

            _SongOptionsActive = view != ESongOptionsView.None;

            if (_SongOptionsActive)
            {
                //Has to be done here otherwhise changed playlist names will not appear until OnShow is called!
                UpdatePlaylistNames();

                Texts[TextOptionsTitle].Visible = true;
                Buttons[ButtonOptionsClose].Visible = true;
                Statics[StaticOptionsBG].Visible = true;
                Buttons[ButtonOpenOptions].Visible = false;
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
            EGameMode LastMode = EGameMode.TR_GAMEMODE_NORMAL;
            if (_AvailableGameModes.Count > 0)
                LastMode = _AvailableGameModes[SelectSlides[SelectSlideOptionsMode].Selection];
            SetInteractionToButton(Buttons[ButtonOptionsSing]);
            _AvailableGameModes.Clear();
            SelectSlides[SelectSlideOptionsMode].Clear();
            if (CSongs.VisibleSongs[SongMenus[SongMenu].GetSelectedSong()].IsDuet)
            {
                SelectSlides[SelectSlideOptionsMode].AddValue(Enum.GetName(typeof(EGameMode), EGameMode.TR_GAMEMODE_DUET));
                _AvailableGameModes.Add(EGameMode.TR_GAMEMODE_DUET);
            }
            else
            {
                SelectSlides[SelectSlideOptionsMode].AddValue(Enum.GetName(typeof(EGameMode), EGameMode.TR_GAMEMODE_NORMAL));
                _AvailableGameModes.Add(EGameMode.TR_GAMEMODE_NORMAL);
                SelectSlides[SelectSlideOptionsMode].AddValue(Enum.GetName(typeof(EGameMode), EGameMode.TR_GAMEMODE_SHORTSONG));
                _AvailableGameModes.Add(EGameMode.TR_GAMEMODE_SHORTSONG);
            }
            if (CSongs.VisibleSongs[SongMenus[SongMenu].GetSelectedSong()].Medley.Source != EMedleySource.None)
            {
                SelectSlides[SelectSlideOptionsMode].AddValue(Enum.GetName(typeof(EGameMode), EGameMode.TR_GAMEMODE_MEDLEY));
                _AvailableGameModes.Add(EGameMode.TR_GAMEMODE_MEDLEY);
            }
            //Set SelectSlide-Selection to last selected game-mode if possible
            for (int i = 0; i < _AvailableGameModes.Count; i++)
            {
                if (_AvailableGameModes[i] == LastMode)
                    SelectSlides[SelectSlideOptionsMode].SetSelectionByValueIndex(i);
            }
            SelectSlides[SelectSlideOptionsMode].Visible = true;
            SelectSlides[SelectSlideOptionsPlaylistAdd].Visible = true;
            Buttons[ButtonOptionsSing].Visible = true;
            Buttons[ButtonOptionsPlaylist].Visible = true;
            SetInteractionToButton(Buttons[ButtonOptionsSing]);
        }

        private void _ShowSongOptionsGeneral()
        {
            if (CSongs.IsInCategory)
            {
                Buttons[ButtonOptionsRandom].Visible = true;
                Buttons[ButtonOptionsSingAllVisible].Visible = true;
            }
            else
                Buttons[ButtonOptionsRandomCategory].Visible = true;
            Buttons[ButtonOptionsSingAll].Visible = true;
            Buttons[ButtonOptionsRandomMedley].Visible = true;
            Buttons[ButtonOptionsOpenSelectedItem].Visible = true;

            if (SelectSlides[SelectSlideOptionsPlaylistOpen].NumValues > 0)
                SelectSlides[SelectSlideOptionsPlaylistOpen].Visible = true;
            
            if (Buttons[ButtonOptionsRandom].Visible)
                SetInteractionToButton(Buttons[ButtonOptionsRandom]);
            else
                SetInteractionToButton(Buttons[ButtonOptionsRandomCategory]);
        }

        private void _ShowSongOptionsMedley()
        {
            Buttons[ButtonOptionsStartMedley].Visible = true;
            SelectSlides[SelectSlideOptionsNumMedleySongs].Visible = true;
            SelectSlides[SelectSlideOptionsNumMedleySongs].Clear();
            if (CSongs.IsInCategory)
            {
                for (int i = 1; i <= CSongs.VisibleSongs.Length; i++)
                {
                    SelectSlides[SelectSlideOptionsNumMedleySongs].AddValue(i.ToString());
                }
            }
            else
            {
                for (int i = 1; i <= CSongs.AllSongs.Length; i++)
                {
                    SelectSlides[SelectSlideOptionsNumMedleySongs].AddValue(i.ToString());
                }
            }
            if (SelectSlides[SelectSlideOptionsNumMedleySongs].NumValues >= 5)
                SelectSlides[SelectSlideOptionsNumMedleySongs].SetSelectionByValueIndex(4);
            else
                SelectSlides[SelectSlideOptionsNumMedleySongs].SetSelectionByValueIndex(SelectSlides[SelectSlideOptionsNumMedleySongs].NumValues - 1);
            SetInteractionToButton(Buttons[ButtonOptionsStartMedley]);
        }

        #region Playlist Actions
        public void CheckPlaylist()
        {
            if (Playlists[Playlist].ActivePlaylistID == -1 && _PlaylistActive)
                ClosePlaylist();
        }

        private void OpenPlaylist(int PlaylistID)
        {
            if (CPlaylists.Playlists.Length > PlaylistID && PlaylistID > -1)
            {
                Playlists[Playlist].LoadPlaylist(PlaylistID);
                SongMenus[SongMenu].SetSmallView(true);
                Playlists[Playlist].Visible = true;
            }
        }

        private void ClosePlaylist()
        {
            if (Playlists[Playlist].Visible || _PlaylistActive)
            {
                SongMenus[SongMenu].SetSmallView(false);
                _PlaylistActive = false;
                Playlists[Playlist].Selected = _PlaylistActive;
                SongMenus[SongMenu].SetActive(!_PlaylistActive);
                Playlists[Playlist].ClosePlaylist();
            }
        }

        private void UpdatePlaylistNames()
        {
            SelectSlides[SelectSlideOptionsPlaylistAdd].Clear();
            SelectSlides[SelectSlideOptionsPlaylistAdd].AddValue("TR_SCREENSONG_NEWPLAYLIST");
            SelectSlides[SelectSlideOptionsPlaylistAdd].AddValues(CPlaylists.PlaylistNames);
            SelectSlides[SelectSlideOptionsPlaylistOpen].Clear();
            SelectSlides[SelectSlideOptionsPlaylistOpen].AddValues(CPlaylists.PlaylistNames);
        }

        private void OpenPlaylistAction()
        {
            //Open a playlist
            if (Playlists[Playlist].ActivePlaylistID != (SelectSlides[SelectSlideOptionsPlaylistOpen].Selection))
            {
                Playlists[Playlist].ActivePlaylistID = SelectSlides[SelectSlideOptionsPlaylistOpen].Selection;
                SetSelectSlidePlaylistToCurrentPlaylist();

                //Open playlist
                OpenPlaylist(Playlists[Playlist].ActivePlaylistID);
            }
        }

        private void OpenAndAddPlaylistAction()
        {
            //Open an existing playlist and add song
            if (Playlists[Playlist].ActivePlaylistID != (SelectSlides[SelectSlideOptionsPlaylistAdd].Selection - 1) && (SelectSlides[SelectSlideOptionsPlaylistAdd].Selection - 1) != -1)
            {
                //Check selected game-mode
                EGameMode gm;
                if (_AvailableGameModes.Count >= SelectSlides[SelectSlideOptionsMode].Selection)
                    gm = _AvailableGameModes[SelectSlides[SelectSlideOptionsMode].Selection];
                else
                    if (CSongs.VisibleSongs[SongMenus[SongMenu].GetSelectedSong()].IsDuet)
                        gm = EGameMode.TR_GAMEMODE_DUET;
                    else
                        gm = EGameMode.TR_GAMEMODE_NORMAL;
                
                //Check if Playlist really exists
                if (SelectSlides[SelectSlideOptionsPlaylistAdd].Selection - 1 >= 0)
                {
                    Playlists[Playlist].ActivePlaylistID = SelectSlides[SelectSlideOptionsPlaylistAdd].Selection - 1;

                    //Add song to playlist
                    CPlaylists.Playlists[Playlists[Playlist].ActivePlaylistID].AddSong(CSongs.VisibleSongs[SongMenus[SongMenu].GetSelectedSong()].ID, gm);

                    //Open playlist
                    OpenPlaylist(Playlists[Playlist].ActivePlaylistID);

                    SetSelectSlidePlaylistToCurrentPlaylist();
                    Playlists[Playlist].ScrollToBottom();
                }
            }
            //Create a new playlist and add song
            else if ((SelectSlides[SelectSlideOptionsPlaylistAdd].Selection - 1) == -1)
            {
                //Check selected game-mode
                EGameMode gm;
                if (_AvailableGameModes.Count >= SelectSlides[SelectSlideOptionsMode].Selection)
                    gm = _AvailableGameModes[SelectSlides[SelectSlideOptionsMode].Selection];
                else
                    if (CSongs.VisibleSongs[SongMenus[SongMenu].GetSelectedSong()].IsDuet)
                        gm = EGameMode.TR_GAMEMODE_DUET;
                    else
                        gm = EGameMode.TR_GAMEMODE_NORMAL;
                
                //Create new playlist
                Playlists[Playlist].ActivePlaylistID = CPlaylists.NewPlaylist();
                
                //Add song to playlist
                CPlaylists.Playlists[Playlists[Playlist].ActivePlaylistID].AddSong(CSongs.VisibleSongs[SongMenus[SongMenu].GetSelectedSong()].ID, gm);
                
                //Open playlist
                OpenPlaylist(Playlists[Playlist].ActivePlaylistID);
                
                //Add new playlist to select-slide
                SelectSlides[SelectSlideOptionsPlaylistAdd].AddValue(CPlaylists.Playlists[Playlists[Playlist].ActivePlaylistID].PlaylistName);
                SelectSlides[SelectSlideOptionsPlaylistOpen].AddValue(CPlaylists.Playlists[Playlists[Playlist].ActivePlaylistID].PlaylistName);

                SetSelectSlidePlaylistToCurrentPlaylist();

            }
            //Add song to loaded playlist
            else
            {
                //Check selected game-mode
                EGameMode gm;
                if (_AvailableGameModes.Count >= SelectSlides[SelectSlideOptionsMode].Selection)
                    gm = _AvailableGameModes[SelectSlides[SelectSlideOptionsMode].Selection];
                else
                    if (CSongs.VisibleSongs[SongMenus[SongMenu].GetSelectedSong()].IsDuet)
                        gm = EGameMode.TR_GAMEMODE_DUET;
                    else
                        gm = EGameMode.TR_GAMEMODE_NORMAL;
                CPlaylists.Playlists[Playlists[Playlist].ActivePlaylistID].AddSong(CSongs.VisibleSongs[SongMenus[SongMenu].GetSelectedSong()].ID, gm);
                Playlists[Playlist].UpdatePlaylist();
                Playlists[Playlist].ScrollToBottom();
            }
        }

        private void SetSelectSlidePlaylistToCurrentPlaylist()
        {
            if (Playlists[Playlist].ActivePlaylistID > -1)
                SelectSlides[SelectSlideOptionsPlaylistAdd].Selection = Playlists[Playlist].ActivePlaylistID + 1;
            else
                SelectSlides[SelectSlideOptionsPlaylistAdd].Selection = 0;
        }
        #endregion Playlist Actions
    }
}
