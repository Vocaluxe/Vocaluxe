using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Menu;

namespace Vocaluxe.PartyModes
{
    #region Communication
    #region ToScreen
    public struct DataToScreenConfig
    {
        public int NumPlayer;
        public int NumPlayerAtOnce;
        public int NumRounds;
    }

    public struct DataToScreenNames
    {
        public int NumPlayer;
        public List<int> ProfileIDs;
    }

    public struct DataToScreenMain
    {
        public int CurrentRoundNr;
        public int NumPlayerAtOnce;
        public List<Combination> Combs;
        public int[,] Results;
        public List<ResultTableRow> ResultTable;
        public List<int> ProfileIDs;
    }

    public class ResultTableRow : IComparable
    {
        public int Position;
        public int PlayerID;
        public int NumPlayed;
        public int NumRounds;
        public int NumWon;
        public int NumSingPoints;
        public int NumGamePoints;

        public int CompareTo(object obj)
        {
            if (obj is ResultTableRow)
            {
                ResultTableRow row = (ResultTableRow)obj;

                int res = row.NumGamePoints.CompareTo(NumGamePoints);
                if (res == 0)
                {
                    res = row.NumSingPoints.CompareTo(NumSingPoints);
                    if (res == 0)
                    {
                        res = row.NumWon.CompareTo(NumWon);
                    }
                }
                return res;
            }

            throw new ArgumentException("object is not a ResultTableRow");
        }
    }

    #endregion ToScreen

    #region FromScreen
    public struct DataFromScreen
    {
        public FromScreenConfig ScreenConfig;
        public FromScreenNames ScreenNames;
        public FromScreenMain ScreenMain;
    }

    public struct FromScreenConfig
    {
        public int NumPlayer;
        public int NumPlayerAtOnce;
        public int NumRounds;
    }

    public struct FromScreenNames
    {
        public bool FadeToConfig;
        public bool FadeToMain;
        public List<int> ProfileIDs;
    }

    public struct FromScreenMain
    {
        public bool FadeToSongSelection;
        public bool FadeToNameSelection;
    }
    #endregion FromScreen
    #endregion Communication

    public sealed class PartyModeChallenge : CPartyMode
    {
        private const int MaxPlayer = 12;
        private const int MinPlayer = 1;
        private const int MaxTeams = 0;
        private const int MinTeams = 0;
        private const int MaxNumRounds = 100;

        enum EStage
        {
            NotStarted,
            Config,
            Names,
            Main,
            Singing
        }

        struct Data
        {
            public int NumPlayer;
            public int NumPlayerAtOnce;
            public int NumRounds;
            public List<int> ProfileIDs;

            public ChallengeRounds Rounds;
            public List<ResultTableRow> ResultTable;
            public int[,] Results;

            public int CurrentRoundNr;

            public int[] CatSongIndices;
        }

        struct Stats
        {
            public int ProfileID;
            public int SingPoints;
            public int GamePoints;
            public int Won;
        }

        private DataToScreenConfig ToScreenConfig;
        private DataToScreenNames ToScreenNames;
        private DataToScreenMain ToScreenMain;

        private Data GameData;
        private EStage _Stage;

        public PartyModeChallenge()
        {
            _ScreenSongOptions.Selection.RandomOnly = false;
            _ScreenSongOptions.Selection.PartyMode = true;
            _ScreenSongOptions.Selection.CategoryChangeAllowed = true;
            _ScreenSongOptions.Selection.NumJokers = new int[] { 5, 5 };
            _ScreenSongOptions.Selection.TeamNames = new string[] { "foo", "bar" };

            _ScreenSongOptions.Sorting.SearchString = String.Empty;
            _ScreenSongOptions.Sorting.SearchActive = false;
            _ScreenSongOptions.Sorting.ShowDuetSongs = false;
            
            _Stage = EStage.NotStarted;

            ToScreenConfig = new DataToScreenConfig();
            ToScreenNames = new DataToScreenNames();
            ToScreenMain = new DataToScreenMain();

            GameData = new Data();
            GameData.NumPlayer = 4;
            GameData.NumPlayerAtOnce = 2;
            GameData.NumRounds = 2;
            GameData.CurrentRoundNr = 1;
            GameData.ProfileIDs = new List<int>();
            GameData.CatSongIndices = null;
            GameData.Results = null;
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

            ToScreenMain.ResultTable = new List<ResultTableRow>();
            GameData.ResultTable = new List<ResultTableRow>();

            return true;
        }

