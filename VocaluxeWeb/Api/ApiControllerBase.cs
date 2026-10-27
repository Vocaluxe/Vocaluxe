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
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using VocaluxeWeb.Contracts;
using VocaluxeWeb.Remote;
using VocaluxeWeb.Services;

namespace VocaluxeWeb.Api
{
    [ApiController]
    [Produces("application/json")]
    public abstract class ApiControllerBase : ControllerBase
    {
        internal const string SessionItemKey = "vlx.session";

        /// The logged in session; set for actions with <see cref="RequireSessionAttribute" />.
        protected CSession CurrentSession => HttpContext.Items[SessionItemKey] as CSession;

        protected bool HasPermission(EPermission permission)
        {
            var session = CurrentSession;
            var settings = HttpContext.RequestServices.GetRequiredService<CWebSettingsStore>().Current;
            return session != null && settings.GetPermissions(session.Role).Contains(permission);
        }

        protected ObjectResult Problem(int status, string title)
        {
            return new ObjectResult(new ProblemDetails { Status = status, Title = title }) { StatusCode = status, ContentTypes = { "application/problem+json" } };
        }

        public static ProfileDto ToDto(RemoteProfile profile)
        {
            return new ProfileDto
            {
                Id = profile.Id,
                Name = profile.Name,
                Role = profile.Role,
                HasPin = profile.PinHash != null,
                HasAvatar = !string.IsNullOrEmpty(profile.AvatarPath),
                Difficulty = profile.Difficulty,
                IsActive = profile.IsActive
            };
        }
    }

    /// The request needs a logged in profile and, optionally, a permission. Admins have all permissions.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireSessionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly EPermission? _Permission;

        public RequireSessionAttribute() { }

        public RequireSessionAttribute(EPermission permission)
        {
            _Permission = permission;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var services = context.HttpContext.RequestServices;
            var session = services.GetRequiredService<CSessionStore>().Get(context.HttpContext);
            if (session == null)
            {
                context.Result = _Problem(StatusCodes.Status401Unauthorized, "Please log in.");
                return;
            }

            context.HttpContext.Items[ApiControllerBase.SessionItemKey] = session;
            if (_Permission.HasValue && !services.GetRequiredService<CWebSettingsStore>().Current.GetPermissions(session.Role).Contains(_Permission.Value))
            {
                context.Result = _Problem(StatusCodes.Status403Forbidden, "You are not allowed to do this.");
            }
        }

        private static ObjectResult _Problem(int status, string title)
        {
            return new ObjectResult(new ProblemDetails { Status = status, Title = title }) { StatusCode = status, ContentTypes = { "application/problem+json" } };
        }
    }

    /// Only admins.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireAdminAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.RequestServices.GetRequiredService<CSessionStore>().Get(context.HttpContext);
            if (session == null || session.Role != ERemoteRole.Admin)
            {
                var status = session == null ? StatusCodes.Status401Unauthorized : StatusCodes.Status403Forbidden;
                context.Result = new ObjectResult(new ProblemDetails { Status = status, Title = "Admins only." }) { StatusCode = status, ContentTypes = { "application/problem+json" } };
                return;
            }

            context.HttpContext.Items[ApiControllerBase.SessionItemKey] = session;
        }
    }
}
