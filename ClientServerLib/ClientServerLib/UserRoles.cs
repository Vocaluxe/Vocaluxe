using System;

namespace ServerLib
{
    [Flags]
    internal enum EUserRoles
    {
        GuestUser = 0,
        AuthenticatedUser = 1,
        Administrator = 2
    }
}
