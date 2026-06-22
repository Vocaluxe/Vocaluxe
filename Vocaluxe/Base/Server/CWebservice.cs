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
using System.IO;
using System.Runtime.Serialization.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Vocaluxe.Base.Server
{
    /// <summary>
    ///     The browser remote-control REST API, ported from WCF to ASP.NET Core (Kestrel).
    ///     Endpoints mirror the former ICWebservice 1:1 and delegate to the same CVocaluxeServer
    ///     logic (marshalled onto the main thread via DoTask). Responses use
    ///     DataContractJsonSerializer so the JSON shape stays identical to the old WCF service,
    ///     keeping the existing web frontend working unchanged.
    /// </summary>
    static class CWebservice
    {
        public static void MapEndpoints(WebApplication app)
        {
            // --- input ---
            app.MapGet("/sendKeyEvent", (HttpContext ctx, string key) =>
            {
                if (!_CheckRight(ctx, EUserRights.UseKeyboard))
                    return _Empty();
                CVocaluxeServer.DoTask(CVocaluxeServer.SendKeyEvent, key);
                return _Empty();
            });

            app.MapGet("/sendKeyStringEvent", (HttpContext ctx, string keyString, bool shift, bool alt, bool ctrl) =>
            {
                if (!_CheckRight(ctx, EUserRights.UseKeyboard))
                    return _Empty();
                CVocaluxeServer.DoTask(CVocaluxeServer.SendKeyStringEvent, keyString, shift, alt, ctrl);
                return _Empty();
            });

            // --- profile ---
            app.MapGet("/getOwnProfileId", (HttpContext ctx) =>
            {
                Guid session = _GetSession(ctx);
                Guid profileId = session == Guid.Empty ? Guid.Empty : CSessionControl.GetUserIdFromSession(session);
                if (profileId == Guid.Empty)
                    _Forbid(ctx, "No session");
                return _Json(profileId);
            });

            app.MapPost("/sendProfile", (HttpContext ctx) =>
            {
                SProfileData profile = _ReadBody<SProfileData>(ctx);
                Guid session = _GetSession(ctx);
                if (profile.ProfileId != Guid.Empty
                    && CSessionControl.GetUserIdFromSession(session) != profile.ProfileId
                    && !_CheckRight(ctx, EUserRights.EditAllProfiles))
                    return _Empty();
                CVocaluxeServer.DoTask(CVocaluxeServer.SendProfileData, profile);
                return _Empty();
            });

            app.MapGet("/getProfile", (HttpContext ctx, Guid profileId) =>
            {
                Guid session = _GetSession(ctx);
                if (CSessionControl.GetUserIdFromSession(session) == profileId || _CheckRight(ctx, EUserRights.ViewOtherProfiles))
                {
                    bool isReadonly = !CSessionControl.RequestRight(session, EUserRights.EditAllProfiles)
                                      && CSessionControl.GetUserIdFromSession(session) != profileId;
                    return _Json(CVocaluxeServer.DoTask(CVocaluxeServer.GetProfileData, profileId, isReadonly));
                }
                return _Json(new SProfileData());
            });

            app.MapGet("/getProfileList", () => _Json(CVocaluxeServer.DoTask(CVocaluxeServer.GetProfileList)));

            // --- photo ---
            app.MapPost("/sendPhoto", (HttpContext ctx) =>
            {
                if (_CheckRight(ctx, EUserRights.UploadPhotos))
                    CVocaluxeServer.DoTask(CVocaluxeServer.SendPhoto, _ReadBody<SPhotoData>(ctx));
                return _Empty();
            });

            // --- website / session ---
            app.MapGet("/login", (HttpContext ctx, string username, string password) =>
            {
                Guid sessionId = CSessionControl.OpenSession(username, password);
                if (sessionId == Guid.Empty)
                    _Forbid(ctx, "Wrong username or password");
                return _Json(sessionId);
            });

            app.MapGet("/logout", (HttpContext ctx) =>
            {
                CSessionControl.InvalidateSessionByID(_GetSession(ctx));
                return _Empty();
            });

            app.MapGet("/", (HttpContext ctx) => _File(ctx, "index.html", "text/html"));
            app.MapGet("/js/{filename}", (HttpContext ctx, string filename) => _File(ctx, "js/" + filename, "text/javascript"));
            app.MapGet("/css/{filename}", (HttpContext ctx, string filename) => _File(ctx, "css/" + filename, "text/css"));
            app.MapGet("/css/images/{filename}", (HttpContext ctx, string filename) => _File(ctx, "css/images/" + filename, "image/png"));
            app.MapGet("/img/{filename}", (HttpContext ctx, string filename) => _File(ctx, "img/" + filename, "image/png"));
            app.MapGet("/locales/{filename}", (HttpContext ctx, string filename) => _File(ctx, "locales/" + filename, "text/javascript"));

            app.MapGet("/delayedImage", (string id) => _Json(CVocaluxeServer.DoTask(CVocaluxeServer.GetDelayedImage, id)));
            app.MapGet("/isServerOnline", (HttpContext ctx) =>
            {
                _GetSession(ctx);
                return _Json(true);
            });
            app.MapGet("/getServerVersion", () => _Json(CVocaluxeServer.DoTask(CVocaluxeServer.GetServerVersion)));

            // --- songs ---
            app.MapGet("/getSong", (int songId) => _Json(CVocaluxeServer.DoTask(CVocaluxeServer.GetSong, songId)));
            app.MapGet("/getCurrentSongId", () => _Json(CVocaluxeServer.DoTask(CVocaluxeServer.GetCurrentSongId)));
            app.MapGet("/getAllSongs", () => _Json(CVocaluxeServer.DoTask(CVocaluxeServer.GetAllSongs)));
            app.MapGet("/getMp3", (HttpContext ctx, int songId) => _Mp3(ctx, songId));

            // --- playlist ---
            app.MapGet("/getPlaylists", () => _Json(CVocaluxeServer.DoTask(CVocaluxeServer.GetPlaylists)));
            app.MapGet("/getPlaylist", (HttpContext ctx, int id) => _GuardArg(ctx, () => _Json(CVocaluxeServer.DoTask(CVocaluxeServer.GetPlaylist, id)), () => _Json(new SPlaylistData())));
            app.MapGet("/addSongToPlaylist", (HttpContext ctx, int songId, int playlistId, bool duplicates) =>
            {
                if (_CheckRight(ctx, EUserRights.AddSongToPlaylist))
                    _GuardArg(ctx, () => { CVocaluxeServer.DoTaskWithoutReturn(CVocaluxeServer.AddSongToPlaylist, songId, playlistId, duplicates); return _Empty(); }, _Empty);
                return _Empty();
            });
            app.MapGet("/removeSongFromPlaylist", (HttpContext ctx, int position, int playlistId, int songId) =>
            {
                if (_CheckRight(ctx, EUserRights.RemoveSongsFromPlaylists))
                    _GuardArg(ctx, () => { CVocaluxeServer.DoTaskWithoutReturn(CVocaluxeServer.RemoveSongFromPlaylist, position, playlistId, songId); return _Empty(); }, _Empty);
                return _Empty();
            });
            app.MapGet("/moveSongInPlaylist", (HttpContext ctx, int newPosition, int playlistId, int songId) =>
            {
                if (_CheckRight(ctx, EUserRights.ReorderPlaylists))
                    _GuardArg(ctx, () => { CVocaluxeServer.DoTaskWithoutReturn(CVocaluxeServer.MoveSongInPlaylist, newPosition, playlistId, songId); return _Empty(); }, _Empty);
                return _Empty();
            });
            app.MapGet("/playlistContainsSong", (HttpContext ctx, int songId, int playlistId) =>
                _GuardArg(ctx, () => _Json(CVocaluxeServer.DoTask(CVocaluxeServer.PlaylistContainsSong, songId, playlistId)), () => _Json(false)));
            app.MapGet("/getPlaylistSongs", (HttpContext ctx, int playlistId) =>
                _GuardArg(ctx, () => _Json(CVocaluxeServer.DoTask(CVocaluxeServer.GetPlaylistSongs, playlistId)), () => _Json(new SPlaylistSongInfo[0])));
            app.MapGet("/removePlaylist", (HttpContext ctx, int playlistId) =>
            {
                if (_CheckRight(ctx, EUserRights.DeletePlaylists))
                    _GuardArg(ctx, () => { CVocaluxeServer.DoTaskWithoutReturn(CVocaluxeServer.RemovePlaylist, playlistId); return _Empty(); }, _Empty);
                return _Empty();
            });
            app.MapGet("/addPlaylist", (HttpContext ctx, string playlistName) =>
            {
                if (!_CheckRight(ctx, EUserRights.CreatePlaylists))
                    return _Json(-1);
                return _GuardArg(ctx, () => _Json(CVocaluxeServer.DoTask(CVocaluxeServer.AddPlaylist, playlistName)), () => _Json(-1));
            });

            // --- user management ---
            app.MapGet("/getUserRole", (Guid profileId) => _Json(CVocaluxeServer.DoTask(CVocaluxeServer.GetUserRole, profileId)));
            app.MapGet("/setUserRole", (HttpContext ctx, Guid profileId, int userRole) =>
            {
                if (_CheckRight(ctx, EUserRights.EditAllProfiles))
                    CVocaluxeServer.DoTaskWithoutReturn(CVocaluxeServer.SetUserRole, profileId, userRole);
                return _Empty();
            });
            app.MapGet("/hasUserRight", (HttpContext ctx, int right) =>
            {
                Guid session = _GetSession(ctx);
                bool ok = session != Guid.Empty && CSessionControl.RequestRight(session, (EUserRights)right);
                return _Json(ok);
            });
        }

        #region helpers
        private static Guid _GetSession(HttpContext ctx)
        {
            string header = ctx.Request.Headers["session"];
            if (string.IsNullOrEmpty(header))
                return Guid.Empty;
            Guid session;
            if (!Guid.TryParse(header, out session) || session == Guid.Empty)
                return Guid.Empty;
            CSessionControl.ResetSessionTimeout(session);
            return session;
        }

        private static bool _CheckRight(HttpContext ctx, EUserRights requestedRight)
        {
            Guid session = _GetSession(ctx);
            if (session == Guid.Empty)
            {
                _Forbid(ctx, "No session");
                return false;
            }
            if (!CSessionControl.RequestRight(session, requestedRight))
            {
                _Forbid(ctx, "Not allowed");
                return false;
            }
            return true;
        }

        private static void _Forbid(HttpContext ctx, string description)
        {
            ctx.Response.StatusCode = 403;
            ctx.Response.Headers["X-Status-Description"] = description;
        }

        /// <summary>Mirrors the WCF behaviour of mapping an ArgumentException to HTTP 403.</summary>
        private static IResult _GuardArg(HttpContext ctx, Func<IResult> action, Func<IResult> onError)
        {
            try
            {
                return action();
            }
            catch (ArgumentException e)
            {
                _Forbid(ctx, e.Message);
                return onError();
            }
        }

        private static IResult _Json(object obj)
        {
            if (obj == null)
                return Results.Bytes(Array.Empty<byte>(), "application/json");
            using (var ms = new MemoryStream())
            {
                new DataContractJsonSerializer(obj.GetType()).WriteObject(ms, obj);
                return Results.Bytes(ms.ToArray(), "application/json");
            }
        }

        private static T _ReadBody<T>(HttpContext ctx)
        {
            using (var ms = new MemoryStream())
            {
                ctx.Request.Body.CopyTo(ms);
                ms.Position = 0;
                if (ms.Length == 0)
                    return default(T);
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(ms);
            }
        }

        private static IResult _Empty()
        {
            return Results.Bytes(Array.Empty<byte>(), "application/json");
        }

        private static IResult _File(HttpContext ctx, string relativePath, string contentType)
        {
            byte[] data = CVocaluxeServer.DoTask(CVocaluxeServer.GetSiteFile, relativePath);
            if (data == null)
                return Results.NotFound();
            return Results.Bytes(data, contentType);
        }

        private static IResult _Mp3(HttpContext ctx, int songId)
        {
            string path = CVocaluxeServer.DoTask(CVocaluxeServer.GetMp3Path, songId);
            if (string.IsNullOrEmpty(path))
                return Results.NotFound();
            path = path.Replace("..", "");

            if (!File.Exists(path))
                return Results.NotFound();

            string contentType;
            if (path.EndsWith(".mp3", StringComparison.InvariantCulture))
                contentType = "audio/mpeg";
            else if (path.EndsWith(".ogg", StringComparison.InvariantCulture))
                contentType = "audio/ogg";
            else if (path.EndsWith(".wav", StringComparison.InvariantCulture))
                contentType = "audio/wav";
            else if (path.EndsWith(".webm", StringComparison.InvariantCulture))
                contentType = "audio/webm";
            else
                return Results.NotFound();

            return Results.File(File.OpenRead(path), contentType);
        }
        #endregion
    }
}
