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
using Vocaluxe.Base;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.Sound.Record.PortAudio
{
    class CPortAudioRecord : CRecordBase, IRecord
    {
        private bool _Initialized;
        private CPortAudioHandle _PaHandle;
        private PortAudioSharp.Stream[] _RecHandle;
        // Kept alive for the lifetime of the streams so the native side does not call collected delegates.
        private PortAudioSharp.Stream.Callback[] _RecCallbacks;

        /// <summary>
        ///     Init PortAudio and list record devices
        /// </summary>
        /// <returns>true if success</returns>
        public override bool Init()
        {
            if (!base.Init())
                return false;

            try
            {
                _PaHandle = new CPortAudioHandle();

                // PortAudioSharp2 exposes no host-API selection; take every input-capable device.
                int numDevices = PortAudioSharp.PortAudio.DeviceCount;
                for (int i = 0; i < numDevices; i++)
                {
                    PortAudioSharp.DeviceInfo info = PortAudioSharp.PortAudio.GetDeviceInfo(i);
                    if (info.maxInputChannels > 0)
                    {
                        var dev = new CRecordDevice(i, info.name, info.name + i, info.maxInputChannels);
                        _Devices.Add(dev);
                    }
                }

                _RecHandle = new PortAudioSharp.Stream[_Devices.Count];
                _RecCallbacks = new PortAudioSharp.Stream.Callback[_Devices.Count];
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
        public bool Start()
        {
            if (!_Initialized)
                return false;

            Stop();
            _CloseAllStreams();

            foreach (CBuffer buffer in _Buffer)
                buffer.Reset();

            for (int dev = 0; dev < _Devices.Count; dev++)
            {
                bool usingDevice = false;
                for (int ch = 0; ch < _Devices[dev].Channels; ++ch)
                {
                    if (_Devices[dev].PlayerChannel[ch] > 0)
                        usingDevice = true;
                }

                if (!usingDevice)
                    continue;

                var inputParams = new PortAudioSharp.StreamParameters
                {
                    channelCount = _Devices[dev].Channels,
                    device = _Devices[dev].ID,
                    sampleFormat = PortAudioSharp.SampleFormat.Int16,
                    suggestedLatency = PortAudioSharp.PortAudio.GetDeviceInfo(_Devices[dev].ID).defaultLowInputLatency,
                    hostApiSpecificStreamInfo = IntPtr.Zero
                };

                // Per-device callback closure replaces the old shared-callback + userData(device index) scheme.
                int devIndex = dev;
                _RecCallbacks[dev] = (IntPtr input, IntPtr output, uint frameCount,
                                      ref PortAudioSharp.StreamCallbackTimeInfo timeInfo,
                                      PortAudioSharp.StreamCallbackFlags statusFlags, IntPtr userData)
                    => _ProcessRecordData(devIndex, input, frameCount);

                _RecHandle[dev] = _PaHandle.OpenInputStream(inputParams, 44100, 882, PortAudioSharp.StreamFlags.NoFlag, _RecCallbacks[dev]);
                if (_RecHandle[dev] == null)
                {
                    _CloseAllStreams();
                    return false;
                }

                try
                {
                    _RecHandle[dev].Start();
                }
                catch (Exception ex)
                {
                    CLog.Error(ex, "Start Stream (rec) failed:");
                    _CloseAllStreams();
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Stop Voice Capturing
        /// </summary>
        public bool Stop()
        {
            if (!_Initialized)
                return false;

            if (_RecHandle == null)
                return true;

            foreach (PortAudioSharp.Stream handle in _RecHandle)
            {
                if (handle == null)
                    continue;

                try
                {
                    if (!handle.IsStopped)
                        handle.Stop();
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
                _CloseAllStreams();
                _RecHandle = new PortAudioSharp.Stream[_Devices.Count];
            }

            if (_PaHandle != null)
            {
                _PaHandle.Close();
                _PaHandle = null;
            }

            _Initialized = false;

            base.Close();
        }

        private void _CloseAllStreams()
        {
            if (_RecHandle == null || _PaHandle == null)
                return;

            for (int i = 0; i < _RecHandle.Length; i++)
            {
                if (_RecHandle[i] == null)
                    continue;

                try
                {
                    _PaHandle.CloseStream(_RecHandle[i]);
                }
                catch (Exception ex)
                {
                    CLog.Error(ex, "Error closing PortAudio record stream:");
                }
                finally
                {
                    _RecHandle[i] = null;
                }
            }
        }

        private PortAudioSharp.StreamCallbackResult _ProcessRecordData(int devIndex, IntPtr input, uint frameCount)
        {
            try
            {
                if (frameCount > 0 && input != IntPtr.Zero)
                {
                    CRecordDevice dev = _Devices[devIndex];
                    uint numBytes = frameCount * (uint)dev.Channels * 2;

                    byte[] recbuffer = new byte[numBytes];

                    // copy from unmanaged to managed memory
                    Marshal.Copy(input, recbuffer, 0, (int)numBytes);
                    _HandleData(dev, recbuffer);
                }
            }
            catch (Exception e)
            {
                CLog.Error("Error on Stream Callback (rec): " + e);
            }

            return PortAudioSharp.StreamCallbackResult.Continue;
        }
    }
}
