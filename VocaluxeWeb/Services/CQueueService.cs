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
using VocaluxeWeb.Contracts;
using VocaluxeWeb.Dispatching;
using VocaluxeWeb.Remote;

namespace VocaluxeWeb.Services
{
    /// The queue is a normal playlist of the game (name from the settings), so it can also be used in the song screen.
    /// Game mode and audio mode are stored in the playlist; who wished a song and who should sing it is only known to the webserver.
    /// New wishes are inserted fairly: everybody gets one song per round.
    /// Every change is pushed to all WebApps as live event "queue".
    public sealed class CQueueService
    {
        private sealed class CMeta
        {
            public string Id;
            public int SongId;
            public ProfileRefDto RequestedBy;
            public List<ProfileRefDto> Players = new List<ProfileRefDto>();
        }

        private sealed class CItem
        {
            public RemotePlaylistEntry Entry;
            public CMeta Meta;
        }

        public sealed class CForbiddenException : Exception { }

        private readonly IVocaluxeRemote _Remote;
        private readonly CMainThreadDispatcher _Dispatcher;
        private readonly CWebSettingsStore _Settings;
        private readonly CSongLibrary _Songs;
        private readonly CLiveEvents _Events;
        private readonly object _Lock = new object();
        private List<CMeta> _Metas = new List<CMeta>();
        private string _Fingerprint;
        private int? _StartedSongId;

        public CQueueService(IVocaluxeRemote remote, CMainThreadDispatcher dispatcher, CWebSettingsStore settings, CSongLibrary songs, CLiveEvents events)
        {
            _Remote = remote;
            _Dispatcher = dispatcher;
            _Settings = settings;
            _Songs = songs;
            _Events = events;
        }

        public async Task<IList<QueueEntryDto>> GetAsync()
        {
            return await _ToDtosAsync((await _ChangeAsync(items => false)).Items);
        }

        public Task<IList<QueueEntryDto>> AddAsync(int songId, ESongGameMode gameMode, ESongAudioMode audioMode, List<ProfileRefDto> players, CSession session)
        {
            return _ChangeAndPublishAsync(items =>
            {
                var position = _FairPosition(items, session.ProfileId);
                items.Insert(position, new CItem
                {
                    Entry = new RemotePlaylistEntry { SongId = songId, GameMode = (int)gameMode, AudioMode = (int)audioMode },
                    Meta = new CMeta
                    {
                        Id = _NewId(),
                        SongId = songId,
                        RequestedBy = new ProfileRefDto { Id = session.ProfileId, Name = session.ProfileName },
                        Players = players ?? new List<ProfileRefDto>()
                    }
                });
                return true;
            });
        }

        /// Adds several songs (e.g. from a playlist) for one person; each one is placed fairly.
        public Task<IList<QueueEntryDto>> AddManyAsync(IList<RemotePlaylistEntry> songs, CSession session)
        {
            return _ChangeAndPublishAsync(items =>
            {
                foreach (var song in songs)
                {
                    var position = _FairPosition(items, session.ProfileId);
                    items.Insert(position, new CItem
                    {
                        Entry = new RemotePlaylistEntry { SongId = song.SongId, GameMode = song.GameMode, AudioMode = song.AudioMode },
                        Meta = new CMeta
                        {
                            Id = _NewId(),
                            SongId = song.SongId,
                            RequestedBy = new ProfileRefDto { Id = session.ProfileId, Name = session.ProfileName }
                        }
                    });
                }

                return songs.Count > 0;
            });
        }

        public Task<IList<QueueEntryDto>> UpdateAsync(string id, ESongGameMode? gameMode, ESongAudioMode? audioMode, List<ProfileRefDto> players, CSession session, bool canManage)
        {
            return _ChangeAndPublishAsync(items =>
            {
                var item = _Find(items, id);
                _CheckOwner(item, session, canManage);
                if (gameMode.HasValue)
                {
                    item.Entry.GameMode = (int)gameMode.Value;
                }

                if (audioMode.HasValue)
                {
                    item.Entry.AudioMode = (int)audioMode.Value;
                }

                if (players != null)
                {
                    item.Meta.Players = players;
                }

                return true;
            });
        }

        public Task<IList<QueueEntryDto>> RemoveAsync(string id, CSession session, bool canManage)
        {
            return _ChangeAndPublishAsync(items =>
            {
                var item = _Find(items, id);
                _CheckOwner(item, session, canManage);
                items.Remove(item);
                return true;
            });
        }

