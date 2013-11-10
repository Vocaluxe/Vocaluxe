using System;

namespace ServerLib
{
    internal class CSession
    {
        readonly Guid _ID;
        public Guid Id
        {
            get { return _ID; }
        }

        readonly int _ProfileId;
        public int ProfileId
        {
            get { return _ProfileId; }
        }

        readonly EUserRoles _Roles;
        internal EUserRoles Roles
        {
            get { return _Roles; }
        }

        public CSession(Guid id, int profileId, EUserRoles roles)
        {
            this._ID = id;
            this._ProfileId = profileId;
            this._Roles = roles;
        }
    }
}
