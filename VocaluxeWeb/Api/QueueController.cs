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
    /// Song wishes. Everybody gets one song per round. Entries can be changed and removed by their requester
    /// or by people allowed to manage the queue; only admins can clear the whole queue.
    [Route("api/v1/queue")]
    [RequireSession]
    public sealed class QueueController : ApiControllerBase
    {
        public const int MaxPlayers = 6;

        private readonly CQueueService _Queue;
        private readonly CSongLibrary _Songs;
        private readonly CLiveEvents _Events;
        private readonly IVocaluxeRemote _Remote;
        private readonly CMainThreadDispatcher _Dispatcher;

        public QueueController(CQueueService queue, CSongLibrary songs, CLiveEvents events, IVocaluxeRemote remote, CMainThreadDispatcher dispatcher)
        {
            _Queue = queue;
            _Songs = songs;
            _Events = events;
            _Remote = remote;
            _Dispatcher = dispatcher;
        }

        [HttpGet]
        public async Task<ActionResult<IList<QueueEntryDto>>> Get()
        {
            return Ok(await _Queue.GetAsync());
        }

        /// Wishes a song. Without game mode the first mode the song supports is used.
        [HttpPost]
        [RequireSession(EPermission.AddToQueue)]
        public async Task<ActionResult<IList<QueueEntryDto>>> Add([FromBody] QueueAddDto request)
        {
            var song = await _Songs.GetAsync(request.SongId.Value);
            if (song == null)
            {
                return Problem(StatusCodes.Status404NotFound, "Song not found.");
            }

            var dto = CSongLibrary.ToDto(song);
            var gameMode = request.GameMode ?? dto.GameModes.DefaultIfEmpty(ESongGameMode.Normal).First();
            var audioMode = request.AudioMode ?? ESongAudioMode.Normal;
            var error = _Validate(dto, gameMode, audioMode);
            var players = await _ResolvePlayers(request.PlayerIds);
            if (error != null || players == null)
            {
                return Problem(StatusCodes.Status400BadRequest, error ?? "Unknown players.");
            }

            var result = await _Queue.AddAsync(dto.Id, gameMode, audioMode, players, CurrentSession);
            _Events.Publish("notification", new NotificationDto
            {
                Type = "queueAdded",
                Profile = new ProfileRefDto { Id = CurrentSession.ProfileId, Name = CurrentSession.ProfileName },
                Song = dto
            });
            return Ok(result);
        }

        /// Adds the songs of a playlist (all or the given positions) to the queue, keeping their game and audio mode.
        [HttpPost("import")]
        [RequireSession(EPermission.AddToQueue)]
        public async Task<ActionResult<IList<QueueEntryDto>>> Import([FromBody] QueueImportDto request)
        {
            var playlist = (await _Dispatcher.InvokeAsync(() => _Remote.GetPlaylists())).FirstOrDefault(p => p.Id == request.PlaylistId.Value);
            if (playlist == null)
            {
                return Problem(StatusCodes.Status404NotFound, "Playlist not found.");
            }

            var entries = request.Positions == null
                ? playlist.Entries.ToList()
                : request.Positions.Where(p => p >= 0 && p < playlist.Entries.Count).Distinct().Select(p => playlist.Entries[p]).ToList();
            if (entries.Count == 0)
            {
                return Problem(StatusCodes.Status400BadRequest, "No songs to add.");
            }

            var result = await _Queue.AddManyAsync(entries, CurrentSession);
            var first = await _Songs.GetDtoAsync(entries[0].SongId);
            _Events.Publish("notification", new NotificationDto
            {
                Type = entries.Count == 1 ? "queueAdded" : "queueAddedMany",
                Profile = new ProfileRefDto { Id = CurrentSession.ProfileId, Name = CurrentSession.ProfileName },
                Song = first,
                Count = entries.Count
            });
            return Ok(result);
        }

        /// Starts singing this entry in the game right away, with its singers. Fails with 409 while the game is busy (e.g. singing).
        [HttpPost("{id}/start")]
        [RequireSession(EPermission.StartSongs)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<IList<QueueEntryDto>>> Start(string id)
        {
            var entry = (await _Queue.GetAsync()).FirstOrDefault(e => e.Id == id);
            if (entry == null)
            {
                return Problem(StatusCodes.Status404NotFound, "Queue entry not found.");
            }

            var started = await _Dispatcher.InvokeAsync(() =>
            {
                var index = _Queue.IndexOf(id);
                if (index < 0)
                {
                    return false;
                }

                var result = _Remote.StartSong(new RemoteStartRequest
                {
                    SongId = entry.Song.Id,
                    GameMode = (int)entry.GameMode,
                    AudioMode = (int)entry.AudioMode,
                    PlayerIds = entry.Players.Select(p => p.Id).ToList()
                });
                //With player selection the entry stays until singing really starts (the selection can be cancelled)
                if (result == ERemoteStartResult.Singing)
                {
                    _Queue.MarkStarted(index);
                }

                return result != ERemoteStartResult.Busy;
            });
            if (!started)
            {
                return Problem(StatusCodes.Status409Conflict, "Vocaluxe cannot start a song right now.");
            }

            return Ok(await _Queue.GetAsync());
        }

        /// Changes game mode, audio mode or players of a wish.
        [HttpPut("{id}")]
        public async Task<ActionResult<IList<QueueEntryDto>>> Update(string id, [FromBody] QueueUpdateDto request)
        {
            var entry = (await _Queue.GetAsync()).FirstOrDefault(e => e.Id == id);
            if (entry == null)
            {
                return Problem(StatusCodes.Status404NotFound, "Queue entry not found.");
            }

            var error = _Validate(entry.Song, request.GameMode ?? entry.GameMode, request.AudioMode ?? entry.AudioMode);
            var players = request.PlayerIds == null ? null : await _ResolvePlayers(request.PlayerIds);
            if (error != null || (request.PlayerIds != null && players == null))
            {
                return Problem(StatusCodes.Status400BadRequest, error ?? "Unknown players.");
            }

            return Ok(await _Queue.UpdateAsync(id, request.GameMode, request.AudioMode, players, CurrentSession, HasPermission(EPermission.ManageQueue)));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<IList<QueueEntryDto>>> Remove(string id)
        {
            return Ok(await _Queue.RemoveAsync(id, CurrentSession, HasPermission(EPermission.ManageQueue)));
        }

        [HttpPut("{id}/position")]
        [RequireSession(EPermission.ManageQueue)]
        public async Task<ActionResult<IList<QueueEntryDto>>> Move(string id, [FromBody] PositionDto request)
        {
            return Ok(await _Queue.MoveAsync(id, request.Position.Value));
        }

        /// Removes all wishes (admins only).
        [HttpDelete]
        [RequireAdmin]
        public async Task<ActionResult<IList<QueueEntryDto>>> Clear()
        {
            return Ok(await _Queue.ClearAsync());
        }

        private static string _Validate(SongDto song, ESongGameMode gameMode, ESongAudioMode audioMode)
        {
            if (song.GameModes.Count > 0 && !song.GameModes.Contains(gameMode))
            {
                return "The song does not support this game mode.";
            }

            if (audioMode != ESongAudioMode.Normal && !song.AudioModes.Contains(audioMode))
            {
                return "The song does not support this audio mode.";
            }

            return null;
        }

        /// The profiles with the given ids, or null if one is unknown or there are too many.
        private async Task<List<ProfileRefDto>> _ResolvePlayers(IList<Guid> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return new List<ProfileRefDto>();
            }

            if (ids.Count > MaxPlayers)
            {
                return null;
            }

            var profiles = await _Dispatcher.InvokeAsync(() => _Remote.GetProfiles());
            var result = ids.Distinct().Select(id => profiles.FirstOrDefault(p => p.Id == id)).ToList();
            return result.Contains(null) ? null : result.Select(p => new ProfileRefDto { Id = p.Id, Name = p.Name }).ToList();
        }
    }
}
