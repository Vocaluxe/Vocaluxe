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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;

namespace VocaluxeLib
{
    public static class CHelper
    {
        private static readonly List<string> _SoundFileTypes = new List<string>
            {
                "*.mp3",
                "*.wma",
                "*.ogg",
                "*.wav"
            };

        private static readonly List<string> _ImageFileTypes = new List<string>
            {
                "*.jpg",
                "*.jpeg",
                "*.png",
                "*.gif"
            };

        public static int CombinationCount(int n, int k)
        {
            if (k > n)
                return 0;

            if (k == 0 || k == n)
                return 1;

            if (k < 0 || n <= 0)
                return 0; //is not defined

            if (k * 2 > n)
                k = n - k;

            long result = n;

            for (long i = 1; i <= k - 1; i++)
                result = result * (n - i) / (i + 1);
            return (int)result;
        }

        /// <summary>
        ///     Makes sure val is between min and max
        ///     Asserts that min&lt;=max
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns>Clamped value</returns>
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            Debug.Assert(min.CompareTo(max) <= 0);
            if (val.CompareTo(min) < 0)
                return min;
            if (val.CompareTo(max) > 0)
                return max;
            return val;
        }

        /// <summary>
        ///     Makes sure val is between min and max but also handles the case where min&gt;max
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="preferMin"></param>
        /// <returns>Clamped value</returns>
        public static T Clamp<T>(this T val, T min, T max, bool preferMin) where T : IComparable<T>
        {
            if (min.CompareTo(max) > 0)
            {
                if (preferMin)
                    max = min;
                else
                    min = max;
            }
            return Clamp(val, min, max);
        }

        /// <summary>
        ///     Concat strings into one string with ", " as separator.
        /// </summary>
        public static string ListStrings(string[] str)
        {
            string result = String.Empty;
            for (int i = 0; i < str.Length; i++)
            {
                result += str[i];
                if (i < str.Length - 1)
                    result += ", ";
            }

            return result;
        }

        public static void SetRect(SRectF bounds, out SRectF rect, float rectAspect, EAspect aspect)
        {
            var bounds2 = new RectangleF(bounds.X, bounds.Y, bounds.W, bounds.H);
            RectangleF rect2;
            SetRect(bounds2, out rect2, rectAspect, aspect);

            rect = new SRectF(rect2.X, rect2.Y, rect2.Width, rect2.Height, bounds.Z);
        }

        public static void SetRect(RectangleF bounds, out RectangleF rect, float rectAspect, EAspect aspect)
        {
            float boundsW = bounds.Width;
            float boundsH = bounds.Height;
            float boundsAspect = boundsW / boundsH;

            float scaledWidth;
            float scaledHeight;

            switch (aspect)
            {
                case EAspect.Crop:
                    if (boundsAspect >= rectAspect)
                    {
                        scaledWidth = boundsW;
                        scaledHeight = boundsW / rectAspect;
                    }
                    else
                    {
                        scaledHeight = boundsH;
                        scaledWidth = boundsH * rectAspect;
                    }
                    break;
                case EAspect.LetterBox:
                    if (boundsAspect <= rectAspect)
                    {
                        scaledWidth = boundsW;
                        scaledHeight = boundsW / rectAspect;
                    }
                    else
                    {
                        scaledHeight = boundsH;
                        scaledWidth = boundsH * rectAspect;
                    }
                    break;
                default:
                    scaledWidth = boundsW;
                    scaledHeight = boundsH;
                    break;
            }

            float left = (boundsW - scaledWidth) / 2 + bounds.Left;
            float upper = (boundsH - scaledHeight) / 2 + bounds.Top;

            rect = new RectangleF(left, upper, scaledWidth, scaledHeight);
        }

        /// <summary>
        ///     Returns a list with all files in the given path that match a given pattern
        /// </summary>
        /// <param name="path">Path to search for</param>
        /// <param name="searchPattern">Pattern to match (e.g. "*.jpg")</param>
        /// <param name="recursive">Search directories recursively</param>
        /// <param name="fullpath">False for just file names, True for full path</param>
        /// <returns>List of file names</returns>
        public static List<string> ListFiles(string path, string searchPattern, bool recursive = false, bool fullpath = false)
        {
            var files = new List<string>();
            var dir = new DirectoryInfo(path);
            if (!dir.Exists)
                return files;

            try
            {
                // ReSharper disable LoopCanBeConvertedToQuery
                foreach (FileInfo file in dir.GetFiles(searchPattern))
                    // ReSharper restore LoopCanBeConvertedToQuery
                    files.Add(!fullpath ? file.Name : file.FullName);

                if (recursive)
                {
                    foreach (DirectoryInfo di in dir.GetDirectories())
                        files.AddRange(ListFiles(di.FullName, searchPattern, true, fullpath));
                }
            }
            catch (Exception) {}

            return files;
        }

