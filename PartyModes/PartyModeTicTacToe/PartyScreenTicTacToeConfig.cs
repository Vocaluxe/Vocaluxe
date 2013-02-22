using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Menu;

namespace Vocaluxe.PartyModes
{
    public class PartyScreenTicTacToeConfig : CMenuParty
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        const int ScreenVersion = 1;

        const string SelectSlideNumPlayerTeam1 = "SelectSlideNumPlayerTeam1";
        const string SelectSlideNumPlayerTeam2 = "SelectSlideNumPlayerTeam2";
        const string SelectSlideNumFields = "SelectSlideNumFields";
        const string SelectSlideSongSource = "SelectSlideSongSource";
        const string SelectSlidePlaylist = "SelectSlidePlaylist";
        const string SelectSlideCategory = "SelectSlideCategory";
        const string SelectSlideGameMode = "SelectSlideGameMode";
        const string ButtonNext = "ButtonNext";
        const string ButtonBack = "ButtonBack";

        private bool ConfigOk = true;

        DataFromScreen Data;

        public PartyScreenTicTacToeConfig()
        {
        }

        protected override void Init()
        {
            base.Init();

            _ThemeName = "PartyScreenTicTacToeConfig";
            _ThemeSelectSlides = new string[] { SelectSlideNumPlayerTeam1, SelectSlideNumPlayerTeam2, SelectSlideNumFields, SelectSlideSongSource, SelectSlidePlaylist, SelectSlideCategory, SelectSlideGameMode };
            _ThemeButtons = new string[] { ButtonNext, ButtonBack };
            _ScreenVersion = ScreenVersion;

            Data = new DataFromScreen();
            FromScreenConfig config = new FromScreenConfig();
            config.PlaylistID = 0;
            config.NumFields = 9;
            config.NumPlayerTeam1 = 2;
            config.NumPlayerTeam2 = 2;
            config.GameMode = EPartyGameMode.TR_GAMEMODE_NORMAL;
            config.CategoryID = 0;
            config.SongSource = ESongSource.TR_ALLSONGS;
            Data.ScreenConfig = config;
        }

        public override void LoadTheme(string XmlPath)
        {
			base.LoadTheme(XmlPath);
        }

        public override void DataToScreen(object ReceivedData)
        {
            DataToScreenConfig config = new DataToScreenConfig();

            try
            {
                config = (DataToScreenConfig)ReceivedData;
                Data.ScreenConfig.NumFields = config.NumFields;
                Data.ScreenConfig.NumPlayerTeam1 = config.NumPlayerTeam1;
                Data.ScreenConfig.NumPlayerTeam2 = config.NumPlayerTeam2;
                Data.ScreenConfig.PlaylistID = config.PlaylistID;
                Data.ScreenConfig.CategoryID = config.CategoryID;
                Data.ScreenConfig.SongSource = config.SongSource;
                Data.ScreenConfig.GameMode = config.GameMode;
            }
            catch (Exception e)
            {
                CBase.Log.LogError("Error in party mode screen TicTacToe config. Can't cast received data from game mode " + _ThemeName + ". " + e.Message);;
            }

        }

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            base.HandleInput(KeyEvent);

