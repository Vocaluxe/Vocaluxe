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
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace VocaluxeLib
{
    public static class CExtensions
    {
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
        ///     Checks if the value is in the specified range
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static bool IsInRange<T>(this T val, T min, T max) where T : IComparable<T>
        {
            Debug.Assert(min.CompareTo(max) <= 0);
            return min.CompareTo(val) <= 0 && max.CompareTo(val) >= 0;
        }

        /// <summary>
        ///     Resizes the given list to the given size. Removes elements or adds default values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="size"></param>
        /// <param name="defaultValue"></param>
        public static void Resize<T>(this List<T> list, int size, T defaultValue = default(T))
        {
            int curSize = list.Count;
            if (size < curSize)
                list.RemoveRange(size, curSize - size);
            else if (size > curSize)
                list.AddRange(Enumerable.Repeat(defaultValue, size - curSize));
        }

        /// <summary>
        ///     Makes the list at least the given size adding the given default values.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="size"></param>
        /// <param name="defaultValue"></param>
        public static void EnsureSize<T>(this List<T> list, int size, T defaultValue)
        {
            int curSize = list.Count;
            if (size > curSize)
                list.AddRange(Enumerable.Repeat(defaultValue, size - curSize));
        }

        /// <summary>
        ///     Makes the list at least the given size adding a new instance of T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="size"></param>
        public static void EnsureSize<T>(this List<T> list, int size) where T : new()
        {
            if (size > list.Count)
                list.Add(new T());
        }

        /// <summary>
        ///     Gets all set bits starting from lowest (Bit 0)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IEnumerable<int> GetSetBits(this int value)
        {
            var result = new List<int>();
            int curBit = 0;
            //Evaluate as bitset
            while (value > 0)
            {
                if ((value & 1) != 0)
                    result.Add(curBit);
                value >>= 1;
                curBit++;
            }
            return result;
        }

        public static bool ContainsIgnoreCase(this string value, string other)
        {
            return value.IndexOf(other, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private static Regex _MultipleWhiteSpaceRegEx = new Regex(@" {2,}");

        public static string TrimMultipleWs(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return _MultipleWhiteSpaceRegEx.Replace(value, " ");
        }

        /// <summary>
        ///     Converts value to a string in fixed point notation using invariant formating (english style decimal point)
        /// </summary>
        /// <param name="value">Value to be converted</param>
        /// <returns>String representation of value</returns>
        public static string ToInvariantString(this float value)
        {
            return value.ToString("F", NumberFormatInfo.InvariantInfo);
        }

        public static SRectF Scale(this SRectF rect, float scale)
        {
            return new SRectF(
                rect.X - rect.W * (scale - 1f),
                rect.Y - rect.H * (scale - 1f),
                rect.W + 2 * rect.W * (scale - 1f),
                rect.H + 2 * rect.H * (scale - 1f),
                rect.Z);
        }

        public static List<T> Shuffle<T>(this List<T> list)
        {
            for (int n = list.Count - 1; n >= 1; n--)
            {
                int k = CBase.Game.GetRandom(n);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        }

        public static Size GetSize(this Bitmap bmp)
        {
            return new Size(bmp.Width, bmp.Height);
        }

        public static Rectangle GetRect(this Bitmap bmp)
        {
            return Rectangle.Round(new Rectangle(0, 0, bmp.Width, bmp.Height));
        }

        /// <summary>
        ///     Resizes the bitmap to the given size creating a copy of it
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="newSize"></param>
        /// <returns>Resized bitmap or null on error</returns>
        public static Bitmap Resize(this Bitmap bmp, Size newSize)
        {
            Bitmap result = null;
            try
            {
                //Create a new Bitmap with the new sizes
                result = new Bitmap(newSize.Width, newSize.Height);
                //Scale the texture
                using (Graphics g = Graphics.FromImage(result))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.DrawImage(bmp, result.GetRect());
                }
            }
            catch (Exception)
            {
                if (result != null)
                {
                    result.Dispose();
                    result = null;
                }
            }
            return result;
        }
    }
}