        public override void DataFromScreen(string ScreenName, Object Data)
        {
            DataFromScreen data = new DataFromScreen();
            switch (ScreenName)
            {
                case "PartyScreenChallengeConfig":
                    
                    try
                    {
                        data = (DataFromScreen)Data;
                        GameData.NumPlayer = data.ScreenConfig.NumPlayer;
                        GameData.NumPlayerAtOnce = data.ScreenConfig.NumPlayerAtOnce;
                        GameData.NumRounds = data.ScreenConfig.NumRounds;

                        _Stage = EStage.Config;
                        CBase.Graphics.FadeTo(EScreens.ScreenPartyDummy);
                    }
                    catch (Exception e)
                    {
                        CBase.Log.LogError("Error in party mode challenge. Can't cast received data from screen " + ScreenName + ". " + e.Message);
                    }
                    break;

                case "PartyScreenChallengeNames":
                    try
                    {
                        data = (DataFromScreen)Data;
                        if (data.ScreenNames.FadeToConfig)
                            _Stage = EStage.NotStarted;
                        else
                        {
                            GameData.ProfileIDs = data.ScreenNames.ProfileIDs;
                            _Stage = EStage.Names;
                        }

                        CBase.Graphics.FadeTo(EScreens.ScreenPartyDummy);
                    }
                    catch (Exception e)
                    {
                        CBase.Log.LogError("Error in party mode challenge. Can't cast received data from screen " + ScreenName + ". " + e.Message);
                    }
                    break;

                case "PartyScreenChallengeMain":
                    try
                    {
                        data = (DataFromScreen)Data;
                        if (data.ScreenMain.FadeToSongSelection)
                            _Stage = EStage.Singing;
                        if (data.ScreenMain.FadeToNameSelection)
                            _Stage = EStage.Config;
                    }
                    catch (Exception e)
                    {
                        CBase.Log.LogError("Error in party mode challenge. Can't cast received data from screen " + ScreenName + ". " + e.Message);
                    }


                    if (_Stage == EStage.Singing)
                        StartNextRound();
                    if (_Stage == EStage.Config)
                        CBase.Graphics.FadeTo(EScreens.ScreenPartyDummy);
                    break;

                default:
                    CBase.Log.LogError("Error in party mode challenge. Wrong screen is sending: " + ScreenName);
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

        public override CMenuParty GetNextPartyScreen(out EScreens AlternativeScreen)
        {
            CMenuParty Screen = null;
            AlternativeScreen = EScreens.ScreenSong;

            switch (_Stage)
            {
                case EStage.NotStarted:
                    _Screens.TryGetValue("PartyScreenChallengeConfig", out Screen);
                    if (_Screens != null)
                    {
                        ToScreenConfig.NumPlayer = GameData.NumPlayer;
                        ToScreenConfig.NumPlayerAtOnce = GameData.NumPlayerAtOnce;
                        ToScreenConfig.NumRounds = GameData.NumRounds;
                        Screen.DataToScreen(ToScreenConfig);
                    }
                    break;
                case EStage.Config:
                    _Screens.TryGetValue("PartyScreenChallengeNames", out Screen);
                    if (_Screens != null)
                    {
                        ToScreenNames.NumPlayer = GameData.NumPlayer;
                        ToScreenNames.ProfileIDs = GameData.ProfileIDs;
                        Screen.DataToScreen(ToScreenNames);
                    }
                    break;
                case EStage.Names:
                    _Screens.TryGetValue("PartyScreenChallengeMain", out Screen);
                    if (_Screens != null)
                    {
                        CBase.Songs.ResetPartySongSung();
                        ToScreenMain.ResultTable = new List<ResultTableRow>();
                        GameData.ResultTable = new List<ResultTableRow>();
                        GameData.Rounds = new ChallengeRounds(GameData.NumRounds, GameData.NumPlayer, GameData.NumPlayerAtOnce);
                        GameData.CurrentRoundNr = 1;
                        ToScreenMain.CurrentRoundNr = 1;
                        ToScreenMain.NumPlayerAtOnce = GameData.NumPlayerAtOnce;
                        ToScreenMain.Combs = GameData.Rounds.Rounds;
                        ToScreenMain.ProfileIDs = GameData.ProfileIDs;
                        UpdateScores();
                        ToScreenMain.ResultTable = GameData.ResultTable;
                        ToScreenMain.Results = GameData.Results;
                        Screen.DataToScreen(ToScreenMain);
                    }
                    break;
                case EStage.Main:
                    //nothing to do
                    break;
                case EStage.Singing:
                    _Screens.TryGetValue("PartyScreenChallengeMain", out Screen);
                    if (_Screens != null)
                    {
                        UpdateScores();
                        ToScreenMain.CurrentRoundNr = GameData.CurrentRoundNr;
                        ToScreenMain.Combs = GameData.Rounds.Rounds;
                        ToScreenMain.Results = GameData.Results;
                        Screen.DataToScreen(ToScreenMain);
                    }
                    break;
                default:
                    break;
            }
            
            return Screen;
        }

        public override EScreens GetStartScreen()
        {
            return EScreens.ScreenPartyDummy;
        }

        public override EScreens GetMainScreen()
        {
            return EScreens.ScreenPartyDummy;
        }

        public override ScreenSongOptions GetScreenSongOptions()
        {
            _ScreenSongOptions.Sorting.SongSorting = CBase.Config.GetSongSorting();
            _ScreenSongOptions.Sorting.Tabs = CBase.Config.GetTabs();
            _ScreenSongOptions.Sorting.IgnoreArticles = CBase.Config.GetIgnoreArticles();

            return _ScreenSongOptions;
        }

        public override void OnSongChange(int SongIndex, ref ScreenSongOptions ScreenSongOptions)
        {
            if (_ScreenSongOptions.Selection.SongIndex != -1)
                _ScreenSongOptions.Selection.SongIndex = -1;

            if (_ScreenSongOptions.Selection.SelectNextRandomSong && SongIndex != -1)
            {
                _ScreenSongOptions.Selection.SelectNextRandomSong = false;
                CreateCatSongIndices();

                if (GameData.CatSongIndices != null)
                {
                    if (GameData.CatSongIndices[CBase.Songs.GetCurrentCategoryIndex()] == -1)
                        GameData.CatSongIndices[CBase.Songs.GetCurrentCategoryIndex()] = SongIndex;
                }
            }

            ScreenSongOptions = _ScreenSongOptions;
        }

        public override void OnCategoryChange(int CategoryIndex, ref ScreenSongOptions ScreenSongOptions)
        {
            if (GameData.CatSongIndices != null && CategoryIndex != -1)
            {
                if (GameData.CatSongIndices[CategoryIndex] == -1)
                    _ScreenSongOptions.Selection.SelectNextRandomSong = true;
                else
                    _ScreenSongOptions.Selection.SongIndex = GameData.CatSongIndices[CategoryIndex];
            }

            if (GameData.CatSongIndices == null && CategoryIndex != -1)
                _ScreenSongOptions.Selection.SelectNextRandomSong = true;

            if (CategoryIndex == -1)
                _ScreenSongOptions.Selection.RandomOnly = false;

            if (_ScreenSongOptions.Sorting.Tabs == EOffOn.TR_CONFIG_OFF || CategoryIndex != -1)
                _ScreenSongOptions.Selection.RandomOnly = true;

            ScreenSongOptions = _ScreenSongOptions;
        }

        public override void SetSearchString(string SearchString, bool Visible)
        {
            _ScreenSongOptions.Sorting.SearchString = SearchString;
            _ScreenSongOptions.Sorting.SearchActive = Visible;
        }

        public override int GetMaxPlayer()
        {
            return MaxPlayer;
        }

        public override int GetMinPlayer()
        {
            return MinPlayer;
        }

        public override int GetMaxTeams()
        {
            return MaxTeams;
        }

        public override int GetMinTeams()
        {
            return MinTeams;
        }

        public override int GetMaxNumRounds()
        {
            return MaxNumRounds;
        }

        public override void JokerUsed(int TeamNr)
        {
            if (_ScreenSongOptions.Selection.NumJokers == null)
                return;

            if (TeamNr >= _ScreenSongOptions.Selection.NumJokers.Length)
                return;

            _ScreenSongOptions.Selection.NumJokers[TeamNr]--;
            _ScreenSongOptions.Selection.RandomOnly = true;
            _ScreenSongOptions.Selection.CategoryChangeAllowed = false;
        }

        public override void SongSelected(int SongID)
        {

            EGameMode gm = EGameMode.TR_GAMEMODE_NORMAL;

            CBase.Game.Reset();
            CBase.Game.ClearSongs();
            CBase.Game.AddSong(SongID, gm);
            CBase.Game.SetNumPlayer(GameData.NumPlayerAtOnce);

            SPlayer[] player = CBase.Game.GetPlayer();
            if (player == null)
                return;

            if (player.Length < GameData.NumPlayerAtOnce)
                return;

            SProfile[] profiles = CBase.Profiles.GetProfiles();
            Combination c = GameData.Rounds.GetRound(GameData.CurrentRoundNr - 1);

            for (int i = 0; i < GameData.NumPlayerAtOnce; i++)
            {
                //default values
                player[i].Name = "foobar";
                player[i].Difficulty = EGameDifficulty.TR_CONFIG_EASY;
                player[i].ProfileID = -1;

                //try to fill with the right data
                if (c != null)
                {
                    if (GameData.ProfileIDs[c.Player[i]] < profiles.Length)
                    {
                        player[i].Name = profiles[GameData.ProfileIDs[c.Player[i]]].PlayerName;
                        player[i].Difficulty = profiles[GameData.ProfileIDs[c.Player[i]]].Difficulty;
                        player[i].ProfileID = GameData.ProfileIDs[c.Player[i]];
                    }
                }
            }

            CBase.Songs.AddPartySongSung(SongID);
            CBase.Graphics.FadeTo(EScreens.ScreenSing);
        }

        public override void LeavingHighscore()
        {
            GameData.CurrentRoundNr++;
            CBase.Graphics.FadeTo(EScreens.ScreenPartyDummy);
        }

        private void StartNextRound()
        {
            _ScreenSongOptions.Selection.RandomOnly = _ScreenSongOptions.Sorting.Tabs != EOffOn.TR_CONFIG_ON;
            _ScreenSongOptions.Selection.CategoryChangeAllowed = _ScreenSongOptions.Sorting.Tabs == EOffOn.TR_CONFIG_ON;
            SetNumJokers();
            SetTeamNames();
            GameData.CatSongIndices = null;
            CBase.Graphics.FadeTo(EScreens.ScreenSong);
        }

        private void SetNumJokers()
        {
            switch (GameData.NumPlayerAtOnce)
            {
                case 1:
                    _ScreenSongOptions.Selection.NumJokers = new int[] { 10 };
                    break;

                case 2:
                    _ScreenSongOptions.Selection.NumJokers = new int[] { 5, 5 };
                    break;

                case 3:
                    _ScreenSongOptions.Selection.NumJokers = new int[] { 4, 4, 4 };
                    break;

                case 4:
                    _ScreenSongOptions.Selection.NumJokers = new int[] { 3, 3, 3, 3 };
                    break;

                case 5:
                    _ScreenSongOptions.Selection.NumJokers = new int[] { 2, 2, 2, 2, 2 };
                    break;

                case 6:
                    _ScreenSongOptions.Selection.NumJokers = new int[] { 2, 2, 2, 2, 2, 2 };
                    break;
                default:
                    _ScreenSongOptions.Selection.NumJokers = new int[] { 5, 5 };
                    break;
            }
        }

        private void SetTeamNames()
        {
            SProfile[] profiles = CBase.Profiles.GetProfiles();

            if (profiles == null)
            {
                _ScreenSongOptions.Selection.TeamNames = new string[] { "foo", "bar" };
                return;
            }

            if (GameData.NumPlayerAtOnce < 1 || GameData.ProfileIDs.Count < GameData.NumPlayerAtOnce || profiles.Length < GameData.NumPlayerAtOnce)
            {
                _ScreenSongOptions.Selection.TeamNames = new string[] { "foo", "bar" };
                return;
            }
            
            _ScreenSongOptions.Selection.TeamNames = new string[GameData.NumPlayerAtOnce];
            Combination c = GameData.Rounds.GetRound(GameData.CurrentRoundNr - 1);

            for (int i = 0; i < GameData.NumPlayerAtOnce; i++)
            {
                if (c != null)
                {
                    if (GameData.ProfileIDs[c.Player[i]] < profiles.Length)
                        _ScreenSongOptions.Selection.TeamNames[i] = profiles[GameData.ProfileIDs[c.Player[i]]].PlayerName;
                    else
                        _ScreenSongOptions.Selection.TeamNames[i] = "foobar";
                }
                else
                    _ScreenSongOptions.Selection.TeamNames[i] = "foobar";
            }
        }

        private void UpdateScores()
        {
            if (GameData.ResultTable.Count == 0)
            {
                for (int i = 0; i < GameData.NumPlayer; i++)
                {
                    ResultTableRow row = new ResultTableRow();
                    row.PlayerID = GameData.ProfileIDs[i];
                    row.NumPlayed = 0;
                    row.NumRounds = 0;
                    row.NumWon = 0;
                    row.NumSingPoints = 0;
                    row.NumGamePoints = 0;
                    GameData.ResultTable.Add(row);
                }

                GameData.Results = new int[GameData.NumRounds, GameData.NumPlayerAtOnce];
                for (int i = 0; i < GameData.NumRounds; i++)
                {
                    for (int j = 0; j < GameData.NumPlayerAtOnce; j++)
                    {
                        GameData.Results[i, j] = 0;
                    }
                }
            }
            else
            {
                SPlayer[] results = CBase.Game.GetPlayer();
                if (results == null)
                    return;

                if (results.Length < GameData.NumPlayerAtOnce)
                    return;

                for (int j = 0; j < GameData.NumPlayerAtOnce; j++)
                {
                    GameData.Results[GameData.CurrentRoundNr - 2, j] = (int)Math.Round(results[j].Points);
                }

                List<Stats> points = GetPointsForPlayer(results);

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
                        ResultTableRow row = GameData.ResultTable[index];

                        row.NumPlayed++;
                        row.NumWon += points[i].Won;
                        row.NumRounds += 1;
                        row.NumSingPoints += points[i].SingPoints;
                        row.NumGamePoints += points[i].GamePoints;

                        GameData.ResultTable[index] = row;
                    }
                }
            }

            GameData.ResultTable.Sort();

            //Update position-number
            int pos = 1;
            int lastPoints = 0;
            int lastSingPoints = 0;
            for (int i = 0; i < GameData.ResultTable.Count; i++ )
            {
                if (lastPoints > GameData.ResultTable[i].NumGamePoints || lastSingPoints > GameData.ResultTable[i].NumSingPoints)
                    pos++;
                GameData.ResultTable[i].Position = pos;
                lastPoints = GameData.ResultTable[i].NumGamePoints;
                lastSingPoints = GameData.ResultTable[i].NumSingPoints;
            }
        }

