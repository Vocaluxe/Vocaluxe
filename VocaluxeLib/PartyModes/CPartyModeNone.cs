using System;
using VocaluxeLib.Menu;

namespace VocaluxeLib.PartyModes
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
            _ScreenSongOptions.Sorting.SearchActive = false;
            _ScreenSongOptions.Sorting.DuetOptions = EDuetOptions.All;
        }

        public override SScreenSongOptions GetScreenSongOptions()
        {
            _ScreenSongOptions.Sorting.SongSorting = CBase.Config.GetSongSorting();
            _ScreenSongOptions.Sorting.Tabs = CBase.Config.GetTabs();

            if (_ScreenSongOptions.Sorting.SearchActive)
                _ScreenSongOptions.Sorting.Tabs = EOffOn.TR_CONFIG_OFF;

            _ScreenSongOptions.Sorting.IgnoreArticles = CBase.Config.GetIgnoreArticles();

            return _ScreenSongOptions;
        }

        public override void SetSearchString(string searchString, bool visible)
        {
            _ScreenSongOptions.Sorting.SearchString = searchString;
            _ScreenSongOptions.Sorting.SearchActive = visible;
        }
    }
}