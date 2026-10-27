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

namespace VocaluxeWeb.Hosting
{
    public sealed class CWebServerOptions
    {
        public int Port = 3000;

        /// Serve HTTPS with a self-signed certificate instead of plain HTTP.
        public bool UseHttps;

        /// Folder with the built WebApp (index.html, assets).
        public string WebRoot;

        /// Where the self-signed certificate is stored (only used with HTTPS).
        public string CertificatePath;

        /// JSON file with the webserver settings (permissions, join token, ...).
        public string SettingsPath;

        /// Folder for scaled covers and avatars.
        public string CachePath;

        /// How long a request waits for the game's main thread.
        public TimeSpan MainThreadTimeout = TimeSpan.FromSeconds(5);

        /// Called for unhandled exceptions while processing a request.
        public Action<Exception> OnError;
    }
}
