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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SlimDX.DirectSound;
using Vocaluxe.Base;

namespace Vocaluxe.Lib.Sound.Record.DirectSound
{
    class CDirectSoundRecord : IRecord
    {
        private bool _Initialized;
        private List<CRecordDevice> _Devices;
        private List<CSoundCardSource> _Sources;

        private readonly CBuffer[] _Buffer;

        public CDirectSoundRecord()
        {
            _Buffer = new CBuffer[CSettings.MaxNumPlayer];
            for (int i = 0; i < _Buffer.Length; i++)
                _Buffer[i] = new CBuffer();

            Init();
        }

        public bool Init()
        {
            DeviceCollection devices = DirectSoundCapture.GetDevices();
            _Devices = new List<CRecordDevice>();
            _Sources = new List<CSoundCardSource>();

            int id = 0;
            foreach (DeviceInformation dev in devices)
            {
                using (var ds = new DirectSoundCapture(dev.DriverGuid))
                {
                    var device = new CRecordDevice(id, dev.DriverGuid.ToString(), dev.Description, ds.Capabilities.Channels);

                    _Devices.Add(device);

                    id++;
                }
            }

            _Initialized = true;

            return true;
        }

        public void Close()
        {
            if (_Initialized)
            {
                Stop();
                _Initialized = false;
            }
            //System.IO.File.WriteAllBytes("test0.raw", _Buffer[0].Buffer);
        }

        public bool Start()
        {
            if (!_Initialized)
                return false;

            foreach (CBuffer buffer in _Buffer)
                buffer.Reset();

            foreach (CRecordDevice device in _Devices)
            {
                if (device.PlayerChannel1 > 0 || device.PlayerChannel2 > 0)
                {
                    var source = new CSoundCardSource(device.Driver, (short)device.Channels) {SampleRateKhz = 44.1};
                    source.SampleDataReady += _OnDataReady;
                    source.Start();

                    _Sources.Add(source);
                }
            }

            return true;
        }

        public bool Stop()
        {
            if (!_Initialized)
                return false;

            foreach (CSoundCardSource source in _Sources)
            {
                source.Stop();
                source.Dispose();
            }
            _Sources.Clear();

            return true;
        }

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

        public ReadOnlyCollection<CRecordDevice> RecordDevices()
        {
            if (!_Initialized)
                return null;

            if (_Devices.Count == 0)
                return null;

            return _Devices.AsReadOnly();
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

        private void _OnDataReady(object sender, CSampleDataEventArgs e)
        {
            if (!_Initialized)
                return;
            CRecordDevice dev = _Devices.FirstOrDefault(device => device.Driver == e.Guid);
            if (dev == null)
                return;
            byte[] leftBuffer;
            byte[] rightBuffer;

            if (dev.Channels == 2)
            {
                leftBuffer = new byte[e.Data.Length / 2];
                rightBuffer = new byte[e.Data.Length / 2];
                //[]: Sample, L: Left channel R: Right channel
                //[LR][LR][LR][LR][LR][LR]
                //The data is interleaved and needs to be demultiplexed
                for (int i = 0; i < e.Data.Length / 4; i++)
                {
                    leftBuffer[i * 2] = e.Data[i * 4];
                    leftBuffer[i * 2 + 1] = e.Data[i * 4 + 1];
                    rightBuffer[i * 2] = e.Data[i * 4 + 2];
                    rightBuffer[i * 2 + 1] = e.Data[i * 4 + 3];
                }
            }
            else
                leftBuffer = rightBuffer = e.Data;

            if (dev.PlayerChannel1 > 0)
                _Buffer[dev.PlayerChannel1 - 1].ProcessNewBuffer(leftBuffer);

            if (dev.PlayerChannel2 > 0)
                _Buffer[dev.PlayerChannel2 - 1].ProcessNewBuffer(rightBuffer);
        }
    }
}