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

using System.Drawing;

namespace VocaluxeLib.Draw
{
    /// <summary>
    ///     Reference to a texture in the drawing driver
    /// </summary>
    public class CTexture
    {
        public readonly int ID;

        public string TexturePath = "";

        /// <summary>
        ///     Size of original image (e.g. of bmp)
        /// </summary>
        public Size OrigSize;

        public float OrigAspect
        {
            get { return (float)OrigSize.Width / OrigSize.Height; }
        }

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
        ///     Current size when drawn
        /// </summary>
        public SRectF Rect;

        public SColorF Color = new SColorF(1f, 1f, 1f, 1f);

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
        /// <param name="id">ID of the texture</param>
        /// <param name="origSize">Original size (Bitmap size)</param>
        /// <param name="dataSize">Size of the data used</param>
        /// <param name="texWidth">Width in video memory</param>
        /// <param name="texHeight">Height in video memory</param>
        public CTexture(int id, Size origSize, Size dataSize, int texWidth = 0, int texHeight = 0)
        {
            ID = id;
            _DataSize = dataSize;
            OrigSize = origSize;
            _W2 = (texWidth > 0) ? texWidth : dataSize.Width;
            _H2 = (texHeight > 0) ? texHeight : dataSize.Height;

            Rect = new SRectF(0f, 0f, origSize.Width, origSize.Height, 0f);

            _CalcRatios();
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
                WidthRatio = 1f;
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