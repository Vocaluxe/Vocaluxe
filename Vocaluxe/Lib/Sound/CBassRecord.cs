using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using Un4seen.Bass;

using Vocaluxe.Base;

namespace Vocaluxe.Lib.Sound
{
    class CBassRecord: IRecord
    {
        private List<SRecordDevice> _Devices = null;
        private SRecordDevice[] _DeviceConfig;

        private RECORDPROC _myRecProc;
        private int[] _recHandle;
        
        private CBuffer[] _Buffer;


        public CBassRecord()
        {
            Init();
            _Buffer = new CBuffer[CSettings.MaxNumPlayer];
            for (int i = 0; i < _Buffer.Length; i++)
            {
                _Buffer[i] = new CBuffer();
            }

            _recHandle = new int[_Devices.Count];
            _myRecProc = new RECORDPROC(MyRecording);

            _DeviceConfig = _Devices.ToArray();
        }

        public bool Init()
        {
            _Devices = new List<SRecordDevice>();
            
            try
            {
                BASS_DEVICEINFO info = new BASS_DEVICEINFO();
                for (int n = 0; Bass.BASS_RecordGetDeviceInfo(n, info); n++)
                {
                    if (info.IsEnabled)
                    {
                        SRecordDevice dev = new SRecordDevice();

                        dev.ID = n;
                        dev.Name = info.name;
                        dev.Driver = info.driver;
                        dev.Inputs = new List<SInput>();

                        if (Bass.BASS_RecordInit(n))
                        {
                            string name = String.Empty;
                            for (int j = 0; ((name = Bass.BASS_RecordGetInputName(j)) != null); j++)
                            {
                                SInput inp = new SInput();
                                inp.Name = name;

                                inp.Channels = 2; //TODO: how to retrieve the amount of channels?

                                dev.Inputs.Add(inp);
                            }

                            _Devices.Add(dev);
                            Bass.BASS_RecordFree();
                        }
                    }
                }
            }
            catch (Exception)
            {
                
                //throw;
            }
            
            return true;
        }

        public bool Start(SRecordDevice[] DeviceConfig)
        {
            for (int i = 0; i < _Buffer.Length; i++)
            {
                _Buffer[i].Reset();
            }

            for (int i = 0; i < _recHandle.Length; i++)
            {
                _recHandle[i] = -1;
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
                    if (Bass.BASS_RecordInit(i))
                    {
                        _recHandle[i] = Bass.BASS_RecordStart(44100, 2, BASSFlag.BASS_RECORD_PAUSE, 20, _myRecProc, IntPtr.Zero);

                        // start recording
                        result |= Bass.BASS_ChannelPlay(_recHandle[i], false);
                    }
                }
            }
            
            return result;
        }

        public bool Stop()
        {
            for (int i = 0; i < _recHandle.Length; i++)
            {
                Bass.BASS_ChannelStop(_recHandle[i]);
            }
            Bass.BASS_RecordFree();

            //System.IO.File.Delete("test0.raw");
            //System.IO.File.Delete("test1.raw");
            //System.IO.File.WriteAllBytes("test0.raw", _Buffer[0].Buffer);
            //System.IO.File.WriteAllBytes("test1.raw", _Buffer[1].Buffer);
            return true;
        }

        public void CloseAll()
        {
            Stop();
        }

        public void AnalyzeBuffer(int Player)
        {
            _Buffer[Player].AnalyzeBuffer();
        }

        public int GetToneAbs(int Player)
        {
            return _Buffer[Player].ToneAbs;
        }

        public int GetTone(int Player)
        {
            return _Buffer[Player].Tone;
        }

        public void SetTone(int Player, int Tone)
        {
            _Buffer[Player].Tone = Tone;
        }

        public float GetMaxVolume(int Player)
        {
            return _Buffer[Player].MaxVolume;
        }

        public bool ToneValid(int Player)
        {
            return _Buffer[Player].ToneValid;
        }

        public SRecordDevice[] RecordDevices()
        {
            return _Devices.ToArray();
        }

                    
        private bool MyRecording(int handle, IntPtr buffer, int length, IntPtr user)
        {
            bool cont = true;
            if (length > 0 && buffer != IntPtr.Zero)
            {
                byte[] _recbuffer = new byte[length];
                byte[] _leftBuffer = new byte[length / 2];
                byte[] _rightBuffer = new byte[length / 2];

                // copy from managed to unmanaged memory
                Marshal.Copy(buffer, _recbuffer, 0, length);

                // copy into left/right Buffer
                for (int i = 0; i < length / 2; i++)
                {
                    _leftBuffer[i] = _recbuffer[i * 2 - (i % 2)];
                    _rightBuffer[i] = _recbuffer[i * 2 - (i % 2) + 2];
                }

                for (int i = 0; i < _recHandle.Length; i++)
                {
                    if (_recHandle[i] == handle)
                    {
                        if (_DeviceConfig[i].Inputs[0].PlayerChannel1 > 0)
                            _Buffer[_DeviceConfig[i].Inputs[0].PlayerChannel1 - 1].ProcessNewBuffer(_leftBuffer);

                        if (_DeviceConfig[i].Inputs[0].PlayerChannel2 > 0)
                            _Buffer[_DeviceConfig[i].Inputs[0].PlayerChannel2 - 1].ProcessNewBuffer(_rightBuffer);
                        
                        break;
                    }
                }
            }
            return cont;
        }
    }
}
