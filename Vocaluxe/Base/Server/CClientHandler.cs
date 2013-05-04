using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vocaluxe.Base.Server
{
    internal class CClientHandler
    {
        public int ConnectionID;
        public bool LoggedIn;

        public CClientHandler(int ConnectionID)
        {
            this.ConnectionID = ConnectionID;
        }
    }
}
