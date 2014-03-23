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
using System.Diagnostics;
using PortAudioSharp;
using Vocaluxe.Base;

namespace Vocaluxe.Lib.Sound
{
    /// <summary>
    ///     PortAudio can be used for record and playback
    ///     So do some common stuff here and make sure those 2 do not interfere
    ///     DO NEVER use following Pa_* functions other than the ones from this class:
    ///     Initialize, Terminate, OpenStream, CloseStream
    /// </summary>
    abstract class CPortAudioCommon
    {
        private static int _RefCount;
        private static readonly object _Mutex = new object();

        /// <summary>
        ///     Safe method to init PortAudio (adds a reference)
        ///     MUST call CloseDriver when done
        /// </summary>
        /// <returns>True on success, falso if not initialized (log written)</returns>
        protected static bool _InitDriver()
        {
            lock (_Mutex)
            {
                if (_RefCount == 0)
                {
                    if (_CheckError("Initialize", PortAudio.Pa_Initialize()))
                        return false;
                }
                _RefCount++;
            }
            return true;
        }

        /// <summary>
        ///     Safe method to close PortAudio
        ///     MUST be called _exactly_ once after successfull InitDriver call
        /// </summary>
        protected static void _CloseDriver()
        {
            lock (_Mutex)
            {
                Debug.Assert(_RefCount > 0);
                _RefCount--;
                if (_RefCount == 0)
                    PortAudio.Pa_Terminate();
            }
        }

        protected static PortAudio.PaError _OpenStream(out IntPtr stream, ref PortAudio.PaStreamParameters? inputParameters, ref PortAudio.PaStreamParameters? outputParameters,
                                                       double sampleRate, uint framesPerBuffer, PortAudio.PaStreamFlags streamFlags,
                                                       PortAudio.PaStreamCallbackDelegate streamCallback, IntPtr userData)
        {
            lock (_Mutex)
            {
                return PortAudio.Pa_OpenStream(out stream, ref inputParameters, ref outputParameters, sampleRate, framesPerBuffer, streamFlags, streamCallback, userData);
            }
        }

        /// <summary>
        ///     Convenience method to safely open an input stream and log potential error
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="inputParameters"></param>
        /// <param name="sampleRate"></param>
        /// <param name="framesPerBuffer"></param>
        /// <param name="streamFlags"></param>
        /// <param name="streamCallback"></param>
        /// <param name="userData"></param>
        /// <returns>True on success</returns>
        protected static bool _OpenInputStream(out IntPtr stream, ref PortAudio.PaStreamParameters? inputParameters,
                                               double sampleRate, uint framesPerBuffer, PortAudio.PaStreamFlags streamFlags,
                                               PortAudio.PaStreamCallbackDelegate streamCallback, IntPtr userData)
        {
            PortAudio.PaStreamParameters? outputParameters = null;
            return
                !_CheckError("OpenInputStream",
                             _OpenStream(out stream, ref inputParameters, ref outputParameters, sampleRate, framesPerBuffer, streamFlags, streamCallback, userData));
        }

        /// <summary>
        ///     Convenience method to safely open an output stream and log potential error
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="outputParameters"></param>
        /// <param name="sampleRate"></param>
        /// <param name="framesPerBuffer"></param>
        /// <param name="streamFlags"></param>
        /// <param name="streamCallback"></param>
        /// <param name="userData"></param>
        /// <returns>True on success</returns>
        protected static bool _OpenOutputStream(out IntPtr stream, ref PortAudio.PaStreamParameters? outputParameters,
                                                double sampleRate, uint framesPerBuffer, PortAudio.PaStreamFlags streamFlags,
                                                PortAudio.PaStreamCallbackDelegate streamCallback, IntPtr userData)
        {
            PortAudio.PaStreamParameters? inputParameters = null;
            return
                !_CheckError("OpenOutputStream",
                             _OpenStream(out stream, ref inputParameters, ref outputParameters, sampleRate, framesPerBuffer, streamFlags, streamCallback, userData));
        }

        protected static void _CloseStream(IntPtr stream)
        {
            lock (_Mutex)
            {
                PortAudio.Pa_CloseStream(stream);
            }
        }

        /// <summary>
        ///     Checks if PA returned an error and logs it
        ///     Returns true on error
        /// </summary>
        /// <param name="action">Action identifier (E.g. openStream)</param>
        /// <param name="errorCode">Result returned by Pa_* call</param>
        /// <returns>True on error</returns>
        protected static bool _CheckError(String action, PortAudio.PaError errorCode)
        {
            if (errorCode != PortAudio.PaError.paNoError)
            {
                CLog.LogError(action + " error: " + PortAudio.Pa_GetErrorText(errorCode));
                if (errorCode == PortAudio.PaError.paUnanticipatedHostError)
                {
                    PortAudio.PaHostErrorInfo errorInfo = PortAudio.Pa_GetLastHostErrorInfo();
                    CLog.LogError("- Host error API type: " + errorInfo.hostApiType);
                    CLog.LogError("- Host error code: " + errorInfo.errorCode);
                    CLog.LogError("- Host error text: " + errorInfo.errorText);
                }
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Selects the most appropriate host api
        /// </summary>
        /// <returns>The most appropriate host api</returns>
        protected static int _GetHostApi()
        {
            //Caller has to hold a reference anyway so no locking needed
            if (_RefCount < 1)
                return -1;

            int selectedHostApi = PortAudio.Pa_GetDefaultHostApi();
            int apiCount = PortAudio.Pa_GetHostApiCount();
            for (int i = 0; i < apiCount; i++)
            {
                PortAudio.PaHostApiInfo apiInfo = PortAudio.Pa_GetHostApiInfo(i);
                if ((apiInfo.type == PortAudio.PaHostApiTypeId.paDirectSound)
                    || (apiInfo.type == PortAudio.PaHostApiTypeId.paALSA))
                    selectedHostApi = i;
            }
            return selectedHostApi;
        }
    }
}