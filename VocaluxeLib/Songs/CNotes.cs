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
        private readonly List<CVoice> _Voices = new List<CVoice>();

        public CVoice[] Voices
        {
            get { return _Voices.ToArray(); }
        }

        public CNotes() {}

        public int LinesCount
        {
            get { return _Voices.Count; }
        }

        public CNotes(CNotes notes)
        {
            foreach (CVoice voice in notes._Voices)
                _Voices.Add(new CVoice(voice));
        }

        public CVoice GetVoice(int index)
        {
            while (index >= _Voices.Count)
                _Voices.Add(new CVoice());

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

        public void Reset()
        {
            _Voices.Clear();
        }

        public void SetMedley(int startBeat, int endBeat)
        {
            foreach (CVoice voice in _Voices)
                voice.SetMedley(startBeat, endBeat);
        }
    }
}