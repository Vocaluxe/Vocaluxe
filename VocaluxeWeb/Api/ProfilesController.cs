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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VocaluxeWeb.Contracts;
using VocaluxeWeb.Dispatching;
using VocaluxeWeb.Remote;
using VocaluxeWeb.Services;

namespace VocaluxeWeb.Api
{
    [Route("api/v1/profiles")]
    public sealed class ProfilesController : ApiControllerBase
    {
        private const int _MaxAvatarUpload = 15 * 1024 * 1024;

        private readonly IVocaluxeRemote _Remote;
        private readonly CMainThreadDispatcher _Dispatcher;
        private readonly CSessionStore _Sessions;
        private readonly CWebSettingsStore _Settings;
        private readonly CImageService _Images;

        public ProfilesController(IVocaluxeRemote remote, CMainThreadDispatcher dispatcher, CSessionStore sessions, CWebSettingsStore settings, CImageService images)
        {
            _Remote = remote;
            _Dispatcher = dispatcher;
            _Sessions = sessions;
            _Settings = settings;
            _Images = images;
        }

        /// All active profiles, for the "Who are you?" page. No login needed.
        [HttpGet]
        public async Task<ActionResult<IList<ProfileDto>>> GetAll()
        {
            var profiles = await _Dispatcher.InvokeAsync(() => _Remote.GetProfiles());
            return profiles.Where(p => p.IsActive).Select(ToDto).ToList();
        }

        /// The avatar as JPEG. No login needed.
        [HttpGet("{id}/avatar")]
        [Produces("image/jpeg")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvatar(Guid id, [FromQuery] int size = 200)
        {
            var profile = await _Find(id);
            var file = _Images.GetScaled(profile?.AvatarPath, size, out var etag);
            if (file == null)
            {
                return NotFound();
            }

            return CImageResult.Create(this, file, etag);
        }

        /// Creates a profile. Allowed without login if profile creation is enabled in the settings.
        [HttpPost]
        [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ProfileDto>> Create([FromBody] ProfileCreateDto request)
        {
            var settings = _Settings.Current;
            var session = _Sessions.Get(HttpContext);
            var allowed = session != null
                ? settings.GetPermissions(session.Role).Contains(EPermission.CreateProfiles)
                : settings.AllowProfileCreation;
            if (!allowed)
            {
                return Problem(StatusCodes.Status403Forbidden, "Creating profiles is not allowed.");
            }

            var change = new ProfileChange { Name = request.Name.Trim(), Difficulty = request.Difficulty ?? 0, Role = ERemoteRole.Player };
            if (!string.IsNullOrEmpty(request.Pin))
            {
                CPinHasher.Hash(request.Pin, out change.PinHash, out change.PinSalt);
            }

            var id = await _Dispatcher.InvokeAsync(() => _Remote.CreateProfile(change));
            return ToDto(await _Find(id));
        }

        /// Changes name, difficulty or PIN.
        [HttpPut("{id}")]
        [RequireSession]
        public async Task<ActionResult<ProfileDto>> Update(Guid id, [FromBody] ProfileUpdateDto request)
        {
            if (!_CanEdit(id))
            {
                return Problem(StatusCodes.Status403Forbidden, "You are not allowed to edit this profile.");
            }

            var change = new ProfileChange { Name = request.Name?.Trim(), Difficulty = request.Difficulty };
            if (request.Pin != null)
            {
                if (request.Pin.Length == 0)
                {
                    change.ClearPin = true;
                }
                else if (request.Pin.Length < 4)
                {
                    return Problem(StatusCodes.Status400BadRequest, "The PIN needs at least 4 characters.");
                }
                else
                {
                    CPinHasher.Hash(request.Pin, out change.PinHash, out change.PinSalt);
                }
            }

            return await _Apply(id, change);
        }

        /// Sets a new avatar. The body is the picture (JPEG or PNG), it is cropped to a square.
        [HttpPut("{id}/avatar")]
        [RequireSession]
        [Consumes("image/jpeg", "image/png", "image/webp", "application/octet-stream")]
        public async Task<ActionResult<ProfileDto>> UpdateAvatar(Guid id)
        {
            if (!_CanEdit(id))
            {
                return Problem(StatusCodes.Status403Forbidden, "You are not allowed to edit this profile.");
            }

            byte[] jpeg;
            try
            {
                using (var buffer = new MemoryStream())
                {
                    await Request.Body.CopyToAsync(buffer);
                    if (buffer.Length == 0 || buffer.Length > _MaxAvatarUpload)
                    {
                        return Problem(StatusCodes.Status400BadRequest, "Please send a picture up to 15 MB.");
                    }

                    jpeg = CImageService.CreateAvatar(buffer.ToArray());
                }
            }
            catch (ArgumentException)
            {
                return Problem(StatusCodes.Status400BadRequest, "This picture format is not supported.");
            }

            return await _Apply(id, new ProfileChange { AvatarJpeg = jpeg });
        }

        /// Uses one of the avatars of the game, see GET /api/v1/avatars.
        [HttpPut("{id}/game-avatar")]
        [RequireSession]
        public async Task<ActionResult<ProfileDto>> SelectAvatar(Guid id, [FromBody] AvatarSelectDto request)
        {
            if (!_CanEdit(id))
            {
                return Problem(StatusCodes.Status403Forbidden, "You are not allowed to edit this profile.");
            }

            var avatars = await _Dispatcher.InvokeAsync(() => _Remote.GetAvatars());
            if (avatars.All(a => a.Id != request.AvatarId.Value))
            {
                return Problem(StatusCodes.Status404NotFound, "Avatar not found.");
            }

            return await _Apply(id, new ProfileChange { AvatarId = request.AvatarId });
        }

        /// Changes the role of a profile (admins only). Logs out the devices of that profile.
        [HttpPut("{id}/role")]
        [RequireAdmin]
        public async Task<ActionResult<ProfileDto>> UpdateRole(Guid id, [FromBody] RoleUpdateDto request)
        {
            if (id == CurrentSession.ProfileId && request.Role.Value != ERemoteRole.Admin)
            {
                return Problem(StatusCodes.Status400BadRequest, "You cannot remove your own admin role.");
            }

            var result = await _Apply(id, new ProfileChange { Role = request.Role });
            _Sessions.RemoveProfile(id);
            return result;
        }

        private bool _CanEdit(Guid id)
        {
            return HasPermission(EPermission.EditAllProfiles) || (id == CurrentSession.ProfileId && HasPermission(EPermission.EditOwnProfile));
        }

        private async Task<ActionResult<ProfileDto>> _Apply(Guid id, ProfileChange change)
        {
            if (await _Find(id) == null)
            {
                return Problem(StatusCodes.Status404NotFound, "Profile not found.");
            }

            await _Dispatcher.InvokeAsync(() => _Remote.UpdateProfile(id, change));
            var profile = await _Find(id);
            _Sessions.UpdateProfileName(id, profile.Name);
            return ToDto(profile);
        }

        private async Task<RemoteProfile> _Find(Guid id)
        {
            return (await _Dispatcher.InvokeAsync(() => _Remote.GetProfiles())).FirstOrDefault(p => p.Id == id);
        }
    }
}
