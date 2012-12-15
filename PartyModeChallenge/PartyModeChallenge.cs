using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Menu;

namespace Vocaluxe.PartyModes
{
    public struct DataToScreen
    {

    }

    public struct DataFromScreen
    {
        public FromScreenConfig ScreenConfig;
        public FromScreenNames ScreenNames;
        public FromScreenMain ScreenMain;
    }

    public struct FromScreenConfig
    {
        public int NumPlayers;
        public int NumPlayersAtOnce;
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
    }

    public class PartyModeChallenge : CPartyMode
    {
        private const int MaxPlayer = 10;
        private const int MinPlayer = 2;
        private const int MaxTeams = 0;
        private const int MinTeams = 0;

        enum EStage
        {
            NotStarted,
            Config,
            Names,
            Main,
            Singing
        }

        private EStage _Stage;

        public PartyModeChallenge()
        {
            _ScreenSongOptions.Selection.RandomOnly = false;
            _ScreenSongOptions.Selection.PartyMode = false;
            _ScreenSongOptions.Selection.CategoryChangeAllowed = true;
            _ScreenSongOptions.Selection.NumJokers = null;

            _ScreenSongOptions.Sorting.SearchString = String.Empty;
            _ScreenSongOptions.Sorting.SearchStringVisible = false;

            _Stage = EStage.NotStarted;
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
                            _Stage = EStage.Names;

                        _Base.Graphics.FadeTo(EScreens.ScreenPartyDummy);
                    }
                    catch (Exception e)
                    {
                        _Base.Log.LogError("Error in party mode challenge. Can't cast received data from screen " + ScreenName + ". " + e.Message);
                    }
                    break;

                case "PartyScreenChallengeMain":
                    _Stage = EStage.Singing;
                    _Base.Graphics.FadeTo(EScreens.ScreenPartyDummy);
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
                    break;
                case EStage.Config:
                    _Screens.TryGetValue("PartyScreenChallengeNames", out Screen);
                    break;
                case EStage.Names:
                    _Screens.TryGetValue("PartyScreenChallengeMain", out Screen);
                    break;
                case EStage.Main:
                    AlternativeScreen = EScreens.ScreenSong;
                    break;
                case EStage.Singing:
                    _Screens.TryGetValue("PartyScreenChallengeMain", out Screen);
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

    }
}