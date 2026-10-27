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

// Data exchanged between the game and the webserver. Never sent to clients directly (contains file paths and PIN hashes).
namespace VocaluxeWeb.Remote
{
    public enum ERemoteRole
    {
        Guest,
        Player,
        Admin
    }

    public enum ERemoteKey
    {
        Up,
        Down,
        Left,
        Right,
        Enter,
        Escape,
        Tab,
        Backspace,
        Space,
        PageUp,
        PageDown
    }

    public sealed class RemoteState
    {
        public string Screen;
        public int? SongId;
    }

    public sealed class RemoteProfile
    {
        public Guid Id;
        public string Name;
        public ERemoteRole Role;
        public int Difficulty;
        public bool IsActive;
        public string AvatarPath;
        public byte[] PinHash;
        public byte[] PinSalt;
    }

    /// Fields that are null stay unchanged.
    public sealed class ProfileChange
    {
        public string Name;
        public int? Difficulty;
        public ERemoteRole? Role;
        public bool? IsActive;

        /// New PIN hash and salt. Set <see cref="ClearPin" /> to remove the PIN.
        public byte[] PinHash;
        public byte[] PinSalt;
        public bool ClearPin;

        /// New avatar as JPEG.
        public byte[] AvatarJpeg;

        /// Avatar of the game, see <see cref="IVocaluxeRemote.GetAvatars" />.
        public int? AvatarId;
    }

    public sealed class RemoteAvatar
    {
        public int Id;
        public string Path;
    }

    public sealed class RemoteSong
    {
        public int Id;
        public string Title;
        public string Artist;
        public string Year;
        public IList<string> Languages;
        public IList<string> Genres;
        public IList<string> Editions;
        public bool IsDuet;

        /// Values of the game's EGameMode / EAudioMode that can be used with this song.
        public IList<int> GameModes;
        public IList<int> AudioModes;

        public string CoverPath;
        public string AudioPath;

        /// Start of the preview in seconds, null if the song does not define one (then a quarter of the song is used).
        public float? PreviewStart;
        public int NumPlayed;
        public DateTime DateAdded;
    }

    public sealed class RemotePlaylistEntry
    {
        public int SongId;
        public int GameMode;
        public int AudioMode;
    }

    /// A queue entry as the game screen shows it: the playlist entry plus what only the webserver knows.
    public sealed class RemoteQueueItem
    {
        public int SongId;
        public int GameMode;
        public int AudioMode;
        public string RequestedBy;
        public IList<Guid> PlayerIds;
        public IList<string> PlayerNames;
    }

    public enum ERemoteStartResult
    {
        /// The game cannot start a song right now (singing, party mode, ...).
        Busy,

        /// Singing starts right away.
        Singing,

        /// The player selection is shown first (no singers chosen or microphones missing).
        PlayerSelection
    }

    public sealed class RemoteStartRequest
    {
        public int SongId;
        public int GameMode;
        public int AudioMode;
        public IList<Guid> PlayerIds;
    }

    public sealed class RemotePlaylist
    {
        public int Id;
        public string Name;
        public IList<RemotePlaylistEntry> Entries;
    }
}
