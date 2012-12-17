using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Menu;

namespace Vocaluxe.PartyModes
{
    #region Structs
    public struct ScreenSongOptions
    {
        public SortingOptions Sorting;
        public SelectionOptions Selection;
    }

    public struct SortingOptions
    {
        public ESongSorting SongSorting;
        public EOffOn Tabs;
        public EOffOn IgnoreArticles;
        public string SearchString;
        public bool SearchStringVisible;
    }

    /// <summary>
    /// Configuration of song selection options
    /// </summary>
    public struct SelectionOptions
    {
        /// <summary>
        /// Choosing song only by random
        /// </summary>
        public bool RandomOnly;

        /// <summary>
        /// If true, it is not allowed to go to MainScreen nor open the playlist nor open the song menu
        /// </summary>
        public bool PartyMode;

        /// <summary>
        /// If true, it is not alled to change or leave the category. It's only valid if Tabs=On.
        /// </summary>
        public bool CategoryChangeAllowed;

        /// <summary>
        /// The number of jokers left for each team
        /// </summary>
        public int[] NumJokers;

        /// <summary>
        /// The Team Name of the teams (:>)
        /// </summary>
        public string[] TeamNames;
    }
    #endregion Structs

    public interface IPartyMode
    {
        bool Init();
        void Initialize(Basic Base);
        void AddScreen(CMenuParty Screen, string ScreenName);
        void DataFromScreen(string ScreenName, Object Data);

        void UpdateGame();

        CMenuParty GetNextPartyScreen(out EScreens AlternativeScreen);
        EScreens GetStartScreen();
        EScreens GetMainScreen();
        ScreenSongOptions GetScreenSongOptions();

        int GetMaxPlayer();
        int GetMinPlayer();
        int GetMaxTeams();
        int GetMinTeams();
        int GetMaxNumRounds();

        void SetSearchString(string SearchString, bool Visible);

        void JokerUsed(int TeamNr);
        void SongSelected(int SongID);
        void FinishedSinging();
        void LeavingScore();
        void LeavingHighscore();
    }
}
