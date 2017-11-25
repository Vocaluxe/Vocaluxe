#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;

namespace Vocaluxe.Base.Server
{
    class CSession
    {
        private readonly Guid _ID;
        public Guid Id
        {
            get { return _ID; }
        }

        private readonly Guid _ProfileId;
        public Guid ProfileId
        {
            get { return _ProfileId; }
        }

        private readonly EUserRoles _Roles;
        internal EUserRoles Roles
        {
            get { return _Roles; }
        }
       
        private DateTime _LastSeen;
        public DateTime LastSeen
        {
            get { return _LastSeen; }
            internal set { _LastSeen = value; }
        }

        public CSession(Guid id, Guid profileId, EUserRoles roles)
        {
            _ID = id;
            _ProfileId = profileId;
            _Roles = roles;
            _LastSeen = DateTime.Now;
        }
    }
}