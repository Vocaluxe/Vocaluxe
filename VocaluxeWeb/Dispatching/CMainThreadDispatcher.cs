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
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace VocaluxeWeb.Dispatching
{
    /// Runs work from request threads on the game's main thread.
    /// Requests enqueue work, the game calls <see cref="ProcessPending" /> once per frame.
    public sealed class CMainThreadDispatcher
    {
        private readonly ConcurrentQueue<Action> _Queue = new ConcurrentQueue<Action>();
        private readonly TimeSpan _Timeout;

        public CMainThreadDispatcher(TimeSpan timeout)
        {
            _Timeout = timeout;
        }

        public Task<T> InvokeAsync<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            var cts = new CancellationTokenSource(_Timeout);
            var registration = cts.Token.Register(() => tcs.TrySetException(new TimeoutException("The game did not respond in time.")));

            _Queue.Enqueue(() =>
            {
                registration.Dispose();
                cts.Dispose();
                if (tcs.Task.IsCompleted)
                {
                    return; //Timed out, the caller is gone
                }

                try
                {
                    tcs.TrySetResult(func());
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
            });

            return tcs.Task;
        }

        public Task InvokeAsync(Action action)
        {
            return InvokeAsync<object>(() =>
            {
                action();
                return null;
            });
        }

        /// Executes all queued work. Must be called from the main thread.
        public void ProcessPending()
        {
            while (_Queue.TryDequeue(out var action))
            {
                action();
            }
        }
    }
}
