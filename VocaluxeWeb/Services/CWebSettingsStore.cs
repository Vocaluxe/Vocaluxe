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
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VocaluxeWeb.Contracts;
using VocaluxeWeb.Remote;

namespace VocaluxeWeb.Services
{
    /// Settings of the webserver, stored as JSON next to the game's data. Admins always have every permission.
    public sealed class CWebSettings
    {
        public bool AllowProfileCreation = true;
        public string QueuePlaylistName = "WebApp Queue";

        [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
        public List<EPermission> GuestPermissions = new List<EPermission> { EPermission.AddToQueue };

        [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
        public List<EPermission> PlayerPermissions = new List<EPermission>
        {
            EPermission.AddToQueue,
            EPermission.UseKeyboard,
            EPermission.EditPlaylists,
            EPermission.EditOwnProfile,
            EPermission.CreateProfiles
        };

        public CWebSettings Clone()
        {
            var clone = (CWebSettings)MemberwiseClone();
            clone.GuestPermissions = GuestPermissions.ToList();
            clone.PlayerPermissions = PlayerPermissions.ToList();
            return clone;
        }

        public IList<EPermission> GetPermissions(ERemoteRole role)
        {
            switch (role)
            {
                case ERemoteRole.Admin:
                    return (EPermission[])Enum.GetValues(typeof(EPermission));
                case ERemoteRole.Player:
                    return PlayerPermissions;
                default:
                    return GuestPermissions;
            }
        }
    }

    public sealed class CWebSettingsStore
    {
        private readonly string _Path;
        private readonly object _Lock = new object();
        private CWebSettings _Settings;

        public CWebSettingsStore(string path)
        {
            _Path = path;
            _Settings = _Load();
        }

        /// A copy of the current settings.
        public CWebSettings Current
        {
            get
            {
                lock (_Lock)
                {
                    return _Settings.Clone();
                }
            }
        }

        public void Update(Action<CWebSettings> change)
        {
            lock (_Lock)
            {
                var settings = _Settings.Clone();
                change(settings);
                _Settings = settings;
                _Save(settings);
            }
        }

        private CWebSettings _Load()
        {
            CWebSettings settings = null;
            if (!string.IsNullOrEmpty(_Path) && File.Exists(_Path))
            {
                try
                {
                    settings = JsonConvert.DeserializeObject<CWebSettings>(File.ReadAllText(_Path));
                }
                catch (Exception)
                {
                    //Broken file, start with the default settings
                }
            }

            return settings ?? new CWebSettings();
        }

        private void _Save(CWebSettings settings)
        {
            if (string.IsNullOrEmpty(_Path))
            {
                return;
            }

            var directory = Path.GetDirectoryName(_Path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_Path, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }

        /// Random URL-safe token with the given number of bytes of entropy.
        public static string CreateToken(int bytes)
        {
            var buffer = new byte[bytes];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }

            return Convert.ToBase64String(buffer).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
