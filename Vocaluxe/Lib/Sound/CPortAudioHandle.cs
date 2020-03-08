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
using System.Diagnostics;
using PortAudioSharp;
using Vocaluxe.Base;

namespace Vocaluxe.Lib.Sound
{
    /// <summary>
    ///     PortAudio can be used for record and playback
    ///     So do some common stuff here and make sure those 2 do not interfere
    ///     Basic lifetime: On Init() get a new handle, close/dispose it in your close/dispose
    ///     DO NEVER use following Pa_* functions other than the ones from this class:
    ///     Initialize, Terminate, OpenStream, CloseStream
    /// </summary>
    class CPortAudioHandle : IDisposable
    {
        private static int _RefCount;
        private static readonly object _Mutex = new object();

        private bool _Disposed;
        private readonly List<IntPtr> _Streams = new List<IntPtr>();

        /// <summary>
        ///     Initializes PortAudio library (if required)
        /// </summary>
        public CPortAudioHandle()
        {
            lock (_Mutex)
            {
                if (_RefCount == 0)
                {
                    if (CheckError("Initialize", PortAudio.Pa_Initialize()))
                        throw new Exception();
                }
                _RefCount++;
            }
        }

        ~CPortAudioHandle()
        {
            _Dispose(false);
        }

        private void _Dispose(bool disposing)
        {
            if (_Disposed)
                return;
            if (!disposing)
                CLog.LogDebug("Did not close CPortAudioHandle");
            //Make sure we do not leek any streams as we may keep PA open
            if (_Streams.Count > 0)
            {
                CLog.LogDebug("Did not close " + _Streams.Count + "PortAudio-Stream(s)");
                while (_Streams.Count > 0)
                    CloseStream(_Streams[0]);
            }
            lock (_Mutex)
            {
                if (_Disposed)
                    return;
                Debug.Assert(_RefCount > 0);
                _RefCount--;
                if (_RefCount == 0)
                {
                    try
                    {
                        PortAudio.Pa_Terminate();
                    }
                    catch (Exception ex)
                    {
                        CLog.LogError(errorText: $"Error disposing PortAudio: {ex.Message}", e: ex);
                    }
                }
                _Disposed = true;
            }
        }

        public void Dispose()
        {
            _Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Close the PortAudio handle once you are done
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        public PortAudio.PaError OpenStream(out IntPtr stream, ref PortAudio.PaStreamParameters? inputParameters, ref PortAudio.PaStreamParameters? outputParameters,
                                            double sampleRate, uint framesPerBuffer, PortAudio.PaStreamFlags streamFlags,
                                            PortAudio.PaStreamCallbackDelegate streamCallback, IntPtr userData)
        {
            lock (_Mutex)
            {
                if (_Disposed)
                    throw new ObjectDisposedException("PortAudioHandle already disposed");

                PortAudio.PaError res = PortAudio.Pa_OpenStream(out stream, ref inputParameters, ref outputParameters, sampleRate, framesPerBuffer, streamFlags, streamCallback,
                                                                userData);
                if (res == PortAudio.PaError.paNoError)
                    _Streams.Add(stream);
                return res;
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
        public bool OpenInputStream(out IntPtr stream, ref PortAudio.PaStreamParameters? inputParameters,
                                    double sampleRate, uint framesPerBuffer, PortAudio.PaStreamFlags streamFlags,
                                    PortAudio.PaStreamCallbackDelegate streamCallback, IntPtr userData)
        {
            PortAudio.PaStreamParameters? outputParameters = null;
            return
                !CheckError("OpenInputStream",
                            OpenStream(out stream, ref inputParameters, ref outputParameters, sampleRate, framesPerBuffer, streamFlags, streamCallback, userData));
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
        public bool OpenOutputStream(out IntPtr stream, ref PortAudio.PaStreamParameters? outputParameters,
                                     double sampleRate, uint framesPerBuffer, PortAudio.PaStreamFlags streamFlags,
                                     PortAudio.PaStreamCallbackDelegate streamCallback, IntPtr userData)
        {
            PortAudio.PaStreamParameters? inputParameters = null;
            return
                !CheckError("OpenOutputStream",
                            OpenStream(out stream, ref inputParameters, ref outputParameters, sampleRate, framesPerBuffer, streamFlags, streamCallback, userData));
        }

        public void CloseStream(IntPtr stream)
        {
            lock (_Mutex)
            {
                if (_Disposed)
                    throw new ObjectDisposedException("PortAudioHandle already disposed");

                try
                {
                    PortAudio.Pa_CloseStream(stream);
                }
                catch (Exception ex)
                {
                    CLog.LogError(errorText:$"Error closing stream: {ex.Message}", e:ex);
                }
                _Streams.Remove(stream);
            }
        }

        /// <summary>
        ///     Checks if PA returned an error and logs it
        ///     Returns true on error
        /// </summary>
        /// <param name="action">Action identifier (E.g. openStream)</param>
        /// <param name="errorCode">Result returned by Pa_* call</param>
        /// <returns>True on error</returns>
        public bool CheckError(String action, PortAudio.PaError errorCode)
        {
            if (_Disposed)
                throw new ObjectDisposedException("PortAudioHandle already disposed");

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
        public int GetHostApi()
        {
            if (_Disposed)
                throw new ObjectDisposedException("PortAudioHandle already disposed");

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