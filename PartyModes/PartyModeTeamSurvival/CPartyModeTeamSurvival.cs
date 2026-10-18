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
using System.Linq;
using System.Runtime.InteropServices;
using VocaluxeLib.Menu;
using VocaluxeLib.Utils;

[assembly: ComVisible(false)]

namespace VocaluxeLib.PartyModes.TeamSurvival
{
    public class CSurvivalPlayer
    {
        public Guid ProfileId;
        public int Team;
        public int StartHearts;
        public int Hearts;
        public int Jokers;
        public int DuelsInPass;

        public bool IsOut
        {
            get { return Hearts <= 0; }
        }
    }

    public class CSurvivalDuel
    {
        public CSurvivalPlayer[] Players = new CSurvivalPlayer[2];
        public int[] Scores = new int[2];
        public int Loser = -1;
    }

    public abstract class CPartyScreenTeamSurvival : CMenuParty
    {
        protected new CPartyModeTeamSurvival _PartyMode;

        public override void Init()
        {
            base.Init();
            _PartyMode = (CPartyModeTeamSurvival)base._PartyMode;
        }
    }

    public sealed class CPartyModeTeamSurvival : CPartyMode
    {
        public const int TeamCount = 2;
        public const int MaxHearts = 5;
        public const int MaxJokers = 3;
        public const int MaxPlayersInTeam = 8;

        public override int MinMics
        {
            get { return TeamCount; }
        }
        public override int MaxMics
        {
            get { return CBase.Config.GetMaxNumMics(); }
        }
        public override int MinPlayers
        {
            get { return TeamCount; }
        }
        public override int MaxPlayers
        {
            get { return TeamCount * MaxPlayersInTeam; }
        }
        public override int MinTeams
        {
            get { return TeamCount; }
        }
        public override int MaxTeams
        {
            get { return TeamCount; }
        }
        public override int MinPlayersPerTeam
        {
            get { return 1; }
        }
        public override int MaxPlayersPerTeam
        {
            get { return MaxPlayersInTeam; }
        }

        private enum EStage
        {
            Config,
            Songs,
            Names,
            Hearts,
            Main,
            SongSelection,
            Singing,
            MedleySinging
        }

        public struct SData
        {
            public bool HeartsPerPlayer;
            public int NumHearts;
            public int NumJokers;
            public Dictionary<Guid, int> PlayerHearts;

            public int NumPlayerTeam1;
            public int NumPlayerTeam2;
            public List<Guid> ProfileIdsTeam1;
            public List<Guid> ProfileIdsTeam2;

            public ESongSource SongSource;
            public ESongSorting Sorting;
            public int CategoryIndex;
            public int PlaylistId;

            public EGameMode GameMode;
            public int NumMedleySongs;

            public List<CSurvivalPlayer> Players;
            public List<CSurvivalDuel> PassDuels;
            public int PassNr;
            public int DuelIndex;
            public int NumDuelsPlayed;
            public bool IsRematch;
            public CSurvivalDuel LastDecidedDuel;
            public int WinnerTeam;

            public List<int> Songs;
        }

        public SData GameData;
        private EStage _Stage;

