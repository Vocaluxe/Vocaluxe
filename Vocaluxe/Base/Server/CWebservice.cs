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
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;

namespace Vocaluxe.Base.Server
{
    class CWebservice : ICWebservice
    {
        private static Guid _GetSession()
        {
            Guid sessionKey = Guid.Empty;
            string sessionHeader =
                ((HttpRequestMessageProperty)OperationContext.Current.IncomingMessageProperties["httpRequest"]).Headers["session"];
            if (string.IsNullOrEmpty(sessionHeader))
                return sessionKey;
            try
            {
                sessionKey = Guid.Parse(sessionHeader);
            }
            catch (Exception)
            { }
            CSessionControl.ResetSessionTimeout(sessionKey);
            return sessionKey;
        }

        public void SendKeyEvent(string key)
        {
            if (!_CheckRight(EUserRights.UseKeyboard))
                return;


            CVocaluxeServer.DoTask(CVocaluxeServer.SendKeyEvent,key);
        }

        public void SendKeyStringEvent(string keyString, bool isShiftPressed = false, bool isAltPressed = false, bool isCtrlPressed = false)
        {
            if (!_CheckRight(EUserRights.UseKeyboard))
                return;
           
            CVocaluxeServer.DoTask(CVocaluxeServer.SendKeyStringEvent, keyString, isShiftPressed, isAltPressed, isCtrlPressed);
        }

