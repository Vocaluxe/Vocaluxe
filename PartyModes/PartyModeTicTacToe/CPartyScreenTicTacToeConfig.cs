using System;
using System.Windows.Forms;
using VocaluxeLib.Menu;

namespace VocaluxeLib.PartyModes.TicTacToe
{
    public class CPartyScreenTicTacToeConfig : CMenuParty
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private const string _SelectSlideNumPlayerTeam1 = "SelectSlideNumPlayerTeam1";
        private const string _SelectSlideNumPlayerTeam2 = "SelectSlideNumPlayerTeam2";
        private const string _SelectSlideNumFields = "SelectSlideNumFields";
        private const string _SelectSlideSongSource = "SelectSlideSongSource";
        private const string _SelectSlidePlaylist = "SelectSlidePlaylist";
        private const string _SelectSlideCategory = "SelectSlideCategory";
        private const string _SelectSlideGameMode = "SelectSlideGameMode";
        private const string _ButtonNext = "ButtonNext";
        private const string _ButtonBack = "ButtonBack";

        private bool _ConfigOk = true;

        private SDataFromScreen _Data;

        public override void Init()
        {
            base.Init();

            _ThemeSelectSlides = new string[]
                {_SelectSlideNumPlayerTeam1, _SelectSlideNumPlayerTeam2, _SelectSlideNumFields, _SelectSlideSongSource, _SelectSlidePlaylist, _SelectSlideCategory, _SelectSlideGameMode};
            _ThemeButtons = new string[] {_ButtonNext, _ButtonBack};

            _Data = new SDataFromScreen();
            SFromScreenConfig config = new SFromScreenConfig();
            config.PlaylistID = 0;
            config.NumFields = 9;
            config.NumPlayerTeam1 = 2;
            config.NumPlayerTeam2 = 2;
            config.GameMode = EPartyGameMode.TR_GAMEMODE_NORMAL;
            config.CategoryID = 0;
            config.SongSource = ESongSource.TR_ALLSONGS;
            _Data.ScreenConfig = config;
        }

        public override void DataToScreen(object receivedData)
        {
            SDataToScreenConfig config = new SDataToScreenConfig();

            try
            {
                config = (SDataToScreenConfig)receivedData;
                _Data.ScreenConfig.NumFields = config.NumFields;
                _Data.ScreenConfig.NumPlayerTeam1 = config.NumPlayerTeam1;
                _Data.ScreenConfig.NumPlayerTeam2 = config.NumPlayerTeam2;
                _Data.ScreenConfig.PlaylistID = config.PlaylistID;
                _Data.ScreenConfig.CategoryID = config.CategoryID;
                _Data.ScreenConfig.SongSource = config.SongSource;
                _Data.ScreenConfig.GameMode = config.GameMode;
            }
            catch (Exception e)
            {
                CBase.Log.LogError("Error in party mode screen TicTacToe config. Can't cast received data from game mode " + ThemeName + ". " + e.Message);
            }
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            base.HandleInput(keyEvent);

            if (keyEvent.KeyPressed) {}
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Back:
                    case Keys.Escape:
                        _Back();
                        break;

                    case Keys.Enter:
                        _UpdateSlides();

                        if (Buttons[_ButtonBack].Selected)
                            _Back();

                        if (Buttons[_ButtonNext].Selected)
                            _Next();
                        break;

                    case Keys.Left:
                        _UpdateSlides();
                        break;

                    case Keys.Right:
                        _UpdateSlides();
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            base.HandleMouse(mouseEvent);

            if (mouseEvent.LB && IsMouseOver(mouseEvent))
            {
                _UpdateSlides();
                if (Buttons[_ButtonBack].Selected)
                    _Back();

                if (Buttons[_ButtonNext].Selected)
                    _Next();
            }

            if (mouseEvent.RB)
                _Back();

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();

            if (CBase.Config.GetMaxNumMics() >= 2)
                _ConfigOk = true;

            _FillSlides();
            _UpdateSlides();
        }

        public override bool UpdateGame()
        {
            Buttons[_ButtonNext].Visible = _ConfigOk;
            return true;
        }

        public override bool Draw()
        {
            base.Draw();
            return true;
        }

