using System.Drawing;

namespace Vocaluxe.Lib.Draw
{
    abstract class CTextureBase
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

        private int _W2, _H2;
        /// <summary>
        ///     Internal texture width (on device), a power of 2 if necessary
        /// </summary>
        public int W2
        {
            get { return _W2; }
            set
            {
                _W2 = value;
                _CalcRatios();
            }
        }
        /// <summary>
        ///     Internal texture height (on device), a power of 2 if necessary
        /// </summary>
        public int H2
        {
            get { return _H2; }
            set
            {
                _H2 = value;
                _CalcRatios();
            }
        }

        /// <summary>
        ///     Internal use. Specifies which part of texture memory is actually used
        /// </summary>
        public float WidthRatio { get; private set; }
        /// <summary>
        ///     Internal use. Specifies which part of texture memory is actually used
        /// </summary>
        public float HeightRatio { get; private set; }

        /// <summary>
        ///     Creates a new texture reference
        /// </summary>
        /// <param name="dataSize">Size of the data used</param>
        /// <param name="texWidth">Width in video memory</param>
        /// <param name="texHeight">Height in video memory</param>
        public CTextureBase(Size dataSize, int texWidth = 0, int texHeight = 0)
        {
            _DataSize = dataSize;
            _W2 = (texWidth > 0) ? texWidth : dataSize.Width;
            _H2 = (texHeight > 0) ? texHeight : dataSize.Height;

            _CalcRatios();
        }

        public bool IsValid()
        {
            return RefCount > 0;
        }

        private void _CalcRatios()
        {
            // OpenGL has a problem with partial (NPOT) textures, so we remove 1 pixel
            if (_DataSize.Width == _W2)
                WidthRatio = 1f;
            else
            {
                int mod = 0;
                if (_DataSize.Width > 1)
                    mod = -1;
                WidthRatio = (float)(_DataSize.Width + mod) / _W2;
            }
            if (_DataSize.Height == _H2)
                HeightRatio = 1f;
            else
            {
                int mod = 0;
                if (_DataSize.Height > 1)
                    mod = -1;
                HeightRatio = (float)(_DataSize.Height + mod) / _H2;
            }
        }
    }
}
