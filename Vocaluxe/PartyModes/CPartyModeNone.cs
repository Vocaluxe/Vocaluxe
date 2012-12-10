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
            _ScreenSongOptions.SongSelection = new SongSelectionOptions();
            _ScreenSongOptions.SongSorting = new SongSortingOptions();

            _ScreenSongOptions.SongSelection.RandomOnly = false;
            _ScreenSongOptions.SongSelection.NumJokers = 0;
            _ScreenSongOptions.SongSelection.NumTeams = 0;

            _ScreenSongOptions.SongSorting.SearchString = String.Empty;
            _ScreenSongOptions.SongSorting.SearchStringVisible = false;
        }

        public override ScreenSongOptions GetScreenSongOptions()
        {
            _ScreenSongOptions.SongSorting.SongSorting = CConfig.SongSorting;
            _ScreenSongOptions.SongSorting.Tabs = CConfig.Tabs;
            _ScreenSongOptions.SongSorting.IgnoreArticles = CConfig.IgnoreArticles;

            return _ScreenSongOptions;
        }

        public override void SetSearchString(string SearchString, bool Visible)
        {
            _ScreenSongOptions.SongSorting.SearchString = SearchString;
            _ScreenSongOptions.SongSorting.SearchStringVisible = Visible;
        }
    }
}
