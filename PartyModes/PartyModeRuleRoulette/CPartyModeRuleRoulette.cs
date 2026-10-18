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
using VocaluxeLib.Songs;
using VocaluxeLib.Utils;

[assembly: ComVisible(false)]

namespace VocaluxeLib.PartyModes.RuleRoulette
{
    public enum ERule
    {
        ShortSong,
        Medley,
        DoublePack,
        DuetOnly,
        FreeChoice,
        UnderdogChoice,
        GoldRush,
        PerfectLines,
        TargetScore,
        Reverse,
        Teamwork,
        DoubleWin,
        DoublePoints,
        Blitz,
        AllOrNothing,
        PointSteal,
        LeadCounts,
        Underdog,
        JokerRain,
        NoNet
    }

    public enum ERuleState
    {
        Random,
        Always,
        Never
    }

    public class CRoulettePlayer
    {
        public Guid ProfileId;
        public int TablePoints;
        public int SingPoints;
        public int Duels;
        public int Wins;
        public int Jokers;
    }

    public class CRouletteDuel
    {
        public CRoulettePlayer[] Players = new CRoulettePlayer[2];
        public ERule Rule;
        public int TargetScore;
        public CRoulettePlayer Chooser;
        public int[] Scores = new int[2];
        public int[] Awarded = new int[2];
        public bool Decided;
    }

    public abstract class CPartyScreenRuleRoulette : CMenuParty
    {
        protected new CPartyModeRuleRoulette _PartyMode;

        public override void Init()
        {
            base.Init();
            _PartyMode = (CPartyModeRuleRoulette)base._PartyMode;
        }
    }

    public sealed class CPartyModeRuleRoulette : CPartyMode
    {
        public const int PlayersPerDuel = 2;
        public const int MaxJokers = 3;
        public const int MinRulesOnWheel = 6;
        public const int MaxRulesOnWheel = 12;
        public const int MaxSongsPerPlayer = 10;
        public const int MinSongsForRule = 5;
        public const int MedleySongs = 3;
        public const int TeamworkScore = 12000;
        public const int DoubleWinScore = 7000;

        public override int MinMics
        {
            get { return PlayersPerDuel; }
        }
        public override int MaxMics
        {
            get { return CBase.Config.GetMaxNumMics(); }
        }
        public override int MinPlayers
        {
            get { return 2; }
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
            Rules,
            Songs,
            Names,
            Main,
            SongSelection,
            Singing,
            DirectSinging
        }

        public struct SData
        {
            public int NumPlayer;
            public int SongsPerPlayer;
            public int NumJokers;
            public bool ManualRules;
            public int NumRulesOnWheel;
            public Dictionary<ERule, ERuleState> RuleStates;
            public List<Guid> ProfileIds;

            public ESongSource SongSource;
            public ESongSorting Sorting;
            public int CategoryIndex;
            public int PlaylistId;
            public EGameMode GameMode;
            public int NumMedleySongs;

            public List<CRoulettePlayer> Players;
            public List<CRouletteDuel> Duels;
            public int DuelIndex;
            public List<ERule> Wheel;
            public List<ERule> UsedInCycle;
            public bool RuleChosen;
            public CRouletteDuel LastDuel;
            public int NumDuelsPlayed;
        }

        public SData GameData;
        private EStage _Stage;
        private List<CSong> _SourceSongs = new List<CSong>();
        private readonly HashSet<int> _SungSongs = new HashSet<int>();

        public CPartyModeRuleRoulette(int id) : base(id)
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

            var states = new Dictionary<ERule, ERuleState>();
            foreach (ERule rule in Enum.GetValues(typeof(ERule)))
            {
                states[rule] = ERuleState.Random;
            }