        private void _FillSlides()
        {
            // build num player slide (min player ... max player);
            SelectSlides[_SelectSlideNumPlayerTeam1].Clear();
            for (int i = _PartyMode.GetMinPlayer() / 2; i <= _PartyMode.GetMaxPlayer() / 2; i++)
                SelectSlides[_SelectSlideNumPlayerTeam1].AddValue(i.ToString());
            SelectSlides[_SelectSlideNumPlayerTeam1].Selection = _Data.ScreenConfig.NumPlayerTeam1 - (_PartyMode.GetMinPlayer() / 2);

            SelectSlides[_SelectSlideNumPlayerTeam2].Clear();
            for (int i = _PartyMode.GetMinPlayer() / 2; i <= _PartyMode.GetMaxPlayer() / 2; i++)
                SelectSlides[_SelectSlideNumPlayerTeam2].AddValue(i.ToString());
            SelectSlides[_SelectSlideNumPlayerTeam2].Selection = _Data.ScreenConfig.NumPlayerTeam2 - (_PartyMode.GetMinPlayer() / 2);

            SelectSlides[_SelectSlideNumFields].Clear();
            SelectSlides[_SelectSlideNumFields].AddValue("9");
            SelectSlides[_SelectSlideNumFields].AddValue("16");
            SelectSlides[_SelectSlideNumFields].AddValue("25");
            if (_Data.ScreenConfig.NumFields == 9)
                SelectSlides[_SelectSlideNumFields].Selection = 0;
            else if (_Data.ScreenConfig.NumFields == 16)
                SelectSlides[_SelectSlideNumFields].Selection = 1;
            else if (_Data.ScreenConfig.NumFields == 25)
                SelectSlides[_SelectSlideNumFields].Selection = 2;

            SelectSlides[_SelectSlideSongSource].Clear();
            SelectSlides[_SelectSlideSongSource].SetValues<ESongSource>((int)_Data.ScreenConfig.SongSource);

            string[] playlists = CBase.Playlist.GetPlaylistNames();
            SelectSlides[_SelectSlidePlaylist].Clear();
            for (int i = 0; i < playlists.Length; i++)
            {
                string value = playlists[i] + " (" + CBase.Playlist.GetPlaylistSongCount(i) + " " + CBase.Language.Translate("TR_SONGS", _PartyModeID) + ")";
                SelectSlides[_SelectSlidePlaylist].AddValue(value);
            }
            SelectSlides[_SelectSlidePlaylist].Selection = _Data.ScreenConfig.PlaylistID;
            SelectSlides[_SelectSlidePlaylist].Visible = _Data.ScreenConfig.SongSource == ESongSource.TR_PLAYLIST;

            string[] categories = new string[CBase.Songs.GetNumCategories()];
            for (int i = 0; i < CBase.Songs.GetNumCategories(); i++)
                categories[i] = CBase.Songs.GetCategory(i).Name;
            SelectSlides[_SelectSlideCategory].Clear();
            for (int i = 0; i < categories.Length; i++)
            {
                string value = categories[i] + " (" + CBase.Songs.NumSongsInCategory(i) + " " + CBase.Language.Translate("TR_SONGS", _PartyModeID) + ")";
                SelectSlides[_SelectSlideCategory].AddValue(value);
            }
            SelectSlides[_SelectSlideCategory].Selection = _Data.ScreenConfig.CategoryID;
            SelectSlides[_SelectSlideCategory].Visible = _Data.ScreenConfig.SongSource == ESongSource.TR_CATEGORY;

            SelectSlides[_SelectSlideGameMode].Visible = true;
            SelectSlides[_SelectSlideGameMode].SetValues<EPartyGameMode>((int)_Data.ScreenConfig.GameMode);
        }

