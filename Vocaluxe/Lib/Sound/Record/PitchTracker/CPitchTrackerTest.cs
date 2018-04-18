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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VocaluxeLib;
using VocaluxeLib.Log;
using VocaluxeLib.Utils;

namespace Vocaluxe.Lib.Sound.Record.PitchTracker
{
    class CPitchTrackerTest
    {
        //Half tones: C C# D D# E F F# G G# A A# B
        private const double _BaseToneFreq = 65.4064; // lowest (half-)tone to analyze (C2 = 65.4064 Hz)
        private const double _HalftoneBase = 1.05946309436; // 2^(1/12) -> HalftoneBase^12 = 2 (one octave)

        private readonly List<CPitchTracker> _Analyzers = new List<CPitchTracker>();
        private int[] _Tones;
        private float[][] _Weights;
        private float _MaxVolume;
        private int _CurTestCount;
        private int[] _CurPassedCount;
        private int _TestCount;
        private int[] _PassedCount;
        private int[] _SamplesPerSec;

        private static bool _IsRun;

        public void AddAnalyzer(CPitchTracker analyzer)
        {
            _Analyzers.Add(analyzer);
        }

        #region Init
        private void _InitTests()
        {
            _Tones = new int[_Analyzers.Count];
            _TestCount = 0;
            _PassedCount = new int[_Analyzers.Count];
            _CurTestCount = 0;
            _CurPassedCount = new int[_Analyzers.Count];
            _Weights = new float[_Analyzers.Count][];
            for (int i = 0; i < _Analyzers.Count; i++)
                _Weights[i] = new float[_Analyzers[i].GetNumHalfTones()];
            _SamplesPerSec = new int[_Analyzers.Count];
        }

        private void _InitNext(string lastTest)
        {
            string msg = "Errors " + lastTest + ": ";
            for (int i = 0; i < _CurPassedCount.Length; i++)
            {
                msg += _CurTestCount - _CurPassedCount[i] + " ";
                _PassedCount[i] += _CurPassedCount[i];
                _CurPassedCount[i] = 0;
            }
            msg += "of " + _CurTestCount;
            CLog.Debug(msg);
            _TestCount += _CurTestCount;
            _CurTestCount = 0;
            //Do a reset first as we actually have an impossible situation (drop by multiple octaves)
            byte[] data = new byte[4096 * 2];
            _Process(data);
        }
        #endregion

        public void RunTest(bool reRun = false)
        {
            if (_IsRun && !reRun)
                return;
            _IsRun = true;

            _InitTests();

            _TestSines();
            _InitNext("Sines");

            _TestFile("toneG3.wav", 19);
            _InitNext("Real G3");

            _TestFile("toneG3Miss.wav", 19);
            _InitNext("Real G3 with miss. fundamental");

            _TestFile("toneG4.wav", 31);
            _InitNext("Real G4");

            _TestFile("whistling3.wav", "whistling3.txt");
            _InitNext("High whistling");

            _TestFile("sClausVoc.wav", "sClausVoc.txt");
            _InitNext("Real song");

            _TestSpeed();
            CLog.Debug("Finished: ");
            for (int i = 0; i < _Analyzers.Count; i++)
            {
                string msg = _Analyzers[i].GetType().Name + ":";
                msg += " Errors=" + (_TestCount - _PassedCount[i]);
                msg += " Passed=" + _PassedCount[i];
                msg += " Total=" + _TestCount;
                msg += " Speed=" + _SamplesPerSec[i] / 1000 + "kSamples/s (=" + (_SamplesPerSec[i] / 44100) + "rec.s/s)";
                CLog.Debug(msg);
            }
        }

        private void _TestSpeed()
        {
            const int samplesPerBuffer = 512;
            byte[] data;
            byte[] data2 = new byte[samplesPerBuffer * 2];
            double angle = 0;
            const int repeats = 100;
            _GetSineWave(_BaseToneFreq * Math.Pow(_HalftoneBase, 5), 44100, samplesPerBuffer * repeats, ref angle, out data);
            for (int i = 0; i < _Analyzers.Count; i++)
            {
                CPitchTracker analyzer = _Analyzers[i];
                Stopwatch sw = new Stopwatch();
                sw.Start();
                for (int j = 0; j < repeats; j++)
                {
                    Buffer.BlockCopy(data, 0, data2, 0, samplesPerBuffer * 2);
                    analyzer.Input(data2);
                    analyzer.GetNote(out _MaxVolume, _Weights[i]);
                }
                sw.Stop();
                _SamplesPerSec[i] = (int)Math.Round(samplesPerBuffer * repeats / (sw.ElapsedMilliseconds / 1000.0));
            }
        }