        public Task<IList<QueueEntryDto>> MoveAsync(string id, int newPosition)
        {
            return _ChangeAndPublishAsync(items =>
            {
                var item = _Find(items, id);
                items.Remove(item);
                items.Insert(Math.Max(0, Math.Min(newPosition, items.Count)), item);
                return true;
            });
        }

        public Task<IList<QueueEntryDto>> ClearAsync()
        {
            return _ChangeAndPublishAsync(items =>
            {
                var changed = items.Count > 0;
                items.Clear();
                return changed;
            });
        }

        /// A song started in the game: its entry leaves the queue.
        /// Skipped if the entry was already removed because it was started from the queue (see <see cref="MarkStarted" />).
        public Task<IList<QueueEntryDto>> RemoveSungAsync(int songId)
        {
            return _ChangeAndPublishAsync(items =>
            {
                lock (_Lock)
                {
                    if (_StartedSongId == songId)
                    {
                        _StartedSongId = null;
                        return false;
                    }
                }

                var item = items.FirstOrDefault(i => i.Entry.SongId == songId);
                return item != null && items.Remove(item);
            });
        }

        /// Creates the queue playlist if it does not exist yet, so it is available in the game right away. Main thread only.
        public void EnsurePlaylist()
        {
            var name = _Settings.Current.QueuePlaylistName;
            if (_Remote.GetPlaylists().All(p => p.Name != name))
            {
                _Remote.CreatePlaylist(name);
            }
        }

        /// The queue with requesters and singers, for the queue screen of the game. Main thread only.
        public IList<RemoteQueueItem> GetGameSnapshot()
        {
            var name = _Settings.Current.QueuePlaylistName;
            var entries = _Remote.GetPlaylists().FirstOrDefault(p => p.Name == name)?.Entries ?? new List<RemotePlaylistEntry>();
            lock (_Lock)
            {
                var items = _Match(entries);
                _Metas = items.Select(i => i.Meta).ToList();
                return items.Select(i => new RemoteQueueItem
                {
                    SongId = i.Entry.SongId,
                    GameMode = i.Entry.GameMode,
                    AudioMode = i.Entry.AudioMode,
                    RequestedBy = i.Meta.RequestedBy?.Name,
                    PlayerIds = i.Meta.Players.Select(p => p.Id).ToList(),
                    PlayerNames = i.Meta.Players.Select(p => p.Name).ToList()
                }).ToList();
            }
        }

        /// Position of the entry in the queue, -1 if it is gone. Main thread only.
        public int IndexOf(string id)
        {
            var name = _Settings.Current.QueuePlaylistName;
            var entries = _Remote.GetPlaylists().FirstOrDefault(p => p.Name == name)?.Entries ?? new List<RemotePlaylistEntry>();
            lock (_Lock)
            {
                return _Match(entries).FindIndex(i => i.Meta.Id == id);
            }
        }

        /// The entry at this position was started: it leaves the queue right away. Main thread only.
        public void MarkStarted(int index)
        {
            var name = _Settings.Current.QueuePlaylistName;
            var playlist = _Remote.GetPlaylists().FirstOrDefault(p => p.Name == name);
            if (playlist == null)
            {
                return;
            }

            lock (_Lock)
            {
                var items = _Match(playlist.Entries);
                if (index < 0 || index >= items.Count)
                {
                    return;
                }

                _StartedSongId = items[index].Entry.SongId;
                items.RemoveAt(index);
                _Remote.SetPlaylistEntries(playlist.Id, items.Select(i => i.Entry).ToList());
                _Metas = items.Select(i => i.Meta).ToList();
                _Fingerprint = _GetFingerprint(items.Select(i => i.Entry));
            }

            Task.Run(PublishAsync);
        }

        /// Called regularly on the main thread: true if the queue playlist was changed in the game.
        public bool HasExternalChange()
        {
            var name = _Settings.Current.QueuePlaylistName;
            var playlist = _Remote.GetPlaylists().FirstOrDefault(p => p.Name == name);
            var fingerprint = _GetFingerprint(playlist?.Entries ?? new List<RemotePlaylistEntry>());
            lock (_Lock)
            {
                return _Fingerprint != null && fingerprint != _Fingerprint;
            }
        }

