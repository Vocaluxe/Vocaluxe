using System;
using System.Collections.Generic;
using VocaluxeLib.Menu;

namespace VocaluxeLib.PartyModes
{
    public abstract class CPartyMode : IPartyMode
    {
        protected SScreenSongOptions _ScreenSongOptions;
        protected Dictionary<string, CMenuParty> _Screens;
        protected string _Folder;

        public CPartyMode()
        {
            _Screens = new Dictionary<string, CMenuParty>();
            _Folder = String.Empty;
            _ScreenSongOptions = new SScreenSongOptions();
            _ScreenSongOptions.Selection = new SSelectionOptions();
            _ScreenSongOptions.Sorting = new SSortingOptions();
        }

        #region Implementation
        public virtual bool Init()
        {
            return false;
        }

        public void Initialize() {}

        public void AddScreen(CMenuParty screen, string screenName)
        {
            _Screens.Add(screenName, screen);
        }

        public virtual void DataFromScreen(string screenName, Object data) {}

        public virtual void UpdateGame() {}

        public virtual CMenuParty GetNextPartyScreen(out EScreens alternativeScreen)
        {
            alternativeScreen = EScreens.ScreenMain;
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

        public virtual SScreenSongOptions GetScreenSongOptions()
        {
            return new SScreenSongOptions();
        }

        public virtual void OnSongChange(int songIndex, ref SScreenSongOptions screenSongOptions) {}

        public virtual void OnCategoryChange(int categoryIndex, ref SScreenSongOptions screenSongOptions) {}

        public virtual int GetMaxPlayer()
        {
            return 6;
        }

        public virtual int GetMinPlayer()
        {
            return 1;
        }

        public virtual int GetMaxTeams()
        {
            return 0;
        }

        public virtual int GetMinTeams()
        {
            return 0;
        }

        public virtual int GetMaxNumRounds()
        {
            return 1;
        }

        public virtual void SetSearchString(string searchString, bool visible) {}

        public virtual void JokerUsed(int teamNr)
        {
            if (_ScreenSongOptions.Selection.NumJokers == null)
                return;

            if (_ScreenSongOptions.Selection.NumJokers.Length < teamNr)
                return;

            if (_ScreenSongOptions.Selection.NumJokers[teamNr] > 0)
                _ScreenSongOptions.Selection.NumJokers[teamNr]--;
        }

        public virtual void SongSelected(int songID) {}

        public virtual void FinishedSinging()
        {
            CBase.Graphics.FadeTo(EScreens.ScreenScore);
        }

        public virtual void LeavingScore()
        {
            CBase.Graphics.FadeTo(EScreens.ScreenHighscore);
        }

        public virtual void LeavingHighscore()
        {
            CBase.Graphics.FadeTo(EScreens.ScreenSong);
        }
        #endregion Implementation
    }
}