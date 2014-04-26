using System;
using System.Runtime.InteropServices;

namespace Vocaluxe.Lib.Sound.Record.PitchTracker
{
    /// <summary>
    /// Pitchtracker (Pt) that uses a dynamic wavelet (DyWa) algorithm 
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