        private void _Process(byte[] data)
        {
            for (int j = 0; j < _Analyzers.Count; j++)
            {
                _Analyzers[j].Input(data);
                _Tones[j] = _Analyzers[j].GetNote(out _MaxVolume, _Weights[j]);
            }
        }

        private static string _ToneToNote(int tone, bool withOctave = true)
        {
            if (tone < 0)
                return "inv.";
            tone += 24;
            string[] notes = {"C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"};
            string result = notes[tone % 12];
            if (withOctave)
                result += (tone / 12);
            return result;
        }

        #region Sines
        private void _TestSines()
        {
            const int toneFrom = 0;
            const int toneTo = 47; //B5
            const int sampleCt = 4096;
            const int batchCt = 512;
            byte[] data2 = new byte[batchCt * 2];
            Console.WriteLine("Testing notes " + _ToneToNote(toneFrom) + " - " + _ToneToNote(toneTo));
            double angle = 0;
            bool[] valids = new bool[_Analyzers.Count];
            for (int distort = 0; distort < 10; distort++)
            {
                //Do a reset first as we actually have an impossible situation (drop by multiple octaves)
                byte[] data = new byte[sampleCt * 2];
                _Process(data);
                for (int tone = toneFrom; tone <= toneTo; tone++)
                {
                    _GetSineWave(_BaseToneFreq * Math.Pow(_HalftoneBase, tone), 44100, sampleCt, ref angle, out data);
                    if (tone == 46 && distort == 4)
                        data = new byte[data.Length];
                    _Distort(data, tone, distort);

                    for (int i = 0; i < sampleCt / batchCt; i++)
                    {
                        Buffer.BlockCopy(data, i * batchCt * 2, data2, 0, batchCt * 2);
                        _Process(data2);
                        if (i * batchCt < 2048)
                            continue;
                        _CurTestCount++;
                        bool ok = true;
                        for (int j = 0; j < valids.Length; j++)
                        {
                            valids[j] = _Tones[j] == tone;
                            if (!valids[j])
                                ok = false;
                            else
                                _CurPassedCount[j]++;
                        }
                        if (ok)
                            continue;
                        string msg = "Note " + _ToneToNote(tone) + "(" + distort + ") at buffer " + (i + 1) + "/" + (sampleCt / batchCt) + " detected as ";
                        for (int j = 0; j < valids.Length; j++)
                            msg += _ToneToNote(_Tones[j]) + (valids[j] ? "" : "(!)") + "; ";
                        CLog.Debug(msg);
                        /*CWavFile w = new CWavFile();
                        w.Create(tone + "-" + distort + ".wav", 1, 44100, 16);
                        w.Write16BitSamples(data);
                        w.Close();*/
                    }
                }
            }
        }

        private static short[] _GetDistort(int sampleCt, int tone, int type)
        {
            short[] sdata = new short[sampleCt];
            if (type < 9)
            {
                int newTone = 1;
                switch (type)
                {
                    case 0:
                        newTone = 2;
                        break;
                    case 1:
                        newTone = 3;
                        break;
                    case 2:
                        newTone = 6;
                        break;
                    case 3:
                        newTone = 8;
                        break;
                    case 4:
                        newTone = 10;
                        break;
                    case 5:
                        newTone = 15;
                        break;
                    case 6:
                        newTone = 25;
                        break;
                    case 7:
                        newTone = 26;
                        break;
                    case 8:
                        newTone = 29;
                        break;
                }
                byte[] data2;
                double angle = 0;
                _GetSineWave(_BaseToneFreq * Math.Pow(_HalftoneBase, tone + newTone), 44100, sampleCt, ref angle, out data2);
                Buffer.BlockCopy(data2, 0, sdata, 0, sampleCt * 2);
            }
            else
            {
                Random r = new Random(0xBEEF);
                for (int i = 0; i < sdata.Length; i++)
                    sdata[i] = (short)(r.Next() - Int16.MinValue);
            }
            return sdata;
        }

