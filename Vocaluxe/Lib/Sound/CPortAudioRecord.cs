using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using PortAudioSharp;

using Vocaluxe.Base;

namespace Vocaluxe.Lib.Sound
{
    class CPortAudioRecord : IRecord
    {
        private bool _initialized = false;
        private List<SRecordDevice> _Devices = null;
        private SRecordDevice[] _DeviceConfig;

        private PortAudio.PaStreamCallbackDelegate _myRecProc;
        private IntPtr[] _recHandle;

        private CBuffer[] _Buffer;


        public CPortAudioRecord()
        {
            _DeviceConfig = null;

            _Buffer = new CBuffer[CSettings.MaxNumPlayer];
            for (int i = 0; i < _Buffer.Length; i++)
            {
                _Buffer[i] = new CBuffer();
            }

            Init();
        }

        /// <summary>
        /// Init PortAudio and list record devices
        /// </summary>
        /// <returns>true if success</returns>
        public bool Init()
        {
            _Devices = new List<SRecordDevice>();

            try
            {
                if (_initialized)
                    CloseAll();

                if (errorCheck("Initialize", PortAudio.Pa_Initialize()))
                    return false;

                _initialized = true;
                int hostAPI = apiSelect();

                int numDevices = PortAudio.Pa_GetDeviceCount();
                for (int i = 0; i < numDevices; i++)
                {
                    PortAudio.PaDeviceInfo info = PortAudio.Pa_GetDeviceInfo(i);
                    if (info.hostApi == hostAPI && info.maxInputChannels > 0)
                    {
                        SRecordDevice dev = new SRecordDevice();

                        dev.ID = i;
                        dev.Name = info.name;
                        dev.Driver = info.name + i.ToString();
                        dev.Inputs = new List<SInput>();

                        SInput inp = new SInput();
                        inp.Name = "Default";

                        inp.Channels = info.maxInputChannels;
                        if (inp.Channels > 2)
                            inp.Channels = 2; //more are not supported in vocaluxe

                        dev.Inputs.Add(inp);
                        _Devices.Add(dev);
                    }
                }

                _recHandle = new IntPtr[_Devices.Count];
                _myRecProc = new PortAudio.PaStreamCallbackDelegate(myPaStreamCallback);

                _DeviceConfig = _Devices.ToArray();
            }
            catch (Exception e)
            {
                _initialized = false;
                CLog.LogError("Error initializing PortAudio: " + e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Start Voice Capturing
        /// </summary>
        /// <param name="DeviceConfig"></param>
        /// <returns></returns>
        public bool Start(SRecordDevice[] DeviceConfig)
        {
            if (!_initialized)
                return false;

            if (DeviceConfig == null)
                return false;

            if (_recHandle == null)
                return false;

            if (_recHandle.Length == 0)
                return false;

            for (int i = 0; i < _Buffer.Length; i++)
            {
                _Buffer[i].Reset();
            }

            for (int i = 0; i < _recHandle.Length; i++)
            {
                int waitcount = 0;
                while (waitcount < 5 && PortAudio.Pa_IsStreamStopped(_recHandle[i]) == PortAudio.PaError.paStreamIsNotStopped)
                {
                    System.Threading.Thread.Sleep(1);
                    waitcount++;
                }
            }

            for (int i = 0; i < _recHandle.Length; i++)
            {
                _recHandle[i] = IntPtr.Zero;
            }

            _DeviceConfig = DeviceConfig;
            bool[] active = new bool[DeviceConfig.Length];
            for (int dev = 0; dev < DeviceConfig.Length; dev++)
            {
                active[dev] = false;
                for (int inp = 0; inp < DeviceConfig[dev].Inputs.Count; inp++)
                {
                    if (DeviceConfig[dev].Inputs[inp].PlayerChannel1 > 0 ||
                        DeviceConfig[dev].Inputs[inp].PlayerChannel2 > 0)
                        active[dev] = true;
                }
            }

            bool result = true;
            for (int i = 0; i < _recHandle.Length; i++)
            {
                if (active[i])
                {
                    PortAudio.PaStreamParameters inputParams = new PortAudio.PaStreamParameters();
                    inputParams.channelCount = _DeviceConfig[i].Inputs[0].Channels;
                    inputParams.device = _DeviceConfig[i].ID;
                    inputParams.sampleFormat = PortAudio.PaSampleFormat.paInt16;
                    inputParams.suggestedLatency = PortAudio.Pa_GetDeviceInfo(_DeviceConfig[i].ID).defaultLowInputLatency;

                    if (errorCheck("OpenStream (rec)", PortAudio.Pa_OpenStream(
                        out _recHandle[i],
                        ref inputParams,
                        IntPtr.Zero,
                        44100,
                        882,
                        PortAudio.PaStreamFlags.paNoFlag,
                        _myRecProc,
                        new IntPtr(i))))
                        return false;

                    if (errorCheck("Start Stream (rec)", PortAudio.Pa_StartStream(_recHandle[i])))
                        return false;
                }
            }
            return result;
        }

        /// <summary>
        /// Stop Voice Capturing
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            if (!_initialized)
                return false;

            for (int i = 0; i < _recHandle.Length; i++)
            {
                PortAudio.Pa_StopStream(_recHandle[i]);
            }
            return true;
        }

        /// <summary>
        /// Stop all voice capturing streams and terminate PortAudio
        /// </summary>
        public void CloseAll()
        {
            Stop();

            if (_initialized)
            {
                PortAudio.Pa_Terminate();
                _initialized = false;
            }

            //System.IO.File.WriteAllBytes("test0.raw", _Buffer[0].Buffer);
        }

        /// <summary>
        /// Detect Pitch and Volume of the newest voice buffer
        /// </summary>
        /// <param name="Player"></param>
        public void AnalyzeBuffer(int Player)
        {
            if (!_initialized)
                return;

            _Buffer[Player].AnalyzeBuffer();
        }

        public int GetToneAbs(int Player)
        {
            if (!_initialized)
                return 0;

            return _Buffer[Player].ToneAbs;
        }

        public int GetTone(int Player)
        {
            if (!_initialized)
                return 0;

            return _Buffer[Player].Tone;
        }

        public void SetTone(int Player, int Tone)
        {
            if (!_initialized)
                return;

            _Buffer[Player].Tone = Tone;
        }

        public float GetMaxVolume(int Player)
        {
            if (!_initialized)
                return 0f;

            return _Buffer[Player].MaxVolume;
        }

        public bool ToneValid(int Player)
        {
            if (!_initialized)
                return false;

            return _Buffer[Player].ToneValid;
        }

        public int NumHalfTones(int Player)
        {
            if (!_initialized)
                return 0;

            return _Buffer[Player].NumHalfTones;
        }

        public float[] ToneWeigth(int Player)
        {
            if (!_initialized)
                return null;

            return _Buffer[Player].ToneWeigth;
        }

        public SRecordDevice[] RecordDevices()
        {
            if (!_initialized)
                return null;

            if (_Devices.Count == 0)
                return null;

            return _Devices.ToArray();
        }

        public PortAudio.PaStreamCallbackResult myPaStreamCallback(
            IntPtr input,
            IntPtr output,
            uint frameCount,
            ref PortAudio.PaStreamCallbackTimeInfo timeInfo,
            PortAudio.PaStreamCallbackFlags statusFlags,
            IntPtr userData)
        {
            try
            {
                frameCount *= 4;
                if (frameCount > 0 && input != IntPtr.Zero)
                {
                    byte[] _recbuffer = new byte[frameCount];
                    byte[] _leftBuffer = new byte[frameCount / 2];
                    byte[] _rightBuffer = new byte[frameCount / 2];

                    // copy from managed to unmanaged memory
                    Marshal.Copy(input, _recbuffer, 0, (int)frameCount);

                    // copy into left/right Buffer
                    for (int i = 0; i < frameCount / 2; i++)
                    {
                        _leftBuffer[i] = _recbuffer[i * 2 - (i % 2)];
                        _rightBuffer[i] = _recbuffer[i * 2 - (i % 2) + 2];
                    }

                    for (int i = 0; i < _recHandle.Length; i++)
                    {
                        if (new IntPtr(i) == userData)
                        {
                            if (_DeviceConfig[i].Inputs[0].PlayerChannel1 > 0)
                                _Buffer[_DeviceConfig[i].Inputs[0].PlayerChannel1 - 1].ProcessNewBuffer(_leftBuffer);

                            if (_DeviceConfig[i].Inputs[0].PlayerChannel2 > 0)
                                _Buffer[_DeviceConfig[i].Inputs[0].PlayerChannel2 - 1].ProcessNewBuffer(_rightBuffer);

                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                CLog.LogError("Error on Stream Callback (rec): " + e.ToString());
            }

            return PortAudio.PaStreamCallbackResult.paContinue;
        }

        private bool errorCheck(String action, PortAudio.PaError errorCode)
        {
            if (errorCode != PortAudio.PaError.paNoError)
            {
                CLog.LogError(action + " error (rec): " + PortAudio.Pa_GetErrorText(errorCode));
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

        private int apiSelect()
        {
            if (!_initialized)
                return 0;

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
