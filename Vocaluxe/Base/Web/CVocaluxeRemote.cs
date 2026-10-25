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
using System.Windows.Forms;
using Vocaluxe.Lib.Input;
using VocaluxeLib;
using VocaluxeLib.Menu;
using VocaluxeLib.Profile;
using VocaluxeLib.Songs;
using VocaluxeWeb;
using VocaluxeWeb.Remote;

namespace Vocaluxe.Base.Web
{
    /// Gives the webserver access to the game. Everything except <see cref="Version" />, <see cref="SendKey" /> and <see cref="SendText" /> runs on the main thread.
    class CVocaluxeRemote : IVocaluxeRemote
    {
        private const EUserRole _RoleMask = EUserRole.TR_USERROLE_GUEST | EUserRole.TR_USERROLE_NORMAL | EUserRole.TR_USERROLE_ADMIN;
        private const string _UploadPrefix = "WebApp_";

        private readonly CControllerFramework _Keyboard;

        public CVocaluxeRemote(CControllerFramework keyboard)
        {
            _Keyboard = keyboard;
        }

        public string Version { get; } = CSettings.FullVersion;

        public bool IsReady => CGraphics.CurrentScreenType != EScreen.Load;

        private void _CheckReady()
        {
            if (!IsReady)
            {
                throw new TimeoutException("Vocaluxe is still loading");
            }
        }

        public RemoteState GetState()
        {
            return new RemoteState { Screen = CGraphics.CurrentScreenType.ToString(), SongId = CGame.GetSong()?.Id };
        }

        #region profiles
        public IList<RemoteProfile> GetProfiles()
        {
            return CProfiles.GetProfiles().Select(p => new RemoteProfile
            {
                Id = p.Id,
                Name = p.PlayerName,
                Role = _ToRemoteRole(p.UserRole),
                Difficulty = (int)p.Difficulty,
                IsActive = p.Active == EOffOn.TR_CONFIG_ON,
                AvatarPath = p.Avatar == null || string.IsNullOrEmpty(p.Avatar.FileName) ? null : Path.GetFullPath(p.Avatar.FileName),
                PinHash = p.PasswordHash,
                PinSalt = p.PasswordSalt
            }).ToList();
        }

        public Guid CreateProfile(ProfileChange change)
        {
            var profile = new CProfile { Active = EOffOn.TR_CONFIG_ON, UserRole = EUserRole.TR_USERROLE_NORMAL };
            _Apply(profile, change);
            CProfiles.AddProfile(profile);
            CProfiles.Update();
            return profile.Id;
        }

        public void UpdateProfile(Guid profileId, ProfileChange change)
        {
            var existing = CProfiles.GetProfile(profileId);
            if (existing == null)
            {
                throw new KeyNotFoundException("Profile not found");
            }

            var profile = new CProfile
            {
                Id = existing.Id,
                FilePath = existing.FilePath,
                PlayerName = existing.PlayerName,
                Difficulty = existing.Difficulty,
                UserRole = existing.UserRole,
                Active = existing.Active,
                Avatar = existing.Avatar,
                PasswordHash = existing.PasswordHash,
                PasswordSalt = existing.PasswordSalt
            };
            _Apply(profile, change);
            CProfiles.EditProfile(profile);
            CProfiles.Update();
            profile.SaveProfile();
        }

        public void DeleteProfile(Guid profileId)
        {
            CProfiles.DeleteProfile(profileId);
            CProfiles.Update();
        }

        public IList<RemoteAvatar> GetAvatars()
        {
            return (CProfiles.GetAvatars() ?? Enumerable.Empty<CAvatar>())
                   .Where(a => !string.IsNullOrEmpty(a.FileName) && !Path.GetFileName(a.FileName).StartsWith(_UploadPrefix))
                   .OrderBy(a => Path.GetFileName(a.FileName), StringComparer.OrdinalIgnoreCase)
                   .Select(a => new RemoteAvatar { Id = a.Id, Path = Path.GetFullPath(a.FileName) })
                   .ToList();
        }

