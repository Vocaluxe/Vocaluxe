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
    ///     PortAudio can be used for both record and playback, so the library lifetime
    ///     (Initialize/Terminate) is reference-counted and shared here.
    ///     Ported from the old low-level PortAudioSharp binding (raw IntPtr streams + PaError
    ///     return codes) to PortAudioSharp2, which wraps streams in <see cref="Stream" /> objects
    ///     and signals failures via <see cref="PortAudioException" />.
    /// </summary>
    class CPortAudioHandle : IDisposable
    {
        private static int _RefCount;
        private static readonly object _Mutex = new object();

        private bool _Disposed;
        private readonly List<Stream> _Streams = new List<Stream>();

        /// <summary>
        ///     Initializes the PortAudio library (if not already done by another handle).
        /// </summary>
        public CPortAudioHandle()
        {
            lock (_Mutex)
            {
                if (_RefCount == 0)
                {
                    PortAudio.LoadNativeLibrary();
                    PortAudio.Initialize();
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
                CLog.Debug("Did not close CPortAudioHandle");

            Stream[] streamsToClose;
            lock (_Mutex)
            {
                streamsToClose = _Streams.ToArray();
            }

            if (streamsToClose.Length > 0)
                CLog.Debug("Did not close " + streamsToClose.Length + " PortAudio-Stream(s)");

            foreach (Stream stream in streamsToClose)
                CloseStream(stream);

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
                        PortAudio.Terminate();
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

        /// <summary>
        ///     Opens and tracks an output (playback) stream. Returns null on failure.
        /// </summary>
        public Stream OpenOutputStream(StreamParameters outputParameters, double sampleRate, uint framesPerBuffer,
                                       StreamFlags streamFlags, Stream.Callback streamCallback)
        {
            return _Open(null, outputParameters, sampleRate, framesPerBuffer, streamFlags, streamCallback);
        }

        /// <summary>
        ///     Opens and tracks an input (record) stream. Returns null on failure.
        /// </summary>
        public Stream OpenInputStream(StreamParameters inputParameters, double sampleRate, uint framesPerBuffer,
                                      StreamFlags streamFlags, Stream.Callback streamCallback)
        {
            return _Open(inputParameters, null, sampleRate, framesPerBuffer, streamFlags, streamCallback);
        }

        private Stream _Open(StreamParameters? inputParameters, StreamParameters? outputParameters, double sampleRate,
                             uint framesPerBuffer, StreamFlags streamFlags, Stream.Callback streamCallback)
        {
            lock (_Mutex)
            {
                if (_Disposed)
                    throw new ObjectDisposedException("PortAudioHandle already disposed");

                try
                {
                    var stream = new Stream(inputParameters, outputParameters, sampleRate, framesPerBuffer, streamFlags, streamCallback, null);
                    _Streams.Add(stream);
                    return stream;
                }
                catch (PortAudioException e)
                {
                    CLog.Error("OpenStream error: " + e.Message);
                    return null;
                }
            }
        }

        public void CloseStream(Stream stream)
        {
            lock (_Mutex)
            {
                if (_Disposed || stream == null)
                    return;

                if (!_Streams.Remove(stream))
                {
                    CLog.Debug("Stream was already removed or never tracked, skipping duplicate close.");
                    return;
                }

                try
                {
                    if (!stream.IsStopped)
                        stream.Stop();
                }
                catch (Exception ex)
                {
                    CLog.Error(ex, "Error stopping stream before close:");
                }

                try
                {
                    stream.Close();
                    stream.Dispose();
                }
                catch (Exception ex)
                {
                    CLog.Error(ex, "Error closing stream:");
                }
            }
        }
    }
}
