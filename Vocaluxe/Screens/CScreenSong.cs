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
using Vocaluxe.Lib.Song;
using Vocaluxe.Menu;
using Vocaluxe.Menu.SongMenu;

namespace Vocaluxe.Screens
{
    class CScreenSong : CMenu
    {
        enum ESongOptionsView
        {
            None,
            Song,
            General
        }

        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 3;

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

        private const string SelectSlideOptionsMode = "SelectSlideOptionsMode";
        private const string SelectSlideOptionsPlaylistAdd = "SelectSlideOptionsPlaylistAdd";
        private const string SelectSlideOptionsPlaylistOpen = "SelectSlideOptionsPlaylistOpen";

        private const string StaticSearchBar = "StaticSearchBar";
        private const string StaticOptionsBG = "StaticOptionsBG";
        private const string SongMenu = "SongMenu";
        private const string Playlist = "Playlist";

        private string _SearchText = String.Empty;
        private bool _SearchActive = false;

        private bool _SongOptionsActive = false;
        private bool _PlaylistActive = false;
        private List<GameModes.EGameMode> _AvailableGameModes;

        private CStatic DragAndDropCover;
        private bool DragAndDropActive;
        private int OldMousePosX;
        private int OldMousePosY; 

        public CScreenSong()
        {
            Init();

            _AvailableGameModes = new List<GameModes.EGameMode>();
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "ScreenSong";
            _ScreenVersion = ScreenVersion;
            _ThemeStatics = new string[] { StaticSearchBar, StaticOptionsBG };
            _ThemeTexts = new string[] { TextCategory, TextSelection, TextSearchBarTitle, TextSearchBar, TextOptionsTitle };
            _ThemeButtons = new string[] { ButtonOptionsClose, ButtonOptionsPlaylist, ButtonOptionsSing, ButtonOptionsRandom, ButtonOptionsRandomCategory, ButtonOptionsSingAll, ButtonOptionsSingAllVisible, ButtonOptionsOpenPlaylist, ButtonOpenOptions };
            _ThemeSelectSlides = new string[] { SelectSlideOptionsMode, SelectSlideOptionsPlaylistAdd, SelectSlideOptionsPlaylistOpen };
            _ThemeSongMenus = new string[] { SongMenu };
            _ThemePlaylists = new string[] { Playlist };
        }

