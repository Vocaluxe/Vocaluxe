using System;
using VocaluxeLib.Menu;

namespace Vocaluxe.Lib.Video
{
    struct SVideoStreams
    {
        public int Handle;
        public string File;

        public SVideoStreams(int stream)
        {
            Handle = stream;
            File = String.Empty;
        }
    }

    interface IVideoDecoder
    {
        bool Init();
        void CloseAll();

        int Load(string videoFileName);
        bool Close(int streamID);
        int GetNumStreams();

        float GetLength(int streamID);
        bool GetFrame(int streamID, ref STexture frame, float time, ref float videoTime);
        bool Skip(int streamID, float start, float gap);
        void SetLoop(int streamID, bool loop);
        void Pause(int streamID);
        void Resume(int streamID);

        bool Finished(int streamID);

        void Update();
    }
}