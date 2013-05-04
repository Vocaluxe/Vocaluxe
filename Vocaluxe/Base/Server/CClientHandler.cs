using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Lib.Input;
using VocaluxeLib.Menu;

namespace Vocaluxe.Base.Server
{
    internal class CClientHandler
    {
        public int ConnectionID;
        public bool LoggedIn;

        public CClientHandler(int ConnectionID)
        {
            this.ConnectionID = ConnectionID;
            LoggedIn = false;
        }
    }
}
