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
using System.Runtime.InteropServices;
using VocaluxeLib.Menu;
using VocaluxeLib.Profile;

[assembly: ComVisible(false)]

namespace VocaluxeLib.PartyModes.TicTacToe
{
    public enum ESongSource
    {
        // ReSharper disable InconsistentNaming
        TR_ALLSONGS,
        TR_CATEGORY,
        TR_PLAYLIST
        // ReSharper restore InconsistentNaming
    }

    public enum EPartyGameMode
    {
        // ReSharper disable InconsistentNaming
        TR_GAMEMODE_NORMAL,
        TR_GAMEMODE_SHORTSONG,
        TR_GAMEMODE_DUET
        // ReSharper restore InconsistentNaming
    }

    public class CRound
    {
        public int SongID;
        public int SingerTeam1;
        public int SingerTeam2;
        public int PointsTeam1;
        public int PointsTeam2;
        public int Winner;
        public bool Finished;
    }

    #region Communication

    #region ToScreen
    public struct SDataToScreenConfig
    {
        public int NumPlayerTeam1;
        public int NumPlayerTeam2;
        public int NumFields;
        public ESongSource SongSource;
        public int CategoryID;
        public int PlaylistID;
        public EPartyGameMode GameMode;
    }

    public struct SDataToScreenNames
    {
        public int NumPlayerTeam1;
        public int NumPlayerTeam2;
        public List<int> ProfileIDsTeam1;
        public List<int> ProfileIDsTeam2;
    }

    public struct SDataToScreenMain
    {
        public int CurrentRoundNr;
        public int Team;
        public int NumFields;
        public List<CRound> Rounds;
        public List<int> Songs;
        public List<int> ProfileIDsTeam1;
        public List<int> ProfileIDsTeam2;
        public List<int> PlayerTeam1;
        public List<int> PlayerTeam2;
        public int[] NumJokerRandom;
        public int[] NumJokerRetry;
    }

    public struct SDataFromScreen
    {
        public SFromScreenConfig ScreenConfig;
        public SFromScreenNames ScreenNames;
        public SFromScreenMain ScreenMain;
    }

    public struct SFromScreenConfig
    {
        public int NumPlayerTeam1;
        public int NumPlayerTeam2;
        public int NumFields;
        public ESongSource SongSource;
        public int CategoryID;
        public int PlaylistID;
        public EPartyGameMode GameMode;
    }

    public struct SFromScreenNames
    {
        public bool FadeToConfig;
        public List<int> ProfileIDsTeam1;
        public List<int> ProfileIDsTeam2;
    }

    public struct SFromScreenMain
    {
        public bool FadeToSinging;
        public bool FadeToNameSelection;
        public List<int> PlayerTeam1;
        public List<int> PlayerTeam2;
        public List<CRound> Rounds;
        public int SingRoundNr;
        public List<int> Songs;
    }
    #endregion FromScreen

    #endregion Communication

    public sealed class CPartyModeTicTacToe : CPartyMode
    {
        private const int _MaxPlayer = 20;
        private const int _MinPlayer = 2;
        private const int _MaxTeams = 2;
        private const int _MinTeams = 2;
        private const int _NumFields = 9;

        private enum EStage
        {
            NotStarted,
            Config,
            Names,
            Main,
            Singing
        }

        private struct SData
        {
            public int NumPlayerTeam1;
            public int NumPlayerTeam2;
            public int NumFields;
            public int Team;
            public List<int> ProfileIDsTeam1;
            public List<int> ProfileIDsTeam2;
            public List<int> PlayerTeam1;
            public List<int> PlayerTeam2;

            public ESongSource SongSource;
            public int CategoryID;
            public int PlaylistID;

            public EPartyGameMode GameMode;

            public List<CRound> Rounds;
            public List<int> Songs;

            public int CurrentRoundNr;
            public int SingRoundNr;

            public int[] NumJokerRandom;
            public int[] NumJokerRetry;
        }

