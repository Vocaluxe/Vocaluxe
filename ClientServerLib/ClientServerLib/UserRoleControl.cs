using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerLib
{
    internal static class UserRoleControl
    {
        private static Dictionary<UserRoles, UserRights> roleRightsMapping = new Dictionary<UserRoles, UserRights> {
                                                                            {UserRoles.Administrator,
                                                                                (UserRights.EditAllProfiles|
                                                                                UserRights.UploadPhotos|
                                                                                UserRights.ViewOtherProfiles)|
                                                                                UserRights.UseKeyboard},
                                                                            {UserRoles.AuthenticatedUser,
                                                                                (UserRights.UploadPhotos|
                                                                                UserRights.ViewOtherProfiles|
                                                                                UserRights.UseKeyboard)}
                                                                        };


        internal static UserRights getUserRightsFromUserRole(UserRoles userRole)
        {
            UserRights resultRights = UserRights.None;          

            foreach (UserRights r in roleRightsMapping.Where(role => userRole.HasFlag(role.Key)).Select(role => role.Value))
            {
                resultRights |= r;
            }

            return resultRights;
        }
    }
}
