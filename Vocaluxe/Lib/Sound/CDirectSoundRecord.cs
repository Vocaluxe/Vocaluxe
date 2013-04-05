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
        private bool _initialized;
        private List<SRecordDevice> _Devices;
        private SRecordDevice[] _DeviceConfig;
        private List<SoundCardSource> _Sources;

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
                _Buffer[i].Reset();

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
                    source.SampleDataReady += OnDataReady;
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
                Data = data;
                Guid = guid;
            }

            public byte[] Data { get; private set; }
            public Guid Guid { get; private set; }
        }

        public class SoundCardSource : IDisposable
        {
            private volatile bool running;
            private readonly int bufferSize;
            private CaptureBuffer CaptureBuffer;
            private CaptureBufferDescription bufferDescription;
            private DirectSoundCapture captureDevice;
            private readonly WaveFormat waveFormat;
            private Thread captureThread;
            private List<NotificationPosition> notifications;
            private int bufferPortionCount;
            private int bufferPortionSize;
            private WaitHandle[] waitHandles;
            private double sampleRate;
            private readonly Guid guid;
            private readonly short channels;

            public SoundCardSource(Guid guid, short channels)
            {
                this.guid = guid;
                this.channels = channels;
                waveFormat = new WaveFormat();
                SampleRateKHz = 44.1;
                bufferSize = 2048;
            }

            public event EventHandler<SampleDataEventArgs> SampleDataReady = delegate { };

            public double SampleRateKHz
            {
                get { return sampleRate; }

                set
                {
                    sampleRate = value;

                    if (running)
                        Restart();
                }
            }

            public void Start()
            {
                if (running)
                    throw new InvalidOperationException();

                if (captureDevice == null)
                    captureDevice = new DirectSoundCapture(guid);

                waveFormat.FormatTag = WaveFormatTag.Pcm; // Change to WaveFormatTag.IeeeFloat for float
                waveFormat.BitsPerSample = 16; // Set this to 32 for float
                waveFormat.BlockAlignment = (short)(channels * (waveFormat.BitsPerSample / 8));
                waveFormat.Channels = channels;
                waveFormat.SamplesPerSecond = (int)(SampleRateKHz * 1000D);
                waveFormat.AverageBytesPerSecond =
                    waveFormat.SamplesPerSecond *
                    waveFormat.BlockAlignment;

                bufferPortionCount = 2;

                bufferDescription.BufferBytes = bufferSize * sizeof(short) * bufferPortionCount * channels;
                bufferDescription.Format = waveFormat;
                bufferDescription.WaveMapped = false;

                CaptureBuffer = new CaptureBuffer(captureDevice, bufferDescription);

                bufferPortionSize = CaptureBuffer.SizeInBytes / bufferPortionCount;
                notifications = new List<NotificationPosition>();

                for (int i = 0; i < bufferPortionCount; i++)
                {
                    NotificationPosition notification = new NotificationPosition();
                    notification.Offset = bufferPortionCount - 1 + (bufferPortionSize * i);
                    notification.Event = new AutoResetEvent(false);
                    notifications.Add(notification);
                }

                CaptureBuffer.SetNotificationPositions(notifications.ToArray());
                waitHandles = new WaitHandle[notifications.Count];

                for (int i = 0; i < notifications.Count; i++)
                    waitHandles[i] = notifications[i].Event;

                captureThread = new Thread(DoCapture);
                captureThread.IsBackground = true;

                running = true;
                captureThread.Start();
            }

            public void Stop()
            {
                running = false;

                if (captureThread != null)
                {
                    captureThread.Join();
                    captureThread = null;
                }

                if (CaptureBuffer != null)
                {
                    CaptureBuffer.Dispose();
                    CaptureBuffer = null;
                }

                if (notifications != null)
                {
                    for (int i = 0; i < notifications.Count; i++)
                        notifications[i].Event.Close();

                    notifications.Clear();
                    notifications = null;
                }
            }

            public void Restart()
            {
                Stop();
                Start();
            }

            private void DoCapture()
            {
                int bufferPortionSamples = bufferPortionSize / sizeof(byte);

                // Buffer type must match this.waveFormat.FormatTag and this.waveFormat.BitsPerSample
                byte[] bufferPortion = new byte[bufferPortionSamples];
                int bufferPortionIndex;

                CaptureBuffer.Start(true);

                while (running)
                {
                    bufferPortionIndex = WaitHandle.WaitAny(waitHandles);

                    CaptureBuffer.Read(
                        bufferPortion,
                        0,
                        bufferPortionSamples,
                        bufferPortionSize * Math.Abs((bufferPortionIndex - 1) % bufferPortionCount));

                    SampleDataReady(this, new SampleDataEventArgs(bufferPortion, guid));
                }

                CaptureBuffer.Stop();
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Stop();

                    if (captureDevice != null)
                    {
                        captureDevice.Dispose();
                        captureDevice = null;
                    }
                }
            }
        }
    }
}