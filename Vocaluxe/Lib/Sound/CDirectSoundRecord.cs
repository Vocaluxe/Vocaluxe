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

using System.Collections.ObjectModel;
using SlimDX.DirectSound;
using SlimDX.Multimedia;
using System;
using System.Collections.Generic;
using System.Threading;
using Vocaluxe.Base;

namespace Vocaluxe.Lib.Sound
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
                using (DirectSoundCapture ds = new DirectSoundCapture(dev.DriverGuid))
                {
                    CRecordDevice device = new CRecordDevice {Driver = dev.DriverGuid.ToString(), ID = id, Name = dev.Description, Channels = ds.Capabilities.Channels};

                    if (device.Channels > 2)
                        device.Channels = 2; //more are not supported in vocaluxe

                    _Devices.Add(device);

                    id++;
                }
            }

            _Initialized = true;

            return true;
        }

        public void CloseAll()
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

            bool[] active = new bool[_Devices.Count];
            Guid[] guid = new Guid[_Devices.Count];
            short[] channels = new short[_Devices.Count];
            for (int dev = 0; dev < _Devices.Count; dev++)
            {
                active[dev] = false;
                if (_Devices[dev].PlayerChannel1 > 0 || _Devices[dev].PlayerChannel2 > 0)
                    active[dev] = true;
                guid[dev] = new Guid(_Devices[dev].Driver);
                channels[dev] = (short)_Devices[dev].Channels;
            }

            for (int i = 0; i < _Devices.Count; i++)
            {
                if (active[i])
                {
                    CSoundCardSource source = new CSoundCardSource(guid[i], channels[i]) {SampleRateKhz = 44.1};
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

        public int NumHalfTones(int player)
        {
            if (!_Initialized)
                return 0;

            return _Buffer[player].NumHalfTones;
        }

        public float[] ToneWeigth(int player)
        {
            if (!_Initialized)
                return null;

            return _Buffer[player].ToneWeigth;
        }

        private void _OnDataReady(object sender, CSampleDataEventArgs e)
        {
            if (_Initialized)
            {
                byte[] leftBuffer = new byte[e.Data.Length / 2];
                byte[] rightBuffer = new byte[e.Data.Length / 2];

                //[]: Sample, L: Left channel R: Right channel
                //[LR][LR][LR][LR][LR][LR]
                //The data is interleaved and needs to be demultiplexed
                for (int i = 0; i < e.Data.Length / 2; i++)
                {
                    leftBuffer[i] = e.Data[i * 2 - (i % 2)];
                    rightBuffer[i] = e.Data[i * 2 - (i % 2) + 2];
                }

                foreach (CRecordDevice device in _Devices)
                {
                    if (device.Driver == e.Guid.ToString())
                    {
                        if (device.PlayerChannel1 > 0)
                            _Buffer[device.PlayerChannel1 - 1].ProcessNewBuffer(leftBuffer);

                        if (device.PlayerChannel2 > 0)
                            _Buffer[device.PlayerChannel2 - 1].ProcessNewBuffer(rightBuffer);
                    }
                }
            }
        }

        private class CSampleDataEventArgs : EventArgs
        {
            public CSampleDataEventArgs(byte[] data, Guid guid)
            {
                Data = data;
                Guid = guid;
            }

            public byte[] Data { get; private set; }
            public Guid Guid { get; private set; }
        }

        private class CSoundCardSource : IDisposable
        {
            private volatile bool _Running;
            private readonly int _BufferSize;
            private CaptureBuffer _CaptureBuffer;
            private CaptureBufferDescription _BufferDescription;
            private DirectSoundCapture _CaptureDevice;
            private readonly WaveFormat _WaveFormat;
            private Thread _CaptureThread;
            private List<NotificationPosition> _Notifications;
            private int _BufferPortionCount;
            private int _BufferPortionSize;
            private WaitHandle[] _WaitHandles;
            private double _SampleRate;
            private readonly Guid _Guid;
            private readonly short _Channels;

            public CSoundCardSource(Guid guid, short channels)
            {
                _Guid = guid;
                _Channels = channels;
                _WaveFormat = new WaveFormat();
                SampleRateKhz = 44.1;
                _BufferSize = 2048;
            }

            public event EventHandler<CSampleDataEventArgs> SampleDataReady = delegate { };

            public double SampleRateKhz
            {
                get { return _SampleRate; }

                set
                {
                    _SampleRate = value;

                    if (_Running)
                        Restart();
                }
            }

            public void Start()
            {
                if (_Running)
                    throw new InvalidOperationException();

                if (_CaptureDevice == null)
                    _CaptureDevice = new DirectSoundCapture(_Guid);

                _WaveFormat.FormatTag = WaveFormatTag.Pcm; // Change to WaveFormatTag.IeeeFloat for float
                _WaveFormat.BitsPerSample = 16; // Set this to 32 for float
                _WaveFormat.BlockAlignment = (short)(_Channels * (_WaveFormat.BitsPerSample / 8));
                _WaveFormat.Channels = _Channels;
                _WaveFormat.SamplesPerSecond = (int)(SampleRateKhz * 1000D);
                _WaveFormat.AverageBytesPerSecond =
                    _WaveFormat.SamplesPerSecond *
                    _WaveFormat.BlockAlignment;

                _BufferPortionCount = 2;

                _BufferDescription.BufferBytes = _BufferSize * sizeof(short) * _BufferPortionCount * _Channels;
                _BufferDescription.Format = _WaveFormat;
                _BufferDescription.WaveMapped = false;

                _CaptureBuffer = new CaptureBuffer(_CaptureDevice, _BufferDescription);

                _BufferPortionSize = _CaptureBuffer.SizeInBytes / _BufferPortionCount;
                _Notifications = new List<NotificationPosition>();

                for (int i = 0; i < _BufferPortionCount; i++)
                {
                    NotificationPosition notification = new NotificationPosition {Offset = _BufferPortionCount - 1 + (_BufferPortionSize * i), Event = new AutoResetEvent(false)};
                    _Notifications.Add(notification);
                }

                _CaptureBuffer.SetNotificationPositions(_Notifications.ToArray());
                _WaitHandles = new WaitHandle[_Notifications.Count];

                for (int i = 0; i < _Notifications.Count; i++)
                    _WaitHandles[i] = _Notifications[i].Event;

                _CaptureThread = new Thread(_DoCapture) {IsBackground = true};

                _Running = true;
                _CaptureThread.Start();
            }

            public void Stop()
            {
                _Running = false;

                if (_CaptureThread != null)
                {
                    _CaptureThread.Join();
                    _CaptureThread = null;
                }

                if (_CaptureBuffer != null)
                {
                    _CaptureBuffer.Dispose();
                    _CaptureBuffer = null;
                }

                if (_Notifications != null)
                {
                    foreach (NotificationPosition notification in _Notifications)
                        notification.Event.Close();

                    _Notifications.Clear();
                    _Notifications = null;
                }
            }

            public void Restart()
            {
                Stop();
                Start();
            }

            private void _DoCapture()
            {
                int bufferPortionSamples = _BufferPortionSize / sizeof(byte);

                // Buffer type must match this.waveFormat.FormatTag and this.waveFormat.BitsPerSample
                byte[] bufferPortion = new byte[bufferPortionSamples];

                _CaptureBuffer.Start(true);

                while (_Running)
                {
                    int bufferPortionIndex = WaitHandle.WaitAny(_WaitHandles);

                    _CaptureBuffer.Read(
                        bufferPortion,
                        0,
                        bufferPortionSamples,
                        _BufferPortionSize * Math.Abs((bufferPortionIndex - 1) % _BufferPortionCount));

                    SampleDataReady(this, new CSampleDataEventArgs(bufferPortion, _Guid));
                }

                _CaptureBuffer.Stop();
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            // ReSharper disable InconsistentNaming
            protected void Dispose(bool disposing)
                // ReSharper restore InconsistentNaming
            {
                if (disposing)
                {
                    Stop();

                    if (_CaptureDevice != null)
                    {
                        _CaptureDevice.Dispose();
                        _CaptureDevice = null;
                    }
                }
            }
        }
    }
}