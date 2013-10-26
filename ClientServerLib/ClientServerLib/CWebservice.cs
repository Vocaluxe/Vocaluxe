using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.IO;
using System.ServiceModel.Channels;

namespace ServerLib
{
    [ServiceContract]
    public interface ICWebservice
    {
        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/sendKeyEvent?key={key}")]
        void SendKeyEvent(string key);

        #region profile

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getOwnProfileId")]
        int GetOwnProfileId();

        [OperationContract]
        [WebInvoke(Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/sendProfile")]
        void SendProfile(SProfileData profile);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getProfile?profileId={profileId}")]
        SProfileData GetProfile(int profileId);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getProfileList")]
        SProfileData[] GetProfileList();

        #endregion

        #region photo

        [OperationContract]
        [WebInvoke(Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/sendPhoto")]
        void SendPhoto(SPhotoData photo);

        #endregion

        #region website

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/login?username={username}&password={password}")]
        Guid Login(string username, string password);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "")]
        Stream Index();

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/js/{filename}")]
        Stream GetJsFile(String filename);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/css/{filename}")]
        Stream GetCssFile(String filename);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/css/images/{filename}")]
        Stream GetCssImageFile(String filename);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/img/{filename}")]
        Stream GetImgFile(String filename);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/delayedImage?id={id}")]
        CBase64Image GetDelayedImage(String id);

        #endregion

        #region songs

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getSong?songId={songId}")]
        SOngInfo GetSong(int songId);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getCurrentSongId")]
        int GetCurrentSongId();

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getAllSongs")]
        SOngInfo[] GetAllSongs();

        #endregion

        #region user management

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getUserRole?profileId={profileId}")]
        int GetUserRole(int profileId);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/setUserRole?profileId={profileId}&userRole={userRole}")]
        void SetUserRole(int profileId, int userRole);

        #endregion
    }

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

            CServer.SendProfileData(profile);

            if (!string.IsNullOrEmpty(profile.Password))
            {
                int profileId = profile.ProfileId;
                if (profileId == -1)
                {
                    profileId = CServer.GetUserIdFromUsername(profile.PlayerName);
                }

                CServer.SetPassword(profileId, profile.Password);
            }
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

        public SOngInfo GetSong(int songId)
        {
            return CServer.GetSong(songId);
        }

        public int GetCurrentSongId()
        {
            return CServer.GetCurrentSongId();
        }

        public SOngInfo[] GetAllSongs()
        {
            return CServer.GetAllSongs();
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
                if (WebOperationContext.Current != null) {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = "No session";
                }
                return;
            }
            if (!CSessionControl.RequestRight(sessionKey, EUserRights.EditAllProfiles))
            {
                if (WebOperationContext.Current != null) {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = "Not allowed";
                }
                return;
            }
            CServer.SetUserRole(profileId, userRole);
        }

        #endregion
    }
}
