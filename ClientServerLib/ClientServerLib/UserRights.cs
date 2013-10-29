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
        EditPlaylists = 0x16,
        AddSongToPlaylist = 0x32
    }
}
