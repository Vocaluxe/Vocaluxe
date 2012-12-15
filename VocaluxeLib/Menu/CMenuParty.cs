using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.PartyModes;

namespace Vocaluxe.Menu
{
    public abstract class CMenuParty : CMenu
    {
        protected IPartyMode _PartyMode;

        public CMenuParty()
        {
            _PartyMode = new CPartyModeNone();
        }

        public void SetPartyModeID(int PartyModeID)
        {
            _PartyModeID = PartyModeID;
        }

        public int GetPartyModeID()
        {
            return _PartyModeID;
        }

        public void AssingPartyMode(IPartyMode PartyMode)
        {
            _PartyMode = PartyMode;
        }

        public virtual void DataToScreen(Object Data)
        {
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
