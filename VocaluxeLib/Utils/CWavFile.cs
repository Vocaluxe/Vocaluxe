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
using System.Diagnostics;
using System.IO;
using System.Text;

namespace VocaluxeLib.Utils
{
    //Simple class to read and write WAV files
    public class CWavFile
    {
        public short NumChannels { get; private set; }
        public int SampleRate { get; private set; }
        public short BitsPerSample { get; private set; }
        public string FileName { get; private set; }
        //Unless this is true, all fields are to be considered as invalid
        public int DataSize { get; private set; }
        //Number of samples (per channel)
        public int NumSamples
        {
            get { return (DataSize / (BitsPerSample / 8 * NumChannels)); }
        }
        //Number of samples left to be read (per channel)
        public int NumSamplesLeft { get; private set; }
        public bool IsOpen { get; private set; }
        private int _DataPos;
        private FileStream _File;
        private bool _IsWritable;

        public CWavFile()
        {
            IsOpen = false;
        }

        ~CWavFile()
        {
            Debug.Assert(!IsOpen);
            Close();
        }

        public void Create(string fileName, short numChannels, int sampleRate, short bitsPerSample)
        {
            if (IsOpen)
                Close();
            _File = new FileStream(fileName, FileMode.Create);
            FileName = fileName;
            NumChannels = numChannels;
            SampleRate = sampleRate;
            BitsPerSample = bitsPerSample;
            DataSize = 0;
            NumSamplesLeft = 0;
            IsOpen = true;
            _IsWritable = true;
            _WriteHeader();
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            if (_IsWritable)
            {
                BinaryWriter writer = new BinaryWriter(_File);
                writer.Seek(4, SeekOrigin.Begin);
                writer.Write((int)_File.Length - 8);
                writer.Seek(_DataPos - 4, SeekOrigin.Begin);
                writer.Write(DataSize);
            }
            _File.Dispose();
            _File = null;
            IsOpen = false;
        }

        public bool Open(string fileName)
        {
            if (IsOpen)
                Close();
            if (!File.Exists(fileName))
                return false;
            FileName = fileName;
            _File = new FileStream(FileName, FileMode.Open);
            try
            {
                BinaryReader reader = new BinaryReader(_File);
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF")
                    return false;
                reader.ReadInt32(); //Chunksize (file)
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE")
                    return false;
                long curPos = _File.Position;
                if (_SeekTo(reader, "fmt ") < 0)
                    return false;
                short audioFormat = reader.ReadInt16();
                if (audioFormat != 1) //PCM
                    return false;
                NumChannels = reader.ReadInt16();
                SampleRate = reader.ReadInt32();
                int bytesPerSec = reader.ReadInt32();
                short blockAlign = reader.ReadInt16();
                BitsPerSample = reader.ReadInt16();
                if (BitsPerSample != 8 && BitsPerSample != 16)
                    return false;
                if (blockAlign != NumChannels * (BitsPerSample / 8))
                    return false;
                if (bytesPerSec != blockAlign * SampleRate)
                    return false;
                _File.Position = curPos;
                DataSize = _SeekTo(reader, "data");
                if (DataSize <= 0)
                    return false;
                _DataPos = (int)_File.Position;
                DataSize = Math.Min(DataSize, (int)_File.Length - _DataPos);
                NumSamplesLeft = NumSamples;
            }
            catch (Exception)
            {
                _File.Dispose();
                throw;
            }
            IsOpen = true;
            return true;
        }

        /// <summary>
        ///     Gets an array of numSamples samples per channel for the selected channel(s)
        /// </summary>
        /// <param name="numSamples">Samples per channel</param>
        /// <param name="channel">Channel for which to get the samples (0=all)</param>
        /// <returns></returns>
        public byte[] GetNextSamples8Bit(int numSamples, int channel = 0)
        {
            if (channel < 0 || channel > NumChannels || BitsPerSample != 8)
                return null;
            if (numSamples > NumSamplesLeft)
                return null;
            byte[] result;
            BinaryReader reader = new BinaryReader(_File);
            if (channel == 0 || NumChannels == 1)
                result = reader.ReadBytes(numSamples * NumChannels);
            else
            {
                result = new byte[numSamples];
                for (int i = 0; i < numSamples; i++)
                {
                    _File.Seek(channel - 1, SeekOrigin.Current);
                    result[i] = reader.ReadByte();
                    _File.Seek(NumChannels - channel, SeekOrigin.Current);
                }
            }
            NumSamplesLeft -= numSamples;
            return result;
        }

