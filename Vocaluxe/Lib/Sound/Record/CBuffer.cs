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

#define TEST_PITCH

using System;
#if TEST_PITCH
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using VocaluxeLib;
using VocaluxeLib.Utils;

#endif

namespace Vocaluxe.Lib.Sound.Record
{
    class CBuffer : IDisposable
    {
#if TEST_PITCH
        //Half tones: C C# D D# E F F# G G# A A# B
        private const double _BaseToneFreq = 65.4064; // lowest (half-)tone to analyze (C2 = 65.4064 Hz)
        private const double _HalftoneBase = 1.05946309436; // 2^(1/12) -> HalftoneBase^12 = 2 (one octave)
#endif
        private CPtAKF _Analyzer = new CPtAKF();

        private double _MaxVolume;

        public CBuffer()
        {
            MinVolume = 0.02f;
            ToneWeigths = new float[GetNumHalfTones()];
            Reset();
#if TEST_PITCH
            _TestPitchDetection();
#endif
        }

        ~CBuffer()
        {
            _Dispose();
        }

        public static int GetNumHalfTones()
        {
            return CPtAKF.GetNumHalfTones();
        }

        public int ToneAbs { get; private set; }

        public int Tone { get; set; }

        public float MaxVolume
        {
            get { return (float)_MaxVolume; }
        }

        public bool ToneValid { get; private set; }

        public float[] ToneWeigths { get; private set; }

        /// <summary>
        ///     Minimum volume for a tone to be valid
        /// </summary>
        public float MinVolume { get; set; }

        public void Reset()
        {
            ToneValid = false;
            ToneAbs = 0;
            Tone = 0;
            for (int i = 0; i < ToneWeigths.Length; i++)
                ToneWeigths[i] = 0f;
        }

        public void ProcessNewBuffer(byte[] buffer)
        {
            // apply software boost
            //BoostBuffer(Buffer, BufferSize);

            // voice passthrough (send data to playback-device)
            //if (assigned(fVoiceStream)) then
            //fVoiceStream.WriteData(Buffer, BufferSize);
            _Analyzer.Input(buffer);
        }

        public void AnalyzeBuffer()
        {
            //_MaxVolume = _Analyzer.GetPeak() / 43 + 1;
            int tone = _Analyzer.GetNote(out _MaxVolume, ToneWeigths); //(int)Math.Round(_Analyzer.FindNote()); // 
            if (tone >= 0 && _MaxVolume >= MinVolume)
            {
                ToneAbs = tone;
                Tone = ToneAbs % 12;
                ToneValid = true;
            }
            else
                ToneValid = false;
        }

        public void Dispose()
        {
            _Dispose();
            GC.SuppressFinalize(this);
        }

        private void _Dispose()
        {
            if (_Analyzer != null)
            {
                _Analyzer.Dispose();
                _Analyzer = null;
            }
        }

#if TEST_PITCH
        private static bool _PitchTestRun;

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

        private void _TestPitchDetection()
        {
            if (_PitchTestRun)
                return;
            _PitchTestRun = true;
            const int toneFrom = 0;
            const int toneTo = -1; // 47; //B5
            Console.WriteLine("Testing notes " + _ToneToNote(toneFrom) + " - " + _ToneToNote(toneTo));
            byte[] data;
            byte[] data2 = new byte[2048];
            int ok = 0;
            int tests = 0;
            double angle = 0;
            for (int distort = 0; distort < 10; distort++)
            {
                for (int tone = toneFrom; tone <= toneTo; tone++)
                {
                    _GetSineWave(_BaseToneFreq * Math.Pow(_HalftoneBase, tone), 44100, ref angle, out data);
                    _Distort(data, tone, distort);

                    for (int i = 0; i < 4; i++)
                    {
                        Buffer.BlockCopy(data, i * 2048, data2, 0, 2048);
                        ProcessNewBuffer(data2);
                        AnalyzeBuffer();
                    }
                    if (!ToneValid)
                        CBase.Log.LogDebug("Note " + _ToneToNote(tone) + " not detected (Distortion: " + distort + ")");
                    else if (Tone != tone % 12)
                    {
                        CBase.Log.LogDebug("Note " + _ToneToNote(tone) + " wrongly detected as " + _ToneToNote(Tone, false) + " (Distortion: " + distort + ")");
                        CWavFile file = new CWavFile();
                        file.Create(tone + "-" + distort + ".wav", 1, 44100, 16);
                        file.Write16BitSamples(data, 1);
                        file.Close();
                    }
                    else
                        ok++;
                    tests++;
                }
            }
            /*if (_TestFile("toneG3.wav", 19))
                ok++;
            tests++;
            if (_TestFile("toneG3Miss.wav", 19))
                ok++;
            tests++;
            if (_TestFile("toneG4.wav", 31))
                ok++;
            tests++;*/
            _TestFile("whistling3.wav", "whistling3.txt", ref tests, ref ok);
            _TestFile("sClausVoc.wav", "sClausVoc.txt", ref tests, ref ok);

            _GetSineWave(_BaseToneFreq * Math.Pow(_HalftoneBase, 5), 44100, ref angle, out data);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            const int repeats = 100;
            for (int i = 0; i < repeats; i++)
            {
                ProcessNewBuffer(data);
                AnalyzeBuffer();
            }
            sw.Stop();
            string msg = "Analyser: Errors: " + (tests - ok) + "/" + tests + "; Speed: " +
                         (int)(repeats / (sw.ElapsedMilliseconds / 1000.0)) + " buffers/s";
            Console.WriteLine(msg);
            MessageBox.Show(msg);
            Reset();
        }

