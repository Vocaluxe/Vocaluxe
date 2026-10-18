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

namespace VocaluxeLib.PartyModes.PartnerSwap
{
    public enum EDuetSongs
    {
        Mixed,
        DuetsOnly,
        NoDuets
    }

    public class CSwapPlayer
    {
        public Guid ProfileId;
        public int TotalPoints;
        public int NumSongs;
        public int Jokers;

        public int Average
        {
            get { return NumSongs == 0 ? 0 : (int)Math.Round((double)TotalPoints / NumSongs); }
        }
    }

    public class CSwapPair
    {
        public CSwapPlayer[] Players = new CSwapPlayer[2];
        public int[] Scores = new int[2];
        public int Bonus;
        public bool Sung;

        public int PairScore
        {
            get { return Scores[0] + Scores[1] + Bonus; }
        }
    }

    public abstract class CPartyScreenPartnerSwap : CMenuParty
    {
        protected new CPartyModePartnerSwap _PartyMode;

        public override void Init()
        {
            base.Init();
            _PartyMode = (CPartyModePartnerSwap)base._PartyMode;
        }
    }

    public sealed class CPartyModePartnerSwap : CPartyMode
    {
        public const int PlayersPerSong = 2;
        public const int MaxJokers = 3;
        public const int HarmonyRange = 500;
        public const int HarmonyBonusPoints = 500;

        public override int MinMics
        {
            get { return PlayersPerSong; }
        }
        public override int MaxMics
        {
            get { return CBase.Config.GetMaxNumMics(); }
        }
        public override int MinPlayers
        {
            get { return 3; }
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
            public int NumPasses;
            public bool HarmonyBonus;
            public EDuetSongs DuetSongs;
            public int NumJokers;
            public List<Guid> ProfileIds;

            public ESongSource SongSource;
            public ESongSorting Sorting;
            public int CategoryIndex;
            public int PlaylistId;

            public EGameMode GameMode;
            public int NumMedleySongs;

            public List<CSwapPlayer> Players;
            public List<List<CSwapPair>> Passes;
            public List<CSwapPlayer> PassBreaks;
            public int PassIndex;
            public int PairIndex;
            public int NumSongsSung;
            public CSwapPair BestPair;
            public CSwapPair LastPair;
            public List<CSwapPlayer> PreviousRanking;

            public List<int> Songs;
        }

        public SData GameData;
        private EStage _Stage;

