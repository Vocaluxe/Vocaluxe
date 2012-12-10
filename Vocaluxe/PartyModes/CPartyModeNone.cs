using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Base;

namespace Vocaluxe.PartyModes
{
    class CPartyModeNone : CPartyMode
    {
        //just a dummy for normal game mode
        private ScreenSongOptions _ScreenSongOptions;

        public CPartyModeNone()
        {
            _ScreenSongOptions = new ScreenSongOptions();
            _ScreenSongOptions.Selection = new SelectionOptions();
            _ScreenSongOptions.Sorting = new SortingOptions();

            _ScreenSongOptions.Selection.RandomOnly = false;
            _ScreenSongOptions.Selection.PartyMode = false;
            _ScreenSongOptions.Selection.NumJokers = 0;
            _ScreenSongOptions.Selection.NumTeams = 0;

            _ScreenSongOptions.Sorting.SearchString = String.Empty;
            _ScreenSongOptions.Sorting.SearchStringVisible = false;
        }

        public override ScreenSongOptions GetScreenSongOptions()
        {
            _ScreenSongOptions.Sorting.SongSorting = CConfig.SongSorting;
            _ScreenSongOptions.Sorting.Tabs = CConfig.Tabs;
            _ScreenSongOptions.Sorting.IgnoreArticles = CConfig.IgnoreArticles;

            return _ScreenSongOptions;
        }

        public override void SetSearchString(string SearchString, bool Visible)
        {
            _ScreenSongOptions.Sorting.SearchString = SearchString;
            _ScreenSongOptions.Sorting.SearchStringVisible = Visible;
        }
    }
}
