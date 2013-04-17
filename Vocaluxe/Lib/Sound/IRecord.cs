using System;
using System.Collections.Generic;

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

        bool Start(SRecordDevice[] deviceConfig);
        bool Stop();
        void AnalyzeBuffer(int player);

        int GetToneAbs(int player);
        int GetTone(int player);
        void SetTone(int player, int tone);
        float GetMaxVolume(int player);
        bool ToneValid(int player);
        int NumHalfTones(int player);
        float[] ToneWeigth(int player);

        SRecordDevice[] RecordDevices();
    }
}