            if (KeyEvent.KeyPressed)
            {
             
            }
            else
            {
                switch (KeyEvent.Key)
                {
                    case Keys.Back:
                    case Keys.Escape:
                        Back();
                        break;

                    case Keys.Enter:
                        UpdateSlides();

                        if (Buttons[htButtons(ButtonBack)].Selected)
                            Back();

                        if (Buttons[htButtons(ButtonNext)].Selected)
                            Next();
                        break;

                    case Keys.Left:
                        UpdateSlides();
                        break;

                    case Keys.Right:
                        UpdateSlides();
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(MouseEvent MouseEvent)
        {
            base.HandleMouse(MouseEvent);

            if (MouseEvent.LB && IsMouseOver(MouseEvent))
            {
                UpdateSlides();
                if (Buttons[htButtons(ButtonBack)].Selected)
                    Back();

                if (Buttons[htButtons(ButtonNext)].Selected)
                    Next();
            }

            if (MouseEvent.RB)
            {
                Back();
            }

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            if (CBase.Config.GetMaxNumMics() >= 2)
                ConfigOk = true;

            FillSlides();
            UpdateSlides();
        }

        public override bool UpdateGame()
        {
            Buttons[htButtons(ButtonNext)].Visible = ConfigOk;
            return true;
        }

        public override bool Draw()
        {
            base.Draw();
            return true;
        }

        public override void OnClose()
        {
            base.OnClose();
        }

        private void FillSlides()
        {
            // build num player slide (min player ... max player);
            SelectSlides[htSelectSlides(SelectSlideNumPlayerTeam1)].Clear();
            for (int i = _PartyMode.GetMinPlayer()/2; i <= _PartyMode.GetMaxPlayer() / 2; i++)
            {
                SelectSlides[htSelectSlides(SelectSlideNumPlayerTeam1)].AddValue(i.ToString());
            }
            SelectSlides[htSelectSlides(SelectSlideNumPlayerTeam1)].Selection = Data.ScreenConfig.NumPlayerTeam1 - (_PartyMode.GetMinPlayer()/2);

            SelectSlides[htSelectSlides(SelectSlideNumPlayerTeam2)].Clear();
            for (int i = _PartyMode.GetMinPlayer()/2; i <= _PartyMode.GetMaxPlayer() / 2; i++)
            {
                SelectSlides[htSelectSlides(SelectSlideNumPlayerTeam2)].AddValue(i.ToString());
            }
            SelectSlides[htSelectSlides(SelectSlideNumPlayerTeam2)].Selection = Data.ScreenConfig.NumPlayerTeam2 -( _PartyMode.GetMinPlayer()/2);

            SelectSlides[htSelectSlides(SelectSlideNumFields)].Clear();
            SelectSlides[htSelectSlides(SelectSlideNumFields)].AddValue("9");
            SelectSlides[htSelectSlides(SelectSlideNumFields)].AddValue("16");
            SelectSlides[htSelectSlides(SelectSlideNumFields)].AddValue("25");
            if (Data.ScreenConfig.NumFields == 9)
                SelectSlides[htSelectSlides(SelectSlideNumFields)].Selection = 0;
            else if (Data.ScreenConfig.NumFields == 16)
                SelectSlides[htSelectSlides(SelectSlideNumFields)].Selection = 1;
            else if (Data.ScreenConfig.NumFields == 25)
                SelectSlides[htSelectSlides(SelectSlideNumFields)].Selection = 2;

            SelectSlides[htSelectSlides(SelectSlideSongSource)].Clear();
            SelectSlides[htSelectSlides(SelectSlideSongSource)].SetValues<ESongSource>((int)Data.ScreenConfig.SongSource);

            string[] _Playlists = CBase.Playlist.GetPlaylistNames();
            SelectSlides[htSelectSlides(SelectSlidePlaylist)].Clear();
            for (int i = 0; i < _Playlists.Length; i++)
            {
                string value = _Playlists[i] + " (" + CBase.Playlist.GetPlaylistSongCount(i) + " " + CBase.Language.Translate("TR_SONGS", _PartyModeID) + ")";
                SelectSlides[htSelectSlides(SelectSlidePlaylist)].AddValue(value);
            }
            SelectSlides[htSelectSlides(SelectSlidePlaylist)].Selection = Data.ScreenConfig.PlaylistID;
            SelectSlides[htSelectSlides(SelectSlidePlaylist)].Visible = Data.ScreenConfig.SongSource == ESongSource.TR_PLAYLIST;

            string[] _Categories = new string[CBase.Songs.GetNumCategories()];
            for (int i = 0; i < CBase.Songs.GetNumCategories(); i++ )
            {
                _Categories[i] = CBase.Songs.GetCategory(i).Name;
            }
            SelectSlides[htSelectSlides(SelectSlideCategory)].Clear();
            for (int i = 0; i < _Categories.Length; i++)
            {
                string value = _Categories[i] + " (" + CBase.Songs.NumSongsInCategory(i) + " " + CBase.Language.Translate("TR_SONGS", _PartyModeID) + ")";
                SelectSlides[htSelectSlides(SelectSlideCategory)].AddValue(value);
            }
            SelectSlides[htSelectSlides(SelectSlideCategory)].Selection = Data.ScreenConfig.CategoryID;
            SelectSlides[htSelectSlides(SelectSlideCategory)].Visible = Data.ScreenConfig.SongSource == ESongSource.TR_CATEGORY;

            SelectSlides[htSelectSlides(SelectSlideGameMode)].Visible = true;
            SelectSlides[htSelectSlides(SelectSlideGameMode)].SetValues<EPartyGameMode>((int)Data.ScreenConfig.GameMode);
        }

        private void UpdateSlides()
        {
            Data.ScreenConfig.NumPlayerTeam1 = SelectSlides[htSelectSlides(SelectSlideNumPlayerTeam1)].Selection + (_PartyMode.GetMinPlayer()/2);
            Data.ScreenConfig.NumPlayerTeam2 = SelectSlides[htSelectSlides(SelectSlideNumPlayerTeam2)].Selection + (_PartyMode.GetMinPlayer()/2);

            if (SelectSlides[htSelectSlides(SelectSlideNumFields)].Selection == 0)
                Data.ScreenConfig.NumFields = 9;
            else if (SelectSlides[htSelectSlides(SelectSlideNumFields)].Selection == 1)
                Data.ScreenConfig.NumFields = 16;
            else if (SelectSlides[htSelectSlides(SelectSlideNumFields)].Selection == 2)
                Data.ScreenConfig.NumFields = 25;

            Data.ScreenConfig.SongSource = (ESongSource)SelectSlides[htSelectSlides(SelectSlideSongSource)].Selection;
            Data.ScreenConfig.PlaylistID = SelectSlides[htSelectSlides(SelectSlidePlaylist)].Selection;
            Data.ScreenConfig.CategoryID = SelectSlides[htSelectSlides(SelectSlideCategory)].Selection;
            Data.ScreenConfig.GameMode = (EPartyGameMode)SelectSlides[htSelectSlides(SelectSlideGameMode)].Selection;

            EGameMode gm = EGameMode.TR_GAMEMODE_NORMAL;

            switch (Data.ScreenConfig.GameMode)
            {
                case EPartyGameMode.TR_GAMEMODE_NORMAL:
                    gm = EGameMode.TR_GAMEMODE_NORMAL;
                    break;

                case EPartyGameMode.TR_GAMEMODE_DUET:
                    gm = EGameMode.TR_GAMEMODE_DUET;
                    break;

                case EPartyGameMode.TR_GAMEMODE_SHORTSONG:
                    gm = EGameMode.TR_GAMEMODE_SHORTSONG;
                    break;
            }

            if (Data.ScreenConfig.SongSource == ESongSource.TR_PLAYLIST)
            {
                if (CBase.Playlist.GetNumPlaylists() > 0)
                    if (CBase.Playlist.GetPlaylistSongCount(Data.ScreenConfig.PlaylistID) > 0)
                    {
                        ConfigOk = false;
                        for (int i = 0; i < CBase.Playlist.GetPlaylistSongCount(Data.ScreenConfig.PlaylistID); i++)
                        {
                            int id = CBase.Playlist.GetPlaylistSong(Data.ScreenConfig.PlaylistID, i).SongID;
                            foreach (EGameMode mode in CBase.Songs.GetSongByID(id).AvailableGameModes)
                                if (mode == gm)
                                {
                                    ConfigOk = true;
                                    break;
                                }
                            if (ConfigOk)
                                break;
                        }
                    }
                    else
                        ConfigOk = false;
                else
                    ConfigOk = false;
            }
            if (Data.ScreenConfig.SongSource == ESongSource.TR_CATEGORY)
            {
                if (CBase.Songs.GetNumCategories() > 0)
                    if (CBase.Songs.NumSongsInCategory(Data.ScreenConfig.CategoryID) > 0)
                    {
                        CBase.Songs.SetCategory(Data.ScreenConfig.CategoryID);
                        ConfigOk = false;
                        for (int i = 0; i < CBase.Songs.NumSongsInCategory(Data.ScreenConfig.CategoryID); i++)
                        {
                            foreach (EGameMode mode in CBase.Songs.GetVisibleSong(i).AvailableGameModes)
                                if (mode == gm)
                                {
                                    ConfigOk = true;
                                    break;
                                }
                            if (ConfigOk)
                                break;
                        }
                        CBase.Songs.SetCategory(-1);
                    }
                    else
                        ConfigOk = false;
                else
                    ConfigOk = false;
            }
            if (Data.ScreenConfig.SongSource == ESongSource.TR_ALLSONGS)
            {
                if (CBase.Songs.GetNumSongs() > 0)
                {
                    for (int i = 0; i < CBase.Songs.GetNumSongs(); i++)
                    {
                        foreach (EGameMode mode in CBase.Songs.GetSongByID(i).AvailableGameModes)
                            if (mode == gm)
                            {
                                ConfigOk = true;
                                break;
                            }
                        if (ConfigOk)
                            break;
                    }
                }
                else
                    ConfigOk = false;
            }
            SelectSlides[htSelectSlides(SelectSlideCategory)].Visible = Data.ScreenConfig.SongSource == ESongSource.TR_CATEGORY;
            SelectSlides[htSelectSlides(SelectSlidePlaylist)].Visible = Data.ScreenConfig.SongSource == ESongSource.TR_PLAYLIST;
        }    

        private void Back()
        {
            FadeTo(EScreens.ScreenParty);
        }

        private void Next()
        {
            _PartyMode.DataFromScreen(_ThemeName, Data);
        }
    }
}
