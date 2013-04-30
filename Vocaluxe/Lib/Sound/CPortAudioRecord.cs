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

using System.Collections.ObjectModel;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vocaluxe.Base;
using Vocaluxe.Lib.Sound.PortAudio;

namespace Vocaluxe.Lib.Sound
{
    class CPortAudioRecord : IRecord
    {
        private bool _Initialized;
        private List<CRecordDevice> _Devices;

        private CPortAudio.PaStreamCallbackDelegate _MyRecProc;
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
                    CloseAll();

                if (_ErrorCheck("Initialize", CPortAudio.Pa_Initialize()))
                    return false;

                _Initialized = true;
                int hostAPI = _ApiSelect();

                int numDevices = CPortAudio.Pa_GetDeviceCount();
                for (int i = 0; i < numDevices; i++)
                {
                    CPortAudio.SPaDeviceInfo info = CPortAudio.PaGetDeviceInfo(i);
                    if (info.HostApi == hostAPI && info.MaxInputChannels > 0)
                    {
                        CRecordDevice dev = new CRecordDevice {ID = i, Name = info.Name, Driver = info.Name + i, Channels = info.MaxInputChannels};

                        if (dev.Channels > 2)
                            dev.Channels = 2; //more are not supported in vocaluxe

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
                while (waitcount < 5 && CPortAudio.Pa_IsStreamStopped(handle) == CPortAudio.EPaError.PaStreamIsNotStopped)
                {
                    Thread.Sleep(1);
                    waitcount++;
                }
            }

            for (int i = 0; i < _RecHandle.Length; i++)
                _RecHandle[i] = IntPtr.Zero;

            bool[] active = new bool[_Devices.Count];
            for (int dev = 0; dev < _Devices.Count; dev++)
            {
                active[dev] = false;
                if (_Devices[dev].PlayerChannel1 > 0 || _Devices[dev].PlayerChannel2 > 0)
                    active[dev] = true;
            }

            for (int i = 0; i < _RecHandle.Length; i++)
            {
                if (active[i])
                {
                    CPortAudio.SPaStreamParameters inputParams = new CPortAudio.SPaStreamParameters
                        {
                            ChannelCount = _Devices[i].Channels,
                            Device = _Devices[i].ID,
                            SampleFormat = CPortAudio.EPaSampleFormat.PaInt16,
                            SuggestedLatency = CPortAudio.PaGetDeviceInfo(_Devices[i].ID).DefaultLowInputLatency
                        };

                    if (_ErrorCheck("OpenStream (rec)", CPortAudio.Pa_OpenStream(
                        out _RecHandle[i],
                        ref inputParams,
                        IntPtr.Zero,
                        44100,
                        882,
                        CPortAudio.EPaStreamFlags.PaNoFlag,
                        _MyRecProc,
                        new IntPtr(i))))
                        return false;

                    if (_ErrorCheck("Start Stream (rec)", CPortAudio.Pa_StartStream(_RecHandle[i])))
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
                CPortAudio.Pa_StopStream(handle);
            return true;
        }

        /// <summary>
        ///     Stop all voice capturing streams and terminate PortAudio
        /// </summary>
        public void CloseAll()
        {
            Stop();

            if (_Initialized)
            {
                CPortAudio.Pa_Terminate();
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

        public int NumHalfTones(int player)
        {
            if (!_Initialized)
                return 0;

            return _Buffer[player].NumHalfTones;
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

        private CPortAudio.EPaStreamCallbackResult _MyPaStreamCallback(
            IntPtr input,
            IntPtr output,
            uint frameCount,
            ref CPortAudio.SPaStreamCallbackTimeInfo timeInfo,
            CPortAudio.EPaStreamCallbackFlags statusFlags,
            IntPtr userData)
        {
            try
            {
                frameCount *= 4;
                if (frameCount > 0 && input != IntPtr.Zero)
                {
                    byte[] recbuffer = new byte[frameCount];
                    byte[] leftBuffer = new byte[frameCount / 2];
                    byte[] rightBuffer = new byte[frameCount / 2];

                    // copy from managed to unmanaged memory
                    Marshal.Copy(input, recbuffer, 0, (int)frameCount);

                    // copy into left/right Buffer
                    for (int i = 0; i < frameCount / 2; i++)
                    {
                        leftBuffer[i] = recbuffer[i * 2 - (i % 2)];
                        rightBuffer[i] = recbuffer[i * 2 - (i % 2) + 2];
                    }

                    for (int i = 0; i < _RecHandle.Length; i++)
                    {
                        if (new IntPtr(i) == userData)
                        {
                            if (_Devices[i].PlayerChannel1 > 0)
                                _Buffer[_Devices[i].PlayerChannel1 - 1].ProcessNewBuffer(leftBuffer);

                            if (_Devices[i].PlayerChannel2 > 0)
                                _Buffer[_Devices[i].PlayerChannel2 - 1].ProcessNewBuffer(rightBuffer);

                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                CLog.LogError("Error on Stream Callback (rec): " + e);
            }

            return CPortAudio.EPaStreamCallbackResult.PaContinue;
        }

        private bool _ErrorCheck(String action, CPortAudio.EPaError errorCode)
        {
            if (errorCode != CPortAudio.EPaError.PaNoError)
            {
                CLog.LogError(action + " error (rec): " + CPortAudio.PaGetErrorText(errorCode));
                if (errorCode == CPortAudio.EPaError.PaUnanticipatedHostError)
                {
                    CPortAudio.SPaHostErrorInfo errorInfo = CPortAudio.PaGetLastHostErrorInfo();
                    CLog.LogError("- Host error API type: " + errorInfo.HostApiType);
                    CLog.LogError("- Host error code: " + errorInfo.ErrorCode);
                    CLog.LogError("- Host error text: " + errorInfo.ErrorText);
                }
                return true;
            }

            return false;
        }

        private int _ApiSelect()
        {
            if (!_Initialized)
                return 0;

            int selectedHostApi = CPortAudio.Pa_GetDefaultHostApi();
            int apiCount = CPortAudio.Pa_GetHostApiCount();
            for (int i = 0; i < apiCount; i++)
            {
                CPortAudio.SPaHostApiInfo apiInfo = CPortAudio.PaGetHostApiInfo(i);
                if ((apiInfo.Type == CPortAudio.EPaHostApiTypeId.PaDirectSound)
                    || (apiInfo.Type == CPortAudio.EPaHostApiTypeId.PaALSA))
                    selectedHostApi = i;
            }
            return selectedHostApi;
        }
    }
}