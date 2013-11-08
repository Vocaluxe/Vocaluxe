using System;

namespace ServerLib
{
    [Flags]
    internal enum EUserRights
    {
        None = 0x00,
        EditAllProfiles = 0x01,
        UploadPhotos = 0x02,
        ViewOtherProfiles = 0x04,
        UseKeyboard = 0x08,
        ReorderPlaylists = 0x16,
        AddSongToPlaylist = 0x32,
        CreatePlaylists = 0x64,
        DeletePlaylists = 0x128,
        RemoveSongsFromPlaylists = 0x256
    }
}
