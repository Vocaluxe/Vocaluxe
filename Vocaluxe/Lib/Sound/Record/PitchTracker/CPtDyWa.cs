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
using System.Runtime.InteropServices;

namespace Vocaluxe.Lib.Sound.Record.PitchTracker
{
    /// <summary>
    ///     Pitchtracker (Pt) that uses a dynamic wavelet (DyWa) algorithm
    /// </summary>
    class CPtDyWa : CPitchTracker
    {
        #region Imports
        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PtDyWa_Create(uint step);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void PtDyWa_Free(IntPtr analyzer);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void PtDyWa_SetVolumeThreshold(IntPtr analyzer, float threshold);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern float PtDyWa_GetVolumeThreshold(IntPtr analyzer);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void PtDyWa_InputByte(IntPtr analyzer, [In] byte[] data, int sampleCt);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern double PtDyWa_FindNote(IntPtr analyzer, [Out] out float maxVolume);
        #endregion

        private IntPtr _Instance;

        public CPtDyWa(uint step = 200)
        {
            _Instance = PtDyWa_Create(step);
        }

        public override void Input(byte[] data)
        {
            PtDyWa_InputByte(_Instance, data, data.Length / 2);
        }

        public override int GetNote(out float maxVolume, float[] weights)
        {
            int note = (int)Math.Round(PtDyWa_FindNote(_Instance, out maxVolume));
            _SetWeights(note, weights);
            return note;
        }

        public override int GetNumHalfTones()
        {
            return NumHalfTonesDef;
        }

        public override float VolumeTreshold
        {
            get { return PtDyWa_GetVolumeThreshold(_Instance); }
            set { PtDyWa_SetVolumeThreshold(_Instance, value); }
        }

        protected override void _Dispose(bool disposing)
        {
            if (_Instance == IntPtr.Zero)
                throw new ObjectDisposedException(GetType().Name);
            PtDyWa_Free(_Instance);
            _Instance = IntPtr.Zero;
        }
    }
}