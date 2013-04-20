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

namespace Vocaluxe.Lib.Sound.PortAudio
{
    /// <summary> PortAudio v.19 bindings for .NET - ASIO bindings </summary>
    public static partial class CPortAudio
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