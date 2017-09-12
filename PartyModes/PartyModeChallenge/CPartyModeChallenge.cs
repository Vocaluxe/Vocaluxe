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
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;

[assembly: ComVisible(false)]

namespace VocaluxeLib.PartyModes.Challenge
{
    public class CResultTableRow : IComparable
    {
        public int Position;
        public int PlayerID;
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
            public List<int> ProfileIDs;

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

            public int[] CatSongIndices;
            public List<int> Songs;
        }

        private struct SStats
        {
            public int ProfileID;
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
                ProfileIDs = new List<int>(),
                Sorting = CBase.Config.GetSongSorting(),
                SongSource = ESongSource.TR_SONGSOURCE_ALLSONGS,
                PlaylistID = 0,
                CategoryIndex = 0,
                GameMode = EGameMode.TR_GAMEMODE_NORMAL,
                NumMedleySongs = 5,
                CatSongIndices = null,
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

            if (CBase.Config.GetTabs() == EOffOn.TR_CONFIG_ON && _ScreenSongOptions.Sorting.SongSorting != ESongSorting.TR_CONFIG_NONE)
                _ScreenSongOptions.Sorting.Tabs = EOffOn.TR_CONFIG_ON;

            GameData.ResultTable = new List<CResultTableRow>();
        }

        public override bool Init()
        {
            if (!base.Init())
                return false;
            _Stage = EStage.Config;

            SetDefaults();
            return true;
        }

        public override void UpdateGame()
        {
            /*
            if (CBase.Songs.IsInCategory() || _ScreenSongOptions.Sorting.Tabs == EOffOn.TR_CONFIG_OFF)
                _ScreenSongOptions.Selection.RandomOnly = true;
            else
                _ScreenSongOptions.Selection.RandomOnly = false;*/
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
                case EStage.Singing:
                    return CBase.Graphics.GetScreen(EScreen.Song);
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
                        _Stage = EStage.MedleySinging;
                    else
                        _Stage = EStage.Singing;
                    _StartNextRound();
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
            switch(GameData.SongSource)
            {
                case ESongSource.TR_SONGSOURCE_ALLSONGS:
                    _ScreenSongOptions.Sorting.SongSorting = CBase.Config.GetSongSorting();
                    _ScreenSongOptions.Sorting.Tabs = CBase.Config.GetTabs();
                    _ScreenSongOptions.Sorting.FilterPlaylistID = -1;

                    _ScreenSongOptions.Selection.CategoryChangeAllowed = true;
                    break;

                case ESongSource.TR_SONGSOURCE_CATEGORY:
                    _ScreenSongOptions.Sorting.SongSorting = GameData.Sorting;
                    _ScreenSongOptions.Sorting.Tabs = EOffOn.TR_CONFIG_ON;
                    _ScreenSongOptions.Sorting.FilterPlaylistID = -1;

                    if (GameData.CatSongIndices == null)
                        _CreateCatSongIndices();

                    _ScreenSongOptions.Selection.SongIndex = GameData.CatSongIndices[GameData.CategoryIndex];

                    _ScreenSongOptions.Selection.CategoryChangeAllowed = false;
                    break;

                case ESongSource.TR_SONGSOURCE_PLAYLIST:
                    _ScreenSongOptions.Sorting.SongSorting = CBase.Config.GetSongSorting();
                    _ScreenSongOptions.Sorting.Tabs = EOffOn.TR_CONFIG_OFF;
                    _ScreenSongOptions.Sorting.FilterPlaylistID = GameData.PlaylistID;

                    _ScreenSongOptions.Selection.CategoryChangeAllowed = false;
                    break;
            }

            _ScreenSongOptions.Sorting.IgnoreArticles = CBase.Config.GetIgnoreArticles();

            return _ScreenSongOptions;
        }

        // ReSharper disable RedundantAssignment
        public override void OnSongChange(int songIndex, ref SScreenSongOptions screenSongOptions)
            // ReSharper restore RedundantAssignment
        {
            _ScreenSongOptions.Selection.SongIndex = -1;

            if (_ScreenSongOptions.Selection.SelectNextRandomSong && songIndex != -1)
            {
                _ScreenSongOptions.Selection.SelectNextRandomSong = false;
                _CreateCatSongIndices();

                if (GameData.CatSongIndices != null)
                {
                    if (GameData.CatSongIndices[CBase.Songs.GetCurrentCategoryIndex()] == -1)
                        GameData.CatSongIndices[CBase.Songs.GetCurrentCategoryIndex()] = songIndex;
                }
            }

            screenSongOptions = _ScreenSongOptions;
        }

        // ReSharper disable RedundantAssignment
        public override void OnCategoryChange(int categoryIndex, ref SScreenSongOptions screenSongOptions)
            // ReSharper restore RedundantAssignment
        {
            if (GameData.CatSongIndices != null && categoryIndex != -1)
            {
                if (GameData.CatSongIndices[categoryIndex] == -1)
                    _ScreenSongOptions.Selection.SelectNextRandomSong = true;
                else
                    _ScreenSongOptions.Selection.SongIndex = GameData.CatSongIndices[categoryIndex];
            }

            if (GameData.CatSongIndices == null && categoryIndex != -1)
                _ScreenSongOptions.Selection.SelectNextRandomSong = true;

            if (categoryIndex == -1)
                _ScreenSongOptions.Selection.RandomOnly = false;

            if (_ScreenSongOptions.Sorting.Tabs == EOffOn.TR_CONFIG_OFF || categoryIndex != -1)
                _ScreenSongOptions.Selection.RandomOnly = true;

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
            _PrepareGame(new int[] { songID });

            CBase.Graphics.FadeTo(EScreen.Sing);
        }

