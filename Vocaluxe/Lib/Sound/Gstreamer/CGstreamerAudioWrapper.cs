using System.Runtime.InteropServices;

namespace Vocaluxe.Lib.Sound.Gstreamer
{
    public static class CGstreamerAudioWrapper
    {
        #region arch
#if ARCH_X86
#if WIN
        private const string _Dll = "x86\\gstreamer\\gstreamerhelper.dll";
#endif
#endif

#if ARCH_X64
#if WIN
        private const string _Dll = "x64\\gstreamer\\gstreamerhelper.dll";
#endif
#endif
        #endregion arch

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void LogCallback(string message);

        [DllImport(_Dll, CallingConvention=CallingConvention.Cdecl)]
        public static extern void SetLogCallback(LogCallback c);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool Init();

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetGlobalVolume(float volume);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetStreamCount();

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CloseAll();

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Load(string media);

        [DllImport(_Dll, EntryPoint = "LoadPrescan", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Load(string media,
                                      [MarshalAs(UnmanagedType.U1)] bool prescan);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Close(int stream);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Play(int stream);

        [DllImport(_Dll, EntryPoint = "PlayLoop")]
        public static extern void Play(int stream,
                                       [MarshalAs(UnmanagedType.U1)] bool loop);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Pause(int stream);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Stop(int stream);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Fade(int stream, float targetVolume, float seconds);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FadeAndPause(int stream, float targetVolume, float seconds);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void FadeAndStop(int stream, float targetVolume, float seconds);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetStreamVolume(int stream, float volume);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetStreamVolumeMax(int stream, float volume);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern float GetLength(int stream);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern float GetPosition(int stream);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool IsPlaying(int stream);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool IsPaused(int stream);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool IsFinished(int stream);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetPosition(int stream, float position);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Update();
    }
}