        #region profile
        public Guid GetOwnProfileId()
        {
            Guid sessionKey = _GetSession();
            if (sessionKey == Guid.Empty)
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = "No session";
                }
                return Guid.Empty;
            }
            Guid profileId = CSessionControl.GetUserIdFromSession(sessionKey);
            if (profileId == Guid.Empty)
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = "No session";
                }
                return Guid.Empty;
            }
            return profileId;
        }

        public void SendProfile(SProfileData profile)
        {
            Guid sessionKey = _GetSession();

            if (profile.ProfileId != Guid.Empty) //Guid.Empty is the id for a new profile
            {
                if (CSessionControl.GetUserIdFromSession(sessionKey) != profile.ProfileId
                    && !(_CheckRight(EUserRights.EditAllProfiles)))
                    return;
            }

            CVocaluxeServer.DoTask(CVocaluxeServer.SendProfileData, profile);
        }

        public SProfileData GetProfile(Guid profileId)
        {
            Guid sessionKey = _GetSession();
            if (CSessionControl.GetUserIdFromSession(sessionKey) == profileId || _CheckRight(EUserRights.ViewOtherProfiles))
            {
                bool isReadonly = (!CSessionControl.RequestRight(sessionKey, EUserRights.EditAllProfiles) &&
                                   CSessionControl.GetUserIdFromSession(sessionKey) != profileId);


                return CVocaluxeServer.DoTask(CVocaluxeServer.GetProfileData,profileId, isReadonly);
            }
            return new SProfileData();
        }

        public SProfileData[] GetProfileList()
        {
            return CVocaluxeServer.DoTask(CVocaluxeServer.GetProfileList);
        }
        #endregion

        #region photo
        public void SendPhoto(SPhotoData photo)
        {
            if (_CheckRight(EUserRights.UploadPhotos))
                CVocaluxeServer.DoTask(CVocaluxeServer.SendPhoto, photo);
        }
        #endregion

        #region website
        public Guid Login(string username, string password)
        {
            Guid sessionId = CSessionControl.OpenSession(username, password);
            if (sessionId == Guid.Empty)
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = "Wrong username or password";
                }
            }
            return sessionId;
        }

        public void Logout()
        {
            Guid sessionKey = _GetSession();
            CSessionControl.InvalidateSessionByID(sessionKey);
        }

        public Stream Index()
        {
            if (WebOperationContext.Current != null)
                WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";

            return new MemoryStream(CVocaluxeServer.DoTask(CVocaluxeServer.GetSiteFile,"index.html"));
        }

        public Stream GetJsFile(string filename)
        {
            if (WebOperationContext.Current != null)
            {
                WebOperationContext.Current.OutgoingResponse.ContentType = "text/javascript";
                WebOperationContext.Current.OutgoingResponse.LastModified = DateTime.UtcNow;
                WebOperationContext.Current.OutgoingResponse.Headers.Add(
                    HttpResponseHeader.Expires,
                    DateTime.UtcNow.AddHours(4).ToString("r"));
            }

            byte[] data = CVocaluxeServer.DoTask(CVocaluxeServer.GetSiteFile, "js/" + filename);

            if (data != null)
                return new MemoryStream(data);

            if (WebOperationContext.Current != null)
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotFound;
            return null;
        }

        public Stream GetCssFile(string filename)
        {
            if (WebOperationContext.Current != null)
            {
                WebOperationContext.Current.OutgoingResponse.ContentType = "text/css";
                WebOperationContext.Current.OutgoingResponse.LastModified = DateTime.UtcNow;
                WebOperationContext.Current.OutgoingResponse.Headers.Add(
                    HttpResponseHeader.Expires,
                    DateTime.UtcNow.AddHours(4).ToString("r"));
            }

            byte[] data = CVocaluxeServer.DoTask(CVocaluxeServer.GetSiteFile, "css/" + filename);

            if (data != null)
                return new MemoryStream(data);
            if (WebOperationContext.Current != null)
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotFound;
            return null;
        }

        public Stream GetCssImageFile(string filename)
        {
            if (WebOperationContext.Current != null)
            {
                WebOperationContext.Current.OutgoingResponse.ContentType = "image/png";
                WebOperationContext.Current.OutgoingResponse.LastModified = DateTime.UtcNow;
                WebOperationContext.Current.OutgoingResponse.Headers.Add(
                    HttpResponseHeader.Expires,
                    DateTime.UtcNow.AddYears(1).ToString("r"));
            }

            byte[] data = CVocaluxeServer.DoTask(CVocaluxeServer.GetSiteFile, "css\\images\\" + filename);

            if (data != null)
                return new MemoryStream(data);
            if (WebOperationContext.Current != null)
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotFound;
            return null;
        }

        public Stream GetImgFile(string filename)
        {
            if (WebOperationContext.Current != null)
            {
                WebOperationContext.Current.OutgoingResponse.ContentType = "image/png";
                WebOperationContext.Current.OutgoingResponse.LastModified = DateTime.UtcNow;
                WebOperationContext.Current.OutgoingResponse.Headers.Add(
                    HttpResponseHeader.Expires,
                    DateTime.UtcNow.AddYears(1).ToString("r"));
            }

            byte[] data = CVocaluxeServer.DoTask(CVocaluxeServer.GetSiteFile, "img/" + filename);

            if (data != null)
                return new MemoryStream(data);
            if (WebOperationContext.Current != null)
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotFound;
            return null;
        }

        public Stream GetLocaleFile(string filename)
        {
            if (WebOperationContext.Current != null)
            {
                WebOperationContext.Current.OutgoingResponse.ContentType = "text/javascript";
                WebOperationContext.Current.OutgoingResponse.LastModified = DateTime.UtcNow;
                WebOperationContext.Current.OutgoingResponse.Headers.Add(
                    HttpResponseHeader.Expires,
                    DateTime.UtcNow.AddHours(4).ToString("r"));
            }

            byte[] data = CVocaluxeServer.DoTask(CVocaluxeServer.GetSiteFile, "locales/" + filename);

            if (data != null)
                return new MemoryStream(data);

            if (WebOperationContext.Current != null)
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotFound;
            return null;
        }

        public CBase64Image GetDelayedImage(string id)
        {
            return CVocaluxeServer.DoTask(CVocaluxeServer.GetDelayedImage, id);
        }

        public bool IsServerOnline()
        {
            _GetSession();
            return true;
        }

        public string GetServerVersion()
        {
            return CVocaluxeServer.DoTask(CVocaluxeServer.GetServerVersion);
        }
        #endregion

        #region songs
        public SSongInfo GetSong(int songId)
        {
            return CVocaluxeServer.DoTask(CVocaluxeServer.GetSong, songId);
        }

        public int GetCurrentSongId()
        {
            return CVocaluxeServer.DoTask(CVocaluxeServer.GetCurrentSongId);
        }

        public SSongInfo[] GetAllSongs()
        {
            return CVocaluxeServer.DoTask(CVocaluxeServer.GetAllSongs);
        }

        public Stream GetMp3File(int songId)
        {
            if (WebOperationContext.Current != null)
            {
                WebOperationContext.Current.OutgoingResponse.LastModified = DateTime.UtcNow;
                WebOperationContext.Current.OutgoingResponse.Headers.Add(
                    HttpResponseHeader.Expires,
                    DateTime.UtcNow.AddYears(1).ToString("r"));
            }


            String path = CVocaluxeServer.DoTask(CVocaluxeServer.GetMp3Path,songId);
            path = path.Replace("..", "");


            if (!File.Exists(path) 
                || !(path.EndsWith(".mp3", StringComparison.InvariantCulture) 
                        || path.EndsWith(".ogg", StringComparison.InvariantCulture)
                        || path.EndsWith(".wav", StringComparison.InvariantCulture)
                        || path.EndsWith(".webm", StringComparison.InvariantCulture)))
            {
                if (WebOperationContext.Current != null)
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotFound;
                return null;
            }

            if (WebOperationContext.Current != null)
            {
                if (path.EndsWith(".mp3", StringComparison.InvariantCulture))
                {
                    WebOperationContext.Current.OutgoingResponse.ContentType = "audio/mpeg";
                }
                else if (path.EndsWith(".ogg", StringComparison.InvariantCulture))
                {
                    WebOperationContext.Current.OutgoingResponse.ContentType = "audio/ogg";
                }
                else if (path.EndsWith(".wav", StringComparison.InvariantCulture))
                {
                    WebOperationContext.Current.OutgoingResponse.ContentType = "audio/wav";
                }
                else if (path.EndsWith(".webm", StringComparison.InvariantCulture))
                {
                    WebOperationContext.Current.OutgoingResponse.ContentType = "audio/webm";
                }
            }

            return File.OpenRead(path);
        }
        #endregion

        #region playlist
        public SPlaylistData[] GetPlaylists()
        {
            return CVocaluxeServer.DoTask(CVocaluxeServer.GetPlaylists);
        }

        public SPlaylistData GetPlaylist(int playlistId)
        {
            try
            {
                return CVocaluxeServer.DoTask(CVocaluxeServer.GetPlaylist, playlistId);
            }
            catch (ArgumentException e)
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = e.Message;
                }

                return new SPlaylistData();
            }
           
        }

        public void AddSongToPlaylist(int songId, int playlistId, bool allowDuplicates)
        {
            if (!_CheckRight(EUserRights.AddSongToPlaylist))
                return;

            try
            {
                CVocaluxeServer.DoTaskWithoutReturn(CVocaluxeServer.AddSongToPlaylist, songId, playlistId, allowDuplicates);
            }
            catch (ArgumentException e)
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = e.Message;
                }
            }
        }

        public void RemoveSongFromPlaylist(int position, int playlistId, int songId)
        {
            if (!_CheckRight(EUserRights.RemoveSongsFromPlaylists))
                return;

            try
            {
                CVocaluxeServer.DoTaskWithoutReturn(CVocaluxeServer.RemoveSongFromPlaylist, position, playlistId, songId);
            }
            catch (ArgumentException e)
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = e.Message;
                }
            }
        }

        public void MoveSongInPlaylist(int newPosition, int playlistId, int songId)
        {
            if (!_CheckRight(EUserRights.ReorderPlaylists))
                return;

            try
            {
                CVocaluxeServer.DoTaskWithoutReturn(CVocaluxeServer.MoveSongInPlaylist, newPosition, playlistId, songId);
            }
            catch (ArgumentException e)
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = e.Message;
                }
            }
        }

        public bool PlaylistContainsSong(int songId, int playlistId)
        {
            try
            {
                return CVocaluxeServer.DoTask(CVocaluxeServer.PlaylistContainsSong, songId, playlistId);
            }
            catch (ArgumentException e)
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = e.Message;
                }

                return false;
            }
        }

        public SPlaylistSongInfo[] GetPlaylistSongs(int playlistId)
        {
            try
            {
                return CVocaluxeServer.DoTask(CVocaluxeServer.GetPlaylistSongs, playlistId);
            }
            catch (ArgumentException e)
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = e.Message;
                }
                return new SPlaylistSongInfo[0];
            }
        }

        public void RemovePlaylist(int playlistId)
        {
            if (!_CheckRight(EUserRights.DeletePlaylists))
                return;

            try
            {
                CVocaluxeServer.DoTaskWithoutReturn(CVocaluxeServer.RemovePlaylist, playlistId);
            }
            catch (ArgumentException e)
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = e.Message;
                }
            }
        }

        public int AddPlaylist(string playlistName)
        {
            if (!_CheckRight(EUserRights.CreatePlaylists))
            return -1;

            try
            {
                return CVocaluxeServer.DoTask(CVocaluxeServer.AddPlaylist, playlistName);
            }
            catch (ArgumentException e)
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = e.Message;
                }

                return -1;
            }
        }
        #endregion

        #region user management
        public int GetUserRole(Guid profileId)
        {
            return CVocaluxeServer.DoTask(CVocaluxeServer.GetUserRole, profileId);
        }

        public void SetUserRole(Guid profileId, int userRole)
        {
            if (_CheckRight(EUserRights.EditAllProfiles))
                CVocaluxeServer.DoTaskWithoutReturn(CVocaluxeServer.SetUserRole, profileId, userRole);
        }

        public bool HasUserRight(int right)
        {
            return _CheckRightWithNoErrorMessage((EUserRights)right);
        }

        private static bool _CheckRight(EUserRights requestedRight)
        {
            Guid sessionKey = _GetSession();

            if (sessionKey == Guid.Empty)
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = "No session";
                }
                return false;
            }

            if (!CSessionControl.RequestRight(sessionKey, requestedRight))
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = "Not allowed";
                }
                return false;
            }
            return true;
        }

        private static bool _CheckRightWithNoErrorMessage(EUserRights requestedRight)
        {
            Guid sessionKey = _GetSession();

            if (sessionKey == Guid.Empty)
                return false;

            if (!CSessionControl.RequestRight(sessionKey, requestedRight))
                return false;

            return true;
        }
        #endregion
    }
}