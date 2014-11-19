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
using System.Diagnostics;

namespace Vocaluxe.Lib.Sound.Record.PitchTracker
{
    /// <summary>
    ///     Base class/Interface for pitch detectors
    ///     Most likely some structures or system resources are used, hence it should be disposed if no longer needed
    /// </summary>
    abstract class CPitchTracker : IDisposable
    {
        //Default number of half tones (1 octave)
        protected const int NumHalfTonesDef = 12;

        ~CPitchTracker()
        {
            _Dispose(false);
        }

        protected abstract void _Dispose(bool disposing);

        public void Dispose()
        {
            _Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Method to set weights if the algorithm does not provide anything that could simulate a visualizer.
        ///     Should be called with each valid detection
        /// </summary>
        /// <param name="note">Current note</param>
        /// <param name="weights">Array of weights (Length = NumHalfTonesDef)</param>
        protected static void _SetWeights(int note, float[] weights)
        {
            Debug.Assert(weights.Length == NumHalfTonesDef);
            for (int i = 0; i < NumHalfTonesDef; i++)
                weights[i] *= 0.9f;
            if (note < 0)
                return;
            int w = note % NumHalfTonesDef;
            weights[w] = Math.Min(1f, weights[w] + 0.15f);
        }

        /// <summary>
        ///     Gets the number of half tones the algorithm provides weights for.
        ///     This may not be the number of half tones the algorithm detects!
        /// </summary>
        /// <returns></returns>
        public abstract int GetNumHalfTones();

        /// <summary>
        ///     The treshold for detecting silence
        ///     Value (0-1) for which everything below is considered silence
        /// </summary>
        public abstract float VolumeTreshold { get; set; }

        /// <summary>
        ///     Recorded data. Has to be 16 bit short values.
        /// </summary>
        /// <param name="data">16bit short values of data</param>
        public abstract void Input(byte[] data);

        /// <summary>
        ///     Returns the current note detected
        /// </summary>
        /// <param name="maxVolume">Current maximum volume (0-1)</param>
        /// <param name="weights">
        ///     Array of NumHalfTones floats that gets filled with the current weights (0-1) where each value indicates how strong that tone is.
        ///     Only for basic visualization as it may not be accurate.
        /// </param>
        /// <returns>Current note index (0 = C2, 12 = C3, ...)</returns>
        public abstract int GetNote(out float maxVolume, float[] weights);
    }
}