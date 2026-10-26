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
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace VocaluxeWeb.Services
{
    /// Pushes events to all connected WebApps using Server-Sent Events (text/event-stream).
    /// Data only flows from the server to the clients; changes are made with the REST API.
    /// Browsers reconnect on their own when the connection drops.
    public sealed class CLiveEvents
    {
        private static readonly TimeSpan _Heartbeat = TimeSpan.FromSeconds(15);

        private static readonly JsonSerializerSettings _Json = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = { new StringEnumConverter() }
        };

        private sealed class CSubscriber
        {
            public readonly ConcurrentQueue<string> Messages = new ConcurrentQueue<string>();
            public readonly SemaphoreSlim Signal = new SemaphoreSlim(0);
        }

        private readonly ConcurrentDictionary<Guid, CSubscriber> _Subscribers = new ConcurrentDictionary<Guid, CSubscriber>();

        public int ClientCount => _Subscribers.Count;

        public void Publish(string type, object data)
        {
            var message = _Format(type, data);
            foreach (var subscriber in _Subscribers.Values)
            {
                subscriber.Messages.Enqueue(message);
                subscriber.Signal.Release();
            }
        }

        /// Streams events to one client until it disconnects or <paramref name="isValid" /> returns false (e.g. after logout).
        public async Task StreamAsync(HttpResponse response, IEnumerable<(string Type, object Data)> initial, Func<bool> isValid, CancellationToken cancel)
        {
            response.ContentType = "text/event-stream";
            response.Headers["Cache-Control"] = "no-cache";
            response.Headers["X-Accel-Buffering"] = "no";

            var id = Guid.NewGuid();
            var subscriber = new CSubscriber();
            _Subscribers[id] = subscriber;
            try
            {
                //Ask the browser to wait 3 s before reconnecting
                await _WriteAsync(response, "retry: 3000\n\n", cancel);
                foreach (var (type, data) in initial)
                {
                    await _WriteAsync(response, _Format(type, data), cancel);
                }

                while (!cancel.IsCancellationRequested)
                {
                    if (!await subscriber.Signal.WaitAsync(_Heartbeat, cancel))
                    {
                        if (!isValid())
                        {
                            return;
                        }

                        await _WriteAsync(response, ": ping\n\n", cancel);
                        continue;
                    }

                    var builder = new StringBuilder();
                    while (subscriber.Messages.TryDequeue(out var message))
                    {
                        builder.Append(message);
                    }

                    if (builder.Length > 0)
                    {
                        await _WriteAsync(response, builder.ToString(), cancel);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                //The client disconnected
            }
            catch (Exception) when (cancel.IsCancellationRequested)
            {
                //Writing failed because the client disconnected
            }
            finally
            {
                _Subscribers.TryRemove(id, out _);
            }
        }

        private static string _Format(string type, object data)
        {
            return "event: " + type + "\ndata: " + JsonConvert.SerializeObject(data, _Json) + "\n\n";
        }

        private static async Task _WriteAsync(HttpResponse response, string text, CancellationToken cancel)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            await response.Body.WriteAsync(bytes, 0, bytes.Length, cancel);
            await response.Body.FlushAsync(cancel);
        }
    }
}
