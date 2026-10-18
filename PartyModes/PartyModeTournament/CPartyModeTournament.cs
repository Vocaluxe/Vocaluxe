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

namespace VocaluxeLib.PartyModes.Tournament
{
    public class CTournamentMatch
    {
        public int Round;
        public int Index;
        public int Player1 = -1;
        public int Player2 = -1;
        public int Score1;
        public int Score2;
        public int Winner = -1;
        public bool Bye;

        public bool Played
        {
            get { return Winner >= 0; }
        }

        public bool Ready
        {
            get { return !Played && Player1 >= 0 && Player2 >= 0; }
        }

        public int WinnerPlayer
        {
            get
            {
                if (Winner == 0)
                {
                    return Player1;
                }

                return Winner == 1 ? Player2 : -1;
            }
        }

        public int LoserPlayer
        {
            get
            {
                if (Winner == 0)
                {
                    return Player2;
                }

                return Winner == 1 ? Player1 : -1;
            }
        }
    }

    public abstract class CPartyScreenTournament : CMenuParty
    {
        protected new CPartyModeTournament _PartyMode;

        public override void Init()
        {
            base.Init();
            _PartyMode = (CPartyModeTournament)base._PartyMode;
        }
    }

    public sealed class CPartyModeTournament : CPartyMode
    {
        public const int NumPlayerAtOnce = 2;
        public const int MaxJokers = 3;

        public override int MinMics
        {
            get { return NumPlayerAtOnce; }
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
            get { return 16; }
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
            public int NumJokers;
            public int[] Jokers;
            public List<Guid> ProfileIds;

            public ESongSource SongSource;
            public ESongSorting Sorting;
            public int CategoryIndex;
            public int PlaylistId;

            public EGameMode GameMode;
            public int NumMedleySongs;

            public List<CTournamentMatch[]> Rounds;
            public bool IsRematch;
            public int NumMatchesPlayed;
            public CTournamentMatch LastDecidedMatch;

            public List<int> Songs;
        }

        public SData GameData;
        private EStage _Stage;

        public CPartyModeTournament(int id) : base(id)
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
                NumPlayer = 8,
                NumJokers = 1,
                ProfileIds = new List<Guid>(),
                Sorting = CBase.Config.GetSongSorting(),
                SongSource = ESongSource.TR_SONGSOURCE_ALLSONGS,
                PlaylistId = 0,
                CategoryIndex = 0,
                GameMode = EGameMode.TR_GAMEMODE_NORMAL,
                NumMedleySongs = 5,
                Rounds = new List<CTournamentMatch[]>(),
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

            GameData.Rounds = new List<CTournamentMatch[]>();
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

        public int NumRounds
        {
            get { return GameData.Rounds.Count; }
        }

        public CTournamentMatch FinalMatch
        {
            get { return GameData.Rounds.Count > 0 ? GameData.Rounds[GameData.Rounds.Count - 1][0] : null; }
        }

        public bool IsFinished
        {
            get { return FinalMatch != null && FinalMatch.Played; }
        }

        public CTournamentMatch CurrentMatch
        {
            get
            {
                foreach (var round in GameData.Rounds)
                {
                    foreach (var match in round)
                    {
                        if (match.Ready)
                        {
                            return match;
                        }
                    }
                }

                return null;
            }
        }

