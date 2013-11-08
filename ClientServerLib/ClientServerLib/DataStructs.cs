﻿using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;

namespace ServerLib
{
    public delegate bool SendKeyEventDelegate(string key);


    #region profile

    public delegate SProfileData GetProfileDataDelegate(int profileId, bool isReadonly);
    public delegate bool SendProfileDataDelegate(SProfileData profile);
    public delegate SProfileData[] GetProfileListDelegate();

    [DataContract]
    public struct SProfileData
    {
        [DataMember]
        public CBase64Image Avatar;
        [DataMember]
        public string PlayerName;
        [DataMember]
        public int Type;
        [DataMember]
        public int Difficulty;
        [DataMember]
        public int ProfileId;
        [DataMember]
        public bool IsEditable;
        [DataMember]
        public string Password;
    }

    #endregion

    #region photo

    public delegate bool SendPhotoDelegate(SPhotoData photo);

    [DataContract]
    public struct SPhotoData
    {
        [DataMember]
        public CBase64Image Photo;
        //Add infomation about the user who took this image??
    }

    #endregion

    #region website

    public delegate byte[] GetSiteFileDelegate(string filename);

    public delegate CBase64Image GetDelayedImageDelegate(string hashedFilename);

    #endregion

    [DataContract]
    public class CBase64Image
    {
        [DataMember]
        private string base64Data = "";

        [DataMember]
        private string imageId = "";

        public CBase64Image(Image img, ImageFormat format)
        {
            MemoryStream ms = new MemoryStream();
            img.Save(ms, format);
            string formatString = ImageCodecInfo.GetImageEncoders().FirstOrDefault(x => x.FormatID == format.Guid).FilenameExtension.Replace("*.", "").ToLower();
            base64Data = "data:image/" + formatString + ";base64," + Convert.ToBase64String(ms.ToArray());
        }

        public CBase64Image(string imageId)
        {
            this.imageId = imageId;
        }

        public Image GetImage()
        {
            string onlyBase64Data = base64Data.Substring(base64Data.IndexOf(";base64,") + (";base64,").Length);
            byte[] imageData = Convert.FromBase64String(onlyBase64Data);
            MemoryStream ms = new MemoryStream(imageData, 0, imageData.Length);
            ms.Write(imageData, 0, imageData.Length);
            Image image = Image.FromStream(ms, true);
            return image;
        }

        public string GetImageType()
        {
            Match match = Regex.Match(base64Data, "(?<=data:image/)[a-zA-Z]+(?=;base64)");
            return match.Success ? match.Groups[0].Value : "";
        }


    }

    #region songs

    public delegate SSongInfo GetSongDelegate(int songId);

    public delegate SSongInfo[] GetAllSongsDelegate();

    public delegate int GetCurrentSongIdDelegate();
    
        
    [DataContract]
    public struct SSongInfo
    {
        [DataMember]
        public string Title;
        [DataMember]
        public string Artist;
        [DataMember]
        public CBase64Image Cover;
        [DataMember]
        public string Genre { get; set; }
        [DataMember]
        public string Language { get; set; }
        [DataMember]
        public string Year { get; set; }
        [DataMember]
        public bool IsDuet { get; set; }
        [DataMember]
        public int SongId { get; set; }
    }

    #endregion

    #region playlists
    public delegate SPlaylistInfo[] GetPlaylistsDelegate();

    public delegate SPlaylistInfo GetPlaylistDelegate(int playlistId);

    public delegate void AddSongToPlaylistDelegate(int songId, int playlistId, bool allowDuplicates);

    public delegate void RemoveSongFromPlaylistDelegate(int position, int playlistId, int songId);

    public delegate void MoveSongInPlaylistDelegate(int newPosition, int playlistId, int songId);

    public delegate bool PlaylistContainsSongDelegate(int songId, int playlistId);

    public delegate SPlaylistSongInfo[] GetPlaylistSongsDelegate(int playlistId);

    public delegate void RemovePlaylistDelegate(int playlistId);

    public delegate int AddPlaylistDelegate(string playlistName);

    [DataContract]
    public struct SPlaylistSongInfo
    {
        [DataMember] 
        public SSongInfo Song;
        [DataMember]
        public int PlaylistId;
        [DataMember] 
        public int PlaylistPosition;
        [DataMember]
        public int GameMode;
    }
    
    [DataContract]
    public struct SPlaylistInfo
    {
        [DataMember] 
        public int PlaylistId;
        [DataMember] 
        public string PlaylistName;
        [DataMember] 
        public int SongCount;
        [DataMember] 
        public string LastChanged;
    }

    #endregion

    #region user management

    public delegate void SetPasswordDelegate(int profileId, string newPassword);

    public delegate bool ValidatePasswordDelegate(int profileId, string password);

    public delegate int GetUserRoleDelegate(int profileId);

    public delegate void SetUserRoleDelegate(int profileId, int userRole);

    public delegate int GetUserIdFromUsernameDelegate(string username);

    #endregion
}
