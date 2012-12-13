using System;
using System.Collections.Generic;
using System.Text;

namespace Vocaluxe.Menu.SingNotes
{
    class CSingNotesClassic : CSingNotes
    {
        private Base _Base;

        public CSingNotesClassic(Base Base)
            : base(Base)
        {
            _Base = Base;
        }
    }
}
