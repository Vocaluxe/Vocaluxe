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
using System.ServiceModel;
using System.ServiceModel.Web;

namespace Vocaluxe.Base.Server
{
    [ServiceContract]
    public interface ICWebservice
    {
        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/sendKeyEvent?key={key}")]
        void SendKeyEvent(string key);

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/sendKeyStringEvent?keyString={keyString}&shift={isShiftPressed}&alt={isAltPressed}&ctrl={isCtrlPressed}")]
        void SendKeyStringEvent(string keyString, bool isShiftPressed = false, bool isAltPressed = false, bool isCtrlPressed = false);

        #region profile
        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getOwnProfileId")]
        Guid GetOwnProfileId();

        [OperationContract, WebInvoke(Method = "POST",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/sendProfile")]
        void SendProfile(SProfileData profile);

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getProfile?profileId={profileId}")]
        SProfileData GetProfile(Guid profileId);

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getProfileList")]
        SProfileData[] GetProfileList();
        #endregion

        #region photo
        [OperationContract, WebInvoke(Method = "POST",
            RequestFormat = WebMessageFormat.Json,
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/sendPhoto")]
        void SendPhoto(SPhotoData photo);
        #endregion

        #region website
        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/login?username={username}&password={password}")]
        Guid Login(string username, string password);

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/logout")]
        void Logout();

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "")]
        Stream Index();

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/js/{filename}")]
        Stream GetJsFile(String filename);

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/css/{filename}")]
        Stream GetCssFile(String filename);

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/css/images/{filename}")]
        Stream GetCssImageFile(String filename);

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/img/{filename}")]
        Stream GetImgFile(String filename);

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/locales/{filename}")]
        Stream GetLocaleFile(String filename);

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/delayedImage?id={id}")]
        CBase64Image GetDelayedImage(String id);

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/isServerOnline")]
        bool IsServerOnline();

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getServerVersion")]
        String GetServerVersion();
        #endregion

        #region songs
        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getSong?songId={songId}")]
        SSongInfo GetSong(int songId);

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getCurrentSongId")]
        int GetCurrentSongId();

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getAllSongs")]
        SSongInfo[] GetAllSongs();

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getMp3?songId={songId}")]
        Stream GetMp3File(int songId);
        #endregion

        #region playlist
        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getPlaylists")]
        SPlaylistData[] GetPlaylists();

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getPlaylist?id={playlistId}")]
        SPlaylistData GetPlaylist(int playlistId);

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/addSongToPlaylist?songId={songId}&playlistId={playlistId}&duplicates={allowDuplicates}")]
        void AddSongToPlaylist(int songId, int playlistId, bool allowDuplicates);

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/removeSongFromPlaylist?position={position}&playlistId={playlistId}&songId={songId}")]
        void RemoveSongFromPlaylist(int position, int playlistId, int songId);

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/moveSongInPlaylist?newPosition={newPosition}&playlistId={playlistId}&songId={songId}")]
        void MoveSongInPlaylist(int newPosition, int playlistId, int songId);

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/playlistContainsSong?songId={songId}&playlistId={playlistId}")]
        bool PlaylistContainsSong(int songId, int playlistId);

        [OperationContract, WebInvoke(Method = "GET",
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
        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/getUserRole?profileId={profileId}")]
        int GetUserRole(Guid profileId);

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/setUserRole?profileId={profileId}&userRole={userRole}")]
        void SetUserRole(Guid profileId, int userRole);

        [OperationContract, WebInvoke(Method = "GET",
            ResponseFormat = WebMessageFormat.Json,
            UriTemplate = "/hasUserRight?right={right}")]
        bool HasUserRight(int right);
        #endregion
    }
}