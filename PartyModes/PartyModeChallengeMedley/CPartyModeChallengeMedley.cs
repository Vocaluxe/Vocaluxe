using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using VocaluxeLib.Menu;
using VocaluxeLib.Menu.SongMenu;

[assembly: ComVisible(false)]

namespace VocaluxeLib.PartyModes.ChallengeMedley
{

    #region Communication

    #region ToScreen
    public struct SDataToScreenConfig
    {
        public int NumPlayer;
        public int NumPlayerAtOnce;
        public int NumRounds;
    }

    public struct SDataToScreenNames
    {
        public int NumPlayer;
        public List<int> ProfileIDs;
    }

    public struct SDataToScreenMain
    {
        public int CurrentRoundNr;
        public int NumPlayerAtOnce;
        public List<CCombination> Combs;
        public int[,] Results;
        public List<CResultTableRow> ResultTable;
        public List<int> ProfileIDs;
    }

    public class CResultTableRow : IComparable
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
            if (obj is CResultTableRow)
            {
                CResultTableRow row = (CResultTableRow)obj;

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
    #endregion ToScreen

    #region FromScreen
    public struct SDataFromScreen
    {
        public SFromScreenConfig ScreenConfig;
        public SFromScreenNames ScreenNames;
        public SFromScreenMain ScreenMain;
    }

    public struct SFromScreenConfig
    {
        public int NumPlayer;
        public int NumPlayerAtOnce;
        public int NumRounds;
    }

    public struct SFromScreenNames
    {
        public bool FadeToConfig;
        public bool FadeToMain;
        public List<int> ProfileIDs;
    }

    public struct SFromScreenMain
    {
        public bool FadeToSongSelection;
        public bool FadeToNameSelection;
    }
    #endregion FromScreen

    #endregion Communication

    public sealed class CPartyModeChallengeMedley : CPartyMode
    {
        private const int _MaxPlayer = 12;
        private const int _MinPlayer = 1;
        private const int _MaxTeams = 0;
        private const int _MinTeams = 0;
        private const int _MaxNumRounds = 100;
        private const int _NumSongs = 5;

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
            public int NumPlayer;
            public int NumPlayerAtOnce;
            public int NumRounds;
            public List<int> ProfileIDs;

            public CChallengeRounds Rounds;
            public List<CResultTableRow> ResultTable;
            public int[,] Results;

            public int CurrentRoundNr;

            public int[] CatSongIndices;
        }

        private struct SStats
        {
            public int ProfileID;
            public int SingPoints;
            public int GamePoints;
            public int Won;
        }

        private SDataToScreenConfig _ToScreenConfig;
        private SDataToScreenNames _ToScreenNames;
        private SDataToScreenMain _ToScreenMain;

        private SData _GameData;
        private EStage _Stage;

        public CPartyModeChallengeMedley()
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

            _GameData = new SData();
            _GameData.NumPlayer = 4;
            _GameData.NumPlayerAtOnce = 2;
            _GameData.NumRounds = 2;
            _GameData.CurrentRoundNr = 1;
            _GameData.ProfileIDs = new List<int>();
            _GameData.CatSongIndices = null;
            _GameData.Results = null;
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

            _ToScreenMain.ResultTable = new List<CResultTableRow>();
            _GameData.ResultTable = new List<CResultTableRow>();

            CBase.Songs.SortSongs(ESongSorting.TR_CONFIG_NONE, EOffOn.TR_CONFIG_OFF, EOffOn.TR_CONFIG_OFF, String.Empty, EDuetOptions.NoDuets);
            CBase.Songs.SetCategory(0);

            return true;
        }

