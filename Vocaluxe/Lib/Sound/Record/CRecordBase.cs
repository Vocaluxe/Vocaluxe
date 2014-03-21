using System.Collections.Generic;
using System.Collections.ObjectModel;
using Vocaluxe.Base;

namespace Vocaluxe.Lib.Sound.Record
{
    /// <summary>
    /// Base class to implement common functionality for Record classes
    /// </summary>
    abstract class CRecordBase
    {
        private bool _Initialized;
        protected List<CRecordDevice> _Devices;
        protected CBuffer[] _Buffer;

        /// <summary>
        ///     Init recording
        ///     Child class should overwrite this to list devices
        /// </summary>
        /// <returns>true if success</returns>
        public virtual bool Init()
        {
            if (_Initialized)
                return false;

            _Devices = new List<CRecordDevice>();
            _Buffer = new CBuffer[CSettings.MaxNumPlayer];
            for (int i = 0; i < _Buffer.Length; i++)
                _Buffer[i] = new CBuffer();

            _Initialized = true;

            return true;
        }

        /// <summary>
        ///     Stop all voice capturing streams and terminates recording freeing resources
        /// </summary>
        public virtual void Close()
        {
            if (!_Initialized)
                return;

            _Devices = null;
            if (_Buffer != null)
            {
                foreach (CBuffer buffer in _Buffer)
                    buffer.Dispose();
                _Buffer = null;
            }

            _Initialized = false;
        }

        /// <summary>
        /// Procedure to be called by descendent classes with data that got recorded
        /// </summary>
        /// <param name="device">Device from which the data was recorded</param>
        /// <param name="data">Recorded samples, assume Int16 and interleaved for multi-channels</param>
        protected void _HandleData(CRecordDevice device, byte[] data)
        {
            byte[] leftBuffer;
            byte[] rightBuffer;

            if (device.Channels == 2)
            {
                leftBuffer = new byte[data.Length / 2];
                rightBuffer = new byte[data.Length / 2];
                //[]: Sample, L: Left channel R: Right channel
                //[LR][LR][LR][LR][LR][LR]
                //The data is interleaved and needs to be demultiplexed
                for (int i = 0; i < data.Length / 4; i++)
                {
                    leftBuffer[i * 2] = data[i * 4];
                    leftBuffer[i * 2 + 1] = data[i * 4 + 1];
                    rightBuffer[i * 2] = data[i * 4 + 2];
                    rightBuffer[i * 2 + 1] = data[i * 4 + 3];
                }
            }
            else
                leftBuffer = rightBuffer = data;

            if (device.PlayerChannel1 > 0)
                _Buffer[device.PlayerChannel1 - 1].ProcessNewBuffer(leftBuffer);

            if (device.PlayerChannel2 > 0)
                _Buffer[device.PlayerChannel2 - 1].ProcessNewBuffer(rightBuffer);
        }

        /// <summary>
        ///     Detect Pitch and Volume of the newest voice buffer
        /// </summary>
        /// <param name="player"></param>
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
            return _Initialized ? _Buffer[player].Tone : 0;
        }

        public void SetTone(int player, int tone)
        {
            if (!_Initialized)
                return;

            _Buffer[player].Tone = tone;
        }

        public float GetMaxVolume(int player)
        {
            return _Initialized ? _Buffer[player].MaxVolume : 0f;
        }

        public bool ToneValid(int player)
        {
            return _Initialized && _Buffer[player].ToneValid;
        }

        public int NumHalfTones()
        {
            return CBuffer.NumHalfTones;
        }

        public float[] ToneWeigth(int player)
        {
            return _Initialized ? _Buffer[player].ToneWeigth : null;
        }

        public ReadOnlyCollection<CRecordDevice> RecordDevices()
        {
            return (_Initialized && _Devices.Count > 0) ? _Devices.AsReadOnly() : null;
        }
    }
}