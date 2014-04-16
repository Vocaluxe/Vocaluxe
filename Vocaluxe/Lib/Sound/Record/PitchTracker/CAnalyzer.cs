using System;
using System.Runtime.InteropServices;

namespace Vocaluxe.Lib.Sound.Record.PitchTracker
{
    /// <summary>
    /// Analyzer from Performous
    /// Should be very reliable but needs some proof (artificial tests show some errors)
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

    /// <summary>
    /// Pitchtracker (Pt) in C# that uses IIR filters for smoothing and an intelligent detection algorithm to reduce calculations
    /// </summary>
    class CPtSharp : CPitchTracker
    {
        private Pitch.PitchTracker _Instance;

        public CPtSharp()
        {
            _Instance = new Pitch.PitchTracker {SampleRate = 44100, DetectLevelThreshold = 0.01f, PitchRecordHistorySize = 1};
        }

        public override void Input(byte[] data)
        {
            short[] samplesShort = new short[data.Length / 2];
            Buffer.BlockCopy(data, 0, samplesShort, 0, data.Length);
            float[] samplesFloat = new float[samplesShort.Length];
            for (int i = 0; i < samplesShort.Length; i++)
                samplesFloat[i] = (float)samplesShort[i] / short.MaxValue;
            _Instance.ProcessBuffer(samplesFloat);
        }

        public override int GetNote(out float maxVolume, float[] weights)
        {
            int note = _Instance.CurrentPitchRecord.MidiNote - 15 - 21;
            maxVolume = _Instance.LastMaxVol;
            _SetWeights(note, weights);
            return note;
        }

        public override int GetNumHalfTones()
        {
            return NumHalfTonesDef;
        }

        public override float VolumeTreshold
        {
            get { return _Instance.DetectLevelThreshold; }
            set { _Instance.DetectLevelThreshold = value; }
        }

        protected override void _Dispose(bool disposing)
        {
            if (_Instance == null)
                throw new ObjectDisposedException(GetType().Name);
            _Instance = null;
        }
    }
}