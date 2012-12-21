using System;
using System.Collections.Generic;
using System.Text;

namespace Vocaluxe.Menu.SongMenu
{
    class CSongMenuList : CSongMenuFramework
    {
        new private Basic _Base;

        public CSongMenuList(Basic Base, int PartyModeID)
            : base(Base, PartyModeID)
        {
            _Base = Base;
        }
    }
}