        private List<Stats> GetPointsForPlayer(SPlayer[] Results)
        {
            List<Stats> result = new List<Stats>();
            for(int i = 0; i < GameData.NumPlayerAtOnce; i++)
            {
                Stats stat = new Stats();
                stat.ProfileID = Results[i].ProfileID;
                stat.SingPoints = (int)Math.Round(Results[i].Points);
                stat.Won = 0;
                stat.GamePoints = 0;
                result.Add(stat);
            }

            result.Sort(delegate(Stats s1, Stats s2) { return s1.SingPoints.CompareTo(s2.SingPoints); });

            int current = result[result.Count - 1].SingPoints;
            int points = result.Count;
            bool wonset = false;

            for (int i = result.Count - 1; i >= 0; i--)
            {
                Stats res = result[i];

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

        private void CreateCatSongIndices()
        {
            if (GameData.CatSongIndices == null && CBase.Songs.GetNumCategories() > 0 && _ScreenSongOptions.Sorting.Tabs == EOffOn.TR_CONFIG_ON)
            {
                GameData.CatSongIndices = new int[CBase.Songs.GetNumCategories()];
                for (int i = 0; i < GameData.CatSongIndices.Length; i++)
                {
                    GameData.CatSongIndices[i] = -1;
                }
            }
            
            if (CBase.Songs.GetNumCategories() == 0 || _ScreenSongOptions.Sorting.Tabs == EOffOn.TR_CONFIG_OFF)
                GameData.CatSongIndices = null;
        }
    }
}