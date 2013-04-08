using System;
using System.Runtime.InteropServices;

namespace Vocaluxe.Lib.Video.Gstreamer
{
    public struct NativeFrame
    {
        internal IntPtr buffer;
        public int Size;
        public int Width;
        public int Height;
        public float Videotime;
    }

    public struct ManagedFrame
    {
        public byte[] buffer;
        public int Size;
        public int Width;
        public int Height;
        public float Videotime;
    }

    public static class CGstreamerVideoWrapper
    {
        #region arch
#if ARCH_X86
#if WIN
        private const string Dll = "x86\\gstreamer\\gstreamerhelper.dll";
#endif
#endif

#if ARCH_X64
#if WIN
        private const string Dll = "x64\\gstreamer\\gstreamerhelper.dll";
#endif
#endif
        #endregion arch

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void LogCallback(string message);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetVideoLogCallback(LogCallback c);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool InitVideo();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CloseAllVideos();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int LoadVideo(string VideoFileName);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool CloseVideo(int StreamID);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetVideoNumStreams();

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern float GetVideoLength(int StreamID);

        [DllImport(Dll, EntryPoint = "GetFrame", CallingConvention = CallingConvention.Cdecl)]
        public static extern NativeFrame GetFrameNative(int StreamID, float Time);

        public static ManagedFrame GetFrame(int StreamID, float Time)
        {
            NativeFrame f = GetFrameNative(StreamID, Time);

            byte[] buffer = null;

            if (f.Size > 0)
            {
                buffer = new byte[f.Size];
                Marshal.Copy(f.buffer, buffer, 0, f.Size);
            }

            ManagedFrame m;
            m.buffer = buffer;
            m.Height = f.Height;
            m.Size = f.Size;
            m.Videotime = f.Videotime;
            m.Width = f.Width;
            return m;
        }

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool Skip(int StreamID, float Start, float Gap);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetVideoLoop(int StreamID,
                                               [MarshalAs(UnmanagedType.U1)] bool Loop);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PauseVideo(int StreamID);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ResumeVideo(int StreamID);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool Finished(int StreamID);

        [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool UpdateVideo();
    }
}