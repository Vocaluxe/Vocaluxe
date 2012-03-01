using System;
using System.Runtime.InteropServices;
using System.IO;

using Vocaluxe.Base;

namespace Vocaluxe.Lib.Video.Acinerella
{
    public enum TAc_stream_type : sbyte
    {
        /*The type of the media stream is not known. This kind of stream can not be
        decoded.*/
        AC_STREAM_TYPE_UNKNOWN = -1,
        //This media stream is a video stream.
        AC_STREAM_TYPE_VIDEO = 0,
        //This media stream is an audio stream.
        AC_STREAM_TYPE_AUDIO = 1
    }

    //Defines the type of an Acinerella media decoder.
    public enum TAc_decoder_type : byte
    {
        //This decoder is used to decode a video stream.
        AC_DECODER_TYPE_VIDEO = 0,
        //This decoder is used to decode an audio stram.
        AC_DECODER_TYPE_AUDIO = 1
    }

    //Defines the format video/image data is outputted in
    public enum TAc_output_format : byte
    {
        AC_OUTPUT_RGB24 = 0,
        AC_OUTPUT_BGR24 = 1,
        AC_OUTPUT_RGBA32 = 2,
        AC_OUTPUT_BGRA32 = 3
    }


    // Contains information about the whole file/stream that has been opened. Default 
    // values are "" for strings and -1 for integer values.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct TAc_file_info
    {
        public Int64 duration;
    }

    // TAc_instance represents an Acinerella instance. Each instance can open and
    // decode one file at once. There can be only 26 Acinerella instances opened at
    // once.
    [StructLayout(LayoutKind.Sequential)]
    public struct TAc_instance
    {
        //If true, the instance currently opened a media file
        public bool opened;
        //Contains the count of streams the media file has. This value is available
        //after calling the ac_open function.
        public Int32 stream_count;
        //Set this value to change the image output format
        public TAc_output_format output_format;
        //Contains information about the opened stream/file
        public TAc_file_info info;
    }

    // Contains information about an Acinerella audio stream.
    [StructLayout(LayoutKind.Sequential)]
    public struct TAc_audio_stream_info
    {
        //Samples per second. Default values are 44100 or 48000.
        public Int32 samples_per_second;
        //Bits per sample. Can be 8 or 16 Bit.
        public Int32 bit_depth;
        //Count of channels in the audio stream.
        public Int32 channel_count;
    }

    // Contains information about an Acinerella video stream.
    [StructLayout(LayoutKind.Sequential)]
    public struct TAc_video_stream_info
    {
        //The width of one frame.
        public Int32 frame_width;
        //The height of one frame.
        public Int32 frame_height;
        //The width of one pixel. 1.07 for 4:3 format, 1,42 for the 16:9 format
        public float pixel_aspect;
        //Frames per second that should be played.
        public double frames_per_second;
    }

    // Contains information about an Acinerella stream.
    [StructLayout(LayoutKind.Sequential)]
    public struct TAc_stream_info
    {
        //Contains the type of the stream.
        public TAc_stream_type stream_type;
        //Additional info about the stream
        public TAc_audio_stream_info audio_info;
        public TAc_video_stream_info video_info;    
    }


    // Contains information about an Acinerella video/audio decoder.
    [StructLayout(LayoutKind.Sequential)]
    public struct TAc_decoder
    {
        //Pointer on the Acinerella instance
        public IntPtr pAcInstance;
        //Contains the type of the decoder.
        public TAc_decoder_type dec_type;

        //The timecode of the currently decoded picture in seconds.
        public double timecode;

        public double video_clock;

        //Contains information about the stream the decoder is attached to.
        public TAc_stream_info stream_info;
        //The index of the stream the decoder is attached to.
        public Int32 stream_index;

        //Pointer to the buffer which contains the data.
        public IntPtr buffer;
        //Size of the data in the buffer.
        public Int32 buffer_size;
    }

    // Contains information about an Acinerella package.
    [StructLayout(LayoutKind.Sequential)]
    public struct TAc_package
    {
        //The stream the package belongs to.
        public Int32 stream_index;
    }


    // Callback function used to ask the application to read data. Should return
    // the number of bytes read or an value smaller than zero if an error occured.
    //TAc_read_callback = function(sender: Pointer; byte[] buf, int size): integer; cdecl;
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate Int32 TAc_read_callback(
        IntPtr sender,
        IntPtr buf,
        Int32 size
        );

