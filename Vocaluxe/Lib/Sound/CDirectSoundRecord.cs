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
        private bool _initialized = false;
        private List<SRecordDevice> _Devices = null;
        private SRecordDevice[] _DeviceConfig = null;
        private List<SoundCardSource> _Sources = null;

        private CBuffer[] _Buffer;

        public CDirectSoundRecord()
        {
            _Buffer = new CBuffer[CSettings.MaxNumPlayer];
            for (int i = 0; i < _Buffer.Length; i++)
            {
                _Buffer[i] = new CBuffer();
            }

            Init();
        }

        public bool Init()
        {
            DeviceCollection devices = DirectSoundCapture.GetDevices();
            _Devices = new List<SRecordDevice>();
            _Sources = new List<SoundCardSource>();

            int id = 0;
            foreach (DeviceInformation dev in devices)
            {
                using (DirectSoundCapture ds = new DirectSoundCapture(dev.DriverGuid))
                {

                    SRecordDevice device = new SRecordDevice();
                    device.Driver = dev.DriverGuid.ToString();
                    device.ID = id;
                    device.Name = dev.Description;
                    device.Inputs = new List<SInput>();

                    SInput inp = new SInput();
                    inp.Name = "Default";
                    inp.Channels = ds.Capabilities.Channels;

                    if (inp.Channels > 2)
                        inp.Channels = 2; //more are not supported in vocaluxe

                    device.Inputs.Add(inp);
                    _Devices.Add(device);

                    id++;
                }
            }

            _DeviceConfig = _Devices.ToArray();
            _initialized = true;

            return true;
        }

        public void CloseAll()
        {
            if (_initialized)
            {
                Stop();
                _initialized = false;
            }
            //System.IO.File.WriteAllBytes("test0.raw", _Buffer[0].Buffer);
        }

        public bool Start(SRecordDevice[] DeviceConfig)
        {
            if (!_initialized)
                return false;

            for (int i = 0; i < _Buffer.Length; i++)
            {
                _Buffer[i].Reset();
            }

            _DeviceConfig = DeviceConfig;
            bool[] active = new bool[DeviceConfig.Length];
            Guid[] guid = new Guid[DeviceConfig.Length];
            short[] channels = new short[DeviceConfig.Length];
            for (int dev = 0; dev < DeviceConfig.Length; dev++)
            {
                active[dev] = false;
                for (int inp = 0; inp < DeviceConfig[dev].Inputs.Count; inp++)
                {
                    if (DeviceConfig[dev].Inputs[inp].PlayerChannel1 > 0 ||
                        DeviceConfig[dev].Inputs[inp].PlayerChannel2 > 0)
                        active[dev] = true;
                    guid[dev] = new Guid(DeviceConfig[dev].Driver);
                    channels[dev] = (short)DeviceConfig[dev].Inputs[0].Channels;
                }
            }

            for (int i = 0; i < _Devices.Count; i++)
            {
                if (active[i])
                {
                    SoundCardSource source = new SoundCardSource(guid[i], channels[i]);
                    source.SampleRateKHz = 44.1;
                    source.SampleDataReady += this.OnDataReady;
                    source.Start();

                    _Sources.Add(source);

                }
            }

            _DeviceConfig = DeviceConfig;
            return true;
        }

        public bool Stop()
        {
            if (!_initialized)
                return false;

            foreach (SoundCardSource source in _Sources)
            {
                source.Stop();
                source.Dispose();
            }
            _Sources.Clear();

            return true;
        }
        public void AnalyzeBuffer(int Player)
        {
            if (!_initialized)
                return;

            _Buffer[Player].AnalyzeBuffer();
        }

        public int GetToneAbs(int Player)
        {
            if (!_initialized)
                return 0;

            return _Buffer[Player].ToneAbs;
        }

        public int GetTone(int Player)
        {
            if (!_initialized)
                return 0;

            return _Buffer[Player].Tone;
        }

        public void SetTone(int Player, int Tone)
        {
            if (!_initialized)
                return;

            _Buffer[Player].Tone = Tone;
        }

        public float GetMaxVolume(int Player)
        {
            if (!_initialized)
                return 0f;

            return _Buffer[Player].MaxVolume;
        }

        public bool ToneValid(int Player)
        {
            if (!_initialized)
                return false;

            return _Buffer[Player].ToneValid;
        }

        public SRecordDevice[] RecordDevices()
        {
            if (!_initialized)
                return null;

            if (_Devices.Count == 0)
                return null;

            return _Devices.ToArray();
        }

        public int NumHalfTones(int Player)
        {
            if (!_initialized)
                return 0;

            return _Buffer[Player].NumHalfTones;
        }

        public float[] ToneWeigth(int Player)
        {
            if (!_initialized)
                return null;

            return _Buffer[Player].ToneWeigth;
        }

        private void OnDataReady(object sender, SampleDataEventArgs e)
        {
            if (_initialized)
            {
                byte[] _leftBuffer = new byte[e.Data.Length / 2];
                byte[] _rightBuffer = new byte[e.Data.Length / 2];

                //[]: Sample, L: Left channel R: Right channel
                //[LR][LR][LR][LR][LR][LR]
                //The data is interleaved and needs to be demultiplexed
                for (int i = 0; i < e.Data.Length / 2; i++)
                {
                    _leftBuffer[i] = e.Data[i * 2 - (i % 2)];
                    _rightBuffer[i] = e.Data[i * 2 - (i % 2) + 2];
                }

                for (int i = 0; i < _DeviceConfig.Length; i++)
                {
                    if (_DeviceConfig[i].Driver == e.Guid.ToString())
                    {
                        if (_DeviceConfig[i].Inputs[0].PlayerChannel1 > 0)
                            _Buffer[_DeviceConfig[i].Inputs[0].PlayerChannel1 - 1].ProcessNewBuffer(_leftBuffer);

                        if (_DeviceConfig[i].Inputs[0].PlayerChannel2 > 0)
                            _Buffer[_DeviceConfig[i].Inputs[0].PlayerChannel2 - 1].ProcessNewBuffer(_rightBuffer);
                    }
                }
            }
        }

        public class SampleDataEventArgs : EventArgs
        {
            public SampleDataEventArgs(byte[] data, Guid guid)
            {
                this.Data = data;
                this.Guid = guid;
            }

            public byte[] Data { get; private set; }
            public Guid Guid { get; private set; }
        }

        public class SoundCardSource : IDisposable
        {
            private volatile bool running;
            private int bufferSize;
            private CaptureBuffer buffer;
            private CaptureBufferDescription bufferDescription;
            private DirectSoundCapture captureDevice;
            private WaveFormat waveFormat;
            private Thread captureThread;
            private List<NotificationPosition> notifications;
            private int bufferPortionCount;
            private int bufferPortionSize;
            private WaitHandle[] waitHandles;
            private double sampleRate;
            private Guid guid;
            private short channels;

            public SoundCardSource(Guid guid, short channels)
            {
                this.guid = guid;
                this.channels = channels;
                this.waveFormat = new WaveFormat();
                this.SampleRateKHz = 44.1;
                this.bufferSize = 2048;
            }

            public event EventHandler<SampleDataEventArgs> SampleDataReady = delegate { };

            public double SampleRateKHz
            {
                get
                {
                    return this.sampleRate;
                }

                set
                {
                    this.sampleRate = value;

                    if (this.running)
                    {
                        this.Restart();
                    }
                }
            }

            public void Start()
            {
                if (this.running)
                {
                    throw new InvalidOperationException();
                }

                if (this.captureDevice == null)
                {
                    this.captureDevice = new DirectSoundCapture(guid);
                }

                this.waveFormat.FormatTag = WaveFormatTag.Pcm; // Change to WaveFormatTag.IeeeFloat for float
                this.waveFormat.BitsPerSample = 16; // Set this to 32 for float
                this.waveFormat.BlockAlignment = (short)(channels * (waveFormat.BitsPerSample / 8));
                this.waveFormat.Channels = this.channels;
                this.waveFormat.SamplesPerSecond = (int)(this.SampleRateKHz * 1000D);
                this.waveFormat.AverageBytesPerSecond =
                    this.waveFormat.SamplesPerSecond *
                    this.waveFormat.BlockAlignment;

                this.bufferPortionCount = 2;

                this.bufferDescription.BufferBytes = this.bufferSize * sizeof(short) * bufferPortionCount * this.channels;
                this.bufferDescription.Format = this.waveFormat;
                this.bufferDescription.WaveMapped = false;

                this.buffer = new CaptureBuffer(this.captureDevice, this.bufferDescription);

                this.bufferPortionSize = this.buffer.SizeInBytes / this.bufferPortionCount;
                this.notifications = new List<NotificationPosition>();

                for (int i = 0; i < this.bufferPortionCount; i++)
                {
                    NotificationPosition notification = new NotificationPosition();
                    notification.Offset = this.bufferPortionCount - 1 + (bufferPortionSize * i);
                    notification.Event = new AutoResetEvent(false);
                    this.notifications.Add(notification);
                }

                this.buffer.SetNotificationPositions(this.notifications.ToArray());
                this.waitHandles = new WaitHandle[this.notifications.Count];

                for (int i = 0; i < this.notifications.Count; i++)
                {
                    this.waitHandles[i] = this.notifications[i].Event;
                }

                this.captureThread = new Thread(new ThreadStart(this.CaptureThread));
                this.captureThread.IsBackground = true;

                this.running = true;
                this.captureThread.Start();
            }

            public void Stop()
            {
                this.running = false;

                if (this.captureThread != null)
                {
                    this.captureThread.Join();
                    this.captureThread = null;
                }

                if (this.buffer != null)
                {
                    this.buffer.Dispose();
                    this.buffer = null;
                }

                if (this.notifications != null)
                {
                    for (int i = 0; i < this.notifications.Count; i++)
                    {
                        this.notifications[i].Event.Close();
                    }

                    this.notifications.Clear();
                    this.notifications = null;
                }
            }

            public void Restart()
            {
                this.Stop();
                this.Start();
            }

            private void CaptureThread()
            {
                int bufferPortionSamples = this.bufferPortionSize / sizeof(byte);

                // Buffer type must match this.waveFormat.FormatTag and this.waveFormat.BitsPerSample
                byte[] bufferPortion = new byte[bufferPortionSamples];
                int bufferPortionIndex;

                this.buffer.Start(true);

                while (this.running)
                {
                    bufferPortionIndex = WaitHandle.WaitAny(this.waitHandles);

                    this.buffer.Read(
                        bufferPortion,
                        0,
                        bufferPortionSamples,
                        bufferPortionSize * Math.Abs((bufferPortionIndex - 1) % bufferPortionCount));

                    this.SampleDataReady(this, new SampleDataEventArgs(bufferPortion, guid));
                }

                this.buffer.Stop();
            }

            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    this.Stop();

                    if (this.captureDevice != null)
                    {
                        this.captureDevice.Dispose();
                        this.captureDevice = null;
                    }
                }
            }
        }

    }
}