        /// <summary>
        ///     Gets an array of numSamples samples per channel for the selected channel(s)
        /// </summary>
        /// <param name="numSamples">Samples per channel</param>
        /// <param name="channel">Channel for which to get the samples (0=all)</param>
        /// <returns></returns>
        public short[] GetNextSamples16Bit(int numSamples, int channel = 0)
        {
            if (channel < 0 || channel > NumChannels || BitsPerSample != 16)
                return null;
            if (numSamples > NumSamplesLeft)
                return null;
            short[] result;
            BinaryReader reader = new BinaryReader(_File);
            try
            {
                if (channel == 0 || NumChannels == 1)
                {
                    result = new short[numSamples * NumChannels];
                    for (int i = 0; i < numSamples * NumChannels; i++)
                        result[i] = reader.ReadInt16();
                }
                else
                {
                    result = new short[numSamples];
                    for (int i = 0; i < numSamples; i++)
                    {
                        _File.Seek((channel - 1) * 2, SeekOrigin.Current);
                        result[i] = reader.ReadInt16();
                        _File.Seek((NumChannels - channel) * 2, SeekOrigin.Current);
                    }
                }
                NumSamplesLeft -= numSamples;
            }
            catch (EndOfStreamException e)
            {
                Console.WriteLine("Error: " + e);
                return null;
            }
            return result;
        }

        public byte[] GetNextSamples16BitAsBytes(int numSamples, int channel = 0)
        {
            short[] samples = GetNextSamples16Bit(numSamples, channel);
            if (samples == null)
                return null;
            byte[] samplesByte = new byte[samples.Length * 2];
            Buffer.BlockCopy(samples, 0, samplesByte, 0, samplesByte.Length);
            return samplesByte;
        }

        public bool WriteSamples(byte[] samples, int channel = 0)
        {
            if (!_IsWritable || channel < 0 || channel > NumChannels || BitsPerSample != 8)
                return false;
            BinaryWriter writer = new BinaryWriter(_File);
            if (channel == 0 || NumChannels == 1)
                writer.Write(samples);
            else
            {
                foreach (byte sample in samples)
                {
                    _File.Seek(channel - 1, SeekOrigin.Current);
                    writer.Write(sample);
                    _File.Seek(NumChannels - channel, SeekOrigin.Current);
                }
            }
            DataSize += samples.Length;
            return true;
        }

        public bool Write16BitSamples(short[] samples, int channel = 0)
        {
            if (!_IsWritable || channel < 0 || channel > NumChannels || BitsPerSample != 16)
                return false;
            BinaryWriter writer = new BinaryWriter(_File);
            if (channel == 0 || NumChannels == 1)
            {
                foreach (short sample in samples)
                    writer.Write(sample);
            }
            else
            {
                foreach (short sample in samples)
                {
                    _File.Seek((channel - 1) * 2, SeekOrigin.Current);
                    writer.Write(sample);
                    _File.Seek((NumChannels - channel) * 2, SeekOrigin.Current);
                }
            }
            DataSize += samples.Length * 2;
            return true;
        }

        public bool Write16BitSamples(byte[] samples, int channel = 0)
        {
            if (samples.Length == 0)
                return true;
            short[] samplesShort = new short[samples.Length / 2];
            Buffer.BlockCopy(samples, 0, samplesShort, 0, samples.Length);
            return Write16BitSamples(samplesShort, channel);
        }

        private void _WriteHeader()
        {
            if (!IsOpen || !_IsWritable)
                return;
            BinaryWriter writer = new BinaryWriter(_File);
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(0);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1); // PCM
            writer.Write(NumChannels); // Channels
            writer.Write(SampleRate); // Sample rate
            writer.Write(SampleRate * NumChannels * (BitsPerSample / 8)); // Average bytes per second
            writer.Write((short)((BitsPerSample / 8) * NumChannels)); // block align
            writer.Write(BitsPerSample); // bits per sample
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(DataSize);
            _DataPos = (int)_File.Position;
            writer.Seek(4, SeekOrigin.Begin);
            writer.Write((int)writer.BaseStream.Length);
            writer.Seek(_DataPos, SeekOrigin.Begin);
        }

        private static int _SeekTo(BinaryReader reader, string chunkId)
        {
            do
            {
                string curChunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                int chunkSize = reader.ReadInt32();
                if (curChunkId == chunkId)
                    return chunkSize;
                reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
            } while (reader.BaseStream.Position < reader.BaseStream.Length);
            return -1;
        }
    }
}