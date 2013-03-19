
namespace Vocaluxe.Lib.Sound.Decoder
{
    abstract class CAudioDecoder: IAudioDecoder
    {
        protected bool _Initialized;

        public virtual void Init()
        {
        }

        public virtual void Close()
        {
        }

        public void Open(string FileName)
        {
            Open(FileName, false);
        }

        public virtual void Open(string FileName, bool Loop)
        {
        }

        public virtual FormatInfo GetFormatInfo()
        {
            return new FormatInfo();
        }

        public virtual float GetLength()
        {
            return 0f;
        }

        public virtual void SetPosition(float Time)
        {
        }

        public virtual float GetPosition()
        {
            return 0f;
        }

        public virtual void Decode(out byte[] Buffer, out float TimeStamp)
        {
            Buffer = null;
            TimeStamp = 0f;
        }
    }
}
