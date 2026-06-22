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
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using SkiaSharp;

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

        /// <summary>
        ///     Wraps an image given as already-encoded bytes (e.g. raw file contents) as a base64 data URI.
        /// </summary>
        /// <param name="encodedData">The encoded image bytes (PNG/JPEG/...)</param>
        /// <param name="formatString">The image format/subtype, e.g. "png" or "jpeg"</param>
        public CBase64Image(byte[] encodedData, string formatString)
        {
            base64Data = "data:image/" + formatString + ";base64," + Convert.ToBase64String(encodedData);
        }

        public CBase64Image(string imageId)
        {
            this.imageId = imageId;
        }

        /// <summary>
        ///     Creates a base64 image from an image file. The original (already-encoded) bytes are embedded
        ///     unchanged; the format is detected via SkiaSharp's codec.
        /// </summary>
        public static CBase64Image FromFile(string fileName)
        {
            byte[] encodedData = File.ReadAllBytes(fileName);
            string formatString;
            using (var codec = SKCodec.Create(new MemoryStream(encodedData, false)))
                formatString = codec != null ? codec.EncodedFormat.ToString().ToLower() : "png";
            return new CBase64Image(encodedData, formatString);
        }

        /// <summary>
        ///     Decodes the embedded image and writes it to the given file path, encoding to the stored format.
        /// </summary>
        public void SaveTo(string filePath)
        {
            string onlyBase64Data = base64Data.Substring(base64Data.IndexOf(";base64,") + (";base64,").Length);
            byte[] imageData = Convert.FromBase64String(onlyBase64Data);
            using (var bitmap = SKBitmap.Decode(imageData))
            using (var image = SKImage.FromBitmap(bitmap))
            using (var encoded = image.Encode(_GetEncodedFormat(GetImageType()), 100))
            using (var stream = File.OpenWrite(filePath))
                encoded.SaveTo(stream);
        }

        public string GetImageType()
        {
            Match match = Regex.Match(base64Data, "(?<=data:image/)[a-zA-Z]+(?=;base64)");
            return match.Success ? match.Groups[0].Value : "";
        }

        private static SKEncodedImageFormat _GetEncodedFormat(string formatString)
        {
            switch (formatString)
            {
                case "jpeg":
                case "jpg":
                    return SKEncodedImageFormat.Jpeg;
                case "gif":
                    return SKEncodedImageFormat.Gif;
                case "bmp":
                    return SKEncodedImageFormat.Bmp;
                case "webp":
                    return SKEncodedImageFormat.Webp;
                default:
                    return SKEncodedImageFormat.Png;
            }
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