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
        public SongSortingOptions SongSorting;
        public SongSelectionOptions SongSelection;
    }

    public struct SongSortingOptions
    {
        public ESongSorting SongSorting;
        public EOffOn Tabs;
        public EOffOn IgnoreArticles;
        public string SearchString;
        public bool SearchStringVisible;
    }

    public struct SongSelectionOptions
    {
        public bool RandomOnly;
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
