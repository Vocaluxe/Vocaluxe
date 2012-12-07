using System;
using System.Collections.Generic;
using System.Text;

namespace Vocaluxe.Lib.Sound
{
    struct AudioStreams
    {
        public int handle;
        public string file;

        public AudioStreams(int stream)
        {
            handle = stream;
            file = String.Empty;
        }
    }

    delegate void CLOSEPROC(int StreamID);

    interface IPlayback
    {
        bool Init();
        void SetGlobalVolume(float Volume);
        int GetStreamCount();
        void CloseAll();

        #region Stream Handling
        int Load(string Media);
        int Load(string Media, bool Prescan);
        void Close(int Stream);

        void Play(int Stream);
        void Play(int Stream, bool Loop);
        void Pause(int Stream);
        void Stop(int Stream);
        void Fade(int Stream, float TargetVolume, float Seconds);
        void FadeAndPause(int Stream, float TargetVolume, float Seconds);
        void FadeAndStop(int Stream, float TargetVolume, float Seconds);
        void SetStreamVolume(int Stream, float Volume);
        void SetStreamVolumeMax(int Stream, float Volume);

        float GetLength(int Stream);
        float GetPosition(int Stream);

        bool IsPlaying(int Stream);
        bool IsPaused(int Stream);
        bool IsFinished(int Stream);

        void SetPosition(int Stream, float Position);

        void Update();
        #endregion Stream Handling
    }

    class RingBuffer
    {
        private byte[] _data;
        private long _size;
        private long _readPos;
        private long _writePos;
        private long _bytesNotRead;

        public long BytesNotRead
        {
            get
            {
                return _bytesNotRead;
            }
        }

        public RingBuffer(long size)
        {
            _size = size;
            _data = new byte[size];
            _readPos = 0L;
            _writePos = 0L;
            _bytesNotRead = 0L;
        }

        public void Write(byte[] Data)
        {
            long written = 0L;
            while (written < Data.Length)
            {
                _data[_writePos] = Data[written];
                _writePos++;
                if (_writePos >= _size)
                    _writePos = 0L;

                written++;
                _bytesNotRead++;
            }
        }

        public void Read(ref byte[] Data)
        {
            long read = 0L;
            while (read < Data.Length && _bytesNotRead > 0L)
            {
                Data[read] = _data[_readPos];
                _readPos++;
                if (_readPos >= _size)
                    _readPos = 0L;

                read++;
                _bytesNotRead--;
            }
        }
    }
}
