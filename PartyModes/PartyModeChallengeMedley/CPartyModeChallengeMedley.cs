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
using VocaluxeLib.Profile;
using VocaluxeLib.Songs;

[assembly: ComVisible(false)]

namespace VocaluxeLib.PartyModes.ChallengeMedley
{
    public enum ESongSource
    {
        TR_ALLSONGS,
        TR_CATEGORY,
        TR_PLAYLIST
    }

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

    public abstract class CPartyScreenChallengeMedley : CMenuParty
    {
        protected new CPartyModeChallengeMedley _PartyMode;

        public override void Init()
        {
            base.Init();
            _PartyMode = (CPartyModeChallengeMedley)base._PartyMode;
        }
    }

    // ReSharper disable ClassNeverInstantiated.Global
    public sealed class CPartyModeChallengeMedley : CPartyMode
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

        public int MinSongs
        {
            get { return 1; }
        }

        public int MaxSongs
        {
            get { return 10; }
        }

        private enum EStage
        {
            Config,
            Names,
            Main,
            Singing
        }

        public struct SData
        {
            public int NumPlayer;
            public int NumPlayerAtOnce;
            public int NumRounds;
            public List<int> ProfileIDs;
            public int NumSongs;
            public List<int> Songs;

            public CChallengeRounds Rounds;
            public List<CResultTableRow> ResultTable;
            public int[,] Results;

            public int CurrentRoundNr;

            public int[] CatSongIndices;

            public ESongSource SongSource;
            public int CategoryIndex;
            public int PlaylistID;
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

        public CPartyModeChallengeMedley(int id) : base(id)
        {
            GameData = new SData {NumPlayer = 4, NumPlayerAtOnce = 2, NumRounds = 12, CurrentRoundNr = 1, ProfileIDs = new List<int>(), NumSongs = 5, Songs = new List<int>(), SongSource = ESongSource.TR_ALLSONGS, CatSongIndices = null, Results = null};
        }

        public override void SetDefaults()
        {
            _Stage = EStage.Config;
            GameData.CurrentRoundNr = 1;
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
                    return _Screens["CPartyScreenChallengeMedleyConfig"];
                case EStage.Names:
                    return _Screens["CPartyScreenChallengeMedleyNames"];
                case EStage.Main:
                    return _Screens["CPartyScreenChallengeMedleyMain"];
                case EStage.Singing:
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
                    _Stage = EStage.Names;
                    break;
                case EStage.Names:
                    _Stage = EStage.Main;
                    CBase.Songs.ResetSongSung();
                    GameData.ResultTable = new List<CResultTableRow>();
                    GameData.Rounds = new CChallengeRounds(GameData.NumRounds, GameData.NumPlayer, GameData.NumPlayerAtOnce);
                    GameData.CurrentRoundNr = 1;
                    break;
                case EStage.Main:
                    _Stage = EStage.Singing;
                    if (!_StartNextRound())
                        CBase.Graphics.FadeTo(EScreen.Party);
                    break;
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
                case EStage.Names:
                    _Stage = EStage.Config;
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
            return _Screens["CPartyScreenChallengeMedleyConfig"];
        }
        public override SScreenSongOptions GetScreenSongOptions()
        {
            throw new ArgumentException("Not required!");
        }

        public override void OnSongChange(int songIndex, ref SScreenSongOptions screenSongOptions)
        {
            throw new ArgumentException("Not required!");
        }

        public override void OnCategoryChange(int categoryIndex, ref SScreenSongOptions screenSongOptions)
        {
            throw new ArgumentException("Not required!");
        }

        public override void SetSearchString(string searchString, bool visible)
        {
            throw new ArgumentException("Not required!");
        }

        public override void JokerUsed(int teamNr) { }

        public override void SongSelected(int songID) {}

        public override void LeavingHighscore()
        {
            GameData.CurrentRoundNr++;
            Next();
        }

        private bool _StartNextRound()
        {
            _SetTeamNames();
            GameData.CatSongIndices = null;

            return _PrepareGame();
        }

        private bool _PrepareGame()
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
            for (int i = 0; i < GameData.NumSongs; i++)
            {
                if (GameData.Songs.Count == 0)
                    _UpdateSongList();
                CBase.Game.AddSong(GameData.Songs[0], EGameMode.TR_GAMEMODE_MEDLEY);
                GameData.Songs.RemoveAt(0);
            }
            return true;
            #endregion SongQueue
        }

