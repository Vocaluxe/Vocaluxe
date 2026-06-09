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

namespace Vocaluxe.Lib.Sound.Playback
{
    class CRingBuffer
    {
        private readonly byte[] _Data;
        private readonly int _Size;
        private int _ReadPos;
        private int _WritePos;

        public int BytesNotRead { get; private set; }

        public CRingBuffer(int size)
        {
            _Size = size;
            _Data = new byte[size];
            Reset();
        }

        public void Reset()
        {
            _ReadPos = 0;
            _WritePos = 0;
            BytesNotRead = 0;
        }

        public void Write(byte[] data)
        {
            var start = 0;
            var end = data.Length;
            if (end - start > _Size)
            {
                start = end - _Size;
            }

            var lenTotal = end - start;
            var len = Math.Min(lenTotal, _Size - _WritePos);
            Buffer.BlockCopy(data, start, _Data, _WritePos, len);
            _WritePos += len;
            if (_WritePos >= _Size)
            {
                _WritePos = 0;
                start += len;
                len = end - start;
                if (len > 0)
                {
                    Buffer.BlockCopy(data, start, _Data, _WritePos, len);
                    _WritePos += len;
                }
            }

            BytesNotRead += lenTotal;
        }

        public void Read(byte[] data)
        {
            var lenTotal = Math.Min(data.Length, BytesNotRead);
            if (lenTotal == 0)
            {
                return;
            }

            var len = Math.Min(lenTotal, _Size - _ReadPos);
            Buffer.BlockCopy(_Data, _ReadPos, data, 0, len);
            _ReadPos += len;
            if (_ReadPos >= _Size)
            {
                _ReadPos = 0;
                var start = len;
                len = lenTotal - len;
                if (len > 0)
                {
                    Buffer.BlockCopy(_Data, _ReadPos, data, start, len);
                    _ReadPos += len;
                }
            }

            BytesNotRead -= lenTotal;
        }
    }
}