/*
  * PortAudioSharp - PortAudio bindings for .NET
  * Copyright 2006-2011 Riccardo Gerosa and individual contributors as indicated
  * by the @authors tag. See the copyright.txt in the distribution for a
  * full listing of individual contributors.
  *
  * Permission is hereby granted, free of charge, to any person obtaining a copy of this software 
  * and associated documentation files (the "Software"), to deal in the Software without restriction, 
  * including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
  * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
  * subject to the following conditions:
  *
  * The above copyright notice and this permission notice shall be included in all copies or substantial 
  * portions of the Software.
  *
  * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT 
  * NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
  * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
  * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
  * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
  */
using System;
using System.Runtime.InteropServices;

namespace PortAudioSharp
{
    /**
		<summary>
			PortAudio v.19 bindings for .NET
		</summary>
	*/
    public partial class CPortAudio
    {
        #region **** PORTAUDIO CALLBACKS ****
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate EPaStreamCallbackResult PaStreamCallbackDelegate(
            IntPtr input,
            IntPtr output,
            uint frameCount,
            ref SPaStreamCallbackTimeInfo timeInfo,
            EPaStreamCallbackFlags statusFlags,
            IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PaStreamFinishedCallbackDelegate(IntPtr userData);
        #endregion

        #region **** PORTAUDIO DATA STRUCTURES ****
        [StructLayout(LayoutKind.Sequential)]
        public struct SPaDeviceInfo
        {
            public int StructVersion;
            [MarshalAs(UnmanagedType.LPStr)] public string Name;
            public int HostApi;
            public int MaxInputChannels;
            public int MaxOutputChannels;
            public double DefaultLowInputLatency;
            public double DefaultLowOutputLatency;
            public double DefaultHighInputLatency;
            public double DefaultHighOutputLatency;
            public double DefaultSampleRate;

            public override string ToString()
            {
                return "[" + GetType().Name + "]" + "\n"
                       + "name: " + Name + "\n"
                       + "hostApi: " + HostApi + "\n"
                       + "maxInputChannels: " + MaxInputChannels + "\n"
                       + "maxOutputChannels: " + MaxOutputChannels + "\n"
                       + "defaultLowInputLatency: " + DefaultLowInputLatency + "\n"
                       + "defaultLowOutputLatency: " + DefaultLowOutputLatency + "\n"
                       + "defaultHighInputLatency: " + DefaultHighInputLatency + "\n"
                       + "defaultHighOutputLatency: " + DefaultHighOutputLatency + "\n"
                       + "defaultSampleRate: " + DefaultSampleRate;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SPaHostApiInfo
        {
            public int StructVersion;
            public EPaHostApiTypeId Type;
            [MarshalAs(UnmanagedType.LPStr)] public string Name;
            public int DeviceCount;
            public int DefaultInputDevice;
            public int DefaultOutputDevice;

            public override string ToString()
            {
                return "[" + GetType().Name + "]" + "\n"
                       + "structVersion: " + StructVersion + "\n"
                       + "type: " + Type + "\n"
                       + "name: " + Name + "\n"
                       + "deviceCount: " + DeviceCount + "\n"
                       + "defaultInputDevice: " + DefaultInputDevice + "\n"
                       + "defaultOutputDevice: " + DefaultOutputDevice;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SPaHostErrorInfo
        {
            public EPaHostApiTypeId HostApiType;
            public int ErrorCode;
            [MarshalAs(UnmanagedType.LPStr)] public string ErrorText;

            public override string ToString()
            {
                return "[" + GetType().Name + "]" + "\n"
                       + "hostApiType: " + HostApiType + "\n"
                       + "errorCode: " + ErrorCode + "\n"
                       + "errorText: " + ErrorText;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SPaStreamCallbackTimeInfo
        {
            public double InputBufferAdcTime;
            public double CurrentTime;
            public double OutputBufferDacTime;

            public override string ToString()
            {
                return "[" + GetType().Name + "]" + "\n"
                       + "currentTime: " + CurrentTime + "\n"
                       + "inputBufferAdcTime: " + InputBufferAdcTime + "\n"
                       + "outputBufferDacTime: " + OutputBufferDacTime;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SPaStreamInfo
        {
            public int StructVersion;
            public double InputLatency;
            public double OutputLatency;
            public double SampleRate;

            public override string ToString()
            {
                return "[" + GetType().Name + "]" + "\n"
                       + "structVersion: " + StructVersion + "\n"
                       + "inputLatency: " + InputLatency + "\n"
                       + "outputLatency: " + OutputLatency + "\n"
                       + "sampleRate: " + SampleRate;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SPaStreamParameters
        {
            public int Device;
            public int ChannelCount;
            public EPaSampleFormat SampleFormat;
            public double SuggestedLatency;
            internal IntPtr HostApiSpecificStreamInfo;

            public override string ToString()
            {
                return "[" + GetType().Name + "]" + "\n"
                       + "device: " + Device + "\n"
                       + "channelCount: " + ChannelCount + "\n"
                       + "sampleFormat: " + SampleFormat + "\n"
                       + "suggestedLatency: " + SuggestedLatency;
            }
        }
        #endregion

        #region **** PORTAUDIO DEFINES ****
        public enum EPaDeviceIndex
        {
            PaNoDevice = -1,
            PaUseHostApiSpecificDeviceSpecification = -2
        }

        public enum EPaSampleFormat : uint
        {
            PaFloat32 = 0x00000001,
            PaInt32 = 0x00000002,
            PaInt24 = 0x00000004,
            PaInt16 = 0x00000008,
            PaInt8 = 0x00000010,
            PaUInt8 = 0x00000020,
            PaCustomFormat = 0x00010000,
            PaNonInterleaved = 0x80000000,
        }

        public const int PaFormatIsSupported = 0;
        public const int PaFramesPerBufferUnspecified = 0;

        public enum EPaStreamFlags : uint
        {
            PaNoFlag = 0,
            PaClipOff = 0x00000001,
            PaDitherOff = 0x00000002,
            PaNeverDropInput = 0x00000004,
            PaPrimeOutputBuffersUsingStreamCallback = 0x00000008,
            PaPlatformSpecificFlags = 0xFFFF0000
        }

        public enum EPaStreamCallbackFlags : uint
        {
            PaInputUnderflow = 0x00000001,
            PaInputOverflow = 0x00000002,
            PaOutputUnderflow = 0x00000004,
            PaOutputOverflow = 0x00000008,
            PaPrimingOutput = 0x00000010
        }
        #endregion

        #region **** PORTAUDIO ENUMERATIONS ****
        public enum EPaError
        {
            PaNoError = 0,
            PaNotInitialized = -10000,
            PaUnanticipatedHostError,
            PaInvalidChannelCount,
            PaInvalidSampleRate,
            PaInvalidDevice,
            PaInvalidFlag,
            PaSampleFormatNotSupported,
            PaBadIODeviceCombination,
            PaInsufficientMemory,
            PaBufferTooBig,
            PaBufferTooSmall,
            PaNullCallback,
            PaBadStreamPtr,
            PaTimedOut,
            PaInternalError,
            PaDeviceUnavailable,
            PaIncompatibleHostApiSpecificStreamInfo,
            PaStreamIsStopped,
            PaStreamIsNotStopped,
            PaInputOverflowed,
            PaOutputUnderflowed,
            PaHostApiNotFound,
            PaInvalidHostApi,
            PaCanNotReadFromCallbackStream,
            PaCanNotWriteToACallbackStream,
            PaCanNotReadFromAnOutputOnlyStream,
            PaCanNotWriteToAnInputOnlyStream,
            PaIncompatibleStreamHostApi,
            PaBadBufferPtr
        }

        public enum EPaHostApiTypeId : uint
        {
            PaInDevelopment = 0,
            PaDirectSound = 1,
            PaMME = 2,
            PaASIO = 3,
            PaSoundManager = 4,
            PaCoreAudio = 5,
            PaOSS = 7,
            PaALSA = 8,
            PaAL = 9,
            PaBeOS = 10,
            PaWDMKS = 11,
            PaJACK = 12,
            PaWASAPI = 13,
            PaAudioScienceHPI = 14
        }

        public enum EPaStreamCallbackResult : uint
        {
            PaContinue = 0,
            PaComplete = 1,
            PaAbort = 2
        }
        #endregion

        #region **** PORTAUDIO FUNCTIONS ****
#if ARCH_X86
#if WIN
        private const string _PaDll = "x86\\portaudio_x86.dll";
#endif

#if LINUX
        private const string _PaDll = "libportaudio.so.2.0.0";
#endif
#endif

#if ARCH_X64
#if WIN
        private const string _PaDll = "x64\\portaudio_x64.dll";
#endif

#if LINUX
        private const string _PaDll = "libportaudio.so.2.0.0";
#endif
#endif

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Pa_GetVersion();

        [DllImport(_PaDll, EntryPoint = "Pa_GetVersionText", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr IntPtr_Pa_GetVersionText();

        public static string PaGetVersionText()
        {
            IntPtr strptr = IntPtr_Pa_GetVersionText();
            return Marshal.PtrToStringAnsi(strptr);
        }

        [DllImport(_PaDll, EntryPoint = "Pa_GetErrorText", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr IntPtr_Pa_GetErrorText(EPaError errorCode);

        public static string PaGetErrorText(EPaError errorCode)
        {
            IntPtr strptr = IntPtr_Pa_GetErrorText(errorCode);
            return Marshal.PtrToStringAnsi(strptr);
        }

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_Initialize();

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_Terminate();

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Pa_GetHostApiCount();

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Pa_GetDefaultHostApi();

        [DllImport(_PaDll, EntryPoint = "Pa_GetHostApiInfo", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr IntPtr_Pa_GetHostApiInfo(int hostApi);

        public static SPaHostApiInfo PaGetHostApiInfo(int hostApi)
        {
            IntPtr structptr = IntPtr_Pa_GetHostApiInfo(hostApi);
            return (SPaHostApiInfo)Marshal.PtrToStructure(structptr, typeof(SPaHostApiInfo));
        }

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Pa_HostApiTypeIdToHostApiIndex(EPaHostApiTypeId type);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Pa_HostApiDeviceIndexToDeviceIndex(int hostApi, int hostApiDeviceIndex);

        [DllImport(_PaDll, EntryPoint = "Pa_GetLastHostErrorInfo", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr IntPtr_Pa_GetLastHostErrorInfo();

        public static SPaHostErrorInfo PaGetLastHostErrorInfo()
        {
            IntPtr structptr = IntPtr_Pa_GetLastHostErrorInfo();
            return (SPaHostErrorInfo)Marshal.PtrToStructure(structptr, typeof(SPaHostErrorInfo));
        }

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Pa_GetDeviceCount();

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Pa_GetDefaultInputDevice();

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Pa_GetDefaultOutputDevice();

        [DllImport(_PaDll, EntryPoint = "Pa_GetDeviceInfo", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr IntPtr_Pa_GetDeviceInfo(int device);

        public static SPaDeviceInfo PaGetDeviceInfo(int device)
        {
            IntPtr structptr = IntPtr_Pa_GetDeviceInfo(device);
            return (SPaDeviceInfo)Marshal.PtrToStructure(structptr, typeof(SPaDeviceInfo));
        }

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_IsFormatSupported(
            ref SPaStreamParameters inputParameters,
            ref SPaStreamParameters outputParameters,
            double sampleRate);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_OpenStream(
            out IntPtr stream,
            ref SPaStreamParameters inputParameters,
            ref SPaStreamParameters outputParameters,
            double sampleRate,
            uint framesPerBuffer,
            EPaStreamFlags streamFlags,
            PaStreamCallbackDelegate streamCallback,
            IntPtr userData);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_OpenStream(
            out IntPtr stream,
            ref SPaStreamParameters inputParameters,
            IntPtr outputParameters,
            double sampleRate,
            uint framesPerBuffer,
            EPaStreamFlags streamFlags,
            PaStreamCallbackDelegate streamCallback,
            IntPtr userData);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_OpenStream(
            out IntPtr stream,
            IntPtr inputParameters,
            ref SPaStreamParameters outputParameters,
            double sampleRate,
            uint framesPerBuffer,
            EPaStreamFlags streamFlags,
            PaStreamCallbackDelegate streamCallback,
            IntPtr userData);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_OpenDefaultStream(
            out IntPtr stream,
            int numInputChannels,
            int numOutputChannels,
            uint sampleFormat,
            double sampleRate,
            uint framesPerBuffer,
            PaStreamCallbackDelegate streamCallback,
            IntPtr userData);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_CloseStream(IntPtr stream);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_SetStreamFinishedCallback(
            ref IntPtr stream,
            [MarshalAs(UnmanagedType.FunctionPtr)] PaStreamFinishedCallbackDelegate streamFinishedCallback);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_StartStream(IntPtr stream);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_StopStream(IntPtr stream);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_AbortStream(IntPtr stream);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_IsStreamStopped(IntPtr stream);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_IsStreamActive(IntPtr stream);

        [DllImport(_PaDll, EntryPoint = "Pa_GetStreamInfo", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr IntPtr_Pa_GetStreamInfo(IntPtr stream);

        public static SPaStreamInfo PaGetStreamInfo(IntPtr stream)
        {
            IntPtr structptr = IntPtr_Pa_GetStreamInfo(stream);
            return (SPaStreamInfo)Marshal.PtrToStructure(structptr, typeof(SPaStreamInfo));
        }

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Pa_GetStreamTime(IntPtr stream);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern double Pa_GetStreamCpuLoad(IntPtr stream);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_ReadStream(
            IntPtr stream,
            [Out] float[] buffer,
            uint frames);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_ReadStream(
            IntPtr stream,
            [Out] byte[] buffer,
            uint frames);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_ReadStream(
            IntPtr stream,
            [Out] sbyte[] buffer,
            uint frames);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_ReadStream(
            IntPtr stream,
            [Out] ushort[] buffer,
            uint frames);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_ReadStream(
            IntPtr stream,
            [Out] short[] buffer,
            uint frames);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_ReadStream(
            IntPtr stream,
            [Out] uint[] buffer,
            uint frames);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_ReadStream(
            IntPtr stream,
            [Out] int[] buffer,
            uint frames);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_WriteStream(
            IntPtr stream,
            [In] float[] buffer,
            uint frames);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_WriteStream(
            IntPtr stream,
            [In] byte[] buffer,
            uint frames);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_WriteStream(
            IntPtr stream,
            [In] sbyte[] buffer,
            uint frames);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_WriteStream(
            IntPtr stream,
            [In] ushort[] buffer,
            uint frames);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_WriteStream(
            IntPtr stream,
            [In] short[] buffer,
            uint frames);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_WriteStream(
            IntPtr stream,
            [In] uint[] buffer,
            uint frames);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_WriteStream(
            IntPtr stream,
            [In] int[] buffer,
            uint frames);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Pa_GetStreamReadAvailable(IntPtr stream);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Pa_GetStreamWriteAvailable(IntPtr stream);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError Pa_GetSampleSize(EPaSampleFormat format);

        [DllImport(_PaDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Pa_Sleep(int msec);
        #endregion

        private CPortAudio()
        {
            // This is a static class
        }
    }
}