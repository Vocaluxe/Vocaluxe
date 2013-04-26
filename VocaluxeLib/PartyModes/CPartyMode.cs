#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using System;
using System.Collections.Generic;
using VocaluxeLib.Menu;

namespace VocaluxeLib.PartyModes
{
    public abstract class CPartyMode : IPartyMode
    {
        protected SScreenSongOptions _ScreenSongOptions;
        protected readonly Dictionary<string, CMenuParty> _Screens;

        protected CPartyMode()
        {
            _Screens = new Dictionary<string, CMenuParty>();
            _ScreenSongOptions = new SScreenSongOptions {Selection = new SSelectionOptions(), Sorting = new SSortingOptions()};
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