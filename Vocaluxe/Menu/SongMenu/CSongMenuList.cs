using System;
using System.Collections.Generic;
using System.Text;

namespace Vocaluxe.Menu.SongMenu
{
    class CSongMenuList : CSongMenuFramework
    {
        new private Base _Base;

        public CSongMenuList(Base Base)
            : base(Base)
        {
            _Base = Base;
        }
    }
}
