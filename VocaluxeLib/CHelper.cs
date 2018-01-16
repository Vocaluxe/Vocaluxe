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
using VocaluxeLib.Log;

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

        /// <summary>
        ///     Places a rect within the bounds maintaining the aspectRatio and using the given aspect
        /// </summary>
        /// <param name="bounds">Bounds to fit the rect in</param>
        /// <param name="aspectRatio">The original aspectRatio of the rect/image/...</param>
        /// <param name="aspect">
        ///     Crop: No empty space in bounds but has overhanging parts (same on both sides)<br />
        ///     LetterBox: Fit the long side possibly leaving some space on the other (rect will be centered)<br />
        ///     StretcH: Just fit the rect in the bounds (rect=bounds)
        /// </param>
        public static SRectF FitInBounds(SRectF bounds, float aspectRatio, EAspect aspect)
        {
            if (aspect == EAspect.Stretch)
                return bounds;

            float boundsAspectRatio = bounds.W / bounds.H;

            float scaledWidth, scaledHeight;

            switch (aspect)
            {
                case EAspect.Crop:
                    if (boundsAspectRatio >= aspectRatio)
                    {
                        scaledWidth = bounds.W;
                        scaledHeight = bounds.W / aspectRatio;
                    }
                    else
                    {
                        scaledHeight = bounds.H;
                        scaledWidth = bounds.H * aspectRatio;
                    }
                    break;
                case EAspect.Zoom1:
                    if (boundsAspectRatio >= aspectRatio)
                    {
                        scaledWidth = bounds.W * 1.33f;
                        scaledHeight = bounds.W * 1.33f / aspectRatio;
                    }
                    else
                    {
                        scaledHeight = bounds.H / 1.33f;
                        scaledWidth = bounds.H / 1.33f * aspectRatio;
                    }
                    break;
                case EAspect.Zoom2:
                    if (boundsAspectRatio >= aspectRatio)
                    {
                        scaledWidth = bounds.W * 1.17f;
                        scaledHeight = bounds.W * 1.17f / aspectRatio;
                    }
                    else
                    {
                        scaledHeight = bounds.H / 1.17f;
                        scaledWidth = bounds.H / 1.17f * aspectRatio;
                    }
                    break;
                case EAspect.LetterBox:
                    if (boundsAspectRatio <= aspectRatio)
                    {
                        scaledWidth = bounds.W;
                        scaledHeight = bounds.W / aspectRatio;
                    }
                    else
                    {
                        scaledHeight = bounds.H;
                        scaledWidth = bounds.H * aspectRatio;
                    }
                    break;
                default:
                    return bounds;
            }
            float left = (bounds.W - scaledWidth) / 2 + bounds.X;
            float top = (bounds.H - scaledHeight) / 2 + bounds.Y;

            return new SRectF(left, top, scaledWidth, scaledHeight, bounds.Z);
        }

        /// <summary>
        ///     Returns a list with all files in the given path that match a given pattern
        /// </summary>
        /// <param name="path">Path to search</param>
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
            return ((float)x).IsInRange(bounds.X, bounds.X + bounds.W) && ((float)y).IsInRange(bounds.Y, bounds.Y + bounds.H);
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

        /// <summary>
        ///     Loads a bitmap from a file logging errors
        /// </summary>
        /// <param name="filePath">Full path to image file</param>
        /// <returns>Bitmap or null on error</returns>
        public static Bitmap LoadBitmap(string filePath)
        {
            if (!File.Exists(filePath))
            {
                CLog.Error("Can't find File: " + filePath);
                return null;
            }
            Bitmap bmp;
            try
            {
                bmp = new Bitmap(filePath);
            }
            catch (Exception)
            {
                CLog.Error("Error loading bitmap: " + filePath);
                return null;
            }
            return bmp;
        }
    }
}