    // Callback function used to ask the application to seek. return 0 if succeed , -1 on failure.
    //TAc_seek_callback = function(sender: Pointer; pos: int64; whence: integer): int64; cdecl;
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate Int64 TAc_seek_callback(IntPtr sender, Int64 pos, Int32 whence);

    // Callback function that is used to notify the application when the data stream
    // is opened or closed. For example the file pointer should be resetted to zero
    // when the "open" function is called.
    // TAc_openclose_callback = function(sender: Pointer): integer; cdecl;
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate Int32 TAc_openclose_callback(IntPtr sender);


    public static class CAcinerella
    {
#if ARCH_X86
        private const string AcDll = "x86\\acinerella.dll";
#endif

#if ARCH_X64
        private const string AcDll = "x64\\acinerella.dll";
#endif

        private static Object _lock = new Object();

        // Defines the type of an Acinerella media stream. Currently only video and
        // audio streams are supported, subtitle and data streams will be marked as
        // "unknown".

        // Initializes an Acinerella instance.
        //function ac_init(): PAc_instance; cdecl; external ac_dll;
        [DllImport(AcDll, EntryPoint = "ac_init", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern IntPtr _ac_init();

        public static IntPtr ac_init()
        {
            lock (_lock)
            {
                return _ac_init();
            }
        }

        // Frees an Acinerella instance.
        //procedure ac_free(inst: PAc_instance); cdecl; external ac_dll;
        [DllImport(AcDll, EntryPoint = "ac_free", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern void _ac_free(IntPtr PAc_instance);

        public static void ac_free(IntPtr PAc_instance)
        {
            lock (_lock)
            {
                _ac_free(PAc_instance);
            }
        }

        // Opens a media file.
        // @param(inst specifies the Acinerella Instance the stream should be opened for)
        // @param(sender specifies a pointer that is sent to all callback functions to
        // allow you to do object orientated programming. May be NULL.)
        // @param(open_proc specifies the callback function that is called, when the
        // media file is opened. May be NULL.)
        // @param(close_proc specifies the callback function that is called when the media
        // file is closed. May be NULL.)
        /*function ac_open(
            inst: PAc_instance;
            sender: Pointer;
            open_proc: TAc_openclose_callback;
            read_proc: TAc_read_callback;
            seek_proc: TAc_seek_callback;
            close_proc: TAc_openclose_callback;
            proberesult: PAc_proberesult): integer; cdecl; external ac_dll;
        */
        [DllImport(AcDll, EntryPoint = "ac_open", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern Int32 _ac_open(
            IntPtr PAc_instance,
            IntPtr sender,
            TAc_openclose_callback open_proc,
            TAc_read_callback read_proc,
            TAc_seek_callback seek_proc,
            TAc_openclose_callback close_proc,
            IntPtr proberesult
            );

        public static Int32 ac_open(
            IntPtr PAc_instance,
            IntPtr sender,
            TAc_openclose_callback open_proc,
            TAc_read_callback read_proc,
            TAc_seek_callback seek_proc,
            TAc_openclose_callback close_proc,
            IntPtr proberesult
            )
        {
            lock (_lock)
            {
                return _ac_open(PAc_instance, sender, open_proc, read_proc, seek_proc, close_proc, proberesult);
            }
        }
        
        // Closes an opened media file.
        //procedure ac_close(inst: PAc_instance);cdecl; external ac_dll;
        [DllImport(AcDll, EntryPoint = "ac_close", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern void _ac_close(IntPtr PAc_instance);

        public static void ac_close(IntPtr PAc_instance)
        {
            lock (_lock)
            {
                _ac_close(PAc_instance);
            }
        }
        
        // Stores information in "pInfo" about stream number "nb".
        //procedure ac_get_stream_info(
        //inst: PAc_instance; nb: integer; pinfo: PAc_stream_info); cdecl; external ac_dll;
        [DllImport(AcDll, EntryPoint = "ac_get_stream_info", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern void _ac_get_stream_info(
            IntPtr PAc_instance,
            Int32 nb,
            out TAc_stream_info Info
            );

        public static void ac_get_stream_info(
            IntPtr PAc_instance,
            Int32 nb,
            out TAc_stream_info Info
            )
        {
            lock (_lock)
            {
                _ac_get_stream_info(
                    PAc_instance,
                    nb,
                    out Info
                    );
            }
        }

        // Reads a package from an opened media file.
        //function ac_read_package(inst: PAc_instance): PAc_package; cdecl; external ac_dll;
        [DllImport(AcDll, EntryPoint = "ac_read_package", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public static extern IntPtr ac_read_package(IntPtr inst);

        // Frees a package that has been read.
        //procedure ac_free_package(package: PAc_package); cdecl; external ac_dll;
        [DllImport(AcDll, EntryPoint = "ac_free_package", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public static extern void ac_free_package(IntPtr PAc_package);

        // Creates an decoder for the specified stream number. Returns NIL if no decoder
        // could be found.
        //function ac_create_decoder(pacInstance: PAc_instance; nb: integer): PAc_decoder; cdecl; external ac_dll;
        [DllImport(AcDll, EntryPoint = "ac_create_decoder", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern IntPtr _ac_create_decoder(IntPtr PAc_instance, Int32 nb);

        public static IntPtr ac_create_decoder(IntPtr PAc_instance, Int32 nb)
        {
            lock (_lock)
            {
                return _ac_create_decoder(PAc_instance, nb);
            }
        }
        
        // Frees an created decoder.
        //procedure ac_free_decoder(pDecoder: PAc_decoder); cdecl; external ac_dll;
        [DllImport(AcDll, EntryPoint = "ac_free_decoder", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern void _ac_free_decoder(IntPtr PAc_decoder);

        public static void ac_free_decoder(IntPtr PAc_decoder)
        {
            lock (_lock)
            {
                _ac_free_decoder(PAc_decoder);
            }
        }

        // Decodes a package using the specified decoder. The decodec data is stored in the
        // "buffer" property of the decoder.
        //function ac_decode_package(pPackage: PAc_package; pDecoder: PAc_decoder): integer; cdecl; external ac_dll;
        [DllImport(AcDll, EntryPoint = "ac_decode_package", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern Int32 _ac_decode_package(IntPtr PAc_package, IntPtr PAc_decoder);

        public static Int32 ac_decode_package(IntPtr PAc_package, IntPtr PAc_decoder)
        {
            lock (_lock)
            {
                return _ac_decode_package(PAc_package, PAc_decoder);
            }
        }

        [DllImport(AcDll, EntryPoint = "ac_get_audio_frame", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern Int32 _ac_get_audio_frame(IntPtr PAcInstance, IntPtr PAc_decoder);

        public static Int32 ac_get_audio_frame(IntPtr PAcInstance, IntPtr PAc_decoder)
        {
            lock (_lock)
            {
                return _ac_get_audio_frame(PAcInstance, PAc_decoder);
            }
        }

        [DllImport(AcDll, EntryPoint = "ac_get_frame", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern Int32 _ac_get_frame(IntPtr PAcInstance, IntPtr PAc_decoder);


        public static Int32 ac_get_frame(IntPtr PAcInstance, IntPtr PAc_decoder)
        {
            lock (_lock)
            {
                return _ac_get_frame(PAcInstance, PAc_decoder);
            }
        }
        [DllImport(AcDll, EntryPoint = "ac_skip_frames", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern Int32 _ac_skip_frames(IntPtr PAcInstance, IntPtr PAc_decoder, Int32 num);

        public static Int32 ac_skip_frames(IntPtr PAcInstance, IntPtr PAc_decoder, Int32 num)
        {
            lock (_lock)
            {
                return _ac_skip_frames(PAcInstance, PAc_decoder, num);
            }
        }

        // Seeks to the given target position in the file. The seek funtion is not able to seek a single audio/video stream
        // but seeks the whole file forward. The deocder parameter is only used as an timecode reference.
        // The parameter "dir" specifies the seek direction: 0 for forward, -1 for backward.
        // The target_pos paremeter is in milliseconds. Returns 1 if the functions succeded.}
        //function ac_seek(pDecoder: PAc_decoder; dir: integer; target_pos: int64): integer; cdecl; external ac_dll;
        [DllImport(AcDll, EntryPoint = "ac_seek", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern Int32 _ac_seek(IntPtr PAc_decoder, Int32 dir, Int64 target_pos);

        public static Int32 ac_seek(IntPtr PAc_decoder, Int32 dir, Int64 target_pos)
        {
            lock (_lock)
            {
                return _ac_seek(PAc_decoder, dir, target_pos);
            }
        }

        //function ac_probe_input_buffer(buf: PChar; bufsize: Integer; filename: PChar;
        //var score_max: Integer): PAc_proberesult; cdecl; external ac_dll;
        [DllImport(AcDll, EntryPoint = "ac_probe_input_buffer", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public static extern IntPtr ac_probe_input_buffer(
            IntPtr buf,
            Int32 bufsize,
            IntPtr filename,
            out Int32 score_max
            );
    }
}
