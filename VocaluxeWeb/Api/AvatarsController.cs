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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VocaluxeWeb.Contracts;
using VocaluxeWeb.Dispatching;
using VocaluxeWeb.Services;

namespace VocaluxeWeb.Api
{
    /// Avatars that come with Vocaluxe, to choose from when changing the profile picture.
    [Route("api/v1/avatars")]
    public sealed class AvatarsController : ApiControllerBase
    {
        private readonly IVocaluxeRemote _Remote;
        private readonly CMainThreadDispatcher _Dispatcher;
        private readonly CImageService _Images;

        public AvatarsController(IVocaluxeRemote remote, CMainThreadDispatcher dispatcher, CImageService images)
        {
            _Remote = remote;
            _Dispatcher = dispatcher;
            _Images = images;
        }

        [HttpGet]
        [RequireSession]
        public async Task<ActionResult<IList<AvatarDto>>> GetAll()
        {
            var avatars = await _Dispatcher.InvokeAsync(() => _Remote.GetAvatars());
            return avatars.Select(a => new AvatarDto { Id = a.Id }).ToList();
        }

        /// The avatar as JPEG. No login needed.
        [HttpGet("{id}")]
        [Produces("image/jpeg")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetImage(int id, [FromQuery] int size = 200)
        {
            var avatar = (await _Dispatcher.InvokeAsync(() => _Remote.GetAvatars())).FirstOrDefault(a => a.Id == id);
            var file = _Images.GetScaled(avatar?.Path, size, out var etag);
            if (file == null)
            {
                return NotFound();
            }

            return CImageResult.Create(this, file, etag);
        }
    }
}
