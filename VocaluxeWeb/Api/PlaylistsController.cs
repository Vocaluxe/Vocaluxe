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
using VocaluxeWeb.Remote;
using VocaluxeWeb.Services;

namespace VocaluxeWeb.Api
{
    /// The game's playlists. The queue playlist is not listed here, see /queue.
    [Route("api/v1/playlists")]
    [RequireSession]
    public sealed class PlaylistsController : ApiControllerBase
    {
        private const string _PlaylistNotFound = "Playlist not found.";

        private readonly IVocaluxeRemote _Remote;
        private readonly CMainThreadDispatcher _Dispatcher;
        private readonly CSongLibrary _Songs;
        private readonly CWebSettingsStore _Settings;

        public PlaylistsController(IVocaluxeRemote remote, CMainThreadDispatcher dispatcher, CSongLibrary songs, CWebSettingsStore settings)
        {
            _Remote = remote;
            _Dispatcher = dispatcher;
            _Songs = songs;
            _Settings = settings;
        }

        [HttpGet]
        public async Task<ActionResult<IList<PlaylistSummaryDto>>> GetAll()
        {
            var queueName = _Settings.Current.QueuePlaylistName;
            var playlists = await _Dispatcher.InvokeAsync(() => _Remote.GetPlaylists());
            return playlists.Where(p => p.Name != queueName)
                            .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
                            .Select(p => new PlaylistSummaryDto { Id = p.Id, Name = p.Name, SongCount = p.Entries.Count })
                            .ToList();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PlaylistDto>> Get(int id)
        {
            var playlist = await _Find(id);
            return playlist == null ? Problem(StatusCodes.Status404NotFound, _PlaylistNotFound) : await _ToDto(playlist);
        }

        [HttpPost]
        [RequireSession(EPermission.EditPlaylists)]
        public async Task<ActionResult<PlaylistDto>> Create([FromBody] PlaylistNameDto request)
        {
            var name = request.Name.Trim();
            var invalid = _CheckName(name);
            if (invalid != null)
            {
                return invalid;
            }

            var id = await _Dispatcher.InvokeAsync(() => _Remote.CreatePlaylist(name));
            return await _ToDto(await _Find(id));
        }

        [HttpPut("{id}")]
        [RequireSession(EPermission.EditPlaylists)]
        public async Task<ActionResult<PlaylistDto>> Rename(int id, [FromBody] PlaylistNameDto request)
        {
            if (await _Find(id) == null)
            {
                return Problem(StatusCodes.Status404NotFound, _PlaylistNotFound);
            }

            var name = request.Name.Trim();
            var invalid = _CheckName(name);
            if (invalid != null)
            {
                return invalid;
            }

            await _Dispatcher.InvokeAsync(() => _Remote.RenamePlaylist(id, name));
            return await _ToDto(await _Find(id));
        }

        [HttpDelete("{id}")]
        [RequireSession(EPermission.DeletePlaylists)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(int id)
        {
            if (await _Find(id) == null)
            {
                return Problem(StatusCodes.Status404NotFound, _PlaylistNotFound);
            }

            await _Dispatcher.InvokeAsync(() => _Remote.DeletePlaylist(id));
            return NoContent();
        }

        [HttpPost("{id}/songs")]
        [RequireSession(EPermission.EditPlaylists)]
        public async Task<ActionResult<PlaylistDto>> AddSong(int id, [FromBody] SongRefDto request)
        {
            if (await _Songs.GetAsync(request.SongId.Value) == null)
            {
                return Problem(StatusCodes.Status404NotFound, "Song not found.");
            }

            return await _Change(id, entries =>
            {
                var position = Math.Min(request.Position ?? entries.Count, entries.Count);
                entries.Insert(position, new RemotePlaylistEntry { SongId = request.SongId.Value });
            });
        }

        [HttpDelete("{id}/songs/{position}")]
        [RequireSession(EPermission.EditPlaylists)]
        public Task<ActionResult<PlaylistDto>> RemoveSong(int id, int position)
        {
            return _Change(id, entries =>
            {
                if (position < 0 || position >= entries.Count)
                {
                    throw new KeyNotFoundException("No song at this position");
                }

                entries.RemoveAt(position);
            });
        }

        [HttpPut("{id}/songs/{position}/position")]
        [RequireSession(EPermission.EditPlaylists)]
        public Task<ActionResult<PlaylistDto>> MoveSong(int id, int position, [FromBody] PositionDto request)
        {
            return _Change(id, entries =>
            {
                if (position < 0 || position >= entries.Count)
                {
                    throw new KeyNotFoundException("No song at this position");
                }

                var entry = entries[position];
                entries.RemoveAt(position);
                entries.Insert(Math.Min(request.Position.Value, entries.Count), entry);
            });
        }

        /// Reads, changes and saves the playlist in one step on the main thread.
        private async Task<ActionResult<PlaylistDto>> _Change(int id, Action<List<RemotePlaylistEntry>> change)
        {
            var queueName = _Settings.Current.QueuePlaylistName;
            var playlist = await _Dispatcher.InvokeAsync(() =>
            {
                var current = _Remote.GetPlaylists().FirstOrDefault(p => p.Id == id && p.Name != queueName);
                if (current == null)
                {
                    return null;
                }

                var entries = current.Entries.ToList();
                change(entries);
                _Remote.SetPlaylistEntries(id, entries);
                current.Entries = entries;
                return current;
            });

            return playlist == null ? Problem(StatusCodes.Status404NotFound, _PlaylistNotFound) : await _ToDto(playlist);
        }

        /// The name of the queue playlist can't be used for other playlists.
        private ObjectResult _CheckName(string name)
        {
            if (name.Length == 0)
            {
                return Problem(StatusCodes.Status400BadRequest, "Please enter a name.");
            }

            return string.Equals(name, _Settings.Current.QueuePlaylistName, StringComparison.OrdinalIgnoreCase)
                       ? Problem(StatusCodes.Status409Conflict, "This name is reserved for the queue.")
                       : null;
        }

        private async Task<RemotePlaylist> _Find(int id)
        {
            var queueName = _Settings.Current.QueuePlaylistName;
            return (await _Dispatcher.InvokeAsync(() => _Remote.GetPlaylists())).FirstOrDefault(p => p.Id == id && p.Name != queueName);
        }

        private async Task<ActionResult<PlaylistDto>> _ToDto(RemotePlaylist playlist)
        {
            var songs = new List<PlaylistEntryDto>(playlist.Entries.Count);
            for (var i = 0; i < playlist.Entries.Count; i++)
            {
                songs.Add(new PlaylistEntryDto { Position = i, Song = await _Songs.GetDtoAsync(playlist.Entries[i].SongId) });
            }

            return new PlaylistDto { Id = playlist.Id, Name = playlist.Name, Songs = songs };
        }
    }
}
