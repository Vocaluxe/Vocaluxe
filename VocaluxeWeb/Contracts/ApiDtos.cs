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
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using VocaluxeWeb.Remote;

namespace VocaluxeWeb.Contracts
{
    public enum EPermission
    {
        AddToQueue,
        ManageQueue,
        UseKeyboard,
        EditPlaylists,
        DeletePlaylists,
        EditOwnProfile,
        EditAllProfiles,
        CreateProfiles,
        StartSongs
    }

    #region profiles & session
    public sealed class ProfileDto
    {
        [Required] public Guid Id { get; set; }
        [Required] public string Name { get; set; }
        [Required] public ERemoteRole Role { get; set; }
        [Required] public bool HasPin { get; set; }
        [Required] public bool HasAvatar { get; set; }

        /// 0 = easy, 1 = normal, 2 = hard
        [Required] public int Difficulty { get; set; }

        /// Inactive profiles are hidden on the login page and in the game's player selection.
        [Required] public bool IsActive { get; set; }
    }

    public sealed class ActiveDto
    {
        [Required] public bool? Active { get; set; }
    }

    public sealed class ProfileCreateDto
    {
        [Required, StringLength(32, MinimumLength = 1)]
        public string Name { get; set; }

        [StringLength(32, MinimumLength = 4)]
        public string Pin { get; set; }

        [Range(0, 2)]
        public int? Difficulty { get; set; }
    }

    public sealed class ProfileUpdateDto
    {
        [StringLength(32, MinimumLength = 1)]
        public string Name { get; set; }

        [Range(0, 2)]
        public int? Difficulty { get; set; }

        /// New PIN. Use an empty string to remove the PIN.
        [StringLength(32)]
        public string Pin { get; set; }
    }

    public sealed class RoleUpdateDto
    {
        [Required] public ERemoteRole? Role { get; set; }
    }

    public sealed class AvatarDto
    {
        [Required] public int Id { get; set; }
    }

    public sealed class AvatarSelectDto
    {
        [Required] public int? AvatarId { get; set; }
    }

    public sealed class SessionCreateDto
    {
        [Required] public Guid? ProfileId { get; set; }
        public string Pin { get; set; }
    }

    public sealed class SessionDto
    {
        [Required] public ProfileDto Profile { get; set; }
        [Required] public IList<EPermission> Permissions { get; set; }
    }
    #endregion

    #region songs
    public sealed class SongDto
    {
        [Required] public int Id { get; set; }
        [Required] public string Title { get; set; }
        [Required] public string Artist { get; set; }
        public string Year { get; set; }
        public string Language { get; set; }
        public string Genre { get; set; }
        [Required] public bool IsDuet { get; set; }
        [Required] public bool HasCover { get; set; }
        [Required] public bool HasAudio { get; set; }

        /// Start of the preview in seconds; when missing, use a quarter of the song length.
        public float? PreviewStart { get; set; }

        [Required] public IList<ESongGameMode> GameModes { get; set; }

        /// Empty if the song has no instrumental track (then only the normal mode is possible).
        [Required] public IList<ESongAudioMode> AudioModes { get; set; }
    }

    /// Same order as the game's EGameMode.
    public enum ESongGameMode
    {
        Normal,
        Medley,
        Duet,
        Short
    }

    /// Same order as the game's EAudioMode.
    public enum ESongAudioMode
    {
        Normal,
        Instrumental,
        Vocals,
        Karaoke
    }

    /// Search parameters for GET /api/v1/songs. All words of the query must appear in artist, title or edition.
    public sealed class SongSearchDto
    {
        [FromQuery(Name = "query")] public string Query { get; set; }
        [FromQuery(Name = "language")] public string Language { get; set; }
        [FromQuery(Name = "genre")] public string Genre { get; set; }
        [FromQuery(Name = "decade")] public int? Decade { get; set; }
        [FromQuery(Name = "duet")] public bool? Duet { get; set; }
        [FromQuery(Name = "sort")] public ESongSort Sort { get; set; } = ESongSort.Artist;
        [FromQuery(Name = "page")] public int Page { get; set; }
        [FromQuery(Name = "pageSize")] public int PageSize { get; set; } = 50;
    }

    public sealed class SongPageDto
    {
        [Required] public IList<SongDto> Items { get; set; }
        [Required] public int Total { get; set; }
        [Required] public int Page { get; set; }
        [Required] public int PageSize { get; set; }
    }

    public sealed class FacetValueDto
    {
        [Required] public string Value { get; set; }
        [Required] public int Count { get; set; }
    }

