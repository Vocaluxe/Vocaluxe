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
using VocaluxeLib.Log;

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
                    {
                        throw new Exception();
                    }
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
            {
                return;
            }

            if (!disposing)
            {
                CLog.Debug("Did not close CPortAudioHandle");
            }

            IntPtr[] streamsToClose;
            lock (_Mutex)
            {
                streamsToClose = _Streams.ToArray();
            }

            if (streamsToClose.Length > 0)
            {
                CLog.Debug("Did not close " + streamsToClose.Length + " PortAudio-Stream(s)");
            }

            foreach (var stream in streamsToClose)
            {
                CloseStream(stream);
            }

            lock (_Mutex)
            {
                if (_Disposed)
                {
                    return;
                }

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
                        CLog.Error(ex, "Error disposing PortAudio");
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
                {
                    throw new ObjectDisposedException("PortAudioHandle already disposed");
                }

                var res = PortAudio.Pa_OpenStream(out stream, ref inputParameters, ref outputParameters, sampleRate, framesPerBuffer, streamFlags, streamCallback,
                    userData);
                if (res == PortAudio.PaError.paNoError)
                {
                    _Streams.Add(stream);
                }

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
                {
                    return;
                }

                if (stream == IntPtr.Zero)
                {
                    CLog.Debug("Stream is null, skipping close.");
                    return;
                }

                var wasTracked = _Streams.Remove(stream);
                if (!wasTracked)
                {
                    CLog.Debug("Stream was already removed or never tracked, skipping duplicate close.");
                    return;
                }

                try
                {
                    try
                    {
                        var isStoppedResult = PortAudio.Pa_IsStreamStopped(stream);
                        if (isStoppedResult == PortAudio.PaError.paStreamIsNotStopped)
                        {
                            var stopResult = PortAudio.Pa_StopStream(stream);
                            if (stopResult != PortAudio.PaError.paNoError &&
                                stopResult != PortAudio.PaError.paStreamIsStopped)
                            {
                                CLog.Error("StopStream before close failed: " + PortAudio.Pa_GetErrorText(stopResult));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        CLog.Error(ex, "Error stopping stream before close:");
                    }

                    var closeResult = PortAudio.Pa_CloseStream(stream);
                    if (closeResult != PortAudio.PaError.paNoError)
                    {
                        CLog.Error("CloseStream error: " + PortAudio.Pa_GetErrorText(closeResult));
                    }
                }
                catch (AccessViolationException ex)
                {
                    CLog.Error(ex, "Access violation while closing PortAudio stream.");
                }
                catch (Exception ex)
                {
                    CLog.Error(ex, "Error closing stream:");
                }
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
            {
                throw new ObjectDisposedException("PortAudioHandle already disposed");
            }

            if (errorCode != PortAudio.PaError.paNoError)
            {
                CLog.Error(action + " error: " + PortAudio.Pa_GetErrorText(errorCode));
                if (errorCode == PortAudio.PaError.paUnanticipatedHostError)
                {
                    var errorInfo = PortAudio.Pa_GetLastHostErrorInfo();
                    CLog.Error("- Host error API type: " + errorInfo.hostApiType);
                    CLog.Error("- Host error code: " + errorInfo.errorCode);
                    CLog.Error("- Host error text: " + errorInfo.errorText);
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
            {
                throw new ObjectDisposedException("PortAudioHandle already disposed");
            }

            var selectedHostApi = PortAudio.Pa_GetDefaultHostApi();
            var apiCount = PortAudio.Pa_GetHostApiCount();
            for (var i = 0; i < apiCount; i++)
            {
                var apiInfo = PortAudio.Pa_GetHostApiInfo(i);
                if (apiInfo.type == PortAudio.PaHostApiTypeId.paDirectSound
                    || apiInfo.type == PortAudio.PaHostApiTypeId.paALSA)
                {
                    selectedHostApi = i;
                }
            }

            return selectedHostApi;
        }
    }
}