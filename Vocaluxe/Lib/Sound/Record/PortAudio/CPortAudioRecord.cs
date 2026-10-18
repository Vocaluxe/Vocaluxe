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
using System.Runtime.InteropServices;
using System.Threading;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.Sound.Record.PortAudio
{
    class CPortAudioRecord : CRecordBase, IRecord
    {
        private bool _Initialized;
        private CPortAudioHandle _PaHandle;
        private PortAudioSharp.PortAudio.PaStreamCallbackDelegate _MyRecProc;
        private IntPtr[] _RecHandle;

        /// <summary>
        ///     Init PortAudio and list record devices
        /// </summary>
        /// <returns>true if success</returns>
        public override bool Init()
        {
            if (!base.Init())
            {
                return false;
            }

            try
            {
                _PaHandle = new CPortAudioHandle();

                var hostAPI = _PaHandle.GetHostApi();
                var numDevices = PortAudioSharp.PortAudio.Pa_GetDeviceCount();
                for (var i = 0; i < numDevices; i++)
                {
                    var info = PortAudioSharp.PortAudio.Pa_GetDeviceInfo(i);
                    if (info.hostApi == hostAPI && info.maxInputChannels > 0)
                    {
                        var dev = new CRecordDevice(i, info.name, info.name + i, info.maxInputChannels);

                        _Devices.Add(dev);
                    }
                }

                _RecHandle = new IntPtr[_Devices.Count];
                _MyRecProc = _MyPaStreamCallback;
                _Initialized = true;
            }
            catch (Exception e)
            {
                _Initialized = false;
                CLog.Error("Error initializing PortAudio: " + e.Message);
                Close();
                return false;
            }

            return true;
        }

        /// <summary>
        ///      Voice Capturing
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            if (!_Initialized)
            {
                return false;
            }

            Stop();

            if (_RecHandle != null && _PaHandle != null)
            {
                for (var i = 0; i < _RecHandle.Length; i++)
                {
                    var handle = _RecHandle[i];
                    if (handle == IntPtr.Zero)
                    {
                        continue;
                    }

                    var waitcount = 0;
                    while (waitcount < 5)
                    {
                        try
                        {
                            if (PortAudioSharp.PortAudio.Pa_IsStreamStopped(handle) !=
                                PortAudioSharp.PortAudio.PaError.paStreamIsNotStopped)
                            {
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            CLog.Error(ex, "Error while waiting for PortAudio stream to stop:");
                            break;
                        }

                        Thread.Sleep(1);
                        waitcount++;
                    }

                    try
                    {
                        _PaHandle.CloseStream(handle);
                    }
                    catch (Exception ex)
                    {
                        CLog.Error(ex, "Error closing old PortAudio record stream before restart:");
                    }
                    finally
                    {
                        _RecHandle[i] = IntPtr.Zero;
                    }
                }
            }

            foreach (var buffer in _Buffer)
            {
                buffer.Reset();
            }

            for (var dev = 0; dev < _Devices.Count; dev++)
            {
                var usingDevice = false;
                for (var ch = 0; ch < _Devices[dev].Channels; ++ch)
                {
                    if (_Devices[dev].PlayerChannel[ch] > 0)
                    {
                        usingDevice = true;
                    }
                }

                if (!usingDevice)
                {
                    continue;
                }

                PortAudioSharp.PortAudio.PaStreamParameters? inputParams =
                    new PortAudioSharp.PortAudio.PaStreamParameters
                    {
                        channelCount = _Devices[dev].Channels,
                        device = _Devices[dev].Id,
                        sampleFormat = PortAudioSharp.PortAudio.PaSampleFormat.paInt16,
                        suggestedLatency = PortAudioSharp.PortAudio.Pa_GetDeviceInfo(_Devices[dev].Id).defaultLowInputLatency,
                        hostApiSpecificStreamInfo = IntPtr.Zero
                    };

                if (!_PaHandle.OpenInputStream(
                        out _RecHandle[dev],
                        ref inputParams,
                        44100,
                        882,
                        PortAudioSharp.PortAudio.PaStreamFlags.paNoFlag,
                        _MyRecProc,
                        new IntPtr(dev)))
                {
                    for (var j = 0; j < _RecHandle.Length; j++)
                    {
                        if (_RecHandle[j] == IntPtr.Zero)
                        {
                            continue;
                        }

                        try
                        {
                            _PaHandle.CloseStream(_RecHandle[j]);
                        }
                        catch (Exception ex)
                        {
                            CLog.Error(ex, "Error rolling back PortAudio stream after open failure:");
                        }
                        finally
                        {
                            _RecHandle[j] = IntPtr.Zero;
                        }
                    }

                    return false;
                }

                if (_PaHandle.CheckError("Start Stream (rec)", PortAudioSharp.PortAudio.Pa_StartStream(_RecHandle[dev])))
                {
                    for (var j = 0; j < _RecHandle.Length; j++)
                    {
                        if (_RecHandle[j] == IntPtr.Zero)
                        {
                            continue;
                        }

                        try
                        {
                            _PaHandle.CloseStream(_RecHandle[j]);
                        }
                        catch (Exception ex)
                        {
                            CLog.Error(ex, "Error rolling back PortAudio stream after start failure:");
                        }
                        finally
                        {
                            _RecHandle[j] = IntPtr.Zero;
                        }
                    }

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Stop Voice Capturing
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            if (!_Initialized)
            {
                return false;
            }

            if (_RecHandle == null)
            {
                return true;
            }

            foreach (var handle in _RecHandle)
            {
                if (handle == IntPtr.Zero)
                {
                    continue;
                }

                try
                {
                    var isStoppedResult = PortAudioSharp.PortAudio.Pa_IsStreamStopped(handle);
                    if (isStoppedResult == PortAudioSharp.PortAudio.PaError.paStreamIsNotStopped)
                    {
                        var stopResult = PortAudioSharp.PortAudio.Pa_StopStream(handle);
                        if (stopResult != PortAudioSharp.PortAudio.PaError.paNoError &&
                            stopResult != PortAudioSharp.PortAudio.PaError.paStreamIsStopped)
                        {
                            CLog.Error("StopStream error: " + PortAudioSharp.PortAudio.Pa_GetErrorText(stopResult));
                        }
                    }
                }
                catch (Exception ex)
                {
                    CLog.Error(ex, "Error stopping PortAudio record stream:");
                }
            }

            return true;
        }

        /// <summary>
        ///     Stop all voice capturing streams and terminate PortAudio
        /// </summary>
        public override void Close()
        {
            if (_RecHandle != null && _RecHandle.Length > 0)
            {
                Stop();

                if (_PaHandle != null)
                {
                    for (var i = 0; i < _RecHandle.Length; i++)
                    {
                        var handle = _RecHandle[i];
                        if (handle == IntPtr.Zero)
                        {
                            continue;
                        }

                        try
                        {
                            _PaHandle.CloseStream(handle);
                        }
                        catch (Exception ex)
                        {
                            CLog.Error(ex, "Error closing PortAudio record stream:");
                        }
                        finally
                        {
                            _RecHandle[i] = IntPtr.Zero;
                        }
                    }
                }

                _RecHandle = new IntPtr[_Devices.Count];
            }

            if (_PaHandle != null)
            {
                _PaHandle.Close();
                _PaHandle = null;
            }

            _Initialized = false;

            base.Close();
        }

        private PortAudioSharp.PortAudio.PaStreamCallbackResult _MyPaStreamCallback(
            IntPtr input,
            IntPtr output,
            uint frameCount,
            ref PortAudioSharp.PortAudio.PaStreamCallbackTimeInfo timeInfo,
            PortAudioSharp.PortAudio.PaStreamCallbackFlags statusFlags,
            IntPtr userData)
        {
            try
            {
                if (frameCount > 0 && input != IntPtr.Zero)
                {
                    var dev = _Devices[userData.ToInt32()];
                    uint numBytes;
                    numBytes = frameCount * (uint)dev.Channels * 2;

                    var recbuffer = new byte[numBytes];

                    // copy from managed to unmanaged memory
                    Marshal.Copy(input, recbuffer, 0, (int)numBytes);
                    _HandleData(dev, recbuffer);
                }
            }
            catch (Exception e)
            {
                CLog.Error("Error on Stream Callback (rec): " + e);
            }

            return PortAudioSharp.PortAudio.PaStreamCallbackResult.paContinue;
        }
    }
}