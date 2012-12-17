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

            public int CurrentRoundNr;
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

            _ScreenSongOptions.Sorting.SearchString = String.Empty;
            _ScreenSongOptions.Sorting.SearchStringVisible = false;

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
        }

        public override bool Init()
        {
            _Stage = EStage.NotStarted;
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
                        _Base.Graphics.FadeTo(EScreens.ScreenPartyDummy);
                    }
                    catch (Exception e)
                    {
                        _Base.Log.LogError("Error in party mode challenge. Can't cast received data from screen " + ScreenName + ". " + e.Message);
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

                        _Base.Graphics.FadeTo(EScreens.ScreenPartyDummy);
                    }
                    catch (Exception e)
                    {
                        _Base.Log.LogError("Error in party mode challenge. Can't cast received data from screen " + ScreenName + ". " + e.Message);
                    }
                    break;

                case "PartyScreenChallengeMain":
                    try
                    {
                        data = (DataFromScreen)Data;
                        if (data.ScreenMain.FadeToSongSelection)
                            _Stage = EStage.Singing;
                    }
                    catch (Exception e)
                    {
                        _Base.Log.LogError("Error in party mode challenge. Can't cast received data from screen " + ScreenName + ". " + e.Message);
                    }


                    if (_Stage == EStage.Singing)
                        StartNextRound();
                    break;

                default:
                    _Base.Log.LogError("Error in party mode challenge. Wrong screen is sending: " + ScreenName);
                    break;
            }
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
                        Screen.DataToScreen(ToScreenMain);
                    break;
                case EStage.Main:
                    AlternativeScreen = EScreens.ScreenSong;
                    break;
                case EStage.Singing:
                    _Screens.TryGetValue("PartyScreenChallengeMain", out Screen);
                    if (_Screens != null)
                        Screen.DataToScreen(ToScreenMain);
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
            _ScreenSongOptions.Sorting.SongSorting = _Base.Config.GetSongSorting();
            _ScreenSongOptions.Sorting.Tabs = _Base.Config.GetTabs();
            _ScreenSongOptions.Sorting.IgnoreArticles = _Base.Config.GetIgnoreArticles();

            return _ScreenSongOptions;
        }

        public override void SetSearchString(string SearchString, bool Visible)
        {
            _ScreenSongOptions.Sorting.SearchString = SearchString;
            _ScreenSongOptions.Sorting.SearchStringVisible = Visible;
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

        private void StartNextRound()
        {

            _Base.Graphics.FadeTo(EScreens.ScreenSong);
        }
    }
}