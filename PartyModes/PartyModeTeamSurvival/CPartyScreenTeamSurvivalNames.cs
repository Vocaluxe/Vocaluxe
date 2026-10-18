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

namespace VocaluxeLib.PartyModes.TeamSurvival
{
    public class CPartyScreenTeamSurvivalNames : CMenuPartyNameSelection
    {
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private new CPartyModeTeamSurvival _PartyMode;

        public override void Init()
        {
            base.Init();
            _PartyMode = (CPartyModeTeamSurvival)base._PartyMode;
        }

        public override void OnShow()
        {
            base.OnShow();

            while (_PartyMode.GameData.ProfileIdsTeam1.Count > _PartyMode.GameData.NumPlayerTeam1)
            {
                _PartyMode.GameData.ProfileIdsTeam1.RemoveAt(_PartyMode.GameData.ProfileIdsTeam1.Count - 1);
            }

            while (_PartyMode.GameData.ProfileIdsTeam2.Count > _PartyMode.GameData.NumPlayerTeam2)
            {
                _PartyMode.GameData.ProfileIdsTeam2.RemoveAt(_PartyMode.GameData.ProfileIdsTeam2.Count - 1);
            }

            SetPartyModeData(CPartyModeTeamSurvival.TeamCount, _PartyMode.GameData.NumPlayerTeam1 + _PartyMode.GameData.NumPlayerTeam2,
                new int[] { _PartyMode.GameData.NumPlayerTeam1, _PartyMode.GameData.NumPlayerTeam2 });
            var ids = new List<Guid>[] { _PartyMode.GameData.ProfileIdsTeam1, _PartyMode.GameData.ProfileIdsTeam2 };
            SetPartyModeProfiles(ids);
        }

        public override void Back()
        {
            _SaveTeams();
            _PartyMode.Back();
        }

        public override void Next()
        {
            _SaveTeams();
            _PartyMode.GameData.NumPlayerTeam1 = _PartyMode.GameData.ProfileIdsTeam1.Count;
            _PartyMode.GameData.NumPlayerTeam2 = _PartyMode.GameData.ProfileIdsTeam2.Count;
            _PartyMode.Next();
        }

        private void _SaveTeams()
        {
            if (_TeamList.Length == CPartyModeTeamSurvival.TeamCount)
            {
                _PartyMode.GameData.ProfileIdsTeam1 = _TeamList[0];
                _PartyMode.GameData.ProfileIdsTeam2 = _TeamList[1];
            }

            if (_NumPlayerTeams.Length == CPartyModeTeamSurvival.TeamCount)
            {
                _PartyMode.GameData.NumPlayerTeam1 = _NumPlayerTeams[0];
                _PartyMode.GameData.NumPlayerTeam2 = _NumPlayerTeams[1];
            }
        }
    }
}
