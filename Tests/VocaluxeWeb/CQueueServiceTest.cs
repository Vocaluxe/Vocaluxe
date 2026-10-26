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
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using VocaluxeWeb;
using VocaluxeWeb.Contracts;
using VocaluxeWeb.Dispatching;
using VocaluxeWeb.Remote;
using VocaluxeWeb.Services;

namespace Tests.VocaluxeWeb
{
    [TestFixture]
    public class CQueueServiceTest
    {
        private sealed class CFakeRemote : IVocaluxeRemote
        {
            public readonly List<RemotePlaylist> Playlists = new List<RemotePlaylist>();

            public string Version => "test";
            public bool IsReady => true;
            public int SongLibraryVersion => 1;

            public RemoteState GetState()
            {
                return new RemoteState { Screen = "Main" };
            }

            public IList<RemoteProfile> GetProfiles()
            {
                return new List<RemoteProfile>();
            }

            public Guid CreateProfile(ProfileChange change)
            {
                throw new NotSupportedException();
            }

            public void UpdateProfile(Guid profileId, ProfileChange change) { }

            public void DeleteProfile(Guid profileId) { }

            public IList<RemoteAvatar> GetAvatars()
            {
                return new List<RemoteAvatar>();
            }

            public IList<RemoteSong> GetSongs()
            {
                return Enumerable.Range(0, 20)
                                 .Select(i => new RemoteSong
                                 {
                                     Id = i,
                                     Title = "Song " + i,
                                     Artist = "Artist",
                                     Languages = new string[0],
                                     Genres = new string[0],
                                     Editions = new string[0],
                                     GameModes = new List<int>(),
                                     AudioModes = new List<int>()
                                 })
                                 .ToList();
            }

            public IList<RemotePlaylist> GetPlaylists()
            {
                return Playlists.Select(p => new RemotePlaylist { Id = p.Id, Name = p.Name, Entries = p.Entries.ToList() }).ToList();
            }

            public int CreatePlaylist(string name)
            {
                var playlist = new RemotePlaylist { Id = Playlists.Count + 1, Name = name, Entries = new List<RemotePlaylistEntry>() };
                Playlists.Add(playlist);
                return playlist.Id;
            }

            public void RenamePlaylist(int playlistId, string name) { }

            public void DeletePlaylist(int playlistId) { }

            public void SetPlaylistEntries(int playlistId, IList<RemotePlaylistEntry> entries)
            {
                Playlists.First(p => p.Id == playlistId).Entries = entries.ToList();
            }

            public ERemoteStartResult StartSong(RemoteStartRequest request)
            {
                return ERemoteStartResult.Singing;
            }

            public void SendKey(ERemoteKey key) { }

            public void SendText(string text) { }
        }

        private readonly CSession _Anna = new CSession { ProfileId = Guid.NewGuid(), ProfileName = "Anna" };
        private readonly CSession _Ben = new CSession { ProfileId = Guid.NewGuid(), ProfileName = "Ben" };
        private readonly CSession _Cleo = new CSession { ProfileId = Guid.NewGuid(), ProfileName = "Cleo" };
        private CFakeRemote _Remote;
        private CQueueService _Queue;
        private CancellationTokenSource _Stop;
        private Task _MainThread;

        [SetUp]
        public void SetUp()
        {
            _Remote = new CFakeRemote();
            var dispatcher = new CMainThreadDispatcher(TimeSpan.FromSeconds(5));
            _Queue = new CQueueService(_Remote, dispatcher, new CWebSettingsStore(null), new CSongLibrary(_Remote, dispatcher), new CLiveEvents());
            _Stop = new CancellationTokenSource();
            _MainThread = Task.Run(async () =>
            {
                while (!_Stop.IsCancellationRequested)
                {
                    dispatcher.ProcessPending();
                    await Task.Delay(1);
                }
            });
        }

        [TearDown]
        public void TearDown()
        {
            _Stop.Cancel();
            _MainThread.Wait();
        }