        private SDataToScreenConfig _ToScreenConfig;
        private SDataToScreenNames _ToScreenNames;
        private SDataToScreenMain _ToScreenMain;

        private SData _GameData;
        private EStage _Stage;

        public CPartyModeTicTacToe()
        {
            _ScreenSongOptions.Selection.RandomOnly = false;
            _ScreenSongOptions.Selection.PartyMode = true;
            _ScreenSongOptions.Selection.CategoryChangeAllowed = true;
            _ScreenSongOptions.Selection.NumJokers = new int[] {5, 5};
            _ScreenSongOptions.Selection.TeamNames = new string[] {"foo", "bar"};

            _ScreenSongOptions.Sorting.SearchString = String.Empty;
            _ScreenSongOptions.Sorting.SearchActive = false;
            _ScreenSongOptions.Sorting.DuetOptions = EDuetOptions.NoDuets;

            _Stage = EStage.NotStarted;

            _ToScreenConfig = new SDataToScreenConfig();
            _ToScreenNames = new SDataToScreenNames();
            _ToScreenMain = new SDataToScreenMain();

            _GameData = new SData
                {
                    NumFields = 9,
                    NumPlayerTeam1 = 2,
                    NumPlayerTeam2 = 2,
                    ProfileIDsTeam1 = new List<int>(),
                    ProfileIDsTeam2 = new List<int>(),
                    PlayerTeam1 = new List<int>(),
                    PlayerTeam2 = new List<int>(),
                    CurrentRoundNr = 0,
                    SingRoundNr = 0,
                    SongSource = ESongSource.TR_ALLSONGS,
                    PlaylistID = 0,
                    CategoryID = 0,
                    GameMode = EPartyGameMode.TR_GAMEMODE_NORMAL,
                    Rounds = new List<CRound>(),
                    Songs = new List<int>(),
                    NumJokerRandom = new int[2],
                    NumJokerRetry = new int[2]
                };
        }

        public override bool Init()
        {
            _Stage = EStage.NotStarted;

            _ScreenSongOptions.Sorting.IgnoreArticles = CBase.Config.GetIgnoreArticles();
            _ScreenSongOptions.Sorting.SongSorting = CBase.Config.GetSongSorting();
            _ScreenSongOptions.Sorting.Tabs = EOffOn.TR_CONFIG_OFF;
            _ScreenSongOptions.Selection.SongIndex = -1;

            if (CBase.Config.GetTabs() == EOffOn.TR_CONFIG_ON && _ScreenSongOptions.Sorting.SongSorting != ESongSorting.TR_CONFIG_NONE)
                _ScreenSongOptions.Sorting.Tabs = EOffOn.TR_CONFIG_ON;

            _ToScreenMain.Rounds = new List<CRound>();
            _ToScreenMain.Songs = new List<int>();
            _ToScreenMain.PlayerTeam1 = new List<int>();
            _ToScreenMain.PlayerTeam2 = new List<int>();
            _GameData.Songs = new List<int>();
            _GameData.Rounds = new List<CRound>();
            _GameData.PlayerTeam1 = new List<int>();
            _GameData.PlayerTeam2 = new List<int>();
            return true;
        }

