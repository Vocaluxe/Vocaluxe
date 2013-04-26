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
using VocaluxeLib.PartyModes;

namespace VocaluxeLib.Menu
{
    public abstract class CMenuParty : CMenu
    {
        protected IPartyMode _PartyMode;

        protected CMenuParty()
        {
            _PartyMode = new CPartyModeNone();
        }

        public void SetPartyModeID(int partyModeID)
        {
            _PartyModeID = partyModeID;
        }

        public void AssingPartyMode(IPartyMode partyMode)
        {
            _PartyMode = partyMode;
        }

        public virtual void DataToScreen(Object data) {}

        /*
        public sealed override void LoadTheme()
        {
        }

        public sealed override void ReloadTextures()
        {
        }

        public sealed override void ReloadTheme()
        {
        }

        public override void UnloadTextures()
        {
        }
        */
    }
}