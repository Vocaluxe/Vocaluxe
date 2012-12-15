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
        protected Dictionary<string, CMenuParty> _Screens;
        protected string _Folder;
        protected Delegate _Callback;

        public CPartyMode()
        {
            _Screens = new Dictionary<string, CMenuParty>();
            _Folder = String.Empty;
            _ScreenSongOptions = new ScreenSongOptions();
            _ScreenSongOptions.Selection = new SelectionOptions();
            _ScreenSongOptions.Sorting = new SortingOptions();
        }

        #region Implementation
        public virtual bool Init(string Folder)
        {
            return false;
        }

        public void Initialize(Basic Base)
        {
            _Base = Base;
        }

        public void AddScreen(CMenuParty Screen, string ScreenName)
        {
            _Screens.Add(ScreenName, Screen);
        }

        public void SetPartyModeCallback(Delegate Callback)
        {
            _Callback = Callback;
        }

        public virtual CMenuParty GetNextPartyScreen()
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
