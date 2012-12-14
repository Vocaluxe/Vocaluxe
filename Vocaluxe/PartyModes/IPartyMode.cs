using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Base;
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
    }
    #endregion Structs

    interface IPartyMode
    {
        bool Init(string Folder);
        void Initialize(Basic Base);

        CMenu GetNextPartyScreen();
        EScreens GetStartScreen();
        EScreens GetMainScreen();
        ScreenSongOptions GetScreenSongOptions();

        void SetSearchString(string SearchString, bool Visible);

        void JokerUsed(int TeamNr);
    }
}
