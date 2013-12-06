using System;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.IO;
using System.ServiceModel.Channels;

namespace ServerLib
{
    class CWebservice : ICWebservice
    {
        private static Guid _GetSession()
        {
            Guid sessionKey = Guid.Empty;
            string sessionHeader =
                ((HttpRequestMessageProperty)OperationContext.Current.IncomingMessageProperties["httpRequest"]).Headers["session"];
            if (string.IsNullOrEmpty(sessionHeader))
            {
                return sessionKey;
            }
            try
            {
                sessionKey = Guid.Parse(sessionHeader);
            }
            catch (Exception e)
            {
            }
            return sessionKey;
        }

        public void SendKeyEvent(string key)
        {
            if (!_CheckRight(EUserRights.UseKeyboard))
            {
                return;
            }

            if (CServer.SendKeyEvent == null)
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotFound;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = "Not found";
                }
            }
            else
            {
                CServer.SendKeyEvent(key);
            }
        }

        #region profile

        public int GetOwnProfileId()
        {
            Guid sessionKey = _GetSession();
            if (sessionKey == Guid.Empty)
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = "No session";
                }
                return -1;
            }
            int profileId = CSessionControl.GetUserIdFromSession(sessionKey);
            if (profileId < 0)
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = "No session";
                }
                return -1;
            }
            return profileId;
        }

        public void SendProfile(SProfileData profile)
        {
            Guid sessionKey = _GetSession();

            if (profile.ProfileId != -1) //-1 is the id for a new profile
            {
                if (CSessionControl.GetUserIdFromSession(sessionKey) != profile.ProfileId 
                    && !(_CheckRight(EUserRights.EditAllProfiles) ))
                {
                    return;
                }
            }

            if (CServer.SendProfileData == null)
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotFound;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = "Not found";
                }
                return;
            }

            CServer.SendProfileData(profile);
        }

        public SProfileData GetProfile(int profileId)
        {
            Guid sessionKey = _GetSession();
            if (_CheckRight(EUserRights.ViewOtherProfiles) || CSessionControl.GetUserIdFromSession(sessionKey) == profileId)
            {
                if (CServer.GetProfileData == null)
                {
                    return new SProfileData();
                }

                bool isReadonly = (!CSessionControl.RequestRight(sessionKey, EUserRights.EditAllProfiles) &&
                                   CSessionControl.GetUserIdFromSession(sessionKey) != profileId);

                return CServer.GetProfileData(profileId, isReadonly);
            }
            else
            {
                return new SProfileData();
            }
        }

        public SProfileData[] GetProfileList()
        {
            if (CServer.GetProfileList == null)
            {
                return new SProfileData[] { };
            }
            return CServer.GetProfileList();
        }

        #endregion

        #region photo

        public void SendPhoto(SPhotoData photo)
        {
            if (_CheckRight(EUserRights.UploadPhotos))
            {
                CServer.SendPhoto(photo);
            }
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
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = "Wrong username or password";
                }
            }
            return sessionId;
        }

        public Stream Index()
        {
            if (WebOperationContext.Current != null)
            {
                WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            }

            return new MemoryStream(CServer.GetSiteFile("index.html"));
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

            byte[] data = CServer.GetSiteFile("js/" + filename);

            if (data != null)
            {
                return new MemoryStream(data);
            }

            if (WebOperationContext.Current != null)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotFound;
            }
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

            byte[] data = CServer.GetSiteFile("css/" + filename);

            if (data != null)
            {
                return new MemoryStream(data);
            }
            if (WebOperationContext.Current != null)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotFound;
            }
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

            byte[] data = CServer.GetSiteFile("css\\images\\" + filename);

            if (data != null)
            {
                return new MemoryStream(data);
            }
            if (WebOperationContext.Current != null)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotFound;
            }
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

            byte[] data = CServer.GetSiteFile("img/" + filename);

            if (data != null)
            {
                return new MemoryStream(data);
            }
            if (WebOperationContext.Current != null)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotFound;
            }
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

            byte[] data = CServer.GetSiteFile("locales/" + filename);

            if (data != null)
            {
                return new MemoryStream(data);
            }

            if (WebOperationContext.Current != null)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotFound;
            }
            return null;
        }

        public CBase64Image GetDelayedImage(string id)
        {
            return CServer.GetDelayedImage(id);
        }

        public bool IsServerOnline()
        {
            return true;
        }
       
        #endregion

        #region songs

        public SSongInfo GetSong(int songId)
        {
            return CServer.GetSong(songId);
        }

        public int GetCurrentSongId()
        {
            return CServer.GetCurrentSongId();
        }

        public SSongInfo[] GetAllSongs()
        {
            return CServer.GetAllSongs();
        }
        #endregion

        #region playlist

        public SPlaylistInfo[] GetPlaylists()
        {
            return CServer.GetPlaylists();
        }

        public SPlaylistInfo GetPlaylist(int playlistId)
        {
            return CServer.GetPlaylist(playlistId);
        }

        public void AddSongToPlaylist(int songId, int playlistId, bool allowDuplicates)
        {
            if (_CheckRight(EUserRights.AddSongToPlaylist))
            {
                CServer.AddSongToPlaylist(songId, playlistId, allowDuplicates);
            }
        }

        public void RemoveSongFromPlaylist(int position, int playlistId, int songId)
        {
            if (_CheckRight(EUserRights.RemoveSongsFromPlaylists))
            {
                CServer.RemoveSongFromPlaylist(position, playlistId, songId);
            }
        }

        public void MoveSongInPlaylist(int newPosition, int playlistId, int songId)
        {
            if (_CheckRight(EUserRights.ReorderPlaylists))
            {
                CServer.MoveSongInPlaylist(newPosition, playlistId, songId);
            }
        }

        public bool PlaylistContainsSong(int songId, int playlistId)
        {
            return CServer.PlaylistContainsSong(songId, playlistId);
        }

        public SPlaylistSongInfo[] GetPlaylistSongs(int playlistId)
        {
            return CServer.GetPlaylistSongs(playlistId);
        }

        public void RemovePlaylist(int playlistId)
        {
            if (_CheckRight(EUserRights.DeletePlaylists))
            {
                CServer.RemovePlaylist(playlistId);
            }
        }

        public int AddPlaylist(string playlistName)
        {
            if (_CheckRight(EUserRights.CreatePlaylists))
            {
                return CServer.AddPlaylist(playlistName);
            }
            return -1;
        }

        #endregion

        #region user management

        public int GetUserRole(int profileId)
        {
            return CServer.GetUserRole(profileId);
        }

        public void SetUserRole(int profileId, int userRole)
        {
            if (_CheckRight(EUserRights.EditAllProfiles))
            {
                CServer.SetUserRole(profileId, userRole);
            }
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
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = "No session";
                }
                return false;
            }

            if (!CSessionControl.RequestRight(sessionKey, requestedRight))
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
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
            {
                return false;
            }

            if (!CSessionControl.RequestRight(sessionKey, requestedRight))
            {
                return false;
            }

            return true;
        }

        #endregion
        
    }
}