        public override void DataFromScreen(string screenName, Object data)
        {
            var dataFrom = new SDataFromScreen();
            switch (screenName)
            {
                case "PartyScreenTicTacToeConfig":

                    try
                    {
                        dataFrom = (SDataFromScreen)data;
                        _GameData.NumPlayerTeam1 = dataFrom.ScreenConfig.NumPlayerTeam1;
                        _GameData.NumPlayerTeam2 = dataFrom.ScreenConfig.NumPlayerTeam2;
                        _GameData.NumFields = dataFrom.ScreenConfig.NumFields;
                        _GameData.SongSource = dataFrom.ScreenConfig.SongSource;
                        _GameData.CategoryID = dataFrom.ScreenConfig.CategoryID;
                        _GameData.PlaylistID = dataFrom.ScreenConfig.PlaylistID;
                        _GameData.GameMode = dataFrom.ScreenConfig.GameMode;

                        _Stage = EStage.Config;
                        CBase.Graphics.FadeTo(EScreens.ScreenPartyDummy);
                    }
                    catch (Exception e)
                    {
                        CBase.Log.LogError("Error in party mode TicTacToe. Can't cast received data from screen " + screenName + ". " + e.Message);
                    }
                    break;

                case "PartyScreenTicTacToeNames":
                    try
                    {
                        dataFrom = (SDataFromScreen)data;
                        if (dataFrom.ScreenNames.FadeToConfig)
                            _Stage = EStage.NotStarted;
                        else
                        {
                            _GameData.Team = CBase.Game.GetRandom(100) < 50 ? 0 : 1;
                            _GameData.ProfileIDsTeam1 = dataFrom.ScreenNames.ProfileIDsTeam1;
                            _GameData.ProfileIDsTeam2 = dataFrom.ScreenNames.ProfileIDsTeam2;
                            _Stage = EStage.Names;
                        }

                        CBase.Graphics.FadeTo(EScreens.ScreenPartyDummy);
                    }
                    catch (Exception e)
                    {
                        CBase.Log.LogError("Error in party mode TicTacToe. Can't cast received data from screen " + screenName + ". " + e.Message);
                    }
                    break;

                case "PartyScreenTicTacToeMain":
                    try
                    {
                        dataFrom = (SDataFromScreen)data;
                        if (dataFrom.ScreenMain.FadeToSinging)
                        {
                            _Stage = EStage.Singing;
                            _GameData.Rounds = dataFrom.ScreenMain.Rounds;
                            _GameData.SingRoundNr = dataFrom.ScreenMain.SingRoundNr;
                            _GameData.Songs = dataFrom.ScreenMain.Songs;
                            _GameData.PlayerTeam1 = dataFrom.ScreenMain.PlayerTeam1;
                            _GameData.PlayerTeam2 = dataFrom.ScreenMain.PlayerTeam2;
                        }
                        if (dataFrom.ScreenMain.FadeToNameSelection)
                            _Stage = EStage.Config;
                    }
                    catch (Exception e)
                    {
                        CBase.Log.LogError("Error in party mode TicTacToe. Can't cast received data from screen " + screenName + ". " + e.Message);
                    }


                    if (_Stage == EStage.Singing)
                        _StartRound(dataFrom.ScreenMain.SingRoundNr);
                    if (_Stage == EStage.Config)
                        CBase.Graphics.FadeTo(EScreens.ScreenPartyDummy);
                    break;

                default:
                    CBase.Log.LogError("Error in party mode TicTacToe. Wrong screen is sending: " + screenName);
                    break;
            }
        }

        public override void UpdateGame()
        {
            /*
            if (CBase.Songs.GetCurrentCategoryIndex() != -1 || _ScreenSongOptions.Sorting.Tabs == EOffOn.TR_CONFIG_OFF)
                _ScreenSongOptions.Selection.RandomOnly = true;
            else
                _ScreenSongOptions.Selection.RandomOnly = false;*/
        }