        /// Sends the current queue to all WebApps.
        public async Task PublishAsync()
        {
            _Events.Publish("queue", await GetAsync());
        }

        private async Task<IList<QueueEntryDto>> _ChangeAndPublishAsync(Func<List<CItem>, bool> change)
        {
            var (items, changed) = await _ChangeAsync(change);
            var dtos = await _ToDtosAsync(items);
            if (changed)
            {
                _Events.Publish("queue", dtos);
            }

            return dtos;
        }

        /// Reads the queue playlist and applies a change in one go on the main thread, so the game cannot interfere.
        private Task<(List<CItem> Items, bool Changed)> _ChangeAsync(Func<List<CItem>, bool> change)
        {
            var name = _Settings.Current.QueuePlaylistName;
            return _Dispatcher.InvokeAsync(() =>
            {
                var playlist = _Remote.GetPlaylists().FirstOrDefault(p => p.Name == name);
                var entries = playlist?.Entries ?? new List<RemotePlaylistEntry>();

                lock (_Lock)
                {
                    var items = _Match(entries);
                    var changed = change(items);
                    if (changed)
                    {
                        var playlistId = playlist?.Id ?? _Remote.CreatePlaylist(name);
                        _Remote.SetPlaylistEntries(playlistId, items.Select(i => i.Entry).ToList());
                    }

                    _Metas = items.Select(i => i.Meta).ToList();
                    _Fingerprint = _GetFingerprint(items.Select(i => i.Entry));
                    return (items, changed);
                }
            });
        }

        /// Assigns the known metadata to the current playlist entries, which may have been changed in the game.
        private List<CItem> _Match(IList<RemotePlaylistEntry> entries)
        {
            var result = new List<CItem>(entries.Count);
            var next = 0;
            foreach (var entry in entries)
            {
                var found = _Metas.FindIndex(next, m => m.SongId == entry.SongId);
                CMeta meta;
                if (found >= 0)
                {
                    meta = _Metas[found];
                    next = found + 1;
                }
                else
                {
                    meta = new CMeta { Id = _NewId(), SongId = entry.SongId };
                }

                result.Add(new CItem { Entry = new RemotePlaylistEntry { SongId = entry.SongId, GameMode = entry.GameMode, AudioMode = entry.AudioMode }, Meta = meta });
            }

            return result;
        }

        private static CItem _Find(List<CItem> items, string id)
        {
            return items.FirstOrDefault(i => i.Meta.Id == id) ?? throw new KeyNotFoundException("Queue entry not found");
        }

        private static void _CheckOwner(CItem item, CSession session, bool canManage)
        {
            if (!canManage && item.Meta.RequestedBy?.Id != session.ProfileId)
            {
                throw new CForbiddenException();
            }
        }

        /// The n-th wish of a person goes behind everybody's n-th wish.
        /// Entries without requester count as a wish of their own.
        private static int _FairPosition(IList<CItem> items, Guid profileId)
        {
            var round = items.Count(i => i.Meta.RequestedBy?.Id == profileId);
            var seen = new Dictionary<Guid, int>();
            var position = 0;
            for (var i = 0; i < items.Count; i++)
            {
                var entryRound = 0;
                var requester = items[i].Meta.RequestedBy;
                if (requester != null)
                {
                    seen.TryGetValue(requester.Id, out entryRound);
                    seen[requester.Id] = entryRound + 1;
                }

                if (entryRound <= round)
                {
                    position = i + 1;
                }
            }

            return position;
        }

        private static string _GetFingerprint(IEnumerable<RemotePlaylistEntry> entries)
        {
            return string.Join(",", entries.Select(e => e.SongId + ":" + e.GameMode + ":" + e.AudioMode));
        }

        private static string _NewId()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 12);
        }

        private async Task<IList<QueueEntryDto>> _ToDtosAsync(List<CItem> items)
        {
            var result = new List<QueueEntryDto>(items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                result.Add(new QueueEntryDto
                {
                    Id = item.Meta.Id,
                    Position = i,
                    Song = await _Songs.GetDtoAsync(item.Entry.SongId),
                    RequestedBy = item.Meta.RequestedBy,
                    GameMode = (ESongGameMode)item.Entry.GameMode,
                    AudioMode = (ESongAudioMode)item.Entry.AudioMode,
                    Players = item.Meta.Players
                });
            }

            return result;
        }
    }
}
