using System;
using System.Collections.Generic;
using System.Text;

namespace Vocaluxe.Menu
{
    public abstract class CMenuParty : CMenu
    {
        public CMenuParty()
        {
        }

        public void SetPartyModeID(int PartyModeID)
        {
            _PartyModeID = PartyModeID;
        }

        public int GetPartyModeID()
        {
            return _PartyModeID;
        }

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
