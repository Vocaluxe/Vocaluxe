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

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VocaluxeWeb.Contracts;
using VocaluxeWeb.Dispatching;
using VocaluxeWeb.Services;

namespace VocaluxeWeb.Api
{
    [Route("api/v1")]
    public sealed class ServerController : ApiControllerBase
    {
        private readonly IVocaluxeRemote _Remote;
        private readonly CMainThreadDispatcher _Dispatcher;
        private readonly CWebSettingsStore _Settings;
        private readonly CSongLibrary _Songs;

        public ServerController(IVocaluxeRemote remote, CMainThreadDispatcher dispatcher, CWebSettingsStore settings, CSongLibrary songs)
        {
            _Remote = remote;
            _Dispatcher = dispatcher;
            _Settings = settings;
            _Songs = songs;
        }

        /// Name and version of the server and how logging in works.
        [HttpGet("server")]
        public ActionResult<ServerInfoDto> GetServerInfo()
        {
            var settings = _Settings.Current;
            return new ServerInfoDto
            {
                Name = "Vocaluxe",
                Version = _Remote.Version,
                AllowProfileCreation = settings.AllowProfileCreation,
                QueuePlaylistName = settings.QueuePlaylistName
            };
        }

        /// Current state of the game.
        [HttpGet("state")]
        [RequireSession]
        public async Task<ActionResult<StateDto>> GetState()
        {
            return await _Songs.CreateStateAsync(await _Dispatcher.InvokeAsync(() => _Remote.GetState()));
        }
    }
}
