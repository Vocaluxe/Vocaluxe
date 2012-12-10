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

    public struct SelectionOptions
    {
        public bool RandomOnly;
        public bool PartyMode;
        public int NumJokers;
        public int NumTeams;
    }
    #endregion Structs

    interface IPartyMode
    {
        void Init();

        EScreens GetStartScreen();
        EScreens GetMainScreen();
        ScreenSongOptions GetScreenSongOptions();

        void SetSearchString(string SearchString, bool Visible);
    }
}
