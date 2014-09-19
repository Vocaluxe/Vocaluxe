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
    public class CTextureRef
    {
        public int ID;

        /// <summary>
        ///     Size of original image (e.g. of bmp)
        /// </summary>
        public Size OrigSize;

        public float OrigAspect
        {
            get { return (float)OrigSize.Width / OrigSize.Height; }
        }

        /// <summary>
        ///     Current size when drawn
        /// </summary>
        public SRectF Rect;

        public SColorF Color = new SColorF(1f, 1f, 1f, 1f);

        /// <summary>
        ///     Creates a new texture reference
        /// </summary>
        /// <param name="id">ID of the texture</param>
        /// <param name="origSize">Original size (Bitmap size)</param>
        public CTextureRef(int id, Size origSize)
        {
            ID = id;
            OrigSize = origSize;

            Rect = new SRectF(0f, 0f, origSize.Width, origSize.Height, 0f);
        }

        ~CTextureRef()
        {
            if (ID >= 0)
            {
                //Free textures that are no longer reference
                CTextureRef tmp = this;
                CBase.Drawing.RemoveTexture(ref tmp);
            }
        }
    }
}