        public CPartyModeTeamSurvival(int id) : base(id)
        {
            _ScreenSongOptions.Selection.RandomOnly = false;
            _ScreenSongOptions.Selection.PartyMode = true;
            _ScreenSongOptions.Selection.CategoryChangeAllowed = true;
            _ScreenSongOptions.Selection.NumJokers = new int[] { 1, 1 };
            _ScreenSongOptions.Selection.TeamNames = new string[] { "foo", "bar" };

            _ScreenSongOptions.Sorting.SearchString = string.Empty;
            _ScreenSongOptions.Sorting.SearchActive = false;
            _ScreenSongOptions.Sorting.DuetOptions = EDuetOptions.NoDuets;
            _ScreenSongOptions.Sorting.FilterPlaylistId = -1;

            GameData = new SData
            {
                HeartsPerPlayer = false,
                NumHearts = 2,
                NumJokers = 1,
                PlayerHearts = new Dictionary<Guid, int>(),
                NumPlayerTeam1 = 2,
                NumPlayerTeam2 = 2,
                ProfileIdsTeam1 = new List<Guid>(),
                ProfileIdsTeam2 = new List<Guid>(),
                Sorting = CBase.Config.GetSongSorting(),
                SongSource = ESongSource.TR_SONGSOURCE_ALLSONGS,
                PlaylistId = 0,
                CategoryIndex = 0,
                GameMode = EGameMode.TR_GAMEMODE_NORMAL,
                NumMedleySongs = 5,
                Players = new List<CSurvivalPlayer>(),
                PassDuels = new List<CSurvivalDuel>(),
                WinnerTeam = -1,
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
            {
                _ScreenSongOptions.Sorting.Tabs = EOffOn.TR_CONFIG_ON;
            }
        }

        public override bool Init()
        {
            if (!base.Init())
            {
                return false;
            }

            SetDefaults();
            return true;
        }

        public override void UpdateGame() { }

        public bool IsFinished
        {
            get { return GameData.WinnerTeam >= 0; }
        }

        public CSurvivalDuel CurrentDuel
        {
            get
            {
                if (IsFinished || GameData.DuelIndex >= GameData.PassDuels.Count)
                {
                    return null;
                }

                return GameData.PassDuels[GameData.DuelIndex];
            }
        }

        public List<CSurvivalPlayer> GetTeam(int team)
        {
            return GameData.Players.Where(player => player.Team == team).ToList();
        }

        public int GetStartHearts(Guid profileId)
        {
            int hearts;
            if (GameData.HeartsPerPlayer && GameData.PlayerHearts.TryGetValue(profileId, out hearts))
            {
                return hearts;
            }

            return GameData.NumHearts;
        }

        private IMenu _GetNextScreen()
        {
            switch (_Stage)
            {
                case EStage.Config:
                    return _Screens["CPartyScreenTeamSurvivalConfig"];
                case EStage.Songs:
                    return _Screens["CPartyScreenTeamSurvivalSongs"];
                case EStage.Names:
                    return _Screens["CPartyScreenTeamSurvivalNames"];
                case EStage.Hearts:
                    return _Screens["CPartyScreenTeamSurvivalHearts"];
                case EStage.Main:
                    return _Screens["CPartyScreenTeamSurvivalMain"];
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
                    if (GameData.HeartsPerPlayer)
                    {
                        _Stage = EStage.Hearts;
                    }
                    else
                    {
                        _StartGame();
                    }

                    break;
                case EStage.Hearts:
                    _StartGame();
                    break;
                case EStage.Main:
                    if (CurrentDuel == null)
                    {
                        return;
                    }

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
                case EStage.Hearts:
                    _Stage = EStage.Names;
                    break;
                case EStage.Main:
                    _Stage = GameData.HeartsPerPlayer ? EStage.Hearts : EStage.Names;
                    break;
                default:
                    throw new ArgumentException("Invalid stage: " + _Stage);
            }

            _FadeToScreen();
        }

        public override IMenu GetStartScreen()
        {
            return _Screens["CPartyScreenTeamSurvivalConfig"];
        }

        public override SScreenSongOptions GetScreenSongOptions()
        {
            return _ScreenSongOptions;
        }

        public override void OnSongChange(int songIndex, ref SScreenSongOptions screenSongOptions)
        {
            if (_ScreenSongOptions.Selection.SelectNextRandomSong && songIndex != -1)
            {
                _ScreenSongOptions.Selection.SelectNextRandomSong = false;
            }

            _ScreenSongOptions.Selection.SongIndex = songIndex;

            screenSongOptions = _ScreenSongOptions;
        }

        public override void OnCategoryChange(int categoryIndex, ref SScreenSongOptions screenSongOptions)
        {
            if (categoryIndex != -1 || CBase.Config.GetTabs() == EOffOn.TR_CONFIG_OFF)
            {
                _ScreenSongOptions.Selection.SelectNextRandomSong = true;
                _ScreenSongOptions.Selection.RandomOnly = true;
            }
            else
            {
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
            if (_ScreenSongOptions.Selection.NumJokers == null || teamNr >= _ScreenSongOptions.Selection.NumJokers.Length)
            {
                return;
            }

            var duel = CurrentDuel;
            if (duel != null && duel.Players[teamNr].Jokers > 0)
            {
                duel.Players[teamNr].Jokers--;
            }

            _ScreenSongOptions.Selection.NumJokers[teamNr]--;
            _ScreenSongOptions.Selection.RandomOnly = true;
            _ScreenSongOptions.Selection.CategoryChangeAllowed = false;
        }

        public override void SongSelected(int songId)
        {
            _PrepareRound(new int[] { songId });

            Next();
        }

        public override void LeavingHighscore()
        {
            for (var i = 0; i < CBase.Game.GetNumSongs(); i++)
            {
                CBase.Songs.AddPartySongSung(CBase.Game.GetSong(i).Id);
            }

            Next();
        }

        private void _StartGame()
        {
            _Stage = EStage.Main;
            CBase.Songs.ResetSongSung();

            GameData.Players = new List<CSurvivalPlayer>();
            var teams = new List<Guid>[] { GameData.ProfileIdsTeam1, GameData.ProfileIdsTeam2 };
            for (var team = 0; team < TeamCount; team++)
            {
                foreach (var profileId in teams[team])
                {
                    var hearts = GetStartHearts(profileId);
                    GameData.Players.Add(new CSurvivalPlayer { ProfileId = profileId, Team = team, StartHearts = hearts, Hearts = hearts, Jokers = GameData.NumJokers });
                }
            }

            GameData.PassNr = 0;
            GameData.NumDuelsPlayed = 0;
            GameData.IsRematch = false;
            GameData.LastDecidedDuel = null;
            GameData.WinnerTeam = -1;
            GameData.Songs = new List<int>();
            _StartPass();
        }

        private void _StartPass()
        {
            GameData.PassNr++;
            GameData.DuelIndex = 0;
            GameData.PassDuels = new List<CSurvivalDuel>();

            var teams = new List<CSurvivalPlayer>[TeamCount];
            var numDuels = 0;
            for (var team = 0; team < TeamCount; team++)
            {
                teams[team] = GetTeam(team).Where(player => !player.IsOut).ToList();
                teams[team].Shuffle();
                foreach (var player in teams[team])
                {
                    player.DuelsInPass = 0;
                }

                numDuels = Math.Max(numDuels, teams[team].Count);
            }

            for (var i = 0; i < numDuels; i++)
            {
                GameData.PassDuels.Add(new CSurvivalDuel());
            }

            for (var team = 0; team < TeamCount; team++)
            {
                var order = new List<CSurvivalPlayer>(teams[team]);
                while (order.Count < numDuels)
                {
                    var extra = new List<CSurvivalPlayer>(teams[team]);
                    extra.Shuffle();
                    order.AddRange(extra);
                }

                for (var i = 1; i < numDuels; i++)
                {
                    if (order[i] != order[i - 1] || teams[team].Count < 2)
                    {
                        continue;
                    }

                    for (var j = i + 1; j < order.Count; j++)
                    {
                        if (order[j] != order[i - 1] && (i + 1 >= numDuels || order[j] != order[i + 1]))
                        {
                            var swap = order[i];
                            order[i] = order[j];
                            order[j] = swap;
                            break;
                        }
                    }
                }

                for (var i = 0; i < numDuels; i++)
                {
                    GameData.PassDuels[i].Players[team] = order[i];
                }
            }
        }

        private CSurvivalPlayer _GetReplacement(int team)
        {
            var candidates = GetTeam(team).Where(player => !player.IsOut).ToList();
            if (candidates.Count == 0)
            {
                return null;
            }

            var minDuels = candidates.Min(player => player.DuelsInPass);
            candidates = candidates.Where(player => player.DuelsInPass == minDuels).ToList();
            return candidates[CBase.Game.GetRandom(candidates.Count)];
        }

        private void _ValidateCurrentDuel()
        {
            var duel = CurrentDuel;
            if (duel == null)
            {
                return;
            }

            for (var team = 0; team < TeamCount; team++)
            {
                if (duel.Players[team] == null || duel.Players[team].IsOut)
                {
                    duel.Players[team] = _GetReplacement(team);
                }
            }
        }

        private void _PrepareMedleyRound()
        {
            var songIds = new List<int>();
            for (var i = 0; i < GameData.NumMedleySongs; i++)
            {
                if (GameData.Songs.Count == 0)
                {
                    _UpdateSongList();
                }

                if (GameData.Songs.Count == 0)
                {
                    break;
                }

                songIds.Add(GameData.Songs[0]);
                GameData.Songs.RemoveAt(0);
            }

            _PrepareRound(songIds.ToArray());
        }

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
                    _ScreenSongOptions.Sorting.FilterPlaylistId = -1;

                    _ScreenSongOptions.Selection.SongIndex = -1;
                    _ScreenSongOptions.Selection.CategoryIndex = -1;
                    _ScreenSongOptions.Selection.CategoryChangeAllowed = true;
                    break;

                case ESongSource.TR_SONGSOURCE_CATEGORY:
                    _ScreenSongOptions.Sorting.SongSorting = GameData.Sorting;
                    _ScreenSongOptions.Sorting.Tabs = EOffOn.TR_CONFIG_ON;
                    _ScreenSongOptions.Sorting.FilterPlaylistId = -1;

                    _ScreenSongOptions.Selection.CategoryIndex = GameData.CategoryIndex;
                    _ScreenSongOptions.Selection.CategoryChangeAllowed = false;
                    break;

                case ESongSource.TR_SONGSOURCE_PLAYLIST:
                    _ScreenSongOptions.Sorting.SongSorting = CBase.Config.GetSongSorting();
                    _ScreenSongOptions.Sorting.Tabs = EOffOn.TR_CONFIG_OFF;
                    _ScreenSongOptions.Sorting.FilterPlaylistId = GameData.PlaylistId;

                    _ScreenSongOptions.Selection.CategoryChangeAllowed = false;
                    break;
            }

            var duel = CurrentDuel;
            _ScreenSongOptions.Selection.NumJokers = new int[] { duel.Players[0].Jokers, duel.Players[1].Jokers };
            _ScreenSongOptions.Selection.TeamNames = new string[]
            {
                CBase.Profiles.GetPlayerName(duel.Players[0].ProfileId),
                CBase.Profiles.GetPlayerName(duel.Players[1].ProfileId)
            };
        }