            GameData = new SData
            {
                NumPlayer = 4,
                SongsPerPlayer = 3,
                NumJokers = 1,
                ManualRules = false,
                NumRulesOnWheel = 8,
                RuleStates = states,
                ProfileIds = new List<Guid>(),
                Sorting = CBase.Config.GetSongSorting(),
                SongSource = ESongSource.TR_SONGSOURCE_ALLSONGS,
                PlaylistId = 0,
                CategoryIndex = 0,
                GameMode = EGameMode.TR_GAMEMODE_NORMAL,
                NumMedleySongs = MedleySongs,
                Players = new List<CRoulettePlayer>(),
                Duels = new List<CRouletteDuel>(),
                Wheel = new List<ERule>(),
                UsedInCycle = new List<ERule>()
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
            get { return GameData.Duels.Count > 0 && GameData.DuelIndex >= GameData.Duels.Count; }
        }

        public CRouletteDuel CurrentDuel
        {
            get { return IsFinished || GameData.Duels.Count == 0 ? null : GameData.Duels[GameData.DuelIndex]; }
        }

        public List<CRoulettePlayer> GetRanking()
        {
            return GameData.Players.OrderByDescending(player => player.TablePoints).ThenByDescending(player => player.SingPoints).ToList();
        }

        public static string GetRuleKey(ERule rule)
        {
            return "TR_RULE_" + rule.ToString().ToUpperInvariant();
        }

