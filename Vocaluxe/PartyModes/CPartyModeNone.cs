using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Base;

namespace Vocaluxe.PartyModes
{
    class CPartyModeNone : CPartyMode
    {
        //just a dummy for normal game mode
        public CPartyModeNone()
        {
            _ScreenSongOptions.Selection.RandomOnly = false;
            _ScreenSongOptions.Selection.PartyMode = false;
            _ScreenSongOptions.Selection.CategoryChangeAllowed = true;
            _ScreenSongOptions.Selection.NumJokers = null;

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
