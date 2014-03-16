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

using System.Collections.ObjectModel;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vocaluxe.Base;

namespace Vocaluxe.Lib.Sound.Record.PortAudio
{
    class CPortAudioRecord : IRecord
    {
        private bool _Initialized;
        private List<CRecordDevice> _Devices;

        private PortAudioSharp.PortAudio.PaStreamCallbackDelegate _MyRecProc;
        private IntPtr[] _RecHandle;

        private readonly CBuffer[] _Buffer;

        public CPortAudioRecord()
        {
            _Devices = null;

            _Buffer = new CBuffer[CSettings.MaxNumPlayer];
            for (int i = 0; i < _Buffer.Length; i++)
                _Buffer[i] = new CBuffer();

            Init();
        }

        /// <summary>
        ///     Init PortAudio and list record devices
        /// </summary>
        /// <returns>true if success</returns>
        public bool Init()
        {
            _Devices = new List<CRecordDevice>();

            try
            {
                if (_Initialized)
                    Close();

                if (_ErrorCheck("Initialize", PortAudioSharp.PortAudio.Pa_Initialize()))
                    return false;

                _Initialized = true;
                int hostAPI = _ApiSelect();

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
            }
            catch (Exception e)
            {
                _Initialized = false;
                CLog.LogError("Error initializing PortAudio: " + e.Message);
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

            if (_RecHandle == null || _RecHandle.Length == 0)
                return false;

            foreach (CBuffer buffer in _Buffer)
                buffer.Reset();

            foreach (IntPtr handle in _RecHandle)
            {
                int waitcount = 0;
                while (waitcount < 5 && PortAudioSharp.PortAudio.Pa_IsStreamStopped(handle) == PortAudioSharp.PortAudio.PaError.paStreamIsNotStopped)
                {
                    Thread.Sleep(1);
                    waitcount++;
                }
            }

            for (int i = 0; i < _RecHandle.Length; i++)
                _RecHandle[i] = IntPtr.Zero;

            for (int dev = 0; dev < _Devices.Count; dev++)
            {
                if (_Devices[dev].PlayerChannel1 > 0 || _Devices[dev].PlayerChannel2 > 0)
                {
                    PortAudioSharp.PortAudio.PaStreamParameters? inputParams = new PortAudioSharp.PortAudio.PaStreamParameters
                        {
                            channelCount = _Devices[dev].Channels,
                            device = _Devices[dev].ID,
                            sampleFormat = PortAudioSharp.PortAudio.PaSampleFormat.paInt16,
                            suggestedLatency = PortAudioSharp.PortAudio.Pa_GetDeviceInfo(_Devices[dev].ID).defaultLowInputLatency
                        };
                    PortAudioSharp.PortAudio.PaStreamParameters? outputParams = null;
                    if (_ErrorCheck("OpenStream (rec)", PortAudioSharp.PortAudio.Pa_OpenStream(
                        out _RecHandle[dev],
                        ref inputParams,
                        ref outputParams,
                        44100,
                        882,
                        PortAudioSharp.PortAudio.PaStreamFlags.paNoFlag,
                        _MyRecProc,
                        new IntPtr(dev))))
                        return false;

                    if (_ErrorCheck("Start Stream (rec)", PortAudioSharp.PortAudio.Pa_StartStream(_RecHandle[dev])))
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
                PortAudioSharp.PortAudio.Pa_StopStream(handle);
            return true;
        }

        /// <summary>
        ///     Stop all voice capturing streams and terminate PortAudio
        /// </summary>
        public void Close()
        {
            Stop();

            if (_Initialized)
            {
                PortAudioSharp.PortAudio.Pa_Terminate();
                _Initialized = false;
            }

            //System.IO.File.WriteAllBytes("test0.raw", _Buffer[0].Buffer);
        }

        /// <summary>
        ///     Detect Pitch and Volume of the newest voice buffer
        /// </summary>
        /// <param name="player"></param>
        public void AnalyzeBuffer(int player)
        {
            if (!_Initialized)
                return;

            _Buffer[player].AnalyzeBuffer();
        }

        public int GetToneAbs(int player)
        {
            if (!_Initialized)
                return 0;

            return _Buffer[player].ToneAbs;
        }

        public int GetTone(int player)
        {
            if (!_Initialized)
                return 0;

            return _Buffer[player].Tone;
        }

        public void SetTone(int player, int tone)
        {
            if (!_Initialized)
                return;

            _Buffer[player].Tone = tone;
        }

        public float GetMaxVolume(int player)
        {
            if (!_Initialized)
                return 0f;

            return _Buffer[player].MaxVolume;
        }

        public bool ToneValid(int player)
        {
            if (!_Initialized)
                return false;

            return _Buffer[player].ToneValid;
        }

        public int NumHalfTones()
        {
            if (!_Initialized)
                return 0;

            return CBuffer.NumHalfTones;
        }

        public float[] ToneWeigth(int player)
        {
            if (!_Initialized)
                return null;

            return _Buffer[player].ToneWeigth;
        }

        public ReadOnlyCollection<CRecordDevice> RecordDevices()
        {
            if (!_Initialized)
                return null;

            if (_Devices.Count == 0)
                return null;

            return _Devices.AsReadOnly();
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
                    if (dev.Channels == 2)
                        numBytes = frameCount * 4;
                    else
                        numBytes = frameCount * 2;

                    byte[] recbuffer = new byte[numBytes];
                    byte[] leftBuffer;
                    byte[] rightBuffer;

                    // copy from managed to unmanaged memory
                    Marshal.Copy(input, recbuffer, 0, (int)numBytes);
                    if (dev.Channels == 2)
                    {
                        leftBuffer = new byte[numBytes / 2];
                        rightBuffer = new byte[numBytes / 2];
                        //[]: Sample, L: Left channel R: Right channel
                        //[LR][LR][LR][LR][LR][LR]
                        //The data is interleaved and needs to be demultiplexed
                        for (int i = 0; i < frameCount; i++)
                        {
                            leftBuffer[i * 2] = recbuffer[i * 4];
                            leftBuffer[i * 2 + 1] = recbuffer[i * 4 + 1];
                            rightBuffer[i * 2] = recbuffer[i * 4 + 2];
                            rightBuffer[i * 2 + 1] = recbuffer[i * 4 + 3];
                        }
                    }
                    else
                        leftBuffer = rightBuffer = recbuffer;

                    if (dev.PlayerChannel1 > 0)
                        _Buffer[dev.PlayerChannel1 - 1].ProcessNewBuffer(leftBuffer);

                    if (dev.PlayerChannel2 > 0)
                        _Buffer[dev.PlayerChannel2 - 1].ProcessNewBuffer(rightBuffer);
                }
            }
            catch (Exception e)
            {
                CLog.LogError("Error on Stream Callback (rec): " + e);
            }

            return PortAudioSharp.PortAudio.PaStreamCallbackResult.paContinue;
        }

        private bool _ErrorCheck(String action, PortAudioSharp.PortAudio.PaError errorCode)
        {
            if (errorCode != PortAudioSharp.PortAudio.PaError.paNoError)
            {
                CLog.LogError(action + " error (rec): " + PortAudioSharp.PortAudio.Pa_GetErrorText(errorCode));
                if (errorCode == PortAudioSharp.PortAudio.PaError.paUnanticipatedHostError)
                {
                    PortAudioSharp.PortAudio.PaHostErrorInfo errorInfo = PortAudioSharp.PortAudio.Pa_GetLastHostErrorInfo();
                    CLog.LogError("- Host error API type: " + errorInfo.hostApiType);
                    CLog.LogError("- Host error code: " + errorInfo.errorCode);
                    CLog.LogError("- Host error text: " + errorInfo.errorText);
                }
                return true;
            }

            return false;
        }

        private int _ApiSelect()
        {
            if (!_Initialized)
                return 0;

            int selectedHostApi = PortAudioSharp.PortAudio.Pa_GetDefaultHostApi();
            int apiCount = PortAudioSharp.PortAudio.Pa_GetHostApiCount();
            for (int i = 0; i < apiCount; i++)
            {
                PortAudioSharp.PortAudio.PaHostApiInfo apiInfo = PortAudioSharp.PortAudio.Pa_GetHostApiInfo(i);
                if ((apiInfo.type == PortAudioSharp.PortAudio.PaHostApiTypeId.paDirectSound)
                    || (apiInfo.type == PortAudioSharp.PortAudio.PaHostApiTypeId.paALSA))
                    selectedHostApi = i;
            }
            return selectedHostApi;
        }
    }
}