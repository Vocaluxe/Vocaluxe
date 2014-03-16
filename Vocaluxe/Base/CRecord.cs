using System.Collections.ObjectModel;
using Vocaluxe.Lib.Sound.Record;
using Vocaluxe.Lib.Sound.Record.DirectSound;
using Vocaluxe.Lib.Sound.Record.PortAudio;
using VocaluxeLib;

namespace Vocaluxe.Base
{
    class CRecord
    {
        private static IRecord _Record;

        public static void RecordInit()
        {
            switch (CConfig.RecordLib)
            {
#if WIN
                case ERecordLib.DirectSound:
                    _Record = new CDirectSoundRecord();
                    break;
#endif

                    // case ERecordLib.PortAudio:
                default:
                    _Record = new CPortAudioRecord();
                    break;
            }
        }

        public static void RecordCloseAll()
        {
            _Record.CloseAll();
        }

        public static bool RecordStart()
        {
            return _Record.Start();
        }

        public static bool RecordStop()
        {
            return _Record.Stop();
        }

        public static void AnalyzeBuffer(int player)
        {
            _Record.AnalyzeBuffer(player);
        }

        public static int RecordGetToneAbs(int player)
        {
            return _Record.GetToneAbs(player);
        }

        public static int RecordGetTone(int player)
        {
            return _Record.GetTone(player);
        }

        public static void RecordSetTone(int player, int tone)
        {
            _Record.SetTone(player, tone);
        }

        public static bool RecordToneValid(int player)
        {
            return _Record.ToneValid(player);
        }

        public static float RecordGetMaxVolume(int player)
        {
            return _Record.GetMaxVolume(player);
        }

        public static int NumHalfTones(int player)
        {
            return _Record.NumHalfTones(player);
        }

        public static float[] ToneWeigth(int player)
        {
            return _Record.ToneWeigth(player);
        }

        public static ReadOnlyCollection<CRecordDevice> RecordGetDevices()
        {
            ReadOnlyCollection<CRecordDevice> devices = _Record.RecordDevices();

            if (devices != null)
            {
                foreach (CRecordDevice device in devices)
                {
                    device.PlayerChannel1 = _GetPlayerFromMicConfig(device.Name, device.Driver, 1);
                    device.PlayerChannel2 = _GetPlayerFromMicConfig(device.Name, device.Driver, 2);
                }
                return devices;
            }

            return null;
        }

        private static int _GetPlayerFromMicConfig(string device, string devicedriver, int channel)
        {
            for (int p = 0; p < CSettings.MaxNumPlayer; p++)
            {
                if (CConfig.MicConfig[p].Channel != 0 &&
                    CConfig.MicConfig[p].DeviceName == device &&
                    CConfig.MicConfig[p].DeviceDriver == devicedriver &&
                    CConfig.MicConfig[p].Channel == channel)
                    return p + 1;
            }
            return 0;
        }
    }
}