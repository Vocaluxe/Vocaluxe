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
using System.Linq;
using SlimDX.DirectSound;

namespace Vocaluxe.Lib.Sound.Record.DirectSound
{
    class CDirectSoundRecord : CRecordBase, IRecord
    {
        private bool _Initialized;

        private List<CSoundCardSource> _Sources;

        public override bool Init()
        {
            if (!base.Init())
                return false;

            _Sources = new List<CSoundCardSource>();

            DeviceCollection devices = DirectSoundCapture.GetDevices();

            foreach (DeviceInformation dev in devices)
            {
                using (var ds = new DirectSoundCapture(dev.DriverGuid))
                {
                    var device = new CRecordDevice(_Devices.Count, dev.DriverGuid.ToString(), dev.Description, ds.Capabilities.Channels);

                    _Devices.Add(device);
                }
            }

            _Initialized = true;

            return true;
        }

        public override void Close()
        {
            Stop();
            _Initialized = false;
            base.Close();
        }

        public bool Start()
        {
            if (!_Initialized)
                return false;

            foreach (CBuffer buffer in _Buffer)
                buffer.Reset();

            foreach (CRecordDevice device in _Devices)
            {
                bool usingDevice = false;
                for (int ch = 0; ch < device.Channels; ++ch)
                {
                    if (device.PlayerChannel[ch] > 0)
                        usingDevice = true;
                }
                if (usingDevice)
                {
                    var source = new CSoundCardSource(device.Driver, (short)device.Channels) { SampleRateKhz = 44.1 };
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

        private void _OnDataReady(object sender, CSampleDataEventArgs e)
        {
            if (!_Initialized)
                return;
            CRecordDevice dev = _Devices.FirstOrDefault(device => device.Driver == e.Guid);
            if (dev == null)
                return;
            _HandleData(dev, e.Data);
        }
    }
}