        private IMenu _GetNextScreen()
        {
            switch (_Stage)
            {
                case EStage.Config:
                    return _Screens["CPartyScreenTournamentConfig"];
                case EStage.Songs:
                    return _Screens["CPartyScreenTournamentSongs"];
                case EStage.Names:
                    return _Screens["CPartyScreenTournamentNames"];
                case EStage.Main:
                    return _Screens["CPartyScreenTournamentMain"];
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
                    _CreateBracket();
                    break;
                case EStage.Main:
                    if (CurrentMatch == null)
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
            return _Screens["CPartyScreenTournamentConfig"];
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

            var match = CurrentMatch;
            if (match != null && GameData.Jokers != null)
            {
                var player = teamNr == 0 ? match.Player1 : match.Player2;
                if (GameData.Jokers[player] > 0)
                {
                    GameData.Jokers[player]--;
                }
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

        private void _CreateBracket()
        {
            var size = 4;
            while (size < GameData.NumPlayer)
            {
                size *= 2;
            }

            GameData.Rounds = new List<CTournamentMatch[]>();
            for (var count = size / 2; count >= 1; count /= 2)
            {
                var round = new CTournamentMatch[count];
                for (var i = 0; i < count; i++)
                {
                    round[i] = new CTournamentMatch { Round = GameData.Rounds.Count, Index = i };
                }

                GameData.Rounds.Add(round);
            }

            var firstRound = GameData.Rounds[0];
            var half = firstRound.Length / 2;
            var byeOrder = new List<int>();
            for (var i = 0; i < half; i++)
            {
                byeOrder.Add(i);
                byeOrder.Add(half + i);
            }

            var numByes = size - GameData.NumPlayer;
            for (var i = 0; i < numByes; i++)
            {
                firstRound[byeOrder[i]].Bye = true;
            }

            var players = Enumerable.Range(0, GameData.NumPlayer).ToList();
            players.Shuffle();
            var next = 0;
            foreach (var match in firstRound)
            {
                match.Player1 = players[next++];
                if (!match.Bye)
                {
                    match.Player2 = players[next++];
                }
            }

            foreach (var match in firstRound)
            {
                if (match.Bye)
                {
                    match.Winner = 0;
                    _Advance(match);
                }
            }

            GameData.Jokers = new int[GameData.NumPlayer];
            for (var i = 0; i < GameData.NumPlayer; i++)
            {
                GameData.Jokers[i] = GameData.NumJokers;
            }

            GameData.IsRematch = false;
            GameData.NumMatchesPlayed = 0;
            GameData.LastDecidedMatch = null;
            GameData.Songs = new List<int>();
        }

        private void _Advance(CTournamentMatch match)
        {
            if (match.Round + 1 >= GameData.Rounds.Count)
            {
                return;
            }

            var nextMatch = GameData.Rounds[match.Round + 1][match.Index / 2];
            if (match.Index % 2 == 0)
            {
                nextMatch.Player1 = match.WinnerPlayer;
            }
            else
            {
                nextMatch.Player2 = match.WinnerPlayer;
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

            var match = CurrentMatch;
            _ScreenSongOptions.Selection.NumJokers = new int[] { GameData.Jokers[match.Player1], GameData.Jokers[match.Player2] };
            _ScreenSongOptions.Selection.TeamNames = new string[]
            {
                CBase.Profiles.GetPlayerName(GameData.ProfileIds[match.Player1]),
                CBase.Profiles.GetPlayerName(GameData.ProfileIds[match.Player2])
            };
        }

        private bool _PrepareRound(int[] songIds)
        {
            CBase.Game.Reset();
            CBase.Game.ClearSongs();

            CBase.Game.SetNumPlayer(NumPlayerAtOnce);
            var players = CBase.Game.GetPlayers();
            var match = CurrentMatch;
            if (players == null || players.Length < NumPlayerAtOnce || match == null)
            {
                return false;
            }

            players[0].ProfileId = GameData.ProfileIds[match.Player1];
            players[1].ProfileId = GameData.ProfileIds[match.Player2];

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
            var match = CurrentMatch;
            if (match == null)
            {
                return;
            }

            var points = CBase.Game.GetPoints();
            if (points == null || points.NumRounds == 0)
            {
                return;
            }

            var scores = new double[NumPlayerAtOnce];
            for (var round = 0; round < points.NumRounds; round++)
            {
                var res = points.GetPlayer(round, NumPlayerAtOnce);
                if (res == null || res.Length < NumPlayerAtOnce)
                {
                    return;
                }

                for (var p = 0; p < NumPlayerAtOnce; p++)
                {
                    scores[p] += res[p].Points;
                }
            }

            match.Score1 = (int)Math.Round(scores[0] / points.NumRounds);
            match.Score2 = (int)Math.Round(scores[1] / points.NumRounds);
            GameData.NumMatchesPlayed++;

            if (match.Score1 == match.Score2)
            {
                GameData.IsRematch = true;
                return;
            }

            GameData.IsRematch = false;
            GameData.LastDecidedMatch = match;
            match.Winner = match.Score1 > match.Score2 ? 0 : 1;
            _Advance(match);
        }
    }
}