        [Test]
        public void TestFairOrder()
        {
            _Add(1, _Anna);
            _Add(2, _Anna);
            _Add(3, _Anna);
            _Add(4, _Ben);
            _Add(5, _Ben);
            var queue = _Add(6, _Cleo);

            Assert.That(_Requesters(queue), Is.EqualTo(new[] { "Anna", "Ben", "Cleo", "Anna", "Ben", "Anna" }));
            Assert.That(queue.Select(e => e.Song.Id), Is.EqualTo(new[] { 1, 4, 6, 2, 5, 3 }));
            Assert.That(queue.Select(e => e.Position), Is.EqualTo(Enumerable.Range(0, 6)));
        }

        [Test]
        public void TestAddMany()
        {
            _Add(1, _Ben);
            var queue = _Queue.AddManyAsync(new[] { 2, 3, 4 }.Select(id => new RemotePlaylistEntry { SongId = id }).ToList(), _Anna).Result;

            Assert.That(_Requesters(queue), Is.EqualTo(new[] { "Ben", "Anna", "Anna", "Anna" }));
            Assert.That(_Requesters(_Add(5, _Ben)), Is.EqualTo(new[] { "Ben", "Anna", "Anna", "Ben", "Anna" }));
        }

        [Test]
        public void TestPlaylistIsUpdated()
        {
            _Add(7, _Anna);
            _Add(8, _Ben);

            var playlist = _Remote.Playlists.Single(p => p.Name == "WebApp Queue");
            Assert.That(playlist.Entries.Select(e => e.SongId), Is.EqualTo(new[] { 7, 8 }));
        }

        [Test]
        public void TestSongsAddedInTheGame()
        {
            _Add(1, _Anna);
            _Remote.Playlists[0].Entries.Add(new RemotePlaylistEntry { SongId = 9 });

            var queue = _Add(2, _Anna);
            Assert.That(queue.Select(e => e.Song.Id), Is.EqualTo(new[] { 1, 9, 2 }));
            Assert.That(queue[1].RequestedBy, Is.Null);
        }

        [Test]
        public void TestOnlyOwnerOrManagerMayRemove()
        {
            var entry = _Add(1, _Anna)[0];

            Exception error = null;
            try
            {
                _Queue.RemoveAsync(entry.Id, _Ben, false).Wait();
            }
            catch (AggregateException e)
            {
                error = e.InnerException;
            }

            Assert.That(error, Is.InstanceOf<CQueueService.CForbiddenException>());
            Assert.That(_Queue.GetAsync().Result, Has.Count.EqualTo(1));

            Assert.That(_Queue.RemoveAsync(entry.Id, _Ben, true).Result, Is.Empty);
            _Add(2, _Anna);
            Assert.That(_Queue.RemoveAsync(_Queue.GetAsync().Result[0].Id, _Anna, false).Result, Is.Empty);
        }

        [Test]
        public void TestMove()
        {
            _Add(1, _Anna);
            _Add(2, _Ben);
            var queue = _Add(3, _Cleo);

            queue = _Queue.MoveAsync(queue[2].Id, 0).Result;
            Assert.That(queue.Select(e => e.Song.Id), Is.EqualTo(new[] { 3, 1, 2 }));
            queue = _Queue.MoveAsync(queue[0].Id, 99).Result;
            Assert.That(queue.Select(e => e.Song.Id), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void TestRemoveSung()
        {
            _Add(1, _Anna);
            _Add(2, _Ben);

            var queue = _Queue.RemoveSungAsync(1).Result;
            Assert.That(queue.Select(e => e.Song.Id), Is.EqualTo(new[] { 2 }));
            Assert.That(_Queue.RemoveSungAsync(5).Result, Has.Count.EqualTo(1));
        }

        private IList<QueueEntryDto> _Add(int songId, CSession session)
        {
            return _Queue.AddAsync(songId, ESongGameMode.Normal, ESongAudioMode.Normal, null, session).Result;
        }

        private static IEnumerable<string> _Requesters(IEnumerable<QueueEntryDto> queue)
        {
            return queue.Select(e => e.RequestedBy?.Name);
        }
    }
}
