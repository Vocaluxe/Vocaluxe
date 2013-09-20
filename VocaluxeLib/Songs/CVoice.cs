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
using System.Linq;

namespace VocaluxeLib.Songs
{
    public class CVoice
    {
        private readonly List<CSongLine> _Lines = new List<CSongLine>();

        public CVoice() {}

        public CVoice(CVoice voice)
        {
            foreach (CSongLine line in voice._Lines)
                _Lines.Add(new CSongLine(line));
        }

        public CSongLine[] Lines
        {
            get { return _Lines.ToArray(); }
        }
        public int NumLines
        {
            get { return _Lines.Count; }
        }

        /// <summary>
        ///     Total song length in beats
        /// </summary>
        public int Length
        {
            get
            {
                if (_Lines.Count == 0)
                    return 0;

                int result = _Lines[_Lines.Count - 1].EndBeat - _Lines[0].StartBeat;
                return result > 0 ? result : 0;
            }
        }

        public int Points
        {
            get { return _Lines.Sum(line => line.Points); }
        }

        public int NumLinesWithPoints
        {
            get { return _Lines.Count(line => line.Points > 0f); }
        }

        #region Methods
        //Find last line with StartBeat<=Beat
        public int FindPreviousLine(int beat)
        {
            //If no line -> No previous line
            if (_Lines.Count == 0)
                return -1;
            int start = 0;
            int end = _Lines.Count - 1;
            //Ensure that start.StartBeat<=Beat && end.StartBeat>Beat
            if (_Lines[0].StartBeat > beat)
                return -1;
            if (_Lines[end].StartBeat <= beat)
                return end;
            //Binary search
            while (end - start > 1)
            {
                int mid = (start + end) / 2;
                if (_Lines[mid].StartBeat <= beat)
                    start = mid;
                else
                    end = mid;
            }
            return start;
        }

        public void AddLine(CSongLine line)
        {
            if (_Lines.Count == 0)
                _Lines.Add(line);
            else
            {
                int insPos = FindPreviousLine(line.StartBeat);
                _Lines.Insert(insPos + 1, line);
            }
        }

        public bool DeleteLine(int index)
        {
            if (_Lines.Count > index)
            {
                _Lines.RemoveAt(index);
                UpdateTimings();
                return true;
            }
            return false;
        }

        public void DeleteAllLines()
        {
            _Lines.Clear();
        }

        public bool AddNote(CSongNote note, bool updateTimings = false)
        {
            int lineIndex = FindPreviousLine(note.StartBeat);
            if (lineIndex < 0)
            {
                //Note is before ALL lines
                var line = new CSongLine {StartBeat = note.StartBeat};
                line.AddNote(note);
                _Lines.Insert(0, line);
            }
            else
            {
                if (!_Lines[lineIndex].AddNote(note))
                    return false;
            }
            if (updateTimings)
                UpdateTimings();
            return true;
        }

        public bool InsertNote(CSongNote note, int lineIndex, bool updateTimings = false)
        {
            if (_Lines.Count > lineIndex && _Lines[lineIndex].StartBeat <= note.StartBeat)
            {
                _Lines[lineIndex].AddNote(note);
                if (updateTimings)
                    UpdateTimings();
                return true;
            }
            return false;
        }

        public void UpdateTimings()
        {
            CSongNote lastNote;

            if (_Lines.Count > 0)
                _Lines[0].StartBeat = -10000;

            for (int i = 1; i < _Lines.Count; i++)
            {
                lastNote = _Lines[i - 1].LastNote;
                CSongNote firstNote = _Lines[i].FirstNote;

                if ((lastNote != null) && (firstNote != null))
                {
                    int min = lastNote.EndBeat;
                    int max = firstNote.StartBeat;

                    int s;
                    switch (max - min)
                    {
                        case 0:
                            s = max;
                            break;
                        case 1:
                            s = max;
                            break;
                        case 2:
                            s = max - 1;
                            break;
                        case 3:
                            s = max - 2;
                            break;
                        default:
                            s = min + 2;
                            break;
                    }

                    _Lines[i].StartBeat = s;
                    _Lines[i - 1].EndBeat = min;
                }
            }

            lastNote = _Lines[_Lines.Count - 1].LastNote;
            if (lastNote != null)
                _Lines[_Lines.Count - 1].EndBeat = lastNote.EndBeat;
        }

        public void SetMedley(int startBeat, int endBeat)
        {
            foreach (CSongLine line in _Lines)
                line.SetMedley(startBeat, endBeat);
        }
        #endregion Methods
    }
}