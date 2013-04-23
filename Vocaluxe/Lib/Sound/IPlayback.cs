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

namespace Vocaluxe.Lib.Sound
{
    struct SAudioStreams
    {
        public int Handle;

        public SAudioStreams(int stream)
        {
            Handle = stream;
        }
    }

    delegate void Closeproc(int streamID);

    interface IPlayback
    {
        bool Init();
        void SetGlobalVolume(float volume);
        int GetStreamCount();
        void CloseAll();

        #region Stream Handling
        int Load(string media);
        int Load(string media, bool prescan);
        void Close(int stream);

        void Play(int stream);
        void Play(int stream, bool loop);
        void Pause(int stream);
        void Stop(int stream);
        void Fade(int stream, float targetVolume, float seconds);
        void FadeAndPause(int stream, float targetVolume, float seconds);
        void FadeAndStop(int stream, float targetVolume, float seconds);
        void SetStreamVolume(int stream, float volume);
        void SetStreamVolumeMax(int stream, float volume);

        float GetLength(int stream);
        float GetPosition(int stream);

        bool IsPlaying(int stream);
        bool IsPaused(int stream);
        bool IsFinished(int stream);

        void SetPosition(int stream, float position);

        void Update();
        #endregion Stream Handling
    }

    class CRingBuffer
    {
        private readonly byte[] _Data;
        private readonly long _Size;
        private long _ReadPos;
        private long _WritePos;
        private long _BytesNotRead;

        public long BytesNotRead
        {
            get { return _BytesNotRead; }
        }

        public CRingBuffer(long size)
        {
            _Size = size;
            _Data = new byte[size];
            _ReadPos = 0L;
            _WritePos = 0L;
            _BytesNotRead = 0L;
        }

        public void Write(byte[] data)
        {
            long written = 0L;
            while (written < data.Length)
            {
                _Data[_WritePos] = data[written];
                _WritePos++;
                if (_WritePos >= _Size)
                    _WritePos = 0L;

                written++;
                _BytesNotRead++;
            }
        }

        public void Read(ref byte[] data)
        {
            long read = 0L;
            while (read < data.Length && _BytesNotRead > 0L)
            {
                data[read] = _Data[_ReadPos];
                _ReadPos++;
                if (_ReadPos >= _Size)
                    _ReadPos = 0L;

                read++;
                _BytesNotRead--;
            }
        }
    }
}