        private void _UpdateSlides()
        {
            _Data.ScreenConfig.NumPlayerTeam1 = SelectSlides[_SelectSlideNumPlayerTeam1].Selection + (_PartyMode.GetMinPlayer() / 2);
            _Data.ScreenConfig.NumPlayerTeam2 = SelectSlides[_SelectSlideNumPlayerTeam2].Selection + (_PartyMode.GetMinPlayer() / 2);

            if (SelectSlides[_SelectSlideNumFields].Selection == 0)
                _Data.ScreenConfig.NumFields = 9;
            else if (SelectSlides[_SelectSlideNumFields].Selection == 1)
                _Data.ScreenConfig.NumFields = 16;
            else if (SelectSlides[_SelectSlideNumFields].Selection == 2)
                _Data.ScreenConfig.NumFields = 25;

            _Data.ScreenConfig.SongSource = (ESongSource)SelectSlides[_SelectSlideSongSource].Selection;
            _Data.ScreenConfig.PlaylistID = SelectSlides[_SelectSlidePlaylist].Selection;
            _Data.ScreenConfig.CategoryID = SelectSlides[_SelectSlideCategory].Selection;
            _Data.ScreenConfig.GameMode = (EPartyGameMode)SelectSlides[_SelectSlideGameMode].Selection;

            EGameMode gm = EGameMode.TR_GAMEMODE_NORMAL;

            switch (_Data.ScreenConfig.GameMode)
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

            if (_Data.ScreenConfig.SongSource == ESongSource.TR_PLAYLIST)
            {
                if (CBase.Playlist.GetNumPlaylists() > 0)
                {
                    if (CBase.Playlist.GetPlaylistSongCount(_Data.ScreenConfig.PlaylistID) > 0)
                    {
                        _ConfigOk = false;
                        for (int i = 0; i < CBase.Playlist.GetPlaylistSongCount(_Data.ScreenConfig.PlaylistID); i++)
                        {
                            int id = CBase.Playlist.GetPlaylistSong(_Data.ScreenConfig.PlaylistID, i).SongID;
                            foreach (EGameMode mode in CBase.Songs.GetSongByID(id).AvailableGameModes)
                            {
                                if (mode == gm)
                                {
                                    _ConfigOk = true;
                                    break;
                                }
                            }
                            if (_ConfigOk)
                                break;
                        }
                    }
                    else
                        _ConfigOk = false;
                }
                else
                    _ConfigOk = false;
            }
            if (_Data.ScreenConfig.SongSource == ESongSource.TR_CATEGORY)
            {
                if (CBase.Songs.GetNumCategories() > 0)
                {
                    if (CBase.Songs.NumSongsInCategory(_Data.ScreenConfig.CategoryID) > 0)
                    {
                        CBase.Songs.SetCategory(_Data.ScreenConfig.CategoryID);
                        _ConfigOk = false;
                        for (int i = 0; i < CBase.Songs.NumSongsInCategory(_Data.ScreenConfig.CategoryID); i++)
                        {
                            foreach (EGameMode mode in CBase.Songs.GetVisibleSong(i).AvailableGameModes)
                            {
                                if (mode == gm)
                                {
                                    _ConfigOk = true;
                                    break;
                                }
                            }
                            if (_ConfigOk)
                                break;
                        }
                        CBase.Songs.SetCategory(-1);
                    }
                    else
                        _ConfigOk = false;
                }
                else
                    _ConfigOk = false;
            }
            if (_Data.ScreenConfig.SongSource == ESongSource.TR_ALLSONGS)
            {
                if (CBase.Songs.GetNumSongs() > 0)
                {
                    for (int i = 0; i < CBase.Songs.GetNumSongs(); i++)
                    {
                        foreach (EGameMode mode in CBase.Songs.GetSongByID(i).AvailableGameModes)
                        {
                            if (mode == gm)
                            {
                                _ConfigOk = true;
                                break;
                            }
                        }
                        if (_ConfigOk)
                            break;
                    }
                }
                else
                    _ConfigOk = false;
            }
            SelectSlides[_SelectSlideCategory].Visible = _Data.ScreenConfig.SongSource == ESongSource.TR_CATEGORY;
            SelectSlides[_SelectSlidePlaylist].Visible = _Data.ScreenConfig.SongSource == ESongSource.TR_PLAYLIST;
        }

        private void _Back()
        {
            _FadeTo(EScreens.ScreenParty);
        }

        private void _Next()
        {
            _PartyMode.DataFromScreen(ThemeName, _Data);
        }
    }
}