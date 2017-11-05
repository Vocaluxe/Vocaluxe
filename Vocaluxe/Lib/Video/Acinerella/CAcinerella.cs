#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Vocaluxe.Lib.Video.Acinerella
{
    // ReSharper disable UnusedMember.Global
    public enum EACStreamType : sbyte
    {
        /*The type of the media stream is not known. This kind of stream can not be
        decoded.*/
        ACStreamTypeUnknown = -1,
        //This media stream is a video stream.
        ACStreamTypeVideo = 0,
        //This media stream is an audio stream.
        ACStreamTypeAudio = 1
    }

    //Defines the type of an Acinerella media decoder.
    public enum EACDecoderType : byte
    {
        //This decoder is used to decode a video stream.
        ACDecoderTypeVideo = 0,
        //This decoder is used to decode an audio stram.
        ACDecoderTypeAudio = 1
    }

    //Defines the format video/image data is outputted in
    public enum EACOutputFormat : byte
    {
        ACOutputRGB24 = 0,
        ACOutputBGR24 = 1,
        ACOutputRGBA32 = 2,
        ACOutputBGra32 = 3
    }

    // ReSharper disable MemberCanBePrivate.Global

    // Contains information about the whole file/stream that has been opened. Default 
    // values are "" for strings and -1 for integer values.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct SACFileInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
        public readonly string title;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
        public readonly string author;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
        public readonly string copyright;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
        public readonly string comment;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
        public readonly string album;
        public readonly int year;
        public readonly int track;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public readonly string genre;
        public readonly Int64 Duration;
        public readonly int Bitrate;
    }

    // TAc_instance represents an Acinerella instance. Each instance can open and
    // decode one file at once. There can be only 26 Acinerella instances opened at
    // once.
    [StructLayout(LayoutKind.Sequential)]
    public struct SACInstance
    {
        //If true, the instance currently opened a media file
        public readonly bool Opened;
        //Contains the count of streams the media file has. This value is available
        //after calling the ac_open function.
        public readonly Int32 StreamCount;
        //Set this value to change the image output format
        public readonly EACOutputFormat OutputFormat;
        //Contains information about the opened stream/file
        public SACFileInfo Info;
    }

    // Contains information about an Acinerella audio stream.
    [StructLayout(LayoutKind.Sequential)]
    public struct SACAudioStreamInfo
    {
        //Samples per second. Default values are 44100 or 48000.
        public readonly Int32 SamplesPerSecond;
        //Bits per sample. Can be 8 or 16 Bit.
        public readonly Int32 BitDepth;
        //Count of channels in the audio stream.
        public readonly Int32 ChannelCount;
    }

    // Contains information about an Acinerella video stream.
    [StructLayout(LayoutKind.Sequential)]
    public struct SACVideoStreamInfo
    {
        //The width of one frame.
        public readonly Int32 FrameWidth;
        //The height of one frame.
        public readonly Int32 FrameHeight;
        //The width of one pixel. 1.07 for 4:3 format, 1,42 for the 16:9 format
        public readonly float PixelAspect;
        //Frames per second that should be played.
        public readonly double FramesPerSecond;
    }

    // Contains information about an Acinerella stream.
    [StructLayout(LayoutKind.Explicit)]
    public struct SACStreamInfo
    {
        //Contains the type of the stream.
        [FieldOffset(0)]
        public readonly EACStreamType StreamType;

        //Additional info about the stream
        [FieldOffset(8)]
        public SACAudioStreamInfo AudioInfo;
        [FieldOffset(8)]
        public SACVideoStreamInfo VideoInfo;
    }

    // Contains information about an Acinerella video/audio decoder.
    [StructLayout(LayoutKind.Sequential)]
    public struct SACDecoder
    {
        //Pointer on the Acinerella instance
        private readonly IntPtr _ACInstancePtr;
        //Contains the type of the decoder.
        public readonly EACDecoderType DecType;

        //The timecode of the currently decoded picture in seconds.
        public readonly double Timecode;

        //public readonly double VideoClock;

        //Contains information about the stream the decoder is attached to.
        public SACStreamInfo StreamInfo;
        //The index of the stream the decoder is attached to.
        public readonly Int32 StreamIndex;

        //Pointer to the buffer which contains the data.
        internal IntPtr Buffer;
        //Size of the data in the buffer.
        public readonly Int32 BufferSize;
    }

    // Contains information about an Acinerella package.
    [StructLayout(LayoutKind.Sequential)]
    public struct SACPackage
    {
        //The stream the package belongs to.
        public readonly Int32 StreamIndex;
    }

    // ReSharper restore MemberCanBePrivate.Global

    // Callback function used to ask the application to read data. Should return
    // the number of bytes read or an value smaller than zero if an error occured.
    //TAc_read_callback = function(sender: Pointer; byte[] buf, int size): integer; cdecl;
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate Int32 ACReadCallback(IntPtr sender, IntPtr buf, Int32 size);

    // Callback function used to ask the application to seek. return 0 if succeed , -1 on failure.
    //TAc_seek_callback = function(sender: Pointer; pos: int64; whence: integer): int64; cdecl;
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate Int64 ACSeekCallback(IntPtr sender, Int64 pos, Int32 whence);

    // Callback function that is used to notify the application when the data stream
    // is opened or closed. For example the file pointer should be resetted to zero
    // when the "open" function is called.
    // TAc_openclose_callback = function(sender: Pointer): integer; cdecl;
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate Int32 ACOpencloseCallback(IntPtr sender);

    public static class CAcinerella
    {
#if ARCH_X86
#if WIN
        private const string _AcDll = "acinerella.dll";
#endif

#if LINUX
        private const string _AcDll = "libacinerella.so";
#endif
#endif

#if ARCH_X64
#if WIN
        private const string _AcDll = "acinerella.dll";
#endif

#if LINUX
        private const string _AcDll = "libacinerella.so";
#endif
#endif

        private static readonly Object _Lock = new Object();
        private static readonly Object _DictionaryLock = new Object();
        private static readonly Dictionary<Int64, object> _AcInstanceLocks = new Dictionary<Int64, object>(20);

        private static object _GetLockToken(IntPtr pAcDecoder)
        {
            object l;

            lock (_DictionaryLock)
            {
                _AcInstanceLocks.TryGetValue(pAcDecoder.ToInt64(), out l);
                if (l == null)
                {
                    l = new object();
                    _AcInstanceLocks.Add(pAcDecoder.ToInt64(), l);
                }
            }
           
            return l;
        }


        // Defines the type of an Acinerella media stream. Currently only video and
        // audio streams are supported, subtitle and data streams will be marked as
        // "unknown".

        // Initializes an Acinerella instance.
        //function ac_init(): PAc_instance; cdecl; external ac_dll;
        [DllImport(_AcDll, EntryPoint = "ac_init_of", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern IntPtr _ac_init(EACOutputFormat outputFormat);

        public static IntPtr AcInit()
        {
            lock (_Lock)
            {
                return _ac_init(EACOutputFormat.ACOutputRGBA32);
            }
        }

        // Frees an Acinerella instance.
        //procedure ac_free(inst: PAc_instance); cdecl; external ac_dll;
        [DllImport(_AcDll, EntryPoint = "ac_free", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern void _ac_free(IntPtr pAcInstance);

        public static void AcFree(IntPtr pAcInstance)
        {
            lock (_GetLockToken(pAcInstance))
            {
                _ac_free(pAcInstance);
            }
        }

        [DllImport(_AcDll, EntryPoint = "ac_open_file", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern Int32 _ac_open2(IntPtr pAcInstance, string filename);

        // ReSharper disable UnusedMethodReturnValue.Global
        public static Int32 AcOpen2(IntPtr pAcInstance, string filename)
            // ReSharper restore UnusedMethodReturnValue.Global
        {
            lock (_GetLockToken(pAcInstance))
            {
                return _ac_open2(pAcInstance, filename);
            }
        }

        // Closes an opened media file.
        //procedure ac_close(inst: PAc_instance);cdecl; external ac_dll;
        [DllImport(_AcDll, EntryPoint = "ac_close", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern void _ac_close(IntPtr pAcInstance);

        public static void AcClose(IntPtr pAcInstance)
        {
            lock (_GetLockToken(pAcInstance))
            {
                _ac_close(pAcInstance);
            }
        }

        // Stores information in "pInfo" about stream number "nb".
        //procedure ac_get_stream_info(inst: PAc_instance; nb: integer; pinfo: PAc_stream_info); cdecl; external ac_dll;
        [DllImport(_AcDll, EntryPoint = "ac_get_stream_info", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern void _ac_get_stream_info(IntPtr pAcInstance, Int32 nb, out SACStreamInfo info);

        public static void AcGetStreamInfo(IntPtr pAcInstance, Int32 nb, out SACStreamInfo info)
        {
            lock (_GetLockToken(pAcInstance))
            {
                _ac_get_stream_info(pAcInstance, nb, out info);
            }
        }

        // Reads a package from an opened media file.
        //function ac_read_package(inst: PAc_instance): PAc_package; cdecl; external ac_dll;
        [DllImport(_AcDll, EntryPoint = "ac_read_package", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public static extern IntPtr ac_read_package(IntPtr inst);

        // Frees a package that has been read.
        //procedure ac_free_package(package: PAc_package); cdecl; external ac_dll;
        [DllImport(_AcDll, EntryPoint = "ac_free_package", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public static extern void ac_free_package(IntPtr pAcPackage);

        [DllImport(_AcDll, EntryPoint = "ac_create_decoder", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern IntPtr _ac_create_decoder(IntPtr pAcInstance, int i);

        public static IntPtr AcCreateVideoDecoder(IntPtr pAcInstance)
        {
            lock (_GetLockToken(pAcInstance))
            {
                var firstVideoStreamId = _GetFirstStreamByType(pAcInstance, EACStreamType.ACStreamTypeVideo);

                if (firstVideoStreamId < 0)
                {
                    throw new ArgumentException("The instace does not contain a video stream.");
                }

                return _ac_create_decoder(pAcInstance, firstVideoStreamId);
            }
        }
        
        public static IntPtr AcCreateAudioDecoder(IntPtr pAcInstance)
        {
            lock (_GetLockToken(pAcInstance))
            {
                var firstAudioStreamId = _GetFirstStreamByType(pAcInstance, EACStreamType.ACStreamTypeAudio);

                if (firstAudioStreamId < 0)
                {
                    throw new ArgumentException("The instace does not contain a audio stream.");
                }
                return _ac_create_decoder(pAcInstance, firstAudioStreamId);
            }
        }

        private static int _GetFirstStreamByType(IntPtr pAcInstance, EACStreamType type)
        {
            var instance = (SACInstance)Marshal.PtrToStructure(pAcInstance, typeof(SACInstance));

            int currentStream = 0;
            SACStreamInfo currentStreamInfo;

            while (currentStream < instance.StreamCount)
            {
                AcGetStreamInfo(pAcInstance, currentStream, out currentStreamInfo);
                if (currentStreamInfo.StreamType == type)
                    return currentStream;
                currentStream++;
            }
            return -1;
        }

        // Frees an created decoder.
        //procedure ac_free_decoder(pDecoder: PAc_decoder); cdecl; external ac_dll;
        [DllImport(_AcDll, EntryPoint = "ac_free_decoder", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern void _ac_free_decoder(IntPtr pAcDecoder);

        public static void AcFreeDecoder(IntPtr pAcDecoder)
        {
            lock (_Lock)
            {
                lock (_GetLockToken(pAcDecoder))
                {
                    _ac_free_decoder(pAcDecoder);
                }
            }
           
        }

        // Decodes a package using the specified decoder. The decodec data is stored in the
        // "buffer" property of the decoder.
        //function ac_decode_package(pPackage: PAc_package; pDecoder: PAc_decoder): integer; cdecl; external ac_dll;
        [DllImport(_AcDll, EntryPoint = "ac_decode_package", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern Int32 _ac_decode_package(IntPtr pAcPackage, IntPtr pAcDecoder);

        public static bool AcDecodePackage(IntPtr pAcPackage, IntPtr pAcDecoder)
        {
            lock (_GetLockToken(pAcDecoder))
            {
                return _ac_decode_package(pAcPackage, pAcDecoder) != 0;
            }
        }

        [DllImport(_AcDll, EntryPoint = "ac_get_audio_frame", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern Int32 _ac_get_audio_frame(IntPtr pAcInstance, IntPtr pAcDecoder);

        public static bool AcGetAudioFrame(IntPtr pAcInstance, IntPtr pAcDecoder)
        {
            lock (_GetLockToken(pAcDecoder))
            {
                return _ac_get_audio_frame(pAcInstance, pAcDecoder) != 0;
            }
        }

        [DllImport(_AcDll, EntryPoint = "ac_get_frame", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern Int32 _ac_get_frame(IntPtr pAcInstance, IntPtr pAcDecoder);

        public static bool AcGetFrame(IntPtr pAcInstance, IntPtr pAcDecoder)
        {
            lock (_GetLockToken(pAcDecoder))
            {
                return _ac_get_frame(pAcInstance, pAcDecoder) != 0;
            }
        }

        [DllImport(_AcDll, EntryPoint = "ac_skip_frames", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern Int32 _ac_skip_frames(IntPtr pAcInstance, IntPtr pAcDecoder, Int32 num);

        public static bool AcSkipFrames(IntPtr pAcInstance, IntPtr pAcDecoder, Int32 num)
        {
            lock (_GetLockToken(pAcDecoder))
            {
                return _ac_skip_frames(pAcInstance, pAcDecoder, num) != 0;
            }
        }

        // Seeks to the given target position in the file. The seek funtion is not able to seek a single audio/video stream
        // but seeks the whole file forward. The deocder parameter is only used as an timecode reference.
        // The parameter "dir" specifies the seek direction: 0 for forward, -1 for backward.
        // The target_pos paremeter is in milliseconds. Returns 1 if the functions succeded.}
        //function ac_seek(pDecoder: PAc_decoder; dir: integer; target_pos: int64): integer; cdecl; external ac_dll;
        [DllImport(_AcDll, EntryPoint = "ac_seek", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern Int32 _ac_seek(IntPtr pAcDecoder, Int32 dir, Int64 targetPos);

        public static bool AcSeek(IntPtr pAcDecoder, Int32 dir, Int64 targetPos)
        {
            lock (_GetLockToken(pAcDecoder))
            {
                return _ac_seek(pAcDecoder, dir, targetPos) != 0;
            }
        }

        //function ac_probe_input_buffer(buf: PChar; bufsize: Integer; filename: PChar;
        //var score_max: Integer): PAc_proberesult; cdecl; external ac_dll;
        [DllImport(_AcDll, EntryPoint = "ac_probe_input_buffer", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public static extern IntPtr ac_probe_input_buffer(IntPtr buf, Int32 bufsize, IntPtr filename, out Int32 scoreMax);
    }

    // ReSharper restore UnusedMember.Global
}