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
    public enum EStreamAction
    {
        Nothing,
        Pause,
        Stop,
        Close
    }

    struct SAudioStreams
    {
        public int Handle;

        public SAudioStreams(int stream)
        {
            Handle = stream;
        }
    }

    delegate void Closeproc(int stream);

    interface IPlayback
    {
        bool Init();
        void Close();
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
        void FadeAndClose(int stream, float targetVolume, float seconds);
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
        private readonly int _Size;
        private int _ReadPos;
        private int _WritePos;
        private int _BytesNotRead;

        public int BytesNotRead
        {
            get { return _BytesNotRead; }
        }

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
            _BytesNotRead = 0;
        }

        public void Write(byte[] data)
        {
            int start = 0;
            int end = data.Length;
            if (end - start > _Size)
                start = end - _Size;
            int lenTotal = end - start;
            int len = Math.Min(lenTotal, _Size - _WritePos);
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
            _BytesNotRead += lenTotal;
        }

        public void Read(byte[] data)
        {
            int lenTotal = Math.Min(data.Length, _BytesNotRead);
            if (lenTotal == 0)
                return;

            int len = Math.Min(lenTotal, _Size - _ReadPos);
            Buffer.BlockCopy(_Data, _ReadPos, data, 0, len);
            _ReadPos += len;
            if (_ReadPos >= _Size)
            {
                _ReadPos = 0;
                int start = len;
                len = lenTotal - len;
                if (len > 0)
                {
                    Buffer.BlockCopy(_Data, _ReadPos, data, start, len);
                    _ReadPos += len;
                }
            }
            _BytesNotRead -= lenTotal;
        }
    }
}