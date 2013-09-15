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

using System;
using System.Runtime.InteropServices;

namespace Vocaluxe.Lib.Video.Acinerella
{
    class CFrame
    {
        public readonly byte[] Data;
        public float Time = -1;

        public CFrame(int dataSize)
        {
            Data = new byte[dataSize];
        }
    }

    class CFramebuffer
    {
        private readonly CFrame[] _Frames;
        private readonly int _Size;
        private int _DataSize;
        private int _Last;
        private int _First;
        private bool _Initialized;

        public int Size
        {
            get { return _Size; }
        }

        public CFramebuffer(int size)
        {
            _Size = size;
            _Frames = new CFrame[size];
        }

        public void Init(int dataSize)
        {
            _DataSize = dataSize;
            for (int i = 0; i < _Size; i++)
                _Frames[i] = new CFrame(dataSize);
            _Initialized = true;
        }

        private int _Next(int current)
        {
            current++;
            return (current < _Size) ? current : 0;
        }

        public bool IsFull()
        {
            return _Next(_Last) == _First;
        }

        public bool IsEmpty()
        {
            return _First == _Last;
        }

        public bool Put(IntPtr data, float time)
        {
            if (!_Initialized || IsFull())
                return false;
            CFrame frame = _Frames[_Last];
            Marshal.Copy(data, frame.Data, 0, _DataSize);
            frame.Time = time;
            _Last = _Next(_Last);
            return true;
        }

        public CFrame Get()
        {
            return IsEmpty() ? null : _Frames[_First];
        }

        public bool SetRead()
        {
            if (IsEmpty())
                return false;
            _First = _Next(_First);
            return true;
        }

        public void Clear()
        {
            _First = 0;
            _Last = 0;
        }
    }
}