        private IMenu _GetNextScreen()
        {
            switch (_Stage)
            {
                case EStage.Config:
                    return _Screens["CPartyScreenRuleRouletteConfig"];
                case EStage.Rules:
                    return _Screens["CPartyScreenRuleRouletteRules"];
                case EStage.Songs:
                    return _Screens["CPartyScreenRuleRouletteSongs"];
                case EStage.Names:
                    return _Screens["CPartyScreenRuleRouletteNames"];
                case EStage.Main:
                    return _Screens["CPartyScreenRuleRouletteMain"];
                case EStage.SongSelection:
                    return CBase.Graphics.GetScreen(EScreen.Song);
                case EStage.Singing:
                case EStage.DirectSinging:
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
                    _Stage = GameData.ManualRules ? EStage.Rules : EStage.Songs;
                    break;
                case EStage.Rules:
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
                    if (CurrentDuel == null || !GameData.RuleChosen)
                    {
                        return;
                    }

                    if (_IsDirectRule(CurrentDuel.Rule))
                    {
                        _Stage = EStage.DirectSinging;
                        _PrepareRound(_PickDirectSongs(CurrentDuel.Rule));
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
                case EStage.Singing:
                case EStage.DirectSinging:
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
                case EStage.Rules:
                    _Stage = EStage.Config;
                    break;
                case EStage.Songs:
                    _Stage = GameData.ManualRules ? EStage.Rules : EStage.Config;
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
            return _Screens["CPartyScreenRuleRouletteConfig"];
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
            var freeChoice = CurrentDuel != null && _IsFreeChoiceRule(CurrentDuel.Rule);
            if (!freeChoice && (categoryIndex != -1 || CBase.Config.GetTabs() == EOffOn.TR_CONFIG_OFF))
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
            var songIds = new List<int> { songId };
            if (CurrentDuel != null && CurrentDuel.Rule == ERule.DoublePack)
            {
                _SungSongs.Add(songId);
                var extra = _PickSongs(song => !song.IsDuet, 1);
                songIds.AddRange(extra);
            }

            _PrepareRound(songIds.ToArray());

            Next();
        }

        public override void LeavingHighscore()
        {
            for (var i = 0; i < CBase.Game.GetNumSongs(); i++)
            {
                CBase.Songs.AddPartySongSung(CBase.Game.GetSong(i).Id);
                _SungSongs.Add(CBase.Game.GetSong(i).Id);
            }

            Next();
        }

        public void SpinWheel(out int resultIndex)
        {
            resultIndex = -1;
            var duel = CurrentDuel;
            if (duel == null || GameData.Wheel.Count == 0)
            {
                return;
            }

            var open = GameData.Wheel.Where(wheelRule => !GameData.UsedInCycle.Contains(wheelRule)).ToList();
            if (open.Count == 0)
            {
                GameData.UsedInCycle.Clear();
                open = new List<ERule>(GameData.Wheel);
            }

            var rule = open[CBase.Game.GetRandom(open.Count)];
            GameData.UsedInCycle.Add(rule);
            resultIndex = GameData.Wheel.IndexOf(rule);

            duel.Rule = rule;
            duel.Chooser = null;
            duel.TargetScore = 0;
            if (rule == ERule.TargetScore)
            {
                duel.TargetScore = 4000 + CBase.Game.GetRandom(41) * 100;
            }
            else if (rule == ERule.UnderdogChoice)
            {
                duel.Chooser = _GetWeaker(duel);
            }

            GameData.RuleChosen = true;
        }

        private CRoulettePlayer _GetWeaker(CRouletteDuel duel)
        {
            var a = duel.Players[0];
            var b = duel.Players[1];
            if (a.TablePoints != b.TablePoints)
            {
                return a.TablePoints < b.TablePoints ? a : b;
            }

            if (a.SingPoints != b.SingPoints)
            {
                return a.SingPoints < b.SingPoints ? a : b;
            }

            return duel.Players[CBase.Game.GetRandom(2)];
        }

        private static bool _IsDirectRule(ERule rule)
        {
            return rule == ERule.Medley || rule == ERule.GoldRush;
        }

        private static bool _IsFreeChoiceRule(ERule rule)
        {
            return rule == ERule.FreeChoice || rule == ERule.UnderdogChoice;
        }

        private static bool _HasGoldenNotes(CSong song)
        {
            return song.Notes.Voices.Any(voice => voice.Lines.Any(line => line.Notes.Any(note => note.IsGoldenNote)));
        }

        private void _StartGame()
        {
            CBase.Songs.ResetSongSung();
            _SungSongs.Clear();
            _SourceSongs = _GetSourceSongs();

            GameData.Players = new List<CRoulettePlayer>();
            foreach (var profileId in GameData.ProfileIds)
            {
                GameData.Players.Add(new CRoulettePlayer { ProfileId = profileId, Jokers = GameData.NumJokers });
            }

            _BuildDuels();
            _BuildWheel();

            GameData.DuelIndex = 0;
            GameData.UsedInCycle = new List<ERule>();
            GameData.RuleChosen = false;
            GameData.LastDuel = null;
            GameData.NumDuelsPlayed = 0;
        }

        private void _BuildDuels()
        {
            var numDuels = Math.Max(1, (GameData.Players.Count * GameData.SongsPerPlayer + 1) / PlayersPerDuel);
            var counts = GameData.Players.ToDictionary(player => player, player => 0);
            var met = new Dictionary<string, int>();
            GameData.Duels = new List<CRouletteDuel>();
            CRouletteDuel last = null;

            for (var d = 0; d < numDuels; d++)
            {
                CRoulettePlayer bestA = null;
                CRoulettePlayer bestB = null;
                var bestValue = int.MaxValue;
                var players = new List<CRoulettePlayer>(GameData.Players);
                players.Shuffle();

                foreach (var a in players)
                {
                    foreach (var b in players)
                    {
                        if (a == b)
                        {
                            continue;
                        }

                        int times;
                        met.TryGetValue(_PairKey(a, b), out times);
                        var repeat = last != null && (last.Players.Contains(a) || last.Players.Contains(b)) ? 1 : 0;
                        var value = (counts[a] + counts[b]) * 100 + times * 10 + repeat;
                        if (value < bestValue)
                        {
                            bestValue = value;
                            bestA = a;
                            bestB = b;
                        }
                    }
                }

                var duel = new CRouletteDuel();
                duel.Players[0] = bestA;
                duel.Players[1] = bestB;
                GameData.Duels.Add(duel);
                counts[bestA]++;
                counts[bestB]++;
                var key = _PairKey(bestA, bestB);
                int count;
                met.TryGetValue(key, out count);
                met[key] = count + 1;
                last = duel;
            }
        }

        private string _PairKey(CRoulettePlayer a, CRoulettePlayer b)
        {
            var ia = GameData.Players.IndexOf(a);
            var ib = GameData.Players.IndexOf(b);
            return Math.Min(ia, ib) + "-" + Math.Max(ia, ib);
        }

        public bool IsRuleAvailable(ERule rule)
        {
            switch (rule)
            {
                case ERule.ShortSong:
                case ERule.Blitz:
                    return _SourceSongs.Count(song => song.IsGameModeAvailable(EGameMode.TR_GAMEMODE_SHORTSONG)) >= MinSongsForRule;
                case ERule.Medley:
                    return _SourceSongs.Count(song => !song.IsDuet && song.IsGameModeAvailable(EGameMode.TR_GAMEMODE_MEDLEY)) >= MinSongsForRule;
                case ERule.DuetOnly:
                    return _SourceSongs.Count(song => song.IsDuet) >= 1;
                case ERule.GoldRush:
                    return _SourceSongs.Count(song => !song.IsDuet && _HasGoldenNotes(song)) >= MinSongsForRule;
                default:
                    return true;
            }
        }

        private void _BuildWheel()
        {
            var available = Enum.GetValues(typeof(ERule)).Cast<ERule>().Where(IsRuleAvailable).ToList();
            var always = new List<ERule>();
            var random = new List<ERule>();
            foreach (var rule in available)
            {
                var state = GameData.ManualRules ? GameData.RuleStates[rule] : ERuleState.Random;
                if (state == ERuleState.Always)
                {
                    always.Add(rule);
                }
                else if (state == ERuleState.Random)
                {
                    random.Add(rule);
                }
            }

            always.Shuffle();
            random.Shuffle();
            var size = Math.Max(Math.Min(always.Count, MaxRulesOnWheel), GameData.NumRulesOnWheel);
            var wheel = always.Take(size).ToList();
            wheel.AddRange(random.Take(size - wheel.Count));
            if (wheel.Count == 0)
            {
                available.Shuffle();
                wheel = available.Take(size).ToList();
            }

            wheel.Shuffle();
            GameData.Wheel = wheel;
        }

        private List<CSong> _GetSourceSongs()
        {
            var songs = new List<CSong>();
            switch (GameData.SongSource)
            {
                case ESongSource.TR_SONGSOURCE_PLAYLIST:
                    for (var i = 0; i < CBase.Playlist.GetSongCount(GameData.PlaylistId); i++)
                    {
                        var song = CBase.Songs.GetSongById(CBase.Playlist.GetSong(GameData.PlaylistId, i).SongId);
                        if (song != null)
                        {
                            songs.Add(song);
                        }
                    }

                    break;

                case ESongSource.TR_SONGSOURCE_ALLSONGS:
                    songs.AddRange(CBase.Songs.GetSongs());
                    break;

                case ESongSource.TR_SONGSOURCE_CATEGORY:
                    var oldSorting = CBase.Config.GetSongSorting();
                    CBase.Songs.SortSongs(GameData.Sorting, EOffOn.TR_CONFIG_ON, CBase.Config.GetIgnoreArticles(), string.Empty, EDuetOptions.All, -1);
                    CBase.Songs.SetCategory(GameData.CategoryIndex);
                    songs.AddRange(CBase.Songs.GetVisibleSongs());
                    CBase.Songs.SetCategory(-1);
                    CBase.Songs.SortSongs(oldSorting, CBase.Config.GetTabs(), CBase.Config.GetIgnoreArticles(), string.Empty, EDuetOptions.All, -1);
                    break;
            }

            return songs;
        }

        private List<int> _PickSongs(Func<CSong, bool> filter, int count)
        {
            var candidates = _SourceSongs.Where(song => filter(song) && !_SungSongs.Contains(song.Id)).ToList();
            if (candidates.Count < count)
            {
                candidates = _SourceSongs.Where(filter).ToList();
            }

            candidates.Shuffle();
            return candidates.Take(count).Select(song => song.Id).ToList();
        }

        private int[] _PickDirectSongs(ERule rule)
        {
            if (rule == ERule.Medley)
            {
                return _PickSongs(song => !song.IsDuet && song.IsGameModeAvailable(EGameMode.TR_GAMEMODE_MEDLEY), MedleySongs).ToArray();
            }

            return _PickSongs(song => !song.IsDuet && _HasGoldenNotes(song), 1).ToArray();
        }

        private void _PrepareSongSelection()
        {
            var duel = CurrentDuel;
            var freeChoice = _IsFreeChoiceRule(duel.Rule);

            _ScreenSongOptions.Selection.RandomOnly = !freeChoice;
            _ScreenSongOptions.Selection.SelectNextRandomSong = !freeChoice;
            _ScreenSongOptions.Sorting.IgnoreArticles = CBase.Config.GetIgnoreArticles();
            _ScreenSongOptions.Sorting.DuetOptions = duel.Rule == ERule.DuetOnly ? EDuetOptions.Duets : EDuetOptions.All;

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

            var noJokers = duel.Rule == ERule.NoNet || freeChoice;
            _ScreenSongOptions.Selection.NumJokers = new int[] { noJokers ? 0 : duel.Players[0].Jokers, noJokers ? 0 : duel.Players[1].Jokers };
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

            CBase.Game.SetNumPlayer(PlayersPerDuel);
            var players = CBase.Game.GetPlayers();
            var duel = CurrentDuel;
            if (players == null || players.Length < PlayersPerDuel || duel == null)
            {
                return false;
            }

            for (var p = 0; p < PlayersPerDuel; p++)
            {
                players[p].ProfileId = duel.Players[p].ProfileId;
                players[p].VoiceNr = p;
            }

            var medley = duel.Rule == ERule.Medley;
            var shortSong = duel.Rule == ERule.ShortSong || duel.Rule == ERule.Blitz;
            foreach (var songId in songIds)
            {
                var song = CBase.Songs.GetSongById(songId);
                EGameMode gameMode;
                if (medley)
                {
                    gameMode = EGameMode.TR_GAMEMODE_MEDLEY;
                }
                else if (song.IsDuet)
                {
                    gameMode = EGameMode.TR_GAMEMODE_DUET;
                }
                else if (shortSong && song.IsGameModeAvailable(EGameMode.TR_GAMEMODE_SHORTSONG))
                {
                    gameMode = EGameMode.TR_GAMEMODE_SHORTSONG;
                }
                else if (song.IsGameModeAvailable(GameData.GameMode))
                {
                    gameMode = GameData.GameMode;
                }
                else
                {
                    gameMode = EGameMode.TR_GAMEMODE_NORMAL;
                }

                CBase.Game.AddSong(songId, gameMode);
            }

            return true;
        }

        private static int _LeadPoints(int lead)
        {
            if (lead <= 1000)
            {
                return 1;
            }

            if (lead <= 4000)
            {
                return 2;
            }

            return lead <= 6500 ? 3 : 4;
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

            var total = new double[PlayersPerDuel];
            var golden = new double[PlayersPerDuel];
            var lines = new double[PlayersPerDuel];
            for (var round = 0; round < points.NumRounds; round++)
            {
                var res = points.GetPlayer(round, PlayersPerDuel);
                if (res == null || res.Length < PlayersPerDuel)
                {
                    return;
                }

                for (var p = 0; p < PlayersPerDuel; p++)
                {
                    total[p] += res[p].Points;
                    golden[p] += res[p].PointsGoldenNotes;
                    lines[p] += res[p].PointsLineBonus;
                }
            }

            var divisor = duel.Rule == ERule.DoublePack ? 1 : points.NumRounds;
            var metric = new int[PlayersPerDuel];
            for (var p = 0; p < PlayersPerDuel; p++)
            {
                duel.Scores[p] = (int)Math.Round(total[p] / divisor);
                switch (duel.Rule)
                {
                    case ERule.GoldRush:
                        metric[p] = (int)Math.Round(golden[p] / divisor);
                        break;
                    case ERule.PerfectLines:
                        metric[p] = (int)Math.Round(lines[p] / divisor);
                        break;
                    case ERule.TargetScore:
                        metric[p] = -Math.Abs(duel.Scores[p] - duel.TargetScore);
                        break;
                    case ERule.Reverse:
                        metric[p] = -duel.Scores[p];
                        break;
                    default:
                        metric[p] = duel.Scores[p];
                        break;
                }
            }

            var weaker = duel.Rule == ERule.Underdog ? _GetWeaker(duel) : null;
            _Award(duel, metric, weaker);

            for (var p = 0; p < PlayersPerDuel; p++)
            {
                var player = duel.Players[p];
                player.TablePoints = Math.Max(0, player.TablePoints + duel.Awarded[p]);
                player.SingPoints += duel.Scores[p];
                player.Duels++;
            }

            duel.Decided = true;
            GameData.LastDuel = duel;
            GameData.NumDuelsPlayed++;
            GameData.DuelIndex++;
            GameData.RuleChosen = false;
        }

        private void _Award(CRouletteDuel duel, int[] metric, CRoulettePlayer weaker)
        {
            duel.Awarded[0] = 0;
            duel.Awarded[1] = 0;

            if (duel.Rule == ERule.Teamwork)
            {
                var together = duel.Scores[0] + duel.Scores[1] > TeamworkScore ? 2 : 0;
                duel.Awarded[0] = together;
                duel.Awarded[1] = together;
                return;
            }

            if (duel.Rule == ERule.DoubleWin && duel.Scores[0] > DoubleWinScore && duel.Scores[1] > DoubleWinScore)
            {
                duel.Awarded[0] = 2;
                duel.Awarded[1] = 2;
                duel.Players[0].Wins++;
                duel.Players[1].Wins++;
                return;
            }

            if (metric[0] == metric[1])
            {
                duel.Awarded[0] = 1;
                duel.Awarded[1] = 1;
                if (duel.Rule == ERule.JokerRain)
                {
                    duel.Players[0].Jokers++;
                    duel.Players[1].Jokers++;
                }

                return;
            }

            var winner = metric[0] > metric[1] ? 0 : 1;
            var loser = 1 - winner;
            duel.Players[winner].Wins++;
            if (duel.Rule == ERule.JokerRain)
            {
                duel.Players[winner].Jokers++;
            }

            switch (duel.Rule)
            {
                case ERule.DoublePoints:
                case ERule.Blitz:
                    duel.Awarded[winner] = 4;
                    break;
                case ERule.AllOrNothing:
                    duel.Awarded[winner] = 3;
                    duel.Awarded[loser] = -1;
                    break;
                case ERule.PointSteal:
                    duel.Awarded[winner] = 2 + (duel.Players[loser].TablePoints > 0 ? 1 : 0);
                    duel.Awarded[loser] = duel.Players[loser].TablePoints > 0 ? -1 : 0;
                    break;
                case ERule.LeadCounts:
                    duel.Awarded[winner] = _LeadPoints(Math.Abs(duel.Scores[0] - duel.Scores[1]));
                    break;
                case ERule.Underdog:
                    duel.Awarded[winner] = duel.Players[winner] == weaker ? 3 : 2;
                    break;
                default:
                    duel.Awarded[winner] = 2;
                    break;
            }
        }
    }
}
