using System;
using System.Collections.Generic;
using System.Text;

namespace Vocaluxe.Menu.SongMenu
{
    class CSongMenuBook : CSongMenuFramework
    {
        new private Base _Base;

        public CSongMenuBook(Base Base)
            : base(Base)
        {
            _Base = Base;
        }
    }
}
