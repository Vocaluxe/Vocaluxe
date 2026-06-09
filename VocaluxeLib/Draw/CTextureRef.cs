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

using System;
using System.Drawing;

namespace VocaluxeLib.Draw
{
    /// <summary>
    ///     Reference to a texture in the drawing driver
    /// </summary>
    public class CTextureRef : IDisposable
    {
        public int Id { get; private set; }
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
        /// <param name="id">Id of the texture</param>
        /// <param name="origSize">Original size (Bitmap size)</param>
        public CTextureRef(int id, Size origSize)
        {
            Id = id;
            OrigSize = origSize;

            Rect = new SRectF(0f, 0f, origSize.Width, origSize.Height, 0f);
        }

        ~CTextureRef()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (Id >= 0)
            {
                //Free textures that are no longer referenced
                var tmp = this;
                CBase.Drawing.RemoveTexture(ref tmp);
                _SetRemoved();
            }
        }

        /// <summary>
        ///     Call this from the graphics driver to denote that this reference is remove and no longer valid <br />
        ///     This avoids the finalizer
        /// </summary>
        public void SetRemoved()
        {
            if (Id < 0)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            _SetRemoved();
        }

        private void _SetRemoved()
        {
            Id = -1;
            GC.SuppressFinalize(this);
        }
    }
}