        public override CMenuParty GetNextPartyScreen(out EScreens alternativeScreen)
        {
            CMenuParty screen = null;
            alternativeScreen = EScreens.ScreenSong;

            switch (_Stage)
            {
                case EStage.NotStarted:
                    _Screens.TryGetValue("CPartyScreenTicTacToeConfig", out screen);
                    if (screen != null)
                    {
                        _ToScreenConfig.NumPlayerTeam1 = _GameData.NumPlayerTeam1;
                        _ToScreenConfig.NumPlayerTeam2 = _GameData.NumPlayerTeam2;
                        _ToScreenConfig.NumFields = _GameData.NumFields;
                        _ToScreenConfig.SongSource = _GameData.SongSource;
                        _ToScreenConfig.CategoryID = _GameData.CategoryID;
                        _ToScreenConfig.PlaylistID = _GameData.PlaylistID;
                        _ToScreenConfig.GameMode = _GameData.GameMode;
                        screen.DataToScreen(_ToScreenConfig);
                    }
                    break;
                case EStage.Config:
                    _Screens.TryGetValue("CPartyScreenTicTacToeNames", out screen);
                    if (screen != null)
                    {
                        _ToScreenNames.NumPlayerTeam1 = _GameData.NumPlayerTeam1;
                        _ToScreenNames.NumPlayerTeam2 = _GameData.NumPlayerTeam2;
                        _ToScreenNames.ProfileIDsTeam1 = _GameData.ProfileIDsTeam1;
                        _ToScreenNames.ProfileIDsTeam2 = _GameData.ProfileIDsTeam2;
                        screen.DataToScreen(_ToScreenNames);
                    }
                    break;
                case EStage.Names:
                    _Screens.TryGetValue("CPartyScreenTicTacToeMain", out screen);
                    if (screen != null)
                    {
                        _GameData.Team = _GameData.Team == 1 ? 0 : 1;
                        CBase.Songs.ResetSongSung();
                        _GameData.CurrentRoundNr = 1;
                        _ToScreenMain.CurrentRoundNr = 1;
                        _ToScreenMain.NumFields = _GameData.NumFields;
                        _ToScreenMain.ProfileIDsTeam1 = _GameData.ProfileIDsTeam1;
                        _ToScreenMain.ProfileIDsTeam2 = _GameData.ProfileIDsTeam2;
                        _CreateRounds();
                        _SetNumJokers();
                        _PreparePlayerList(0);
                        _PrepareSongList();
                        _ToScreenMain.Rounds = _GameData.Rounds;
                        _ToScreenMain.Songs = _GameData.Songs;
                        _ToScreenMain.PlayerTeam1 = _GameData.PlayerTeam1;
                        _ToScreenMain.PlayerTeam1 = _GameData.PlayerTeam2;
                        _ToScreenMain.NumJokerRandom = _GameData.NumJokerRandom;
                        _ToScreenMain.NumJokerRetry = _GameData.NumJokerRetry;
                        _ToScreenMain.Team = _GameData.Team;
                        screen.DataToScreen(_ToScreenMain);
                    }
                    break;
                case EStage.Main:
                    //nothing to do
                    break;
                case EStage.Singing:
                    _Screens.TryGetValue("CPartyScreenTicTacToeMain", out screen);
                    if (screen != null)
                    {
                        _GameData.Team = _GameData.Team == 1 ? 0 : 1;
                        _UpdateSongList();
                        _UpdatePlayerList();
                        _ToScreenMain.CurrentRoundNr = _GameData.CurrentRoundNr;
                        _ToScreenMain.NumFields = _GameData.NumFields;
                        _ToScreenMain.ProfileIDsTeam1 = _GameData.ProfileIDsTeam1;
                        _ToScreenMain.ProfileIDsTeam2 = _GameData.ProfileIDsTeam2;
                        _ToScreenMain.Rounds = _GameData.Rounds;
                        _ToScreenMain.Songs = _GameData.Songs;
                        _ToScreenMain.PlayerTeam1 = _GameData.PlayerTeam1;
                        _ToScreenMain.PlayerTeam2 = _GameData.PlayerTeam2;
                        _ToScreenMain.NumJokerRandom = _GameData.NumJokerRandom;
                        _ToScreenMain.NumJokerRetry = _GameData.NumJokerRetry;
                        _ToScreenMain.Team = _GameData.Team;
                        screen.DataToScreen(_ToScreenMain);
                    }
                    break;
            }

            return screen;
        }

        public override EScreens GetStartScreen()
        {
            return EScreens.ScreenPartyDummy;
        }

        public override EScreens GetMainScreen()
        {
            return EScreens.ScreenPartyDummy;
        }