        private static void _Distort(byte[] data, int tone, int distortCt)
        {
            if (distortCt < 1)
                return;
            short[] sdata = new short[data.Length / 2];
            Buffer.BlockCopy(data, 0, sdata, 0, data.Length);

            short[][] distortions = new short[distortCt][];
            for (int i = 0; i < distortCt; i++)
                distortions[i] = _GetDistort(data.Length / 2, tone, i);

            for (int i = 0; i < sdata.Length; i++)
            {
                double distortion = 0;
                for (int j = 0; j < distortCt; j++)
                    distortion += distortions[j][i];
                distortion /= distortCt;
                sdata[i] = (short)(sdata[i] * 4.0 / 5.0 + distortion / 5.0);
            }
            Buffer.BlockCopy(sdata, 0, data, 0, data.Length);
        }

        private static void _GetSineWave(double freq, int sampleRate, int sampleCt, ref double angle, out byte[] data)
        {
            const short max = short.MaxValue;
            short[] data16Bit = new short[sampleCt];
            for (int i = 0; i < sampleCt; i++)
                data16Bit[i] = (short)(Math.Sin(2 * Math.PI * i / sampleRate * freq + angle) * max);
            angle = 2 * Math.PI * sampleCt / sampleRate * freq + angle;
            angle = angle % (2 * Math.PI);
            data = new byte[data16Bit.Length * 2];
            Buffer.BlockCopy(data16Bit, 0, data, 0, data.Length);
        }
        #endregion

        #region File
        private struct STimedNote
        {
            public int Time, Note;
        }

        private void _TestFile(string fileName, string testFileName)
        {
            if (!File.Exists(testFileName))
                return;
            List<STimedNote> tones = new List<STimedNote>();
            using (StreamReader reader = new StreamReader(testFileName))
            {
                String line;
                while ((line = reader.ReadLine()) != null)
                {
                    int p = line.IndexOf(' ');
                    if (p < 0)
                        continue;
                    STimedNote note;
                    note.Time = int.Parse(line.Substring(0, p));
                    note.Note = int.Parse(line.Substring(p + 1));
                    tones.Add(note);
                }
            }
            _TestFile(fileName, tones);
        }

        private void _TestFile(string fileName, int tone)
        {
            STimedNote note;
            note.Note = tone;
            note.Time = 46;
            List<STimedNote> tones = new List<STimedNote> {note};
            _TestFile(fileName, tones);
        }

        private static bool _IsNoteValid(int note, int time, IList<STimedNote> tones)
        {
            const int lastNoteMaxTimeDiff = 1536 * 1000 / 44100; // old note is valid for 1536 more samples
            for (int i = 0; i < tones.Count; i++)
            {
                if (tones[i].Time > time)
                    break;
                if (i + 1 == tones.Count || time <= tones[i + 1].Time + lastNoteMaxTimeDiff)
                {
                    if (tones[i].Note == note || tones[i].Note < 0)
                        return true;
                }
            }
            return false;
        }

        private void _TestFile(string fileName, IList<STimedNote> tones)
        {
            CWavFile wavFile = new CWavFile();
            try
            {
                if (!wavFile.Open(fileName))
                    return;
                if (wavFile.BitsPerSample != 16)
                {
                    wavFile.Close();
                    return;
                }
                int samplesRead = 0;
                int curTimeIndex = -1;
                int curNote = -1;
                const int maxSamplesPerBatch = 512;
                bool[] valids = new bool[_Tones.Length];
                while (wavFile.NumSamplesLeft > maxSamplesPerBatch)
                {
                    byte[] samples = wavFile.GetNextSamples16BitAsBytes(maxSamplesPerBatch, 1);
                    samplesRead += samples.Length / 2;
                    int time = samplesRead * 1000 / wavFile.SampleRate;
                    _Process(samples);
                    while (curTimeIndex + 1 < tones.Count && time >= tones[curTimeIndex + 1].Time)
                    {
                        curTimeIndex++;
                        curNote = tones[curTimeIndex].Note;
                    }
                    if (curNote < 0)
                        continue;
                    _CurTestCount++;
                    bool error = false;
                    for (int i = 0; i < _Tones.Length; i++)
                    {
                        valids[i] = _IsNoteValid(_Tones[i], time, tones);
                        if (valids[i])
                            _CurPassedCount[i]++;
                        else
                            error = true;
                    }
                    if (error)
                    {
                        string msg = "Note " + _ToneToNote(curNote) + " at " + time + "ms detected as ";
                        for (int i = 0; i < _Tones.Length; i++)
                            msg += _ToneToNote(_Tones[i]) + (valids[i] ? "" : "(!)") + "; ";
                        CLog.Debug(msg);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error on file " + fileName + ": " + e);
            }
            wavFile.Close();
        }
        #endregion
    }
}