using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Menu;

namespace Vocaluxe.PartyModes
{
    public abstract class CPartyMode : IPartyMode
    {
        protected ScreenSongOptions _ScreenSongOptions;
        protected Dictionary<string, CMenuParty> _Screens;
        protected string _Folder;

        public CPartyMode()
        {
            _Screens = new Dictionary<string, CMenuParty>();
            _Folder = String.Empty;
            _ScreenSongOptions = new ScreenSongOptions();
            _ScreenSongOptions.Selection = new SelectionOptions();
            _ScreenSongOptions.Sorting = new SortingOptions();
        }

        #region Implementation
        public virtual bool Init()
        {
            return false;
        }

        public void Initialize()
        {
        }

        public void AddScreen(CMenuParty Screen, string ScreenName)
        {
            _Screens.Add(ScreenName, Screen);
        }

        public virtual void DataFromScreen(string ScreenName, Object Data)
        {
        }

        public virtual void UpdateGame()
        {
        }

        public virtual CMenuParty GetNextPartyScreen(out EScreens AlternativeScreen)
        {
            AlternativeScreen = EScreens.ScreenMain;
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

        public virtual void OnSongChange(int SongIndex, ref ScreenSongOptions ScreenSongOptions)
        {
        }

        public virtual void OnCategoryChange(int CategoryIndex, ref ScreenSongOptions ScreenSongOptions)
        {
        }

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

        public virtual void SongSelected(int SongID)
        {
        }

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
