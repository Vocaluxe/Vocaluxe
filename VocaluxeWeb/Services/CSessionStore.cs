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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using VocaluxeWeb.Remote;

namespace VocaluxeWeb.Services
{
    public sealed class CSession
    {
        /// Public id of the session (the token itself is secret).
        public string Id;
        public string Token;
        public string UserAgent;
        public string RemoteAddress;
        public DateTime Created;
        public Guid ProfileId;
        public string ProfileName;
        public ERemoteRole Role;
        public DateTime LastSeen;
    }

    /// Logged in devices. The token is kept in an HttpOnly cookie. Sessions live in memory only:

    /// after a restart of Vocaluxe the WebApp logs in again (automatically for profiles without PIN).
    public sealed class CSessionStore
    {
        public const string CookieName = "vlx_session";
        private static readonly TimeSpan _IdleTimeout = TimeSpan.FromHours(12);
        private static readonly TimeSpan _LockTime = TimeSpan.FromSeconds(30);
        private const int _MaxFailedLogins = 5;

        private readonly ConcurrentDictionary<string, CSession> _Sessions = new ConcurrentDictionary<string, CSession>();
        private readonly ConcurrentDictionary<Guid, (int Count, DateTime LockedUntil)> _FailedLogins = new ConcurrentDictionary<Guid, (int, DateTime)>();

        public CSession Create(HttpContext context, Guid profileId, string profileName, ERemoteRole role)
        {
            var session = new CSession
            {
                Id = CWebSettingsStore.CreateToken(6),
                Token = CWebSettingsStore.CreateToken(32),
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                RemoteAddress = context.Connection.RemoteIpAddress?.ToString(),
                Created = DateTime.UtcNow,
                ProfileId = profileId,
                ProfileName = profileName,
                Role = role,
                LastSeen = DateTime.UtcNow
            };
            _Sessions[session.Token] = session;
            _FailedLogins.TryRemove(profileId, out _);

            context.Response.Cookies.Append(CookieName, session.Token, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Secure = context.Request.IsHttps,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
            return session;
        }

        public CSession Get(HttpContext context)
        {
            if (!context.Request.Cookies.TryGetValue(CookieName, out var token) || !_Sessions.TryGetValue(token, out var session))
            {
                return null;
            }

            var now = DateTime.UtcNow;
            if (now - session.LastSeen > _IdleTimeout)
            {
                _Sessions.TryRemove(token, out _);
                return null;
            }

            session.LastSeen = now;
            return session;
        }

        public void Remove(HttpContext context)
        {
            if (context.Request.Cookies.TryGetValue(CookieName, out var token))
            {
                _Sessions.TryRemove(token, out _);
            }

            context.Response.Cookies.Delete(CookieName, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Secure = context.Request.IsHttps,
                Path = "/"
            });
        }

        /// All active sessions, most recently used first.
        public IList<CSession> List()
        {
            var limit = DateTime.UtcNow - _IdleTimeout;
            foreach (var expired in _Sessions.Values.Where(s => s.LastSeen < limit).ToList())
            {
                _Sessions.TryRemove(expired.Token, out _);
            }

            return _Sessions.Values.OrderByDescending(s => s.LastSeen).ToList();
        }

        /// Logs out the device with this session id.
        public bool RemoveById(string id)
        {
            var session = _Sessions.Values.FirstOrDefault(s => s.Id == id);
            return session != null && _Sessions.TryRemove(session.Token, out _);
        }

        /// Logs out all devices of a profile, e.g. after its role changed.
        public void RemoveProfile(Guid profileId)
        {
            foreach (var session in _Sessions.Values.Where(s => s.ProfileId == profileId).ToList())
            {
                _Sessions.TryRemove(session.Token, out _);
            }
        }

        public void UpdateProfileName(Guid profileId, string name)
        {
            foreach (var session in _Sessions.Values.Where(s => s.ProfileId == profileId))
            {
                session.ProfileName = name;
            }
        }

        public bool IsLocked(Guid profileId)
        {
            return _FailedLogins.TryGetValue(profileId, out var entry) && entry.LockedUntil > DateTime.UtcNow;
        }

        /// Counts a wrong PIN. After too many attempts the profile is locked for a short time.
        public void RegisterFailedLogin(Guid profileId)
        {
            _FailedLogins.AddOrUpdate(profileId,
                _ => (1, DateTime.MinValue),
                (_, entry) =>
                {
                    var count = entry.Count + 1;
                    return count >= _MaxFailedLogins ? (0, DateTime.UtcNow + _LockTime) : (count, entry.LockedUntil);
                });
        }
    }
}
