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
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;

namespace VocaluxeLib
{
    public static class CHelper
    {
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

        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0)
                return min;
            if (val.CompareTo(max) > 0)
                return max;
            return val;
        }

        /// <summary>
        ///     Concat strings into one string with ", " as separator.
        /// </summary>
        public static string ListStrings(string[] str)
        {
            string result = string.Empty;
            for (int i = 0; i < str.Length; i++)
            {
                result += str[i];
                if (i < str.Length - 1)
                    result += ", ";
            }

            return result;
        }

        public static int TryReadInt(StreamReader sr)
        {
            string value = String.Empty;

            try
            {
                int tmp = sr.Peek();
                //Check for ' ', ?, ?, \n, \r, E
                while (tmp != 32 && tmp != 19 && tmp != 16 && tmp != 13 && tmp != 10 && tmp != 69)
                {
                    if (sr.EndOfStream)
                        break;

                    var chr = (char)sr.Read();
                    value += chr.ToString();
                    tmp = sr.Peek();
                }
            }
            catch (Exception)
            {
                return 0;
            }
            int result;
            return int.TryParse(value, out result) ? result : 0;
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
        /// Returns a list with all files in the given path that match a given pattern
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
        /// Returns a list with all image files in the given path
        /// Searches for: jpg, jpeg, png, gif
        /// </summary>
        /// <param name="path">Path to search for</param>
        /// <param name="recursive">Search directories recursively</param>
        /// <param name="fullpath">False for just file names, True for full path</param>
        /// <returns>List of image file names</returns>
        public static List<string> ListImageFiles(string path, bool recursive = false, bool fullpath = false)
        {
            List<string> files = ListFiles(path, "*.jpg", recursive, fullpath);
            files.AddRange(ListFiles(path, "*.jpeg", recursive, fullpath));
            files.AddRange(ListFiles(path, "*.png", recursive, fullpath));
            files.AddRange(ListFiles(path, "*.gif", recursive, fullpath));
            return files;
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
            return float.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo, out result);
        }

        public static bool IsInBounds(SRectF bounds, SMouseEvent mouseEvent)
        {
            return IsInBounds(bounds, mouseEvent.X, mouseEvent.Y);
        }

        public static bool IsInBounds(SRectF bounds, int x, int y)
        {
            return (bounds.X <= x) && (bounds.X + bounds.W >= x) && (bounds.Y <= y) && (bounds.Y + bounds.H >= y);
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