        private static void _Apply(CProfile profile, ProfileChange change)
        {
            if (!string.IsNullOrEmpty(change.Name))
            {
                profile.PlayerName = change.Name;
            }

            if (change.Difficulty.HasValue)
            {
                profile.Difficulty = (EGameDifficulty)change.Difficulty.Value;
            }

            if (change.IsActive.HasValue)
            {
                profile.Active = change.IsActive.Value ? EOffOn.TR_CONFIG_ON : EOffOn.TR_CONFIG_OFF;
            }

            if (change.Role.HasValue)
            {
                profile.UserRole = (profile.UserRole & ~_RoleMask) | _ToUserRole(change.Role.Value);
            }

            if (change.ClearPin)
            {
                profile.PasswordHash = null;
                profile.PasswordSalt = null;
            }
            else if (change.PinHash != null)
            {
                profile.PasswordHash = change.PinHash;
                profile.PasswordSalt = change.PinSalt;
            }

            if (change.AvatarId.HasValue)
            {
                var avatar = CProfiles.GetAvatars()?.FirstOrDefault(a => a.Id == change.AvatarId.Value);
                if (avatar != null)
                {
                    profile.Avatar = avatar;
                }
            }

            if (change.AvatarJpeg != null)
            {
                var avatar = _CreateAvatar(change.AvatarJpeg);
                if (avatar != null)
                {
                    profile.Avatar = avatar;
                }
            }
        }

        private static CAvatar _CreateAvatar(byte[] jpeg)
        {
            var folder = CConfig.ProfileFolders[0];
            Directory.CreateDirectory(folder);
            var fileName = Path.Combine(folder, _UploadPrefix + Guid.NewGuid().ToString("N") + ".jpg");
            File.WriteAllBytes(fileName, jpeg);

            var avatar = CAvatar.GetAvatar(fileName);
            if (avatar != null)
            {
                CProfiles.AddAvatar(avatar);
            }

            return avatar;
        }

        private static ERemoteRole _ToRemoteRole(EUserRole role)
        {
            if (role.HasFlag(EUserRole.TR_USERROLE_ADMIN))
            {
                return ERemoteRole.Admin;
            }

            return role.HasFlag(EUserRole.TR_USERROLE_NORMAL) ? ERemoteRole.Player : ERemoteRole.Guest;
        }

        private static EUserRole _ToUserRole(ERemoteRole role)
        {
            switch (role)
            {
                case ERemoteRole.Admin:
                    return EUserRole.TR_USERROLE_ADMIN;
                case ERemoteRole.Player:
                    return EUserRole.TR_USERROLE_NORMAL;
                default:
                    return EUserRole.TR_USERROLE_GUEST;
            }
        }
        #endregion

        #region songs
        public int SongLibraryVersion => CSongs.AllSongs.Count * 2 + (CSongs.SongsLoaded ? 1 : 0);

        public IList<RemoteSong> GetSongs()
        {
            _CheckReady();
            return CSongs.AllSongs.Select(s => new RemoteSong
            {
                Id = s.Id,
                Title = s.Title,
                Artist = s.Artist,
                Year = s.Year,
                Languages = s.Languages.ToList(),
                Genres = s.Genres.ToList(),
                Editions = s.Editions.ToList(),
                IsDuet = s.IsDuet,
                GameModes = s.AvailableGameModes.Select(m => (int)m).ToList(),
                AudioModes = _GetAudioModes(s),
                CoverPath = _GetFullPath(s, s.Cover),
                AudioPath = _GetFullPath(s, s.Audio),
                PreviewStart = s.Preview.Source == EDataSource.None ? (float?)null : s.Preview.StartTime,
                NumPlayed = s.NumPlayed,
                DateAdded = s.DateAdded
            }).ToList();
        }

        private static IList<int> _GetAudioModes(CSong song)
        {
            var modes = new List<int>();
            if (song.HasInstrumental())
            {
                modes.Add((int)EAudioMode.TR_AUDIOMODE_NORMAL);
                modes.Add((int)EAudioMode.TR_AUDIOMODE_INSTRUMENTAL);
                if (song.HasVocals())
                {
                    modes.Add((int)EAudioMode.TR_AUDIOMODE_VOCALS);
                }

                modes.Add((int)EAudioMode.TR_AUDIOMODE_KARAOKE);
            }

            return modes;
        }

