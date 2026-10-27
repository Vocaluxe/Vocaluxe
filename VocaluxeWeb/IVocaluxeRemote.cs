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
using VocaluxeWeb.Remote;

namespace VocaluxeWeb
{
    /// The only view the webserver has on the game. Implemented by Vocaluxe.
    /// Methods marked as main-thread-only must be called through <see cref="Dispatching.CMainThreadDispatcher" />.
    public interface IVocaluxeRemote
    {
        /// Full version text of the running Vocaluxe. Thread-safe.
        string Version { get; }

        /// False while the game is still loading (songs, playlists). Main thread only.
        bool IsReady { get; }

        /// Current state of the game. Main thread only.
        RemoteState GetState();

        #region profiles
        /// All profiles, sorted by name. Main thread only.
        IList<RemoteProfile> GetProfiles();

        /// Creates a profile and returns its id. Main thread only.
        Guid CreateProfile(ProfileChange change);

        /// Changes the given fields of a profile. Main thread only.
        void UpdateProfile(Guid profileId, ProfileChange change);

        /// Deletes a profile including its file. Main thread only.
        void DeleteProfile(Guid profileId);

        /// Avatars that come with Vocaluxe or were put into the profile folder (not the ones uploaded with the WebApp). Main thread only.
        IList<RemoteAvatar> GetAvatars();
        #endregion

        #region songs
        /// Changes whenever songs are added or (re)loaded. Main thread only.
        int SongLibraryVersion { get; }

        /// All songs. Main thread only.
        IList<RemoteSong> GetSongs();
        #endregion

        #region playlists
        /// All playlists with their songs. Main thread only.
        IList<RemotePlaylist> GetPlaylists();

        /// Creates an empty playlist and returns its id. Main thread only.
        int CreatePlaylist(string name);

        /// Main thread only.
        void RenamePlaylist(int playlistId, string name);

        /// Main thread only.
        void DeletePlaylist(int playlistId);

        /// Replaces all songs of a playlist and saves it. Main thread only.
        void SetPlaylistEntries(int playlistId, IList<RemotePlaylistEntry> entries);
        #endregion

        #region game
        /// Starts singing a song with the given players (skipping the player selection if possible). Main thread only.
        ERemoteStartResult StartSong(RemoteStartRequest request);
        #endregion

        #region input
        /// Sends a key press to the game as if it came from a keyboard. Thread-safe.
        void SendKey(ERemoteKey key);

        /// Types text into the game. Thread-safe.
        void SendText(string text);
        #endregion
    }
}
