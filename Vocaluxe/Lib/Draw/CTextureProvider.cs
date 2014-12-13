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
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Lib.Draw
{
    /// <summary>
    ///     Base class for graphic drivers containing the handling of textures
    ///     A few notes:
    ///     Writes to textures (TTextureType) should only be done in the main thread
    ///     Writes to the RefCount of existing textures should only be done in the main thread and guarded by _Textures lock
    /// </summary>
    /// <typeparam name="TTextureType"></typeparam>
    abstract class CTextureProvider<TTextureType> where TTextureType : CTextureBase, IDisposable
    {
        private enum EQueueAction
        {
            Add,
            Update,
            Delete
        }

        private struct STextureQueue
        {
            public readonly object TextureOrRef;
            /// <summary>
            ///     Only valid if Data is byte[]
            /// </summary>
            public readonly Size DataSize;
            public readonly object Data;
            public readonly EQueueAction Action;

            /// <summary>
            ///     Creates a new Queue entry
            /// </summary>
            /// <param name="textureOrRef">TTextureType or CTextureRef</param>
            /// <param name="action"></param>
            /// <param name="data">Texture data as a bitmap</param>
            public STextureQueue(object textureOrRef, EQueueAction action, Bitmap data)
            {
                TextureOrRef = textureOrRef;
                Action = action;
                Data = data;
                DataSize = new Size();
            }

            /// <summary>
            ///     Creates a new Queue entry
            /// </summary>
            /// <param name="textureOrRef">TTextureType or CTextureRef</param>
            /// <param name="action"></param>
            /// <param name="dataSize">Size of data</param>
            /// <param name="data">Texture data</param>
            public STextureQueue(object textureOrRef, EQueueAction action, Size dataSize, byte[] data)
            {
                TextureOrRef = textureOrRef;
                Action = action;
                DataSize = dataSize;
                Data = data;
            }
        }

        private struct STextureCacheEntry
        {
            public TTextureType Texture;
            /// <summary>
            ///     Size of original image (e.g. of bmp)
            /// </summary>
            public Size OrigSize;
        }

        protected bool _NonPowerOf2TextureSupported;

        private int _NextID;
        private readonly Object _MutexID = new object();

        // Maps texture IDs to textures. Multiple IDs can be mapped to one texture (-->texture.RefCount>1)
        private readonly Dictionary<int, TTextureType> _Textures = new Dictionary<int, TTextureType>();
        // Maps texturePaths to textures. Texture may have already been disposed (RefCount<=0)
        private readonly Dictionary<string, STextureCacheEntry> _TextureCache = new Dictionary<string, STextureCacheEntry>();
        private readonly Queue<STextureQueue> _TextureQueue = new Queue<STextureQueue>();
        private readonly Dictionary<string, Task<Size>> _BitmapsLoading = new Dictionary<string, Task<Size>>();
        private int _TextureCount;

        private int _MainThreadID;

        public virtual bool Init()
        {
            _MainThreadID = Thread.CurrentThread.ManagedThreadId;
            return true;
        }

        public virtual void Close()
        {
            _EnsureMainThread();
            lock (_Textures)
            {
                //Dispose all textures
                foreach (TTextureType texture in _Textures.Values)
                {
                    if (texture != null && --texture.RefCount <= 0)
                        texture.Dispose();
                }
                _Textures.Clear();
                _TextureCache.Clear();
            }
        }

        /// <summary>
        ///     This makes sure, the function is called from within the main thread
        /// </summary>
        protected void _EnsureMainThread()
        {
            Debug.Assert(_MainThreadID == Thread.CurrentThread.ManagedThreadId);
        }

        /// <summary>
        ///     Calculates the next power of two if the device has the POW2 flag set
        /// </summary>
        /// <param name="n">The value of which the next power of two will be calculated</param>
        /// <returns>The next power of two</returns>
        protected int _CheckForNextPowerOf2(int n)
        {
            if (_NonPowerOf2TextureSupported)
                return n;
            if (n < 0)
                throw new ArgumentOutOfRangeException("n", "Must be positive.");
            return (int)Math.Pow(2, Math.Ceiling(Math.Log(n, 2)));
        }

        /// <summary>
        ///     Returns the maximum area allowed for a texture based on the current quality
        /// </summary>
        /// <returns></returns>
        private static int _GetMaxTextureArea()
        {
            switch (CConfig.Config.Graphics.TextureQuality)
            {
                case ETextureQuality.TR_CONFIG_TEXTURE_LOWEST:
                    return 128 * 128;
                case ETextureQuality.TR_CONFIG_TEXTURE_LOW:
                    return 256 * 128;
                case ETextureQuality.TR_CONFIG_TEXTURE_MEDIUM:
                    return 512 * 256;
                case ETextureQuality.TR_CONFIG_TEXTURE_HIGH:
                    return 1024 * 512;
                case ETextureQuality.TR_CONFIG_TEXTURE_HIGHEST:
                    return CSettings.RenderW * CSettings.RenderH;
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        ///     Returns true if a texture with the given size needs to be resized
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private static bool _RequiresResize(Size size)
        {
            int maxArea = _GetMaxTextureArea();
            int curArea = size.Width * size.Height;
            return curArea > maxArea;
        }

        /// <summary>
        ///     Calculates the size for a new texture using the quality and smart values for rescaling
        /// </summary>
        /// <returns>New size for the texture</returns>
        private Size _GetNewTextureSize(Size size)
        {
            Debug.Assert(size.Width > 0 && size.Height > 0);
            int maxArea = _GetMaxTextureArea();
            int curArea = size.Width * size.Height;
            if (curArea <= maxArea)
                return size;
            Debug.Assert(_RequiresResize(size));
            double factor = Math.Sqrt((double)maxArea / curArea);
            Size newSize = new Size((int)(size.Width * factor), (int)(size.Height * factor));
            if (!_NonPowerOf2TextureSupported)
            {
                if (size.Width < size.Height)
                {
                    newSize.Width = _CheckForNextPowerOf2(newSize.Width);
                    newSize.Height = _CheckForNextPowerOf2(maxArea / newSize.Width);
                    if (newSize.Width * newSize.Height > maxArea)
                        newSize.Height /= 2;
                }
                else
                {
                    newSize.Height = _CheckForNextPowerOf2(newSize.Height);
                    newSize.Width = _CheckForNextPowerOf2(maxArea / newSize.Height);
                    if (newSize.Width * newSize.Height > maxArea)
                        newSize.Width /= 2;
                }
            }
            Debug.Assert(newSize.Width * newSize.Height <= maxArea);
            return newSize;
        }

        protected abstract void _WriteDataToTexture(TTextureType texture, byte[] data);

        protected virtual void _WriteDataToTexture(TTextureType texture, IntPtr data)
        {
            byte[] dataArray = new byte[4 * texture.DataSize.Width * texture.DataSize.Height];
            Marshal.Copy(data, dataArray, 0, dataArray.Length);
            _WriteDataToTexture(texture, dataArray);
        }

        /// <summary>
        ///     Factory method to create a texture of the actual type <br />
        ///     The implementer has to create an invalid texture (IsLoaded=false) if dataSize=(-1,-1)
        /// </summary>
        /// <param name="dataSize"></param>
        /// <returns></returns>
        protected abstract TTextureType _CreateTexture(Size dataSize);

        /// <summary>
        ///     Creates the texture specified by the reference and fills it with the given data
        /// </summary>
        /// <param name="dataSize"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private TTextureType _CreateAndFillTexture(Size dataSize, byte[] data)
        {
            TTextureType texture = _CreateTexture(dataSize);
            _WriteDataToTexture(texture, data);
            return texture;
        }

        /// <summary>
        ///     Writes the specified bitmap to the texture, creating it if null<br />
        ///     Also does the resizing if required
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="bmp"></param>
        private void _WriteBitmapToTexture(ref TTextureType texture, Bitmap bmp)
        {
            Bitmap bmp2 = null;
            try
            {
                Size size = _GetNewTextureSize(bmp.GetSize());
                if (!size.Equals(bmp.GetSize()))
                {
                    bmp2 = bmp.Resize(size);
                    bmp = bmp2;
                }

                //Fill the new Bitmap with the texture data
                BitmapData bmpData = bmp.LockBits(bmp.GetRect(), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                if (texture == null)
                    texture = _CreateTexture(size);
                else
                    texture.DataSize = size;
                _WriteDataToTexture(texture, bmpData.Scan0);
                bmp.UnlockBits(bmpData);
            }
            finally
            {
                if (bmp2 != null)
                    bmp2.Dispose();
            }
        }

        /// <summary>
        ///     Adds the texture to the cache<br />
        ///     Asserts that the cache entry does not exist yet. So make Get/Add cache atomic through use of _TextureCache lock!<br />
        ///     Thread safe
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="origSize"></param>
        /// <param name="texturePath"></param>
        private void _AddToCache(TTextureType texture, Size origSize, string texturePath)
        {
            if (String.IsNullOrEmpty(texturePath))
                return;
            Debug.Assert(texture != null);
            lock (_Textures)
            {
                Debug.Assert(!_TextureCache.ContainsKey(texturePath));
                texture.TexturePath = texturePath;
                STextureCacheEntry cacheEntry = new STextureCacheEntry {OrigSize = origSize, Texture = texture};
                _TextureCache.Add(texturePath, cacheEntry);
            }
        }

        /// <summary>
        ///     Gets a new texture reference from the cache <br />
        ///     Thread safe
        /// </summary>
        /// <param name="texturePath"></param>
        /// <param name="loader">Task that loads the texture or null if not loading</param>
        /// <returns></returns>
        private CTextureRef _GetFromCache(string texturePath, out Task<Size> loader)
        {
            loader = null;
            if (String.IsNullOrEmpty(texturePath))
                return null;
            lock (_TextureCache)
                lock (_Textures)
                {
                    STextureCacheEntry cacheEntry;
                    if (_TextureCache.TryGetValue(texturePath, out cacheEntry))
                    {
                        if (cacheEntry.Texture.RefCount <= 0)
                            _TextureCache.Remove(texturePath);
                        else
                        {
                            if (cacheEntry.OrigSize.Width < 0)
                            {
                                lock (_BitmapsLoading)
                                {
                                    loader = _BitmapsLoading[texturePath];
                                }
                            }
                            return _GetTextureReference(cacheEntry.OrigSize, cacheEntry.Texture);
                        }
                    }
                }
            return null;
        }

        /// <summary>
        ///     Method used to add a texture to the texture list and return a TextureRef to it <br />
        ///     If you are using an already used texture, you have to hold the _Textures lock or call from the main thread! <br />
        ///     Thread safe
        /// </summary>
        /// <param name="origSize"></param>
        /// <param name="texture"></param>
        /// <returns></returns>
        protected CTextureRef _GetTextureReference(Size origSize, TTextureType texture)
        {
            Debug.Assert(origSize.Width > 0 && origSize.Height > 0 || origSize.Width == -1 && origSize.Height == -1);
            Debug.Assert(texture != null);
            int id;
            lock (_MutexID)
            {
                id = _NextID++;
            }
            CTextureRef textureRef = new CTextureRef(id, origSize);
            lock (_Textures)
            {
                if (texture.RefCount == 0)
                    _TextureCount++;
                texture.RefCount++;
                _Textures.Add(id, texture);
            }
            return textureRef;
        }

        /// <summary>
        ///     Method used to add a texture to the texture list and return a TextureRef to it <br />
        ///     Thread safe <br />
        ///     Convenience method
        /// </summary>
        /// <param name="origWidth"></param>
        /// <param name="origHeight"></param>
        /// <param name="texture"></param>
        /// <returns></returns>
        protected CTextureRef _GetTextureReference(int origWidth, int origHeight, TTextureType texture)
        {
            return _GetTextureReference(new Size(origWidth, origHeight), texture);
        }

        private static bool _IsTextureUsable(TTextureType texture, Size dataSize)
        {
            if (texture == null)
                return false;
            if (!texture.DataSize.Equals(dataSize))
            {
                if (texture.Size.Width < dataSize.Width || texture.Size.Height < dataSize.Height)
                    return false; // Texture memory to small
                if (texture.Size.Width * 0.9 > dataSize.Width || texture.Size.Height * 0.9 > dataSize.Height)
                    return false; // Texture memory to big
            }
            return true;
        }

        protected bool _GetTexture(CTextureRef textureRef, out TTextureType texture, bool checkDrawable = true)
        {
            if (textureRef == null)
            {
                texture = null;
                return false;
            }
            lock (_Textures)
            {
                return _Textures.TryGetValue(textureRef.ID, out texture) && (!checkDrawable || texture.IsLoaded);
            }
        }

        /// <summary>
        ///     Decreases the RefCount of a texture and disposes it if necessary
        /// </summary>
        /// <param name="texture"></param>
        private void _DisposeTexture(TTextureType texture)
        {
            _EnsureMainThread();
            lock (_Textures)
            {
                if (texture != null && --texture.RefCount <= 0)
                {
                    if (texture.TexturePath != null)
                        _TextureCache.Remove(texture.TexturePath);
                    texture.Dispose();
                    _TextureCount--;
                }
            }
        }

        protected void _CheckQueue()
        {
            _EnsureMainThread();
            lock (_TextureQueue)
            {
                while (_TextureQueue.Count > 0)
                {
                    STextureQueue q = _TextureQueue.Dequeue();

                    if (q.Action == EQueueAction.Add)
                    {
                        TTextureType oldTexture = q.TextureOrRef as TTextureType;
                        Debug.Assert(oldTexture != null, "Queued type is wrong");
                        Debug.Assert(!oldTexture.IsLoaded);
                        if (oldTexture.RefCount <= 0)
                        {
                            Bitmap bmp = q.Data as Bitmap;
                            if (bmp != null)
                                bmp.Dispose();
                            continue;
                        }
                        TTextureType texture = null;
                        // ReSharper disable CanBeReplacedWithTryCastAndCheckForNull
                        if (q.Data is Bitmap)
                        {
                            Bitmap bmp = (Bitmap)q.Data;
                            _WriteBitmapToTexture(ref texture, bmp);
                            bmp.Dispose();
                        }
                        else if (q.Data is byte[])
                            texture = _CreateAndFillTexture(q.DataSize, (byte[])q.Data);
                        else
                            throw new ArgumentException("q.Data is of invalid type");
                        // ReSharper restore CanBeReplacedWithTryCastAndCheckForNull
                        _MergeTextures(oldTexture, texture);
                    }
                    else
                    {
                        CTextureRef textureRef = q.TextureOrRef as CTextureRef;
                        Debug.Assert(textureRef != null, "Queued type is wrong");
                        if (!_Textures.ContainsKey(textureRef.ID))
                        {
                            Bitmap bmp = q.Data as Bitmap;
                            if (bmp != null)
                                bmp.Dispose();
                            continue;
                        }
                        if (q.Action == EQueueAction.Update)
                        {
                            // ReSharper disable CanBeReplacedWithTryCastAndCheckForNull
                            if (q.Data is Bitmap)
                            {
                                Bitmap bmp = (Bitmap)q.Data;
                                UpdateTexture(textureRef, bmp);
                                bmp.Dispose();
                            }
                            else if (q.Data is byte[])
                                UpdateTexture(textureRef, q.DataSize, (byte[])q.Data);
                            else
                                throw new ArgumentException("q.Data is of invalid type");
                            // ReSharper restore CanBeReplacedWithTryCastAndCheckForNull
                        }
                        else if (q.Action == EQueueAction.Delete)
                            RemoveTexture(ref textureRef);
                    }
                }
            }
        }

        /// <summary>
        ///     Replaces one oldTexture by newTexture summing the refCounts. Also disposes oldTexture
        /// </summary>
        /// <param name="oldTexture"></param>
        /// <param name="newTexture"></param>
        private void _MergeTextures(TTextureType oldTexture, TTextureType newTexture)
        {
            _EnsureMainThread();
            Debug.Assert(oldTexture.RefCount > 0);
            lock (_Textures)
            {
                newTexture.RefCount += oldTexture.RefCount;
                IEnumerable<int> oldKeys = _Textures.Where(pair => pair.Value == oldTexture).Select(pair => pair.Key).ToArray();
                foreach (int key in oldKeys)
                    _Textures[key] = newTexture;
                if (!String.IsNullOrEmpty(oldTexture.TexturePath))
                {
                    newTexture.TexturePath = oldTexture.TexturePath;
                    STextureCacheEntry cacheEntry;
                    if (_TextureCache.TryGetValue(newTexture.TexturePath, out cacheEntry))
                    {
                        cacheEntry.Texture = newTexture;
                        _TextureCache[newTexture.TexturePath] = cacheEntry;
                    }
                }
                oldTexture.Dispose();
            }
        }

        /// <summary>
        ///     Enqueues a bitmap to add or update the texture
        /// </summary>
        /// <param name="texture">TTextureType (Add) or CTextureRef(Update)</param>
        /// <param name="bmp"></param>
        /// <param name="action"></param>
        /// <param name="asyncResize">True if resizing should be done in an extra thread</param>
        private void _EnqueueTextureAddOrUpdate(Object texture, Bitmap bmp, EQueueAction action, bool asyncResize)
        {
            Debug.Assert(action == EQueueAction.Add || action == EQueueAction.Update);
            Debug.Assert(action != EQueueAction.Add || texture is TTextureType);
            Debug.Assert(action != EQueueAction.Update || texture is CTextureRef);
            if (_RequiresResize(bmp.GetSize()))
            {
                if (asyncResize)
                    Task.Factory.StartNew(() => _ResizeTextureAndEnqueue(texture, bmp, action));
                else
                    _ResizeTextureAndEnqueue(texture, bmp, action);
            }
            else
            {
                lock (_TextureQueue)
                {
                    _TextureQueue.Enqueue(new STextureQueue(texture, action, bmp));
                }
            }
        }

        /// <summary>
        ///     Helper function to resize a bitmap and enqueue it
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="bmp"></param>
        /// <param name="action"></param>
        private void _ResizeTextureAndEnqueue(object texture, Bitmap bmp, EQueueAction action)
        {
            Bitmap bmp2 = bmp.Resize(_GetNewTextureSize(bmp.Size));
            bmp.Dispose();
            lock (_TextureQueue)
            {
                _TextureQueue.Enqueue(new STextureQueue(texture, action, bmp2));
            }
        }

        /// <summary>
        ///     Helper function for loading a bitmap in a task
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="textureRef"></param>
        /// <param name="texture"></param>
        /// <returns></returns>
        private Size _LoadAndEnqueueBitmap(string filePath, CTextureRef textureRef, TTextureType texture)
        {
            Bitmap bmp = CHelper.LoadBitmap(filePath);
            if (bmp == null)
            {
                RemoveTexture(ref textureRef); // Done asynchonously in the function
                lock (_Textures)
                {
                    _TextureCache.Remove(filePath);
                    lock (_BitmapsLoading)
                    {
                        _BitmapsLoading.Remove(filePath);
                    }
                }
                return new Size(-1, -1);
            }
            Size origSize = bmp.GetSize();
            textureRef.OrigSize = origSize;
            // Update cache, use the same lock as in add/get cache methods
            lock (_Textures)
            {
                STextureCacheEntry cacheEntry;
                if (_TextureCache.TryGetValue(filePath, out cacheEntry))
                {
                    cacheEntry.OrigSize = origSize;
                    _TextureCache[filePath] = cacheEntry;
                }
                lock (_BitmapsLoading)
                {
                    _BitmapsLoading.Remove(filePath);
                }
            }
            _EnqueueTextureAddOrUpdate(texture, bmp, EQueueAction.Add, false);
            return origSize;
        }

        /// <summary>
        ///     Adds a texture and stores it in the VRam
        /// </summary>
        /// <param name="texturePath">The texture's filepath</param>
        /// <returns>A STexture object containing the added texture</returns>
        public CTextureRef AddTexture(string texturePath)
        {
            _EnsureMainThread();
            CTextureRef textureRef;
            Task<Size> loader;
            lock (_TextureCache)
            {
                textureRef = _GetFromCache(texturePath, out loader);
            }
            if (textureRef != null)
            {
                if (loader != null)
                    textureRef.OrigSize = loader.Result;
                Debug.Assert(textureRef.OrigSize.Width > 0);
                return textureRef;
            }

            Bitmap bmp = CHelper.LoadBitmap(texturePath);
            if (bmp == null)
                return null;
            try
            {
                textureRef = AddTexture(bmp, texturePath);
            }
            finally
            {
                bmp.Dispose();
            }
            return textureRef;
        }

        public CTextureRef AddTexture(Bitmap bmp)
        {
            _EnsureMainThread();
            return AddTexture(bmp, null);
        }

        /// <summary>
        ///     Adds a texture and stores it in the Vram
        /// </summary>
        /// <param name="bmp">The Bitmap of which the texure will be created from</param>
        /// <param name="texturePath"></param>
        /// <returns>A reference to the texture</returns>
        public CTextureRef AddTexture(Bitmap bmp, string texturePath)
        {
            _EnsureMainThread();
            if (bmp.Height == 0 || bmp.Width == 0)
                return null;

            CTextureRef textureRef;
            Size origSize = bmp.GetSize();
            TTextureType texture = null;
            Task<Size> loader;
            // Make the Get/Add Cache methods atomic
            lock (_TextureCache)
            {
                textureRef = _GetFromCache(texturePath, out loader);
                if (textureRef == null)
                {
                    _WriteBitmapToTexture(ref texture, bmp);

                    _AddToCache(texture, origSize, texturePath);
                }
            }
            if (textureRef == null)
                textureRef = _GetTextureReference(origSize, texture);
            else if (loader != null)
                textureRef.OrigSize = loader.Result;
            Debug.Assert(textureRef.OrigSize.Width > 0);
            return textureRef;
        }

        public CTextureRef AddTexture(int w, int h, byte[] data)
        {
            _EnsureMainThread();
            TTextureType texture = _CreateAndFillTexture(new Size(w, h), data);
            return _GetTextureReference(w, h, texture);
        }

        public CTextureRef EnqueueTexture(int w, int h, byte[] data)
        {
            lock (_TextureQueue)
            {
                TTextureType texture = _CreateTexture(new Size(-1, -1));
                CTextureRef textureRef = _GetTextureReference(w, h, texture);
                _TextureQueue.Enqueue(new STextureQueue(texture, EQueueAction.Add, new Size(w, h), data));
                return textureRef;
            }
        }

        public CTextureRef EnqueueTexture(Bitmap bmp)
        {
            TTextureType texture = _CreateTexture(new Size(-1, -1));
            CTextureRef textureRef = _GetTextureReference(bmp.GetSize(), texture);
            _EnqueueTextureAddOrUpdate(texture, bmp, EQueueAction.Add, true);

            return textureRef;
        }

        public CTextureRef EnqueueTexture(String filePath)
        {
            // 3 important scenarios: (+ means after, || means in parallel)
            // 1) EnqueueTexture(fileA) (+ or ||) EnqueueTexture(fileA) --> No block on 2nd call and same texture returned
            // 2) EnqueueTexture(fileA) + AddTexture(fileA) before texture is loaded --> Wait in AddTexture for this to finish
            // 3) a) AddTexture(fileA) || b) EnqueueTexture(fileA) with a)getFromCache + b)getFromCache + a)addToCache + b)addToCache --> Only keep one copy
            // All solved with atomic get/add cache and _WaitForTextureLoaded

            if (!File.Exists(filePath))
                return null;
            CTextureRef textureRef;
            TTextureType texture = null;
            Task<Size> loader;
            // Make Get/Add cache atomic
            lock (_TextureCache)
            {
                textureRef = _GetFromCache(filePath, out loader);
                if (textureRef == null)
                {
                    Debug.Assert(loader == null);
                    Size invalidSize = new Size(-1, -1);
                    texture = _CreateTexture(invalidSize);
                    // This cache entry has to be updated once the real origSize is available
                    _AddToCache(texture, invalidSize, filePath);
                    textureRef = _GetTextureReference(invalidSize, texture);
                    // Add the loader to the dictionary in the same lock as the add cache call
                    // Avoids that we get a cache entry in AddTexture but no Task
                    loader = new Task<Size>(() => _LoadAndEnqueueBitmap(filePath, textureRef, texture));
                    lock (_BitmapsLoading)
                    {
                        _BitmapsLoading.Add(filePath, loader);
                    }
                }
            }

            if (texture == null)
            {
                // Found in cache
                CTextureRef tmp = textureRef; // Workaround for implicitly captured closure warning (false positive)
                if (loader != null)
                    Task.Factory.StartNew(() => tmp.OrigSize = loader.Result);
                return textureRef;
            }

            Debug.Assert(loader != null);
            Debug.Assert(texture != null);
            loader.Start();
            return textureRef;
        }

        public void EnqueueTextureUpdate(CTextureRef textureRef, Bitmap bmp)
        {
            _EnqueueTextureAddOrUpdate(textureRef, bmp, EQueueAction.Update, true);
        }

        public void UpdateTexture(CTextureRef textureRef, int w, int h, byte[] data)
        {
            UpdateTexture(textureRef, new Size(w, h), data);
        }

        /// <summary>
        ///     Updates the data of a texture
        /// </summary>
        /// <param name="textureRef">The texture to update</param>
        /// <param name="dataSize"></param>
        /// <param name="data">A byte array containing the new texture's data</param>
        /// <returns>True if succeeded</returns>
        public void UpdateTexture(CTextureRef textureRef, Size dataSize, byte[] data)
        {
            _EnsureMainThread();
            TTextureType texture;
            if (!_GetTexture(textureRef, out texture, false))
                return;
            bool reuseTexture = _IsTextureUsable(texture, dataSize);
            if (reuseTexture && texture.RefCount == 1)
            {
                texture.DataSize = dataSize;
                _WriteDataToTexture(texture, data);
            }
            else
            {
                _DisposeTexture(texture);
                texture = _CreateAndFillTexture(dataSize, data);
                texture.RefCount = 1;
                _Textures[textureRef.ID] = texture;
            }
        }

        public void UpdateTexture(CTextureRef textureRef, Bitmap bmp)
        {
            _EnsureMainThread();
            TTextureType texture;
            if (!_GetTexture(textureRef, out texture, false))
                return;
            bool reuseTexture = _IsTextureUsable(texture, bmp.Size);
            if (reuseTexture && texture.RefCount == 1)
                _WriteBitmapToTexture(ref texture, bmp);
            else
            {
                _DisposeTexture(texture);
                texture = null;
                _WriteBitmapToTexture(ref texture, bmp);
                texture.RefCount = 1;
                _Textures[textureRef.ID] = texture;
            }
        }

        public CTextureRef CopyTexture(CTextureRef textureRef)
        {
            // Lock to make sure the texture is not disposed while we are in this method
            lock (_Textures)
            {
                TTextureType texture;
                if (!_GetTexture(textureRef, out texture, false))
                    return null;
                // If bitmap is not yet loaded, wait for it
                if (!String.IsNullOrEmpty(texture.TexturePath))
                {
                    Task<Size> loader;
                    lock (_BitmapsLoading)
                    {
                        _BitmapsLoading.TryGetValue(texture.TexturePath, out loader);
                    }
                    if (loader != null)
                        textureRef.OrigSize = loader.Result;
                }
                Debug.Assert(textureRef.OrigSize.Width > 0);
                return _GetTextureReference(textureRef.OrigSize, texture);
            }
        }

        /// <summary>
        ///     Removes a texture from the Vram
        /// </summary>
        /// <param name="textureRef">The texture to be removed</param>
        public void RemoveTexture(ref CTextureRef textureRef)
        {
            if (textureRef == null)
                return;
            if (textureRef.ID > 0)
            {
                if (_MainThreadID != Thread.CurrentThread.ManagedThreadId)
                {
                    lock (_TextureQueue)
                    {
                        _TextureQueue.Enqueue(new STextureQueue(textureRef, EQueueAction.Delete, null));
                    }
                    textureRef = null;
                    return;
                }
                lock (_Textures)
                {
                    TTextureType t;
                    if (_Textures.TryGetValue(textureRef.ID, out t))
                    {
                        _DisposeTexture(t);
                        _Textures.Remove(textureRef.ID);
                    }
                    textureRef.SetRemoved();
                }
            }
            textureRef = null;
        }

        public int GetTextureCount()
        {
            return _TextureCount;
        }
    }
}