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

using System.Collections.Generic;
using System.Linq;

namespace Vocaluxe.Base.Server
{
    static class CUserRoleControl
    {
        private static readonly Dictionary<EUserRoles, EUserRights> _RoleRightsMapping = new Dictionary<EUserRoles, EUserRights>
            {
                {
                    EUserRoles.TR_USERROLE_ADMIN,
                    (EUserRights.EditAllProfiles |
                     EUserRights.UploadPhotos |
                     EUserRights.ViewOtherProfiles |
                     EUserRights.UseKeyboard |
                     EUserRights.AddSongToPlaylist |
                     EUserRights.ReorderPlaylists |
                     EUserRights.CreatePlaylists |
                     EUserRights.DeletePlaylists |
                     EUserRights.RemoveSongsFromPlaylists)
                },
                {
                    EUserRoles.TR_USERROLE_GUEST,
                    EUserRights.None
                },
                {
                    EUserRoles.TR_USERROLE_NORMAL,
                    (EUserRights.UploadPhotos |
                     EUserRights.ViewOtherProfiles)
                },
                {
                    EUserRoles.TR_USERROLE_KEYBOARDUSER,
                    EUserRights.UseKeyboard
                },
                {
                    EUserRoles.TR_USERROLE_ADDSONGSUSER,
                    EUserRights.AddSongToPlaylist
                },
                {
                    EUserRoles.TR_USERROLE_PLAYLISTEDITOR,
                    (EUserRights.AddSongToPlaylist |
                     EUserRights.ReorderPlaylists |
                     EUserRights.CreatePlaylists |
                     EUserRights.DeletePlaylists |
                     EUserRights.RemoveSongsFromPlaylists)
                },
                {
                    EUserRoles.TR_USERROLE_PROFILEEDITOR,
                    EUserRights.EditAllProfiles
                }
            };

        internal static EUserRights GetUserRightsFromUserRole(EUserRoles userRole)
        {
            return _RoleRightsMapping.Where(role => userRole.HasFlag(role.Key))
                                     .Select(role => role.Value)
                                     .Aggregate(EUserRights.None, (current, r) => current | r);
        }
    }
}