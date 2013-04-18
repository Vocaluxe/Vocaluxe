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

namespace Vocaluxe.Lib.Sound.Decoder
{
    abstract class CAudioDecoder : IAudioDecoder
    {
        protected bool _Initialized;

        public virtual void Init() {}

        public virtual void Close() {}

        public void Open(string fileName)
        {
            Open(fileName, false);
        }

        public virtual void Open(string fileName, bool loop) {}

        public virtual SFormatInfo GetFormatInfo()
        {
            return new SFormatInfo();
        }

        public virtual float GetLength()
        {
            return 0f;
        }

        public virtual void SetPosition(float time) {}

        public virtual float GetPosition()
        {
            return 0f;
        }

        public virtual void Decode(out byte[] buffer, out float timeStamp)
        {
            buffer = null;
            timeStamp = 0f;
        }
    }
}