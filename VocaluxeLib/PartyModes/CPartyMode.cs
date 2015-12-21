#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System.Collections.Generic;
using System.Diagnostics;
using VocaluxeLib.Menu;

namespace VocaluxeLib.PartyModes
{
    public abstract class CPartyMode : IPartyMode
    {
        private readonly int _ID;
        protected SScreenSongOptions _ScreenSongOptions = new SScreenSongOptions {Selection = new SSelectionOptions(), Sorting = new SSortingOptions()};
        protected readonly Dictionary<string, CMenuParty> _Screens = new Dictionary<string, CMenuParty>();

        protected CPartyMode(int id)
        {
            _ID = id;
        }

        #region Implementation
        public int ID
        {
            get { return _ID; }
        }

        public int NumPlayers { get; set; }
        public int NumTeams { get; set; }

        public virtual bool Init()
        {
            Debug.Assert(MinPlayers > 0 && MinPlayers <= MaxPlayers);
            Debug.Assert(MinTeams > 0 && MinTeams <= MaxTeams);
            return true;
        }

        public virtual void SetDefaults()
        {
            return;
        }

        public void LoadTheme()
        {
            string xmlPath = CBase.Themes.GetThemeScreensPath(ID);
            foreach (CMenuParty menu in _Screens.Values)
            {
                menu.Init();
                menu.LoadTheme(xmlPath);
            }
        }

        public void ReloadSkin()
        {
            foreach (CMenuParty menu in _Screens.Values)
                menu.ReloadSkin();
        }

        public void ReloadTheme()
        {
            string xmlPath = CBase.Themes.GetThemeScreensPath(ID);
            foreach (CMenuParty menu in _Screens.Values)
                menu.ReloadTheme(xmlPath);
        }

        public void AddScreen(CMenuParty screen, string screenName)
        {
            _Screens.Add(screenName, screen);
        }

        public void SaveScreens()
        {
            foreach (KeyValuePair<string, CMenuParty> entry in _Screens)
                entry.Value.SaveTheme();
        }

        public virtual void JokerUsed(int teamNr)
        {
            if (_ScreenSongOptions.Selection.NumJokers == null)
                return;

            if (_ScreenSongOptions.Selection.NumJokers.Length < teamNr)
                return;

            if (_ScreenSongOptions.Selection.NumJokers[teamNr] > 0)
                _ScreenSongOptions.Selection.NumJokers[teamNr]--;
        }

        public virtual void FinishedSinging()
        {
            CBase.Graphics.FadeTo(EScreen.Score);
        }

        public virtual void LeavingScore()
        {
            CBase.Graphics.FadeTo(EScreen.Highscore);
        }

        public virtual void LeavingHighscore()
        {
            CBase.Graphics.FadeTo(EScreen.Song);
        }
        #endregion Implementation

        #region Abstract members
        public abstract int MinMics { get; }
        public abstract int MaxMics { get; }
        public abstract int MinPlayers { get; }
        public abstract int MaxPlayers { get; }
        public abstract int MinTeams { get; }
        public abstract int MaxTeams { get; }
        public abstract int MinPlayersPerTeam { get; }
        public abstract int MaxPlayersPerTeam { get; }

        public abstract void UpdateGame();
        public abstract IMenu GetStartScreen();
        public abstract SScreenSongOptions GetScreenSongOptions();
        public abstract void OnSongChange(int songIndex, ref SScreenSongOptions screenSongOptions);
        public abstract void OnCategoryChange(int categoryIndex, ref SScreenSongOptions screenSongOptions);
        public abstract void SetSearchString(string searchString, bool visible);

        public abstract void SongSelected(int songID);
        #endregion Abstract members
    }
}