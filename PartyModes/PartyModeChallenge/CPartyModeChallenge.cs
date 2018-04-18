#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using VocaluxeLib.Game;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;

[assembly: ComVisible(false)]

namespace VocaluxeLib.PartyModes.Challenge
{
    public class CResultTableRow : IComparable
    {
        public int Position;
        public Guid PlayerID;
        public int NumPlayed;
        public int NumWon;
        public int NumSingPoints;
        public int NumGamePoints;

        public int CompareTo(object obj)
        {
            var row = obj as CResultTableRow;
            if (row != null)
            {
                int res = row.NumGamePoints.CompareTo(NumGamePoints);
                if (res == 0)
                {
                    res = row.NumSingPoints.CompareTo(NumSingPoints);
                    if (res == 0)
                        res = row.NumWon.CompareTo(NumWon);
                }
                return res;
            }

            throw new ArgumentException("object is not a ResultTableRow");
        }
    }

    public abstract class CPartyScreenChallenge : CMenuParty
    {
        protected new CPartyModeChallenge _PartyMode;

        public override void Init()
        {
            base.Init();
            _PartyMode = (CPartyModeChallenge)base._PartyMode;
        }
    }

    // ReSharper disable ClassNeverInstantiated.Global
    public sealed class CPartyModeChallenge : CPartyMode
        // ReSharper restore ClassNeverInstantiated.Global
    {
        public override int MinMics
        {
            get { return 1; }
        }
        public override int MaxMics
        {
            get { return CBase.Config.GetMaxNumMics(); }
        }
        public override int MinPlayers
        {
            get { return 1; }
        }
        public override int MaxPlayers
        {
            get { return 12; }
        }
        public override int MinTeams
        {
            get { return 1; }
        }
        public override int MaxTeams
        {
            get { return 1; }
        }
        public override int MinPlayersPerTeam
        {
            get { return MinPlayers; }
        }
        public override int MaxPlayersPerTeam
        {
            get { return MaxPlayers; }
        }

        private enum EStage
        {
            Config,
            Songs,
            Names,
            Main,
            SongSelection,
            Singing,
            MedleySinging
        }

        public struct SData
        {
            public int NumPlayer;
            public int NumPlayerAtOnce;
            public int NumRounds;
            public int NumJokers;
            public bool RefillJokers;
            public int[] Jokers;
            public List<Guid> ProfileIDs;

            public ESongSource SongSource;
            public ESongSorting Sorting;
            public int CategoryIndex;
            public int PlaylistID;

            public EGameMode GameMode;
            public int NumMedleySongs;

            public CChallengeRounds Rounds;
            public List<CResultTableRow> ResultTable;
            public int[,] Results;

            public int CurrentRoundNr;

            public List<int> Songs;
        }

        private struct SStats
        {
            public Guid ProfileID;
            public int SingPoints;
            public int GamePoints;
            public int Won;
        }

        public SData GameData;
        private EStage _Stage;

        public CPartyModeChallenge(int id) : base(id)
        {
            _ScreenSongOptions.Selection.RandomOnly = false;
            _ScreenSongOptions.Selection.PartyMode = true;
            _ScreenSongOptions.Selection.CategoryChangeAllowed = true;
            _ScreenSongOptions.Selection.NumJokers = new int[] {5, 5};
            _ScreenSongOptions.Selection.TeamNames = new string[] {"foo", "bar"};

            _ScreenSongOptions.Sorting.SearchString = String.Empty;
            _ScreenSongOptions.Sorting.SearchActive = false;
            _ScreenSongOptions.Sorting.DuetOptions = EDuetOptions.NoDuets;
            _ScreenSongOptions.Sorting.FilterPlaylistID = -1;

            GameData = new SData
            {
                NumPlayer = 4,
                NumPlayerAtOnce = 2,
                NumRounds = 12,
                NumJokers = 5,
                RefillJokers = true,
                CurrentRoundNr = 1,
                ProfileIDs = new List<Guid>(),
                Sorting = CBase.Config.GetSongSorting(),
                SongSource = ESongSource.TR_SONGSOURCE_ALLSONGS,
                PlaylistID = 0,
                CategoryIndex = 0,
                GameMode = EGameMode.TR_GAMEMODE_NORMAL,
                NumMedleySongs = 5,
                Results = null,
                Songs = new List<int>()
            };
        }

        public override void SetDefaults()
        {
            _Stage = EStage.Config;

            _ScreenSongOptions.Sorting.IgnoreArticles = CBase.Config.GetIgnoreArticles();
            _ScreenSongOptions.Sorting.SongSorting = CBase.Config.GetSongSorting();
            _ScreenSongOptions.Sorting.Tabs = EOffOn.TR_CONFIG_OFF;
            _ScreenSongOptions.Selection.SongIndex = -1;
            _ScreenSongOptions.Selection.CategoryIndex = -1;

            if (CBase.Config.GetTabs() == EOffOn.TR_CONFIG_ON && _ScreenSongOptions.Sorting.SongSorting != ESongSorting.TR_CONFIG_NONE)
                _ScreenSongOptions.Sorting.Tabs = EOffOn.TR_CONFIG_ON;

            GameData.ResultTable = new List<CResultTableRow>();
        }

        public override bool Init()
        {
            if (!base.Init())
                return false;

            SetDefaults();
            return true;
        }

        public override void UpdateGame()
        {

        }

        private IMenu _GetNextScreen()
        {
            switch (_Stage)
            {
                case EStage.Config:
                    return _Screens["CPartyScreenChallengeConfig"];
                case EStage.Songs:
                    return _Screens["CPartyScreenChallengeSongs"];
                case EStage.Names:
                    return _Screens["CPartyScreenChallengeNames"];
                case EStage.Main:
                    return _Screens["CPartyScreenChallengeMain"];
                case EStage.SongSelection:
                    return CBase.Graphics.GetScreen(EScreen.Song);
                case EStage.Singing:
                case EStage.MedleySinging:
                    return CBase.Graphics.GetScreen(EScreen.Sing);
                default:
                    throw new ArgumentException("Invalid stage: " + _Stage);
            }
        }

        private void _FadeToScreen()
        {
            CBase.Graphics.FadeTo(_GetNextScreen());
        }

        public void Next()
        {
            switch (_Stage)
            {
                case EStage.Config:
                    _Stage = EStage.Songs;
                    break;
                case EStage.Songs:
                    _Stage = EStage.Names;
                    break;
                case EStage.Names:
                    _Stage = EStage.Main;
                    CBase.Songs.ResetSongSung();
                    GameData.ResultTable = new List<CResultTableRow>();
                    GameData.Rounds = new CChallengeRounds(GameData.NumRounds, GameData.NumPlayer, GameData.NumPlayerAtOnce);
                    GameData.CurrentRoundNr = 1;
                    GameData.Jokers = null;
                    break;
                case EStage.Main:
                    if (GameData.GameMode == EGameMode.TR_GAMEMODE_MEDLEY)
                    {
                        _Stage = EStage.MedleySinging;
                        _PrepareMedleyRound();
                    }
                    else
                    {
                        _Stage = EStage.SongSelection;
                        _PrepareSongSelection();
                    }
                    break;
                case EStage.SongSelection:
                    _Stage = EStage.Singing;
                    break;
                case EStage.MedleySinging:
                case EStage.Singing:
                    _Stage = EStage.Main;
                    _UpdateScores();
                    break;
                default:
                    throw new ArgumentException("Invalid stage: " + _Stage);
            }
            _FadeToScreen();
        }

        public void Back()
        {
            switch (_Stage)
            {
                case EStage.Config:
                    CBase.Graphics.FadeTo(EScreen.Party);
                    return;
                case EStage.Songs:
                    _Stage = EStage.Config;
                    break;
                case EStage.Names:
                    _Stage = EStage.Songs;
                    break;
                case EStage.Main:
                    _Stage = EStage.Names;
                    break;
                default: // Rest is not allowed
                    throw new ArgumentException("Invalid stage: " + _Stage);
            }
            _FadeToScreen();
        }

        public override IMenu GetStartScreen()
        {
            return _Screens["CPartyScreenChallengeConfig"];
        }

        public override SScreenSongOptions GetScreenSongOptions()
        {
            return _ScreenSongOptions;
        }

        public override void OnSongChange(int songIndex, ref SScreenSongOptions screenSongOptions)
        {
            if (_ScreenSongOptions.Selection.SelectNextRandomSong && songIndex != -1)
                _ScreenSongOptions.Selection.SelectNextRandomSong = false;

            _ScreenSongOptions.Selection.SongIndex = songIndex;

            screenSongOptions = _ScreenSongOptions;
        }

        public override void OnCategoryChange(int categoryIndex, ref SScreenSongOptions screenSongOptions)
        {
            if (categoryIndex != -1 || CBase.Config.GetTabs() == EOffOn.TR_CONFIG_OFF)
            {
                //If category is selected or tabs off: only random song selection
                _ScreenSongOptions.Selection.SelectNextRandomSong = true;
                _ScreenSongOptions.Selection.RandomOnly = true;
            }
            else
            {
                //If no category is selected: let user choose category
                _ScreenSongOptions.Selection.SongIndex = -1;
                _ScreenSongOptions.Selection.RandomOnly = false;
            }

            _ScreenSongOptions.Selection.CategoryIndex = categoryIndex;

            screenSongOptions = _ScreenSongOptions;
        }

        public override void SetSearchString(string searchString, bool visible)
        {
            _ScreenSongOptions.Sorting.SearchString = searchString;
            _ScreenSongOptions.Sorting.SearchActive = visible;
        }

        public override void JokerUsed(int teamNr)
        {
            if (_ScreenSongOptions.Selection.NumJokers == null)
                return;

            if (teamNr >= _ScreenSongOptions.Selection.NumJokers.Length)
                return;

            if (!GameData.RefillJokers)
            {
                CRound round = GameData.Rounds[GameData.CurrentRoundNr - 1];
                GameData.Jokers[round.Players[teamNr]]--;
            }
            _ScreenSongOptions.Selection.NumJokers[teamNr]--;
            _ScreenSongOptions.Selection.RandomOnly = true;
            _ScreenSongOptions.Selection.CategoryChangeAllowed = false;
        }

        public override void SongSelected(int songID)
        {
            _PrepareRound(new int[] { songID });

            Next();
        }

        public override void LeavingHighscore()
        {
            //Remember sung songs, so they don't will be selected a second time
            for(int i = 0; i < CBase.Game.GetNumSongs(); i++)
                CBase.Songs.AddPartySongSung(CBase.Game.GetSong(i).ID);

            GameData.CurrentRoundNr++;

            Next();
        }

        /// <summary>
        /// Start a new medley round based on song configuration
        /// </summary>
        private void _PrepareMedleyRound()
        {
            //Select songs for medley
            int[] songIDs = new int[GameData.NumMedleySongs];
            for (int i = 0; i < GameData.NumMedleySongs; i++)
            {
                if (GameData.Songs.Count == 0)
                    _UpdateSongList();

                songIDs[i] = GameData.Songs[0];
                GameData.Songs.RemoveAt(0);
            }

            _PrepareRound(songIDs);
        }

        /// <summary>
        /// Setup options for song selection
        /// </summary>
        private void _PrepareSongSelection()
        {
            _ScreenSongOptions.Selection.RandomOnly = true;
            _ScreenSongOptions.Selection.SelectNextRandomSong = true;

            _ScreenSongOptions.Sorting.IgnoreArticles = CBase.Config.GetIgnoreArticles();

            switch (GameData.SongSource)
            {
                case ESongSource.TR_SONGSOURCE_ALLSONGS:
                    _ScreenSongOptions.Sorting.SongSorting = CBase.Config.GetSongSorting();
                    _ScreenSongOptions.Sorting.Tabs = CBase.Config.GetTabs();
                    _ScreenSongOptions.Sorting.FilterPlaylistID = -1;

                    _ScreenSongOptions.Selection.CategoryIndex = -1;
                    _ScreenSongOptions.Selection.CategoryChangeAllowed = true;
                    break;

                case ESongSource.TR_SONGSOURCE_CATEGORY:
                    _ScreenSongOptions.Sorting.SongSorting = GameData.Sorting;
                    _ScreenSongOptions.Sorting.Tabs = EOffOn.TR_CONFIG_ON;
                    _ScreenSongOptions.Sorting.FilterPlaylistID = -1;

                    _ScreenSongOptions.Selection.CategoryIndex = GameData.CategoryIndex;
                    _ScreenSongOptions.Selection.CategoryChangeAllowed = false;
                    break;

                case ESongSource.TR_SONGSOURCE_PLAYLIST:
                    _ScreenSongOptions.Sorting.SongSorting = CBase.Config.GetSongSorting();
                    _ScreenSongOptions.Sorting.Tabs = EOffOn.TR_CONFIG_OFF;
                    _ScreenSongOptions.Sorting.FilterPlaylistID = GameData.PlaylistID;

                    _ScreenSongOptions.Selection.CategoryChangeAllowed = false;
                    break;
            }

            _SetNumJokers();
            _SetTeamNames();
        }

        /// <summary>
        /// Prepare next game and fill song queue based on configuration and given songs.
        /// </summary>
        /// <param name="songIDs">Array of SongIDs that are selected</param>
        /// <returns>false, if something can't setup correctly</returns>
        private bool _PrepareRound(int[] songIDs)
        {
            //Reset game
            CBase.Game.Reset();
            CBase.Game.ClearSongs();

            #region PlayerNames
            CBase.Game.SetNumPlayer(GameData.NumPlayerAtOnce);
            SPlayer[] players = CBase.Game.GetPlayers();
            if (players == null || players.Length < GameData.NumPlayerAtOnce)
                return false;

            //Get current round
            CRound c = GameData.Rounds[GameData.CurrentRoundNr - 1];

            for (int i = 0; i < GameData.NumPlayerAtOnce; i++)
            {
                //try to fill with correct player data
                if (c != null)
                    players[i].ProfileID = GameData.ProfileIDs[c.Players[i]];
                else
                    players[i].ProfileID = Guid.Empty;
            }
            #endregion PlayerNames

            #region SongQueue
            //Add all songs with configure game mode to song queue
            for (int i = 0; i < songIDs.Length; i++)
                CBase.Game.AddSong(songIDs[i], GameData.GameMode);
            #endregion SongQueue

            return true;

        }

        private void _SetNumJokers()
        {
            int[] jokers = new int[GameData.NumPlayerAtOnce];
            if (GameData.RefillJokers)
            {                
                for (int i = 0; i < jokers.Length; i++)
                    jokers[i] = GameData.NumJokers;                
            }
            else
            {
                if (GameData.Jokers == null)
                {
                    GameData.Jokers = new int[GameData.NumPlayer];
                    for (int i = 0; i < GameData.ProfileIDs.Count; i++)
                    {
                        GameData.Jokers[i] = GameData.NumJokers;
                    }
                }
                CRound round = GameData.Rounds[GameData.CurrentRoundNr - 1];
                for (int i = 0; i < GameData.NumPlayerAtOnce; i++)
                {
                    jokers[i] = GameData.Jokers[round.Players[i]];
                }
            }
            _ScreenSongOptions.Selection.NumJokers = jokers;
        }

        private void _SetTeamNames()
        {
            if (GameData.NumPlayerAtOnce < 1 || GameData.ProfileIDs.Count < GameData.NumPlayerAtOnce)
            {
                _ScreenSongOptions.Selection.TeamNames = new string[] {"foo", "bar"};
                return;
            }

            _ScreenSongOptions.Selection.TeamNames = new string[GameData.NumPlayerAtOnce];
            CRound c = GameData.Rounds[GameData.CurrentRoundNr - 1];

            for (int i = 0; i < GameData.NumPlayerAtOnce; i++)
                _ScreenSongOptions.Selection.TeamNames[i] = CBase.Profiles.GetPlayerName(GameData.ProfileIDs[c.Players[i]]);
        }

        /// <summary>
        /// Fill song list based on song configuration. This is basically needed for singing medleys, 
        /// because we don't show song selection screen for these games.
        /// </summary>
        private void _UpdateSongList()
        {
            if (GameData.Songs.Count > 0)
                return;

            switch (GameData.SongSource)
            {
                case ESongSource.TR_SONGSOURCE_PLAYLIST:
                    for (int i = 0; i < CBase.Playlist.GetSongCount(GameData.PlaylistID); i++)
                    {
                        int id = CBase.Playlist.GetSong(GameData.PlaylistID, i).SongID;
                        if (CBase.Songs.GetSongByID(id).AvailableGameModes.Contains(EGameMode.TR_GAMEMODE_MEDLEY))
                            GameData.Songs.Add(id);
                    }
                    break;

                case ESongSource.TR_SONGSOURCE_ALLSONGS:
                    ReadOnlyCollection<CSong> avSongs = CBase.Songs.GetSongs();
                    GameData.Songs.AddRange(avSongs.Where(song => song.AvailableGameModes.Contains(EGameMode.TR_GAMEMODE_MEDLEY)).Select(song => song.ID));
                    break;

                case ESongSource.TR_SONGSOURCE_CATEGORY:
                    //Save old sorting to roll it back after getting songs for configured category
                    ESongSorting oldSorting = CBase.Config.GetSongSorting();
                    CBase.Songs.SortSongs(GameData.Sorting, EOffOn.TR_CONFIG_ON, CBase.Config.GetIgnoreArticles() ,String.Empty, EDuetOptions.NoDuets, -1);

                    CBase.Songs.SetCategory(GameData.CategoryIndex);
                    avSongs = CBase.Songs.GetVisibleSongs();
                    GameData.Songs.AddRange(avSongs.Where(song => song.AvailableGameModes.Contains(EGameMode.TR_GAMEMODE_MEDLEY)).Select(song => song.ID));

                    CBase.Songs.SetCategory(-1);
                    CBase.Songs.SortSongs(oldSorting, CBase.Config.GetTabs(), CBase.Config.GetIgnoreArticles(), String.Empty, EDuetOptions.NoDuets, -1);
                    break;
            }
            GameData.Songs.Shuffle();
        }

        private void _UpdateScores()
        {
            //Prepare results table
            if (GameData.ResultTable.Count == 0)
            {
                for (int i = 0; i < GameData.NumPlayer; i++)
                {
                    var row = new CResultTableRow { PlayerID = GameData.ProfileIDs[i], NumPlayed = 0, NumWon = 0, NumSingPoints = 0, NumGamePoints = 0 };
                    GameData.ResultTable.Add(row);
                }

                GameData.Results = new int[GameData.NumRounds, GameData.NumPlayerAtOnce];
                for (int i = 0; i < GameData.NumRounds; i++)
                {
                    for (int j = 0; j < GameData.NumPlayerAtOnce; j++)
                        GameData.Results[i, j] = 0;
                }
            }

            //Get points from game
            CPoints points = CBase.Game.GetPoints();
            SPlayer[] players = CBase.Game.GetPlayers();

            //Go over all rounds and sum up points
            for (int round = 0; round < points.NumRounds; round++)
            {
                SPlayer[] res = points.GetPlayer(round, GameData.NumPlayerAtOnce);

                if (res == null || res.Length < GameData.NumPlayerAtOnce)
                    return;

                for (int p = 0; p < GameData.NumPlayerAtOnce; p++)
                {
                    players[p].Points += res[p].Points;
                    players[p].PointsGoldenNotes += res[p].PointsGoldenNotes;
                    players[p].PointsLineBonus += res[p].PointsLineBonus;
                }
            }
            //Calculate average points
            for (int p = 0; p < GameData.NumPlayerAtOnce; p++)
            {
                players[p].Points /= points.NumRounds;
                players[p].PointsGoldenNotes /= points.NumRounds;
                players[p].PointsLineBonus /= points.NumRounds;

                //Save points in GameData
                GameData.Results[GameData.CurrentRoundNr - 2, p] = (int)Math.Round(players[p].Points);
            }

            List<SStats> stats = _GetPointsForPlayer(players);

            for (int i = 0; i < GameData.NumPlayerAtOnce; i++)
            {
                //Find matching row in results table
                int index = -1;
                for (int j = 0; j < GameData.ResultTable.Count; j++)
                {
                    if (stats[i].ProfileID == GameData.ResultTable[j].PlayerID)
                    {
                        index = j;
                        break;
                    }
                }

                if (index == -1)
                    continue;
                CResultTableRow row = GameData.ResultTable[index];

                //Update results entry
                row.NumPlayed++;
                row.NumWon += stats[i].Won;
                row.NumSingPoints += stats[i].SingPoints;
                row.NumGamePoints += stats[i].GamePoints;

                GameData.ResultTable[index] = row;
            }

            GameData.ResultTable.Sort();

            //Update position-number
            int pos = 1;
            int lastPoints = 0;
            int lastSingPoints = 0;
            foreach (CResultTableRow resultRow in GameData.ResultTable)
            {
                if (lastPoints > resultRow.NumGamePoints || lastSingPoints > resultRow.NumSingPoints)
                    pos++;
                resultRow.Position = pos;
                lastPoints = resultRow.NumGamePoints;
                lastSingPoints = resultRow.NumSingPoints;
            }
        }

        private List<SStats> _GetPointsForPlayer(SPlayer[] results)
        {
            var result = new List<SStats>();
            for (int i = 0; i < GameData.NumPlayerAtOnce; i++)
            {
                var stat = new SStats {ProfileID = results[i].ProfileID, SingPoints = (int)Math.Round(results[i].Points), Won = 0, GamePoints = 0};
                result.Add(stat);
            }

            result.Sort((s1, s2) => s1.SingPoints.CompareTo(s2.SingPoints));

            int current = result[result.Count - 1].SingPoints;
            int points = result.Count;
            bool wonset = false;

            for (int i = result.Count - 1; i >= 0; i--)
            {
                SStats res = result[i];

                if (i < result.Count - 1)
                {
                    if (current > res.SingPoints)
                    {
                        res.GamePoints = i * 2;
                        wonset = true;
                        points = res.GamePoints;
                    }
                    else
                    {
                        if (!wonset)
                            res.Won = 1;
                        res.GamePoints = points;
                    }
                }
                else
                {
                    res.GamePoints = i * 2;
                    res.Won = 1;
                }

                current = res.SingPoints;

                result[i] = res;
            }


            return result;
        }
    }
}