using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Draw;
using System.Diagnostics;

namespace Vocaluxe.Lib.Draw
{
    /// <summary>
    ///     Base class for graphic drivers
    ///     A few notes:
    ///     Writes to textures (TTextureType) should only be done in the main thread
    ///     Writes to the RefCount should only be done in the main thread and guarded by _Textures lock
    /// </summary>
    /// <typeparam name="TTextureType"></typeparam>
    abstract class CDrawBase<TTextureType> where TTextureType : CTextureBase, IDisposable
    {
        private enum EQueueAction
        {
            Add,
            Update,
            Delete
        }

        private struct STextureQueue
        {
            public readonly WeakReference TextureRef;
            /// <summary>
            ///     Only valid if Data is byte[]
            /// </summary>
            public readonly Size DataSize;
            public readonly object Data;
            public readonly EQueueAction Action;

            /// <summary>
            ///     Creates a new Queue entry
            /// </summary>
            /// <param name="textureRef"></param>
            /// <param name="action"></param>
            /// <param name="data">Texture data as a bitmap</param>
            public STextureQueue(CTextureRef textureRef, EQueueAction action, Bitmap data)
            {
                TextureRef = new WeakReference(textureRef);
                Action = action;
                Data = data;
                DataSize = new Size();
            }

            /// <summary>
            ///     Creates a new Queue entry
            /// </summary>
            /// <param name="textureRef"></param>
            /// <param name="action"></param>
            /// <param name="dataSize">Size of data</param>
            /// <param name="data">Texture data</param>
            public STextureQueue(CTextureRef textureRef, EQueueAction action, Size dataSize, byte[] data)
            {
                TextureRef = new WeakReference(textureRef);
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
        protected bool _Fullscreen;

        protected bool _Run;

        protected readonly CKeys _Keys = new CKeys();
        protected readonly CMouse _Mouse = new CMouse();

        private int _NextID;
        private readonly Object _MutexID = new object();

        // Maps texture IDs to textures. Multiple IDs can be mapped to one texture (-->texture.RefCount>1)
        private readonly Dictionary<int, TTextureType> _Textures = new Dictionary<int, TTextureType>();
        // Maps texturePaths to textures. Texture may have already been disposed (RefCount<=0)
        private readonly Dictionary<string, STextureCacheEntry> _TextureCache = new Dictionary<string, STextureCacheEntry>();
        private readonly Queue<STextureQueue> _TextureQueue = new Queue<STextureQueue>();
        private readonly Dictionary<string, Task<Size>> _BitmapsLoading = new Dictionary<string, Task<Size>>();
        private int _TextureCount;

        protected int _H;
        protected int _W;
        protected int _Y;
        protected int _X;

        protected int _BorderLeft;
        protected int _BorderRight;
        protected int _BorderTop;
        protected int _BorderBottom;
        protected EGeneralAlignment _CurrentAlignment = EGeneralAlignment.Middle;

        private int _MainThreadID;

        protected abstract void _ClearScreen();
        protected abstract void _AdjustNewBorders();
        protected abstract void _LeaveFullScreen();

        protected abstract void _OnAfterDraw();

        protected abstract void _OnBeforeDraw();

        protected abstract void _DoResize();

        protected abstract void _EnterFullScreen();

        public virtual bool Init()
        {
            _MainThreadID = Thread.CurrentThread.ManagedThreadId;
            return true;
        }

        /// <summary>
        ///     This makes sure, the function is called from within the main thread
        /// </summary>
        private void _EnsureMainThread()
        {
            Debug.Assert(_MainThreadID == Thread.CurrentThread.ManagedThreadId);
        }

        public virtual void Unload()
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

        protected void _AdjustAspect(bool reverse)
        {
            if (_W / (float)_H > CSettings.GetRenderAspect())
            {
                //The windows width is too big
                int old = _W;
                _W = (int)Math.Round(_H * CSettings.GetRenderAspect());
                int diff = old - _W;
                switch (_CurrentAlignment)
                {
                    case EGeneralAlignment.Start:
                        _X = 0;
                        break;
                    case EGeneralAlignment.Middle:
                        _X = diff / 2;
                        break;
                    case EGeneralAlignment.End:
                        _X = diff;
                        break;
                }
            }
            else
            {
                //The windows height is too big
                int old = _H;
                _H = (int)Math.Round(_W / CSettings.GetRenderAspect());
                int diff = old - _H;
                switch (_CurrentAlignment)
                {
                    case EGeneralAlignment.Start:
                        _Y = reverse ? diff : 0;
                        break;
                    case EGeneralAlignment.Middle:
                        _Y = diff / 2;
                        break;
                    case EGeneralAlignment.End:
                        _Y = reverse ? 0 : diff;
                        break;
                }
            }
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

        #region Textures

        #region private/protected
        /// <summary>
        ///     Returns the maximum area allowed for a texture based on the current quality
        /// </summary>
        /// <returns></returns>
        private static int _GetMaxTextureArea()
        {
            switch (CConfig.TextureQuality)
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
        ///     Adds the texture to the cache
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
        ///     Thread safe but you should hold the _TextureCache lock if you need the loader task
        /// </summary>
        /// <param name="texturePath"></param>
        /// <returns></returns>
        private CTextureRef _GetFromCache(string texturePath)
        {
            if (String.IsNullOrEmpty(texturePath))
                return null;
            lock (_Textures)
            {
                STextureCacheEntry cacheEntry;
                if (_TextureCache.TryGetValue(texturePath, out cacheEntry))
                {
                    if (cacheEntry.Texture.RefCount <= 0)
                        _TextureCache.Remove(texturePath);
                    else
                        return _GetTextureReference(cacheEntry.OrigSize, cacheEntry.Texture);
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
            Debug.Assert(origSize.Width > 0 && origSize.Height > 0);
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

        private static bool _IsTextureUsable(TTextureType texture, int dataWidth, int dataHeight)
        {
            if (texture == null)
                return false;
            if (texture.DataSize.Width != dataWidth || texture.DataSize.Height != dataHeight)
            {
                if (texture.W2 > dataWidth || texture.H2 > dataHeight)
                    return false; // Texture memory to small
                if (texture.W2 * 0.9 < dataWidth || texture.H2 * 0.9 < dataHeight)
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

        private void _CheckQueue()
        {
            _EnsureMainThread();
            lock (_TextureQueue)
            {
                while (_TextureQueue.Count > 0)
                {
                    STextureQueue q = _TextureQueue.Dequeue();
                    CTextureRef textureRef = q.TextureRef.Target as CTextureRef;
                    if (!q.TextureRef.IsAlive || textureRef == null || !_Textures.ContainsKey(textureRef.ID))
                        continue;

                    if (q.Action == EQueueAction.Add)
                    {
                        TTextureType texture = null;
                        // ReSharper disable CanBeReplacedWithTryCastAndCheckForNull
                        if (q.Data is Bitmap)
                            _WriteBitmapToTexture(ref texture, (Bitmap)q.Data);
                        else if (q.Data is byte[])
                            texture = _CreateAndFillTexture(q.DataSize, (byte[])q.Data);
                        else
                            throw new ArgumentException("q.Data is of invalid type");
                        // ReSharper restore CanBeReplacedWithTryCastAndCheckForNull
                        TTextureType oldTexture = _Textures[textureRef.ID];
                        _MergeTextures(oldTexture, texture);
                    }
                    else if (q.Action == EQueueAction.Update)
                    {
                        // ReSharper disable CanBeReplacedWithTryCastAndCheckForNull
                        if (q.Data is Bitmap)
                            UpdateTexture(textureRef, (Bitmap)q.Data);
                        else if (q.Data is byte[])
                            UpdateTexture(textureRef, q.DataSize.Width, q.DataSize.Height, (byte[])q.Data);
                        else
                            throw new ArgumentException("q.Data is of invalid type");
                        // ReSharper restore CanBeReplacedWithTryCastAndCheckForNull
                    }
                    else if (q.Action == EQueueAction.Delete)
                        RemoveTexture(ref textureRef);
                }
            }
        }

        /// <summary>
        ///     Replaces one oldTexture by newTexture summing the refCounts. Also disposes oldTexture
        ///     IMPORTANT: Caller MUST hold the _Textures lock!
        /// </summary>
        /// <param name="oldTexture"></param>
        /// <param name="newTexture"></param>
        private void _MergeTextures(TTextureType oldTexture, TTextureType newTexture)
        {
            Debug.Assert(oldTexture.RefCount > 0);
            newTexture.RefCount += oldTexture.RefCount;
            // ReSharper disable AccessToDisposedClosure
            IEnumerable<int> oldKeys = _Textures.Where(pair => pair.Value == oldTexture).Select(pair => pair.Key).ToArray();
            // ReSharper restore AccessToDisposedClosure
            foreach (int key in oldKeys)
                _Textures[key] = newTexture;
            oldTexture.Dispose();
        }

        /// <summary>
        ///     Checks if a texture is currently beeing loaded and waits for completion if it is <br />
        ///     Required for cached entries, but should not be called from inside any locks unless async is true
        /// </summary>
        /// <param name="textureRef"></param>
        /// <param name="filePath"></param>
        /// <param name="async">If true, do not block</param>
        private void _WaitForTextureLoaded(CTextureRef textureRef, string filePath, bool async)
        {
            // Check if texture is already loaded
            if (textureRef.OrigSize.Width > 0)
                return;
            Task<Size> loader;
            lock (_BitmapsLoading)
            {
                if (!_BitmapsLoading.TryGetValue(filePath, out loader))
                    Debug.Assert(false, "Corruption: Texture not loaded but no loader found");
            }
            // We found the texture in the cache, but its bitmap is not yet loaded
            // So wait for completion and update the origSize once it is available
            // Note: loader.Result blocks till completion of the task
            if (async)
                Task.Factory.StartNew(() => textureRef.OrigSize = loader.Result);
            else
                textureRef.OrigSize = loader.Result;
        }
        #endregion private/protected

        /// <summary>
        ///     Adds a texture and stores it in the VRam
        /// </summary>
        /// <param name="texturePath">The texture's filepath</param>
        /// <returns>A STexture object containing the added texture</returns>
        public CTextureRef AddTexture(string texturePath)
        {
            _EnsureMainThread();
            CTextureRef textureRef;
            lock (_TextureCache)
            {
                textureRef = _GetFromCache(texturePath);
            }
            if (textureRef != null)
            {
                _WaitForTextureLoaded(textureRef, texturePath, false);
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
            // Make the Get/Add Cache methods atomic
            lock (_TextureCache)
            {
                textureRef = _GetFromCache(texturePath);
                if (textureRef == null)
                {
                    _WriteBitmapToTexture(ref texture, bmp);

                    _AddToCache(texture, origSize, texturePath);
                }
            }
            if (textureRef == null)
                textureRef = _GetTextureReference(origSize, texture);
            else
                _WaitForTextureLoaded(textureRef, texturePath, false);
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
                CTextureRef textureRef = _GetTextureReference(w, h, _CreateTexture(new Size(-1, -1)));
                _TextureQueue.Enqueue(new STextureQueue(textureRef, EQueueAction.Add, new Size(w, h), data));
                return textureRef;
            }
        }

        public CTextureRef EnqueueTexture(Bitmap bmp)
        {
            CTextureRef textureRef = _GetTextureReference(bmp.GetSize(), _CreateTexture(new Size(-1, -1)));
            if (_RequiresResize(bmp.GetSize()))
            {
                Task.Factory.StartNew(() =>
                    {
                        Bitmap bmp2 = bmp.Resize(_GetNewTextureSize(bmp.Size));
                        bmp.Dispose();
                        lock (_TextureQueue)
                        {
                            _TextureQueue.Enqueue(new STextureQueue(textureRef, EQueueAction.Add, bmp2));
                        }
                    });
            }
            else
            {
                lock (_TextureQueue)
                {
                    _TextureQueue.Enqueue(new STextureQueue(textureRef, EQueueAction.Add, bmp));
                }
            }

            return textureRef;
        }

        public CTextureRef EnqueueTexture(String filePath)
        {
            // 3 important scenarios: (+ means after, || means in parallel)
            // 1) EnqueueTexture(fileA) (+ or ||) EnqueueTexture(fileA) --> No block on 2nd call and same texture returned
            // 2) EnqueueTexture(fileA) + AddTexture(fileA) before texture is loaded --> Wait in AddTexture for this to finish
            // 3) a) AddTexture(fileA) || b) EnqueueTexture(fileA) with a)getFromCache + b)getFromCache + a)addToCache + b)addToCache --> Only keep one copy
            // All solved with atomic get/add cache and _WaitForTextureLoaded

            CTextureRef textureRef;
            TTextureType texture = null;
            Task<Size> loader = null;
            // Make Get/Add cache atomic
            lock (_TextureCache)
            {
                textureRef = _GetFromCache(filePath);
                if (textureRef == null)
                {
                    Size invalidSize = new Size(-1, -1);
                    texture = _CreateTexture(invalidSize);
                    // This cache entry has to be updated once the real origSize is available
                    _AddToCache(texture, invalidSize, filePath);
                    textureRef = _GetTextureReference(invalidSize, texture);
                    // Add the loader to the dictionary in the same lock as the add cache call
                    // Avoids that we get a cache entry in AddTexture but no Task
                    loader = new Task<Size>(() => _LoadAndEnqueueBitmap(filePath, textureRef));
                    lock (_BitmapsLoading)
                    {
                        _BitmapsLoading.Add(filePath, loader);
                    }
                }
            }

            if (texture == null)
            {
                // Found in cache
                _WaitForTextureLoaded(textureRef, filePath, true);
                return textureRef;
            }

            Debug.Assert(loader != null);
            Debug.Assert(texture != null);
            loader.Start();
            return textureRef;
        }

        /// <summary>
        ///     Helper function for loading a bitmap in a task
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="textureRef"></param>
        /// <returns></returns>
        private Size _LoadAndEnqueueBitmap(string filePath, CTextureRef textureRef)
        {
            Size origSize;
            Bitmap bmp = CHelper.LoadBitmap(filePath);
            if (bmp == null)
            {
                RemoveTexture(ref textureRef); // Done asynchonously in the function
                origSize = new Size(-1, -1);
            }
            else
            {
                origSize = bmp.GetSize();
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
                }
                Size size = _GetNewTextureSize(bmp.GetSize());
                if (!size.Equals(bmp.GetSize()))
                {
                    Bitmap bmp2 = bmp.Resize(size);
                    bmp.Dispose();
                    bmp = bmp2;
                }
                lock (_TextureQueue)
                {
                    _TextureQueue.Enqueue(new STextureQueue(textureRef, EQueueAction.Add, bmp));
                }
            }
            lock (_BitmapsLoading)
            {
                _BitmapsLoading.Remove(filePath);
            }
            return origSize;
        }

        public void EnqueueTextureUpdate(CTextureRef textureRef, Bitmap bmp)
        {
            lock (_TextureQueue)
            {
                _TextureQueue.Enqueue(new STextureQueue(textureRef, EQueueAction.Update, bmp));
            }
        }

        /// <summary>
        ///     Updates the data of a texture
        /// </summary>
        /// <param name="textureRef">The texture to update</param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="data">A byte array containing the new texture's data</param>
        /// <returns>True if succeeded</returns>
        public void UpdateTexture(CTextureRef textureRef, int w, int h, byte[] data)
        {
            _EnsureMainThread();
            TTextureType texture;
            if (!_GetTexture(textureRef, out texture, false))
                return;
            bool reuseTexture = _IsTextureUsable(texture, w, h);
            if (reuseTexture && texture.RefCount == 1)
            {
                texture.DataSize = new Size(w, h);
                _WriteDataToTexture(texture, data);
            }
            else
            {
                _DisposeTexture(texture);
                texture = _CreateAndFillTexture(new Size(w, h), data);
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
            bool reuseTexture = _IsTextureUsable(texture, bmp.Width, bmp.Height);
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
            _EnsureMainThread();
            TTextureType texture;
            if (!_GetTexture(textureRef, out texture, false))
                return null;
            // If bitmap is not yet loaded, wait for it
            if (!String.IsNullOrEmpty(texture.TexturePath))
                _WaitForTextureLoaded(textureRef, texture.TexturePath, false);
            return _GetTextureReference(textureRef.OrigSize, texture);
        }

        /// <summary>
        ///     Removes a texture from the Vram
        /// </summary>
        /// <param name="textureRef">The texture to be removed</param>
        public void RemoveTexture(ref CTextureRef textureRef)
        {
            if (textureRef == null)
                return;
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
            textureRef = null;
        }

        public int GetTextureCount()
        {
            return _TextureCount;
        }
        #endregion

        // Resharper doesn't get that this is used -.-
        // ReSharper disable UnusedMemberHiearchy.Global
        /// <summary>
        ///     Starts the rendering
        /// </summary>
        public virtual void MainLoop()
        {
            // ReSharper restore UnusedMemberHiearchy.Global
            _EnsureMainThread();
            _Run = true;

            _Fullscreen = false;
            if (CConfig.FullScreen == EOffOn.TR_CONFIG_ON)
                _EnterFullScreen();
            else
                _DoResize(); //Resize window if aspect ratio is incorrect

            while (_Run)
            {
                _CheckQueue();

                //We want to begin drawing
                _OnBeforeDraw();

                //Clear the previous Frame
                _ClearScreen();
                if (!CGraphics.Draw())
                    _Run = false;
                _OnAfterDraw();

                if (!CGraphics.UpdateGameLogic(_Keys, _Mouse))
                    _Run = false;

                //Apply fullscreen mode
                if ((CConfig.FullScreen == EOffOn.TR_CONFIG_ON) != _Fullscreen)
                    _ToggleFullScreen();

                //Apply border changes
                if (_BorderLeft != CConfig.BorderLeft || _BorderRight != CConfig.BorderRight || _BorderTop != CConfig.BorderTop || _BorderBottom != CConfig.BorderBottom)
                {
                    _BorderLeft = CConfig.BorderLeft;
                    _BorderRight = CConfig.BorderRight;
                    _BorderTop = CConfig.BorderTop;
                    _BorderBottom = CConfig.BorderBottom;

                    _AdjustNewBorders();
                }

                if (_CurrentAlignment != CConfig.ScreenAlignment)
                    _DoResize();

                if (CConfig.VSync == EOffOn.TR_CONFIG_OFF)
                {
                    if (CTime.IsRunning())
                    {
                        int delay = (int)Math.Floor(CConfig.CalcCycleTime() - CTime.GetMilliseconds());

                        if (delay >= 1 && delay < 500)
                            Thread.Sleep(delay);
                    }
                }
                //Calculate the FPS Rate and restart the timer after a frame
                CTime.CalculateFPS();
                CTime.Restart();
            }
        }

        private void _ToggleFullScreen()
        {
            if (!_Fullscreen)
                _EnterFullScreen();
            else
                _LeaveFullScreen();
        }
    }
}