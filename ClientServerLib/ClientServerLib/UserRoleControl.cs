using System.Collections.Generic;
using System.Linq;

namespace ServerLib
{
    internal static class CUserRoleControl
    {
        private static readonly Dictionary<EUserRoles, EUserRights> _RoleRightsMapping = new Dictionary<EUserRoles, EUserRights> {
                                                                            {EUserRoles.TR_USERROLE_ADMIN,
                                                                                (EUserRights.EditAllProfiles|
                                                                                EUserRights.UploadPhotos|
                                                                                EUserRights.ViewOtherProfiles|
                                                                                EUserRights.UseKeyboard|
                                                                                EUserRights.AddSongToPlaylist|
                                                                                EUserRights.ReorderPlaylists|
                                                                                EUserRights.CreatePlaylists|
                                                                                EUserRights.DeletePlaylists|
                                                                                EUserRights.RemoveSongsFromPlaylists)},
                                                                            {EUserRoles.TR_USERROLE_GUEST,
                                                                                EUserRights.None},
                                                                            {EUserRoles.TR_USERROLE_NORMAL, 
                                                                                (EUserRights.UploadPhotos|
                                                                                EUserRights.ViewOtherProfiles)},
                                                                            {EUserRoles.TR_USERROLE_KEYBOARDUSER,
                                                                                EUserRights.UseKeyboard},
                                                                            {EUserRoles.TR_USERROLE_ADDSONGSUSER,
                                                                                EUserRights.AddSongToPlaylist},
                                                                            {EUserRoles.TR_USERROLE_PLAYLISTEDITOR,
                                                                                (EUserRights.AddSongToPlaylist|
                                                                                EUserRights.ReorderPlaylists|
                                                                                EUserRights.CreatePlaylists|
                                                                                EUserRights.DeletePlaylists|
                                                                                EUserRights.RemoveSongsFromPlaylists)},
                                                                            {EUserRoles.TR_USERROLE_PROFILEEDITOR,
                                                                                EUserRights.EditAllProfiles}
                                                                        };


        internal static EUserRights GetUserRightsFromUserRole(EUserRoles userRole)
        {
            return _RoleRightsMapping.Where(role => userRole.HasFlag(role.Key))
                .Select(role => role.Value)
                .Aggregate(EUserRights.None, (current, r) => current | r);
        }
    }
}