        public override void LeavingHighscore()
        {
            CBase.Songs.AddPartySongSung(CBase.Game.GetSong(0).ID);
            GameData.CurrentRoundNr++;
            Next();
        }

        private void _StartNextRound()
        {
            if (GameData.GameMode == EGameMode.TR_GAMEMODE_MEDLEY)
            {
                int[] songIDs = new int[GameData.NumMedleySongs];
                for (int i = 0; i < GameData.NumMedleySongs; i++)
                {
                    if (GameData.Songs.Count == 0)
                        _UpdateSongList();

                    songIDs[i] = GameData.Songs[0];
                    GameData.Songs.RemoveAt(0);
                }

                _PrepareGame(songIDs);
            }
            else
            {
                _ScreenSongOptions.Selection.RandomOnly = _ScreenSongOptions.Sorting.Tabs != EOffOn.TR_CONFIG_ON;
                _ScreenSongOptions.Selection.CategoryChangeAllowed = false;
                _SetNumJokers();
                _SetTeamNames();
                GameData.CatSongIndices = null;
            }
        }

        private bool _PrepareGame(int[] songIDs)
        {
            CBase.Game.Reset();
            CBase.Game.ClearSongs();

            #region PlayerNames
            CBase.Game.SetNumPlayer(GameData.NumPlayerAtOnce);
            SPlayer[] players = CBase.Game.GetPlayers();
            if (players == null)
                return false;

            if (players.Length < GameData.NumPlayerAtOnce)
                return false;

            CRound c = GameData.Rounds[GameData.CurrentRoundNr - 1];

            for (int i = 0; i < GameData.NumPlayerAtOnce; i++)
            {
                //try to fill with the right data
                if (c != null)
                    players[i].ProfileID = GameData.ProfileIDs[c.Players[i]];
                else
                    players[i].ProfileID = -1;
            }
            #endregion PlayerNames

            #region SongQueue
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
                    CBase.Songs.SetCategory(GameData.CategoryIndex);
                    avSongs = CBase.Songs.GetVisibleSongs();
                    GameData.Songs.AddRange(avSongs.Where(song => song.AvailableGameModes.Contains(EGameMode.TR_GAMEMODE_MEDLEY)).Select(song => song.ID));

                    CBase.Songs.SetCategory(-1);
                    break;
            }
            GameData.Songs.Shuffle();
        }

        private void _UpdateScores()
        {
            if (GameData.ResultTable.Count == 0)
            {
                for (int i = 0; i < GameData.NumPlayer; i++)
                {
                    var row = new CResultTableRow {PlayerID = GameData.ProfileIDs[i], NumPlayed = 0, NumWon = 0, NumSingPoints = 0, NumGamePoints = 0};
                    GameData.ResultTable.Add(row);
                }

                GameData.Results = new int[GameData.NumRounds,GameData.NumPlayerAtOnce];
                for (int i = 0; i < GameData.NumRounds; i++)
                {
                    for (int j = 0; j < GameData.NumPlayerAtOnce; j++)
                        GameData.Results[i, j] = 0;
                }
            }
            SPlayer[] results = CBase.Game.GetPlayers();
            if (results == null)
                return;

            if (results.Length < GameData.NumPlayerAtOnce)
                return;

            for (int j = 0; j < GameData.NumPlayerAtOnce; j++)
                GameData.Results[GameData.CurrentRoundNr - 2, j] = (int)Math.Round(results[j].Points);

            List<SStats> points = _GetPointsForPlayer(results);

            for (int i = 0; i < GameData.NumPlayerAtOnce; i++)
            {
                int index = -1;
                for (int j = 0; j < GameData.ResultTable.Count; j++)
                {
                    if (points[i].ProfileID == GameData.ResultTable[j].PlayerID)
                    {
                        index = j;
                        break;
                    }
                }

                if (index == -1)
                    continue;
                CResultTableRow row = GameData.ResultTable[index];

                row.NumPlayed++;
                row.NumWon += points[i].Won;
                row.NumSingPoints += points[i].SingPoints;
                row.NumGamePoints += points[i].GamePoints;

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

        private void _CreateCatSongIndices()
        {
            if (GameData.CatSongIndices == null && CBase.Songs.GetNumCategories() > 0 && _ScreenSongOptions.Sorting.Tabs == EOffOn.TR_CONFIG_ON)
            {
                GameData.CatSongIndices = new int[CBase.Songs.GetNumCategories()];
                for (int i = 0; i < GameData.CatSongIndices.Length; i++)
                    GameData.CatSongIndices[i] = -1;
            }

            if (CBase.Songs.GetNumCategories() == 0 || _ScreenSongOptions.Sorting.Tabs == EOffOn.TR_CONFIG_OFF)
                GameData.CatSongIndices = null;
        }
    }
}