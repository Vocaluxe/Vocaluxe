using System;
using System.Collections.Generic;
using System.Text;

namespace Vocaluxe.Menu.SongMenu
{
    class CSongMenuList : CSongMenuFramework
    {
        new private Basic _Base;

        public CSongMenuList(Basic Base)
            : base(Base)
        {
            _Base = Base;
        }
    }
}
