using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Draw;
using System.Diagnostics;

namespace Vocaluxe.Lib.Draw
{
    abstract class CDrawBase<TTextureType> where TTextureType : CTextureBase, IDisposable
    {
        private enum EQueueAction
        {
            Add,
            Update
        }

        private struct STextureQueue
        {
            public readonly WeakReference TextureRef;
            public readonly Size DataSize;
            //Use either Data or DataBmp!
            public readonly byte[] Data;
            //This is disposed after use
            public readonly Bitmap DataBmp;
            public readonly EQueueAction Action;

            public STextureQueue(CTextureRef textureRef, EQueueAction action, Size dataSize, byte[] data)
            {
                TextureRef = new WeakReference(textureRef);
                Action = action;
                DataSize = dataSize;
                Data = data;
                DataBmp = null;
            }

            public STextureQueue(CTextureRef textureRef, EQueueAction action, Bitmap bmp)
            {
                TextureRef = new WeakReference(textureRef);
                Action = action;
                DataSize = new Size(bmp.Width, bmp.Height);
                Data = null;
                DataBmp = bmp;
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

        private readonly Object _MutexID = new object();
        private int _NextID;

        // Maps texture IDs to textures. Multiple IDs can be mapped to one texture (-->texture.RefCount>1)
        private readonly Dictionary<int, TTextureType> _Textures = new Dictionary<int, TTextureType>();
        // Maps texturePaths to textures. Texture may have already been disposed (RefCount<=0) or even null
        private readonly Dictionary<string, STextureCacheEntry> _TextureCache = new Dictionary<string, STextureCacheEntry>();
        private readonly Queue<STextureQueue> _TexturesToLoad = new Queue<STextureQueue>();
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

        public abstract void ClearScreen();
        protected abstract void _AdjustNewBorders();
        protected abstract void _LeaveFullScreen();

        protected abstract void _OnAfterDraw();

        protected abstract void _OnBeforeDraw();

        protected abstract void _DoResize();

        protected abstract void _EnterFullScreen();

        public virtual void Unload()
        {
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
        protected abstract void _WriteDataToTexture(TTextureType texture, byte[] data);

        protected virtual void _WriteDataToTexture(TTextureType texture, IntPtr data)
        {
            byte[] dataArray = new byte[4 * texture.DataSize.Width * texture.DataSize.Height];
            Marshal.Copy(data, dataArray, 0, dataArray.Length);
            _WriteDataToTexture(texture, dataArray);
        }

        /// <summary>
        ///     Factory method to create a texture of the actual type
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
            int maxSize;
            //Apply the right max size
            switch (CConfig.TextureQuality)
            {
                case ETextureQuality.TR_CONFIG_TEXTURE_LOWEST:
                    maxSize = 128;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_LOW:
                    maxSize = 256;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_MEDIUM:
                    maxSize = 512;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_HIGH:
                    maxSize = 1024;
                    break;
                case ETextureQuality.TR_CONFIG_TEXTURE_HIGHEST:
                    maxSize = 2048;
                    break;
                default:
                    maxSize = 512;
                    break;
            }

            // Make sure maxSize is a power of 2 if required
            maxSize = _CheckForNextPowerOf2(maxSize);

            Bitmap bmp2 = null;
            try
            {
                // Do not stretch the texture, only make it smaller
                int w = Math.Min(bmp.Width, maxSize);
                int h = Math.Min(bmp.Height, maxSize);
                if (bmp.Width != w || bmp.Height != h)
                {
                    //Create a new Bitmap with the new sizes
                    bmp2 = new Bitmap(w, h);
                    //Scale the texture
                    using (Graphics g = Graphics.FromImage(bmp2))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.DrawImage(bmp, new Rectangle(0, 0, bmp2.Width, bmp2.Height));
                    }
                    bmp = bmp2;
                }

                //Fill the new Bitmap with the texture data
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                Size size = new Size(w, h);
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

        private void _AddToCache(TTextureType texture, Size origSize, string texturePath)
        {
            if (texture != null && !String.IsNullOrEmpty(texturePath))
            {
                texture.TexturePath = texturePath;
                STextureCacheEntry cacheEntry = new STextureCacheEntry {OrigSize = origSize, Texture = texture};
                lock (_Textures)
                {
                    _TextureCache[texturePath] = cacheEntry;
                }
            }
        }

        private CTextureRef _GetFromCache(string texturePath)
        {
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
        ///     Method used to add a texture to the texture list and return a TextureRef to it<br />
        ///     Important: This should be the only function to ever write to _Textures
        /// </summary>
        /// <param name="origSize"></param>
        /// <param name="texture"></param>
        /// <returns></returns>
        protected CTextureRef _GetTextureReference(Size origSize, TTextureType texture)
        {
            Debug.Assert(origSize.Width > 0 && origSize.Height > 0);
            int id;
            lock (_MutexID)
            {
                id = _NextID++;
            }
            CTextureRef textureRef = new CTextureRef(id, origSize);
            if (texture != null)
                texture.RefCount++;
            if (texture == null || texture.RefCount == 1)
                _TextureCount++;
            lock (_Textures)
            {
                _Textures.Add(id, texture);
            }
            return textureRef;
        }

        /// <summary>
        ///     Method used to add a texture to the texture list and return a TextureRef to it<br />
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

        protected bool _GetTexture(CTextureRef textureRef, out TTextureType texture)
        {
            if (textureRef == null)
            {
                texture = null;
                return false;
            }
            lock (_Textures)
            {
                return _Textures.TryGetValue(textureRef.ID, out texture) && texture != null;
            }
        }

        /// <summary>
        ///     Decreases the RefCount of a texture and disposes it if necessary
        /// </summary>
        /// <param name="texture"></param>
        private void _DisposeTexture(TTextureType texture)
        {
            if (texture != null && --texture.RefCount <= 0)
            {
                if (texture.TexturePath != null)
                    _TextureCache.Remove(texture.TexturePath);
                texture.Dispose();
                _TextureCount--;
            }
        }

        private void _CheckQueue()
        {
            lock (_Textures)
            {
                while (_TexturesToLoad.Count > 0)
                {
                    STextureQueue q = _TexturesToLoad.Dequeue();
                    Debug.Assert(q.Data != null ^ q.DataBmp != null);
                    CTextureRef textureRef = q.TextureRef.Target as CTextureRef;
                    if (!q.TextureRef.IsAlive || textureRef == null || !_Textures.ContainsKey(textureRef.ID))
                        continue;

                    if (q.Action == EQueueAction.Add)
                    {
                        TTextureType texture = null;
                        if (q.Data == null)
                            _WriteBitmapToTexture(ref texture, q.DataBmp);
                        else
                            texture = _CreateAndFillTexture(q.DataSize, q.Data);
                        texture.RefCount = 1; //Just created, so only 1 reference
                        _Textures[textureRef.ID] = texture;
                    }
                    else
                    {
                        if (q.Data == null)
                            UpdateTexture(textureRef, q.DataBmp);
                        else
                            UpdateTexture(textureRef, q.DataSize.Width, q.DataSize.Height, q.Data);
                    }
                }
            }
        }
        #endregion private/protected

        /// <summary>
        ///     Adds a texture and stores it in the VRam
        /// </summary>
        /// <param name="texturePath">The texture's filepath</param>
        /// <returns>A STexture object containing the added texture</returns>
        public CTextureRef AddTexture(string texturePath)
        {
            CTextureRef textureRef = _GetFromCache(texturePath);
            if (textureRef != null)
                return textureRef;

            if (!File.Exists(texturePath))
            {
                CLog.LogError("Can't find File: " + texturePath);
                return null;
            }
            Bitmap bmp;
            try
            {
                bmp = new Bitmap(texturePath);
            }
            catch (Exception)
            {
                CLog.LogError("Error loading Texture: " + texturePath);
                return null;
            }
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
            return AddTexture(bmp, null);
        }

        /// <summary>
        ///     Adds a texture and stores it in the Vram
        /// </summary>
        /// <param name="bmp">The Bitmap of which the texure will be created from</param>
        /// <param name="texturePath"></param>
        /// <returns>A STexture object containing the added texture</returns>
        public CTextureRef AddTexture(Bitmap bmp, string texturePath)
        {
            if (bmp.Height == 0 || bmp.Width == 0)
                return null;

            Size origSize = new Size(bmp.Width, bmp.Height);

            TTextureType texture = null;
            _WriteBitmapToTexture(ref texture, bmp);

            _AddToCache(texture, origSize, texturePath);
            return _GetTextureReference(origSize, texture);
        }

        public CTextureRef AddTexture(int w, int h, byte[] data)
        {
            TTextureType texture = _CreateAndFillTexture(new Size(w, h), data);
            return _GetTextureReference(w, h, texture);
        }

        public CTextureRef EnqueueTexture(int w, int h, byte[] data)
        {
            lock (_Textures)
            {
                CTextureRef textureRef = _GetTextureReference(w, h, null);
                _TexturesToLoad.Enqueue(new STextureQueue(textureRef, EQueueAction.Add, new Size(w, h), data));
                return textureRef;
            }
        }

        public CTextureRef EnqueueTexture(Bitmap bmp)
        {
            lock (_Textures)
            {
                CTextureRef textureRef = _GetTextureReference(bmp.Width, bmp.Height, null);
                _TexturesToLoad.Enqueue(new STextureQueue(textureRef, EQueueAction.Add, bmp));
                return textureRef;
            }
        }

        public void EnqueueTextureUpdate(CTextureRef textureRef, Bitmap bmp)
        {
            lock (_Textures)
            {
                _TexturesToLoad.Enqueue(new STextureQueue(textureRef, EQueueAction.Update, bmp));
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
            TTextureType texture;
            if (!_GetTexture(textureRef, out texture))
                return;
            bool reuseTexture = _IsTextureUsable(texture, w, h);
            if (reuseTexture)
            {
                texture.DataSize = new Size(w, h);
                _WriteDataToTexture(texture, data);
            }
            else
            {
                _DisposeTexture(texture);
                texture = _CreateAndFillTexture(new Size(w, h), data);
                texture.RefCount = 1;
                lock (_Textures)
                {
                    _Textures[textureRef.ID] = texture;
                }
            }
        }

        public void UpdateTexture(CTextureRef textureRef, Bitmap bmp)
        {
            TTextureType texture;
            if (!_GetTexture(textureRef, out texture))
                return;
            bool reuseTexture = _IsTextureUsable(texture, bmp.Width, bmp.Height);
            if (reuseTexture)
                _WriteBitmapToTexture(ref texture, bmp);
            else
            {
                _DisposeTexture(texture);
                texture = null;
                _WriteBitmapToTexture(ref texture, bmp);
                texture.RefCount = 1;
                lock (_Textures)
                {
                    _Textures[textureRef.ID] = texture;
                }
            }
        }

        public CTextureRef CopyTexture(CTextureRef textureRef)
        {
            TTextureType texture;
            if (!_GetTexture(textureRef, out texture))
                return null;
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

        /// <summary>
        ///     Starts the rendering
        /// </summary>
        public virtual void MainLoop()
        {
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
                ClearScreen();
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