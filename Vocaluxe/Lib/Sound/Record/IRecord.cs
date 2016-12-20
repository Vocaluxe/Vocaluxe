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
using System.Collections.ObjectModel;

namespace Vocaluxe.Lib.Sound.Record
{
    class CRecordDevice
    {
        public readonly int ID;
        public readonly string Name;
        public readonly string Driver;

        public readonly int Channels;
        public int[] PlayerChannel;

        public CRecordDevice(int id, string name, string driver, int channels)
        {
            ID = id;
            Name = name;
            Driver = driver;
            Channels = channels;
            PlayerChannel = new int[channels];
        }
    }

    struct SMicConfig
    {
        public string DeviceName;
        public string DeviceDriver;
        public int Channel;

        // ReSharper disable UnusedParameter.Local
        public SMicConfig(int dummy)
            // ReSharper restore UnusedParameter.Local
        {
            DeviceName = String.Empty;
            DeviceDriver = String.Empty;
            Channel = 0;
        }
    }

    interface IRecord
    {
        /// <summary>
        ///     Initializes recording
        /// </summary>
        /// <returns>True on success</returns>
        bool Init();

        /// <summary>
        ///     Closes recording. You must not use it before calling Init again
        /// </summary>
        void Close();

        /// <summary>
        ///     Starts recording on all enabled devices (PlayerChannelX set)
        /// </summary>
        /// <returns>True on success, false on error or not initialized</returns>
        bool Start();

        /// <summary>
        ///     Stops recording on all devices
        /// </summary>
        /// <returns>True on success, false on error or not initialized</returns>
        bool Stop();

        /// <summary>
        ///     Analyzes the buffer for the given player
        /// </summary>
        /// <param name="player"></param>
        void AnalyzeBuffer(int player);

        /// <summary>
        ///     Get the current raw tone value for a player
        /// </summary>
        /// <param name="player">0-based player index</param>
        /// <returns>Tone between 0 and NumHalftones-1</returns>
        int GetToneAbs(int player);

        /// <summary>
        ///     Get the current tone value for a player
        /// </summary>
        /// <param name="player">0-based player index</param>
        /// <returns>Tone between 0 and 11 (one octave)</returns>
        int GetTone(int player);

        /// <summary>
        ///     DEBUG ONLY: Sets a tone for a player
        /// </summary>
        /// <param name="player">0-based player index</param>
        /// <param name="tone">Raw tone value between 0 and NumHalfTones-1</param>
        void SetTone(int player, int tone);

        /// <summary>
        ///     Get the current maximum input volume (normalized) for a player
        /// </summary>
        /// <param name="player">0-based player index</param>
        /// <returns>Maximum volume between 0 and 1</returns>
        float GetMaxVolume(int player);

        /// <summary>
        ///     Gets the treshold for detecting silence
        ///     Value (0-1) for which everything below is considered silence
        ///     <param name="player">0-based player index</param>
        ///     <returns>Threshold</returns>
        /// </summary>
        float GetVolumeThreshold(int player);

        /// <summary>
        ///     Sets the treshold for detecting silence
        ///     Value (0-1) for which everything below is considered silence
        ///     <param name="player">0-based player index</param>
        ///     <param name="threshold">Threshold</param>
        /// </summary>
        void SetVolumeThreshold(int player, float threshold);

        /// <summary>
        ///     Returns whether the current tone is valid. That includes, if maximum volume is higher than the threshold
        /// </summary>
        /// <param name="player">0-based player index</param>
        /// <returns>True if tone is detected and volume higher than threshold</returns>
        bool ToneValid(int player);

        /// <summary>
        ///     Returns the number of half tones used for the raw tone detection
        /// </summary>
        /// <returns>number of half tones used for the raw tone detection</returns>
        int NumHalfTones();

        /// <summary>
        ///     Returns the tone weights for a player as an array of normalized values for each halftone
        /// </summary>
        /// <param name="player">0-based player index</param>
        /// <returns>Array of values (0-1) with one entry per halftone</returns>
        float[] ToneWeigth(int player);

        /// <summary>
        ///     Readonly list of all detected record devices. Modifying the devices should not be done when record is running.
        /// </summary>
        /// <returns>Readonly list of all detected record devices</returns>
        ReadOnlyCollection<CRecordDevice> RecordDevices();
    }
}