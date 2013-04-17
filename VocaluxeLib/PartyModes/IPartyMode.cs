using System;
using VocaluxeLib.Menu;

namespace VocaluxeLib.PartyModes
{

    #region Structs
    public struct SScreenSongOptions
    {
        public SSortingOptions Sorting;
        public SSelectionOptions Selection;
    }

    public struct SSortingOptions
    {
        public ESongSorting SongSorting;
        public EOffOn Tabs;
        public EOffOn IgnoreArticles;
        public string SearchString;
        public bool SearchActive;
        public EDuetOptions DuetOptions;
    }

    /// <summary>
    ///     Configuration of song selection options
    /// </summary>
    public struct SSelectionOptions
    {
        /// <summary>
        ///     If != -1, the SongMenu should set the song selection on the provided song index (visible index) if possible
        /// </summary>
        public int SongIndex;

        /// <summary>
        ///     If true, the SongMenu should perform the select random song method
        /// </summary>
        public bool SelectNextRandomSong;

        /// <summary>
        ///     Choosing song only by random
        /// </summary>
        public bool RandomOnly;

        /// <summary>
        ///     If true, it is not allowed to go to MainScreen nor open the playlist nor open the song menu
        /// </summary>
        public bool PartyMode;

        /// <summary>
        ///     If true, it is not alled to change or leave the category. It's only valid if Tabs=On.
        /// </summary>
        public bool CategoryChangeAllowed;

        /// <summary>
        ///     The number of jokers left for each team
        /// </summary>
        public int[] NumJokers;

        /// <summary>
        ///     The Team Name of the teams (:>)
        /// </summary>
        public string[] TeamNames;
    }
    #endregion Structs

    public interface IPartyMode
    {
        bool Init();
        void Initialize();
        void AddScreen(CMenuParty screen, string screenName);
        void DataFromScreen(string screenName, Object data);

        void UpdateGame();

        CMenuParty GetNextPartyScreen(out EScreens alternativeScreen);
        EScreens GetStartScreen();
        EScreens GetMainScreen();
        SScreenSongOptions GetScreenSongOptions();

        void OnSongChange(int songIndex, ref SScreenSongOptions screenSongOptions);
        void OnCategoryChange(int categoryIndex, ref SScreenSongOptions screenSongOptions);

        int GetMaxPlayer();
        int GetMinPlayer();
        int GetMaxTeams();
        int GetMinTeams();
        int GetMaxNumRounds();

        void SetSearchString(string searchString, bool visible);

        void JokerUsed(int teamNr);
        void SongSelected(int songID);
        void FinishedSinging();
        void LeavingScore();
        void LeavingHighscore();
    }
}