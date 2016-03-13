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
using Vocaluxe.Base;

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
                return false;

            try
            {
                _PaHandle = new CPortAudioHandle();

                int hostAPI = _PaHandle.GetHostApi();
                int numDevices = PortAudioSharp.PortAudio.Pa_GetDeviceCount();
                for (int i = 0; i < numDevices; i++)
                {
                    PortAudioSharp.PortAudio.PaDeviceInfo info = PortAudioSharp.PortAudio.Pa_GetDeviceInfo(i);
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
                CLog.LogError("Error initializing PortAudio: " + e.Message);
                Close();
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Start Voice Capturing
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            if (!_Initialized)
                return false;

            Stop();

            foreach (IntPtr handle in _RecHandle)
            {
                int waitcount = 0;
                while (waitcount < 5 && PortAudioSharp.PortAudio.Pa_IsStreamStopped(handle) == PortAudioSharp.PortAudio.PaError.paStreamIsNotStopped)
                {
                    Thread.Sleep(1);
                    waitcount++;
                }
            }

            foreach (CBuffer buffer in _Buffer)
                buffer.Reset();

            for (int i = 0; i < _RecHandle.Length; i++)
                _RecHandle[i] = IntPtr.Zero;

            for (int dev = 0; dev < _Devices.Count; dev++)
            {
                bool usingDevice = false;
                for (int ch = 0; ch < _Devices[dev].Channels; ++ch) {
                    if (_Devices[dev].PlayerChannel[ch] > 0)
                        usingDevice = true;
                }
                if (usingDevice)
                {
                    PortAudioSharp.PortAudio.PaStreamParameters? inputParams = new PortAudioSharp.PortAudio.PaStreamParameters
                    {
                        channelCount = _Devices[dev].Channels,
                        device = _Devices[dev].ID,
                        sampleFormat = PortAudioSharp.PortAudio.PaSampleFormat.paInt16,
                        suggestedLatency = PortAudioSharp.PortAudio.Pa_GetDeviceInfo(_Devices[dev].ID).defaultLowInputLatency,
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
                        return false;

                    if (_PaHandle.CheckError("Start Stream (rec)", PortAudioSharp.PortAudio.Pa_StartStream(_RecHandle[dev])))
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
                return false;

            foreach (IntPtr handle in _RecHandle)
            {
                PortAudioSharp.PortAudio.Pa_StopStream(handle);
                PortAudioSharp.PortAudio.Pa_CloseStream(handle);
            }
            return true;
        }

        /// <summary>
        ///     Stop all voice capturing streams and terminate PortAudio
        /// </summary>
        public override void Close()
        {
            if (_RecHandle != null)
            {
                foreach (IntPtr handle in _RecHandle)
                {
                    if (handle != IntPtr.Zero)
                        _PaHandle.CloseStream(handle);
                }
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
                    CRecordDevice dev = _Devices[userData.ToInt32()];
                    uint numBytes;
                    numBytes = frameCount * (uint)dev.Channels * 2;

                    byte[] recbuffer = new byte[numBytes];

                    // copy from managed to unmanaged memory
                    Marshal.Copy(input, recbuffer, 0, (int)numBytes);
                    _HandleData(dev, recbuffer);
                }
            }
            catch (Exception e)
            {
                CLog.LogError("Error on Stream Callback (rec): " + e);
            }

            return PortAudioSharp.PortAudio.PaStreamCallbackResult.paContinue;
        }
    }
}