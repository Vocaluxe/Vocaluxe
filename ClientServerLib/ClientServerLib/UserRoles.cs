using System;

namespace ServerLib
{
    [Flags]
    internal enum EUserRoles
    {
        TR_USERROLE_GUEST = 0,
        TR_USERROLE_NORMAL = 1,
        TR_USERROLE_ADMIN = 2,
        TR_USERROLE_KEYBOARDUSER = 4,
        TR_USERROLE_ADDSONGSUSER = 8,
        TR_USERROLE_PLAYLISTEDITOR = 16,
        TR_USERROLE_PROFILEEDITOR = 32
    }
}
