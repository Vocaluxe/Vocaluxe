using System;
using System.Diagnostics;
using PortAudioSharp;
using Vocaluxe.Base;

namespace Vocaluxe.Lib.Sound
{
    /// <summary>
    /// PortAudio can be used for record and playback
    /// So do some common stuff here and make sure those 2 do not interfere
    /// 
    /// DO NEVER use following Pa_* functions other than the ones from this class:
    /// Initialize, Terminate, OpenStream, CloseStream
    /// </summary>
    static class CPortAudioCommon
    {
        private static int _RefCount;
        private static readonly object _Mutex = new object();

        /// <summary>
        /// Safe method to init PortAudio (adds a reference)
        /// MUST call CloseDriver when done
        /// </summary>
        /// <returns>True on success, falso if not initialized (log written)</returns>
        public static bool InitDriver()
        {
            lock (_Mutex)
            {
                if (_RefCount == 0)
                {
                    if (CheckError("Initialize", PortAudio.Pa_Initialize()))
                        return false;
                }
                _RefCount++;
            }
            return true;
        }

        /// <summary>
        /// Safe method to close PortAudio
        /// MUST be called _exactly_ once after successfull InitDriver call
        /// </summary>
        public static void CloseDriver()
        {
            lock (_Mutex)
            {
                Debug.Assert(_RefCount > 0);
                _RefCount--;
                if (_RefCount == 0)
                    PortAudio.Pa_Terminate();
            }
        }

        public static PortAudio.PaError OpenStream(out IntPtr stream, ref PortAudio.PaStreamParameters? inputParameters, ref PortAudio.PaStreamParameters? outputParameters,
                                                   double sampleRate, uint framesPerBuffer, PortAudio.PaStreamFlags streamFlags,
                                                   PortAudio.PaStreamCallbackDelegate streamCallback, IntPtr userData)
        {
            lock (_Mutex)
            {
                return PortAudio.Pa_OpenStream(out stream, ref inputParameters, ref outputParameters, sampleRate, framesPerBuffer, streamFlags, streamCallback, userData);
            }
        }

        /// <summary>
        /// Convenience method to safely open an input stream and log potential error
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="inputParameters"></param>
        /// <param name="sampleRate"></param>
        /// <param name="framesPerBuffer"></param>
        /// <param name="streamFlags"></param>
        /// <param name="streamCallback"></param>
        /// <param name="userData"></param>
        /// <returns>True on success</returns>
        public static bool OpenInputStream(out IntPtr stream, ref PortAudio.PaStreamParameters? inputParameters,
                                           double sampleRate, uint framesPerBuffer, PortAudio.PaStreamFlags streamFlags,
                                           PortAudio.PaStreamCallbackDelegate streamCallback, IntPtr userData)
        {
            PortAudio.PaStreamParameters? outputParameters = null;
            return
                !CheckError("OpenInputStream",
                            OpenStream(out stream, ref inputParameters, ref outputParameters, sampleRate, framesPerBuffer, streamFlags, streamCallback, userData));
        }

        /// <summary>
        /// Convenience method to safely open an output stream and log potential error
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="outputParameters"></param>
        /// <param name="sampleRate"></param>
        /// <param name="framesPerBuffer"></param>
        /// <param name="streamFlags"></param>
        /// <param name="streamCallback"></param>
        /// <param name="userData"></param>
        /// <returns>True on success</returns>
        public static bool OpenOutputStream(out IntPtr stream, ref PortAudio.PaStreamParameters? outputParameters,
                                            double sampleRate, uint framesPerBuffer, PortAudio.PaStreamFlags streamFlags,
                                            PortAudio.PaStreamCallbackDelegate streamCallback, IntPtr userData)
        {
            PortAudio.PaStreamParameters? inputParameters = null;
            return
                !CheckError("OpenOutputStream",
                            OpenStream(out stream, ref inputParameters, ref outputParameters, sampleRate, framesPerBuffer, streamFlags, streamCallback, userData));
        }

        public static void CloseStream(IntPtr stream)
        {
            lock (_Mutex)
            {
                PortAudio.Pa_CloseStream(stream);
            }
        }

        /// <summary>
        /// Checks if PA returned an error and logs it
        /// Returns true on error
        /// </summary>
        /// <param name="action">Action identifier (E.g. openStream)</param>
        /// <param name="errorCode">Result returned by Pa_* call</param>
        /// <returns>True on error</returns>
        public static bool CheckError(String action, PortAudio.PaError errorCode)
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
        /// Selects the most appropriate host api
        /// </summary>
        /// <returns>The most appropriate host api</returns>
        public static int GetHostApi()
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