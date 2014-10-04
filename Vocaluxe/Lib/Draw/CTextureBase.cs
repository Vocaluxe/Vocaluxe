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
        ///     texture Size (on device), a power of 2 if necessary
        /// </summary>
        public Size Size { get; private set; }

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
        /// <param name="textureSize">Size in video memory</param>
        protected CTextureBase(Size dataSize, Size textureSize)
        {
            _DataSize = dataSize;
            Size = textureSize;

            _CalcRatios();
        }

        private void _CalcRatios()
        {
            if (_DataSize.Width == Size.Width)
                WidthRatio = 1f;
            else
                WidthRatio = (float)_DataSize.Width / Size.Width;
            if (_DataSize.Height == Size.Height)
                HeightRatio = 1f;
            else
                HeightRatio = (float)_DataSize.Height / Size.Height;
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