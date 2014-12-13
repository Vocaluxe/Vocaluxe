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
    ///     Analyzer from Performous
    ///     Should be very reliable but needs some proof (artificial tests show some errors)
    /// </summary>
    class CAnalyzer : CPitchTracker
    {
        #region Imports
        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Analyzer_Create(uint step = 200);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Analyzer_Free(IntPtr analyzer);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Analyzer_InputFloat(IntPtr analyzer, [In] float[] data, int sampleCt);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Analyzer_InputShort(IntPtr analyzer, [In] short[] data, int sampleCt);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Analyzer_InputByte(IntPtr analyzer, [In] byte[] data, int sampleCt);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Analyzer_Process(IntPtr analyzer);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern float Analyzer_GetPeak(IntPtr analyzer);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern double Analyzer_FindNote(IntPtr analyzer, double minFreq, double maxFreq);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Analyzer_OutputFloat(IntPtr analyzer, [Out] float[] data, int sampleCt, float rate);
        #endregion

        private IntPtr _Instance;
        private float _VolumeTreshold;

        public CAnalyzer(uint step = 200)
        {
            _Instance = Analyzer_Create(step);
            _VolumeTreshold = 0.01f;
        }

        public override void Input(byte[] data)
        {
            Analyzer_InputByte(_Instance, data, data.Length / 2);
        }

        public override int GetNote(out float maxVolume, float[] weights)
        {
            Analyzer_Process(_Instance);
            maxVolume = Analyzer_GetPeak(_Instance);
            if (maxVolume < _VolumeTreshold)
                return -1;
            int note = (int)Math.Round(Analyzer_FindNote(_Instance, 60, 1800));
            _SetWeights(note, weights);
            return note;
        }

        public bool Output(float[] data, float rate)
        {
            return Analyzer_OutputFloat(_Instance, data, data.Length, rate);
        }

        protected override void _Dispose(bool disposing)
        {
            if (_Instance == IntPtr.Zero)
                throw new ObjectDisposedException(GetType().Name);
            Analyzer_Free(_Instance);
            _Instance = IntPtr.Zero;
        }

        public override int GetNumHalfTones()
        {
            return NumHalfTonesDef;
        }

        public override float VolumeTreshold
        {
            get { return _VolumeTreshold; }
            set { _VolumeTreshold = value; }
        }
    }
}