        private struct STimedNote
        {
            public int Time, Note;
        }

        private void _TestFile(string fileName, string testFileName, ref int ct, ref int ok)
        {
            if (!File.Exists(testFileName))
            {
                ok = ct = 0;
                return;
            }
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
            _TestFile(fileName, tones, ref ct, ref ok);
        }

        private bool _TestFile(string fileName, int tone)
        {
            STimedNote note;
            note.Note = tone;
            note.Time = 46;
            List<STimedNote> tones = new List<STimedNote> {note};
            int ct = 0;
            int ok = 0;
            _TestFile(fileName, tones, ref ct, ref ok);
            return ok >= ct - 4;
        }

        private bool _IsNoteValid(int note, int time, IList<STimedNote> tones)
        {
            const int lastNoteMaxTimeDiff = 1024 * 1000 / 44100; // old note is valid for 1024 more samples
            for (int i = 0; i < tones.Count; i++)
            {
                if (tones[i].Time > time)
                    break;
                if (i + 1 == tones.Count || time <= tones[i + 1].Time + lastNoteMaxTimeDiff)
                {
                    if (tones[i].Note == note)
                        return true;
                }
            }
            return false;
        }

        private void _TestFile(string fileName, IList<STimedNote> tones, ref int ct, ref int ok)
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
                CAnalyzer analyzer2 = new CAnalyzer();
                CPtDyWa analyzer3 = new CPtDyWa();
                while (wavFile.NumSamplesLeft > maxSamplesPerBatch)
                {
                    byte[] samples = wavFile.GetNextSamples16BitAsBytes(maxSamplesPerBatch, 1);
                    samplesRead += samples.Length / 2;
                    int time = samplesRead * 1000 / wavFile.SampleRate;
                    ProcessNewBuffer(samples);
                    AnalyzeBuffer();
                    analyzer2.Input(samples);
                    analyzer3.Input(samples);
                    analyzer2.Process();
                    analyzer3.Process();
                    while (curTimeIndex + 1 < tones.Count && time >= tones[curTimeIndex + 1].Time)
                    {
                        curTimeIndex++;
                        curNote = tones[curTimeIndex].Note;
                    }
                    if (curNote < 0)
                        continue;
                    int tone1 = ToneValid ? ToneAbs : -1;
                    int tone2 = (int)Math.Round(analyzer2.FindNote(64, 1770));
                    int tone3 = (int)Math.Round(analyzer3.GetNote());
                    /*ct++;
                    if (!ToneValid)
                        CBase.Log.LogDebug("Note " + _ToneToNote(curNote) + " at " + time + "ms not detected");
                    else if (ToneAbs % 12 != curNote % 12)
                        CBase.Log.LogDebug("Note " + _ToneToNote(curNote) + " at " + time + "ms wrongly detected as " + _ToneToNote(ToneAbs));
                    else
                        ok++;*/
                    bool ok1 = _IsNoteValid(tone1, time, tones);
                    bool ok2 = _IsNoteValid(tone2, time, tones);
                    bool ok3 = _IsNoteValid(tone3, time, tones);
                    if (!ok1 || !ok2 || !ok3)
                    {
                        CBase.Log.LogDebug("Note " + _ToneToNote(curNote) + " at " + time + "ms detected as " + _ToneToNote(tone1) + (ok1 ? "" : "(!)") + "; " + _ToneToNote(tone2) +
                                           (ok2 ? "" : "(!)") + "; " + _ToneToNote(tone3) + (ok3 ? "" : "(!)") + "; ");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error on file " + fileName + ": " + e);
            }
            wavFile.Close();
        }

        private static short[] _GetDistort(int len, int tone, int type)
        {
            short[] sdata = new short[len];
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
                _GetSineWave(_BaseToneFreq * Math.Pow(_HalftoneBase, tone + newTone), 44100, ref angle, out data2);
                Buffer.BlockCopy(data2, 0, sdata, 0, len * 2);
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

        private static void _GetSineWave(double freq, int sampleRate, ref double angle, out byte[] data)
        {
            const short max = short.MaxValue;
            const int len = 4096; //sampleRate * durationMs / 1000;
            short[] data16Bit = new short[len];
            for (int i = 0; i < len; i++)
                data16Bit[i] = (short)(Math.Sin(2 * Math.PI * i / sampleRate * freq + angle) * max);
            angle = 2 * Math.PI * len / sampleRate * freq + angle;
            angle = angle % (2 * Math.PI);
            data = new byte[data16Bit.Length * 2];
            Buffer.BlockCopy(data16Bit, 0, data, 0, data.Length);
        }
#endif
    }
}