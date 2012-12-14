using System;
using System.Collections.Generic;
using System.Text;

namespace Vocaluxe.Menu.SongMenu
{
    class CSongMenuBook : CSongMenuFramework
    {
        new private Basic _Base;

        public CSongMenuBook(Basic Base)
            : base(Base)
        {
            _Base = Base;
        }
    }
}
