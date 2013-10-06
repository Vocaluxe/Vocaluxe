using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.ServiceModel.Web;
using System.Web.Services;
using System.IO;

namespace ClientServerLib
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
    }

    class CWebservice : ICWebservice
    {
        public CWebservice()
        {
        }

        public bool sendKeyEvent(string key)
        {
            if (CServer.SendKeyEvent == null)
            {
                return false;
            }
            return CServer.SendKeyEvent(key);
        }

        #region profile

        public bool sendProfile(ProfileData profile)
        {
            if (CServer.SendProfileData == null)
            {
                return false;
            }
            return CServer.SendProfileData(profile);
        }

        public ProfileData getProfile(int profileId)
        {
            if (CServer.GetProfileData == null)
            {
                return new ProfileData();
            }
            return CServer.GetProfileData(profileId);
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
            return CServer.SendPhoto(photo);
        }

        #endregion

        #region website

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
        
    }
}
