using System;

namespace ServerLib
{
    [Flags]
    internal enum EUserRights
    {
        None = 0,
        EditAllProfiles = 1,
        UploadPhotos = 2,
        ViewOtherProfiles = 4,
        UseKeyboard = 8,
        ReorderPlaylists = 16,
        AddSongToPlaylist = 32,
        CreatePlaylists = 64,
        DeletePlaylists = 128,
        RemoveSongsFromPlaylists = 256
    }
}