        private void _UpdateSongList()
        {
            if (GameData.Songs.Count > 0)
                return;

            switch (GameData.SongSource)
            {
                case ESongSource.TR_PLAYLIST:
                    for (int i = 0; i < CBase.Playlist.GetSongCount(GameData.PlaylistID); i++)
                    {
                        int id = CBase.Playlist.GetSong(GameData.PlaylistID, i).SongID;
                        if (CBase.Songs.GetSongByID(id).AvailableGameModes.Contains(EGameMode.TR_GAMEMODE_MEDLEY))
                            GameData.Songs.Add(id);
                    }
                    break;

                case ESongSource.TR_ALLSONGS:
                    ReadOnlyCollection<CSong> avSongs = CBase.Songs.GetSongs();
                    GameData.Songs.AddRange(avSongs.Where(song => song.AvailableGameModes.Contains(EGameMode.TR_GAMEMODE_MEDLEY)).Select(song => song.ID));
                    break;

                case ESongSource.TR_CATEGORY:
                    CBase.Songs.SetCategory(GameData.CategoryIndex);
                    avSongs = CBase.Songs.GetVisibleSongs();
                    GameData.Songs.AddRange(avSongs.Where(song => song.AvailableGameModes.Contains(EGameMode.TR_GAMEMODE_MEDLEY)).Select(song => song.ID));

                    CBase.Songs.SetCategory(-1);
                    break;
            }
            GameData.Songs.Shuffle();
        }


        private void _SetTeamNames()
        {
            CProfile[] profiles = CBase.Profiles.GetProfiles();

            if (profiles == null)
            {
                _ScreenSongOptions.Selection.TeamNames = new string[] {"foo", "bar"};
                return;
            }

            if (GameData.NumPlayerAtOnce < 1 || GameData.ProfileIDs.Count < GameData.NumPlayerAtOnce || profiles.Length < GameData.NumPlayerAtOnce)
            {
                _ScreenSongOptions.Selection.TeamNames = new string[] {"foo", "bar"};
                return;
            }

            _ScreenSongOptions.Selection.TeamNames = new string[GameData.NumPlayerAtOnce];
            CRound c = GameData.Rounds[GameData.CurrentRoundNr - 1];

            for (int i = 0; i < GameData.NumPlayerAtOnce; i++)
            {
                if (c != null)
                {
                    if (GameData.ProfileIDs[c.Players[i]] < profiles.Length)
                        _ScreenSongOptions.Selection.TeamNames[i] = profiles[GameData.ProfileIDs[c.Players[i]]].PlayerName;
                    else
                        _ScreenSongOptions.Selection.TeamNames[i] = "foobar";
                }
                else
                    _ScreenSongOptions.Selection.TeamNames[i] = "foobar";
            }
        }

        private void _UpdateScores()
        {
            if (GameData.ResultTable.Count == 0)
            {
                for (int i = 0; i < GameData.NumPlayer; i++)
                {
                    CResultTableRow row = new CResultTableRow {PlayerID = GameData.ProfileIDs[i], NumPlayed = 0, NumWon = 0, NumSingPoints = 0, NumGamePoints = 0};
                    GameData.ResultTable.Add(row);
                }

                GameData.Results = new int[GameData.NumRounds,GameData.NumPlayerAtOnce];
                for (int i = 0; i < GameData.NumRounds; i++)
                {
                    for (int j = 0; j < GameData.NumPlayerAtOnce; j++)
                        GameData.Results[i, j] = 0;
                }
            }
            
            CPoints gamePoints = CBase.Game.GetPoints();
            if (gamePoints == null || gamePoints.NumPlayer < GameData.NumPlayerAtOnce)
                return;

            SPlayer[] results = gamePoints.GetPlayer(0, GameData.NumPlayerAtOnce);
            if (results == null)
                return;

            if (results.Length < GameData.NumPlayerAtOnce)
                return;

            if (gamePoints.NumRounds == 0)
                return;

            for (int i = 1; i < gamePoints.NumRounds; i++)
            {
                SPlayer[] temp = gamePoints.GetPlayer(i, GameData.NumPlayerAtOnce);
                for (int p = 0; p < GameData.NumPlayerAtOnce; p++)
                    results[p].Points += temp[p].Points;
            }

            for (int j = 0; j < GameData.NumPlayerAtOnce; j++)
            {
                results[j].Points = results[j].Points / gamePoints.NumRounds;
                GameData.Results[GameData.CurrentRoundNr - 2, j] = (int)Math.Round(results[j].Points);
            }

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

                if (index != -1)
                {
                    CResultTableRow row = GameData.ResultTable[index];

                    row.NumPlayed++;
                    row.NumWon += points[i].Won;
                    row.NumSingPoints += points[i].SingPoints;
                    row.NumGamePoints += points[i].GamePoints;

                    GameData.ResultTable[index] = row;
                }
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