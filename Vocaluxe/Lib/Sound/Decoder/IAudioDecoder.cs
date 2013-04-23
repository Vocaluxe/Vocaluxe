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
    struct SFormatInfo
    {
        public int ChannelCount;
        public int SamplesPerSecond;
        public int BitDepth;
    }

    interface IAudioDecoder
    {
        void Init();
        void Close();

        void Open(string fileName);
        SFormatInfo GetFormatInfo();

        float GetLength();

        void SetPosition(float time);
        float GetPosition();

        void Decode(out byte[] buffer, out float timeStamp);
    }
}