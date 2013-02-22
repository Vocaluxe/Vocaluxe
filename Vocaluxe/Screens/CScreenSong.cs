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
        const int ScreenVersion = 4;

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
        private const string ButtonOptionsOpenPlaylist = "ButtonOptionsOpenPlaylist";
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
            blist.Add(ButtonOptionsOpenPlaylist);
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
            SelectSlides[htSelectSlides(SelectSlideOptionsMode)].Visible = false;
            SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistAdd)].Visible = false;
            SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistOpen)].Visible = false;
            SelectSlides[htSelectSlides(SelectSlideOptionsNumMedleySongs)].Visible = false;
            Buttons[htButtons(ButtonOptionsClose)].Visible = false;
            Buttons[htButtons(ButtonOptionsSing)].Visible = false;
            Buttons[htButtons(ButtonOptionsPlaylist)].Visible = false;
            Buttons[htButtons(ButtonOptionsRandom)].Visible = false;
            Buttons[htButtons(ButtonOptionsRandomCategory)].Visible = false;
            Buttons[htButtons(ButtonOptionsSingAll)].Visible = false;
            Buttons[htButtons(ButtonOptionsSingAllVisible)].Visible = false;
            Buttons[htButtons(ButtonOptionsOpenPlaylist)].Visible = false;
            Buttons[htButtons(ButtonOptionsRandomMedley)].Visible = false;
            Buttons[htButtons(ButtonOptionsStartMedley)].Visible = false;
            Texts[htTexts(TextOptionsTitle)].Visible = false;
            Statics[htStatics(StaticOptionsBG)].Visible = false;
            Playlists[htPlaylists(Playlist)].Visible = false;

            DragAndDropCover = GetNewStatic();

            Playlists[htPlaylists(Playlist)].Init();

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
                if (!KeyEvent.KeyPressed && KeyEvent.Key == Keys.Tab)
                {
                    _PlaylistActive = !_PlaylistActive;
                    Playlists[htPlaylists(Playlist)].Selected = _PlaylistActive;
                    SongMenus[htSongMenus(SongMenu)].SetActive(!_PlaylistActive);
                    return true;
                }

                Playlists[htPlaylists(Playlist)].HandleInput(KeyEvent);

                if (CPlaylists.NumPlaylists != SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistOpen)].NumValues)
                    UpdatePlaylistNames();

                return true;
            }

            if (!_SongOptionsActive)
            {
                if (KeyEvent.KeyPressed && !Char.IsControl(KeyEvent.Unicode) && KeyEvent.Mod != EModifier.Ctrl)
                {
                    if (_SearchActive)
                        ApplyNewSearchFilter(_SearchText + KeyEvent.Unicode);
                    /*
                    else if (!Char.IsControl(KeyEvent.Unicode))
                    {
                        JumpTo(KeyEvent.Unicode);
                        return true;
                    } */
                }
                else
                {
                    SongMenus[htSongMenus(SongMenu)].HandleInput(ref KeyEvent, _sso);
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
                                        if (Buttons[htButtons(ButtonsJoker[i])].Selected)
                                        {
                                            SelectNextRandom(i);
                                            return true;
                                        }
                                    }
                                }
                                if (Buttons[htButtons(ButtonStart)].Selected)
                                {
                                    HandlePartySongSelection(SongMenus[htSongMenus(SongMenu)].GetSelectedSong());
                                }
                            }
                            if (CSongs.NumVisibleSongs > 0 && !_sso.Selection.PartyMode)
                            {
                                if (SongMenus[htSongMenus(SongMenu)].GetSelectedSong() != -1 && !_SongOptionsActive)
                                {
                                    if (!_sso.Selection.PartyMode)
                                        ToggleSongOptions(ESongOptionsView.Song);
                                }
                            }
                            break;

                        case Keys.Tab:
                            if (Playlists[htPlaylists(Playlist)].Visible)
                            {
                                _PlaylistActive = !_PlaylistActive;
                                Playlists[htPlaylists(Playlist)].Selected = _PlaylistActive;
                                SongMenus[htSongMenus(SongMenu)].SetActive(!_PlaylistActive);
                            }
                            break;

                        case Keys.Back:
                            if (_SearchText.Length > 0)
                            {
                                ApplyNewSearchFilter(_SearchText.Remove(_SearchText.Length - 1));
                            }

                            if ((CSongs.Category < 0 || _sso.Sorting.Tabs == EOffOn.TR_CONFIG_OFF) && !_sso.Selection.PartyMode && !_SearchActive)
                            {
                                CGraphics.FadeTo(EScreens.ScreenMain);
                            }

                            break;

                        case Keys.Space:
                            if (!_SearchActive && !_sso.Selection.PartyMode)
                                ToggleSongOptions(ESongOptionsView.General);
                            break;

                        case Keys.F3:
                            if (_SearchActive)
                            {
                                _SearchActive = false;
                                _SearchText = String.Empty;
                                ApplyNewSearchFilter(_SearchText);
                            }
                            else if (!_sso.Selection.PartyMode)
                            {
                                _SearchActive = true;
                            }
                            break;

                        //TODO: Delete it! ??? Shouldn't we keep this as shortcut?
                        case Keys.A:
                            if (!_SearchActive && KeyEvent.Mod == EModifier.None && !_sso.Selection.PartyMode)
                            {
                                StartRandomAllSongs();
                            }
                            if (KeyEvent.Mod == EModifier.Ctrl && !_sso.Selection.PartyMode)
                            {
                                StartRandomVisibleSongs();
                            }
                            break;

                        //TODO: Delete that from here and from wiki!
                        case Keys.F:
                            if (KeyEvent.Mod == EModifier.Ctrl)
                            {
                                if (_SearchActive)
                                {
                                    _SearchActive = false;
                                    _SearchText = String.Empty;
                                    ApplyNewSearchFilter(_SearchText);
                                }
                                else if (!_sso.Selection.PartyMode)
                                {
                                    _SearchActive = true;
                                }
                            }
                            break;

                        //TODO: We need another key for random!
                        case Keys.R:
                            if (KeyEvent.Mod == EModifier.Ctrl && !_sso.Selection.RandomOnly)
                                SelectNextRandom(-1);
                            break;

                        //TODO: Delete that!
                        case Keys.S:
                            if (!_SearchActive && CSongs.NumVisibleSongs > 0 && !_sso.Selection.PartyMode)
                            {
                                StartMedleySong(SongMenus[htSongMenus(SongMenu)].GetSelectedSong());
                            }
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

            else
            {
                switch (KeyEvent.Key)
                {
                    case Keys.Enter:
                        if (Buttons[htButtons(ButtonOptionsClose)].Selected)
                        {
                            ToggleSongOptions(ESongOptionsView.None);
                        }
                        else if (Buttons[htButtons(ButtonOptionsSing)].Selected)
                        {
                            ToggleSongOptions(ESongOptionsView.None);
                            StartSong(SongMenus[htSongMenus(SongMenu)].GetSelectedSong());
                        }
                        else if (Buttons[htButtons(ButtonOptionsPlaylist)].Selected)
                        {
                            ToggleSongOptions(ESongOptionsView.None);
                            OpenAndAddPlaylistAction();
                        }
                        else if (Buttons[htButtons(ButtonOptionsRandom)].Selected)
                        {
                            if (CSongs.Category != -1)
                            {
                                SongMenus[htSongMenus(SongMenu)].SetSelectedSong(CSongs.GetRandomSong());
                            }
                        }
                        else if (Buttons[htButtons(ButtonOptionsRandom)].Selected)
                        {
                            if (CSongs.Category == -1)
                            {
                                SongMenus[htSongMenus(SongMenu)].SetSelectedCategory(CSongs.GetRandomCategory());
                            }
                        }                        
                        else if (Buttons[htButtons(ButtonOptionsSingAll)].Selected)
                        {
                            StartRandomAllSongs();
                        }
                        else if (Buttons[htButtons(ButtonOptionsSingAllVisible)].Selected)
                        {
                            StartRandomVisibleSongs();
                        }
                        else if (Buttons[htButtons(ButtonOptionsOpenPlaylist)].Selected)
                        {
                            ToggleSongOptions(ESongOptionsView.None);
                            OpenPlaylistAction();
                        }
                        else if (Buttons[htButtons(ButtonOptionsRandomMedley)].Selected)
                        {
                            ToggleSongOptions(ESongOptionsView.Medley);
                        }
                        else if (Buttons[htButtons(ButtonOptionsStartMedley)].Selected)
                        {
                            ToggleSongOptions(ESongOptionsView.None);
                            StartRandomMedley(SelectSlides[htSelectSlides(SelectSlideOptionsNumMedleySongs)].Selection + 1, CSongs.Category == -1);
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

            if (Playlists[htPlaylists(Playlist)].Visible && Playlists[htPlaylists(Playlist)].IsMouseOver(MouseEvent))
            {
                _PlaylistActive = true;
                Playlists[htPlaylists(Playlist)].Selected = _PlaylistActive;
                SongMenus[htSongMenus(SongMenu)].SetActive(!_PlaylistActive);
                ToggleSongOptions(ESongOptionsView.None);
            }
            else if (CHelper.IsInBounds(SongMenus[htSongMenus(SongMenu)].Rect, MouseEvent.X, MouseEvent.Y))
            {
                _PlaylistActive = false;
                Playlists[htPlaylists(Playlist)].Selected = _PlaylistActive;
                SongMenus[htSongMenus(SongMenu)].SetActive(!_PlaylistActive);
            }


            if (Playlists[htPlaylists(Playlist)].Visible && _PlaylistActive)
            {
                if (Playlists[htPlaylists(Playlist)].HandleMouse(MouseEvent))
                {
                    if (CPlaylists.NumPlaylists != SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistOpen)].NumValues)
                        UpdatePlaylistNames();
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
                if (CSongs.NumVisibleSongs > 0 && SongMenus[htSongMenus(SongMenu)].GetActualSelection() != -1)
                {
                    ToggleSongOptions(ESongOptionsView.None);
                    StartVisibleSong(SongMenus[htSongMenus(SongMenu)].GetActualSelection());
                    return true;
                }
            }

            SongMenus[htSongMenus(SongMenu)].HandleMouse(ref MouseEvent, _sso);
            UpdatePartyModeOptions();

            if (MouseEvent.Handled)
                return true;

            if (MouseEvent.LB)
            {
                if (IsMouseOver(MouseEvent))
                {
                    if (Buttons[htButtons(ButtonOpenOptions)].Selected)
                    {
                        ToggleSongOptions(ESongOptionsView.General);
                        return true;
                    }
                    
                    if (Buttons[htButtons(ButtonOptionsClose)].Selected)
                    {
                        ToggleSongOptions(ESongOptionsView.None);
                        return true;
                    }
                    
                    if (Buttons[htButtons(ButtonOptionsSing)].Selected)
                    {
                        ToggleSongOptions(ESongOptionsView.None);
                        StartSong(SongMenus[htSongMenus(SongMenu)].GetSelectedSong());
                        return true;
                    }
                    
                    if (Buttons[htButtons(ButtonOptionsPlaylist)].Selected)
                    {
                        ToggleSongOptions(ESongOptionsView.None);
                        OpenAndAddPlaylistAction();
                        return true;
                    }
                    
                    if (Buttons[htButtons(ButtonOptionsRandom)].Selected)
                    {
                        if (CSongs.Category != -1)
                        {
                            SongMenus[htSongMenus(SongMenu)].SetSelectedSong(CSongs.GetRandomSong());
                            return true;
                        }
                    }
                    
                    if (Buttons[htButtons(ButtonOptionsRandomCategory)].Selected)
                    {
                        if (CSongs.Category == -1)
                        {
                            SongMenus[htSongMenus(SongMenu)].SetSelectedCategory(CSongs.GetRandomCategory());
                            return true;
                        }
                    }
                    
                    if (Buttons[htButtons(ButtonOptionsSingAll)].Selected)
                    {
                        ToggleSongOptions(ESongOptionsView.None);
                        StartRandomAllSongs();
                        return true;
                    }
                    
                    if (Buttons[htButtons(ButtonOptionsSingAllVisible)].Selected)
                    {
                        ToggleSongOptions(ESongOptionsView.None);
                        StartRandomVisibleSongs();
                        return true;
                    }
                                       
                    if (Buttons[htButtons(ButtonOptionsOpenPlaylist)].Selected)
                    {
                        ToggleSongOptions(ESongOptionsView.None);
                        OpenPlaylistAction();
                        return true;
                    }

                    if (Buttons[htButtons(ButtonOptionsRandomMedley)].Selected)
                    {
                        ToggleSongOptions(ESongOptionsView.Medley);
                        return true;
                    }

                    if (Buttons[htButtons(ButtonOptionsStartMedley)].Selected)
                    {
                        ToggleSongOptions(ESongOptionsView.None);
                        StartRandomMedley(SelectSlides[htSelectSlides(SelectSlideOptionsNumMedleySongs)].Selection + 1, CSongs.Category == -1);
                        return true;
                    }

                    if (_sso.Selection.RandomOnly && _sso.Selection.NumJokers != null)
                    {
                        if (Buttons[htButtons(ButtonStart)].Selected)
                        {
                            HandlePartySongSelection(SongMenus[htSongMenus(SongMenu)].GetSelectedSong());
                            return true;
                        }
                            
                        for (int i = 0; i < ButtonsJoker.Count; i++)
                        {
                            if (i < _sso.Selection.NumJokers.Length)
                            {
                                if (Buttons[htButtons(ButtonsJoker[i])].Selected)
                                {
                                    SelectNextRandom(i);
                                    return true;
                                }
                            }
                        }
                    }
                }

                if (CSongs.NumVisibleSongs > 0 && SongMenus[htSongMenus(SongMenu)].GetActualSelection() != -1 && !_sso.Selection.PartyMode)
                {
                    if (SongMenus[htSongMenus(SongMenu)].GetSelectedSong() != -1 && !_SongOptionsActive)
                    {
                        if (!_sso.Selection.PartyMode)
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
                if (!DragAndDropActive && Playlists[htPlaylists(Playlist)].Visible && CSongs.NumVisibleSongs > 0 && SongMenus[htSongMenus(SongMenu)].GetActualSelection() != -1)
                {
                    DragAndDropCover = SongMenus[htSongMenus(SongMenu)].GetSelectedSongCover();
                    DragAndDropCover.Rect.Z = CSettings.zNear;
                    Playlists[htPlaylists(Playlist)].DragAndDropSongID = CSongs.VisibleSongs[SongMenus[htSongMenus(SongMenu)].GetActualSelection()].ID;
                    DragAndDropActive = true;
                    return true;
                }
            }


            if (!MouseEvent.LBH && DragAndDropActive)
            {
                DragAndDropActive = false;
                Playlists[htPlaylists(Playlist)].DragAndDropSongID = -1;
                return true;
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            SelectedSongID = -1;
            SelectedCategoryIndex = -2;

            _sso = CParty.GetSongSelectionOptions();
            CSongs.Sort(_sso.Sorting.SongSorting, _sso.Sorting.Tabs, _sso.Sorting.IgnoreArticles, _sso.Sorting.SearchString, _sso.Sorting.ShowDuetSongs);
            _SearchActive = _sso.Sorting.SearchActive;
            _SearchText = _sso.Sorting.SearchString;

            CGame.EnterNormalGame();
            SongMenus[htSongMenus(SongMenu)].OnShow();

            if (_sso.Selection.PartyMode)
                _PlaylistActive = false;

            if (_sso.Selection.PartyMode)
                ToggleSongOptions(ESongOptionsView.None);

            SongMenus[htSongMenus(SongMenu)].SetActive(!_PlaylistActive);
            SongMenus[htSongMenus(SongMenu)].SetSmallView(Playlists[htPlaylists(Playlist)].Visible);
                        
            if (Playlists[htPlaylists(Playlist)].ActivePlaylistID != -1)
                Playlists[htPlaylists(Playlist)].LoadPlaylist(Playlists[htPlaylists(Playlist)].ActivePlaylistID);
            UpdatePlaylistNames();

            DragAndDropActive = false;
            Playlists[htPlaylists(Playlist)].DragAndDropSongID = -1;

            UpdateGame();
        }

        public override bool UpdateGame()
        {
            if (SongMenus[htSongMenus(SongMenu)].IsSmallView())
                CheckPlaylist();

            Texts[htTexts(TextCategory)].Text = CSongs.GetCurrentCategoryName();

            if (CSongs.Category > -1 || CConfig.Tabs == EOffOn.TR_CONFIG_OFF)
                CBackgroundMusic.Disabled = true;
            else
                CBackgroundMusic.Disabled = false;

            int song = SongMenus[htSongMenus(SongMenu)].GetActualSelection();
            if ((CSongs.Category >= 0 || CConfig.Tabs == EOffOn.TR_CONFIG_OFF) && song >= 0 && song < CSongs.VisibleSongs.Length)
            {
                Texts[htTexts(TextSelection)].Text = CSongs.VisibleSongs[song].Artist + " - " + CSongs.VisibleSongs[song].Title;                
            }
            else if (CSongs.Category == -1 && song >= 0 && song < CSongs.Categories.Length)
            {
                Texts[htTexts(TextSelection)].Text = CSongs.Categories[song].Name;                
            }
            else
                Texts[htTexts(TextSelection)].Text = String.Empty;

            Texts[htTexts(TextSearchBar)].Text = _SearchText;
            if (_SearchActive)
            {
                Texts[htTexts(TextSearchBar)].Text += '|';

                Texts[htTexts(TextSearchBar)].Visible = true;
                Texts[htTexts(TextSearchBarTitle)].Visible = true;
                Statics[htStatics(StaticSearchBar)].Visible = true;
            }
            else
            {
                Texts[htTexts(TextSearchBar)].Visible = false;
                Texts[htTexts(TextSearchBarTitle)].Visible = false;
                Statics[htStatics(StaticSearchBar)].Visible = false;
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
            SongMenus[htSongMenus(SongMenu)].OnHide();
        }

        public override void ApplyVolume()
        {
            SongMenus[htSongMenus(SongMenu)].ApplyVolume(CConfig.PreviewMusicVolume);
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
            if (CSongs.Category == -1)
                SelectedSongID = -1;

            if (SelectedCategoryIndex != CSongs.Category)
            {
                SelectedCategoryIndex = CSongs.Category;
                CParty.OnCategoryChange(SelectedCategoryIndex, ref _sso);

                if (_sso.Selection.SelectNextRandomSong)
                    SelectNextRandomSong();

                if (_sso.Selection.SongIndex != -1)
                    SongMenus[htSongMenus(SongMenu)].SetSelectedSong(_sso.Selection.SongIndex);
            }

            if (SelectedSongID != SongMenus[htSongMenus(SongMenu)].GetSelectedSong() && CSongs.Category > -1)
            {
                SelectedSongID = SongMenus[htSongMenus(SongMenu)].GetSelectedSong();
                CParty.OnSongChange(SelectedSongID, ref _sso);
            }

            _sso = CParty.GetSongSelectionOptions();
 

            if (_sso.Selection.PartyMode)
            {
                CSongs.Sort(_sso.Sorting.SongSorting, _sso.Sorting.Tabs, _sso.Sorting.IgnoreArticles, _sso.Sorting.SearchString, _sso.Sorting.ShowDuetSongs);
                _SearchActive = _sso.Sorting.SearchActive;
                _SearchText = _sso.Sorting.SearchString;

                ClosePlaylist();
                ToggleSongOptions(ESongOptionsView.None);
            }

            SongMenus[htSongMenus(SongMenu)].Update(_sso);

            if (_sso.Selection.RandomOnly && _sso.Selection.NumJokers != null)
            {
                Buttons[htButtons(ButtonStart)].Visible = true;

                if (!SongMenus[htSongMenus(SongMenu)].IsSmallView())
                    SongMenus[htSongMenus(SongMenu)].SetSmallView(true);

                for (int i = 0; i < CMain.Settings.GetMaxNumPlayer(); i++)
                {
                    if (i < _sso.Selection.NumJokers.Length)
                    {
                        Buttons[htButtons(ButtonsJoker[i])].Visible = true;
                        Buttons[htButtons(ButtonsJoker[i])].Text.Text = _sso.Selection.NumJokers[i].ToString();
                        Texts[htTexts(TextsPlayer[i])].Visible = true;

                        bool NameExists = false;
                        if (_sso.Selection.TeamNames != null)
                        {
                            if (_sso.Selection.TeamNames.Length > i)
                            {
                                Texts[htTexts(TextsPlayer[i])].Text = _sso.Selection.TeamNames[i];
                                NameExists = true;
                            }
                        }

                        if (!NameExists)
                            Texts[htTexts(TextsPlayer[i])].Text = i.ToString();
                    }
                    else
                    {
                        Buttons[htButtons(ButtonsJoker[i])].Visible = false;
                        Texts[htTexts(TextsPlayer[i])].Visible = false;
                    }
                }
            }
            else
            {
                if (_sso.Selection.PartyMode && SongMenus[htSongMenus(SongMenu)].IsSmallView())
                    SongMenus[htSongMenus(SongMenu)].SetSmallView(false);

                for (int i = 0; i < CMain.Settings.GetMaxNumPlayer(); i++)
                {
                    Buttons[htButtons(ButtonsJoker[i])].Visible = false;
                    Texts[htTexts(TextsPlayer[i])].Visible = false;
                }

                Buttons[htButtons(ButtonStart)].Visible = false;
            }
        }

        private void StartSong(int SongNr)
        {
            if ((CSongs.Category >= 0) && (SongNr >= 0))
            {
                EGameMode gm;
                if (_AvailableGameModes.Count >= SelectSlides[htSelectSlides(SelectSlideOptionsMode)].Selection)
                {
                    gm = _AvailableGameModes[SelectSlides[htSelectSlides(SelectSlideOptionsMode)].Selection];
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
            if (CSongs.Category != -1)
            {
                ToggleSongOptions(ESongOptionsView.None);
                SongMenus[htSongMenus(SongMenu)].SetSelectedSong(CSongs.GetRandomSong());
                return true;
            }
            return false;
        }

        private bool SelectNextRandomCategory()
        {
            if (CSongs.Category == -1)
            {
                ToggleSongOptions(ESongOptionsView.None);
                SongMenus[htSongMenus(SongMenu)].SetSelectedCategory(CSongs.GetRandomCategory());
                return true;
            }
            return false;
        }

        private void JumpTo(char Letter)
        {
            int song = SongMenus[htSongMenus(SongMenu)].GetSelectedSong();
            int id = -1;
            if (song > -1 && song < CSongs.NumVisibleSongs)
            {
                id = CSongs.VisibleSongs[song].ID;
            }

            int visibleID = Array.FindIndex<CSong>(CSongs.VisibleSongs, element => element.Artist.StartsWith(Letter.ToString(), StringComparison.OrdinalIgnoreCase));
            if (visibleID > -1)
            {
                id = visibleID;
                SongMenus[htSongMenus(SongMenu)].SetSelectedSong(id);
            }
        }

        private void ApplyNewSearchFilter(string NewFilterString)
        {
            CParty.SetSearchString(NewFilterString, _SearchActive);
            _sso = CParty.GetSongSelectionOptions();
            
            bool refresh = false;
            _SearchText = NewFilterString;

            int SongIndex = SongMenus[htSongMenus(SongMenu)].GetSelectedSong();
            int SongID = -1;
            if (SongIndex != -1 && CSongs.NumVisibleSongs > 0 && CSongs.NumVisibleSongs > SongIndex)
            {
                SongID = CSongs.VisibleSongs[SongIndex].ID;
            }

            if (NewFilterString == String.Empty && _sso.Sorting.Tabs == EOffOn.TR_CONFIG_ON)
            {
                CSongs.Category = -1;
                refresh = true;
            }

            if (NewFilterString != String.Empty && CSongs.Category != 0)
            {
                CSongs.Category = 0;
                refresh = true;
            }

            CSongs.Sort(_sso.Sorting.SongSorting, _sso.Sorting.Tabs, _sso.Sorting.IgnoreArticles, NewFilterString, _sso.Sorting.ShowDuetSongs);

            if (SongID == -1 || CSongs.NumVisibleSongs == 0 || CSongs.NumVisibleSongs <= SongIndex || CSongs.VisibleSongs[SongIndex].ID != SongID)
                refresh = true;

            if (refresh)
                SongMenus[htSongMenus(SongMenu)].OnHide();

            SongMenus[htSongMenus(SongMenu)].OnShow();
        }

        private void ToggleSongOptions(ESongOptionsView view)
        {
            _SongOptionsActive = view != ESongOptionsView.None;

            SelectSlides[htSelectSlides(SelectSlideOptionsMode)].Visible = false;
            SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistAdd)].Visible = false;
            SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistOpen)].Visible = false;
            SelectSlides[htSelectSlides(SelectSlideOptionsNumMedleySongs)].Visible = false;
            Buttons[htButtons(ButtonOptionsClose)].Visible = false;
            Buttons[htButtons(ButtonOptionsSing)].Visible = false;
            Buttons[htButtons(ButtonOptionsPlaylist)].Visible = false;
            Buttons[htButtons(ButtonOptionsRandom)].Visible = false;
            Buttons[htButtons(ButtonOptionsRandomCategory)].Visible = false;
            Buttons[htButtons(ButtonOptionsSingAll)].Visible = false;
            Buttons[htButtons(ButtonOptionsSingAllVisible)].Visible = false;
            Buttons[htButtons(ButtonOptionsOpenPlaylist)].Visible = false;
            Buttons[htButtons(ButtonOptionsRandomMedley)].Visible = false;
            Buttons[htButtons(ButtonOptionsStartMedley)].Visible = false;
            Texts[htTexts(TextOptionsTitle)].Visible = false;
            Statics[htStatics(StaticOptionsBG)].Visible = false;
            Buttons[htButtons(ButtonOpenOptions)].Visible = true;

            if (_SongOptionsActive)
            {
                if (view == ESongOptionsView.Song)
                {
                    EGameMode LastMode = EGameMode.TR_GAMEMODE_NORMAL;
                    if (_AvailableGameModes.Count > 0)
                        LastMode = _AvailableGameModes[SelectSlides[htSelectSlides(SelectSlideOptionsMode)].Selection];
                    SetInteractionToButton(Buttons[htButtons(ButtonOptionsSing)]);
                    _AvailableGameModes.Clear();
                    SelectSlides[htSelectSlides(SelectSlideOptionsMode)].Clear();
                    if (!CSongs.VisibleSongs[SongMenus[htSongMenus(SongMenu)].GetSelectedSong()].IsDuet)
                    {
                        SelectSlides[htSelectSlides(SelectSlideOptionsMode)].AddValue(Enum.GetName(typeof(EGameMode), EGameMode.TR_GAMEMODE_NORMAL));
                        _AvailableGameModes.Add(EGameMode.TR_GAMEMODE_NORMAL);
                        SelectSlides[htSelectSlides(SelectSlideOptionsMode)].AddValue(Enum.GetName(typeof(EGameMode), EGameMode.TR_GAMEMODE_SHORTSONG));
                        _AvailableGameModes.Add(EGameMode.TR_GAMEMODE_SHORTSONG);
                    }
                    if (CSongs.VisibleSongs[SongMenus[htSongMenus(SongMenu)].GetSelectedSong()].IsDuet)
                    {
                        SelectSlides[htSelectSlides(SelectSlideOptionsMode)].AddValue(Enum.GetName(typeof(EGameMode), EGameMode.TR_GAMEMODE_DUET));
                        _AvailableGameModes.Add(EGameMode.TR_GAMEMODE_DUET);
                    }
                    if (CSongs.VisibleSongs[SongMenus[htSongMenus(SongMenu)].GetSelectedSong()].Medley.Source != EMedleySource.None)
                    {
                        SelectSlides[htSelectSlides(SelectSlideOptionsMode)].AddValue(Enum.GetName(typeof(EGameMode), EGameMode.TR_GAMEMODE_MEDLEY));
                        _AvailableGameModes.Add(EGameMode.TR_GAMEMODE_MEDLEY);
                    }
                    //Set SelectSlide-Selection to last selected game-mode if possible
                    for (int i = 0; i < _AvailableGameModes.Count; i++)
                    {
                        if (_AvailableGameModes[i] == LastMode)
                            SelectSlides[htSelectSlides(SelectSlideOptionsMode)].SetSelectionByValueIndex(i);
                    }
                    SelectSlides[htSelectSlides(SelectSlideOptionsMode)].Visible = true;
                    SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistAdd)].Visible = true;
                    Buttons[htButtons(ButtonOptionsSing)].Visible = true;
                    Buttons[htButtons(ButtonOptionsPlaylist)].Visible = true;
                    SetInteractionToButton(Buttons[htButtons(ButtonOptionsSing)]);
                }
                else if (view == ESongOptionsView.General)
                {
                    Buttons[htButtons(ButtonOptionsRandom)].Visible = CSongs.Category != -1;
                    Buttons[htButtons(ButtonOptionsRandomCategory)].Visible = CSongs.Category == -1;
                    Buttons[htButtons(ButtonOptionsSingAll)].Visible = true;
                    Buttons[htButtons(ButtonOptionsSingAllVisible)].Visible = CSongs.Category != -1;

                    if (SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistOpen)].NumValues > 0)
                    {
                        Buttons[htButtons(ButtonOptionsOpenPlaylist)].Visible = true;
                        SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistOpen)].Visible = true;
                        SetInteractionToButton(Buttons[htButtons(ButtonOptionsOpenPlaylist)]);
                    }
                    else
                    {
                        Buttons[htButtons(ButtonOptionsOpenPlaylist)].Visible = false;
                        SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistOpen)].Visible = false;

                        if (Buttons[htButtons(ButtonOptionsRandom)].Visible)
                            SetInteractionToButton(Buttons[htButtons(ButtonOptionsRandom)]);
                        else
                            SetInteractionToButton(Buttons[htButtons(ButtonOptionsSingAll)]);
                    }
                    Buttons[htButtons(ButtonOptionsRandomMedley)].Visible = true;
                    if(Buttons[htButtons(ButtonOptionsRandom)].Visible)
                        SetInteractionToButton(Buttons[htButtons(ButtonOptionsRandom)]);
                    else
                        SetInteractionToButton(Buttons[htButtons(ButtonOptionsRandomCategory)]);
                }
                else if (view == ESongOptionsView.Medley)
                {
                    Buttons[htButtons(ButtonOptionsStartMedley)].Visible = true;
                    SelectSlides[htSelectSlides(SelectSlideOptionsNumMedleySongs)].Visible = true;
                    SelectSlides[htSelectSlides(SelectSlideOptionsNumMedleySongs)].Clear();
                    if (CSongs.Category != -1)
                    {
                        for (int i = 1; i <= CSongs.VisibleSongs.Length; i++)
                        {
                            SelectSlides[htSelectSlides(SelectSlideOptionsNumMedleySongs)].AddValue(i.ToString());
                        }
                    }
                    else
                    {
                        for (int i = 1; i <= CSongs.AllSongs.Length; i++)
                        {
                            SelectSlides[htSelectSlides(SelectSlideOptionsNumMedleySongs)].AddValue(i.ToString());
                        }
                    }
                    if (SelectSlides[htSelectSlides(SelectSlideOptionsNumMedleySongs)].NumValues >= 5)
                        SelectSlides[htSelectSlides(SelectSlideOptionsNumMedleySongs)].SetSelectionByValueIndex(4);
                    else
                        SelectSlides[htSelectSlides(SelectSlideOptionsNumMedleySongs)].SetSelectionByValueIndex(SelectSlides[htSelectSlides(SelectSlideOptionsNumMedleySongs)].NumValues - 1);
                    SetInteractionToButton(Buttons[htButtons(ButtonOptionsStartMedley)]);
                    
                }
                Texts[htTexts(TextOptionsTitle)].Visible = true;
                Buttons[htButtons(ButtonOptionsClose)].Visible = true;
                Statics[htStatics(StaticOptionsBG)].Visible = true;
                Buttons[htButtons(ButtonOpenOptions)].Visible = false;
            }
        }

        #region Playlist Actions
        public void CheckPlaylist()
        {
            if (Playlists[htPlaylists(Playlist)].ActivePlaylistID == -1 && _PlaylistActive)
                ClosePlaylist();
        }

        private void OpenPlaylist(int PlaylistID)
        {
            if (CPlaylists.Playlists.Length > PlaylistID && PlaylistID > -1)
            {
                Playlists[htPlaylists(Playlist)].LoadPlaylist(PlaylistID);
                SongMenus[htSongMenus(SongMenu)].SetSmallView(true);
                Playlists[htPlaylists(Playlist)].Visible = true;
            }
        }

        private void ClosePlaylist()
        {
            if (Playlists[htPlaylists(Playlist)].Visible || _PlaylistActive)
            {
                SongMenus[htSongMenus(SongMenu)].SetSmallView(false);
                _PlaylistActive = false;
                Playlists[htPlaylists(Playlist)].Selected = _PlaylistActive;
                SongMenus[htSongMenus(SongMenu)].SetActive(!_PlaylistActive);
                Playlists[htPlaylists(Playlist)].ClosePlaylist();
            }
        }

        private void UpdatePlaylistNames()
        {
            SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistAdd)].Clear();
            SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistAdd)].AddValue("TR_SCREENSONG_NEWPLAYLIST");
            SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistAdd)].AddValues(CPlaylists.PlaylistNames);
            SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistOpen)].Clear();
            SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistOpen)].AddValues(CPlaylists.PlaylistNames);
        }

        private void OpenPlaylistAction()
        {
            //Open a playlist
            if (Playlists[htPlaylists(Playlist)].ActivePlaylistID != (SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistOpen)].Selection))
            {
                Playlists[htPlaylists(Playlist)].ActivePlaylistID = SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistOpen)].Selection;
                SetSelectSlidePlaylistToCurrentPlaylist();

                //Open playlist
                OpenPlaylist(Playlists[htPlaylists(Playlist)].ActivePlaylistID);
            }
        }

        private void OpenAndAddPlaylistAction()
        {
            //Open an existing playlist and add song
            if (Playlists[htPlaylists(Playlist)].ActivePlaylistID != (SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistAdd)].Selection - 1) && (SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistAdd)].Selection - 1) != -1)
            {
                //Check selected game-mode
                EGameMode gm;
                if (_AvailableGameModes.Count >= SelectSlides[htSelectSlides(SelectSlideOptionsMode)].Selection)
                    gm = _AvailableGameModes[SelectSlides[htSelectSlides(SelectSlideOptionsMode)].Selection];
                else
                    if (CSongs.VisibleSongs[SongMenus[htSongMenus(SongMenu)].GetSelectedSong()].IsDuet)
                        gm = EGameMode.TR_GAMEMODE_DUET;
                    else
                        gm = EGameMode.TR_GAMEMODE_NORMAL;
                
                //Check if Playlist really exists
                if (SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistAdd)].Selection - 1 >= 0)
                {
                    Playlists[htPlaylists(Playlist)].ActivePlaylistID = SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistAdd)].Selection - 1;

                    //Add song to playlist
                    CPlaylists.Playlists[Playlists[htPlaylists(Playlist)].ActivePlaylistID].AddSong(CSongs.VisibleSongs[SongMenus[htSongMenus(SongMenu)].GetSelectedSong()].ID, gm);

                    //Open playlist
                    OpenPlaylist(Playlists[htPlaylists(Playlist)].ActivePlaylistID);

                    SetSelectSlidePlaylistToCurrentPlaylist();
                    Playlists[htPlaylists(Playlist)].ScrollToBottom();
                }
            }
            //Create a new playlist and add song
            else if ((SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistAdd)].Selection - 1) == -1)
            {
                //Check selected game-mode
                EGameMode gm;
                if (_AvailableGameModes.Count >= SelectSlides[htSelectSlides(SelectSlideOptionsMode)].Selection)
                    gm = _AvailableGameModes[SelectSlides[htSelectSlides(SelectSlideOptionsMode)].Selection];
                else
                    if (CSongs.VisibleSongs[SongMenus[htSongMenus(SongMenu)].GetSelectedSong()].IsDuet)
                        gm = EGameMode.TR_GAMEMODE_DUET;
                    else
                        gm = EGameMode.TR_GAMEMODE_NORMAL;
                
                //Create new playlist
                Playlists[htPlaylists(Playlist)].ActivePlaylistID = CPlaylists.NewPlaylist();
                
                //Add song to playlist
                CPlaylists.Playlists[Playlists[htPlaylists(Playlist)].ActivePlaylistID].AddSong(CSongs.VisibleSongs[SongMenus[htSongMenus(SongMenu)].GetSelectedSong()].ID, gm);
                
                //Open playlist
                OpenPlaylist(Playlists[htPlaylists(Playlist)].ActivePlaylistID);
                
                //Add new playlist to select-slide
                SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistAdd)].AddValue(CPlaylists.Playlists[Playlists[htPlaylists(Playlist)].ActivePlaylistID].PlaylistName);
                SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistOpen)].AddValue(CPlaylists.Playlists[Playlists[htPlaylists(Playlist)].ActivePlaylistID].PlaylistName);

                SetSelectSlidePlaylistToCurrentPlaylist();

            }
            //Add song to loaded playlist
            else
            {
                //Check selected game-mode
                EGameMode gm;
                if (_AvailableGameModes.Count >= SelectSlides[htSelectSlides(SelectSlideOptionsMode)].Selection)
                    gm = _AvailableGameModes[SelectSlides[htSelectSlides(SelectSlideOptionsMode)].Selection];
                else
                    if (CSongs.VisibleSongs[SongMenus[htSongMenus(SongMenu)].GetSelectedSong()].IsDuet)
                        gm = EGameMode.TR_GAMEMODE_DUET;
                    else
                        gm = EGameMode.TR_GAMEMODE_NORMAL;
                CPlaylists.Playlists[Playlists[htPlaylists(Playlist)].ActivePlaylistID].AddSong(CSongs.VisibleSongs[SongMenus[htSongMenus(SongMenu)].GetSelectedSong()].ID, gm);
                Playlists[htPlaylists(Playlist)].UpdatePlaylist();
                Playlists[htPlaylists(Playlist)].ScrollToBottom();
            }
        }

        private void SetSelectSlidePlaylistToCurrentPlaylist()
        {
            if (Playlists[htPlaylists(Playlist)].ActivePlaylistID > -1)
                SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistAdd)].Selection = Playlists[htPlaylists(Playlist)].ActivePlaylistID + 1;
            else
                SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistAdd)].Selection = 0;
        }
        #endregion Playlist Actions
    }
}
