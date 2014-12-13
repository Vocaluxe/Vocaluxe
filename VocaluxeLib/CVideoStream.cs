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

using VocaluxeLib.Draw;

namespace VocaluxeLib
{
    public class CVideoStream
    {
        public int ID { get; private set; }
        public CTextureRef Texture;
        /// The actual position in s of the returned frame. Should be ~ time+VideoGap
        public float VideoTime;

        public CVideoStream(int id)
        {
            ID = id;
        }

        ~CVideoStream()
        {
            if (ID >= 0)
            {
                CVideoStream tmp = this;
                CBase.Video.Close(ref tmp);
            }
        }

        /// <summary>
        ///     Should only be used by video decoders<br />
        ///     Set's this stream to closed state and frees the texture
        /// </summary>
        public void SetClosed()
        {
            ID = -1;
            CBase.Drawing.RemoveTexture(ref Texture);
        }

        /// <summary>
        ///     Returns true if the video stream is closed
        /// </summary>
        /// <returns></returns>
        public bool IsClosed()
        {
            return ID < 0;
        }
    }
}