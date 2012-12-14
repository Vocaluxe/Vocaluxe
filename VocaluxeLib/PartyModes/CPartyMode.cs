using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Menu;

namespace Vocaluxe.PartyModes
{
    public abstract class CPartyMode : IPartyMode
    {
        protected Basic _Base;
        protected ScreenSongOptions _ScreenSongOptions;
        protected List<CMenu> _Screens;
        protected string _Folder;

        public CPartyMode()
        {
            _Screens = new List<CMenu>();
            _Folder = String.Empty;
            _ScreenSongOptions = new ScreenSongOptions();
            _ScreenSongOptions.Selection = new SelectionOptions();
            _ScreenSongOptions.Sorting = new SortingOptions();
        }

        public void Initialize(Basic Base)
        {
            _Base = Base;
        }

        #region Implementation
        public virtual bool Init(string Folder)
        {
            return false;
        }

        public virtual CMenu GetNextPartyScreen()
        {
            return null;
        }

        public virtual EScreens GetStartScreen()
        {
            return EScreens.ScreenSong;
        }

        public virtual EScreens GetMainScreen()
        {
            return EScreens.ScreenSong;
        }

        public virtual ScreenSongOptions GetScreenSongOptions()
        {
            return new ScreenSongOptions();
        }

        public virtual void SetSearchString(string SearchString, bool Visible)
        {
        }

        public virtual void JokerUsed(int TeamNr)
        {
            if (_ScreenSongOptions.Selection.NumJokers == null)
                return;

            if (_ScreenSongOptions.Selection.NumJokers.Length < TeamNr)
                return;

            if (_ScreenSongOptions.Selection.NumJokers[TeamNr] > 0)
                _ScreenSongOptions.Selection.NumJokers[TeamNr]--;
        }
        #endregion Implementation
    }
}
