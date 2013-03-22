using System;
using System.Windows.Forms;
using VocaluxeLib.Menu;

namespace VocaluxeLib.PartyModes.TicTacToe
{
    public class PartyScreenTicTacToeConfig : CMenuParty
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string SelectSlideNumPlayerTeam1 = "SelectSlideNumPlayerTeam1";
        private const string SelectSlideNumPlayerTeam2 = "SelectSlideNumPlayerTeam2";
        private const string SelectSlideNumFields = "SelectSlideNumFields";
        private const string SelectSlideSongSource = "SelectSlideSongSource";
        private const string SelectSlidePlaylist = "SelectSlidePlaylist";
        private const string SelectSlideCategory = "SelectSlideCategory";
        private const string SelectSlideGameMode = "SelectSlideGameMode";
        private const string ButtonNext = "ButtonNext";
        private const string ButtonBack = "ButtonBack";

        private bool ConfigOk = true;

        private DataFromScreen Data;

        public override void Init()
        {
            base.Init();

            _ThemeSelectSlides = new[]
                {SelectSlideNumPlayerTeam1, SelectSlideNumPlayerTeam2, SelectSlideNumFields, SelectSlideSongSource, SelectSlidePlaylist, SelectSlideCategory, SelectSlideGameMode};
            _ThemeButtons = new[] {ButtonNext, ButtonBack};

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
                CBase.Log.LogError("Error in party mode screen TicTacToe config. Can't cast received data from game mode " + ThemeName + ". " + e.Message);
            }
        }

        public override bool HandleInput(KeyEvent KeyEvent)
        {
            base.HandleInput(KeyEvent);

            if (KeyEvent.KeyPressed) {}
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

                        if (Buttons[ButtonBack].Selected)
                            Back();

                        if (Buttons[ButtonNext].Selected)
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
                if (Buttons[ButtonBack].Selected)
                    Back();

                if (Buttons[ButtonNext].Selected)
                    Next();
            }

            if (MouseEvent.RB)
                Back();

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
            Buttons[ButtonNext].Visible = ConfigOk;
            return true;
        }

        public override bool Draw()
        {
            base.Draw();
            return true;
        }

        private void FillSlides()
        {
            // build num player slide (min player ... max player);
            SelectSlides[SelectSlideNumPlayerTeam1].Clear();
            for (int i = _PartyMode.GetMinPlayer() / 2; i <= _PartyMode.GetMaxPlayer() / 2; i++)
                SelectSlides[SelectSlideNumPlayerTeam1].AddValue(i.ToString());
            SelectSlides[SelectSlideNumPlayerTeam1].Selection = Data.ScreenConfig.NumPlayerTeam1 - (_PartyMode.GetMinPlayer() / 2);

            SelectSlides[SelectSlideNumPlayerTeam2].Clear();
            for (int i = _PartyMode.GetMinPlayer() / 2; i <= _PartyMode.GetMaxPlayer() / 2; i++)
                SelectSlides[SelectSlideNumPlayerTeam2].AddValue(i.ToString());
            SelectSlides[SelectSlideNumPlayerTeam2].Selection = Data.ScreenConfig.NumPlayerTeam2 - (_PartyMode.GetMinPlayer() / 2);

            SelectSlides[SelectSlideNumFields].Clear();
            SelectSlides[SelectSlideNumFields].AddValue("9");
            SelectSlides[SelectSlideNumFields].AddValue("16");
            SelectSlides[SelectSlideNumFields].AddValue("25");
            if (Data.ScreenConfig.NumFields == 9)
                SelectSlides[SelectSlideNumFields].Selection = 0;
            else if (Data.ScreenConfig.NumFields == 16)
                SelectSlides[SelectSlideNumFields].Selection = 1;
            else if (Data.ScreenConfig.NumFields == 25)
                SelectSlides[SelectSlideNumFields].Selection = 2;

            SelectSlides[SelectSlideSongSource].Clear();
            SelectSlides[SelectSlideSongSource].SetValues<ESongSource>((int)Data.ScreenConfig.SongSource);

            string[] _Playlists = CBase.Playlist.GetPlaylistNames();
            SelectSlides[SelectSlidePlaylist].Clear();
            for (int i = 0; i < _Playlists.Length; i++)
            {
                string value = _Playlists[i] + " (" + CBase.Playlist.GetPlaylistSongCount(i) + " " + CBase.Language.Translate("TR_SONGS", _PartyModeID) + ")";
                SelectSlides[SelectSlidePlaylist].AddValue(value);
            }
            SelectSlides[SelectSlidePlaylist].Selection = Data.ScreenConfig.PlaylistID;
            SelectSlides[SelectSlidePlaylist].Visible = Data.ScreenConfig.SongSource == ESongSource.TR_PLAYLIST;

            string[] _Categories = new string[CBase.Songs.GetNumCategories()];
            for (int i = 0; i < CBase.Songs.GetNumCategories(); i++)
                _Categories[i] = CBase.Songs.GetCategory(i).Name;
            SelectSlides[SelectSlideCategory].Clear();
            for (int i = 0; i < _Categories.Length; i++)
            {
                string value = _Categories[i] + " (" + CBase.Songs.NumSongsInCategory(i) + " " + CBase.Language.Translate("TR_SONGS", _PartyModeID) + ")";
                SelectSlides[SelectSlideCategory].AddValue(value);
            }
            SelectSlides[SelectSlideCategory].Selection = Data.ScreenConfig.CategoryID;
            SelectSlides[SelectSlideCategory].Visible = Data.ScreenConfig.SongSource == ESongSource.TR_CATEGORY;

            SelectSlides[SelectSlideGameMode].Visible = true;
            SelectSlides[SelectSlideGameMode].SetValues<EPartyGameMode>((int)Data.ScreenConfig.GameMode);
        }

        private void UpdateSlides()
        {
            Data.ScreenConfig.NumPlayerTeam1 = SelectSlides[SelectSlideNumPlayerTeam1].Selection + (_PartyMode.GetMinPlayer() / 2);
            Data.ScreenConfig.NumPlayerTeam2 = SelectSlides[SelectSlideNumPlayerTeam2].Selection + (_PartyMode.GetMinPlayer() / 2);

            if (SelectSlides[SelectSlideNumFields].Selection == 0)
                Data.ScreenConfig.NumFields = 9;
            else if (SelectSlides[SelectSlideNumFields].Selection == 1)
                Data.ScreenConfig.NumFields = 16;
            else if (SelectSlides[SelectSlideNumFields].Selection == 2)
                Data.ScreenConfig.NumFields = 25;

            Data.ScreenConfig.SongSource = (ESongSource)SelectSlides[SelectSlideSongSource].Selection;
            Data.ScreenConfig.PlaylistID = SelectSlides[SelectSlidePlaylist].Selection;
            Data.ScreenConfig.CategoryID = SelectSlides[SelectSlideCategory].Selection;
            Data.ScreenConfig.GameMode = (EPartyGameMode)SelectSlides[SelectSlideGameMode].Selection;

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
                {
                    if (CBase.Playlist.GetPlaylistSongCount(Data.ScreenConfig.PlaylistID) > 0)
                    {
                        ConfigOk = false;
                        for (int i = 0; i < CBase.Playlist.GetPlaylistSongCount(Data.ScreenConfig.PlaylistID); i++)
                        {
                            int id = CBase.Playlist.GetPlaylistSong(Data.ScreenConfig.PlaylistID, i).SongID;
                            foreach (EGameMode mode in CBase.Songs.GetSongByID(id).AvailableGameModes)
                            {
                                if (mode == gm)
                                {
                                    ConfigOk = true;
                                    break;
                                }
                            }
                            if (ConfigOk)
                                break;
                        }
                    }
                    else
                        ConfigOk = false;
                }
                else
                    ConfigOk = false;
            }
            if (Data.ScreenConfig.SongSource == ESongSource.TR_CATEGORY)
            {
                if (CBase.Songs.GetNumCategories() > 0)
                {
                    if (CBase.Songs.NumSongsInCategory(Data.ScreenConfig.CategoryID) > 0)
                    {
                        CBase.Songs.SetCategory(Data.ScreenConfig.CategoryID);
                        ConfigOk = false;
                        for (int i = 0; i < CBase.Songs.NumSongsInCategory(Data.ScreenConfig.CategoryID); i++)
                        {
                            foreach (EGameMode mode in CBase.Songs.GetVisibleSong(i).AvailableGameModes)
                            {
                                if (mode == gm)
                                {
                                    ConfigOk = true;
                                    break;
                                }
                            }
                            if (ConfigOk)
                                break;
                        }
                        CBase.Songs.SetCategory(-1);
                    }
                    else
                        ConfigOk = false;
                }
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
                        {
                            if (mode == gm)
                            {
                                ConfigOk = true;
                                break;
                            }
                        }
                        if (ConfigOk)
                            break;
                    }
                }
                else
                    ConfigOk = false;
            }
            SelectSlides[SelectSlideCategory].Visible = Data.ScreenConfig.SongSource == ESongSource.TR_CATEGORY;
            SelectSlides[SelectSlidePlaylist].Visible = Data.ScreenConfig.SongSource == ESongSource.TR_PLAYLIST;
        }

        private void Back()
        {
            FadeTo(EScreens.ScreenParty);
        }

        private void Next()
        {
            _PartyMode.DataFromScreen(ThemeName, Data);
        }
    }
}