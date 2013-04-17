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