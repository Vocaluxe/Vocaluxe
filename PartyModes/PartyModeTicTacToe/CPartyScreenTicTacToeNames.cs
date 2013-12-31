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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using VocaluxeLib.Menu;
using VocaluxeLib.Profile;

namespace VocaluxeLib.PartyModes.TicTacToe
{
    // ReSharper disable UnusedMember.Global
    public class CPartyScreenTicTacToeNames : CMenuPartyNameSelection
        // ReSharper restore UnusedMember.Global
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private int _NumPlayerTeam1 = 2;
        private int _NumPlayerTeam2 = 2;

        private SDataFromScreen _Data;

        public override void Init()
        {
            base.Init();

            _Data.ScreenNames.ProfileIDsTeam1 = new List<int>();
            _Data.ScreenNames.ProfileIDsTeam2 = new List<int>();

            _Data = new SDataFromScreen();
            var names = new SFromScreenNames {FadeToConfig = false, ProfileIDsTeam1 = new List<int>(), ProfileIDsTeam2 = new List<int>()};
            _Data.ScreenNames = names;
        }

        public override void DataToScreen(object receivedData)
        {
            try
            {
                var config = (SDataToScreenNames)receivedData;
                _Data.ScreenNames.ProfileIDsTeam1 = config.ProfileIDsTeam1;
                _Data.ScreenNames.ProfileIDsTeam2 = config.ProfileIDsTeam2;
                if (_Data.ScreenNames.ProfileIDsTeam1 == null)
                    _Data.ScreenNames.ProfileIDsTeam1 = new List<int>();
                if (_Data.ScreenNames.ProfileIDsTeam2 == null)
                    _Data.ScreenNames.ProfileIDsTeam2 = new List<int>();

                _NumPlayerTeam1 = config.NumPlayerTeam1;
                _NumPlayerTeam2 = config.NumPlayerTeam2;

                while (_Data.ScreenNames.ProfileIDsTeam1.Count > _NumPlayerTeam1)
                    _Data.ScreenNames.ProfileIDsTeam1.RemoveAt(_Data.ScreenNames.ProfileIDsTeam1.Count - 1);
                while (_Data.ScreenNames.ProfileIDsTeam2.Count > _NumPlayerTeam2)
                    _Data.ScreenNames.ProfileIDsTeam2.RemoveAt(_Data.ScreenNames.ProfileIDsTeam2.Count - 1);
            }
            catch (Exception e)
            {
                CBase.Log.LogError("Error in party mode screen TicTacToe names. Can't cast received data from game mode " + ThemeName + ". " + e.Message);
            }
        }


        public override void OnShow()
        {
            base.OnShow();
            SetPartyModeData(2, _NumPlayerTeam1 + _NumPlayerTeam2, new int[] { _NumPlayerTeam1, _NumPlayerTeam2 });
            List<int>[] ids = new List<int>[] {_Data.ScreenNames.ProfileIDsTeam1, _Data.ScreenNames.ProfileIDsTeam2};
            SetPartyModeProfiles(ids);
        }

        public override void Back()
        {
            SPartyNameOptions options = GetData();
            if(options.TeamList.Length == 2)
            {
                _Data.ScreenNames.ProfileIDsTeam1 = options.TeamList[0];
                _Data.ScreenNames.ProfileIDsTeam2 = options.TeamList[1];
            }
            if (options.NumPlayerTeams.Length == 2)
            {
                _Data.ScreenNames.NumPlayerTeam1 = options.NumPlayerTeams[0];
                _Data.ScreenNames.NumPlayerTeam2 = options.NumPlayerTeams[1];
            }
            _Data.ScreenNames.FadeToConfig = true;
            _PartyMode.DataFromScreen(ThemeName, _Data);
        }

        public override void Next()
        {
            SPartyNameOptions options = GetData();
            if (options.TeamList.Length == 2)
            {
                _Data.ScreenNames.ProfileIDsTeam1 = options.TeamList[0];
                _Data.ScreenNames.ProfileIDsTeam2 = options.TeamList[1];
            }
            if (options.NumPlayerTeams.Length == 2)
            {
                _Data.ScreenNames.NumPlayerTeam1 = options.NumPlayerTeams[0];
                _Data.ScreenNames.NumPlayerTeam2 = options.NumPlayerTeams[1];
            }
            _Data.ScreenNames.FadeToConfig = false;
            _PartyMode.DataFromScreen(ThemeName, _Data);
        }
    }
}