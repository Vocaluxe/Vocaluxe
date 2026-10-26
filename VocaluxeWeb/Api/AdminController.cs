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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VocaluxeWeb.Contracts;
using VocaluxeWeb.Dispatching;
using VocaluxeWeb.Hosting;
using VocaluxeWeb.Remote;
using VocaluxeWeb.Services;

namespace VocaluxeWeb.Api
{
    [Route("api/v1/admin")]
    [RequireAdmin]
    public sealed class AdminController : ApiControllerBase
    {
        private readonly CWebSettingsStore _Settings;
        private readonly CQueueService _Queue;
        private readonly CMainThreadDispatcher _Dispatcher;
        private readonly IVocaluxeRemote _Remote;
        private readonly CSessionStore _Sessions;
        private readonly CLiveEvents _Events;
        private readonly CWebHost _Host;

        public AdminController(CWebSettingsStore settings, CQueueService queue, CMainThreadDispatcher dispatcher, IVocaluxeRemote remote, CSessionStore sessions,
            CLiveEvents events, CWebHost host)
        {
            _Settings = settings;
            _Queue = queue;
            _Dispatcher = dispatcher;
            _Remote = remote;
            _Sessions = sessions;
            _Events = events;
            _Host = host;
        }

        [HttpGet("settings")]
        public ActionResult<AdminSettingsDto> GetSettings()
        {
            return _ToDto(_Settings.Current);
        }

        [HttpPut("settings")]
        public async Task<ActionResult<AdminSettingsDto>> UpdateSettings([FromBody] AdminSettingsDto request)
        {
            _Settings.Update(s =>
            {
                s.AllowProfileCreation = request.AllowProfileCreation.Value;
                s.QueuePlaylistName = request.QueuePlaylistName.Trim();
                s.GuestPermissions = request.GuestPermissions.Distinct().ToList();
                s.PlayerPermissions = request.PlayerPermissions.Distinct().ToList();
            });
            await _Dispatcher.InvokeAsync(_Queue.EnsurePlaylist);
            return _ToDto(_Settings.Current);
        }

        /// Address, HTTPS and connected WebApps. The port is set in config.xml, HTTPS in the game (Options - Server).
        [HttpGet("status")]
        public ActionResult<AdminStatusDto> GetStatus()
        {
            return new AdminStatusDto
            {
                Version = _Remote.Version,
                Address = _Host.Address?.AbsoluteUri ?? "",
                Port = _Host.Port,
                Https = _Host.UseHttps,
                ConnectedClients = _Events.ClientCount
            };
        }

        /// All profiles, including inactive ones.
        [HttpGet("profiles")]
        public async Task<ActionResult<IList<ProfileDto>>> GetProfiles()
        {
            var profiles = await _Dispatcher.InvokeAsync(() => _Remote.GetProfiles());
            return profiles.Select(ToDto).ToList();
        }

        /// Activates or deactivates a profile. Inactive profiles can't log in; their devices are logged out.
        [HttpPut("profiles/{id}/active")]
        public async Task<ActionResult<ProfileDto>> SetActive(Guid id, [FromBody] ActiveDto request)
        {
            if (id == CurrentSession.ProfileId && !request.Active.Value)
            {
                return Problem(StatusCodes.Status400BadRequest, "You cannot deactivate your own profile.");
            }

            var profile = await _Find(id);
            if (profile == null)
            {
                return Problem(StatusCodes.Status404NotFound, "Profile not found.");
            }

            await _Dispatcher.InvokeAsync(() => _Remote.UpdateProfile(id, new ProfileChange { IsActive = request.Active.Value }));
            if (!request.Active.Value)
            {
                _Sessions.RemoveProfile(id);
            }

            return ToDto(await _Find(id));
        }

        /// Deletes a profile in the game (including its file) and logs out its devices.
        [HttpDelete("profiles/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteProfile(Guid id)
        {
            if (id == CurrentSession.ProfileId)
            {
                return Problem(StatusCodes.Status400BadRequest, "You cannot delete your own profile.");
            }

            if (await _Find(id) == null)
            {
                return Problem(StatusCodes.Status404NotFound, "Profile not found.");
            }

            await _Dispatcher.InvokeAsync(() => _Remote.DeleteProfile(id));
            _Sessions.RemoveProfile(id);
            return NoContent();
        }

        /// Devices that are logged in.
        [HttpGet("sessions")]
        public ActionResult<IList<AdminSessionDto>> GetSessions()
        {
            return _Sessions.List().Select(s => new AdminSessionDto
            {
                Id = s.Id,
                Profile = new ProfileRefDto { Id = s.ProfileId, Name = s.ProfileName },
                Role = s.Role,
                Device = CUserAgent.Describe(s.UserAgent),
                RemoteAddress = s.RemoteAddress,
                LastSeen = s.LastSeen,
                IsCurrent = s.Id == CurrentSession.Id
            }).ToList();
        }

        /// Logs out a device.
        [HttpDelete("sessions/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult DeleteSession(string id)
        {
            if (id == CurrentSession.Id)
            {
                return Problem(StatusCodes.Status400BadRequest, "Use logout to log out this device.");
            }

            return _Sessions.RemoveById(id) ? NoContent() : (IActionResult)Problem(StatusCodes.Status404NotFound, "Session not found.");
        }

        private async Task<RemoteProfile> _Find(Guid id)
        {
            return (await _Dispatcher.InvokeAsync(() => _Remote.GetProfiles())).FirstOrDefault(p => p.Id == id);
        }

        private static AdminSettingsDto _ToDto(CWebSettings settings)
        {
            return new AdminSettingsDto
            {
                AllowProfileCreation = settings.AllowProfileCreation,
                QueuePlaylistName = settings.QueuePlaylistName,
                GuestPermissions = settings.GuestPermissions,
                PlayerPermissions = settings.PlayerPermissions
            };
        }
    }
}
