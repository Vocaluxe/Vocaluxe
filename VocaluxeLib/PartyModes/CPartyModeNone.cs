using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Menu;

namespace Vocaluxe.PartyModes
{
    public class CPartyModeNone : CPartyMode
    {
        //just a dummy for normal game mode
        public CPartyModeNone()
        {
            _ScreenSongOptions.Selection.RandomOnly = false;
            _ScreenSongOptions.Selection.PartyMode = false;
            _ScreenSongOptions.Selection.CategoryChangeAllowed = true;
            _ScreenSongOptions.Selection.NumJokers = null;
            _ScreenSongOptions.Selection.TeamNames = null;
            _ScreenSongOptions.Selection.SongIndex = -1;

            _ScreenSongOptions.Sorting.SearchString = String.Empty;
            _ScreenSongOptions.Sorting.SearchStringVisible = false;
            _ScreenSongOptions.Sorting.ShowDuetSongs = true;
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
    }
}