        public override void LoadTheme()
        {
            base.LoadTheme();
            SelectSlides[htSelectSlides(SelectSlideOptionsMode)].Visible = false;
            SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistAdd)].Visible = false;
            SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistOpen)].Visible = false;
            Buttons[htButtons(ButtonOptionsClose)].Visible = false;
            Buttons[htButtons(ButtonOptionsSing)].Visible = false;
            Buttons[htButtons(ButtonOptionsPlaylist)].Visible = false;
            Buttons[htButtons(ButtonOptionsRandom)].Visible = false;
            Buttons[htButtons(ButtonOptionsRandomCategory)].Visible = false;
            Buttons[htButtons(ButtonOptionsSingAll)].Visible = false;
            Buttons[htButtons(ButtonOptionsSingAllVisible)].Visible = false;
            Buttons[htButtons(ButtonOptionsOpenPlaylist)].Visible = false;
            Texts[htTexts(TextOptionsTitle)].Visible = false;
            Statics[htStatics(StaticOptionsBG)].Visible = false;
            Playlists[htPlaylists(Playlist)].Visible = false;

            DragAndDropCover = new CStatic();

            Playlists[htPlaylists(Playlist)].Init();
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
                if (KeyEvent.KeyPressed && !Char.IsControl(KeyEvent.Unicode))
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
                    SongMenus[htSongMenus(SongMenu)].HandleInput(ref KeyEvent);
                    if (KeyEvent.Handled)
                        return true;

                    switch (KeyEvent.Key)
                    {
                        case Keys.Escape:
                            if (CSongs.Category < 0 || CConfig.Tabs == EOffOn.TR_CONFIG_OFF)
                                CGraphics.FadeTo(EScreens.ScreenMain);
                            break;

                        case Keys.Enter:
                            if (CSongs.NumVisibleSongs > 0)
                            {
                                if (SongMenus[htSongMenus(SongMenu)].GetSelectedSong() != -1 && !_SongOptionsActive)
                                    ToggleSongOptions(ESongOptionsView.Song);
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

                            if (!_SearchActive && CSongs.Category < 0)
                                CGraphics.FadeTo(EScreens.ScreenMain);

                            break;

                        case Keys.Space:
                            ToggleSongOptions(ESongOptionsView.General);
                            break;

                        case Keys.F3:
                            if (_SearchActive)
                            {
                                _SearchActive = false;
                                _SearchText = String.Empty;
                                ApplyNewSearchFilter(_SearchText);
                            }
                            else
                            {
                                _SearchActive = true;
                            }
                            break;

                        //TODO: Delete it! ??? Shouldn't we keep this as shortcut?
                        case Keys.A:
                            if (!_SearchActive && KeyEvent.Mod == Modifier.None)
                            {
                                StartRandomAllSongs();
                            }
                            if (KeyEvent.Mod == Modifier.Ctrl)
                            {
                                StartRandomVisibleSongs();
                            }
                            break;

                        //TODO: Delete that from here and from wiki!
                        case Keys.F:
                            if (KeyEvent.Mod == Modifier.Ctrl)
                            {
                                if (_SearchActive)
                                {
                                    _SearchActive = false;
                                    _SearchText = String.Empty;
                                    ApplyNewSearchFilter(_SearchText);
                                }
                                else
                                {
                                    _SearchActive = true;
                                }
                            }
                            break;

                        //TODO: We need another key for random!
                        case Keys.R:
                            if (CSongs.Category != -1 && KeyEvent.Mod == Modifier.Ctrl)
                            {
                                SongMenus[htSongMenus(SongMenu)].SetSelectedSong(CSongs.GetRandomSong());
                            }
                            else if (CSongs.Category == -1 && KeyEvent.Mod == Modifier.Ctrl)
                            {
                                SongMenus[htSongMenus(SongMenu)].SetSelectedCategory(CSongs.GetRandomCategory());
                            }
                            break;

                        //TODO: Delete that!
                        case Keys.S:
                            if (CSongs.NumVisibleSongs > 0)
                            {
                                StartMedleySong(SongMenus[htSongMenus(SongMenu)].GetSelectedSong());
                            }
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
                            OpenPlaylistAction();
                        }
                        break;

                    case Keys.Escape:
                    case Keys.Back:
                    case Keys.Space:
                        ToggleSongOptions(ESongOptionsView.None);
                        break;
                }
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
            }
            else if (CHelper.IsInBounds(SongMenus[htSongMenus(SongMenu)].Rect, MouseEvent.X, MouseEvent.Y))
            {
                _PlaylistActive = false;
                Playlists[htPlaylists(Playlist)].Selected = _PlaylistActive;
                SongMenus[htSongMenus(SongMenu)].SetActive(!_PlaylistActive);
            }

            if (Playlists[htPlaylists(Playlist)].Visible && Playlists[htPlaylists(Playlist)].HandleMouse(MouseEvent))
                return true;

            if (!_SongOptionsActive)
            {

                if ((MouseEvent.RB) && (CSongs.Category < 0))
                {
                    CGraphics.FadeTo(EScreens.ScreenMain);
                }
                else if (MouseEvent.RB && _SongOptionsActive)
                    ToggleSongOptions(ESongOptionsView.None);

                if (MouseEvent.MB && CSongs.Category != -1)
                {
                    Console.WriteLine("MB pressed");
                    SongMenus[htSongMenus(SongMenu)].SetSelectedSong(CSongs.GetRandomSong());
                }
                else if (MouseEvent.MB && CSongs.Category == -1)
                {
                    Console.WriteLine("MB pressed");
                    SongMenus[htSongMenus(SongMenu)].SetSelectedCategory(CSongs.GetRandomCategory());
                }
                else
                    SongMenus[htSongMenus(SongMenu)].HandleMouse(ref MouseEvent);

                if (MouseEvent.LBH && !DragAndDropActive && Playlists[htPlaylists(Playlist)].Visible && CSongs.NumVisibleSongs > 0 && SongMenus[htSongMenus(SongMenu)].GetActualSelection() != -1)
                {
                    DragAndDropCover = SongMenus[htSongMenus(SongMenu)].GetSelectedSongCover();
                    DragAndDropCover.Rect.Z = CSettings.zNear;
                    Playlists[htPlaylists(Playlist)].DragAndDropSongID = CSongs.VisibleSongs[SongMenus[htSongMenus(SongMenu)].GetActualSelection()].ID;
                    DragAndDropActive = true;
                }
                else if (MouseEvent.LB && CSongs.NumVisibleSongs > 0 && SongMenus[htSongMenus(SongMenu)].GetActualSelection() != -1)
                {
                    if (SongMenus[htSongMenus(SongMenu)].GetSelectedSong() != -1 && !_SongOptionsActive)
                    {
                        ToggleSongOptions(ESongOptionsView.Song);
                    }
                }
                else if (MouseEvent.LB && IsMouseOver(MouseEvent))
                {
                    if (Buttons[htButtons(ButtonOpenOptions)].Selected)
                    {
                        ToggleSongOptions(ESongOptionsView.General);
                    }
                }
            }
            else
            {
                if (MouseEvent.LB && IsMouseOver(MouseEvent))
                {
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
                        OpenAndAddPlaylistAction();
                    }
                    else if (Buttons[htButtons(ButtonOptionsRandom)].Selected)
                    {
                        if (CSongs.Category != -1)
                        {
                            SongMenus[htSongMenus(SongMenu)].SetSelectedSong(CSongs.GetRandomSong());
                        }
                    }
                    else if (Buttons[htButtons(ButtonOptionsRandomCategory)].Selected)
                    {
                        if (CSongs.Category == -1)
                        {
                            SongMenus[htSongMenus(SongMenu)].SetSelectedCategory(CSongs.GetRandomCategory());
                        }
                    }
                    else if (Buttons[htButtons(ButtonOptionsSingAll)].Selected)
                    {
                        ToggleSongOptions(ESongOptionsView.None);
                        StartRandomAllSongs();
                    }
                    else if (Buttons[htButtons(ButtonOptionsSingAllVisible)].Selected)
                    {
                        ToggleSongOptions(ESongOptionsView.None);
                        StartRandomVisibleSongs();
                    }
                    else if (Buttons[htButtons(ButtonOptionsOpenPlaylist)].Selected)
                    {
                        OpenPlaylistAction();
                    }
                }
                if (MouseEvent.RB)
                {
                    ToggleSongOptions(ESongOptionsView.None);
                }
            }

            if (!MouseEvent.LBH && DragAndDropActive)
            {
                DragAndDropActive = false;
                Playlists[htPlaylists(Playlist)].DragAndDropSongID = -1;
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
            CGame.EnterNormalGame();
            SongMenus[htSongMenus(SongMenu)].OnShow();
            SongMenus[htSongMenus(SongMenu)].SetActive(!_PlaylistActive);
            SongMenus[htSongMenus(SongMenu)].SetSmallView(Playlists[htPlaylists(Playlist)].Visible);
                        
            if (Playlists[htPlaylists(Playlist)].ActivePlaylistID != -1)
                Playlists[htPlaylists(Playlist)].LoadPlaylist(Playlists[htPlaylists(Playlist)].ActivePlaylistID);
            UpdatePlaylistNames();

            DragAndDropActive = false;
            Playlists[htPlaylists(Playlist)].DragAndDropSongID = -1;
        }

        public override bool UpdateGame()
        {
            SongMenus[htSongMenus(SongMenu)].Update();

            if (SongMenus[htSongMenus(SongMenu)].IsSmallView())
                CheckPlaylist();

            Texts[htTexts(TextCategory)].Text = CSongs.GetActualCategoryName();

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
                       gm = GameModes.EGameMode.TR_GAMEMODE_DUET;
                    else
                       gm = GameModes.EGameMode.TR_GAMEMODE_NORMAL;
                }

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
                    gm = GameModes.EGameMode.TR_GAMEMODE_MEDLEY;
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

        private void JumpTo(char Letter)
        {
            int song = SongMenus[htSongMenus(SongMenu)].GetSelectedSong();
            int id = -1;
            if (song > -1 && song < CSongs.NumVisibleSongs)
            {
                id = CSongs.VisibleSongs[song].ID;
            }

            int visibleID = Array.FindIndex<Vocaluxe.Lib.Song.CSong>(CSongs.VisibleSongs, element => element.Artist.StartsWith(Letter.ToString(), StringComparison.OrdinalIgnoreCase));
            if (visibleID > -1)
            {
                id = visibleID;
                SongMenus[htSongMenus(SongMenu)].SetSelectedSong(id);
            }
        }

        private void ApplyNewSearchFilter(string NewFilterString)
        {
            int song = SongMenus[htSongMenus(SongMenu)].GetSelectedSong();
            int id = -1;
            if (song > -1 && song < CSongs.NumVisibleSongs)
            {
                id = CSongs.VisibleSongs[song].ID;
            }

            _SearchText = NewFilterString;
            CSongs.SearchFilter = _SearchText;

            if (NewFilterString != String.Empty)
                CSongs.Category = 0;
            else
                CSongs.Category = -1;

            if (id > -1)
            {
                if (CSongs.NumVisibleSongs > 0)
                {
                    if (id != CSongs.VisibleSongs[0].ID)
                        SongMenus[htSongMenus(SongMenu)].OnHide();
                }
            }

            if (CSongs.NumVisibleSongs == 0)
                SongMenus[htSongMenus(SongMenu)].OnHide();

            SongMenus[htSongMenus(SongMenu)].OnShow();
        }

        private void ToggleSongOptions(ESongOptionsView view)
        {
            _SongOptionsActive = !_SongOptionsActive;
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
                        SelectSlides[htSelectSlides(SelectSlideOptionsMode)].AddValue(Enum.GetName(typeof(GameModes.EGameMode), GameModes.EGameMode.TR_GAMEMODE_NORMAL));
                        _AvailableGameModes.Add(GameModes.EGameMode.TR_GAMEMODE_NORMAL);
                        SelectSlides[htSelectSlides(SelectSlideOptionsMode)].AddValue(Enum.GetName(typeof(GameModes.EGameMode), GameModes.EGameMode.TR_GAMEMODE_SHORTSONG));
                        _AvailableGameModes.Add(GameModes.EGameMode.TR_GAMEMODE_SHORTSONG);
                    }
                    if (CSongs.VisibleSongs[SongMenus[htSongMenus(SongMenu)].GetSelectedSong()].IsDuet)
                    {
                        SelectSlides[htSelectSlides(SelectSlideOptionsMode)].AddValue(Enum.GetName(typeof(GameModes.EGameMode), GameModes.EGameMode.TR_GAMEMODE_DUET));
                        _AvailableGameModes.Add(GameModes.EGameMode.TR_GAMEMODE_DUET);
                    }
                    if (CSongs.VisibleSongs[SongMenus[htSongMenus(SongMenu)].GetSelectedSong()].Medley.Source != EMedleySource.None)
                    {
                        SelectSlides[htSelectSlides(SelectSlideOptionsMode)].AddValue(Enum.GetName(typeof(GameModes.EGameMode), GameModes.EGameMode.TR_GAMEMODE_MEDLEY));
                        _AvailableGameModes.Add(GameModes.EGameMode.TR_GAMEMODE_MEDLEY);
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
                }
                Buttons[htButtons(ButtonOptionsClose)].Visible = true;
                Texts[htTexts(TextOptionsTitle)].Visible = true;
                Statics[htStatics(StaticOptionsBG)].Visible = true;
                Buttons[htButtons(ButtonOpenOptions)].Visible = false;
            }
            else 
            {
                SelectSlides[htSelectSlides(SelectSlideOptionsMode)].Visible = false;
                SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistAdd)].Visible = false;
                SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistOpen)].Visible = false;
                Buttons[htButtons(ButtonOptionsClose)].Visible = false;
                Buttons[htButtons(ButtonOptionsSing)].Visible = false;
                Buttons[htButtons(ButtonOptionsPlaylist)].Visible = false;
                Buttons[htButtons(ButtonOptionsRandom)].Visible = false;
                Buttons[htButtons(ButtonOptionsRandomCategory)].Visible = false;
                Buttons[htButtons(ButtonOptionsSingAll)].Visible = false;
                Buttons[htButtons(ButtonOptionsSingAllVisible)].Visible = false;
                Buttons[htButtons(ButtonOptionsOpenPlaylist)].Visible = false;
                Buttons[htButtons(ButtonOpenOptions)].Visible = true;
                Texts[htTexts(TextOptionsTitle)].Visible = false;
                Statics[htStatics(StaticOptionsBG)].Visible = false;
            }
        }

        public void CheckPlaylist()
        {
            if (Playlists[htPlaylists(Playlist)].ActivePlaylistID == -1)
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
            SongMenus[htSongMenus(SongMenu)].SetSmallView(false);
            _PlaylistActive = false;
            Playlists[htPlaylists(Playlist)].Selected = _PlaylistActive;
            SongMenus[htSongMenus(SongMenu)].SetActive(!_PlaylistActive);
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
                Playlists[htPlaylists(Playlist)].ScrollToBottom();
            }
            ToggleSongOptions(ESongOptionsView.None);
        }

        private void OpenAndAddPlaylistAction()
        {
            //Open a playlist and add song
            if (Playlists[htPlaylists(Playlist)].ActivePlaylistID != (SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistAdd)].Selection - 1))
            {
                //Check selected game-mode
                EGameMode gm;
                if (_AvailableGameModes.Count >= SelectSlides[htSelectSlides(SelectSlideOptionsMode)].Selection)
                    gm = _AvailableGameModes[SelectSlides[htSelectSlides(SelectSlideOptionsMode)].Selection];
                else
                    if (CSongs.VisibleSongs[SongMenus[htSongMenus(SongMenu)].GetSelectedSong()].IsDuet)
                        gm = GameModes.EGameMode.TR_GAMEMODE_DUET;
                    else
                        gm = GameModes.EGameMode.TR_GAMEMODE_NORMAL;
                
                //Check if Playlist really exists
                if (SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistAdd)].Selection - 1 < 0)
                    Playlists[htPlaylists(Playlist)].ActivePlaylistID = CPlaylists.NewPlaylist();
                else
                    Playlists[htPlaylists(Playlist)].ActivePlaylistID = SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistAdd)].Selection - 1;
                
                //Add song to playlist
                CPlaylists.Playlists[Playlists[htPlaylists(Playlist)].ActivePlaylistID].AddSong(CSongs.VisibleSongs[SongMenus[htSongMenus(SongMenu)].GetSelectedSong()].ID, gm);
                
                //Open playlist
                OpenPlaylist(Playlists[htPlaylists(Playlist)].ActivePlaylistID);

                SetSelectSlidePlaylistToCurrentPlaylist();
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
                        gm = GameModes.EGameMode.TR_GAMEMODE_DUET;
                    else
                        gm = GameModes.EGameMode.TR_GAMEMODE_NORMAL;
                
                //Create new playlist
                Playlists[htPlaylists(Playlist)].ActivePlaylistID = CPlaylists.NewPlaylist();
                
                //Add song to playlist
                CPlaylists.Playlists[Playlists[htPlaylists(Playlist)].ActivePlaylistID].AddSong(CSongs.VisibleSongs[SongMenus[htSongMenus(SongMenu)].GetSelectedSong()].ID, gm);
                
                //Open playlist
                OpenPlaylist(Playlists[htPlaylists(Playlist)].ActivePlaylistID);
                
                //Add new playlist to select-slide
                SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistAdd)].AddValue(CPlaylists.Playlists[Playlists[htPlaylists(Playlist)].ActivePlaylistID].PlaylistName);

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
                        gm = GameModes.EGameMode.TR_GAMEMODE_DUET;
                    else
                        gm = GameModes.EGameMode.TR_GAMEMODE_NORMAL;
                CPlaylists.Playlists[Playlists[htPlaylists(Playlist)].ActivePlaylistID].AddSong(CSongs.VisibleSongs[SongMenus[htSongMenus(SongMenu)].GetSelectedSong()].ID, gm);
                Playlists[htPlaylists(Playlist)].UpdatePlaylist();
                Playlists[htPlaylists(Playlist)].ScrollToBottom();
            }
            ToggleSongOptions(ESongOptionsView.Song);
        }

        private void SetSelectSlidePlaylistToCurrentPlaylist()
        {
            SelectSlides[htSelectSlides(SelectSlideOptionsPlaylistAdd)].Selection = Playlists[htPlaylists(Playlist)].ActivePlaylistID + 1;
        }
    }
}
