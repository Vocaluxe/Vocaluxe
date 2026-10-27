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
using VocaluxeWeb.Dispatching;
using VocaluxeWeb.Services;

namespace VocaluxeWeb.Api
{
    /// Live events as Server-Sent Events (use EventSource in the browser).
    /// Events: "state" (StateDto), "queue" (QueueEntryDto[]), "notification" (NotificationDto).
    /// The current state and queue are sent right after connecting.
    [Route("api/v1/events")]
    [RequireSession]
    public sealed class EventsController : ApiControllerBase
    {
        private readonly CLiveEvents _Events;
        private readonly IVocaluxeRemote _Remote;
        private readonly CMainThreadDispatcher _Dispatcher;
        private readonly CSongLibrary _Songs;
        private readonly CQueueService _Queue;
        private readonly CSessionStore _Sessions;

        public EventsController(CLiveEvents events, IVocaluxeRemote remote, CMainThreadDispatcher dispatcher, CSongLibrary songs, CQueueService queue, CSessionStore sessions)
        {
            _Events = events;
            _Remote = remote;
            _Dispatcher = dispatcher;
            _Songs = songs;
            _Queue = queue;
            _Sessions = sessions;
        }

        [HttpGet]
        [Produces("text/event-stream")]
        public async Task Stream()
        {
            var state = await _Songs.CreateStateAsync(await _Dispatcher.InvokeAsync(() => _Remote.GetState()));
            var queue = await _Queue.GetAsync();
            await _Events.StreamAsync(Response, new (string, object)[] { ("state", state), ("queue", queue) }, () => _Sessions.Get(HttpContext) != null,
                HttpContext.RequestAborted);
        }
    }
}