        /// <summary>
        ///     Returns a list with all files in the given path that matches at least one of the given patterns
        /// </summary>
        /// <param name="path">Path to search for</param>
        /// <param name="searchPatterns">List of patterns to match</param>
        /// <param name="recursive">Search directories recursively</param>
        /// <param name="fullpath">False for just file names, True for full path</param>
        /// <returns>List of file names</returns>
        public static List<string> ListFiles(string path, IEnumerable<string> searchPatterns, bool recursive = false, bool fullpath = false)
        {
            var files = new List<string>();
            foreach (string pattern in searchPatterns)
                files.AddRange(ListFiles(path, pattern, recursive, fullpath));

            return files;
        }

        /// <summary>
        ///     Returns a list with all image files in the given path
        /// </summary>
        /// <param name="path">Path to search for</param>
        /// <param name="recursive">Search directories recursively</param>
        /// <param name="fullpath">False for just file names, True for full path</param>
        /// <returns>List of image file names</returns>
        public static List<string> ListImageFiles(string path, bool recursive = false, bool fullpath = false)
        {
            return ListFiles(path, _ImageFileTypes, recursive, fullpath);
        }

        /// <summary>
        ///     Returns a list with all image files in the given path
        /// </summary>
        /// <param name="path">Path to search for</param>
        /// <param name="recursive">Search directories recursively</param>
        /// <param name="fullpath">False for just file names, True for full path</param>
        /// <returns>List of image file names</returns>
        public static List<string> ListSoundFiles(string path, bool recursive = false, bool fullpath = false)
        {
            return ListFiles(path, _SoundFileTypes, recursive, fullpath);
        }

        public static bool TryParse<T>(string value, out T result, bool ignoreCase = false)
            where T : struct
        {
            result = default(T);
            try
            {
                result = (T)Enum.Parse(typeof(T), value, ignoreCase);
                return true;
            }
            catch {}

            return false;
        }

        public static bool TryParse(string value, out float result)
        {
            value = value.Replace(',', '.');
            return Single.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo, out result);
        }

        public static bool IsInBounds(SRectF bounds, SMouseEvent mouseEvent)
        {
            return IsInBounds(bounds, mouseEvent.X, mouseEvent.Y);
        }

        public static bool IsInBounds(SRectF bounds, int x, int y)
        {
            return (bounds.X <= x) && (bounds.X + bounds.W >= x) && (bounds.Y <= y) && (bounds.Y + bounds.H >= y);
        }

        /// <summary>
        ///     Returns a filename that is unique in that path
        /// </summary>
        /// <param name="path">Path in which the file should be stored</param>
        /// <param name="filename">filename (including extension)</param>
        /// <param name="withPath">Whether the fullpath or only the filename should be returned</param>
        /// <returns></returns>
        public static string GetUniqueFileName(string path, string filename, bool withPath = true)
        {
            string ext = Path.GetExtension(filename);
            filename = Path.GetFileNameWithoutExtension(filename) ?? "1";
            if (File.Exists(Path.Combine(path, filename + ext)))
            {
                int i = 1;
                while (File.Exists(Path.Combine(path, filename + "_" + i + ext)))
                    i++;
                filename += "_" + i;
            }
            if (withPath)
                filename = Path.Combine(path, filename);
            return filename + ext;
        }

        public static int Sum(int n)
        {
            return (n * n + n) / 2;
        }
    }

    static class CEncoding
    {
        public static Encoding GetEncoding(this string encodingName)
        {
            switch (encodingName)
            {
                case "AUTO":
                    return Encoding.Default;
                case "CP1250":
                    return Encoding.GetEncoding(1250);
                case "CP1252":
                    return Encoding.GetEncoding(1252);
                case "LOCALE":
                    return Encoding.Default;
                case "UTF8":
                    return Encoding.UTF8;
                default:
                    return Encoding.Default;
            }
        }

        public static string GetEncodingName(this Encoding enc)
        {
            string result = "UTF8";

            if (enc.CodePage == 1250)
                result = "CP1250";

            if (enc.CodePage == 1252)
                result = "CP1252";

            return result;
        }
    }
}