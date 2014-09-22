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
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;

namespace VocaluxeLib
{
    public static class CExtensions
    {
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

        public static string TrimMultipleWs(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            int start = 0;
            int len = value.Length;
            while (value[start] == ' ')
            {
                start++;
                if (start >= len)
                    return "";
            }
            if (start > 0)
                start--;
            int end = --len;
            while (value[end] == ' ')
                end--;
            if (end < len)
                end++;
            if (end - start < len)
                value = value.Substring(start, end - start + 1);
            return value;
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