        public override SScreenSongOptions GetScreenSongOptions()
        {
            _ScreenSongOptions.Sorting.SongSorting = CBase.Config.GetSongSorting();
            _ScreenSongOptions.Sorting.Tabs = CBase.Config.GetTabs();
            _ScreenSongOptions.Sorting.IgnoreArticles = CBase.Config.GetIgnoreArticles();

            return _ScreenSongOptions;
        }

        // ReSharper disable RedundantAssignment
        public override void OnSongChange(int songIndex, ref SScreenSongOptions screenSongOptions)
            // ReSharper restore RedundantAssignment
        {
            screenSongOptions = _ScreenSongOptions;
        }

        // ReSharper disable RedundantAssignment
        public override void OnCategoryChange(int categoryIndex, ref SScreenSongOptions screenSongOptions)
            // ReSharper restore RedundantAssignment
        {
            screenSongOptions = _ScreenSongOptions;
        }

        public override void SetSearchString(string searchString, bool visible)
        {
            _ScreenSongOptions.Sorting.SearchString = searchString;
            _ScreenSongOptions.Sorting.SearchActive = visible;
        }

        public override int GetMaxPlayer()
        {
            return _MaxPlayer;
        }

        public override int GetMinPlayer()
        {
            return _MinPlayer;
        }

        public override int GetMaxTeams()
        {
            return _MaxTeams;
        }

        public override int GetMinTeams()
        {
            return _MinTeams;
        }

        public override int GetMaxNumRounds()
        {
            return _NumFields;
        }

        public override void JokerUsed(int teamNr) {}

        public override void SongSelected(int songID)
        {
            var gm = EGameMode.TR_GAMEMODE_NORMAL;

            switch (_GameData.GameMode)
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
            CBase.Game.AddSong(songID, gm);

            CBase.Songs.AddPartySongSung(songID);
            CBase.Graphics.FadeTo(EScreens.ScreenSing);
        }

        public override void LeavingHighscore()
        {
            _UpdateScores();
            CBase.Graphics.FadeTo(EScreens.ScreenPartyDummy);
        }

        private void _CreateRounds()
        {
            _GameData.Rounds = new List<CRound>();
            for (int i = 0; i < _GameData.NumFields; i++)
            {
                var r = new CRound();
                _GameData.Rounds.Add(r);
            }
        }

