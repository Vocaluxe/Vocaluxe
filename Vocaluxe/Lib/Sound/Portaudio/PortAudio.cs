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
using System.IO;

using Vocaluxe.Base;

namespace PortAudioSharp {

	/**
		<summary>
			PortAudio v.19 bindings for .NET
		</summary>
	*/
	public partial class PortAudio
	{	
	    #region **** PORTAUDIO CALLBACKS ****
	    
	    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate PaStreamCallbackResult PaStreamCallbackDelegate(
	 		IntPtr input,
	 		IntPtr output,
	 		uint frameCount, 
	 		ref PaStreamCallbackTimeInfo timeInfo,
	 		PaStreamCallbackFlags statusFlags, 
	 		IntPtr userData);
	 	
	    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	 	public delegate void PaStreamFinishedCallbackDelegate(IntPtr userData);
	 	
	 	#endregion
	 	
		#region **** PORTAUDIO DATA STRUCTURES ****
		
		[StructLayout (LayoutKind.Sequential)]
		public struct PaDeviceInfo {
			
			public int structVersion;
			[MarshalAs (UnmanagedType.LPStr)]
			public string name;
			public int hostApi;
			public int maxInputChannels;
			public int maxOutputChannels;
			public double defaultLowInputLatency;
			public double defaultLowOutputLatency;
			public double defaultHighInputLatency;
			public double defaultHighOutputLatency;
			public double defaultSampleRate;
			
			public override string ToString() {
				return "[" + this.GetType().Name + "]" + "\n"
					+ "name: " + name + "\n"
					+ "hostApi: " + hostApi + "\n"
					+ "maxInputChannels: " + maxInputChannels + "\n"
					+ "maxOutputChannels: " + maxOutputChannels + "\n"
					+ "defaultLowInputLatency: " + defaultLowInputLatency + "\n"
					+ "defaultLowOutputLatency: " + defaultLowOutputLatency + "\n"
					+ "defaultHighInputLatency: " + defaultHighInputLatency + "\n"
					+ "defaultHighOutputLatency: " + defaultHighOutputLatency + "\n"
					+ "defaultSampleRate: " + defaultSampleRate;
			}
		}
		
		[StructLayout (LayoutKind.Sequential)]
		public struct PaHostApiInfo {
			
			public int structVersion;
			public PaHostApiTypeId type;
			[MarshalAs (UnmanagedType.LPStr)]
			public string name;
			public int deviceCount;
			public int defaultInputDevice;
			public int defaultOutputDevice;
			
			public override string ToString() {
				return "[" + this.GetType().Name + "]" + "\n"
					+ "structVersion: " + structVersion + "\n"
					+ "type: " + type + "\n"
					+ "name: " + name + "\n"
					+ "deviceCount: " + deviceCount + "\n"
					+ "defaultInputDevice: " + defaultInputDevice + "\n"
					+ "defaultOutputDevice: " + defaultOutputDevice;
			}
		}
		
		[StructLayout (LayoutKind.Sequential)]
		public struct PaHostErrorInfo {
			
			public PaHostApiTypeId 	hostApiType;
			public int errorCode;
			[MarshalAs (UnmanagedType.LPStr)]
			public string errorText;
			
			public override string ToString() {
				return "[" + this.GetType().Name + "]" + "\n"
					+ "hostApiType: " + hostApiType + "\n"
					+ "errorCode: " + errorCode + "\n"
					+ "errorText: " + errorText;
			}
		}
		
		[StructLayout (LayoutKind.Sequential)]
		public struct PaStreamCallbackTimeInfo {
			
			public double inputBufferAdcTime;
	 		public double currentTime;
  			public double outputBufferDacTime;
  			
  			public override string ToString() {
				return "[" + this.GetType().Name + "]" + "\n"
					+ "currentTime: " + currentTime + "\n"
					+ "inputBufferAdcTime: " + inputBufferAdcTime + "\n"
					+ "outputBufferDacTime: " + outputBufferDacTime;
			}
	 	}
	 	
	 	[StructLayout (LayoutKind.Sequential)]
		public struct PaStreamInfo {
	 		
			public int structVersion;
			public double inputLatency;
			public double outputLatency;
			public double sampleRate;
			
			public override string ToString() {
				return "[" + this.GetType().Name + "]" + "\n"
					+ "structVersion: " + structVersion + "\n"
					+ "inputLatency: " + inputLatency + "\n"
					+ "outputLatency: " + outputLatency + "\n"
					+ "sampleRate: " + sampleRate;
			}
		}
	 	
		[StructLayout (LayoutKind.Sequential)]
		public struct PaStreamParameters {
			
			public int device;
			public int channelCount;
			public PaSampleFormat sampleFormat;
			public double suggestedLatency;
			public IntPtr hostApiSpecificStreamInfo;
			
