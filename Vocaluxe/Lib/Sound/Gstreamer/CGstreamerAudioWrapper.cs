using System.Runtime.InteropServices;

namespace Vocaluxe.Lib.Sound.Gstreamer
{
    public static class CGstreamerAudioWrapper
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

        public delegate void LogCallback(string message);

        [DllImport(Dll)]
        public static extern void SetLogCallback(LogCallback c);

        [DllImport(Dll)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool Init();

        [DllImport(Dll)]
        public static extern void SetGlobalVolume(float Volume);

        [DllImport(Dll)]
        public static extern int GetStreamCount();

        [DllImport(Dll)]
        public static extern void CloseAll();

        [DllImport(Dll)]
        public static extern int Load(string Media);

        [DllImport(Dll, EntryPoint = "LoadPrescan")]
        public static extern int Load(string Media,
                                      [MarshalAs(UnmanagedType.U1)] bool Prescan);

        [DllImport(Dll)]
        public static extern void Close(int Stream);

        [DllImport(Dll)]
        public static extern void Play(int Stream);

        [DllImport(Dll, EntryPoint = "PlayLoop")]
        public static extern void Play(int Stream,
                                       [MarshalAs(UnmanagedType.U1)] bool Loop);

        [DllImport(Dll)]
        public static extern void Pause(int Stream);

        [DllImport(Dll)]
        public static extern void Stop(int Stream);

        [DllImport(Dll)]
        public static extern void Fade(int Stream, float TargetVolume, float Seconds);

        [DllImport(Dll)]
        public static extern void FadeAndPause(int Stream, float TargetVolume, float Seconds);

        [DllImport(Dll)]
        public static extern void FadeAndStop(int Stream, float TargetVolume, float Seconds);

        [DllImport(Dll)]
        public static extern void SetStreamVolume(int Stream, float Volume);

        [DllImport(Dll)]
        public static extern void SetStreamVolumeMax(int Stream, float Volume);

        [DllImport(Dll)]
        public static extern float GetLength(int Stream);

        [DllImport(Dll)]
        public static extern float GetPosition(int Stream);

        [DllImport(Dll)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool IsPlaying(int Stream);

        [DllImport(Dll)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool IsPaused(int Stream);

        [DllImport(Dll)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool IsFinished(int Stream);

        [DllImport(Dll)]
        public static extern void SetPosition(int Stream, float Position);

        [DllImport(Dll)]
        public static extern void Update();
    }
}