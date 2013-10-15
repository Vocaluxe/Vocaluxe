using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerLib
{
    internal class Session
    {
        Guid id;
        public Guid Id
        {
            get { return id; }
        }

        int profileId;
        public int ProfileId
        {
            get { return profileId; }
        }

        UserRoles roles;
        internal UserRoles Roles
        {
            get { return roles; }
        }

        public Session(Guid id, int profileId, UserRoles roles)
        {
            this.id = id;
            this.profileId = profileId;
            this.roles = roles;
        }
    }
}
