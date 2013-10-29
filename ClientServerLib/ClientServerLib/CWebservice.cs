using System;
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
            Guid sessionKey = _GetSession();

            if (sessionKey == Guid.Empty)
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = "No session";
                }
            }
            else if (!CSessionControl.RequestRight(sessionKey, EUserRights.UseKeyboard))
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = "Not allowed";
                }
            }
            else if (CServer.SendKeyEvent == null)
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
                if (sessionKey == Guid.Empty)
                {
                    if (WebOperationContext.Current != null)
                    {
                        WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                        WebOperationContext.Current.OutgoingResponse.StatusDescription = "No session";
                    }
                    return;
                }

                if (!CSessionControl.RequestRight(sessionKey, EUserRights.EditAllProfiles) &&
                    CSessionControl.GetUserIdFromSession(sessionKey) != profile.ProfileId)
                {
                    if (WebOperationContext.Current != null)
                    {
                        WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                        WebOperationContext.Current.OutgoingResponse.StatusDescription = "Not allowed";
                    }
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

            if (!string.IsNullOrEmpty(profile.Password))
            {
                int profileId = profile.ProfileId;
                if (profileId == -1)
                {
                    profileId = CServer.GetUserIdFromUsername(profile.PlayerName);
                }

                CServer.SetPassword(profileId, profile.Password);
            }

            CServer.SendProfileData(profile);
        }

        public SProfileData GetProfile(int profileId)
        {
            Guid sessionKey = _GetSession();

            if (sessionKey == Guid.Empty)
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = "No session";
                }
                return new SProfileData();
            }

            if (!CSessionControl.RequestRight(sessionKey, EUserRights.ViewOtherProfiles) &&
                CSessionControl.GetUserIdFromSession(sessionKey) != profileId)
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = "Not allowed";
                }
                return new SProfileData();
            }

            if (CServer.GetProfileData == null)
            {
                return new SProfileData();
            }

            bool isReadonly = (!CSessionControl.RequestRight(sessionKey, EUserRights.EditAllProfiles) &&
                CSessionControl.GetUserIdFromSession(sessionKey) != profileId);

            return CServer.GetProfileData(profileId, isReadonly);
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
            Guid sessionKey = _GetSession();
            if (sessionKey == Guid.Empty)
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = "No session";
                }
                return;
            }

            if (!CSessionControl.RequestRight(sessionKey, EUserRights.UploadPhotos))
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = "Not allowed";
                }
                return;
            }

            CServer.SendPhoto(photo);
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

        public CBase64Image GetDelayedImage(string id)
        {
            return CServer.GetDelayedImage(id);
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
            if (_CheckRight(EUserRights.AddSongToPlaylist)||_CheckRight(EUserRights.EditPlaylists))
            {
                CServer.AddSongToPlaylist(songId, playlistId, allowDuplicates);
            }
        }

        public void RemoveSongFromPlaylist(int position, int playlistId, int songId)
        {
            if (_CheckRight(EUserRights.EditPlaylists))
            {
                CServer.RemoveSongFromPlaylist(position, playlistId, songId);
            }
        }

        public void MoveSongInPlaylist(int oldPosition, int newPosition, int playlistId, int songId)
        {
            if (_CheckRight(EUserRights.EditPlaylists))
            {
                CServer.MoveSongInPlaylist(oldPosition, newPosition, playlistId, songId);
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

        public bool IsPlaylistEditable(int playlistId)
        {
            return _CheckRight(EUserRights.EditPlaylists);
        }
        
        #endregion

        #region user management

        public int GetUserRole(int profileId)
        {
            return CServer.GetUserRole(profileId);
        }

        public void SetUserRole(int profileId, int userRole)
        {
            Guid sessionKey = _GetSession();
            if (sessionKey == Guid.Empty)
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = "No session";
                }
                return;
            }
            if (!CSessionControl.RequestRight(sessionKey, EUserRights.EditAllProfiles))
            {
                if (WebOperationContext.Current != null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = "Not allowed";
                }
                return;
            }
            CServer.SetUserRole(profileId, userRole);
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

        #endregion
    }
}
