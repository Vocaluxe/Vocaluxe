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
    /// <summary> PortAudio v.19 bindings for .NET - ASIO bindings </summary>
    public partial class CPortAudio
    {
        #region **** PORTAUDIO DATA STRUCTURES ****
        [StructLayout(LayoutKind.Sequential)]
        public struct SPaAsioStreamInfo
        {
            public ulong Size; /**< sizeof(PaAsioStreamInfo) */
            public int HostApiType; /**< paASIO */
            public ulong Version; /**< 1 */
            public ulong Flags;

            /// Support for opening only specific channels of an ASIO device.
            /// If the paAsioUseChannelSelectors flag is set, channelSelectors is a
            /// pointer to an array of integers specifying the device channels to use.
            /// When used, the length of the channelSelectors array must match the
            /// corresponding channelCount parameter to Pa_OpenStream() otherwise a
            /// crash may result.
            /// The values in the selectors array must specify channels within the
            /// range of supported channels for the device or paInvalidChannelCount will
            /// result.
            private readonly IntPtr _ChannelSelectors;
        }
        #endregion

        #region **** PORTAUDIO DEFINES ****
        public const int PaAsioUseChannelSelectors = 0x01;
        #endregion

        #region **** PORTAUDIO FUNCTIONS ****
        //		/// <summary> Retrieve legal latency settings for the specificed device, in samples. </summary>
        //		/// <param name="device"> The global index of the device about which the query is being made. </param>
        //		/// <param name="minLatency"> A pointer to the location which will recieve the minimum latency value. </param>
        //		/// <param name="maxLatency"> A pointer to the location which will recieve the maximum latency value. </param>
        //		/// <param name="preferredLatency"> A pointer to the location which will recieve the preferred latency value. </param>
        //		/// <param name="granularity"> A pointer to the location which will recieve the granularity. This value 
        //		/// 	determines which values between minLatency and maxLatency are available. ie the step size,
        //		/// 	if granularity is -1 then available latency settings are powers of two. </param>
        //		/// See ASIOGetBufferSize in the ASIO SDK.
        //		PaError PaAsio_GetAvailableLatencyValues( PaDeviceIndex device, long *minLatency, long *maxLatency, 
        //			long *preferredLatency, long *granularity );

        /// <summary> Display the ASIO control panel for the specified device. </summary>
        /// <param name="device"> The global index of the device whose control panel is to be displayed. </param>
        /// <param name="systemSpecific">
        ///     On Windows, the calling application's main window handle,
        ///     on Macintosh this value should be zero.
        /// </param>
        [DllImport("PortAudio.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError PaAsio_ShowControlPanel(int device, IntPtr systemSpecific);

        //	 	/// <summary> Retrieve a pointer to a string containing the name of the specified input channel. </summary>
        //	 	/// The string is valid until Pa_Terminate is called.
        //	 	/// The string will be no longer than 32 characters including the null terminator.
        //		PaError PaAsio_GetInputChannelName(PaDeviceIndex device, int channelIndex, const char** channelName );

        //		/// <summary> Retrieve a pointer to a string containing the name of the specified input channel. </summary>
        //		/// The string is valid until Pa_Terminate is called. 
        //		/// The string will be no longer than 32 characters including the null terminator.
        //		PaError PaAsio_GetOutputChannelName( PaDeviceIndex device, int channelIndex, const char** channelName );

        /// <summary> Set the sample rate of an open paASIO stream. </summary>
        /// <param name="stream"></param>
        /// The stream to operate on.
        /// <param name="sampleRate"></param>
        /// The new sample rate. 
        /// Note that this function may fail if the stream is alredy running and the 
        /// ASIO driver does not support switching the sample rate of a running stream.
        /// <returns> paIncompatibleStreamHostApi if stream is not a paASIO stream. </returns>
        [DllImport("PortAudio.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern EPaError PaAsio_SetStreamSampleRate(IntPtr stream, double sampleRate);
        #endregion
    }
}