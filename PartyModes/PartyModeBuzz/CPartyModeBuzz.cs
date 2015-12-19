using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;

namespace VocaluxeLib.PartyModes.Buzz
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
        protected new CPartyModeBuzz _PartyMode;

        public override void Init()
        {
            base.Init();
            _PartyMode = (CPartyModeBuzz)base._PartyMode;
        }
    }

    public class CPartyModeBuzz : CPartyMode
    {
        public override int MinMics
        {
            get { return 1; }
        }
        public override int MaxMics
        {
            get { return (CBase.Config.GetMaxNumMics() >= 4 ? 4 : CBase.Config.GetMaxNumMics()); }
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
            Names,
            Main,
            Singing,
            Scores
        }

        public struct SData
        {
            public int NumPlayer;
            public int NumPlayerAtOnce;
            public int NumRounds;
            public int NumChoices;
            public List<int> ProfileIDs;

            public CChallengeRounds Rounds;
            public List<CResultTableRow> ResultTable;
            public int[,] Results;
            public List<int> Songs;
            public int CurrentRoundNr;
            public bool Preview;
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
        public CPartyModeBuzz(int id) : base(id)
        {
            GameData = new SData {NumPlayer = 4, NumPlayerAtOnce = 2, NumRounds = 12, NumChoices = 4, CurrentRoundNr = 1, ProfileIDs = new List<int>(), Results = null, Preview = true};
        }

        public override bool Init()
        {
            if (!base.Init())
                return false;
            _Stage = EStage.Config;

            _ScreenSongOptions.Sorting.IgnoreArticles = CBase.Config.GetIgnoreArticles();
            _ScreenSongOptions.Sorting.SongSorting = CBase.Config.GetSongSorting();
            _ScreenSongOptions.Sorting.Tabs = EOffOn.TR_CONFIG_OFF;
            _ScreenSongOptions.Selection.SongIndex = -1;

            if (CBase.Config.GetTabs() == EOffOn.TR_CONFIG_ON && _ScreenSongOptions.Sorting.SongSorting != ESongSorting.TR_CONFIG_NONE)
                _ScreenSongOptions.Sorting.Tabs = EOffOn.TR_CONFIG_ON;

            GameData.ResultTable = new List<CResultTableRow>();
            GameData.Songs = new List<int>();
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
                    return _Screens["CPartyScreenBuzzConfig"];
                case EStage.Names:
                    return _Screens["CPartyScreenBuzzNames"];
                case EStage.Scores:
                    return _Screens["CPartyScreenBuzzScores"];
                case EStage.Main:
                    return _Screens["CPartyScreenBuzzMain"];
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
                    _Stage = EStage.Scores;
                    CBase.Songs.ResetSongSung();
                    GameData.ResultTable = new List<CResultTableRow>();
                    GameData.Rounds = new CChallengeRounds(GameData.NumRounds, GameData.NumPlayer, GameData.NumPlayerAtOnce);
                    GameData.CurrentRoundNr = 1;
                    break;
                case EStage.Main:
                    _Stage = EStage.Singing;
                    _StartRound(GameData.CurrentRoundNr);
                    break;
                case EStage.Singing:
                    _Stage = EStage.Scores;
                    _UpdateScores();
                    break;
                case EStage.Scores:
                    _Stage = EStage.Main;
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
            return _Screens["CPartyScreenBuzzConfig"];
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
            
        }

        // ReSharper disable RedundantAssignment
        public override void OnCategoryChange(int categoryIndex, ref SScreenSongOptions screenSongOptions)
            // ReSharper restore RedundantAssignment
        {
            
        }

        public override void SetSearchString(string searchString, bool visible)
        {
            _ScreenSongOptions.Sorting.SearchString = searchString;
            _ScreenSongOptions.Sorting.SearchActive = visible;
        }

        public override void JokerUsed(int teamNr) {}

        public override void SongSelected(int songID)
        {
            const EGameMode gm = EGameMode.TR_GAMEMODE_NORMAL;

            CBase.Game.Reset();
            CBase.Game.ClearSongs();
            CBase.Game.AddSong(songID, gm);
            CBase.Game.SetNumPlayer(GameData.NumPlayerAtOnce);

            SPlayer[] players = CBase.Game.GetPlayers();
            if (players == null)
                return;

            if (players.Length < GameData.NumPlayerAtOnce)
                return;

            CRound c = GameData.Rounds[GameData.CurrentRoundNr - 1];

            for (int i = 0; i < GameData.NumPlayerAtOnce; i++)
            {
                //try to fill with the right data
                if (c != null)
                    players[i].ProfileID = GameData.ProfileIDs[c.Players[i]];
                else
                    players[i].ProfileID = -1;
            }

            CBase.Graphics.FadeTo(EScreen.Sing);
        }

        public override void LeavingHighscore()
        {
            CBase.Songs.AddPartySongSung(CBase.Game.GetSong(0).ID);
            GameData.CurrentRoundNr++;
            Next();
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

        public void UpdateSongList()
        {
            if (GameData.Songs.Count > 0)
                return;
            
            ReadOnlyCollection<CSong> avSongs = CBase.Songs.GetSongs();
            GameData.Songs.AddRange(avSongs.Where(song => song.AvailableGameModes.Contains(EGameMode.TR_GAMEMODE_NORMAL)).Select(song => song.ID));
            
            GameData.Songs.Shuffle();
        }

        private void _StartRound(int roundNr)
        {
            CBase.Game.Reset();
            CBase.Game.ClearSongs();

            CBase.Game.SetNumPlayer(GameData.NumPlayerAtOnce);

            SPlayer[] players = CBase.Game.GetPlayers();
            if (players == null)
                return;

            if (players.Length < GameData.NumPlayerAtOnce)
                return;

            CRound round = GameData.Rounds[roundNr];

            for (int i = 0; i < GameData.NumPlayerAtOnce; i++)
            {
                players[i].ProfileID = GameData.ProfileIDs[round.Players[i]];
            }

            CBase.Game.AddSong(round.SongID, EGameMode.TR_GAMEMODE_NORMAL);

        }
    }
}
