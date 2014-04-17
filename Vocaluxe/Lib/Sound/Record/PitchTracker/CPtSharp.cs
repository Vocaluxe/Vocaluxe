using System;

namespace Vocaluxe.Lib.Sound.Record.PitchTracker
{
    /// <summary>
    /// Pitchtracker (Pt) in C# that uses IIR filters for smoothing and an intelligent detection algorithm to reduce calculations
    /// DO NOT USE in 32 Bit! Due to floating point errors the filter will raise all values to NAN (TODO: FIX it!)
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