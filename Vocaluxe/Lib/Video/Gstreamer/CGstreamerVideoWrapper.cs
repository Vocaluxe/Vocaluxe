#region license
// /*
//     This file is part of Vocaluxe.
// 
//     Vocaluxe is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Vocaluxe is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//     You should have received a copy of the GNU General Public License
//     along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
//  */
#endregion

using System;
using System.Runtime.InteropServices;

namespace Vocaluxe.Lib.Video.Gstreamer
{
    public struct SNativeFrame
    {
        // ReSharper disable UnassignedField.Global
        internal IntPtr Buffer;
        public int Size;
        public int Width;
        public int Height;
        public float Videotime;
        // ReSharper restore UnassignedField.Global
    }

    public struct SManagedFrame
    {
        public byte[] Buffer;
        public int Width;
        public int Height;
        public float Videotime;
    }

    public static class CGstreamerVideoWrapper
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

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetVideoLogCallback(LogCallback c);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool InitVideo();

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CloseAllVideos();

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int LoadVideo(string videoFileName);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool CloseVideo(int streamID);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetVideoNumStreams();

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern float GetVideoLength(int streamID);

        [DllImport(_Dll, EntryPoint = "GetFrame", CallingConvention = CallingConvention.Cdecl)]
        public static extern SNativeFrame GetFrameNative(int streamID, float time);

        public static SManagedFrame GetFrame(int streamID, float time)
        {
            SNativeFrame f = GetFrameNative(streamID, time);

            byte[] buffer = null;

            if (f.Size > 0)
            {
                buffer = new byte[f.Size];
                Marshal.Copy(f.Buffer, buffer, 0, f.Size);
            }

            SManagedFrame m;
            m.Buffer = buffer;
            m.Height = f.Height;
            m.Videotime = f.Videotime;
            m.Width = f.Width;
            return m;
        }

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool Skip(int streamID, float start, float gap);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetVideoLoop(int streamID,
                                               [MarshalAs(UnmanagedType.U1)] bool loop);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PauseVideo(int streamID);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ResumeVideo(int streamID);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool Finished(int streamID);

        [DllImport(_Dll, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool UpdateVideo();
    }
}