using System;
using System.Collections.Generic;
using System.Text;

using Vocaluxe.Lib.Draw;
using Vocaluxe.Menu;

namespace Vocaluxe.Lib.Video
{
    struct VideoStreams
    {
        public int handle;
        public string file;
        
        public VideoStreams(int stream)
        {
            handle = stream;
            file = String.Empty;
        }
    }

    interface IVideoDecoder
    {
        bool Init();
        void CloseAll();

        int Load(string VideoFileName);
        bool Close(int StreamID);
        int GetNumStreams();

        float GetLength(int StreamID);
        bool GetFrame(int StreamID, ref STexture Frame, float Time, ref float VideoTime);
        bool Skip(int StreamID, float Start, float Gap);
        void SetLoop(int StreamID, bool Loop);
        void Pause(int StreamID);
        void Resume(int StreamID);

        bool Finished(int StreamID);
    }
}
