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
using System.Linq;
using System.Windows.Forms;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;

namespace VocaluxeLib.PartyModes.TicTacToe
{
    // ReSharper disable UnusedMember.Global
    public class CPartyScreenTicTacToeConfig : CMenuParty
        // ReSharper restore UnusedMember.Global
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
                {
                    _SelectSlideNumPlayerTeam1, _SelectSlideNumPlayerTeam2, _SelectSlideNumFields, _SelectSlideSongSource, _SelectSlidePlaylist, _SelectSlideCategory,
                    _SelectSlideGameMode
                };
            _ThemeButtons = new string[] {_ButtonNext, _ButtonBack};

            _Data = new SDataFromScreen();
            var config = new SFromScreenConfig
                {
                    PlaylistID = 0,
                    NumFields = 9,
                    NumPlayerTeam1 = 2,
                    NumPlayerTeam2 = 2,
                    GameMode = EPartyGameMode.TR_GAMEMODE_NORMAL,
                    CategoryID = 0,
                    SongSource = ESongSource.TR_ALLSONGS
                };
            _Data.ScreenConfig = config;
        }

        public override void DataToScreen(object receivedData)
        {
            try
            {
                var config = (SDataToScreenConfig)receivedData;
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

                        if (_Buttons[_ButtonBack].Selected)
                            _Back();

                        if (_Buttons[_ButtonNext].Selected)
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

            if (mouseEvent.LB && _IsMouseOver(mouseEvent))
            {
                _UpdateSlides();
                if (_Buttons[_ButtonBack].Selected)
                    _Back();

                if (_Buttons[_ButtonNext].Selected)
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
            _Buttons[_ButtonNext].Visible = _ConfigOk;
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
            _SelectSlides[_SelectSlideNumPlayerTeam1].Clear();
            for (int i = _PartyMode.GetMinPlayer() / 2; i <= _PartyMode.GetMaxPlayer() / 2; i++)
                _SelectSlides[_SelectSlideNumPlayerTeam1].AddValue(i.ToString());
            _SelectSlides[_SelectSlideNumPlayerTeam1].Selection = _Data.ScreenConfig.NumPlayerTeam1 - (_PartyMode.GetMinPlayer() / 2);

            _SelectSlides[_SelectSlideNumPlayerTeam2].Clear();
            for (int i = _PartyMode.GetMinPlayer() / 2; i <= _PartyMode.GetMaxPlayer() / 2; i++)
                _SelectSlides[_SelectSlideNumPlayerTeam2].AddValue(i.ToString());
            _SelectSlides[_SelectSlideNumPlayerTeam2].Selection = _Data.ScreenConfig.NumPlayerTeam2 - (_PartyMode.GetMinPlayer() / 2);

            _SelectSlides[_SelectSlideNumFields].Clear();
            _SelectSlides[_SelectSlideNumFields].AddValue("9");
            _SelectSlides[_SelectSlideNumFields].AddValue("16");
            _SelectSlides[_SelectSlideNumFields].AddValue("25");
            if (_Data.ScreenConfig.NumFields == 9)
                _SelectSlides[_SelectSlideNumFields].Selection = 0;
            else if (_Data.ScreenConfig.NumFields == 16)
                _SelectSlides[_SelectSlideNumFields].Selection = 1;
            else if (_Data.ScreenConfig.NumFields == 25)
                _SelectSlides[_SelectSlideNumFields].Selection = 2;

            _SelectSlides[_SelectSlideSongSource].Clear();
            _SelectSlides[_SelectSlideSongSource].SetValues<ESongSource>((int)_Data.ScreenConfig.SongSource);

            List<string> playlists = CBase.Playlist.GetPlaylistNames();
            _SelectSlides[_SelectSlidePlaylist].Clear();
            for (int i = 0; i < playlists.Count; i++)
            {
                string value = playlists[i] + " (" + CBase.Playlist.GetPlaylistSongCount(i) + " " + CBase.Language.Translate("TR_SONGS", _PartyModeID) + ")";
                _SelectSlides[_SelectSlidePlaylist].AddValue(value);
            }
            _SelectSlides[_SelectSlidePlaylist].Selection = _Data.ScreenConfig.PlaylistID;
            _SelectSlides[_SelectSlidePlaylist].Visible = _Data.ScreenConfig.SongSource == ESongSource.TR_PLAYLIST;

            var categories = new string[CBase.Songs.GetNumCategories()];
            for (int i = 0; i < CBase.Songs.GetNumCategories(); i++)
                categories[i] = CBase.Songs.GetCategory(i).Name;
            _SelectSlides[_SelectSlideCategory].Clear();
            for (int i = 0; i < categories.Length; i++)
            {
                string value = categories[i] + " (" + CBase.Songs.GetNumSongsNotSungInCategory(i) + " " + CBase.Language.Translate("TR_SONGS", _PartyModeID) + ")";
                _SelectSlides[_SelectSlideCategory].AddValue(value);
            }
            _SelectSlides[_SelectSlideCategory].Selection = _Data.ScreenConfig.CategoryID;
            _SelectSlides[_SelectSlideCategory].Visible = _Data.ScreenConfig.SongSource == ESongSource.TR_CATEGORY;

            _SelectSlides[_SelectSlideGameMode].Visible = true;
            _SelectSlides[_SelectSlideGameMode].SetValues<EPartyGameMode>((int)_Data.ScreenConfig.GameMode);
        }

        private void _UpdateSlides()
        {
            _Data.ScreenConfig.NumPlayerTeam1 = _SelectSlides[_SelectSlideNumPlayerTeam1].Selection + (_PartyMode.GetMinPlayer() / 2);
            _Data.ScreenConfig.NumPlayerTeam2 = _SelectSlides[_SelectSlideNumPlayerTeam2].Selection + (_PartyMode.GetMinPlayer() / 2);

            if (_SelectSlides[_SelectSlideNumFields].Selection == 0)
                _Data.ScreenConfig.NumFields = 9;
            else if (_SelectSlides[_SelectSlideNumFields].Selection == 1)
                _Data.ScreenConfig.NumFields = 16;
            else if (_SelectSlides[_SelectSlideNumFields].Selection == 2)
                _Data.ScreenConfig.NumFields = 25;

            _Data.ScreenConfig.SongSource = (ESongSource)_SelectSlides[_SelectSlideSongSource].Selection;
            _Data.ScreenConfig.PlaylistID = _SelectSlides[_SelectSlidePlaylist].Selection;
            _Data.ScreenConfig.CategoryID = _SelectSlides[_SelectSlideCategory].Selection;
            _Data.ScreenConfig.GameMode = (EPartyGameMode)_SelectSlides[_SelectSlideGameMode].Selection;

            var gm = EGameMode.TR_GAMEMODE_NORMAL;

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
                            _ConfigOk = CBase.Songs.GetSongByID(id).AvailableGameModes.Any(mode => mode == gm);
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
                if (CBase.Songs.GetNumCategories() == 0)
                    _ConfigOk = false;
                else if (CBase.Songs.GetNumSongsNotSungInCategory(_Data.ScreenConfig.CategoryID) <= 0)
                    _ConfigOk = false;
                else
                {
                    CBase.Songs.SetCategory(_Data.ScreenConfig.CategoryID);
                    _ConfigOk = false;
                    foreach (CSong song in CBase.Songs.GetVisibleSongs())
                    {
                        _ConfigOk = song.AvailableGameModes.Any(mode => mode == gm);
                        if (_ConfigOk)
                            break;
                    }
                    CBase.Songs.SetCategory(-1);
                }
            }
            if (_Data.ScreenConfig.SongSource == ESongSource.TR_ALLSONGS)
            {
                if (CBase.Songs.GetNumSongs() > 0)
                {
                    for (int i = 0; i < CBase.Songs.GetNumSongs(); i++)
                    {
                        _ConfigOk = CBase.Songs.GetSongByID(i).AvailableGameModes.Any(mode => mode == gm);
                        if (_ConfigOk)
                            break;
                    }
                }
                else
                    _ConfigOk = false;
            }
            _SelectSlides[_SelectSlideCategory].Visible = _Data.ScreenConfig.SongSource == ESongSource.TR_CATEGORY;
            _SelectSlides[_SelectSlidePlaylist].Visible = _Data.ScreenConfig.SongSource == ESongSource.TR_PLAYLIST;
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