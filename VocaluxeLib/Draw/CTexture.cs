using System.Drawing;
using VocaluxeLib.Menu;

namespace VocaluxeLib.Draw
{
    public class CTexture
    {
        public int Index = -1;
        public int PBO;
        public int ID = -1;

        public string TexturePath = "";

        /// <summary>
        /// Original size (e.g. of bmp)
        /// </summary>
        public readonly Size OrigSize;
        public float OrigAspect
        {
            get { return (float)OrigSize.Width / OrigSize.Height; }
        }

        /// <summary>
        /// Current size when drawn
        /// </summary>
        public SRectF Rect;

        public SColorF Color = new SColorF(1f, 1f, 1f, 1f);

        private int _W2, _H2;
        private bool _UseFullTexture;
        /// <summary>
        /// Internal texture width (on device), a power of 2 if necessary
        /// </summary>
        public int W2
        {
            get { return _W2; }
            set
            {
                _W2 = value;
                if (!_UseFullTexture)
                    WidthRatio = (float)OrigSize.Width / _W2;
            }
        }
        /// <summary>
        /// Internal texture height (on device), a power of 2 if necessary
        /// </summary>
        public int H2
        {
            get { return _H2; }
            set
            {
                _H2 = value;
                if (!_UseFullTexture)
                    HeightRatio = (float)OrigSize.Height / _H2;
            }
        }

        /// <summary>
        /// Internal use. Specifies which part of texture memory is actually used
        /// </summary>
        public float WidthRatio { get; private set; }
        /// <summary>
        /// Internal use. Specifies which part of texture memory is actually used
        /// </summary>
        public float HeightRatio { get; private set; }

        /// <summary>
        /// Internal use. Specifies if full texture memory should be used
        /// Set to true if you resized the original image to the maximum
        /// </summary>
        public bool UseFullTexture
        {
            get { return _UseFullTexture; }
            set
            {
                _UseFullTexture = value;
                if (value)
                {
                    WidthRatio = 1;
                    HeightRatio = 1;
                }
                else
                {
                    WidthRatio = (float)OrigSize.Width / _W2;
                    HeightRatio = (float)OrigSize.Height / _H2;
                }
            }
        }

        public CTexture(int index, int origWidth = 1, int origHeight = 1)
        {
            Index = index;

            OrigSize = new Size(origWidth, origHeight);
            Rect = new SRectF(0f, 0f, origWidth, origHeight, 0f);

            _W2 = origWidth;
            _H2 = origHeight;
        }
    }
}