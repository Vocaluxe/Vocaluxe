using System;

namespace Vocaluxe.Lib.Sound
{
    struct SAudioStreams
    {
        public int Handle;
        public string File;

        public SAudioStreams(int stream)
        {
            Handle = stream;
            File = String.Empty;
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