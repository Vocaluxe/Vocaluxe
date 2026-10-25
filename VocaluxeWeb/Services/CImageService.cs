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
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace VocaluxeWeb.Services
{
    /// Scaled JPEG versions of covers and avatars, cached on disk.
    /// Uses System.Drawing, which is fine on .NET Framework; replace it (e.g. with SkiaSharp) when moving to .NET 10 / Linux.
    public sealed class CImageService
    {
        private static readonly int[] _Sizes = { 96, 200, 400 };
        private const long _JpegQuality = 82L;
        private const int _MaxAvatarSize = 512;

        private readonly string _CacheFolder;
        private readonly object _Lock = new object();

        public CImageService(string cacheFolder)
        {
            _CacheFolder = cacheFolder;
        }

        /// Path of a cached, square-bounded JPEG of the image, or null if the source does not exist.
        public string GetScaled(string sourcePath, int requestedSize, out string etag)
        {
            etag = null;
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            {
                return null;
            }

            var size = _Sizes.FirstOrDefault(s => s >= requestedSize);
            if (size == 0)
            {
                size = _Sizes[_Sizes.Length - 1];
            }

            var key = _Hash(sourcePath + "|" + File.GetLastWriteTimeUtc(sourcePath).Ticks + "|" + size + "|white");
            etag = "\"" + key + "\"";
            var target = Path.Combine(_CacheFolder, key + ".jpg");
            if (File.Exists(target))
            {
                return target;
            }

            lock (_Lock)
            {
                if (!File.Exists(target))
                {
                    Directory.CreateDirectory(_CacheFolder);
                    using (var source = _Load(sourcePath))
                    using (var scaled = _Resize(source, size, false))
                    {
                        var temp = target + ".tmp";
                        _SaveJpeg(scaled, temp);
                        File.Move(temp, target);
                    }
                }
            }

            return target;
        }

        /// Turns an uploaded picture (JPEG, PNG, ...) into a square JPEG suitable as avatar.
        public static byte[] CreateAvatar(byte[] uploaded)
        {
            using (var input = new MemoryStream(uploaded))
            using (var source = Image.FromStream(input))
            {
                _ApplyExifOrientation(source);
                using (var square = _Resize(source, Math.Min(_MaxAvatarSize, Math.Min(source.Width, source.Height)), true))
                using (var output = new MemoryStream())
                {
                    _SaveJpeg(square, output);
                    return output.ToArray();
                }
            }
        }

        private static Image _Load(string path)
        {
            //Read into memory so the file is not locked; the game may have it open at the same time
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                var buffer = new MemoryStream();
                file.CopyTo(buffer);
                buffer.Position = 0;
                return Image.FromStream(buffer);
            }
        }

        /// Fits the image into size x size, or crops it to a square of that size.
        private static Bitmap _Resize(Image source, int size, bool crop)
        {
            int width, height;
            var srcRect = new Rectangle(0, 0, source.Width, source.Height);
            if (crop)
            {
                var side = Math.Min(source.Width, source.Height);
                srcRect = new Rectangle((source.Width - side) / 2, (source.Height - side) / 2, side, side);
                width = height = size;
            }
            else
            {
                var scale = Math.Min(1.0, (double)size / Math.Max(source.Width, source.Height));
                width = Math.Max(1, (int)Math.Round(source.Width * scale));
                height = Math.Max(1, (int)Math.Round(source.Height * scale));
            }

            var result = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(result))
            {
                g.Clear(Color.White);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.DrawImage(source, new Rectangle(0, 0, width, height), srcRect, GraphicsUnit.Pixel);
            }

            return result;
        }

        private static void _SaveJpeg(Image image, string path)
        {
            using (var file = File.Create(path))
            {
                _SaveJpeg(image, file);
            }
        }

        private static void _SaveJpeg(Image image, Stream stream)
        {
            var codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            using (var parameters = new EncoderParameters(1))
            {
                parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, _JpegQuality);
                image.Save(stream, codec, parameters);
            }
        }

        /// Photos from phones are often stored rotated with an EXIF hint.
        private static void _ApplyExifOrientation(Image image)
        {
            const int orientationId = 0x0112;
            if (!image.PropertyIdList.Contains(orientationId))
            {
                return;
            }

            var rotate = RotateFlipType.RotateNoneFlipNone;
            switch (image.GetPropertyItem(orientationId).Value[0])
            {
                case 3:
                    rotate = RotateFlipType.Rotate180FlipNone;
                    break;
                case 6:
                    rotate = RotateFlipType.Rotate90FlipNone;
                    break;
                case 8:
                    rotate = RotateFlipType.Rotate270FlipNone;
                    break;
            }

            image.RotateFlip(rotate);
        }

        private static string _Hash(string text)
        {
            using (var sha = SHA256.Create())
            {
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(text)).Select(b => b.ToString("x2")));
            }
        }
    }
}
