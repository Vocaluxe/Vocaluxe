using System;
using System.Collections.Generic;
using System.Linq;

namespace ServerLib
{
    internal static class CSessionControl
    {
        private static readonly Dictionary<Guid, CSession> _ActiveSessions = new Dictionary<Guid, CSession>();

        public static Guid OpenSession(string userName, string password)
        {
            if (!_ValidateUserAndPassword(userName, password))
            {
                return Guid.Empty;
            }

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
            foreach (var s in (from kv in _ActiveSessions
                               where kv.Value.ProfileId == profileId
                               select kv).ToList())
            {
                _ActiveSessions.Remove(s.Key);
            }

        }

        internal static bool RequestRight(Guid sessionId, EUserRights requestedRight)
        {
            return ((CUserRoleControl.GetUserRightsFromUserRole(_ActiveSessions[sessionId].Roles) & requestedRight) != 0);
        }

        internal static int GetUserIdFromSession(Guid sessionId)
        {
            if (!_ActiveSessions.ContainsKey(sessionId))
            {
                return -1;
            }
            return _ActiveSessions[sessionId].ProfileId;
        }
    }
}
