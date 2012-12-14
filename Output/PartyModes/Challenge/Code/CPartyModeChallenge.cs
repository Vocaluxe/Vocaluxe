using System;
using System.Collections.Generic;
using System.Text;

namespace Vocaluxe.PartyModes
{
    public class CPartyModeChallenge : CPartyMode
    {
        public CPartyModeChallenge()
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