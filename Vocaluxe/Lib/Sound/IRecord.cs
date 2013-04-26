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

        // ReSharper disable UnusedParameter.Local
        public SMicConfig(int dummy)
            // ReSharper restore UnusedParameter.Local
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