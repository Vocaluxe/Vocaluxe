using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VocaluxeLib
{
    public static class Extensions
    {
        /// <summary>
        /// Resizes the given list to the given size. Removes elements or adds default values
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
        /// Makes the list at least the given size adding the given default values.
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
        /// Makes the list at least the given size adding a new instance of T
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
        /// Gets all set bits starting from lowest (Bit 0)
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
    }
}