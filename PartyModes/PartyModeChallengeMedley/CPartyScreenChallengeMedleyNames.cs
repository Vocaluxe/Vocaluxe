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

namespace VocaluxeLib.PartyModes.ChallengeMedley
{
    // ReSharper disable UnusedMember.Global
    public class CPartyScreenChallengeMedleyNames : CMenuPartyNameSelection
        // ReSharper restore UnusedMember.Global
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private SDataFromScreen _Data;

        public override void Init()
        {
            base.Init();

            _Data.ScreenNames.ProfileIDs = new List<int>();

            _Data = new SDataFromScreen();
            var names = new SFromScreenNames {FadeToConfig = false, ProfileIDs = new List<int>()};
            _Data.ScreenNames = names;

            _AllowChangePlayerNum = false;
        }

        public override void DataToScreen(object receivedData)
        {
            try
            {
                var config = (SDataToScreenNames)receivedData;
                _Data.ScreenNames.ProfileIDs = config.ProfileIDs ?? new List<int>();

                SetPartyModeData(config.NumPlayer);

                while (_Data.ScreenNames.ProfileIDs.Count > _NumPlayer)
                    _Data.ScreenNames.ProfileIDs.RemoveAt(_Data.ScreenNames.ProfileIDs.Count - 1);
            }
            catch (Exception e)
            {
                CBase.Log.LogError("Error in party mode screen challenge names. Can't cast received data from game mode " + ThemeName + ". " + e.Message);
            }
        }


        public override void Back()
        {
            SPartyNameOptions options = GetData();
            if (options.TeamList.Length == 1)
                _Data.ScreenNames.ProfileIDs = options.TeamList[0];
            _Data.ScreenNames.FadeToConfig = true;
            _PartyMode.DataFromScreen(ThemeName, _Data);
        }

        public override void Next()
        {
            SPartyNameOptions options = GetData();
            if (options.TeamList.Length == 1)
                _Data.ScreenNames.ProfileIDs = options.TeamList[0];
            _Data.ScreenNames.FadeToConfig = false;
            _PartyMode.DataFromScreen(ThemeName, _Data);
        }
    }
}