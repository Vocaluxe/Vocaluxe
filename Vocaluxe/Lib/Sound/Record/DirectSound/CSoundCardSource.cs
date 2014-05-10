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
using System.Threading;
using SlimDX.DirectSound;
using SlimDX.Multimedia;

namespace Vocaluxe.Lib.Sound.Record.DirectSound
{
    public class CSoundCardSource : IDisposable
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
        private readonly string _Guid;
        private readonly short _Channels;

        public CSoundCardSource(string guid, short channels)
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
                _CaptureDevice = new DirectSoundCapture(new Guid(_Guid));

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
                var notification = new NotificationPosition {Offset = _BufferPortionCount - 1 + (_BufferPortionSize * i), Event = new AutoResetEvent(false)};
                _Notifications.Add(notification);
            }

            _CaptureBuffer.SetNotificationPositions(_Notifications.ToArray());
            _WaitHandles = new WaitHandle[_Notifications.Count];

            for (int i = 0; i < _Notifications.Count; i++)
                _WaitHandles[i] = _Notifications[i].Event;

            _CaptureThread = new Thread(_DoCapture) {Name = "DirectSoundCapture", IsBackground = true};

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
            var bufferPortion = new byte[bufferPortionSamples];

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