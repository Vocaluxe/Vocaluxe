using System;

namespace ServerLib
{
    [Flags]
    internal enum EUserRoles
    {
        AuthenticatedUser = 0x00,
        Administrator = 0x01
    }
}