			public override string ToString() {
				return "[" + this.GetType().Name + "]" + "\n"
					+ "device: " + device + "\n"
					+ "channelCount: " + channelCount + "\n"
					+ "sampleFormat: " + sampleFormat + "\n"
					+ "suggestedLatency: " + suggestedLatency;
			}
		}
	 		
		#endregion
		
		#region **** PORTAUDIO DEFINES ****
		
		public enum PaDeviceIndex: int
		{
			paNoDevice = -1,
			paUseHostApiSpecificDeviceSpecification = -2
		}

		public enum PaSampleFormat: uint
		{
		   paFloat32 = 0x00000001,
		   paInt32 = 0x00000002,
		   paInt24 = 0x00000004,
		   paInt16 = 0x00000008,
		   paInt8 = 0x00000010,
		   paUInt8 = 0x00000020,
		   paCustomFormat = 0x00010000,
		   paNonInterleaved = 0x80000000,
		} 

		public const int paFormatIsSupported = 0;
		public const int paFramesPerBufferUnspecified = 0;
		
		public enum PaStreamFlags: uint
		{
			paNoFlag = 0,
			paClipOff = 0x00000001,
			paDitherOff = 0x00000002,
			paNeverDropInput = 0x00000004,
			paPrimeOutputBuffersUsingStreamCallback = 0x00000008,
			paPlatformSpecificFlags = 0xFFFF0000
		}
		
		public enum PaStreamCallbackFlags: uint
		{
			paInputUnderflow = 0x00000001,
			paInputOverflow = 0x00000002,
			paOutputUnderflow = 0x00000004,
			paOutputOverflow = 0x00000008,
			paPrimingOutput = 0x00000010
		}
		
		#endregion
		
		#region **** PORTAUDIO ENUMERATIONS ****
		
		public enum PaError : int {
  			paNoError = 0,
  			paNotInitialized = -10000,
  			paUnanticipatedHostError,
		    paInvalidChannelCount,
		    paInvalidSampleRate,
		    paInvalidDevice,
		    paInvalidFlag,
		    paSampleFormatNotSupported,
		    paBadIODeviceCombination,
		    paInsufficientMemory,
		    paBufferTooBig,
		    paBufferTooSmall,
		    paNullCallback,
		    paBadStreamPtr,
		    paTimedOut,
		    paInternalError,
		    paDeviceUnavailable,
		    paIncompatibleHostApiSpecificStreamInfo,
		    paStreamIsStopped,
		    paStreamIsNotStopped,
		    paInputOverflowed,
		    paOutputUnderflowed,
		    paHostApiNotFound,
		    paInvalidHostApi,
		    paCanNotReadFromACallbackStream,
		    paCanNotWriteToACallbackStream,
		    paCanNotReadFromAnOutputOnlyStream,
		    paCanNotWriteToAnInputOnlyStream,
		    paIncompatibleStreamHostApi,
		    paBadBufferPtr
  		}
		
		public enum PaHostApiTypeId : uint {
  			paInDevelopment=0,
		    paDirectSound=1,
		    paMME=2,
		    paASIO=3,
		    paSoundManager=4,
		    paCoreAudio=5,
		    paOSS=7,
		    paALSA=8,
		    paAL=9,
		    paBeOS=10,
		    paWDMKS=11,
		    paJACK=12,
		    paWASAPI=13,
		    paAudioScienceHPI=14
		}
		
		public enum PaStreamCallbackResult : uint { 
			paContinue = 0, 
			paComplete = 1, 
			paAbort = 2 
		}
		
		#endregion
		
		#region **** PORTAUDIO FUNCTIONS ****
#if ARCH_X86
#if WIN
        private const string PaDll = "x86\\portaudio_x86.dll";
#endif

#if LINUX
        private const string PaDll = "libportaudio.so.2.0.0";
#endif
#endif

#if ARCH_X64
#if WIN
        private const string PaDll = "x64\\portaudio_x64.dll";
#endif

#if LINUX
        private const string PaDll = "libportaudio.so.2.0.0";
#endif
#endif

        [DllImport (PaDll)]
	 	public static extern int Pa_GetVersion();
	 	
	 	[DllImport (PaDll,EntryPoint="Pa_GetVersionText")]
	 	private static extern IntPtr IntPtr_Pa_GetVersionText();
	 	
	 	public static string Pa_GetVersionText() {
	 		IntPtr strptr = IntPtr_Pa_GetVersionText();
	 		return Marshal.PtrToStringAnsi(strptr);
	 	}
	 	
	 	[DllImport (PaDll,EntryPoint="Pa_GetErrorText")]
	 	public static extern IntPtr IntPtr_Pa_GetErrorText(PaError errorCode);
	 	
