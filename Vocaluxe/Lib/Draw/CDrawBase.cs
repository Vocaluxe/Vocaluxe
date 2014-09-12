using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Lib.Draw
{
    struct STextureQueue
    {
        public readonly CTexture Texture;
        public readonly byte[] Data;

        public STextureQueue(CTexture texture, byte[] data)
        {
            Texture = texture;
            Data = data;
        }
    }

    abstract class CDrawBase<TTextureType> where TTextureType : class, IDisposable
    {
        protected bool _NonPowerOf2TextureSupported;
        protected bool _Fullscreen;

        protected bool _Run;

        protected readonly CKeys _Keys = new CKeys();
        protected readonly CMouse _Mouse = new CMouse();

        private readonly Object _MutexID = new object();
        private int _NextID;

        protected readonly Dictionary<int, TTextureType> _Textures = new Dictionary<int, TTextureType>();
        private readonly Queue<STextureQueue> _TexturesToLoad = new Queue<STextureQueue>();

        protected int _BorderLeft;
        protected int _BorderRight;
        protected int _BorderTop;
        protected int _BorderBottom;

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
                    if (texture != null)
                        texture.Dispose();
                }
                _Textures.Clear();
            }
        }

        /// <summary>
        ///     Calculates the next power of two if the device has the POW2 flag set
        /// </summary>
        /// <param name="n">The value of which the next power of two will be calculated</param>
        /// <returns>The next power of two</returns>
        private int _CheckForNextPowerOf2(int n)
        {
            if (_NonPowerOf2TextureSupported)
                return n;
            if (n < 0)
                throw new ArgumentOutOfRangeException("n", "Must be positive.");
            return (int)Math.Pow(2, Math.Ceiling(Math.Log(n, 2)));
        }

        #region Textures
        /// <summary>
        ///     Creates the texture specified by the reference and fills it with the given data
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected abstract TTextureType _CreateTexture(CTexture texture, byte[] data);

        /// <summary>
        ///     Creates the texture specified by the reference and fills it with the given data<br />
        ///     Data has to be a pointer to an array of size W*H with 4 Byte values for each pixel (e.g. bmpData.Scan0)
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual TTextureType _CreateTexture(CTexture texture, IntPtr data)
        {
            byte[] dataArray = new byte[4 * texture.DataSize.Width * texture.DataSize.Height];
            Marshal.Copy(data, dataArray, 0, dataArray.Length);
            return _CreateTexture(texture, dataArray);
        }

        protected CTexture _GetNewTextureRef(int origWidth, int origHeight, int dataWidth = 0, int dataHeight = 0)
        {
            Debug.Assert(origWidth > 0 && origHeight > 0);
            Debug.Assert(dataWidth > 0 || dataHeight <= 0);
            int id;
            lock (_MutexID)
            {
                id = _NextID++;
            }
            Size origSize = new Size(origWidth, origHeight);
            Size dataSize = (dataHeight > 0) ? new Size(dataWidth, dataHeight) : origSize;
            return new CTexture(id, origSize, dataSize, _CheckForNextPowerOf2(dataSize.Width), _CheckForNextPowerOf2(dataSize.Height));
        }

        /// <summary>
        ///     Adds a texture and stores it in the VRam
        /// </summary>
        /// <param name="texturePath">The texture's filepath</param>
        /// <returns>A STexture object containing the added texture</returns>
        public CTexture AddTexture(string texturePath)
        {
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
            CTexture s;
            try
            {
                s = AddTexture(bmp);
            }
            finally
            {
                bmp.Dispose();
            }
            return s;
        }

        /// <summary>
        ///     Adds a texture and stores it in the Vram
        /// </summary>
        /// <param name="bmp">The Bitmap of which the texure will be created from</param>
        /// <returns>A STexture object containing the added texture</returns>
        public CTexture AddTexture(Bitmap bmp)
        {
            if (bmp.Height == 0 || bmp.Width == 0)
                return null;
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

            int w = Math.Min(bmp.Width, maxSize);
            int h = Math.Min(bmp.Height, maxSize);

            CTexture texture = _GetNewTextureRef(bmp.Width, bmp.Height, w, h);

            Bitmap bmp2 = null;
            try
            {
                // Do not stretch the texture, only make it smaller
                w = Math.Min(bmp.Width, texture.W2);
                h = Math.Min(bmp.Height, texture.H2);
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
                    texture.DataSize = new Size(w, h);
                }

                //Fill the new Bitmap with the texture data
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                _AddTexture(texture, bmpData.Scan0);
                bmp.UnlockBits(bmpData);
            }
            finally
            {
                if (bmp2 != null)
                    bmp2.Dispose();
            }

            return texture;
        }

        public CTexture AddTexture(int w, int h, byte[] data)
        {
            CTexture texture = _GetNewTextureRef(w, h);
            _AddTexture(texture, data);
            return texture;
        }

        private void _AddTexture(CTexture texture, byte[] data)
        {
            TTextureType t = _CreateTexture(texture, data);
            lock (_Textures)
            {
                _Textures.Add(texture.ID, t);
            }
        }

        private void _AddTexture(CTexture texture, IntPtr data)
        {
            TTextureType t = _CreateTexture(texture, data);
            lock (_Textures)
            {
                _Textures.Add(texture.ID, t);
            }
        }

        public CTexture EnqueueTexture(int w, int h, byte[] data)
        {
            CTexture texture = _GetNewTextureRef(w, h);

            lock (_Textures)
            {
                _Textures.Add(texture.ID, null);
                _TexturesToLoad.Enqueue(new STextureQueue(texture, data));
            }
            return texture;
        }

        /// <summary>
        ///     Updates the data of a texture
        /// </summary>
        /// <param name="texture">The texture to update</param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="data">A byte array containing the new texture's data</param>
        /// <returns>True if succeeded</returns>
        public abstract bool UpdateTexture(CTexture texture, int w, int h, byte[] data);

        public bool UpdateOrAddTexture(ref CTexture texture, int w, int h, byte[] data)
        {
            if (!UpdateTexture(texture, w, h, data))
            {
                RemoveTexture(ref texture);
                texture = AddTexture(w, h, data);
            }
            return true;
        }

        /// <summary>
        ///     Checks if a texture exists
        /// </summary>
        /// <param name="texture">The texture to check</param>
        /// <returns>True if the texture exists</returns>
        protected bool _TextureExists(CTexture texture)
        {
            lock (_Textures)
            {
                TTextureType t;
                return texture != null && _Textures.TryGetValue(texture.ID, out t) && t != null;
            }
        }

        protected TTextureType _GetTexture(CTexture textureRef)
        {
            if (textureRef == null)
                return null;
            TTextureType texture;
            _Textures.TryGetValue(textureRef.ID, out texture);
            return texture;
        }

        /// <summary>
        ///     Removes a texture from the Vram
        /// </summary>
        /// <param name="texture">The texture to be removed</param>
        public void RemoveTexture(ref CTexture texture)
        {
            if (texture == null)
                return;
            lock (_Textures)
            {
                TTextureType t;
                if (_Textures.TryGetValue(texture.ID, out t))
                {
                    if (t != null)
                        t.Dispose();
                    _Textures.Remove(texture.ID);
                }
            }
            texture = null;
        }

        protected void _CheckQueue()
        {
            lock (_Textures)
            {
                while (_TexturesToLoad.Count > 0)
                {
                    STextureQueue q = _TexturesToLoad.Dequeue();
                    if (!_Textures.ContainsKey(q.Texture.ID))
                        continue;

                    TTextureType t = _CreateTexture(q.Texture, q.Data);
                    _Textures[q.Texture.ID] = t;
                }
            }
        }

        public int GetTextureCount()
        {
            return _Textures.Count;
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

        protected void _ToggleFullScreen()
        {
            if (!_Fullscreen)
                _EnterFullScreen();
            else
                _LeaveFullScreen();
        }
    }
}