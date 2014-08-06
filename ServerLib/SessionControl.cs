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
using System.Collections.Generic;
using System.Linq;

namespace ServerLib
{
    static class CSessionControl
    {
        private static readonly Dictionary<Guid, CSession> _ActiveSessions = new Dictionary<Guid, CSession>();

        public static Guid OpenSession(string userName, string password)
        {
            if (!_ValidateUserAndPassword(userName, password))
                return Guid.Empty;

            Guid newId = Guid.NewGuid();
            int id = _GetProfileIdFormUsername(userName);
            EUserRoles roles = _GetUserRoles(id);
            CSession session = new CSession(newId, id, roles);
            InvalidateSessions(id);
            _ActiveSessions.Add(newId, session);

            return newId;
        }

        private static bool _ValidateUserAndPassword(string userName, string password)
        {
            return CServer.ValidatePassword(_GetProfileIdFormUsername(userName), password);
        }

        private static int _GetProfileIdFormUsername(string username)
        {
            return CServer.GetUserIdFromUsername(username);
        }

        private static EUserRoles _GetUserRoles(int profileId)
        {
            return (EUserRoles)CServer.GetUserRole(profileId);
        }

        internal static void InvalidateSessions(int profileId)
        {
            foreach (KeyValuePair<Guid, CSession> s in (from kv in _ActiveSessions
                                                        where kv.Value.ProfileId == profileId
                                                        select kv).ToList())
                _ActiveSessions.Remove(s.Key);
        }

        internal static bool RequestRight(Guid sessionId, EUserRights requestedRight)
        {
            return ((CUserRoleControl.GetUserRightsFromUserRole(_ActiveSessions[sessionId].Roles)
                                     .HasFlag(requestedRight)));
        }

        internal static int GetUserIdFromSession(Guid sessionId)
        {
            if (!_ActiveSessions.ContainsKey(sessionId))
                return -1;
            return _ActiveSessions[sessionId].ProfileId;
        }
    }
}