        public CPartyModePartnerSwap(int id) : base(id)
        {
            _ScreenSongOptions.Selection.RandomOnly = false;
            _ScreenSongOptions.Selection.PartyMode = true;
            _ScreenSongOptions.Selection.CategoryChangeAllowed = true;
            _ScreenSongOptions.Selection.NumJokers = new int[] { 1, 1 };
            _ScreenSongOptions.Selection.TeamNames = new string[] { "foo", "bar" };

            _ScreenSongOptions.Sorting.SearchString = string.Empty;
            _ScreenSongOptions.Sorting.SearchActive = false;
            _ScreenSongOptions.Sorting.DuetOptions = EDuetOptions.All;
            _ScreenSongOptions.Sorting.FilterPlaylistId = -1;

            GameData = new SData
            {
                NumPlayer = 6,
                NumPasses = 3,
                HarmonyBonus = true,
                DuetSongs = EDuetSongs.Mixed,
                NumJokers = 1,
                ProfileIds = new List<Guid>(),
                Sorting = CBase.Config.GetSongSorting(),
                SongSource = ESongSource.TR_SONGSOURCE_ALLSONGS,
                PlaylistId = 0,
                CategoryIndex = 0,
                GameMode = EGameMode.TR_GAMEMODE_NORMAL,
                NumMedleySongs = 5,
                Players = new List<CSwapPlayer>(),
                Passes = new List<List<CSwapPair>>(),
                PassBreaks = new List<CSwapPlayer>(),
                PreviousRanking = new List<CSwapPlayer>(),
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

        public static int GetMaxPasses(int numPlayer)
        {
            return numPlayer % 2 == 0 ? numPlayer - 1 : numPlayer;
        }

        public bool IsFinished
        {
            get { return GameData.Passes.Count > 0 && GameData.PassIndex >= GameData.Passes.Count; }
        }

        public CSwapPair CurrentPair
        {
            get
            {
                if (IsFinished || GameData.Passes.Count == 0)
                {
                    return null;
                }

                return GameData.Passes[GameData.PassIndex][GameData.PairIndex];
            }
        }

        public CSwapPlayer CurrentBreak
        {
            get { return IsFinished || GameData.PassIndex >= GameData.PassBreaks.Count ? null : GameData.PassBreaks[GameData.PassIndex]; }
        }

        public List<CSwapPlayer> GetRanking()
        {
            return GameData.Players.OrderByDescending(player => player.Average).ThenByDescending(player => player.TotalPoints).ToList();
        }

        public int GetTimesTogether(CSwapPlayer player1, CSwapPlayer player2)
        {
            var count = 0;
            foreach (var pass in GameData.Passes)
            {
                foreach (var pair in pass)
                {
                    if (pair.Sung && pair.Players.Contains(player1) && pair.Players.Contains(player2))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private IMenu _GetNextScreen()
        {
            switch (_Stage)
            {
                case EStage.Config:
                    return _Screens["CPartyScreenPartnerSwapConfig"];
                case EStage.Songs:
                    return _Screens["CPartyScreenPartnerSwapSongs"];
                case EStage.Names:
                    return _Screens["CPartyScreenPartnerSwapNames"];
                case EStage.Main:
                    return _Screens["CPartyScreenPartnerSwapMain"];
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
                    _StartGame();
                    break;
                case EStage.Main:
                    if (CurrentPair == null)
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
                case EStage.Main:
                    _Stage = EStage.Names;
                    break;
                default:
                    throw new ArgumentException("Invalid stage: " + _Stage);
            }

            _FadeToScreen();
        }

        public override IMenu GetStartScreen()
        {
            return _Screens["CPartyScreenPartnerSwapConfig"];
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

            var pair = CurrentPair;
            if (pair != null && pair.Players[teamNr].Jokers > 0)
            {
                pair.Players[teamNr].Jokers--;
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
            CBase.Songs.ResetSongSung();

            GameData.Players = new List<CSwapPlayer>();
            foreach (var profileId in GameData.ProfileIds)
            {
                GameData.Players.Add(new CSwapPlayer { ProfileId = profileId, Jokers = GameData.NumJokers });
            }

            var slots = new List<CSwapPlayer>(GameData.Players);
            slots.Shuffle();
            if (slots.Count % 2 == 1)
            {
                slots.Add(null);
            }

            GameData.Passes = new List<List<CSwapPair>>();
            GameData.PassBreaks = new List<CSwapPlayer>();
            var numPasses = Math.Max(1, Math.Min(GameData.NumPasses, GetMaxPasses(GameData.Players.Count)));
            for (var pass = 0; pass < numPasses; pass++)
            {
                var pairs = new List<CSwapPair>();
                CSwapPlayer pause = null;
                for (var i = 0; i < slots.Count / 2; i++)
                {
                    var player1 = slots[i];
                    var player2 = slots[slots.Count - 1 - i];
                    if (player1 == null || player2 == null)
                    {
                        pause = player1 ?? player2;
                        continue;
                    }

                    var pair = new CSwapPair();
                    pair.Players[0] = player1;
                    pair.Players[1] = player2;
                    pairs.Add(pair);
                }

                pairs.Shuffle();
                GameData.Passes.Add(pairs);
                GameData.PassBreaks.Add(pause);

                var last = slots[slots.Count - 1];
                slots.RemoveAt(slots.Count - 1);
                slots.Insert(1, last);
            }

            GameData.PassIndex = 0;
            GameData.PairIndex = 0;
            GameData.NumSongsSung = 0;
            GameData.BestPair = null;
            GameData.LastPair = null;
            GameData.PreviousRanking = GetRanking();
            GameData.Songs = new List<int>();
        }

        private EDuetOptions _GetDuetOptions()
        {
            switch (GameData.DuetSongs)
            {
                case EDuetSongs.NoDuets:
                    return EDuetOptions.NoDuets;
                case EDuetSongs.DuetsOnly:
                    return CBase.Songs.GetSongs().Any(song => song.IsDuet) ? EDuetOptions.Duets : EDuetOptions.All;
                default:
                    return EDuetOptions.All;
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
            _ScreenSongOptions.Sorting.DuetOptions = _GetDuetOptions();

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

            var pair = CurrentPair;
            _ScreenSongOptions.Selection.NumJokers = new int[] { pair.Players[0].Jokers, pair.Players[1].Jokers };
            _ScreenSongOptions.Selection.TeamNames = new string[]
            {
                CBase.Profiles.GetPlayerName(pair.Players[0].ProfileId),
                CBase.Profiles.GetPlayerName(pair.Players[1].ProfileId)
            };
        }

        private bool _PrepareRound(int[] songIds)
        {
            CBase.Game.Reset();
            CBase.Game.ClearSongs();

            CBase.Game.SetNumPlayer(PlayersPerSong);
            var players = CBase.Game.GetPlayers();
            var pair = CurrentPair;
            if (players == null || players.Length < PlayersPerSong || pair == null)
            {
                return false;
            }

            for (var p = 0; p < PlayersPerSong; p++)
            {
                players[p].ProfileId = pair.Players[p].ProfileId;
                players[p].VoiceNr = p;
            }

            foreach (var songId in songIds)
            {
                var song = CBase.Songs.GetSongById(songId);
                var gameMode = GameData.GameMode;
                if (gameMode != EGameMode.TR_GAMEMODE_MEDLEY)
                {
                    if (song.IsDuet)
                    {
                        gameMode = EGameMode.TR_GAMEMODE_DUET;
                    }
                    else if (!song.IsGameModeAvailable(gameMode))
                    {
                        gameMode = EGameMode.TR_GAMEMODE_NORMAL;
                    }
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
                        var song = CBase.Songs.GetSongById(id);
                        if (song.IsGameModeAvailable(EGameMode.TR_GAMEMODE_MEDLEY) && !song.IsDuet)
                        {
                            GameData.Songs.Add(id);
                        }
                    }

                    break;

                case ESongSource.TR_SONGSOURCE_ALLSONGS:
                    var avSongs = CBase.Songs.GetSongs();
                    GameData.Songs.AddRange(avSongs.Where(song => song.IsGameModeAvailable(EGameMode.TR_GAMEMODE_MEDLEY) && !song.IsDuet).Select(song => song.Id));
                    break;

                case ESongSource.TR_SONGSOURCE_CATEGORY:
                    var oldSorting = CBase.Config.GetSongSorting();
                    CBase.Songs.SortSongs(GameData.Sorting, EOffOn.TR_CONFIG_ON, CBase.Config.GetIgnoreArticles(), string.Empty, EDuetOptions.NoDuets, -1);

                    CBase.Songs.SetCategory(GameData.CategoryIndex);
                    avSongs = CBase.Songs.GetVisibleSongs();
                    GameData.Songs.AddRange(avSongs.Where(song => song.IsGameModeAvailable(EGameMode.TR_GAMEMODE_MEDLEY) && !song.IsDuet).Select(song => song.Id));

                    CBase.Songs.SetCategory(-1);
                    CBase.Songs.SortSongs(oldSorting, CBase.Config.GetTabs(), CBase.Config.GetIgnoreArticles(), string.Empty, EDuetOptions.NoDuets, -1);
                    break;
            }

            GameData.Songs.Shuffle();
        }

        private void _UpdateScores()
        {
            var pair = CurrentPair;
            if (pair == null)
            {
                return;
            }

            var points = CBase.Game.GetPoints();
            if (points == null || points.NumRounds == 0)
            {
                return;
            }

            var scores = new double[PlayersPerSong];
            for (var round = 0; round < points.NumRounds; round++)
            {
                var res = points.GetPlayer(round, PlayersPerSong);
                if (res == null || res.Length < PlayersPerSong)
                {
                    return;
                }

                for (var p = 0; p < PlayersPerSong; p++)
                {
                    scores[p] += res[p].Points;
                }
            }

            GameData.PreviousRanking = GetRanking();

            for (var p = 0; p < PlayersPerSong; p++)
            {
                pair.Scores[p] = (int)Math.Round(scores[p] / points.NumRounds);
            }

            pair.Bonus = GameData.HarmonyBonus && Math.Abs(pair.Scores[0] - pair.Scores[1]) <= HarmonyRange ? HarmonyBonusPoints : 0;
            pair.Sung = true;

            foreach (var player in pair.Players)
            {
                player.TotalPoints += pair.PairScore;
                player.NumSongs++;
            }

            if (GameData.BestPair == null || pair.PairScore > GameData.BestPair.PairScore)
            {
                GameData.BestPair = pair;
            }

            GameData.LastPair = pair;
            GameData.NumSongsSung++;

            GameData.PairIndex++;
            if (GameData.PairIndex >= GameData.Passes[GameData.PassIndex].Count)
            {
                GameData.PairIndex = 0;
                GameData.PassIndex++;
            }
        }
    }
}
