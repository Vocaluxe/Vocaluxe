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
using System.Threading.Tasks;
using VocaluxeWeb.Remote;

namespace VocaluxeWeb.Services
{
    /// Watches the game from the main thread and turns changes into live events:
    /// "state" when the screen or the current song changes, "queue" when the queue playlist was changed in the game.
    /// When a song starts to be sung, its wish is removed from the queue.
    public sealed class CLiveMonitor
    {
        public const string SingScreen = "Sing";
        private static readonly TimeSpan _StateInterval = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan _QueueInterval = TimeSpan.FromSeconds(1);

        private readonly IVocaluxeRemote _Remote;
        private readonly CLiveEvents _Events;
        private readonly CQueueService _Queue;
        private readonly CSongLibrary _Songs;
        private readonly Action<Exception> _OnError;
        private DateTime _NextStateCheck;
        private DateTime _NextQueueCheck;
        private RemoteState _Last;

        public CLiveMonitor(IVocaluxeRemote remote, CLiveEvents events, CQueueService queue, CSongLibrary songs, Action<Exception> onError)
        {
            _Remote = remote;
            _Events = events;
            _Queue = queue;
            _Songs = songs;
            _OnError = onError;
        }

        /// Called once per frame from the main thread. The queue is only watched while a WebApp is connected.
        public void Tick()
        {
            var now = DateTime.UtcNow;
            if (now >= _NextStateCheck)
            {
                _NextStateCheck = now + _StateInterval;
                _CheckState();
            }

            if (now >= _NextQueueCheck && _Events.ClientCount > 0)
            {
                _NextQueueCheck = now + _QueueInterval;
                if (_Queue.HasExternalChange())
                {
                    _Run(_Queue.PublishAsync);
                }
            }
        }

        private void _CheckState()
        {
            var state = _Remote.GetState();
            if (_Last != null && state.Screen == _Last.Screen && state.SongId == _Last.SongId)
            {
                return;
            }

            var startedSinging = state.Screen == SingScreen && state.SongId.HasValue && (_Last?.Screen != SingScreen || _Last.SongId != state.SongId);
            _Last = state;
            _Run(async () =>
            {
                if (_Events.ClientCount > 0)
                {
                    _Events.Publish("state", await _Songs.CreateStateAsync(state));
                }

                if (startedSinging)
                {
                    await _Queue.RemoveSungAsync(state.SongId.Value);
                }
            });
        }

        private void _Run(Func<Task> work)
        {
            Task.Run(async () =>
            {
                try
                {
                    await work();
                }
                catch (Exception e)
                {
                    _OnError?.Invoke(e);
                }
            });
        }
    }
}
