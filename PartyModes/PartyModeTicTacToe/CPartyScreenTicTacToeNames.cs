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
using VocaluxeLib.Menu;

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

        private new CPartyModeTicTacToe _PartyMode;

        public override void Init()
        {
            base.Init();
            _PartyMode = (CPartyModeTicTacToe)base._PartyMode;
        }

        public override void OnShow()
        {
            base.OnShow();

            while (_PartyMode.GameData.ProfileIDsTeam1.Count > _PartyMode.GameData.NumPlayerTeam1)
                _PartyMode.GameData.ProfileIDsTeam1.RemoveAt(_PartyMode.GameData.ProfileIDsTeam1.Count - 1);
            while (_PartyMode.GameData.ProfileIDsTeam2.Count > _PartyMode.GameData.NumPlayerTeam2)
                _PartyMode.GameData.ProfileIDsTeam2.RemoveAt(_PartyMode.GameData.ProfileIDsTeam2.Count - 1);

            SetPartyModeData(2, _PartyMode.GameData.NumPlayerTeam1 + _PartyMode.GameData.NumPlayerTeam2,
                             new int[] {_PartyMode.GameData.NumPlayerTeam1, _PartyMode.GameData.NumPlayerTeam2});
            List<Guid>[] ids = new List<Guid>[] {_PartyMode.GameData.ProfileIDsTeam1, _PartyMode.GameData.ProfileIDsTeam2};
            SetPartyModeProfiles(ids);
        }

        public override void Back()
        {
            if (_TeamList.Length == 2)
            {
                _PartyMode.GameData.ProfileIDsTeam1 = _TeamList[0];
                _PartyMode.GameData.ProfileIDsTeam2 = _TeamList[1];
            }
            if (_NumPlayerTeams.Length == 2)
            {
                _PartyMode.GameData.NumPlayerTeam1 = _NumPlayerTeams[0];
                _PartyMode.GameData.NumPlayerTeam2 = _NumPlayerTeams[1];
            }
            _PartyMode.Back();
        }

        public override void Next()
        {
            if (_TeamList.Length == 2)
            {
                _PartyMode.GameData.ProfileIDsTeam1 = _TeamList[0];
                _PartyMode.GameData.ProfileIDsTeam2 = _TeamList[1];
            }
            if (_NumPlayerTeams.Length == 2)
            {
                _PartyMode.GameData.NumPlayerTeam1 = _NumPlayerTeams[0];
                _PartyMode.GameData.NumPlayerTeam2 = _NumPlayerTeams[1];
            }
            _PartyMode.Next();
        }
    }
}