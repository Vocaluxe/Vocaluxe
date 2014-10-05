using VocaluxeLib.Draw;

namespace VocaluxeLib
{
    public class CVideoStream
    {
        public int ID { get; private set; }
        public CTextureRef Texture;
        /// The actual position in s of the returned frame. Should be ~ time+VideoGap
        public float VideoTime;

        public CVideoStream(int id)
        {
            ID = id;
        }

        ~CVideoStream()
        {
            if (ID >= 0)
            {
                CVideoStream tmp = this;
                CBase.Video.Close(ref tmp);
            }
        }

        /// <summary>
        ///     Should only be used by video decoders<br />
        ///     Set's this stream to closed state and frees the texture
        /// </summary>
        public void SetClosed()
        {
            ID = -1;
            CBase.Drawing.RemoveTexture(ref Texture);
        }

        /// <summary>
        ///     Returns true if the video stream is closed
        /// </summary>
        /// <returns></returns>
        public bool IsClosed()
        {
            return ID < 0;
        }
    }
}