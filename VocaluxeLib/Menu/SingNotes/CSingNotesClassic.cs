using System;
using System.Collections.Generic;
using System.Text;

namespace Vocaluxe.Menu.SingNotes
{
    class CSingNotesClassic : CSingNotes
    {
        private Basic _Base;

        public CSingNotesClassic(Basic Base)
            : base(Base)
        {
            _Base = Base;
        }
    }
}
