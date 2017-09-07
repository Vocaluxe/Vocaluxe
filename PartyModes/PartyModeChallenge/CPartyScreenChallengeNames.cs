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

namespace VocaluxeLib.PartyModes.Challenge
{
    // ReSharper disable UnusedMember.Global
    public class CPartyScreenChallengeNames : CMenuPartyNameSelection
        // ReSharper restore UnusedMember.Global
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 1; }
        }

        private new CPartyModeChallenge _PartyMode;

        public override void Init()
        {
            base.Init();
            _PartyMode = (CPartyModeChallenge)base._PartyMode;
            _AllowChangePlayerNum = false;
            _Teams = false;
        }

        public override void OnShow()
        {
            base.OnShow();
            SetPartyModeData(_PartyMode.GameData.NumPlayer);
            while (_PartyMode.GameData.ProfileIDs.Count > _NumPlayer)
                _PartyMode.GameData.ProfileIDs.RemoveAt(_PartyMode.GameData.ProfileIDs.Count - 1);

            List<int>[] ids = new List<int>[] {_PartyMode.GameData.ProfileIDs};
            SetPartyModeProfiles(ids);
        }

        public override void Back()
        {
            if (_TeamList.Length == 1)
                _PartyMode.GameData.ProfileIDs = _TeamList[0];
            _PartyMode.Back();
        }

        public override void Next()
        {
            if (_TeamList.Length == 1)
                _PartyMode.GameData.ProfileIDs = _TeamList[0];
            _PartyMode.Next();
        }
    }
}