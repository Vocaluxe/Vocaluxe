using System;
using System.Runtime.InteropServices;

namespace Vocaluxe.Lib.Sound.Record.PitchTracker
{
    /// <summary>
    /// Pitchtracker (Pt) that uses autocorrelation (AKF) and AMDF
    /// Quite fast and perfect in artificial tests, but may fail in real world scenarios (e.g. missing fundamental)
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

        public CPtAKF(uint step = 200)
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