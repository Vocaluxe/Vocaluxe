using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;

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
            UriTemplate = "/locales/{filename}")]
        Stream GetLocaleFile(String filename);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/delayedImage?id={id}")]
        CBase64Image GetDelayedImage(String id);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/isServerOnline")]
        bool IsServerOnline();

        #endregion

        #region songs

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getSong?songId={songId}")]
        SSongInfo GetSong(int songId);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getCurrentSongId")]
        int GetCurrentSongId();

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getAllSongs")]
        SSongInfo[] GetAllSongs();

        #endregion

        #region playlist

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getPlaylists")]
        SPlaylistInfo[] GetPlaylists();

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getPlaylist?id={playlistId}")]
        SPlaylistInfo GetPlaylist(int playlistId);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/addSongToPlaylist?songId={songId}&playlistId={playlistId}&duplicates={allowDuplicates}")]
        void AddSongToPlaylist(int songId, int playlistId, bool allowDuplicates);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/removeSongFromPlaylist?position={position}&playlistId={playlistId}&songId={songId}")]
        void RemoveSongFromPlaylist(int position, int playlistId, int songId);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/moveSongInPlaylist?newPosition={newPosition}&playlistId={playlistId}&songId={songId}")]
        void MoveSongInPlaylist(int newPosition, int playlistId, int songId);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/playlistContainsSong?songId={songId}&playlistId={playlistId}")]
        bool PlaylistContainsSong(int songId, int playlistId);

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getPlaylistSongs?playlistId={playlistId}")]
        SPlaylistSongInfo[] GetPlaylistSongs(int playlistId);

        [OperationContract, WebInvoke(Method = "GET",
           ResponseFormat = WebMessageFormat.Json,
           UriTemplate = "/removePlaylist?playlistId={playlistId}")]
        void RemovePlaylist(int playlistId);

        [OperationContract, WebInvoke(Method = "GET",
          ResponseFormat = WebMessageFormat.Json,
          UriTemplate = "/addPlaylist?playlistName={playlistName}")]
        int AddPlaylist(string playlistName);

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

        [OperationContract]
        [WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/hasUserRight?right={right}")]
        bool HasUserRight(int right);

        #endregion
    }
}