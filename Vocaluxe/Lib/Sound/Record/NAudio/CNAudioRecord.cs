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

using System;
using System.Collections.Generic;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.Sound.Record.NAudio
{
    class CNAudioRecord : CRecordBase, IRecord
    {
        private const int _SampleRate = 44100;
        private const int _BitsPerSample = 16;
        private const int _BufferMilliseconds = 20;

        private bool _Initialized;
        private MMDeviceEnumerator _Enumerator;
        private readonly List<WasapiCapture> _Captures = new List<WasapiCapture>();

        public override bool Init()
        {
            if (!base.Init())
            {
                return false;
            }

            try
            {
                _Enumerator = new MMDeviceEnumerator();
                foreach (var device in _Enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
                {
                    try
                    {
                        using (device)
                        using (var client = device.AudioClient)
                        {
                            _Devices.Add(new CRecordDevice(_Devices.Count, device.FriendlyName, device.ID, client.MixFormat.Channels));
                        }
                    }
                    catch (Exception e)
                    {
                        CLog.Error(e, "Error initializing NAudio record device:");
                    }
                }

                _Initialized = true;
            }
            catch (Exception e)
            {
                CLog.Error(e, "Error initializing NAudio (WASAPI) recording:");
                Close();
                return false;
            }

            return true;
        }

        public bool Start()
        {
            if (!_Initialized)
            {
                return false;
            }

            Stop();

            foreach (var buffer in _Buffer)
            {
                buffer.Reset();
            }

            var success = true;
            foreach (var dev in _Devices)
            {
                var usingDevice = false;
                for (var ch = 0; ch < dev.Channels; ++ch)
                {
                    if (dev.PlayerChannel[ch] > 0)
                    {
                        usingDevice = true;
                    }
                }

                if (!usingDevice)
                {
                    continue;
                }

                WasapiCapture capture = null;
                try
                {
                    using (var mmDevice = _Enumerator.GetDevice(dev.Driver))
                    {
                        capture = new WasapiCapture(mmDevice, true, _BufferMilliseconds)
                        {
                            WaveFormat = new WaveFormat(_SampleRate, _BitsPerSample, dev.Channels)
                        };
                    }

                    var device = dev;
                    capture.DataAvailable += (sender, e) => _OnDataAvailable(device, e);
                    capture.RecordingStopped += (sender, e) => _OnRecordingStopped(device, e);
                    capture.StartRecording();
                    _Captures.Add(capture);
                }
                catch (Exception e)
                {
                    CLog.Error(e, "Error starting NAudio record device " + dev.Name + ":");
                    capture?.Dispose();
                    success = false;
                }
            }

            return success;
        }

        public bool Stop()
        {
            if (!_Initialized)
            {
                return false;
            }

            foreach (var capture in _Captures)
            {
                try
                {
                    capture.Dispose();
                }
                catch (Exception e)
                {
                    CLog.Error(e, "Error stopping NAudio record device:");
                }
            }

            _Captures.Clear();

            return true;
        }

        public override void Close()
        {
            Stop();
            _Initialized = false;

            if (_Enumerator != null)
            {
                _Enumerator.Dispose();
                _Enumerator = null;
            }

            base.Close();
        }

        private static void _OnRecordingStopped(CRecordDevice device, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                CLog.Error(e.Exception, "NAudio record device " + device.Name + " stopped:");
            }
        }

        private void _OnDataAvailable(CRecordDevice device, WaveInEventArgs e)
        {
            if (e.BytesRecorded <= 0)
            {
                return;
            }

            try
            {
                var recbuffer = new byte[e.BytesRecorded];
                Array.Copy(e.Buffer, recbuffer, e.BytesRecorded);
                _HandleData(device, recbuffer);
            }
            catch (Exception ex)
            {
                CLog.Error(ex, "Error on NAudio record callback:");
            }
        }
    }
}