    public sealed class SongFacetsDto
    {
        [Required] public IList<FacetValueDto> Languages { get; set; }
        [Required] public IList<FacetValueDto> Genres { get; set; }
        [Required] public IList<FacetValueDto> Decades { get; set; }
    }

    public enum ESongSort
    {
        Artist,
        Title,
        Year
    }
    #endregion

    #region playlists & queue
    public sealed class PlaylistSummaryDto
    {
        [Required] public int Id { get; set; }
        [Required] public string Name { get; set; }
        [Required] public int SongCount { get; set; }
    }

    public sealed class PlaylistEntryDto
    {
        [Required] public int Position { get; set; }
        [Required] public SongDto Song { get; set; }
    }

    public sealed class PlaylistDto
    {
        [Required] public int Id { get; set; }
        [Required] public string Name { get; set; }
        [Required] public IList<PlaylistEntryDto> Songs { get; set; }
    }

    public sealed class PlaylistNameDto
    {
        [Required, StringLength(64, MinimumLength = 1)]
        public string Name { get; set; }
    }

    public sealed class SongRefDto
    {
        [Required] public int? SongId { get; set; }

        /// Insert position; appended when missing.
        public int? Position { get; set; }
    }

    public sealed class PositionDto
    {
        [Required, Range(0, int.MaxValue)]
        public int? Position { get; set; }
    }

    public sealed class ProfileRefDto
    {
        [Required] public Guid Id { get; set; }
        [Required] public string Name { get; set; }
    }

    public sealed class QueueEntryDto
    {
        /// Stable id of the entry while it is in the queue.
        [Required] public string Id { get; set; }

        [Required] public int Position { get; set; }
        [Required] public SongDto Song { get; set; }

        /// Who wished the song; null if unknown (e.g. added in the game).
        public ProfileRefDto RequestedBy { get; set; }

        [Required] public ESongGameMode GameMode { get; set; }
        [Required] public ESongAudioMode AudioMode { get; set; }

        /// Who should sing; empty if the players are chosen in the game.
        [Required] public IList<ProfileRefDto> Players { get; set; }
    }

    public sealed class QueueAddDto
    {
        [Required] public int? SongId { get; set; }
        public ESongGameMode? GameMode { get; set; }
        public ESongAudioMode? AudioMode { get; set; }
        public IList<Guid> PlayerIds { get; set; }
    }

    /// Adds songs of a playlist to the queue, all of them or only the given positions.
    public sealed class QueueImportDto
    {
        [Required] public int? PlaylistId { get; set; }
        public IList<int> Positions { get; set; }
    }

    /// Fields that are missing stay unchanged.
    public sealed class QueueUpdateDto
    {
        public ESongGameMode? GameMode { get; set; }
        public ESongAudioMode? AudioMode { get; set; }
        public IList<Guid> PlayerIds { get; set; }
    }

    /// Sent as live event "notification".
    public sealed class NotificationDto
    {
        /// "queueAdded" or "queueAddedMany" (several songs of a playlist; Song is the first one)
        [Required] public string Type { get; set; }

        [Required] public ProfileRefDto Profile { get; set; }
        public SongDto Song { get; set; }
        public int Count { get; set; } = 1;
    }
    #endregion

    #region input & admin
    public sealed class KeyDto
    {
        [Required] public ERemoteKey? Key { get; set; }
    }

    public sealed class TextDto
    {
        [Required, StringLength(200, MinimumLength = 1)]
        public string Text { get; set; }
    }

    public sealed class AdminStatusDto
    {
        [Required] public string Version { get; set; }

        /// Address of the WebApp in the network.
        [Required] public string Address { get; set; }

        [Required] public int Port { get; set; }
        [Required] public bool Https { get; set; }

        /// Number of WebApps that are connected right now.
        [Required] public int ConnectedClients { get; set; }
    }

    public sealed class AdminSessionDto
    {
        [Required] public string Id { get; set; }
        [Required] public ProfileRefDto Profile { get; set; }
        [Required] public ERemoteRole Role { get; set; }
        public string Device { get; set; }
        public string RemoteAddress { get; set; }
        [Required] public DateTime LastSeen { get; set; }
        [Required] public bool IsCurrent { get; set; }
    }

    public sealed class AdminSettingsDto
    {
        /// New profiles can be created on the login page.
        [Required] public bool? AllowProfileCreation { get; set; }

        /// Name of the playlist used as queue.
        [Required, StringLength(64, MinimumLength = 1)]
        public string QueuePlaylistName { get; set; }

        [Required] public IList<EPermission> GuestPermissions { get; set; }
        [Required] public IList<EPermission> PlayerPermissions { get; set; }
    }
    #endregion
}
