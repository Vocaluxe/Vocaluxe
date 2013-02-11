using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Vocaluxe.Lib.Video.Gstreamer
{
    public static class CGstreamerVideoWrapper
    {
#region arch
#if ARCH_X86
#if WIN
        private const string Dll = "x86\\gstreamerhelper.dll";
#endif
#endif

#if ARCH_X64
#if WIN
        private const string Dll = "x64\\gstreamer\\gstreamerhelper.dll";
#endif
#endif
#endregion arch
        public delegate void LogCallback(string message);

        [DllImport(Dll)]
        public static extern void SetVideoLogCallback(LogCallback c);

        [DllImport(Dll)]
        public static extern bool InitVideo();

        [DllImport(Dll)]
        public static extern void CloseAllVideos();

        [DllImport(Dll)]
        public static extern int LoadVideo(string VideoFileName);

        [DllImport(Dll)]
        public static extern bool CloseVideo(int StreamID);

        [DllImport(Dll)]
        public static extern int GetVideoNumStreams();

        [DllImport(Dll)]
        public static extern float GetVideoLength(int StreamID);

        [DllImport(Dll, EntryPoint="GetFrame")]
        public static extern IntPtr GetFrameNative(int StreamID, float Time, ref float VideoTime, ref int Size, ref int Width, ref int Height);

        public static byte[] GetFrame(int StreamID, float Time, ref float VideoTime, ref int Size, ref int Width, ref int Height)
        {
            IntPtr buf = GetFrameNative(StreamID, Time, ref VideoTime, ref Size, ref Width, ref Height);
            byte[] buffer = new byte[Size];
            Marshal.Copy(buf, buffer, 0, Size);
            return buffer;
        }

        [DllImport(Dll)]
        public static extern bool Skip(int StreamID, float Start, float Gap);

        [DllImport(Dll)]
        public static extern void SetVideoLoop(int StreamID, bool Loop);

        [DllImport(Dll)]
        public static extern void PauseVideo(int StreamID);

        [DllImport(Dll)]
        public static extern void ResumeVideo(int StreamID);

        [DllImport(Dll)]
        public static extern bool Finished(int StreamID);

        [DllImport(Dll)]
        public static extern bool UpdateVideo();

    }
}
