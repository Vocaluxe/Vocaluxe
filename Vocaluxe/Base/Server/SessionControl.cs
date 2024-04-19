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
using System.Timers;

namespace Vocaluxe.Base.Server
{
    static class CSessionControl
    {
        private static readonly Dictionary<Guid, CSession> _ActiveSessions;
        private const int _UserTimeoutCheckIntervall = 120000;
        private const int _UserTimeout = 120000;

        static CSessionControl()
        {
            _ActiveSessions = new Dictionary<Guid, CSession>();
            Timer timer = new Timer(_UserTimeoutCheckIntervall) {AutoReset = true, Enabled = true};
            timer.Elapsed += _CheckForUserTimeouts;
            timer.Start();
        }

        private static void _CheckForUserTimeouts(object sender, ElapsedEventArgs e)
        {
            var sessionIdsToRemove = _ActiveSessions.Where(pair => (DateTime.Now-pair.Value.LastSeen).TotalMilliseconds > _UserTimeout)
                         .Select(pair => pair.Key)
                         .ToList();

            foreach (var sessionToRemove in sessionIdsToRemove)
            {
                InvalidateSessionByID(sessionToRemove);
            }
        }

        public static Guid OpenSession(string userName, string password)
        {
            if (!_ValidateUserAndPassword(userName, password))
                return Guid.Empty;

            Guid newId = Guid.NewGuid();
            Guid id = _GetProfileIdFormUsername(userName);
            EUserRoles roles = _GetUserRoles(id);
            CSession session = new CSession(newId, id, roles);
            //InvalidateSessions(id);
            _ActiveSessions.Add(newId, session);

            return newId;
        }

        private static bool _ValidateUserAndPassword(string userName, string password)
        {
            return CVocaluxeServer.ValidatePassword(_GetProfileIdFormUsername(userName), password);
        }

        private static Guid _GetProfileIdFormUsername(string username)
        {
            return CVocaluxeServer.GetUserIdFromUsername(username);
        }

        private static EUserRoles _GetUserRoles(Guid profileId)
        {
            return (EUserRoles)CVocaluxeServer.GetUserRole(profileId);
        }

        internal static void InvalidateSessionByProfile(Guid profileId)
        {
            foreach (KeyValuePair<Guid, CSession> s in (from kv in _ActiveSessions
                                                        where kv.Value.ProfileId == profileId
                                                        select kv).ToList())
                _ActiveSessions.Remove(s.Key);
        }

        internal static void InvalidateSessionByID(Guid sessionId)
        {
            if (_ActiveSessions.ContainsKey(sessionId))
            {
                _ActiveSessions.Remove(sessionId);
            }
        }

        internal static bool RequestRight(Guid sessionId, EUserRights requestedRight)
        {
            return ((CUserRoleControl.GetUserRightsFromUserRole(_ActiveSessions[sessionId].Roles)
                                     .HasFlag(requestedRight)));
        }

        internal static Guid GetUserIdFromSession(Guid sessionId)
        {
            if (!_ActiveSessions.ContainsKey(sessionId))
                return Guid.Empty;
            return _ActiveSessions[sessionId].ProfileId;
        }

        internal static void ResetSessionTimeout(Guid sessionId)
        {
            CSession value;
            if (_ActiveSessions.TryGetValue(sessionId, out value))
            {
                value.LastSeen = DateTime.Now;
            }
        }
    }
}