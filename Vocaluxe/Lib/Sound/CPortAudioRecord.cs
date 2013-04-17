using System.Threading;
using PortAudioSharp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vocaluxe.Base;

namespace Vocaluxe.Lib.Sound
{
    class CPortAudioRecord : IRecord
    {
        private bool _Initialized;
        private List<SRecordDevice> _Devices;
        private SRecordDevice[] _DeviceConfig;

        private CPortAudio.PaStreamCallbackDelegate _MyRecProc;
        private IntPtr[] _RecHandle;

        private readonly CBuffer[] _Buffer;

        public CPortAudioRecord()
        {
            _DeviceConfig = null;

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
            _Devices = new List<SRecordDevice>();

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
                        SRecordDevice dev = new SRecordDevice();

                        dev.ID = i;
                        dev.Name = info.Name;
                        dev.Driver = info.Name + i.ToString();
                        dev.Inputs = new List<SInput>();

                        SInput inp = new SInput();
                        inp.Name = "Default";

                        inp.Channels = info.MaxInputChannels;
                        if (inp.Channels > 2)
                            inp.Channels = 2; //more are not supported in vocaluxe

                        dev.Inputs.Add(inp);
                        _Devices.Add(dev);
                    }
                }

                _RecHandle = new IntPtr[_Devices.Count];
                _MyRecProc = MyPaStreamCallback;

                _DeviceConfig = _Devices.ToArray();
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
        /// <param name="deviceConfig"></param>
        /// <returns></returns>
        public bool Start(SRecordDevice[] deviceConfig)
        {
            if (!_Initialized)
                return false;

            if (deviceConfig == null)
                return false;

            if (_RecHandle == null)
                return false;

            if (_RecHandle.Length == 0)
                return false;

            for (int i = 0; i < _Buffer.Length; i++)
                _Buffer[i].Reset();

            for (int i = 0; i < _RecHandle.Length; i++)
            {
                int waitcount = 0;
                while (waitcount < 5 && CPortAudio.Pa_IsStreamStopped(_RecHandle[i]) == CPortAudio.EPaError.PaStreamIsNotStopped)
                {
                    Thread.Sleep(1);
                    waitcount++;
                }
            }

            for (int i = 0; i < _RecHandle.Length; i++)
                _RecHandle[i] = IntPtr.Zero;

            _DeviceConfig = deviceConfig;
            bool[] active = new bool[deviceConfig.Length];
            for (int dev = 0; dev < deviceConfig.Length; dev++)
            {
                active[dev] = false;
                for (int inp = 0; inp < deviceConfig[dev].Inputs.Count; inp++)
                {
                    if (deviceConfig[dev].Inputs[inp].PlayerChannel1 > 0 ||
                        deviceConfig[dev].Inputs[inp].PlayerChannel2 > 0)
                        active[dev] = true;
                }
            }

            bool result = true;
            for (int i = 0; i < _RecHandle.Length; i++)
            {
                if (active[i])
                {
                    CPortAudio.SPaStreamParameters inputParams = new CPortAudio.SPaStreamParameters();
                    inputParams.ChannelCount = _DeviceConfig[i].Inputs[0].Channels;
                    inputParams.Device = _DeviceConfig[i].ID;
                    inputParams.SampleFormat = CPortAudio.EPaSampleFormat.PaInt16;
                    inputParams.SuggestedLatency = CPortAudio.PaGetDeviceInfo(_DeviceConfig[i].ID).DefaultLowInputLatency;

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
            return result;
        }

        /// <summary>
        ///     Stop Voice Capturing
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            if (!_Initialized)
                return false;

            for (int i = 0; i < _RecHandle.Length; i++)
                CPortAudio.Pa_StopStream(_RecHandle[i]);
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

        public SRecordDevice[] RecordDevices()
        {
            if (!_Initialized)
                return null;

            if (_Devices.Count == 0)
                return null;

            return _Devices.ToArray();
        }

        public CPortAudio.EPaStreamCallbackResult MyPaStreamCallback(
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
                            if (_DeviceConfig[i].Inputs[0].PlayerChannel1 > 0)
                                _Buffer[_DeviceConfig[i].Inputs[0].PlayerChannel1 - 1].ProcessNewBuffer(leftBuffer);

                            if (_DeviceConfig[i].Inputs[0].PlayerChannel2 > 0)
                                _Buffer[_DeviceConfig[i].Inputs[0].PlayerChannel2 - 1].ProcessNewBuffer(rightBuffer);

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