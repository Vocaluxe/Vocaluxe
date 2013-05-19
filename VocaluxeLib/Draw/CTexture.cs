#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using System.Drawing;

namespace VocaluxeLib.Draw
{
    public class CTexture
    {
        public int ID = -1;
        public int PBO;
        //Only used by OpenGL (the texture "name" according to the specs)
        public int Name = -1;

        public string TexturePath = "";

        /// <summary>
        ///     Original size (e.g. of bmp)
        /// </summary>
        public readonly Size OrigSize;
        public float OrigAspect
        {
            get { return (float)OrigSize.Width / OrigSize.Height; }
        }

        /// <summary>
        ///     Current size when drawn
        /// </summary>
        public SRectF Rect;

        public SColorF Color = new SColorF(1f, 1f, 1f, 1f);

        private int _W2, _H2;
        private readonly bool _UseFullTexture;
        /// <summary>
        ///     Internal texture width (on device), a power of 2 if necessary
        /// </summary>
        public int W2
        {
            get { return _W2; }
            set
            {
                _W2 = value;
                if (_UseFullTexture)
                    WidthRatio = 1;
                else
                    WidthRatio = (float)OrigSize.Width / _W2;
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
                if (_UseFullTexture)
                    HeightRatio = 1;
                else
                    HeightRatio = (float)OrigSize.Height / _H2;
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
        ///     Actual texture space used in x direction
        /// </summary>
        public int UsedWidth
        {
            get { return (int)(_W2 * WidthRatio); }
        }

        /// <summary>
        ///     Actual texture space used in y direction
        /// </summary>
        public int UsedHeight
        {
            get { return (int)(_H2 * HeightRatio); }
        }

        /*
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
*/

        /// <summary>
        ///     Creates a new texture reference
        /// </summary>
        /// <param name="origWidth">Original width (Bitmap size)</param>
        /// <param name="origHeight">Original height (Bitmap size)</param>
        /// <param name="texWidth">Width in video memory</param>
        /// <param name="texHeight">Height in video memory</param>
        /// <param name="useFullTexture">True if it is using the full size of the video memory (no blank parts due to Pow-2-Issue)</param>
        public CTexture(int origWidth, int origHeight, int texWidth = 0, int texHeight = 0, bool useFullTexture = false)
        {
            OrigSize = new Size(origWidth, origHeight);
            Rect = new SRectF(0f, 0f, origWidth, origHeight, 0f);
            _UseFullTexture = useFullTexture;

            //IMPORTANT: Use setter here to calculate ratios!
            W2 = (texWidth > 0) ? texWidth : origWidth;
            H2 = (texHeight > 0) ? texHeight : origHeight;
        }
    }
}