	 	public static string Pa_GetErrorText(PaError errorCode) {
	 		IntPtr strptr = IntPtr_Pa_GetErrorText(errorCode);
	 		return Marshal.PtrToStringAnsi(strptr);
	 	}
	 	
	 	[DllImport (PaDll)]
	 	public static extern PaError Pa_Initialize();
	 	
	 	[DllImport (PaDll)]
	 	public static extern PaError Pa_Terminate();
	 	
		[DllImport (PaDll)]
	 	public static extern int Pa_GetHostApiCount();

		[DllImport (PaDll)]
	 	public static extern int Pa_GetDefaultHostApi();

		[DllImport (PaDll,EntryPoint="Pa_GetHostApiInfo")]
	 	public static extern IntPtr IntPtr_Pa_GetHostApiInfo(int hostApi);
	 	
	 	public static PaHostApiInfo Pa_GetHostApiInfo(int hostApi) {
	 		IntPtr structptr = IntPtr_Pa_GetHostApiInfo(hostApi);
	 		return (PaHostApiInfo) Marshal.PtrToStructure(structptr, typeof(PaHostApiInfo));
	 	}
		
		[DllImport (PaDll)]
	 	public static extern int Pa_HostApiTypeIdToHostApiIndex(PaHostApiTypeId type);

		[DllImport (PaDll)]
	 	public static extern int Pa_HostApiDeviceIndexToDeviceIndex(int hostApi, int hostApiDeviceIndex);

		[DllImport (PaDll,EntryPoint="Pa_GetLastHostErrorInfo")]
	 	public static extern IntPtr IntPtr_Pa_GetLastHostErrorInfo();
	 	
	 	public static PaHostErrorInfo Pa_GetLastHostErrorInfo() {
	 		IntPtr structptr = IntPtr_Pa_GetLastHostErrorInfo();
	 		return (PaHostErrorInfo) Marshal.PtrToStructure(structptr, typeof(PaHostErrorInfo));
	 	}

		[DllImport (PaDll)]
	 	public static extern int Pa_GetDeviceCount();
		
		[DllImport (PaDll)]
	 	public static extern int Pa_GetDefaultInputDevice();

		[DllImport (PaDll)]
	 	public static extern int Pa_GetDefaultOutputDevice();
		
		[DllImport (PaDll,EntryPoint="Pa_GetDeviceInfo")]
	 	public static extern IntPtr IntPtr_Pa_GetDeviceInfo(int device);
	 	
	 	public static PaDeviceInfo Pa_GetDeviceInfo(int device) {
	 		IntPtr structptr = IntPtr_Pa_GetDeviceInfo(device);
	 		return (PaDeviceInfo) Marshal.PtrToStructure(structptr, typeof(PaDeviceInfo));
	 	}
		
		[DllImport (PaDll)]
	 	public static extern PaError Pa_IsFormatSupported(
	 		ref PaStreamParameters inputParameters, 
	 		ref PaStreamParameters outputParameters, 
	 		double sampleRate);
		
		[DllImport (PaDll)]
	 	public static extern PaError Pa_OpenStream(
	 		out IntPtr stream,
	 		ref PaStreamParameters inputParameters, 
	 		ref PaStreamParameters outputParameters,
	 		double sampleRate, 
	 		uint framesPerBuffer,
	 		PaStreamFlags streamFlags,
	 		PaStreamCallbackDelegate streamCallback,
	 		IntPtr userData);

        [DllImport(PaDll)]
        public static extern PaError Pa_OpenStream(
            out IntPtr stream,
            ref PaStreamParameters inputParameters,
            IntPtr outputParameters,
            double sampleRate,
            uint framesPerBuffer,
            PaStreamFlags streamFlags,
            PaStreamCallbackDelegate streamCallback,
            IntPtr userData);

        [DllImport(PaDll)]
        public static extern PaError Pa_OpenStream(
            out IntPtr stream,
            IntPtr inputParameters,
            ref PaStreamParameters outputParameters,
            double sampleRate,
            uint framesPerBuffer,
            PaStreamFlags streamFlags,
            PaStreamCallbackDelegate streamCallback,
            IntPtr userData);

		[DllImport (PaDll)]
	 	public static extern PaError Pa_OpenDefaultStream(
	 		out IntPtr stream,
	 		int numInputChannels, 
	 		int numOutputChannels, 
	 		uint sampleFormat,
	 		double sampleRate, 
	 		uint framesPerBuffer,
	 		PaStreamCallbackDelegate streamCallback,
	 		IntPtr userData);
	 		
		[DllImport (PaDll)]
	 	public static extern PaError Pa_CloseStream(IntPtr stream);
	 	
