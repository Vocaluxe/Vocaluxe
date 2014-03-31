using System;
using System.Runtime.InteropServices;

namespace Vocaluxe.Lib.Sound.Record
{
    /// <summary>
    /// Analyzer from Performous
    /// Should be very reliable but needs some proof (artificial tests show some errors)
    /// </summary>
    class CAnalyzer : IDisposable
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
        private static extern double Analyzer_GetPeak(IntPtr analyzer);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern double Analyzer_FindNote(IntPtr analyzer, double minFreq, double maxFreq);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool Analyzer_OutputFloat(IntPtr analyzer, [Out] float[] data, int sampleCt, float rate);
        #endregion

        private IntPtr _Instance;

        public CAnalyzer(uint step = 200)
        {
            _Instance = Analyzer_Create(step);
        }

        ~CAnalyzer()
        {
            _Dispose(false);
        }

        public void Input(float[] data)
        {
            Analyzer_InputFloat(_Instance, data, data.Length);
        }

        public void Input(short[] data)
        {
            Analyzer_InputShort(_Instance, data, data.Length);
        }

        public void Input(byte[] data)
        {
            Analyzer_InputByte(_Instance, data, data.Length / 2);
        }

        public void Process()
        {
            Analyzer_Process(_Instance);
        }

        public double GetPeak()
        {
            return Analyzer_GetPeak(_Instance);
        }

        public double FindNote(double minFreq = 35.0, double maxFreq = 1000.0)
        {
            return Analyzer_FindNote(_Instance, minFreq, maxFreq);
        }

        public bool Output(float[] data, float rate)
        {
            return Analyzer_OutputFloat(_Instance, data, data.Length, rate);
        }

        private void _Dispose(bool disposing)
        {
            if (_Instance == IntPtr.Zero)
                throw new ObjectDisposedException(GetType().Name);
            Analyzer_Free(_Instance);
            _Instance = IntPtr.Zero;
        }

        public void Dispose()
        {
            _Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Pitchtracker (Pt) that uses autocorrelation (AKF) and AMDF
    /// Quite fast and perfect in artificial tests, but may fail in real world scenarios (e.g. missing fundamental)
    /// </summary>
    class CPtAKF : IDisposable
    {
        #region Imports
        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PtAKF_Create(uint step);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void PtAKF_Free(IntPtr analyzer);

        [DllImport("PitchTracker.dll", EntryPoint = "PtAKF_GetNumHalfTones", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetNumHalfTones();

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void PtAKF_InputByte(IntPtr analyzer, [In] byte[] data, int sampleCt);

        [DllImport("PitchTracker.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int PtAKF_GetNote(IntPtr analyzer, [Out] out double maxVolume, [Out] float[] weights);
        #endregion

        private IntPtr _Instance;

        public CPtAKF(uint step = 200)
        {
            _Instance = PtAKF_Create(step);
        }

        ~CPtAKF()
        {
            _Dispose(false);
        }

        public void Input(byte[] data)
        {
            PtAKF_InputByte(_Instance, data, data.Length / 2);
        }

        public int GetNote(out double maxVolume, float[] weights)
        {
            return PtAKF_GetNote(_Instance, out maxVolume, weights);
        }

        private void _Dispose(bool disposing)
        {
            if (_Instance == IntPtr.Zero)
                throw new ObjectDisposedException(GetType().Name);
            PtAKF_Free(_Instance);
            _Instance = IntPtr.Zero;
        }

        public void Dispose()
        {
            _Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}