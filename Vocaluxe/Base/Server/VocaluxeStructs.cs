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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Vocaluxe.Base.Server
{
    public struct SLoginData
    {
        public byte[] Sha256;
    }

    public struct SPicture
    {
        public int Width;
        public int Height;
        public byte[] Data;
    }

    public struct SProfile
    {
        public SPicture Avatar;
        public string PlayerName;
        public int Difficulty;
    }

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
        public Guid ProfileId;
        [DataMember]
        public bool IsEditable;
        [DataMember]
        public string Password;
    }

    [DataContract]
    public struct SPhotoData
    {
        [DataMember]
        public CBase64Image Photo;
        //Add infomation about the user who took this image??
    }

    [DataContract]
    public class CBase64Image
    {
        // ReSharper disable InconsistentNaming
        [DataMember]
        private string base64Data = "";
        [DataMember]
        private string imageId = "";
        // ReSharper restore InconsistentNaming

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
    public struct SPlaylistData
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
}