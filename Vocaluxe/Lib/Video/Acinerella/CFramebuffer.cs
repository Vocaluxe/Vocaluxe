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
using System.Runtime.InteropServices;

namespace Vocaluxe.Lib.Video.Acinerella
{
    class CFramebuffer
    {
        public class CFrame
        {
            private readonly CFramebuffer _Parent;
            private readonly int _Index;
            public readonly byte[] Data;
            public float Time = -1;

            public CFrame(CFramebuffer parent, int index, int dataSize)
            {
                _Parent = parent;
                _Index = index;
                Data = new byte[dataSize];
            }

            //Only call from reader thread
            public void SetRead()
            {
                _Parent._SetRead(_Index);
            }
        }

        private readonly CFrame[] _Frames;
        private readonly int _Size;
        private int _DataSize;
        private int _Last;
        private int _First;
        private int _Next;
        private bool _Initialized;

        public int Size
        {
            get { return _Size; }
        }

        // Constructs a framebuffer with max. size frames
        public CFramebuffer(int size)
        {
            _Size = size;
            _Frames = new CFrame[size];
        }

        // Initializes the framebuffer with the size of each frame
        // MUST be called before all others
        public void Init(int dataSize)
        {
            _DataSize = dataSize;
            for (int i = 0; i < _Size; i++)
                _Frames[i] = new CFrame(this, i, dataSize);
            _Initialized = true;
        }

        private int _GetNextIndex(int current)
        {
            current++;
            return (current < _Size) ? current : 0;
        }

        public bool IsFull()
        {
            return _GetNextIndex(_Last) == _First;
        }

        public bool IsEmpty()
        {
            return _First == _Last;
        }

        //Only call from writer thread
        public bool Put(IntPtr data, float time)
        {
            if (!_Initialized || IsFull())
                return false;
            CFrame frame = _Frames[_Last];
            Marshal.Copy(data, frame.Data, 0, _DataSize);
            frame.Time = time;
            return true;
        }

        //Only call from writer thread after succesfull put
        public void SetWritten()
        {
            _Last = _GetNextIndex(_Last);
        }

        //Only call from reader thread
        public void ResetStack()
        {
            _Next = _First;
        }

        //Pops the next frame if available
        //You have to call ResetNext before the first call to this function and call the CFrame.SetRead to free the buffers
        //Only call from reader thread
        public CFrame Pop()
        {
            if (_Next == _Last)
                return null;
            CFrame res = _Frames[_Next];
            _Next = _GetNextIndex(_Next);
            return res;
        }

        private void _SetRead(int index)
        {
            if ((_First < index && (index < _Next || _Next < _First)) || (_Next < _First && index < _Next))
                _First = _GetNextIndex(index);
        }

        //Only call from reader thread
        public void Clear()
        {
            _Next = _First = _Last;
        }
    }
}