using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerLib
{
    [Flags]
    internal enum UserRoles
    {
        AuthenticatedUser = 0x00,
        Administrator = 0x01
    }
}
