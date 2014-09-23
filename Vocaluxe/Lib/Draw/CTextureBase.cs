using System;
using System.Drawing;

namespace Vocaluxe.Lib.Draw
{
    abstract class CTextureBase : IDisposable
    {
        public int RefCount;
        public string TexturePath;

        private Size _DataSize;
        /// <summary>
        ///     Size of data used (mostly equal to OrigSize but due to resizing this might change)
        /// </summary>
        public Size DataSize
        {
            get { return _DataSize; }
            set
            {
                _DataSize = value;
                _CalcRatios();
            }
        }

        /// <summary>
        ///     texture width (on device), a power of 2 if necessary
        /// </summary>
        public int W2 { get; protected set; }
        /// <summary>
        ///     texture height (on device), a power of 2 if necessary
        /// </summary>
        public int H2 { get; protected set; }

        /// <summary>
        ///     Specifies which part of texture memory is actually used
        /// </summary>
        public float WidthRatio { get; private set; }
        /// <summary>
        ///     Specifies which part of texture memory is actually used
        /// </summary>
        public float HeightRatio { get; private set; }

        /// <summary>
        ///     True if the texture contains usefull data
        /// </summary>
        public abstract bool IsLoaded { get; }

        /// <summary>
        ///     Creates a new texture
        /// </summary>
        /// <param name="dataSize">Size of the data used</param>
        /// <param name="texWidth">Width in video memory</param>
        /// <param name="texHeight">Height in video memory</param>
        protected CTextureBase(Size dataSize, int texWidth = 0, int texHeight = 0)
        {
            _DataSize = dataSize;
            W2 = (texWidth > 0) ? texWidth : dataSize.Width;
            H2 = (texHeight > 0) ? texHeight : dataSize.Height;

            _CalcRatios();
        }

        private void _CalcRatios()
        {
            // OpenGL has a problem with partial (NPOT) textures, so we remove 1 pixel
            if (_DataSize.Width == W2)
                WidthRatio = 1f;
            else
            {
                int mod = 0;
                if (_DataSize.Width > 1)
                    mod = -1;
                WidthRatio = (float)(_DataSize.Width + mod) / W2;
            }
            if (_DataSize.Height == H2)
                HeightRatio = 1f;
            else
            {
                int mod = 0;
                if (_DataSize.Height > 1)
                    mod = -1;
                HeightRatio = (float)(_DataSize.Height + mod) / H2;
            }
        }

        private bool _IsDisposed;

        public virtual void Dispose()
        {
            if (_IsDisposed)
                throw new ObjectDisposedException(GetType().Name);
            RefCount = 0;
            _IsDisposed = true;
        }
    }
}