        private static string _GetFullPath(CSong song, string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                return null;
            }

            try
            {
                return Path.GetFullPath(Path.Combine(song.Folder, file));
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
        #endregion

        #region playlists
        public IList<RemotePlaylist> GetPlaylists()
        {
            _CheckReady();
            return CPlaylists.Playlists.Select(p => new RemotePlaylist
            {
                Id = p.Id,
                Name = p.Name,
                Entries = p.Songs.Select(s => new RemotePlaylistEntry { SongId = s.SongId, GameMode = (int)s.GameMode, AudioMode = (int)s.AudioMode }).ToList()
            }).ToList();
        }

        public int CreatePlaylist(string name)
        {
            _CheckReady();
            var id = CPlaylists.NewPlaylist(name);
            CPlaylists.Save(id);
            return id;
        }

        public void RenamePlaylist(int playlistId, string name)
        {
            CPlaylists.SetName(playlistId, name);
            CPlaylists.Save(playlistId);
        }

        public void DeletePlaylist(int playlistId)
        {
            CPlaylists.Delete(playlistId);
        }

        public void SetPlaylistEntries(int playlistId, IList<RemotePlaylistEntry> entries)
        {
            var playlist = CPlaylists.Get(playlistId);
            if (playlist == null)
            {
                throw new KeyNotFoundException("Playlist not found");
            }

            playlist.Songs = entries.Select(e => new CPlaylistSong(e.SongId, (EGameMode)e.GameMode) { AudioMode = (EAudioMode)e.AudioMode }).ToList();
            playlist.Save();
            CWebServer.NotifyPlaylistChanged();
        }
        #endregion

        #region game
        public ERemoteStartResult StartSong(RemoteStartRequest request)
        {
            if (!CWebQueue.Start(request.SongId, (EGameMode)request.GameMode, (EAudioMode)request.AudioMode, request.PlayerIds, false, out var singsNow))
            {
                return ERemoteStartResult.Busy;
            }

            return singsNow ? ERemoteStartResult.Singing : ERemoteStartResult.PlayerSelection;
        }
        #endregion

        #region input
        public void SendKey(ERemoteKey key)
        {
            _Keyboard.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, false, false, false, char.MinValue, _ToKeys(key)));
        }

        public void SendText(string text)
        {
            foreach (var chr in text)
            {
                _Keyboard.AddKeyEvent(new SKeyEvent(ESender.Keyboard, false, char.IsUpper(chr), false, true, chr, _CharToKeys(chr)));
            }
        }

        private static Keys _CharToKeys(char chr)
        {
            var upper = char.ToUpperInvariant(chr);
            if (upper >= 'A' && upper <= 'Z')
            {
                return Keys.A + (upper - 'A');
            }

            if (chr >= '0' && chr <= '9')
            {
                return Keys.D0 + (chr - '0');
            }

            return chr == ' ' ? Keys.Space : Keys.None;
        }

        private static Keys _ToKeys(ERemoteKey key)
        {
            switch (key)
            {
                case ERemoteKey.Up:
                    return Keys.Up;
                case ERemoteKey.Down:
                    return Keys.Down;
                case ERemoteKey.Left:
                    return Keys.Left;
                case ERemoteKey.Right:
                    return Keys.Right;
                case ERemoteKey.Enter:
                    return Keys.Return;
                case ERemoteKey.Escape:
                    return Keys.Escape;
                case ERemoteKey.Tab:
                    return Keys.Tab;
                case ERemoteKey.Backspace:
                    return Keys.Back;
                case ERemoteKey.Space:
                    return Keys.Space;
                case ERemoteKey.PageUp:
                    return Keys.PageUp;
                case ERemoteKey.PageDown:
                    return Keys.PageDown;
                default:
                    return Keys.None;
            }
        }
        #endregion
    }
}
