using System.Collections.Generic;
using System.Linq;

namespace ServerLib
{
    internal static class CUserRoleControl
    {
        private static readonly Dictionary<EUserRoles, EUserRights> _RoleRightsMapping = new Dictionary<EUserRoles, EUserRights> {
                                                                            {EUserRoles.Administrator,
                                                                                (EUserRights.EditAllProfiles|
                                                                                EUserRights.UploadPhotos|
                                                                                EUserRights.ViewOtherProfiles)|
                                                                                EUserRights.UseKeyboard},
                                                                            {EUserRoles.AuthenticatedUser,
                                                                                (EUserRights.UploadPhotos|
                                                                                EUserRights.ViewOtherProfiles|
                                                                                EUserRights.UseKeyboard)}
                                                                        };


        internal static EUserRights GetUserRightsFromUserRole(EUserRoles userRole)
        {
            return _RoleRightsMapping.Where(role => userRole.HasFlag(role.Key))
                .Select(role => role.Value)
                .Aggregate(EUserRights.None, (current, r) => current | r);
        }
    }
}