        private void _PreparePlayerList(int team)
        {
            switch (team)
            {
                case 0:
                    {
                        _GameData.PlayerTeam1 = new List<int>();
                        _GameData.PlayerTeam2 = new List<int>();

                        //Prepare Player-IDs
                        var ids1 = new List<int>();
                        var ids2 = new List<int>();
                        //Add IDs to team-list
                        while (_GameData.PlayerTeam1.Count < _GameData.NumFields + _GameData.NumJokerRetry[0] &&
                               _GameData.PlayerTeam2.Count < _GameData.NumFields + _GameData.NumJokerRetry[1])
                        {
                            if (ids1.Count == 0)
                            {
                                for (int i = 0; i < _GameData.NumPlayerTeam1; i++)
                                    ids1.Add(i);
                            }
                            if (ids2.Count == 0)
                            {
                                for (int i = 0; i < _GameData.NumPlayerTeam2; i++)
                                    ids2.Add(i);
                            }
                            int num;
                            if (_GameData.PlayerTeam1.Count < _GameData.NumFields + _GameData.NumJokerRetry[0])
                            {
                                num = CBase.Game.GetRandom(ids1.Count);
                                if (num >= ids1.Count)
                                    num = ids1.Count - 1;
                                _GameData.PlayerTeam1.Add(ids1[num]);
                                ids1.RemoveAt(num);
                            }
                            if (_GameData.PlayerTeam2.Count < _GameData.NumFields + _GameData.NumJokerRetry[1])
                            {
                                num = CBase.Game.GetRandom(ids2.Count);
                                if (num >= ids2.Count)
                                    num = ids2.Count - 1;
                                _GameData.PlayerTeam2.Add(ids2[num]);
                                ids2.RemoveAt(num);
                            }
                        }
                    }
                    break;
                case 1:
                    {
                        //Prepare Player-IDs
                        var ids = new List<int>();
                        //Add IDs to team-list
                        while (_GameData.PlayerTeam1.Count < _GameData.NumFields + _GameData.NumJokerRetry[0] && ids.Count == 0)
                        {
                            if (ids.Count == 0)
                            {
                                for (int i = 0; i < _GameData.NumPlayerTeam1; i++)
                                    ids.Add(i);
                            }
                            if (_GameData.PlayerTeam1.Count < _GameData.NumFields + _GameData.NumJokerRetry[0])
                            {
                                int num = CBase.Game.GetRandom(ids.Count);
                                if (num >= ids.Count)
                                    num = ids.Count - 1;
                                _GameData.PlayerTeam1.Add(ids[num]);
                                ids.RemoveAt(num);
                            }
                        }
                    }
                    break;
                case 2:
                    {
                        //Prepare Player-IDs
                        var ids = new List<int>();
                        //Add IDs to team-list
                        while (_GameData.PlayerTeam2.Count < _GameData.NumFields + _GameData.NumJokerRetry[1] && ids.Count == 0)
                        {
                            if (ids.Count == 0)
                            {
                                for (int i = 0; i < _GameData.NumPlayerTeam2; i++)
                                    ids.Add(i);
                            }
                            if (_GameData.PlayerTeam2.Count < _GameData.NumFields + _GameData.NumJokerRetry[1])
                            {
                                int num = CBase.Game.GetRandom(ids.Count);
                                if (num >= ids.Count)
                                    num = ids.Count - 1;
                                _GameData.PlayerTeam2.Add(ids[num]);
                                ids.RemoveAt(num);
                            }
                        }
                    }
                    break;
            }
        }

        private void _PrepareSongList()
        {
            var gm = EGameMode.TR_GAMEMODE_NORMAL;

            switch (_GameData.GameMode)
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

            while (_GameData.Songs.Count < (_GameData.NumFields + _GameData.NumJokerRandom[0] + _GameData.NumJokerRandom[1]))
            {
                var songs = new List<int>();
                switch (_GameData.SongSource)
                {
                    case ESongSource.TR_PLAYLIST:
                        for (int i = 0; i < CBase.Playlist.GetPlaylistSongCount(_GameData.PlaylistID); i++)
                        {
                            int id = CBase.Playlist.GetPlaylistSong(_GameData.PlaylistID, i).SongID;
                            // ReSharper disable LoopCanBeConvertedToQuery
                            foreach (EGameMode mode in CBase.Songs.GetSongByID(id).AvailableGameModes)
                                // ReSharper restore LoopCanBeConvertedToQuery
                            {
                                if (mode == gm)
                                    songs.Add(id);
                            }
                        }
                        break;

                    case ESongSource.TR_ALLSONGS:
                        for (int i = 0; i < CBase.Songs.GetNumSongs(); i++)
                        {
                            // ReSharper disable LoopCanBeConvertedToQuery
                            foreach (EGameMode mode in CBase.Songs.GetSongByID(i).AvailableGameModes)
                                // ReSharper restore LoopCanBeConvertedToQuery
                            {
                                if (mode == gm)
                                    songs.Add(i);
                            }
                        }
                        break;

                    case ESongSource.TR_CATEGORY:
                        CBase.Songs.SetCategory(_GameData.CategoryID);
                        for (int i = 0; i < CBase.Songs.GetNumSongsVisible(); i++)
                        {
                            // ReSharper disable LoopCanBeConvertedToQuery
                            foreach (EGameMode mode in CBase.Songs.GetVisibleSong(i).AvailableGameModes)
                                // ReSharper restore LoopCanBeConvertedToQuery
                            {
                                if (mode == gm)
                                    songs.Add(CBase.Songs.GetVisibleSong(i).ID);
                            }
                        }
                        CBase.Songs.SetCategory(-1);
                        break;
                }
                while (songs.Count > 0)
                {
                    _GameData.Songs.Add(songs[CBase.Game.GetRandom(songs.Count - 1)]);
                    songs.Remove(_GameData.Songs[_GameData.Songs.Count - 1]);
                }
            }
        }