		[DllImport (PaDll)]
	 	public static extern PaError Pa_SetStreamFinishedCallback(
	 		ref IntPtr stream,
	 		[MarshalAs(UnmanagedType.FunctionPtr)]PaStreamFinishedCallbackDelegate streamFinishedCallback);
		
		[DllImport (PaDll)]
	 	public static extern PaError Pa_StartStream(IntPtr stream);
		
		[DllImport (PaDll)]
	 	public static extern PaError Pa_StopStream(IntPtr stream);
		
		[DllImport (PaDll)]
	 	public static extern PaError Pa_AbortStream(IntPtr stream);
		
		[DllImport (PaDll)]
	 	public static extern PaError Pa_IsStreamStopped(IntPtr stream);
		
		[DllImport (PaDll)]
	 	public static extern PaError Pa_IsStreamActive(IntPtr stream);
		
		[DllImport (PaDll,EntryPoint="Pa_GetStreamInfo")]
	 	public static extern IntPtr IntPtr_Pa_GetStreamInfo(IntPtr stream);
	 	
	 	public static PaStreamInfo Pa_GetStreamInfo(IntPtr stream) {
	 		IntPtr structptr = IntPtr_Pa_GetStreamInfo(stream);
	 		return (PaStreamInfo) Marshal.PtrToStructure(structptr,typeof(PaStreamInfo));
	 	}
		
		[DllImport (PaDll)]
	 	public static extern double Pa_GetStreamTime(IntPtr stream);
		
		[DllImport (PaDll)]
	 	public static extern double Pa_GetStreamCpuLoad(IntPtr stream);
		
		[DllImport (PaDll)]
	 	public static extern PaError Pa_ReadStream(
	 		IntPtr stream,
	 		[Out]float[] buffer,
			uint frames);
		
		[DllImport (PaDll)]
	 	public static extern PaError Pa_ReadStream(
	 		IntPtr stream,
	 		[Out]byte[] buffer,
			uint frames);
		
		[DllImport (PaDll)]
	 	public static extern PaError Pa_ReadStream(
	 		IntPtr stream,
	 		[Out]sbyte[] buffer,
			uint frames);
		
		[DllImport (PaDll)]
	 	public static extern PaError Pa_ReadStream(
	 		IntPtr stream,
	 		[Out]ushort[] buffer,
			uint frames);
		
		[DllImport (PaDll)]
	 	public static extern PaError Pa_ReadStream(
	 		IntPtr stream,
	 		[Out]short[] buffer,
			uint frames);
		
		[DllImport (PaDll)]
	 	public static extern PaError Pa_ReadStream(
	 		IntPtr stream,
	 		[Out]uint[] buffer,
			uint frames);
		
		[DllImport (PaDll)]
	 	public static extern PaError Pa_ReadStream(
	 		IntPtr stream,
	 		[Out]int[] buffer,
			uint frames);
		
		[DllImport (PaDll)]
	 	public static extern PaError Pa_WriteStream(
	 		IntPtr stream,
	 		[In]float[] buffer,
			uint frames);
		
		[DllImport (PaDll)]
	 	public static extern PaError Pa_WriteStream(
	 		IntPtr stream,
	 		[In]byte[] buffer,
			uint frames);
				
		[DllImport (PaDll)]
	 	public static extern PaError Pa_WriteStream(
	 		IntPtr stream,
	 		[In]sbyte[] buffer,
			uint frames);
		
		[DllImport (PaDll)]
	 	public static extern PaError Pa_WriteStream(
	 		IntPtr stream,
	 		[In]ushort[] buffer,
			uint frames);
			
		[DllImport (PaDll)]
	 	public static extern PaError Pa_WriteStream(
	 		IntPtr stream,
	 		[In]short[] buffer,
			uint frames);
		
		[DllImport (PaDll)]
	 	public static extern PaError Pa_WriteStream(
	 		IntPtr stream,
	 		[In]uint[] buffer,
			uint frames);
		
		[DllImport (PaDll)]
	 	public static extern PaError Pa_WriteStream(
	 		IntPtr stream,
	 		[In]int[] buffer,
			uint frames);
		
		[DllImport (PaDll)]
	 	public static extern int Pa_GetStreamReadAvailable(IntPtr stream);
		
		[DllImport (PaDll)]
	 	public static extern int Pa_GetStreamWriteAvailable(IntPtr stream);
		
		[DllImport (PaDll)]
	 	public static extern PaError Pa_GetSampleSize(PaSampleFormat format);
		
		[DllImport (PaDll)]
	 	public static extern void Pa_Sleep(int msec);
	 	
	 	#endregion
	 	
	 	private PortAudio() {
	 		// This is a static class
	 	}
	 	
	 }
 
 }
