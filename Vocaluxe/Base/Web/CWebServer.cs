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
using System.IO;
using System.Linq;
using VocaluxeLib;
using VocaluxeLib.Log;
#if WIN
using Newtonsoft.Json.Linq;
#endif
#if WIN
using VocaluxeWeb.Hosting;
#endif

namespace Vocaluxe.Base.Web
{
    /// Starts and stops the embedded webserver (project VocaluxeWeb) and runs its requests on the main thread.
    /// The webserver is not available in Linux builds yet.
    static class CWebServer
    {
        private const string _CertificateFileName = "WebServer.pfx";
        private const string _SettingsFileName = "WebServer.json";
        private const string _CacheFolderName = "WebCache";
        private const string _DefaultQueuePlaylistName = "WebApp Queue";

        public static readonly CRemoteKeyboard Controller = new CRemoteKeyboard();

#if WIN
        private static CWebHost _Host;
#endif
        private static readonly List<string> _PhotosOfThisRound = new List<string>();

        public static void Init()
        {
            if (CConfig.Config.Server.ServerActive != EOffOn.TR_CONFIG_ON)
            {
                return;
            }

#if WIN
            var options = new CWebServerOptions
            {
                Port = CConfig.Config.Server.ServerPort,
                UseHttps = CConfig.Config.Server.ServerEncryption == EOffOn.TR_CONFIG_ON,
                WebRoot = Path.Combine(CSettings.ProgramFolder, CSettings.FolderNameWebsite),
                CertificatePath = Path.Combine(CSettings.DataFolder, _CertificateFileName),
                SettingsPath = Path.Combine(CSettings.DataFolder, _SettingsFileName),
                CachePath = Path.Combine(CSettings.DataFolder, _CacheFolderName),
                OnError = e => CLog.Error(e, "Webserver request failed")
            };

            var host = new CWebHost(new CVocaluxeRemote(Controller), options);
            try
            {
                host.Start();
                _Host = host;
                CLog.Information("Webserver running at {Address}", new object[] { host.Address });
            }
            catch (Exception e)
            {
                host.Dispose();
                CLog.Error(e, "Could not start the webserver on port {Port}. You may try a different port (change it in config.xml).", new object[] { options.Port }, true);
            }
#endif
        }

        public static void Close()
        {
#if WIN
            _Host?.Dispose();
            _Host = null;
#endif
        }

        /// Runs pending webserver requests. Called once per frame from the main thread.
        public static void ProcessTasks()
        {
#if WIN
            _Host?.Tick();
#endif
        }

        public static bool IsServerRunning()
        {
#if WIN
            return _Host != null && _Host.IsRunning;
#else
            return false;
#endif
        }

        public static string GetServerAddress()
        {
#if WIN
            return _Host?.Address?.AbsoluteUri ?? "";
#else
            return "";
#endif
        }

        /// Name of the playlist used as WebApp queue; read from the webserver settings also while the server is off.
        public static string GetQueuePlaylistName()
        {
#if WIN
            if (_Host != null)
            {
                return _Host.QueuePlaylistName;
            }

            try
            {
                var path = Path.Combine(CSettings.DataFolder, _SettingsFileName);
                if (File.Exists(path))
                {
                    var name = (string)JObject.Parse(File.ReadAllText(path))["QueuePlaylistName"];
                    if (!string.IsNullOrEmpty(name))
                    {
                        return name;
                    }
                }
            }
            catch (Exception e)
            {
                CLog.Warning(e, "Could not read the webserver settings");
            }
#endif
            return _DefaultQueuePlaylistName;
        }

        /// The queue with requesters and singers, or null if the webserver is not running.
        public static IList<CWebQueue.CItem> GetQueueItems()
        {
#if WIN
            return _Host?.GetQueueForGame().Select(i => new CWebQueue.CItem
            {
                SongId = i.SongId,
                GameMode = (EGameMode)i.GameMode,
                AudioMode = (EAudioMode)i.AudioMode,
                RequestedBy = i.RequestedBy,
                PlayerIds = i.PlayerIds,
                PlayerNames = i.PlayerNames
            }).ToList();
#else
            return null;
#endif
        }

        /// The queue entry at this position was started from the queue screen: it leaves the queue.
        public static void MarkQueueEntryStarted(int index)
        {
#if WIN
            if (_Host != null)
            {
                _Host.MarkQueueEntryStarted(index);
                return;
            }
#endif
            CWebQueue.Remove(index);
        }

        /// Increases whenever a playlist was changed by the webserver or the queue screen, so open views can reload it.
        public static int PlaylistVersion { get; private set; }

        public static void NotifyPlaylistChanged()
        {
            PlaylistVersion++;
        }

        /// Photos taken with the WebApp since the last call (shown as slideshow on the score screen).
        public static string[] GetPhotosOfThisRound()
        {
            lock (_PhotosOfThisRound)
            {
                var result = _PhotosOfThisRound.ToArray();
                _PhotosOfThisRound.Clear();
                return result;
            }
        }
    }
}
