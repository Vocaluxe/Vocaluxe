using System;
using VocaluxeLib.PartyModes;

namespace VocaluxeLib.Menu
{
    public abstract class CMenuParty : CMenu
    {
        protected IPartyMode _PartyMode;

        public CMenuParty()
        {
            _PartyMode = new CPartyModeNone();
        }

        public void SetPartyModeID(int partyModeID)
        {
            _PartyModeID = partyModeID;
        }

        public int GetPartyModeID()
        {
            return _PartyModeID;
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