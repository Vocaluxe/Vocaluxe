using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VocaluxeLib.Utils
{
    // Modified version of https://docs.microsoft.com/en-us/dotnet/standard/collections/thread-safe/how-to-create-an-object-pool

    /// <summary>
    /// Pool for objects.
    /// </summary>
    /// <typeparam name="T">Type of the objects in the pool.</typeparam>
    public class CObjectPool<T>
    {
        private readonly ConcurrentBag<T> _Objects;
        private readonly Func<T> _ObjectGenerator;
        private readonly int _Poolsize;

        /// <summary>
        /// Create a new object pool.
        /// </summary>
        /// <param name="objectGenerator">Function witch generate a new object instance.</param>
        /// <param name="poolSize">Number of objects to keep in the pool.</param>
        public CObjectPool(Func<T> objectGenerator, int poolSize)
        {
            if (objectGenerator == null)
                throw new ArgumentNullException(nameof(objectGenerator));
            if (poolSize <= 0)
                throw new ArgumentNullException(nameof(poolSize));
            _Objects = new ConcurrentBag<T>();
            _ObjectGenerator = objectGenerator;
            _Poolsize = poolSize;
        }

        /// <summary>
        /// Get an object (from the pool or a new one).
        /// </summary>
        /// <returns>An object instance.</returns>
        public T GetObject()
        {
            T item;
            if (_Objects.TryTake(out item)) return item;
            return _ObjectGenerator();
        }

        /// <summary>
        /// Put an intance back to the pool.
        /// </summary>
        /// <param name="item">The object instance that is given back to the pool.</param>
        public void PutObject(T item)
        {
            if(_Objects.Count < _Poolsize)
                _Objects.Add(item);
            else
                (item as IDisposable)?.Dispose();
        }
    }
}
