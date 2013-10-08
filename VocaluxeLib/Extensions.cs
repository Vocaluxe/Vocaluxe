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
    }
}