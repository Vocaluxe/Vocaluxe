using System;
using System.Collections.Generic;
using System.Text;

namespace Vocaluxe.Lib.Sound
{
    struct SRecordDevice
    {
        public int ID;
        public string Name;
        public string Driver;
        public List<SInput> Inputs;
    }

    struct SInput
    {
        public string Name;
        public int Channels;

        public int PlayerChannel1;
        public int PlayerChannel2;
    }

    struct SMicConfig
    {
        public string DeviceName;
        public string DeviceDriver;
        public string InputName;
        public int Channel;

        public SMicConfig(int dummy)
        {
            DeviceName = String.Empty;
            DeviceDriver = String.Empty;
            InputName = String.Empty;
            Channel = 0;
        }
    }

    interface IRecord
    {
        bool Init();
        void CloseAll();

        bool Start(SRecordDevice[] DeviceConfig);
        bool Stop();
        void AnalyzeBuffer(int Player);

        int GetToneAbs(int Player);
        int GetTone(int Player);
        void SetTone(int Player, int Tone);
        float GetMaxVolume(int Player);
        bool ToneValid(int Player);

        SRecordDevice[] RecordDevices();
    }
}