        private bool _PrepareRound(int[] songIds)
        {
            CBase.Game.Reset();
            CBase.Game.ClearSongs();

            CBase.Game.SetNumPlayer(TeamCount);
            var players = CBase.Game.GetPlayers();
            var duel = CurrentDuel;
            if (players == null || players.Length < TeamCount || duel == null)
            {
                return false;
            }

            for (var team = 0; team < TeamCount; team++)
            {
                players[team].ProfileId = duel.Players[team].ProfileId;
            }

            foreach (var songId in songIds)
            {
                var gameMode = GameData.GameMode;
                if (GameData.IsRematch && gameMode != EGameMode.TR_GAMEMODE_MEDLEY && CBase.Songs.GetSongById(songId).IsGameModeAvailable(EGameMode.TR_GAMEMODE_SHORTSONG))
                {
                    gameMode = EGameMode.TR_GAMEMODE_SHORTSONG;
                }

                CBase.Game.AddSong(songId, gameMode);
            }

            return true;
        }

        private void _UpdateSongList()
        {
            if (GameData.Songs.Count > 0)
            {
                return;
            }

            switch (GameData.SongSource)
            {
                case ESongSource.TR_SONGSOURCE_PLAYLIST:
                    for (var i = 0; i < CBase.Playlist.GetSongCount(GameData.PlaylistId); i++)
                    {
                        var id = CBase.Playlist.GetSong(GameData.PlaylistId, i).SongId;
                        if (CBase.Songs.GetSongById(id).IsGameModeAvailable(EGameMode.TR_GAMEMODE_MEDLEY))
                        {
                            GameData.Songs.Add(id);
                        }
                    }

                    break;

                case ESongSource.TR_SONGSOURCE_ALLSONGS:
                    var avSongs = CBase.Songs.GetSongs();
                    GameData.Songs.AddRange(avSongs.Where(song => song.IsGameModeAvailable(EGameMode.TR_GAMEMODE_MEDLEY)).Select(song => song.Id));
                    break;

                case ESongSource.TR_SONGSOURCE_CATEGORY:
                    var oldSorting = CBase.Config.GetSongSorting();
                    CBase.Songs.SortSongs(GameData.Sorting, EOffOn.TR_CONFIG_ON, CBase.Config.GetIgnoreArticles(), string.Empty, EDuetOptions.NoDuets, -1);

                    CBase.Songs.SetCategory(GameData.CategoryIndex);
                    avSongs = CBase.Songs.GetVisibleSongs();
                    GameData.Songs.AddRange(avSongs.Where(song => song.IsGameModeAvailable(EGameMode.TR_GAMEMODE_MEDLEY)).Select(song => song.Id));

                    CBase.Songs.SetCategory(-1);
                    CBase.Songs.SortSongs(oldSorting, CBase.Config.GetTabs(), CBase.Config.GetIgnoreArticles(), string.Empty, EDuetOptions.NoDuets, -1);
                    break;
            }

            GameData.Songs.Shuffle();
        }

