using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.ServiceModel.Web;
using System.Web.Services;
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
        bool sendKeyEvent(string key);

        #region profile

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getOwnProfileId")]
        int getOwnProfileId();

        [OperationContract]
        [WebInvoke(Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/sendProfile")]
        bool sendProfile(ProfileData profile);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getProfile?profileId={profileId}")]
        ProfileData getProfile(int profileId);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getProfileList")]
        ProfileData[] getProfileList();

        #endregion

        #region photo

        [OperationContract]
        [WebInvoke(Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/sendPhoto")]
        bool sendPhoto(PhotoData photo);

        #endregion

        #region website

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/login?username={username}&password={password}")]
        Guid login(string username, string password);

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
        Base64Image GetDelayedImage(String id);

        #endregion

        #region songs

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getSong?songId={songId}")]
        SongInfo getSong(int songId);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getCurrentSongId")]
        int getCurrentSongId();

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getAllSongs")]
        SongInfo[] getAllSongs();

        #endregion

        #region user management

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getUserRole?profileId={profileId}")]
        int getUserRole(int profileId);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/setUserRole?profileId={profileId}&userRole={userRole}")]
        void setUserRole(int profileId, int userRole);

        #endregion
    }

    class CWebservice : ICWebservice
    {
        public CWebservice()
        {
        }

        private static Guid getSession()
        {
            Guid sessionKey = Guid.Empty;
            string sessionHeader =
                ((HttpRequestMessageProperty)OperationContext.Current.IncomingMessageProperties["httpRequest"]).Headers["session"];
            if (sessionHeader == null || sessionHeader == "")
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

        public bool sendKeyEvent(string key)
        {
            Guid sessionKey = getSession();

            if (sessionKey == Guid.Empty || !SessionControl.requestRight(sessionKey, UserRights.UseKeyboard))
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                return false;
            }

            if (CServer.SendKeyEvent == null)
            {
                return false;
            }
            return CServer.SendKeyEvent(key);
        }

        #region profile

        public int getOwnProfileId()
        {
            Guid sessionKey = getSession();
            if (sessionKey == Guid.Empty)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                return -1;
            }

            return SessionControl.getUserIdFromSession(sessionKey);
        }

        public bool sendProfile(ProfileData profile)
        {
            Guid sessionKey = getSession();

            if (profile.ProfileId != -1) //-1 is the id for a new profile
            {
                if (sessionKey == Guid.Empty || (!SessionControl.requestRight(sessionKey, UserRights.EditAllProfiles) &&
                    SessionControl.getUserIdFromSession(sessionKey) != profile.ProfileId))
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                    return false;
                }
            }

            if (CServer.SendProfileData == null)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotFound;
                return false;
            }

            bool result = CServer.SendProfileData(profile);

            if (profile.Password != null && profile.Password != "")
            {
                int profileId = profile.ProfileId;
                if (profileId == -1)
                {
                    profileId = CServer.GetUserIdFromUsername(profile.PlayerName);
                }

                CServer.SetPassword(profileId, profile.Password);
            }

            return result;
        }

        public ProfileData getProfile(int profileId)
        {
            Guid sessionKey = getSession();

            if (sessionKey == Guid.Empty || (!SessionControl.requestRight(sessionKey, UserRights.ViewOtherProfiles) &&
                SessionControl.getUserIdFromSession(sessionKey) != profileId))
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                return new ProfileData();
            }

            if (CServer.GetProfileData == null)
            {
                return new ProfileData();
            }

            bool isReadonly = (!SessionControl.requestRight(sessionKey, UserRights.EditAllProfiles) &&
                SessionControl.getUserIdFromSession(sessionKey) != profileId);

            return CServer.GetProfileData(profileId, isReadonly);
        }

        public ProfileData[] getProfileList()
        {
            if (CServer.GetProfileList == null)
            {
                return new ProfileData[] { };
            }
            return CServer.GetProfileList();
        }

        #endregion

        #region photo

        public bool sendPhoto(PhotoData photo)
        {
            Guid sessionKey = getSession();
            if (!SessionControl.requestRight(sessionKey, UserRights.UploadPhotos))
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                return false;
            }

            return CServer.SendPhoto(photo);
        }

        #endregion

        #region website

        public Guid login(string username, string password)
        {
            Guid sessionId = SessionControl.openSession(username, password);
            if (sessionId == Guid.Empty)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
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
            else
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotFound;
                return null;
            }
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
            else
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotFound;
                return null;
            }


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
            else
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotFound;
                return null;
            }
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
            else
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotFound;
                return null;
            }
        }

        public Base64Image GetDelayedImage(string id)
        {
            return CServer.GetDelayedImage(id);
        }

        #endregion

        #region songs

        public SongInfo getSong(int songId)
        {
            return CServer.GetSong(songId);
        }

        public int getCurrentSongId()
        {
            return CServer.GetCurrentSongId();
        }

        public SongInfo[] getAllSongs()
        {
            return CServer.GetAllSongs();
        }

        #endregion

        #region user management 

        public int getUserRole(int profileId)
        {
            return CServer.GetUserRole(profileId);
        }

        public void setUserRole(int profileId, int userRole)
        {
            Guid sessionKey = getSession();
            if (!SessionControl.requestRight(sessionKey, UserRights.EditAllProfiles))
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
                return;
            }
            CServer.SetUserRole(profileId, userRole);
        }

        #endregion
    }
}
