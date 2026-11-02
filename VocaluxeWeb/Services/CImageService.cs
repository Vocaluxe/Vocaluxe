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
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SkiaSharp;

namespace VocaluxeWeb.Services
{
    /// Scaled JPEG versions of covers and avatars, cached on disk.
    public sealed class CImageService
    {
        private static readonly int[] _Sizes = { 96, 200, 400 };
        private const int _JpegQuality = 82;
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
            using (var source = _Decode(input, true))
            using (var square = _Resize(source, Math.Min(_MaxAvatarSize, Math.Min(source.Width, source.Height)), true))
            using (var output = new MemoryStream())
            {
                _SaveJpeg(square, output);
                return output.ToArray();
            }
        }

        private static SKBitmap _Load(string path)
        {
            //Read into memory so the file is not locked; the game may have it open at the same time
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                var buffer = new MemoryStream();
                file.CopyTo(buffer);
                buffer.Position = 0;
                return _Decode(buffer, false);
            }
        }

        private static SKBitmap _Decode(Stream stream, bool applyOrientation)
        {
            using (var codec = SKCodec.Create(stream))
            {
                if (codec == null)
                {
                    throw new ArgumentException("Unsupported image format");
                }

                var bitmap = SKBitmap.Decode(codec);
                if (bitmap == null)
                {
                    throw new ArgumentException("Invalid image");
                }

                return applyOrientation ? _ApplyExifOrientation(bitmap, codec.EncodedOrigin) : bitmap;
            }
        }

        /// Fits the image into size x size, or crops it to a square of that size.
        private static SKBitmap _Resize(SKBitmap source, int size, bool crop)
        {
            int width, height;
            var srcRect = new SKRect(0, 0, source.Width, source.Height);
            if (crop)
            {
                var side = Math.Min(source.Width, source.Height);
                srcRect = SKRect.Create((source.Width - side) / 2, (source.Height - side) / 2, side, side);
                width = height = size;
            }
            else
            {
                var scale = Math.Min(1.0, (double)size / Math.Max(source.Width, source.Height));
                width = Math.Max(1, (int)Math.Round(source.Width * scale));
                height = Math.Max(1, (int)Math.Round(source.Height * scale));
            }

            var result = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);
            using (var canvas = new SKCanvas(result))
            using (var image = SKImage.FromBitmap(source))
            using (var paint = new SKPaint { IsAntialias = true })
            {
                canvas.Clear(SKColors.White);
                canvas.DrawImage(image, srcRect, new SKRect(0, 0, width, height), new SKSamplingOptions(SKCubicResampler.Mitchell), paint);
            }

            return result;
        }

        private static void _SaveJpeg(SKBitmap image, string path)
        {
            using (var file = File.Create(path))
            {
                _SaveJpeg(image, file);
            }
        }

        private static void _SaveJpeg(SKBitmap image, Stream stream)
        {
            image.Encode(stream, SKEncodedImageFormat.Jpeg, _JpegQuality);
        }

        /// Photos from phones are often stored rotated with an EXIF hint.
        private static SKBitmap _ApplyExifOrientation(SKBitmap image, SKEncodedOrigin origin)
        {
            float degrees;
            switch (origin)
            {
                case SKEncodedOrigin.BottomRight:
                    degrees = 180;
                    break;
                case SKEncodedOrigin.RightTop:
                    degrees = 90;
                    break;
                case SKEncodedOrigin.LeftBottom:
                    degrees = 270;
                    break;
                default:
                    return image;
            }

            var swap = degrees != 180;
            var rotated = new SKBitmap(swap ? image.Height : image.Width, swap ? image.Width : image.Height, image.ColorType, image.AlphaType);
            using (var canvas = new SKCanvas(rotated))
            using (var source = SKImage.FromBitmap(image))
            {
                canvas.Translate(rotated.Width / 2f, rotated.Height / 2f);
                canvas.RotateDegrees(degrees);
                canvas.Translate(-image.Width / 2f, -image.Height / 2f);
                canvas.DrawImage(source, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));
            }

            image.Dispose();
            return rotated;
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
