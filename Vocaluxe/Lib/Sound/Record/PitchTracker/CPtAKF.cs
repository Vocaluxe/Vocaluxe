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
    ///     Pitchtracker (Pt) that uses autocorrelation (AKF) and AMDF
    ///     Quite fast and perfect in artificial tests, but may fail in real world scenarios (e.g. missing fundamental)
    /// </summary>
    class CPtAKF : CPitchTracker
    {
        #region Imports
        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PtAKF_Create(uint step);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void PtAKF_Free(IntPtr analyzer);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int PtAKF_GetNumHalfTones();

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void PtAKF_InputByte(IntPtr analyzer, [In] byte[] data, int sampleCt);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void PtAKF_SetVolumeThreshold(IntPtr analyzer, float threshold);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern float PtAKF_GetVolumeThreshold(IntPtr analyzer);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int PtAKF_GetNote(IntPtr analyzer, [Out] out float maxVolume, [Out] float[] weights);
        #endregion

        private IntPtr _Instance;

        public CPtAKF(uint step = 1024)
        {
            _Instance = PtAKF_Create(step);
        }

        public override int GetNumHalfTones()
        {
            return PtAKF_GetNumHalfTones();
        }

        public override float VolumeTreshold
        {
            get { return PtAKF_GetVolumeThreshold(_Instance); }
            set { PtAKF_SetVolumeThreshold(_Instance, value); }
        }

        public override void Input(byte[] data)
        {
            PtAKF_InputByte(_Instance, data, data.Length / 2);
        }

        public override int GetNote(out float maxVolume, float[] weights)
        {
            return PtAKF_GetNote(_Instance, out maxVolume, weights);
        }

        protected override void _Dispose(bool disposing)
        {
            if (_Instance == IntPtr.Zero)
                throw new ObjectDisposedException(GetType().Name);
            PtAKF_Free(_Instance);
            _Instance = IntPtr.Zero;
        }
    }
}