        public override void DataFromScreen(string screenName, Object data)
        {
            SDataFromScreen dataFrom = new SDataFromScreen();
            switch (screenName)
            {
                case "PartyScreenChallengeMedleyConfig":

                    try
                    {
                        dataFrom = (SDataFromScreen)data;
                        _GameData.NumPlayer = dataFrom.ScreenConfig.NumPlayer;
                        _GameData.NumPlayerAtOnce = dataFrom.ScreenConfig.NumPlayerAtOnce;
                        _GameData.NumRounds = dataFrom.ScreenConfig.NumRounds;

                        _Stage = EStage.Config;
                        CBase.Graphics.FadeTo(EScreens.ScreenPartyDummy);
                    }
                    catch (Exception e)
                    {
                        CBase.Log.LogError("Error in party mode challenge. Can't cast received data from screen " + screenName + ". " + e.Message);
                    }
                    break;

                case "PartyScreenChallengeMedleyNames":
                    try
                    {
                        dataFrom = (SDataFromScreen)data;
                        if (dataFrom.ScreenNames.FadeToConfig)
                            _Stage = EStage.NotStarted;
                        else
                        {
                            _GameData.ProfileIDs = dataFrom.ScreenNames.ProfileIDs;
                            _Stage = EStage.Names;
                        }

                        CBase.Graphics.FadeTo(EScreens.ScreenPartyDummy);
                    }
                    catch (Exception e)
                    {
                        CBase.Log.LogError("Error in party mode challenge. Can't cast received data from screen " + screenName + ". " + e.Message);
                    }
                    break;

                case "PartyScreenChallengeMedleyMain":
                    try
                    {
                        dataFrom = (SDataFromScreen)data;
                        if (dataFrom.ScreenMain.FadeToSongSelection)
                            _Stage = EStage.Singing;
                        if (dataFrom.ScreenMain.FadeToNameSelection)
                            _Stage = EStage.Config;
                    }
                    catch (Exception e)
                    {
                        CBase.Log.LogError("Error in party mode challenge. Can't cast received data from screen " + screenName + ". " + e.Message);
                    }


                    if (_Stage == EStage.Singing)
                        _StartNextRound();
                    if (_Stage == EStage.Config)
                        CBase.Graphics.FadeTo(EScreens.ScreenPartyDummy);
                    break;

                default:
                    CBase.Log.LogError("Error in party mode challenge. Wrong screen is sending: " + screenName);
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
                    _Screens.TryGetValue("CPartyScreenChallengeMedleyConfig", out screen);
                    if (screen != null)
                    {
                        _ToScreenConfig.NumPlayer = _GameData.NumPlayer;
                        _ToScreenConfig.NumPlayerAtOnce = _GameData.NumPlayerAtOnce;
                        _ToScreenConfig.NumRounds = _GameData.NumRounds;
                        screen.DataToScreen(_ToScreenConfig);
                    }
                    break;
                case EStage.Config:
                    _Screens.TryGetValue("CPartyScreenChallengeMedleyNames", out screen);
                    if (screen != null)
                    {
                        _ToScreenNames.NumPlayer = _GameData.NumPlayer;
                        _ToScreenNames.ProfileIDs = _GameData.ProfileIDs;
                        screen.DataToScreen(_ToScreenNames);
                    }
                    break;
                case EStage.Names:
                    _Screens.TryGetValue("CPartyScreenChallengeMedleyMain", out screen);
                    if (screen != null)
                    {
                        CBase.Songs.ResetPartySongSung();
                        _ToScreenMain.ResultTable = new List<CResultTableRow>();
                        _GameData.ResultTable = new List<CResultTableRow>();
                        _GameData.Rounds = new CChallengeRounds(_GameData.NumRounds, _GameData.NumPlayer, _GameData.NumPlayerAtOnce);
                        _GameData.CurrentRoundNr = 1;
                        _ToScreenMain.CurrentRoundNr = 1;
                        _ToScreenMain.NumPlayerAtOnce = _GameData.NumPlayerAtOnce;
                        _ToScreenMain.Combs = _GameData.Rounds.Rounds;
                        _ToScreenMain.ProfileIDs = _GameData.ProfileIDs;
                        _UpdateScores();
                        _ToScreenMain.ResultTable = _GameData.ResultTable;
                        _ToScreenMain.Results = _GameData.Results;
                        screen.DataToScreen(_ToScreenMain);
                    }
                    break;
                case EStage.Main:
                    //nothing to do
                    break;
                case EStage.Singing:
                    _Screens.TryGetValue("CPartyScreenChallengeMedleyMain", out screen);
                    if (screen != null)
                    {
                        _UpdateScores();
                        _ToScreenMain.CurrentRoundNr = _GameData.CurrentRoundNr;
                        _ToScreenMain.Combs = _GameData.Rounds.Rounds;
                        _ToScreenMain.Results = _GameData.Results;
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

        public override void OnSongChange(int songIndex, ref SScreenSongOptions screenSongOptions)
        {
            if (_ScreenSongOptions.Selection.SongIndex != -1)
                _ScreenSongOptions.Selection.SongIndex = -1;

            if (_ScreenSongOptions.Selection.SelectNextRandomSong && songIndex != -1)
            {
                _ScreenSongOptions.Selection.SelectNextRandomSong = false;
                _CreateCatSongIndices();

                if (_GameData.CatSongIndices != null)
                {
                    if (_GameData.CatSongIndices[CBase.Songs.GetCurrentCategoryIndex()] == -1)
                        _GameData.CatSongIndices[CBase.Songs.GetCurrentCategoryIndex()] = songIndex;
                }
            }

            screenSongOptions = _ScreenSongOptions;
        }

        public override void OnCategoryChange(int categoryIndex, ref SScreenSongOptions screenSongOptions)
        {
            if (_GameData.CatSongIndices != null && categoryIndex != -1)
            {
                if (_GameData.CatSongIndices[categoryIndex] == -1)
                    _ScreenSongOptions.Selection.SelectNextRandomSong = true;
                else
                    _ScreenSongOptions.Selection.SongIndex = _GameData.CatSongIndices[categoryIndex];
            }

            if (_GameData.CatSongIndices == null && categoryIndex != -1)
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
            return _MaxNumRounds;
        }

        public override void JokerUsed(int teamNr)
        {
            if (_ScreenSongOptions.Selection.NumJokers == null)
                return;

            if (teamNr >= _ScreenSongOptions.Selection.NumJokers.Length)
                return;

            _ScreenSongOptions.Selection.NumJokers[teamNr]--;
            _ScreenSongOptions.Selection.RandomOnly = true;
            _ScreenSongOptions.Selection.CategoryChangeAllowed = false;
        }

        public override void SongSelected(int songID) {}

        public override void LeavingHighscore()
        {
            _GameData.CurrentRoundNr++;
            CBase.Graphics.FadeTo(EScreens.ScreenPartyDummy);
        }

        private void _StartNextRound()
        {
            _ScreenSongOptions.Selection.RandomOnly = _ScreenSongOptions.Sorting.Tabs != EOffOn.TR_CONFIG_ON;
            _ScreenSongOptions.Selection.CategoryChangeAllowed = _ScreenSongOptions.Sorting.Tabs == EOffOn.TR_CONFIG_ON;
            _SetNumJokers();
            _SetTeamNames();
            _GameData.CatSongIndices = null;

            if (_RandomizeSongs())
                CBase.Graphics.FadeTo(EScreens.ScreenSing);
        }

        private bool _RandomizeSongs()
        {
            CBase.Game.Reset();
            CBase.Game.ClearSongs();

            #region PlayerNames
            CBase.Game.SetNumPlayer(_GameData.NumPlayerAtOnce);
            SPlayer[] player = CBase.Game.GetPlayer();
            if (player == null)
                return false;

            if (player.Length < _GameData.NumPlayerAtOnce)
                return false;

            SProfile[] profiles = CBase.Profiles.GetProfiles();
            CCombination c = _GameData.Rounds.GetRound(_GameData.CurrentRoundNr - 1);

            for (int i = 0; i < _GameData.NumPlayerAtOnce; i++)
            {
                //default values
                player[i].Name = "foobar";
                player[i].Difficulty = EGameDifficulty.TR_CONFIG_EASY;
                player[i].ProfileID = -1;

                //try to fill with the right data
                if (c != null)
                {
                    if (_GameData.ProfileIDs[c.Player[i]] < profiles.Length)
                    {
                        player[i].Name = profiles[_GameData.ProfileIDs[c.Player[i]]].PlayerName;
                        player[i].Difficulty = profiles[_GameData.ProfileIDs[c.Player[i]]].Difficulty;
                        player[i].ProfileID = _GameData.ProfileIDs[c.Player[i]];
                    }
                }
            }
            #endregion PlayerNames

            #region SongQueque            
            if (CBase.Songs.GetNumVisibleSongs() < _NumSongs)
                CBase.Songs.ResetPartySongSung();

            EGameMode gm = EGameMode.TR_GAMEMODE_MEDLEY;
            List<int> iDs = new List<int>();

            CSong[] visSongs = CBase.Songs.GetSongsNotSung();
            if (visSongs == null)
            {
                CBase.Log.LogError("CPartyChallengeMedleyMode Error: There are no songs!");
                CBase.Graphics.FadeTo(EScreens.ScreenParty);
                return false;
            }

            for (int i = 0; i < visSongs.Length; i++)
            {
                foreach (EGameMode mode in visSongs[i].AvailableGameModes)
                {
                    if (mode == EGameMode.TR_GAMEMODE_MEDLEY)
                        iDs.Add(visSongs[i].ID);
                }
            }

            if (iDs.Count == 0)
            {
                CBase.Log.LogError("CPartyChallengeMedleyMode Error: There are no medley songs!");
                CBase.Graphics.FadeTo(EScreens.ScreenParty);
                return false;
            }

            int s = 0;
            while (s < _NumSongs && iDs.Count > 0)
            {
                int songID = iDs[CBase.Game.GetRandom(iDs.Count)];
                CBase.Game.AddSong(songID, gm);
                CBase.Songs.AddPartySongSung(songID);
                iDs.Remove(songID);
                s++;
            }
            return true;
            #endregion SongQueque
        }

        private void _SetNumJokers()
        {
            switch (_GameData.NumPlayerAtOnce)
            {
                case 1:
                    _ScreenSongOptions.Selection.NumJokers = new int[] {10};
                    break;

                case 2:
                    _ScreenSongOptions.Selection.NumJokers = new int[] {5, 5};
                    break;

                case 3:
                    _ScreenSongOptions.Selection.NumJokers = new int[] {4, 4, 4};
                    break;

                case 4:
                    _ScreenSongOptions.Selection.NumJokers = new int[] {3, 3, 3, 3};
                    break;

                case 5:
                    _ScreenSongOptions.Selection.NumJokers = new int[] {2, 2, 2, 2, 2};
                    break;

                case 6:
                    _ScreenSongOptions.Selection.NumJokers = new int[] {2, 2, 2, 2, 2, 2};
                    break;
                default:
                    _ScreenSongOptions.Selection.NumJokers = new int[] {5, 5};
                    break;
            }
        }

        private void _SetTeamNames()
        {
            SProfile[] profiles = CBase.Profiles.GetProfiles();

            if (profiles == null)
            {
                _ScreenSongOptions.Selection.TeamNames = new string[] {"foo", "bar"};
                return;
            }

            if (_GameData.NumPlayerAtOnce < 1 || _GameData.ProfileIDs.Count < _GameData.NumPlayerAtOnce || profiles.Length < _GameData.NumPlayerAtOnce)
            {
                _ScreenSongOptions.Selection.TeamNames = new string[] {"foo", "bar"};
                return;
            }

            _ScreenSongOptions.Selection.TeamNames = new string[_GameData.NumPlayerAtOnce];
            CCombination c = _GameData.Rounds.GetRound(_GameData.CurrentRoundNr - 1);

            for (int i = 0; i < _GameData.NumPlayerAtOnce; i++)
            {
                if (c != null)
                {
                    if (_GameData.ProfileIDs[c.Player[i]] < profiles.Length)
                        _ScreenSongOptions.Selection.TeamNames[i] = profiles[_GameData.ProfileIDs[c.Player[i]]].PlayerName;
                    else
                        _ScreenSongOptions.Selection.TeamNames[i] = "foobar";
                }
                else
                    _ScreenSongOptions.Selection.TeamNames[i] = "foobar";
            }
        }

        private void _UpdateScores()
        {
            if (_GameData.ResultTable.Count == 0)
            {
                for (int i = 0; i < _GameData.NumPlayer; i++)
                {
                    CResultTableRow row = new CResultTableRow();
                    row.PlayerID = _GameData.ProfileIDs[i];
                    row.NumPlayed = 0;
                    row.NumRounds = 0;
                    row.NumWon = 0;
                    row.NumSingPoints = 0;
                    row.NumGamePoints = 0;
                    _GameData.ResultTable.Add(row);
                }

                _GameData.Results = new int[_GameData.NumRounds,_GameData.NumPlayerAtOnce];
                for (int i = 0; i < _GameData.NumRounds; i++)
                {
                    for (int j = 0; j < _GameData.NumPlayerAtOnce; j++)
                        _GameData.Results[i, j] = 0;
                }
            }
            else
            {
                CPoints gamePoints = CBase.Game.GetPoints();
                if (gamePoints == null || gamePoints.NumPlayer < _GameData.NumPlayerAtOnce)
                    return;

                SPlayer[] results = gamePoints.GetPlayer(0, _GameData.NumPlayerAtOnce);
                if (results == null)
                    return;

                if (results.Length < _GameData.NumPlayerAtOnce)
                    return;

                if (gamePoints.NumRounds == 0)
                    return;

                for (int i = 1; i < gamePoints.NumRounds; i++)
                {
                    SPlayer[] temp = gamePoints.GetPlayer(i, _GameData.NumPlayerAtOnce);
                    for (int p = 0; p < _GameData.NumPlayerAtOnce; p++)
                        results[p].Points += temp[p].Points;
                }

                for (int j = 0; j < _GameData.NumPlayerAtOnce; j++)
                {
                    results[j].Points = results[j].Points / gamePoints.NumRounds;
                    _GameData.Results[_GameData.CurrentRoundNr - 2, j] = (int)Math.Round(results[j].Points);
                }

                List<SStats> points = _GetPointsForPlayer(results);

                for (int i = 0; i < _GameData.NumPlayerAtOnce; i++)
                {
                    int index = -1;
                    for (int j = 0; j < _GameData.ResultTable.Count; j++)
                    {
                        if (points[i].ProfileID == _GameData.ResultTable[j].PlayerID)
                        {
                            index = j;
                            break;
                        }
                    }

                    if (index != -1)
                    {
                        CResultTableRow row = _GameData.ResultTable[index];

                        row.NumPlayed++;
                        row.NumWon += points[i].Won;
                        row.NumRounds += 1;
                        row.NumSingPoints += points[i].SingPoints;
                        row.NumGamePoints += points[i].GamePoints;

                        _GameData.ResultTable[index] = row;
                    }
                }
            }

            _GameData.ResultTable.Sort();

            //Update position-number
            int pos = 1;
            int lastPoints = 0;
            int lastSingPoints = 0;
            for (int i = 0; i < _GameData.ResultTable.Count; i++)
            {
                if (lastPoints > _GameData.ResultTable[i].NumGamePoints || lastSingPoints > _GameData.ResultTable[i].NumSingPoints)
                    pos++;
                _GameData.ResultTable[i].Position = pos;
                lastPoints = _GameData.ResultTable[i].NumGamePoints;
                lastSingPoints = _GameData.ResultTable[i].NumSingPoints;
            }
        }

        private List<SStats> _GetPointsForPlayer(SPlayer[] results)
        {
            List<SStats> result = new List<SStats>();
            for (int i = 0; i < _GameData.NumPlayerAtOnce; i++)
            {
                SStats stat = new SStats();
                stat.ProfileID = results[i].ProfileID;
                stat.SingPoints = (int)Math.Round(results[i].Points);
                stat.Won = 0;
                stat.GamePoints = 0;
                result.Add(stat);
            }

            result.Sort(delegate(SStats s1, SStats s2) { return s1.SingPoints.CompareTo(s2.SingPoints); });

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
            if (_GameData.CatSongIndices == null && CBase.Songs.GetNumCategories() > 0 && _ScreenSongOptions.Sorting.Tabs == EOffOn.TR_CONFIG_ON)
            {
                _GameData.CatSongIndices = new int[CBase.Songs.GetNumCategories()];
                for (int i = 0; i < _GameData.CatSongIndices.Length; i++)
                    _GameData.CatSongIndices[i] = -1;
            }

            if (CBase.Songs.GetNumCategories() == 0 || _ScreenSongOptions.Sorting.Tabs == EOffOn.TR_CONFIG_OFF)
                _GameData.CatSongIndices = null;
        }
    }
}