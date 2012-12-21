using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Menu;

namespace Vocaluxe.Lib.Video
{
    abstract class CVideoDecoder : IVideoDecoder
    {
        protected List<VideoStreams> _Streams = new List<VideoStreams>();
        protected bool _Initialized = false;

        public virtual bool Init()
        {
            _Streams = new List<VideoStreams>();
            _Initialized = true;

            return true;
        }

        public abstract void CloseAll();

        public virtual int GetNumStreams()
        {
            return _Streams.Count;
        }
        
        public virtual int Load(string VideoFileName)
        {
            return 0;
        }

        public virtual bool Close(int StreamID)
        {
            return true;
        }

        public virtual float GetLength(int StreamID)
        {
            return 0f;
        }

        public virtual bool GetFrame(int StreamID, ref STexture Frame, float Time, ref float VideoTime)
        {
            return true;
        }

        public virtual bool Skip(int StreamID, float Start, float Gap)
        {
            return true;
        }

        public virtual void SetLoop(int StreamID, bool Loop)
        {
        }

        public virtual void Pause(int StreamID)
        {
        }

        public virtual void Resume(int StreamID)
        {
        }

        public virtual bool Finished(int StreamID)
        {
            return false;
        }

        protected bool AlreadyAdded(int StreamID)
        {
            foreach (VideoStreams st in _Streams)
            {
                if (st.handle == StreamID)
                {
                    return true;
                }
            }
            return false;
        }

        protected int GetStreamIndex(int StreamID)
        {
            for (int i = 0; i < _Streams.Count; i++)
            {
                if (_Streams[i].handle == StreamID)
                    return i;
            }
            return -1;
        }
    }
}