        private void _UpdatePlayerList()
        {
            if (_GameData.PlayerTeam1.Count == 0)
                _PreparePlayerList(1);
            if (_GameData.PlayerTeam2.Count == 0)
                _PreparePlayerList(2);
        }

        private void _UpdateSongList()
        {
            if (_GameData.Songs.Count == 0)
                _PrepareSongList();
        }

        private void _StartRound(int roundNr)
        {
            CBase.Game.Reset();
            CBase.Game.ClearSongs();

            CBase.Game.SetNumPlayer(2);

            SPlayer[] players = CBase.Game.GetPlayers();
            if (players == null)
                return;

            if (players.Length < 2)
                return;

            CRound r = _GameData.Rounds[roundNr];
            bool isDuet = CBase.Songs.GetSongByID(r.SongID).IsDuet;

            for (int i = 0; i < 2; i++)
            {
                //default values
                players[i].ProfileID = -1;
            }

            //try to fill with the right data
            if (r != null)
            {
                players[0].ProfileID = _GameData.ProfileIDsTeam1[r.SingerTeam1];
                if (isDuet)
                    players[0].LineNr = 0;

                players[1].ProfileID = _GameData.ProfileIDsTeam2[r.SingerTeam2];
                if (isDuet)
                    players[1].LineNr = 1;

                SongSelected(r.SongID);
            }
        }

        private void _SetNumJokers()
        {
            switch (_GameData.NumFields)
            {
                case 9:
                    _GameData.NumJokerRandom[0] = 1;
                    _GameData.NumJokerRandom[1] = 1;
                    _GameData.NumJokerRetry[0] = 0;
                    _GameData.NumJokerRetry[1] = 0;
                    break;

                case 16:
                    _GameData.NumJokerRandom[0] = 2;
                    _GameData.NumJokerRandom[1] = 2;
                    _GameData.NumJokerRetry[0] = 1;
                    _GameData.NumJokerRetry[1] = 1;
                    break;

                case 25:
                    _GameData.NumJokerRandom[0] = 3;
                    _GameData.NumJokerRandom[1] = 3;
                    _GameData.NumJokerRetry[0] = 2;
                    _GameData.NumJokerRetry[1] = 2;
                    break;
            }
        }

        private void _UpdateScores()
        {
            if (!_GameData.Rounds[_GameData.SingRoundNr].Finished)
                _GameData.CurrentRoundNr++;

            SPlayer[] results = CBase.Game.GetPlayers();
            if (results == null)
                return;

            if (results.Length < 2)
                return;

            _GameData.Rounds[_GameData.SingRoundNr].PointsTeam1 = (int)Math.Round(results[0].Points);
            _GameData.Rounds[_GameData.SingRoundNr].PointsTeam2 = (int)Math.Round(results[1].Points);
            _GameData.Rounds[_GameData.SingRoundNr].Finished = true;
            if (_GameData.Rounds[_GameData.SingRoundNr].PointsTeam1 < _GameData.Rounds[_GameData.SingRoundNr].PointsTeam2)
                _GameData.Rounds[_GameData.SingRoundNr].Winner = 2;
            else if (_GameData.Rounds[_GameData.SingRoundNr].PointsTeam1 > _GameData.Rounds[_GameData.SingRoundNr].PointsTeam2)
                _GameData.Rounds[_GameData.SingRoundNr].Winner = 1;
            else
            {
                _GameData.Rounds[_GameData.SingRoundNr].Finished = false;
                _GameData.CurrentRoundNr--;
            }
        }
    }
}