        private void _UpdateScores()
        {
            var duel = CurrentDuel;
            if (duel == null)
            {
                return;
            }

            var points = CBase.Game.GetPoints();
            if (points == null || points.NumRounds == 0)
            {
                return;
            }

            var scores = new double[TeamCount];
            for (var round = 0; round < points.NumRounds; round++)
            {
                var res = points.GetPlayer(round, TeamCount);
                if (res == null || res.Length < TeamCount)
                {
                    return;
                }

                for (var p = 0; p < TeamCount; p++)
                {
                    scores[p] += res[p].Points;
                }
            }

            for (var p = 0; p < TeamCount; p++)
            {
                duel.Scores[p] = (int)Math.Round(scores[p] / points.NumRounds);
            }

            GameData.NumDuelsPlayed++;

            if (duel.Scores[0] == duel.Scores[1])
            {
                GameData.IsRematch = true;
                return;
            }

            GameData.IsRematch = false;
            duel.Loser = duel.Scores[0] < duel.Scores[1] ? 0 : 1;
            duel.Players[duel.Loser].Hearts--;
            foreach (var player in duel.Players)
            {
                player.DuelsInPass++;
            }

            GameData.LastDecidedDuel = duel;

            for (var team = 0; team < TeamCount; team++)
            {
                if (GetTeam(team).All(player => player.IsOut))
                {
                    GameData.WinnerTeam = 1 - team;
                    return;
                }
            }

            GameData.DuelIndex++;
            if (GameData.DuelIndex >= GameData.PassDuels.Count)
            {
                _StartPass();
            }

            _ValidateCurrentDuel();
        }
    }
}
