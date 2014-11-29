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
using VocaluxeLib.Menu;

namespace VocaluxeLib.PartyModes
{
    public abstract class CPartyMode : IPartyMode
    {
        private readonly string _Folder;
        public int ID { get; private set; }
        protected SScreenSongOptions _ScreenSongOptions = new SScreenSongOptions {Selection = new SSelectionOptions(), Sorting = new SSortingOptions()};
        protected readonly Dictionary<string, CMenuParty> _Screens = new Dictionary<string, CMenuParty>();

        protected CPartyMode(int id, string folder)
        {
            ID = id;
            _Folder = folder;
        }

        #region Implementation
        public abstract bool Init();

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

        public virtual void UpdateGame() {}

        public abstract IMenu GetStartScreen();

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

        public virtual int GetMinPlayerPerTeam()
        {
            return GetMinPlayer();
        }

        public virtual int GetMaxPlayerPerTeam()
        {
            return GetMaxPlayer();
        }

        public virtual int GetMaxNumRounds()
        {
            return 1;
        }

        public string GetFolder()
        {
            return _Folder;
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

        public abstract void SongSelected(int songID);

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
    }
}