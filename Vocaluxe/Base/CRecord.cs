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
using Vocaluxe.Lib.Sound.Record;
#if WIN
using Vocaluxe.Lib.Sound.Record.DirectSound;
#endif
using Vocaluxe.Lib.Sound.Record.PortAudio;
using VocaluxeLib;

namespace Vocaluxe.Base
{
    static class CRecord
    {
        private static IRecord _Record;

        public static bool Init()
        {
            if (_Record != null)
                return false;
            switch (CConfig.Config.Sound.RecordLib)
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
            return _Record.Init();
        }

        public static void Close()
        {
            if (_Record != null)
            {
                _Record.Close();
                _Record = null;
            }
        }

        public static bool Start()
        {
            return _Record.Start();
        }

        public static bool Stop()
        {
            return _Record.Stop();
        }

        public static void AnalyzeBuffer(int player)
        {
            _Record.AnalyzeBuffer(player);
        }

        public static int GetToneAbs(int player)
        {
            return _Record.GetToneAbs(player);
        }

        public static int GetTone(int player)
        {
            return _Record.GetTone(player);
        }

        public static void SetTone(int player, int tone)
        {
            _Record.SetTone(player, tone);
        }

        public static bool ToneValid(int player)
        {
            return _Record.ToneValid(player);
        }

        public static float GetMaxVolume(int player)
        {
            return _Record.GetMaxVolume(player);
        }

        public static float GetVolumeThreshold(int player)
        {
            return _Record.GetVolumeThreshold(player);
        }

        public static void SetVolumeThreshold(int player, float threshold)
        {
            _Record.SetVolumeThreshold(player, threshold);
        }

        public static int NumHalfTones(int player)
        {
            return _Record.NumHalfTones();
        }

        public static float[] ToneWeigth(int player)
        {
            return _Record.ToneWeigth(player);
        }

        public static ReadOnlyCollection<CRecordDevice> GetDevices()
        {
            ReadOnlyCollection<CRecordDevice> devices = _Record.RecordDevices();

            if (devices != null)
            {
                foreach (CRecordDevice device in devices)
                {
                    for(int ch = 0; ch < device.Channels; ++ch)
                        device.PlayerChannel[ch] = _GetPlayerFromMicConfig(device.Name, device.Driver, ch+1);
                }
                return devices;
            }

            return null;
        }

        private static int _GetPlayerFromMicConfig(string device, string devicedriver, int channel)
        {
            for (int p = 0; p < CSettings.MaxNumPlayer; p++)
            {
                if (CConfig.Config.Record.MicConfig[p].DeviceName == device &&
                    CConfig.Config.Record.MicConfig[p].DeviceDriver == devicedriver &&
                    CConfig.Config.Record.MicConfig[p].Channel == channel)
                    return p + 1;
            }
            return 0;
        }
    }
}