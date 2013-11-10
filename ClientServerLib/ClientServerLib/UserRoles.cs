using System;

namespace ServerLib
{
    [Flags]
    internal enum EUserRoles
    {
        AuthenticatedUser = 0,
        Administrator = 1
    }
}
