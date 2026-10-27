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
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using VocaluxeWeb.Dispatching;
using VocaluxeWeb.Remote;
using VocaluxeWeb.Services;

namespace VocaluxeWeb.Hosting
{
    /// The embedded webserver (Kestrel). Serves the WebApp and the REST API.
    public sealed class CWebHost : IDisposable
    {
        private readonly IVocaluxeRemote _Remote;
        private readonly CWebServerOptions _Options;
        private IWebHost _Host;
        private CLiveMonitor _Monitor;
        private CQueueService _Queue;
        private bool _QueuePlaylistReady;

        public CMainThreadDispatcher Dispatcher { get; }

        public CWebSettingsStore Settings { get; }

        /// Address other devices in the network use to reach the server, e.g. http://192.168.0.10:3000/
        public Uri Address { get; private set; }

        public bool IsRunning => _Host != null;

        public bool UseHttps => _Options.UseHttps;

        public int Port => _Options.Port;

        public CWebHost(IVocaluxeRemote remote, CWebServerOptions options)
        {
            _Remote = remote;
            _Options = options;
            Dispatcher = new CMainThreadDispatcher(options.MainThreadTimeout);
            Settings = new CWebSettingsStore(options.SettingsPath);
        }

        public void Start()
        {
            if (_Host != null)
            {
                return;
            }

            var hostName = Dns.GetHostName();
            var lanAddresses = CNetworkAddress.GetLanAddresses();
            var certificate = _Options.UseHttps ? CCertificateProvider.GetOrCreate(_Options.CertificatePath, hostName, lanAddresses) : null;

            var host = new WebHostBuilder()
                .UseKestrel(kestrel => _ConfigureKestrel(kestrel, certificate))
                .UseContentRoot(AppDomain.CurrentDomain.BaseDirectory)
                .ConfigureServices(services => CStartup.ConfigureServices(services, this, _Remote, Dispatcher, Settings, _Options))
                .Configure(app => CStartup.Configure(app, _Options))
                .Build();

            try
            {
                host.StartAsync().GetAwaiter().GetResult();
            }
            catch
            {
                host.Dispose();
                throw;
            }

            _Host = host;
            _Monitor = (CLiveMonitor)host.Services.GetService(typeof(CLiveMonitor));
            _Queue = (CQueueService)host.Services.GetService(typeof(CQueueService));
            _QueuePlaylistReady = false;

            var shownHost = lanAddresses.Count > 0 ? lanAddresses[0].ToString() : hostName;
            Address = new UriBuilder(_Options.UseHttps ? "https" : "http", shownHost, _Options.Port).Uri;
        }

        /// Name of the playlist that is used as queue.
        public string QueuePlaylistName => Settings.Current.QueuePlaylistName;

        /// The queue with requesters and singers, for the queue screen of the game. Main thread only.
        public IList<RemoteQueueItem> GetQueueForGame()
        {
            return _Queue?.GetGameSnapshot() ?? new List<RemoteQueueItem>();
        }

        /// The queue entry at this position was started in the game and leaves the queue. Main thread only.
        public void MarkQueueEntryStarted(int index)
        {
            _Queue?.MarkStarted(index);
        }

        /// Runs pending requests and watches the game for live events. Must be called once per frame from the main thread.
        public void Tick()
        {
            Dispatcher.ProcessPending();
            if (_Queue == null || !_Remote.IsReady)
            {
                return;
            }

            try
            {
                //The playlists are loaded after the server started, so the queue playlist is created here
                if (!_QueuePlaylistReady)
                {
                    _QueuePlaylistReady = true;
                    _Queue.EnsurePlaylist();
                }

                _Monitor?.Tick();
            }
            catch (Exception e)
            {
                _Options.OnError?.Invoke(e);
            }
        }

        public void Stop()
        {
            if (_Host == null)
            {
                return;
            }

            try
            {
                _Host.StopAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
            }
            finally
            {
                _Host.Dispose();
                _Host = null;
                _Monitor = null;
                _Queue = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private void _ConfigureKestrel(KestrelServerOptions kestrel, X509Certificate2 certificate)
        {
            kestrel.AddServerHeader = false;
            kestrel.Limits.MaxRequestBodySize = 20 * 1024 * 1024; //Photos
            kestrel.Listen(IPAddress.Any, _Options.Port, listen =>
            {
                if (certificate != null)
                {
                    listen.UseHttps(certificate);
                }
            });
        }
    }
}
