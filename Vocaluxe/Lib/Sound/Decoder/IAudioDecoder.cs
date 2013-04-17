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
        void Open(string fileName, bool loop);
        SFormatInfo GetFormatInfo();

        float GetLength();

        void SetPosition(float time);
        float GetPosition();

        void Decode(out byte[] buffer, out float timeStamp);
    }
}