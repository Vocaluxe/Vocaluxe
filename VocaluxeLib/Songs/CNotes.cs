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

using System.Collections.Generic;

namespace VocaluxeLib.Songs
{
    public class CNotes
    {
        public class CVoiceNames
        {
            private readonly List<string> _Names = new List<string>();

            public CVoiceNames() {}

            public CVoiceNames(CVoiceNames names)
            {
                _Names.AddRange(names._Names);
            }

            public string this[int index]
            {
                get
                {
                    if (IsSet(index))
                        return _Names[index];
                    return "Part " + (index + 1);
                }
                set
                {
                    _Names.EnsureSize(index + 1, null);
                    _Names[index] = value;
                }
            }

            public bool IsSet(int index)
            {
                return _Names.Count > index && !string.IsNullOrEmpty(_Names[index]);
            }

            public void Reset()
            {
                _Names.Clear();
            }
        }

        private readonly List<CVoice> _Voices = new List<CVoice>();
        public readonly CVoiceNames VoiceNames = new CVoiceNames();

        public CNotes() {}

        public CNotes(CNotes notes)
        {
            foreach (CVoice voice in notes._Voices)
                _Voices.Add(new CVoice(voice));
            VoiceNames = new CVoiceNames(notes.VoiceNames);
        }

        public IEnumerable<CVoice> Voices
        {
            get { return _Voices; }
        }

        public int VoiceCount
        {
            get { return _Voices.Count; }
        }

        public CVoice GetVoice(int index, bool add = false)
        {
            if (add)
            {
                while (index >= _Voices.Count)
                    _Voices.Add(new CVoice());
            }
            else if (index >= _Voices.Count)
                return null;

            return _Voices[index];
        }

        public int GetPoints(int index)
        {
            if (index >= _Voices.Count)
                return 0;

            return _Voices[index].Points;
        }

        public int GetNumLinesWithPoints(int index)
        {
            if (index >= _Voices.Count)
                return 0;

            return _Voices[index].NumLinesWithPoints;
        }

        public void AddVoice(CVoice voice)
        {
            _Voices.Add(voice);
        }

        public bool ReplaceVoiceAt(int index, CVoice voice)
        {
            if (index >= _Voices.Count)
                return false;

            _Voices[index] = voice;
            return true;
        }

        public void Reset(bool resetVoices = false)
        {
            _Voices.Clear();
            if (resetVoices)
                VoiceNames.Reset();
        }

        public void SetMedley(int startBeat, int endBeat)
        {
            foreach (CVoice voice in _Voices)
                voice.SetMedley(startBeat, endBeat);
        }
    }
}