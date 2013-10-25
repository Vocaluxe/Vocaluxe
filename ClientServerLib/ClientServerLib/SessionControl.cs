using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerLib
{
    internal static class SessionControl
    {
        private static Dictionary<Guid, Session> activeSessions = new Dictionary<Guid, Session>();

        public static Guid openSession(string userName, string password)
        {
            if (!validateUserAndPassword(userName, password))
            {
                return Guid.Empty;
            }

            Guid newId = Guid.NewGuid();
            int id = getProfileIdFormUsername(userName);
            UserRoles roles = getUserRoles(id);
            Session session = new Session(newId, id, roles);
            invalidateSessions(id);
            activeSessions.Add(newId, session);

            return newId;
        }

        private static bool validateUserAndPassword(string userName, string password)
        {
            return CServer.ValidatePassword(getProfileIdFormUsername(userName), password);
        }

        private static int getProfileIdFormUsername(string username)
        {
            return CServer.GetUserIdFromUsername(username);
        }

        private static UserRoles getUserRoles(int profileId)
        {
            return (UserRoles)CServer.GetUserRole(profileId);
        }

        internal static void invalidateSessions(int profileId)
        {
            foreach (var s in (from kv in activeSessions
                               where kv.Value.ProfileId == profileId
                               select kv).ToList())
            {
                activeSessions.Remove(s.Key);
            }

        }

        internal static bool requestRight(Guid sessionId, UserRights requestedRight)
        {
            return ((UserRoleControl.getUserRightsFromUserRole(activeSessions[sessionId].Roles) & requestedRight) != 0);
        }

        internal static int getUserIdFromSession(Guid sessionId)
        {
            if (!activeSessions.ContainsKey(sessionId))
            {
                return -1;
            }
            return activeSessions[sessionId].ProfileId;
        }
    }
}
